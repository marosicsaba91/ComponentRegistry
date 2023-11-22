using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ComponentRegistrySystem
{
	abstract class ComponentList : IEnumerable<object>
	{
		public abstract void Add(object element);

		public abstract IEnumerator<object> GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	class ComponentList<T> : ComponentList
	{
		readonly List<T> _list;

		public ComponentList(List<T> list)
		{
			_list = list;
		}

		public override IEnumerator<object> GetEnumerator()
		{
			return (IEnumerator<object>)GetEnumeratorTyped();
		}

		public IEnumerator<T> GetEnumeratorTyped()
		{
			for (int i = _list.Count - 1; i >= 0; i--)
			{
				T element = _list[i];
				if (element != null)
					yield return element;
				else
					_list.RemoveAt(i);
			}
		}

		public override void Add(object element) => _list.Add((T)element);
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
					where IsRegistrableComponent(t)
					select t;
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
				// TODO: Add new element
				//IList list = CreateListOfType(registrableType);
				//registrableTypeToObjects.Add(registrableType, new ComponentList(list));
			}
		}

		public static IEnumerable<T> GetAll<T>() => (IEnumerable<T>)GetAll(typeof(T));

		public static IEnumerable<object> GetAll(Type t)
		{
			if (!Application.isPlaying)
				return FindObjectsOfType(t, enabledOnly: false);

			CheckRegistrableTypeToObjectMap(t);
			IEnumerable<object> cl = registrableTypeToObjects[t];
			return cl;
		}

		public static int Count(Type type) => GetAll(type).Count();


		// Util
		internal static IReadOnlyList<T> FindObjectsOfType<T>(bool enabledOnly) where T : Component =>
			(IReadOnlyList<T>)FindObjectsOfType(typeof(T), enabledOnly);

		internal static IReadOnlyList<Component> FindObjectsOfType(Type type, bool enabledOnly)
		{
			IEnumerable<GameObject> gameObjects = enabledOnly
				? Object.FindObjectsOfType<GameObject>()
				: GetAllObjects();
			return gameObjects.SelectMany(gameObject => gameObject.GetComponents(type)).ToList();
		}

		static IEnumerable<GameObject> GetAllObjects()
		{
			var rootObjects = new List<GameObject>();
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				UnityEngine.SceneManagement.Scene scene = SceneManager.GetSceneAt(i);
				if (!scene.isLoaded)
					continue;
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
			if (type.ContainsGenericParameters)
				return false;
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
