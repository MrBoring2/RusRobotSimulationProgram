using Assets.Scripts.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class HierarchyPanelEvents : MonoBehaviour
{
    public GameObjectManager objectManager;
    public List<HierarchyItem> Items { get; set; } = new List<HierarchyItem>();
    private VisualElement root;
    [SerializeField] private VisualElement hierarchyPanel;
    private VisualElement contextMenu;
    public Foldout MainHierarchyItem { get; private set; }
    public UIBlocker iBlocker;
    public PropertiesPanelEvents propertiesPanelEvents;

    public ObjectPicker objectPicker;
    private void Awake()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        hierarchyPanel = root.Q("hierarchy-container");
        Debug.Log(hierarchyPanel);
        propertiesPanelEvents.OnTargetNameChanged += PropertiesPanelEvents_OnTargetNameChanged;
        RegisterElements();
        InitExistedObjects();
    }

    private void PropertiesPanelEvents_OnTargetNameChanged()
    {
        Debug.Log("IPDATE");
        UpdateHierarchy();
    }

    //private void OnEnable()
    //{
    //    // Получаем корень UI
    //    root = GetComponent<UIDocument>().rootVisualElement;

    //    // Находим панель иерархии
    //    hierarchyPanel = root.Q<VisualElement>("hierarchy-container");

    //    // Находим Foldout, куда будем добавлять элементы
    //    MainHierarchyItem = root.Q<Foldout>("main-item");

    //    // Подписываемся на событие мыши
    //    hierarchyPanel.RegisterCallback<MouseDownEvent>(OnMouseDown);
    //    InitExistedObjects();
    //}

    private void OnMouseDown(MouseDownEvent evt)
    {
        if (evt.button == 1) // ПКМ
        {
            // Проверяем, был ли клик внутри панели иерархии
            if (IsInsideHierarchyPanel(evt.target as VisualElement))
            {
                ShowContextMenu(evt.mousePosition, evt.target as VisualElement);
                evt.StopPropagation();
            }
            else
            {
                HideContextMenu();
            }
        }
        else
        {
            HideContextMenu(); // любой другой клик закрывает меню
        }
        evt.StopPropagation();
    }
    private bool IsInsideHierarchyPanel(VisualElement element)
    {
        while (element != null)
        {
            if (element == hierarchyPanel)
                return true;
            element = element.parent;
        }
        return false;
    }

    private void ShowContextMenu(Vector2 position, VisualElement clickedElement)
    {
        // Если уже есть, удаляем старое меню
        if (contextMenu != null)
            root.Remove(contextMenu);

        contextMenu = new VisualElement();
        contextMenu.AddToClassList("context-menu-hierarchy-container");
        contextMenu.style.position = Position.Absolute;
        contextMenu.style.left = position.x;
        contextMenu.style.top = position.y;
        contextMenu.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        contextMenu.style.borderTopWidth = 1;
        contextMenu.style.borderBottomWidth = 1;
        contextMenu.style.borderLeftWidth = 1;
        contextMenu.style.borderRightWidth = 1;
        contextMenu.style.borderBottomColor = Color.black;
        contextMenu.style.borderTopColor = Color.black;
        contextMenu.style.borderLeftColor = Color.black;
        contextMenu.style.borderRightColor = Color.black;
        contextMenu.style.flexDirection = FlexDirection.Column;
        contextMenu.style.paddingTop = 2;
        contextMenu.style.paddingBottom = 2;
        contextMenu.style.paddingLeft = 4;
        contextMenu.style.paddingRight = 4;

        // Кнопки меню
        if (clickedElement != null && clickedElement != hierarchyPanel)
        {
            // Кликнули на объект
            contextMenu.Add(CreateMenuButton("Удалить объект", () => DeleteObject(clickedElement)));
            contextMenu.Add(CreateMenuButton("Свойства", () => ShowProperties(clickedElement)));
        }
        else
        {
            // Кликнули на пустое место
            contextMenu.Add(CreateMenuButton("Добавить объект", AddObject));
        }

        root.Add(contextMenu);
        iBlocker.AddNewContextMenu(contextMenu);
    }

    private Button CreateMenuButton(string text, System.Action action)
    {
        var btn = new Button(() =>
        {
            action?.Invoke();
            HideContextMenu();
        });
        btn.text = text;
        btn.style.unityTextAlign = TextAnchor.MiddleLeft;
        btn.style.height = 20;
        btn.style.width = 150;
        btn.style.marginBottom = 2;
        return btn;
    }
    private void HideContextMenu()
    {
        if (contextMenu != null)
        {
            iBlocker.RemoveContextMenu(contextMenu);
            root.Remove(contextMenu);
            contextMenu = null;
        }
    }


    private void OnMouseDownHierarchyItem(MouseDownEvent evt)
    {
        if (evt.button == 0)
        {
            if (evt.target is VisualElement element)
            {
                var gameObject = objectManager.GetObjectByUniqueID((int)element.userData);
                if (gameObject != null)
                {
                    objectPicker.PickObject(gameObject);
                }
            }
        }
    }

    private void AddObject()
    {
        Debug.Log("Добавить объект");
        var cube = objectManager.AddCube();
        var m = cube.AddComponent<SceneObjectMarker>();
        m.type = ObjectType.Primitive;
        m.sourcePath = "";
        Items.Add(new HierarchyItem(cube.GetInstanceID(), cube));
        UpdateHierarchy();
    }
    private void AddObject(GameObject gameObject)
    {
        Debug.Log("Добавить объект");
        var m = gameObject.AddComponent<SceneObjectMarker>();
        m.type = ObjectType.Primitive;
        m.sourcePath = "";
        Items.Add(new HierarchyItem(gameObject.GetInstanceID(), gameObject));
        UpdateHierarchy();
    }


    private void UpdateHierarchy()
    {
        MainHierarchyItem.Clear();
        foreach (var item in Items)
        {
            Debug.Log(item.Reference.name);
            var newElement = new Label(item.Reference.name);
            newElement.style.height = 20;
            newElement.style.marginTop = 2;
            newElement.style.marginBottom = 2;
            newElement.userData = item.Id;
            newElement.RegisterCallback<MouseDownEvent>(OnMouseDownHierarchyItem);
            MainHierarchyItem.Add(newElement);
        }
    }

    private void DeleteObject(VisualElement clickedElement)
    {
        Debug.Log("Удалить объект: " + clickedElement.name);
        clickedElement.UnregisterCallback<MouseDownEvent>(OnMouseDownHierarchyItem);
        Items.Remove(Items.FirstOrDefault(p => p.Id == (int)clickedElement.userData));
        UpdateHierarchy();
        objectPicker.UnpickObject();
        objectManager.DeleteObject((int)clickedElement.userData);

    }

    private void ShowProperties(VisualElement clickedElement)
    {
        var obj = objectManager.GetObjectByUniqueID((int)clickedElement.userData);
        if (obj != null)
        {
            var provider = obj.TryGetComponent<IPropertyProvider>(out IPropertyProvider d);
            if (d != null)
            {
                propertiesPanelEvents.ShowPanel();
                propertiesPanelEvents.ShowProperties(d);
            }
        }

        Debug.Log("Показать свойства для объекта: " + clickedElement.name);
        // Ваш код для отображения свойств объекта
    }

    private void RegisterElements()
    {
        MainHierarchyItem = root.Q<Foldout>("main-item");
        root.RegisterCallback<MouseDownEvent>(OnMouseDown);
    }

    private void InitExistedObjects()
    {
        var objects = objectManager.GetGameObjectsList();
        foreach (var obj in objects)
        {
            AddObject(obj);
        }
        UpdateHierarchy();
    }

    public void LoadHierarchy()
    {
        Items.Clear();
        InitExistedObjects();
    }
}
