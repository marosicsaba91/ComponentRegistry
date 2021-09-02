using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine;
using UnityEngine.SceneManagement;
 using MUtility;
 using Object = UnityEngine.Object;

namespace ComponentRegistrySystem
{
public static class ComponentRegistry
{
    static Type[] _nonAbstractClasses;
    static Type[] _collectableClasses;
    static Dictionary<Type, List<Type>> _typeToInterfacesMap;
    
    internal class ComponentDatabase
    {
        public Dictionary<Type, IList> interfaceToComponents;
        event Action ComponentListChanged;

        internal ComponentDatabase(Action componentListChanged)
        {
            ComponentListChanged = componentListChanged;
        }

        internal void AddComponent(IRegisteredComponent registeredComponent, Type type) 
        {
            InitializeDatabase();
            foreach (Type interF in _typeToInterfacesMap[type])
                interfaceToComponents[interF].Add(registeredComponent);
            ComponentListChanged?.Invoke();
        }
         
        public void RemoveComponent(IRegisteredComponent registeredComponent, Type type)
        {
            InitializeDatabase();
            
            for (int i = _typeToInterfacesMap[type].Count - 1; i >= 0; i--)
            {
                Type interF = _typeToInterfacesMap[type][i];
                interfaceToComponents[interF].Remove(registeredComponent);
            }

            ComponentListChanged?.Invoke();
        }

        public IReadOnlyList<T> GetComponents<T>(bool enabledOnly) where T : IRegisteredComponent
        {
            InitializeDatabase();
            if (!Application.isPlaying)
                return FindObjectsOfType<T>(enabledOnly);

            Type type = typeof(T);
            return interfaceToComponents.ContainsKey(type) ? (IReadOnlyList<T>) interfaceToComponents[type] : null;
        }

        internal IReadOnlyList<object> GetComponents(Type type, bool enabledOnly)
        { 
            
            InitializeDatabase();
            if (!Application.isPlaying)
                return FindObjectsOfType(type, enabledOnly);

            return interfaceToComponents.ContainsKey(type) ? (IReadOnlyList<object>) interfaceToComponents[type] : null;
        }

        public void InitializeDatabase()
        {
            if (interfaceToComponents != null) return;
            
            InitTypeMap();
            
            interfaceToComponents = new Dictionary<Type, IList>();
            foreach (Type type in _collectableClasses)
                    interfaceToComponents.Add(type, CreateListOfType(type));
        }
    }
    
    

    static void InitTypeMap()
    {
        if (_typeToInterfacesMap != null) return;
        
        
        _nonAbstractClasses = GetAllNonAbstractSubclassOf(typeof(IRegisteredComponent)).ToArray();
        _collectableClasses = GetAllCollectableSubclassOf(typeof(IRegisteredComponent)).ToArray();
        _typeToInterfacesMap = new Dictionary<Type, List<Type>>();
        
        foreach (Type type in _nonAbstractClasses)
        {
            List<Type> interfaces = GetIRegisteredComponentParentTypes(type, _collectableClasses);
            _typeToInterfacesMap.Add(type, interfaces);
        }
    }


    /// <summary>
    /// Registrate any IRegisteredComponent. Must be called in Awake
    /// </summary>  
    public static void RegistrateComponent(IRegisteredComponent registeredComponent)
    {
        registeredComponent.Enabled += () => Enabled(registeredComponent);
        registeredComponent.Disabled += () => Disabled(registeredComponent);
        registeredComponent.Destroyed += () => Destroyed(registeredComponent);
        Awaken(registeredComponent);
    }
    
    /// <summary>
    /// Returns all awaken component of T Type
    /// </summary>  
    public static IReadOnlyList<T> GetAll<T>() where T : IRegisteredComponent =>
        all.GetComponents<T>(false);
    
    /// <summary>
    /// Returns all awaken component of type Type
    /// </summary>  
    public static IReadOnlyList<object> GetAll(Type type) =>
        all.GetComponents(type, false);
    
    /// <summary>
    /// Returns all active & enabled component of T Type
    /// </summary>  
    public static IReadOnlyList<T> GetAllEnabled<T>() where T : IRegisteredComponent  =>
        enabled.GetComponents<T>(true);
    
    /// <summary>
    /// Returns all active & enabled component of type Type
    /// </summary>  
    public static IReadOnlyList<object> GetAllEnabled(Type type) =>
        enabled.GetComponents(type, true);
    

    // All
    internal static readonly ComponentDatabase all = new ComponentDatabase(OnAllComponentListChanged);
    static void OnAllComponentListChanged() => AllComponentListChanged?.Invoke();
    
    internal static event Action AllComponentListChanged;

    internal static void Awaken<T>(T component) where T : IRegisteredComponent =>
        all.AddComponent(component, component.GetType());
    
    internal static void Awaken (IRegisteredComponent registeredComponent, Type type) => all.AddComponent(registeredComponent, type);

    internal static void Destroyed<T>(T component) where T : IRegisteredComponent =>
        all.RemoveComponent(component, component.GetType());
    
    internal static void Destroyed (IRegisteredComponent registeredComponent, Type type) => all.RemoveComponent(registeredComponent, type);


    // Enabled
    internal static readonly ComponentDatabase enabled = new ComponentDatabase(OnEnabledComponentListChanged);
    internal static void OnEnabledComponentListChanged() => EnabledComponentListChanged?.Invoke();
    
    internal static event Action EnabledComponentListChanged;

    internal static void Enabled<T>(T component) where T : IRegisteredComponent =>
        enabled.AddComponent(component, component.GetType());
    
    internal static void Enabled (IRegisteredComponent registeredComponent, Type type) => 
        enabled.AddComponent(registeredComponent, type);
    internal static void Disabled<T>(T component) where T : IRegisteredComponent =>
        enabled.RemoveComponent(component, component.GetType());
    
    internal static void Disabled (IRegisteredComponent registeredComponent, Type type) => 
        enabled.RemoveComponent(registeredComponent, type);

    // Init
    internal static void Initialize()
    {
        all.InitializeDatabase();
        enabled.InitializeDatabase();
    }
    static IReadOnlyList<T> FindObjectsOfType<T>(bool enabledOnly)
    {
        IEnumerable<GameObject> gameObjects = enabledOnly
            ? Object.FindObjectsOfType<GameObject>()
            : GetAllObjects();

        return gameObjects.SelectMany(gameObject => gameObject.GetComponents<T>()).ToList();
    }

    internal static IReadOnlyList<object> FindObjectsOfType(Type type, bool enabledOnly)
    {
        IEnumerable<GameObject> gameObjects = enabledOnly
            ? Object.FindObjectsOfType<GameObject>()
            : GetAllObjects();
        return gameObjects.SelectMany(gameObject => gameObject.GetComponents(type)).ToList();
    }

    static IEnumerable<GameObject> GetAllObjects()
    {
        var rootObjects = new List<GameObject>();
        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            scene.GetRootGameObjects(rootObjects);
 
            foreach (Transform result in rootObjects.SelectMany(rootObject => rootObject.transform.SelfAndAllChildrenRecursively()))
                yield return result.gameObject;
        }
    }

    static List<Type> GetIRegisteredComponentParentTypes(Type type, Type[] collectableClasses)
    {  
        List<Type> iRegisteredComponentParents = type.GetInterfaces().Where(collectableClasses.Contains).ToList();

        while (true)
        {
            if (type != null && type != typeof(IRegisteredComponent))
            {
                if (collectableClasses.Contains(type))
                    iRegisteredComponentParents.Add(type);
            }
            else
                break;
            type = type.BaseType;
        }

        return iRegisteredComponentParents;
    }

    static IEnumerable<Type> GetAllNonAbstractSubclassOf(Type parent) => 
        from assembly in AppDomain.CurrentDomain.GetAssemblies() 
        from type in assembly.GetTypes() 
        where type.GetInterfaces().Contains(parent) 
              && !type.ContainsGenericParameters 
              && !type.IsAbstract 
              && !type.IsInterface 
        select type;

    static IEnumerable<Type> GetAllCollectableSubclassOf(Type parent) => 
        from assembly in AppDomain.CurrentDomain.GetAssemblies() 
        from type in assembly.GetTypes() 
        where type.GetInterfaces().Contains(parent) 
              && !type.IsNonRegistrableComponent() 
        select type;

    static IList CreateListOfType(Type myType)
    {
        Type genericListType = typeof(List<>).MakeGenericType(myType);
        return (IList) Activator.CreateInstance(genericListType);
    }
}
}
