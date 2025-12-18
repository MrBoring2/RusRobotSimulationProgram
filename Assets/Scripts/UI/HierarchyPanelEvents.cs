using Assets.Scripts.Models;
using Assets.Scripts.SystemManager;
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
    public ObjectsLibraryEvents objectsLibraryEvents;
    private void Awake()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        hierarchyPanel = root.Q("hierarchy-container");
        propertiesPanelEvents.OnTargetNameChanged += PropertiesPanelEvents_OnTargetNameChanged;
        objectsLibraryEvents.OnObjectSelected += ObjectsLibraryEvents_OnObjectSelected;
        objectManager.OnObjectAdded += ObjectManager_OnObjectAdded;
        objectManager.OnObjectRemoved += ObjectManager_OnObjectRemoved;
        UndoRedoSystem.Instance.OnCommandExecuted += Instance_OnCommandExecuted;
        UndoRedoSystem.Instance.OnCommandUndone += Instance_OnCommandUndone;
        RegisterElements();
        InitExistedObjects();
    }

    private void Instance_OnCommandUndone(ICommand obj)
    {
        if(obj is IDestructiveCommand)
        {
            UpdateHierarchy();
        }
    }

    private void Instance_OnCommandExecuted(ICommand obj)
    {
        if (obj is IDestructiveCommand)
        {
            UpdateHierarchy();
        }
    }

    private void ObjectManager_OnObjectRemoved(GameObject obj)
    {
        Items.RemoveAll(i => i.Id == obj.GetInstanceID());
        UpdateHierarchy();
    }

    private void ObjectManager_OnObjectAdded(GameObject obj)
    {
        Items.Add(new HierarchyItem(obj.GetInstanceID(), obj));
        UpdateHierarchy();
    }

    //private void ObjectsLibraryEvents_OnObjectSelected(string path, ObjectType type, GameObject obj)
    private void ObjectsLibraryEvents_OnObjectSelected(GameObject prefab)
    {
        AddObject(prefab);
    }

    private void PropertiesPanelEvents_OnTargetNameChanged()
    {
        UpdateHierarchy();
    }
    private void OnMouseDown(MouseDownEvent evt)
    {
        if (evt.button == 1)
        {
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
            HideContextMenu();
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
        //Debug.Log("CliCK " + clickedElement);
        if (clickedElement != null && clickedElement.name == "hierarchy-item")
        {
            contextMenu.Add(CreateMenuButton("Удалить объект", () => DeleteObject(clickedElement)));
            //contextMenu.Add(CreateMenuButton("Свойства", () => ShowProperties(clickedElement)));
        }
        else
        {
            // Кликнули на пустое место
            contextMenu.Add(CreateMenuButton("Добавить объект", CreateObject));
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
                var a = objectManager.Get
                var gameObject = objectManager.GetObjectByUniqueID((int)element.userData);
                Debug.Log(gameObject.GetInstanceID());
                if (gameObject != null)
                {
                    objectPicker.PickObject(gameObject);
                    ShowProperties(element);
                }
            }
        }
    }

    private void CreateObject()
    {
        objectsLibraryEvents.Show();
    }
    private void AddObject(GameObject prefab)
    {
        if (gameObject == null)
            return;

        if (!Items.Any(x => x.Reference.GetInstanceID() == gameObject.GetInstanceID()))
        {
            objectPicker.UnpickObject();
            var command = new AddObjectCommand(objectManager, prefab, Vector3.zero);
            UndoRedoSystem.Instance.Execute(command);
        }
    }


    private void UpdateHierarchy()
    {
        MainHierarchyItem.Clear();
        foreach (var item in Items)
        {
            if (item.Reference.activeSelf == false) continue;
            // Debug.Log(item.Reference.name);
            var newElement = new Label(item.Reference.name);
            var a = item.Reference.GetInstanceID();
            newElement.name = "hierarchy-item";
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
        int id = (int)clickedElement.userData;
        var obj = objectManager.GetObjectByUniqueID(id);

        if (obj == null)
            return;
        objectPicker.UnpickObject();
        propertiesPanelEvents.HidePanel();
        var command = new RemoveObjectCommand(objectManager, obj);
        UndoRedoSystem.Instance.Execute(command);
        UpdateHierarchy();
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
            LoadObject(obj);
        }
        UpdateHierarchy();
    }

    private void LoadObject(GameObject obj)
    {
        Items.Add(new HierarchyItem(obj.GetInstanceID(), obj));
    }

    public void LoadHierarchy()
    {
        Items.Clear();
        InitExistedObjects();
    }
}
