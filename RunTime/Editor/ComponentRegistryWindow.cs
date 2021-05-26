#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MUtility;
using Object = UnityEngine.Object;

namespace ComponentRegistrySystem
{
class ComponentRegistryWindow : EditorWindow
{
    const string editorPrefsKey = "ComponentRegistryWindowState";
    
    [MenuItem("Tools/Component Registry")]
    static void ShowWindow()
    {
        var window = GetWindow<ComponentRegistryWindow>();
        window.titleContent = new GUIContent("Component Registry");
        window.Show();
    } 

    void OnGUI() => Draw(new Rect(0,0, position.width, position.height));

    void Draw(Rect position)
    { 
        float columnW = (position.width) / 2;
        float rightX = position.x + columnW;

        var leftContentPosition = new Rect(position.x, position.y, columnW, position.height);
        var rightContentPosition = new Rect(rightX, position.y, columnW, position.height);

        DrawLeftSideContent(leftContentPosition);
        DrawRightSideContent(rightContentPosition);
    }

    [SerializeField] string selectedComponentType;

    Type SelectedComponentType
    {
        get
        {
            if (selectedComponentType == null) return null;
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

    [SerializeField] bool enabledOnly = false;
    
    protected void DrawLeftSideContent(Rect position)
        => DrawComponentTypes(position);

    protected void DrawRightSideContent(Rect position)
        => DrawSelectedComponents(position);

    void DrawComponentTypes(Rect position)
    {
        ComponentRegistry.ComponentDatabase selectedDatabase = enabledOnly ? ComponentRegistry.enabled : ComponentRegistry.all;

        Rect linePos = position;
        linePos.height = EditorGUIUtility.singleLineHeight;
        ComponentRegistry.Initialize();

        var infos = new List<ComponentTypeInfo>();
        foreach (KeyValuePair<Type, IList> typeToComponents in selectedDatabase.interfaceToComponents)
        {
            Type componentType = typeToComponents.Key;

            int allCount, enabledCount;
            if (Application.isPlaying)
            {
                allCount = ComponentRegistry.GetAll(componentType).Count;
                enabledCount = ComponentRegistry.GetAllEnabled(componentType).Count;
            }
            else
            {
                allCount = ComponentRegistry.FindObjectsOfType(componentType, enabledOnly: false).Count;
                enabledCount = ComponentRegistry.FindObjectsOfType(componentType, enabledOnly: true).Count;
            }

            infos.Add(new ComponentTypeInfo
            {
                type = componentType,
                allCount = allCount,
                enabledCount = enabledCount
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

        ComponentRegistry.ComponentDatabase selectedDatabase = enabledOnly ? ComponentRegistry.enabled : ComponentRegistry.all;
        List<IRegisteredComponent> componentInstances = selectedDatabase.GetComponents(SelectedComponentType, enabledOnly)
            .Cast<IRegisteredComponent>().ToList();

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
    GUITable<IRegisteredComponent> _componentInstancesTable;

    void GenerateComponentTypesTable()
    {
        if (_componentTypesTable != null) return;
        var componentTypeColumns = new List<IColumn<ComponentTypeInfo>>
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
        const int toggleWidth = 17;
        var togglePos = new Rect(position.xMax - toggleWidth, position.y, toggleWidth, position.height);

        enabledOnly = !EditorGUI.Toggle(togglePos, !enabledOnly);
        const int titleWidth = 38;
        var labelPos = new Rect(position.xMax - titleWidth, position.y, titleWidth, position.height);
        GUI.Label(labelPos, "All");
    }

    void GenerateComponentInstancesTable()
    {
        if (_componentInstancesTable != null) return;
        var componentInstanceColumns = new List<IColumn<IRegisteredComponent>>
        {
            new CheckboxColumn<IRegisteredComponent>(
                IsSelected,
                SetSelection,
                new ColumnInfo
                {
                    fixWidth = 19,
                    relativeWidthWeight = 0,
                    customHeaderDrawer = DrawSelectAllCheckbox
                }),
            new LabelColumn<IRegisteredComponent>(
                component => component.ToMonoBehaviour().name,
                new ColumnInfo
                {
                    titleGetter = () => 
                        (enabledOnly ? "All Enabled": (Application.isPlaying? "All Awaken": "All")) +
                        " " + SelectedComponentType.Name + " Objects",
                    fixWidth = 0,
                    relativeWidthWeight = 2,
                }),
            new LabelColumn<IRegisteredComponent>(
                component => component.GetType().Name,
                new ColumnInfo
                {
                    titleGetter = () => "Concrete Type",
                    fixWidth = 0,
                    relativeWidthWeight = 1,
                })
        };

        _componentInstancesTable = new GUITable<IRegisteredComponent>(componentInstanceColumns, this)
        {
            clickOnRow = ClickOnRow,
            isRowHighlightedGetter = (index, component) => IsSelected(component),
            emptyCollectionTextGetter = () => "No instance of the selected type is active in Scene."
        };
    }

    void DrawSelectAllCheckbox(Rect position)
    {
        ComponentRegistry.ComponentDatabase selectedDatabase = enabledOnly ? ComponentRegistry.enabled : ComponentRegistry.all;
        List<IRegisteredComponent> componentInstances = selectedDatabase.GetComponents(SelectedComponentType, enabledOnly)
            .Cast<IRegisteredComponent>().ToList();
        bool isAllSelected = componentInstances
            .All(component => Selection.objects.Contains(component.GetGameObject()));

        const float checkBoxWidth = 14;
        float x = position.x + ((position.width - checkBoxWidth) / 2);
        var pos = new Rect(x, position.y, checkBoxWidth, position.height);
        bool newValue = EditorGUI.Toggle(pos, isAllSelected);
        if (isAllSelected == newValue) return;

        Selection.objects = newValue
            ? componentInstances.Select(component => (Object) component.GetGameObject()).ToArray()
            : new Object[0];

    }

    static bool IsSelected(IRegisteredComponent registeredComponent) =>
        Selection.Contains(registeredComponent.GetGameObject());

    static void ClickOnRow(int index, IRegisteredComponent registeredComponent)
    {
        if (Selection.objects.Length == 1 && Selection.objects[0] == registeredComponent.GetGameObject())
            Selection.objects = new Object[0];
        else
            Selection.objects = new Object[] {registeredComponent.GetGameObject()};
    }

    static void SetSelection(IRegisteredComponent registeredComponent, bool isSelected)
    {
        Object obj = registeredComponent.GetGameObject();
        bool alreadyIn = Selection.objects.Contains(obj);
        if (isSelected)
        {
            if (alreadyIn) return;
            Object[] alreadySelected = Selection.objects;
            var newSelection = new Object [alreadySelected.Length + 1];
            var i = 0;
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
            if (!alreadyIn) return;
            Object[] alreadySelected = Selection.objects;
            var newSelection = new Object [alreadySelected.Length - 1];
            var i = 0;
            foreach (Object selected in alreadySelected)
            {
                if (selected == obj) continue;
                newSelection[i] = selected;
                i++;
            }

            Selection.objects = newSelection;
        }
    }

    public void OnEnable()
    {
        wantsMouseMove = true;
        ComponentRegistry.EnabledComponentListChanged += OnComponentsChanged;
        ComponentRegistry.AllComponentListChanged += OnComponentsChanged;
        
        
        string data = EditorPrefs.GetString(
            editorPrefsKey, JsonUtility.ToJson(this, prettyPrint: false));
        JsonUtility.FromJsonOverwrite(data, this);
    }

    public void OnDisable()
    {
        ComponentRegistry.EnabledComponentListChanged -= OnComponentsChanged;
        ComponentRegistry.AllComponentListChanged -= OnComponentsChanged;
        
        string data = JsonUtility.ToJson(this, prettyPrint: false);
        EditorPrefs.SetString(editorPrefsKey, data);
    }

    void OnComponentsChanged() => Repaint();
}
}
#endif
