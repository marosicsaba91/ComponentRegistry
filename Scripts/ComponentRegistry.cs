using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MarosiUtility;
using UnityEngine;
using UnityEngine.SceneManagement;
 using MUtility;
 using Object = UnityEngine.Object;

namespace ComponentRegistrySystem
{


class ComponentList : IEnumerable
{
    readonly IList _list;

    public ComponentList(IList list)
    {
        _list = list;
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator GetEnumerator()
    {
        for (int i = _list.Count - 1; i >= 0; i--)
        {
            object element = _list[i];
            if (element != null)
                yield return element;
            else
                _list.RemoveAt(i);
        }
    }

    internal void Add(object element) => _list.Add(element);
}


public static class ComponentRegistry
{

    static List<Type> _registrableTypes;
    static readonly Dictionary<Type, List<Type>> componentTypeToRegistrableTypeMap = 
        new Dictionary<Type, List<Type>>();
    static readonly Dictionary<Type, ComponentList> registrableTypeToObjects = new Dictionary<Type, ComponentList>();

    public static IEnumerable<Type> RegistrableTypes()
    {
        if (_registrableTypes == null)
        { 
            IEnumerable<Type> typeList = 
                from a in ReflectionHelper.FindRelevantAssemblies() 
                from t in a.GetTypes() 
                where IsRegistrableComponent(t) select t;
            _registrableTypes = typeList.ToList();
        }

        return _registrableTypes;
    }
    
    internal static event Action ComponentListChanged;

    public static void AutoRegistrate(Component componentToRegistrate) 
    {
        Type type = componentToRegistrate.GetType();
        CheckComponentTypeToRegistrableTypeMapInit(type);

        foreach (Type registrableType in componentTypeToRegistrableTypeMap[type])
        {
            CheckRegistrableTypeToObjectMap(registrableType);
            registrableTypeToObjects[registrableType].Add(componentToRegistrate);
        }

        ComponentListChanged?.Invoke();
    }

    public static void Registrate(Component componentToRegistrate, params Type[] types) 
    {
        foreach (Type registrableType in types)
        {
            CheckRegistrableTypeToObjectMap(registrableType);
            registrableTypeToObjects[registrableType].Add(componentToRegistrate);
        }

        ComponentListChanged?.Invoke();
    }

    static void CheckComponentTypeToRegistrableTypeMapInit(Type componentType)
    {
        if (!componentTypeToRegistrableTypeMap.ContainsKey(componentType))
            componentTypeToRegistrableTypeMap.Add(componentType, GetAllRegistrableTypeOf(componentType).ToList());
    }

    static void CheckRegistrableTypeToObjectMap(Type registrableType)
    {
        if (!registrableTypeToObjects.ContainsKey(registrableType))
        {
            var list = (IList) GetAllRegistrableTypeOf(registrableType).ToList();
            registrableTypeToObjects.Add(registrableType, new ComponentList(list));
        }
    }

    public static IEnumerable<T> GetAll<T>() =>
        (IEnumerable<T>) GetAll(typeof(T));
    
    public static IEnumerable GetAll(Type type)
    {
        if (!Application.isPlaying)
            return FindObjectsOfType(type, enabledOnly: false);

        CheckRegistrableTypeToObjectMap(type);
        return (IReadOnlyList<object>)registrableTypeToObjects[type];
    } 
    
    public static int Count(Type type) => GetAll(type).Cast<object>().Count();


    // Util

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

            foreach (Transform result in rootObjects.SelectMany(rootObject =>
                rootObject.transform.SelfAndAllChildrenRecursively()))
                yield return result.gameObject;
        }
    }

    static IEnumerable<Type> GetAllRegistrableTypeOf(Type type)
    {
        foreach (Type interfaceType in type.GetInterfaces())
        {
            if (IsRegistrableComponent(interfaceType))
                yield return interfaceType;
        }

        Type baseType = type;
        while (baseType != null)
        {
            if (IsRegistrableComponent(baseType))
                yield return baseType;
            baseType = baseType.BaseType;
        }
    }

    static bool IsRegistrableComponent(Type type)
    {
        if (type.ContainsGenericParameters) return false;
        var attribute = Attribute.GetCustomAttribute(type, typeof(RegistrableAttribute), inherit: false);
        return attribute != null;
    }

    static IList CreateListOfType(Type myType)
    {
        Type genericListType = typeof(List<>).MakeGenericType(myType);
        return (IList)Activator.CreateInstance(genericListType);
    }
}
}
