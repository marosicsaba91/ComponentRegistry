#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MUtility;
using Object = UnityEngine.Object;
using EasyInspector;

namespace ComponentRegistrySystem
{
	class ComponentRegistryWindow : EditorWindow
	{
		const string editorPrefsKey = "ComponentRegistryWindowState";

		[MenuItem("Tools/Component Registry")]
		static void ShowWindow()
		{
			ComponentRegistryWindow window = GetWindow<ComponentRegistryWindow>();
			window.titleContent = new GUIContent("Component Registry");
			window.Show();
		}

		void OnGUI() => Draw(new Rect(0, 0, position.width, position.height));

		void Draw(Rect position)
		{
			float columnW = (position.width) / 2;
			float rightX = position.x + columnW;

			Rect leftContentPosition = new (position.x, position.y, columnW, position.height);
			Rect rightContentPosition = new (rightX, position.y, columnW, position.height);

			DrawLeftSideContent(leftContentPosition);
			DrawRightSideContent(rightContentPosition);
		}

		[SerializeField] string selectedComponentType;

		Type SelectedComponentType
		{
			get
			{
				if (selectedComponentType == null)
					return null;
				try
				{
					return Type.GetType(selectedComponentType);
				}
				catch (Exception)
				{
					selectedComponentType = null;
					return null;
				}
			}
			set => selectedComponentType = value?.AssemblyQualifiedName;
		}

		protected void DrawLeftSideContent(Rect position)
			=> DrawComponentTypes(position);

		protected void DrawRightSideContent(Rect position)
			=> DrawSelectedComponents(position);

		void DrawComponentTypes(Rect position)
		{
			Rect linePos = position;
			linePos.height = EditorGUIUtility.singleLineHeight;


			List<ComponentTypeInfo> infos = new();

			foreach (Type componentType in ComponentRegistry.RegistrableTypes())
			{
				int allCount = Application.isPlaying
					? ComponentRegistry.Count(componentType)
					: ComponentRegistry.FindObjectsOfType(componentType, enabledOnly: false).Count;

				infos.Add(new ComponentTypeInfo
				{
					type = componentType,
					allCount = allCount
				});
			}

			GenerateComponentTypesTable();
			_componentTypesTable.Draw(position, infos);
		}


		void DrawSelectedComponents(Rect position)
		{
			if (SelectedComponentType == null)
			{
				Rect pos = position;
				pos.height = Mathf.Min(pos.height, 150);
				GUI.Label(pos, "No IRegisteredComponent type selected.", Alignment.Center.ToGUIStyle());
				return;
			}

			List<Component> componentInstances = ComponentRegistry.GetAll(SelectedComponentType)
				.Cast<Component>().ToList();

			GenerateComponentInstancesTable();
			_componentInstancesTable.Draw(position, componentInstances);
		}

		struct ComponentTypeInfo
		{
			public Type type;
			public int allCount;
			public int enabledCount;
		}

		GUITable<ComponentTypeInfo> _componentTypesTable;
		GUITable<Component> _componentInstancesTable;

		void GenerateComponentTypesTable()
		{
			if (_componentTypesTable != null)
				return;
			List<IColumn<ComponentTypeInfo>> componentTypeColumns = new()
			{
			new LabelColumn<ComponentTypeInfo>(info => info.type.Name, new ColumnInfo
			{
				titleGetter = () => "Type",
				fixWidth = 0,
				relativeWidthWeight = 1,
			}),
			new LabelColumn<ComponentTypeInfo>(info => info.enabledCount.ToString(), new ColumnInfo
			{
				title = "Enabled",
				fixWidth = 60,
				headerAlignment = Alignment.Right,
				style = Alignment.Right.ToGUIStyle(),
			}),
			new LabelColumn<ComponentTypeInfo>(info => info.allCount.ToString(), new ColumnInfo
			{
				title = "All",
				fixWidth = 60,
				headerAlignment = Alignment.Right,
				style = Alignment.Right.ToGUIStyle(),
				customHeaderDrawer = DrawCheckboxHeader
			}),
		};

			_componentTypesTable = new GUITable<ComponentTypeInfo>(componentTypeColumns, this)
			{
				clickOnRow = (index, info) =>
					SelectedComponentType = SelectedComponentType == info.type ? null : info.type,
				isRowHighlightedGetter = (index, info) => SelectedComponentType == info.type,
				emptyCollectionTextGetter = () => "No IRegisteredComponent type is in the project."
			};
		}

		void DrawCheckboxHeader(Rect position)
		{
			const int titleWidth = 38;
			Rect labelPos = new(position.xMax - titleWidth, position.y, titleWidth, position.height);
			GUI.Label(labelPos, "All");
		}

		void GenerateComponentInstancesTable()
		{
			if (_componentInstancesTable != null)
				return;
			List<IColumn<Component>> componentInstanceColumns = new()
			{
			new CheckboxColumn<Component>(
				IsSelected,
				SetSelection,
				new ColumnInfo
				{
					fixWidth = 19,
					relativeWidthWeight = 0,
					customHeaderDrawer = DrawSelectAllCheckbox
				}),
			new LabelColumn<Component>(
				component => component.name,
				new ColumnInfo
				{
					titleGetter = () => (Application.isPlaying? "All Awaken": "All") +
										" " + SelectedComponentType.Name + " Objects",
					fixWidth = 0,
					relativeWidthWeight = 2,
				}),
			new LabelColumn<Component>(
				component => component.GetType().Name,
				new ColumnInfo
				{
					titleGetter = () => "Type",
					fixWidth = 0,
					relativeWidthWeight = 1,
				})
		};

			_componentInstancesTable = new GUITable<Component>(componentInstanceColumns, this)
			{
				clickOnRow = ClickOnRow,
				isRowHighlightedGetter = (index, component) => IsSelected(component),
				emptyCollectionTextGetter = () => "No instance of the selected type is active in Scene."
			};
		}

		void DrawSelectAllCheckbox(Rect position)
		{

			List<Component> componentInstances = ComponentRegistry.GetAll(SelectedComponentType)
				.Cast<Component>().ToList();
			bool isAllSelected = componentInstances
				.All(component => Selection.objects.Contains(component.gameObject));

			const float checkBoxWidth = 14;
			float x = position.x + ((position.width - checkBoxWidth) / 2);
			Rect pos = new(x, position.y, checkBoxWidth, position.height);
			bool newValue = EditorGUI.Toggle(pos, isAllSelected);
			if (isAllSelected == newValue)
				return;

			Selection.objects = newValue
				? componentInstances.Select(component => (Object)component.gameObject).ToArray()
				: Array.Empty<Object>();

		}

		static bool IsSelected(Component trackableComponent) =>
			Selection.Contains(trackableComponent.gameObject);

		static void ClickOnRow(int index, Component trackableComponent)
		{
			if (Selection.objects.Length == 1 && Selection.objects[0] == trackableComponent.gameObject)
				Selection.objects = Array.Empty<Object>();
			else
				Selection.objects = new Object[] { trackableComponent.gameObject };
		}

		static void SetSelection(Component trackableComponent, bool isSelected)
		{
			Object obj = trackableComponent.gameObject;
			bool alreadyIn = Selection.objects.Contains(obj);
			if (isSelected)
			{
				if (alreadyIn)
					return;
				Object[] alreadySelected = Selection.objects;
				Object[] newSelection = new Object[alreadySelected.Length + 1];
				int i = 0;
				foreach (Object selected in alreadySelected)
				{
					newSelection[i] = selected;
					i++;
				}

				newSelection[i] = obj;
				Selection.objects = newSelection;
			}
			else
			{
				if (!alreadyIn)
					return;
				Object[] alreadySelected = Selection.objects;
				Object[] newSelection = new Object[alreadySelected.Length - 1];
				int i = 0;
				foreach (Object selected in alreadySelected)
				{
					if (selected == obj)
						continue;
					newSelection[i] = selected;
					i++;
				}

				Selection.objects = newSelection;
			}
		}

		public void OnEnable()
		{
			wantsMouseMove = true;
			ComponentRegistry.ComponentListChanged += OnComponentsChanged;


			string data = EditorPrefs.GetString(
				editorPrefsKey, JsonUtility.ToJson(this, prettyPrint: false));
			JsonUtility.FromJsonOverwrite(data, this);
		}

		public void OnDisable()
		{
			ComponentRegistry.ComponentListChanged -= OnComponentsChanged;

			string data = JsonUtility.ToJson(this, prettyPrint: false);
			EditorPrefs.SetString(editorPrefsKey, data);
		}

		void OnComponentsChanged() => Repaint();
	}
}
#endif
