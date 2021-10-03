using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ComponentDatabasesSystem
{
public class ComponentDatabase
{
    readonly bool _isGlobal = false;
    public static ComponentDatabase global = new ComponentDatabase(global: true);

    public ComponentDatabase() { }

    public ComponentDatabase(bool global)
    {
        _isGlobal = global;
    }

    internal readonly Dictionary<Type, IList<object>> componentTypeToObjects = new Dictionary<Type, IList<object>>();

    public void AddComponent(MonoBehaviour registeredComponent)
    {
        if(registeredComponent== null)
            return;
        
        foreach (Type interF in GetAllRegistrableSubclassesOf(registeredComponent.GetType()))
        {
            if (!componentTypeToObjects.ContainsKey(interF))
                componentTypeToObjects.Add(interF, new List<object>());
            componentTypeToObjects[interF].Add(registeredComponent);
        }
    }

    public IEnumerable<T> GetComponents<T>(bool enabledOnly) => GetComponents(typeof(T), enabledOnly).Cast<T>();

    internal IEnumerable<object> GetComponents(Type type, bool enabledOnly)
    {
        if (_isGlobal && !Application.isPlaying)
        {
            foreach (object element in FindObjectsOfType(type, enabledOnly))
                yield return element;
            yield break;
        }

        if (!componentTypeToObjects.ContainsKey(type))
            yield break;

        IList<object> list = componentTypeToObjects[type];
        for (var i = 0; i < list.Count; i++)
        {
            object element = list[i];
            if (element == null)
            {
                list.RemoveAt(i);
                i--;
            }

            if (!enabledOnly || ((MonoBehaviour)element).isActiveAndEnabled)
                yield return list[i];
        }
    }
    
    // STATIC
    internal static IReadOnlyList<object> FindObjectsOfType(Type type, bool enabledOnly)
    {
        IEnumerable<GameObject> gameObjects = enabledOnly
            ? Object.FindObjectsOfType<GameObject>()
            : FindAllObjects();
        return gameObjects.SelectMany(gameObject => gameObject.GetComponents(type)).ToList();
    }

    static IEnumerable<GameObject> FindAllObjects()
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
    
    static List<Type> _allTypes = null;

    static IReadOnlyList<Type> AllTypes => _allTypes ?? 
                                           (_allTypes = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                               from type in assembly.GetTypes() select type).ToList());

    static Dictionary<Type, List<Type>> _typeToRegistrableTypeDictionary;

    internal static IReadOnlyList<Type> GetAllRegistrableSubclassesOf(Type parent)
    {
        if (_typeToRegistrableTypeDictionary == null)
        {
            _typeToRegistrableTypeDictionary = new Dictionary<Type, List<Type>>();
            foreach (Type baseClass in AllTypes)
            {
                List<Type> children = FindAllRegistrableSubclassesOf(baseClass).ToList();
                _typeToRegistrableTypeDictionary.Add(baseClass, children);
            }
        }

        return _typeToRegistrableTypeDictionary[parent];

    }
    static IEnumerable<Type> FindAllRegistrableSubclassesOf(Type parent) => 
        AllTypes.Where(t => t.GetInterfaces().Contains(parent) && IsRegistrableType(t));

    internal static bool IsRegistrableType(Type type)
    {
        var inheritedAttribute = (ComponentDatabaseTypeAttribute)
            Attribute.GetCustomAttribute(type, typeof(ComponentDatabaseTypeAttribute), inherit: false);
        return inheritedAttribute != null;
    }
}
}