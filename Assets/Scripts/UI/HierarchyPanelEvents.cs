using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;

public class HierarchyPanelEvents : MonoBehaviour
{
    public GameObjectManager objectManager;
    public List<HierarchyItem> Items { get; set; }
    private VisualElement root;
    [SerializeField] private VisualElement hierarchyPanel;
    private VisualElement contextMenu; // Само меню
    public Foldout MainHierarchyItem { get; private set; }
    public UIBlocker iBlocker;
    public PropertiesPanelEvents propertiesPanelEvents;

    public ObjectPicker objectPicker;
    private void Awake()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        hierarchyPanel = root.Q("hierarchy-container");
        Debug.Log(hierarchyPanel);
        RegisterElements();
        InitExistedObjects();
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
        Debug.Log("На интерфейсе: " + EventSystem.current.IsPointerOverGameObject());
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
        if(evt.target is VisualElement element)
        {
            var gameObject = objectManager.GetObjectByUniqueID((int)element.userData);
            if (gameObject != null)
            {
                objectPicker.PickObject(gameObject);
            }
        }
    }

    private void AddObject()
    {
        Debug.Log("Добавить объект");
        var cube = objectManager.AddCube();
        var newElement = new Label(cube.name);
        newElement.style.height = 20;
        newElement.style.marginTop = 2;
        newElement.style.marginBottom = 2;
        newElement.userData = cube.GetInstanceID();
        newElement.RegisterCallback<MouseDownEvent>(OnMouseDownHierarchyItem);
        MainHierarchyItem.Add(newElement);
    }
    private void AddObject(GameObject gameObject)
    {
        Debug.Log("Добавить объект");
        var newElement = new Label(gameObject.name);
        newElement.name = gameObject.name;
        newElement.style.height = 20;
        newElement.style.marginTop = 2;
        newElement.style.marginBottom = 2;
        newElement.userData = gameObject.GetInstanceID();
        newElement.RegisterCallback<MouseDownEvent>(OnMouseDownHierarchyItem);
        MainHierarchyItem.Add(newElement);
    }

    private void DeleteObject(VisualElement clickedElement)
    {
        Debug.Log("Удалить объект: " + clickedElement.name);
        clickedElement.UnregisterCallback<MouseDownEvent>(OnMouseDownHierarchyItem);
        MainHierarchyItem.Remove(clickedElement);
        objectPicker.UnpickObject();
        objectManager.DeleteObject((int)clickedElement.userData);
        
    }

    private void ShowProperties(VisualElement clickedElement)
    {
        var propertiesPanel = root.Q("properties-container");
        propertiesPanel.visible = true;
        Debug.Log("Показать свойства для объекта: " + clickedElement.name);
        // Ваш код для отображения свойств объекта
    }
    
    private void RegisterElements()
    {
        MainHierarchyItem = root.Q<Foldout>("main-item");
        root.RegisterCallback<MouseDownEvent>(OnMouseDown);
        //hierarchyPanel.RegisterCallback<MouseDownEvent>(OnMouseDown);
        Debug.Log(MainHierarchyItem);
        //RegisterContextMenu();
        
    }

    private void InitExistedObjects()
    {
        var objects = objectManager.GetGameObjectsList();
        foreach (var obj in objects)
        {
            AddObject(obj);
        }
    }

    public void UpdateHierarhy()
    {
        MainHierarchyItem.Clear();
        InitExistedObjects();
    }
}
