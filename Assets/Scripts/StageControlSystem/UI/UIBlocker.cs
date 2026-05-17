using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using System.Collections.Generic;
using System.Windows.Forms;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

public class UIBlocker : MonoBehaviour
{
    private VisualElement root;
    //public bool isPointerOverUI { get; private set; }
    //public bool isInputMode { get; private set; }
    private UIStatusManager _uiStatusManager;
    private List<string> uiElements = new List<string>();

    public void Start()
    {
        _uiStatusManager = ServiceManager.Current.Get<UIStatusManager>();
        root = GetComponent<UIDocument>().rootVisualElement;
        // Определяем список панелей, по которым нужно отслеживать курсор
        var a = root.Q("left-column");
        uiElements = new List<string>
        {
            "menu-bar-container",
            "main-menu-container",
            "left-column",
            "properties-container",
            //root.Q("hierarchy-container"),
            //root.Q("panel-divider"),
            //root.Q("hierarchy-commands-container"),
            "perspective-panel-container",
            "notification-container"
        };
        // Debug.Log(root);
        // Регистрируем события для каждой панели
        foreach (var panel in uiElements)
        {
           // panel.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            //panel.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }
    }

    public void AddNewContextMenu(VisualElement contextMenu)
    {
      //  contextMenu.RegisterCallback<MouseEnterEvent>(OnContextMenuMouseEnter);
      //  contextMenu.RegisterCallback<MouseLeaveEvent>(OnContextMenuMouseLeave);
    }

    public void AddNewModalWindow(VisualElement modalWindow)
    {
      //  modalWindow.RegisterCallback<MouseEnterEvent>(OnContextMenuMouseEnter);
       // modalWindow.RegisterCallback<MouseLeaveEvent>(OnContextMenuMouseLeave);
    }
    public void RemoveModalWindow(VisualElement modalWindow)
    {
        //uiElements.Remove(modalWindow);
       // modalWindow.UnregisterCallback<MouseEnterEvent>(OnContextMenuMouseEnter);
       // modalWindow.UnregisterCallback<MouseLeaveEvent>(OnContextMenuMouseLeave);
    }

    public void RemoveContextMenu(VisualElement contextMenu)
    {
        //uiElements.Remove(contextMenu);
       // contextMenu.UnregisterCallback<MouseEnterEvent>(OnContextMenuMouseEnter);
       // contextMenu.UnregisterCallback<MouseLeaveEvent>(OnContextMenuMouseLeave);
        //!!!!если чо вернуть///
        //ResolveUI();
    }

    public bool CheckIsOnUI()
    {
        Vector2 screenPos = new Vector2(Input.mousePosition.x, UnityEngine.Screen.height - Input.mousePosition.y);
        var panel = GetComponent<UIDocument>().rootVisualElement.panel;
        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(panel, screenPos);
        VisualElement picked = panel.Pick(panelPos);

        if (picked != null)
        {
            // Проверяем является ли элемент или его родитель одной из UI панелей
            VisualElement current = picked;
            while (current != null)
            {
                if (uiElements.Contains(current.name))
                {
                    return true;
                }
                current = current.parent;
            }
        }
        return false;
    }

    public void ResolveUI()
    {
       // _uiStatusManager?.SetPointerOverUI(false);
    }

    private void OnMouseEnter(MouseEnterEvent evt)
    {
        _uiStatusManager?.SetPointerOverUI(true);  // Когда курсор заходит на панель 
    }

    private void OnMouseLeave(MouseLeaveEvent evt)
    {
        _uiStatusManager?.SetPointerOverUI(false); // Когда курсор покидает панель
    }

    private void OnModalWindowMouseEnter(MouseEnterEvent evt)
    {
        _uiStatusManager.SetPointerOverUI(true);
    }
    private void OnModalWindowMouseLeaveLeave(MouseLeaveEvent evt)
    {
        _uiStatusManager.SetPointerOverUI(false);
    }
    private void OnContextMenuMouseEnter(MouseEnterEvent evt)
    {
       // _uiStatusManager?.SetPointerOverUI(true);
    }

    private void OnContextMenuMouseLeave(MouseLeaveEvent evt)
    {
       // _uiStatusManager?.SetPointerOverUI(false);
    }
    private void UnregisterUIElements()
    {
        if (uiElements == null || uiElements.Count == 0) return;
        foreach (var panel in uiElements)
        {
            //panel.UnregisterCallback<MouseEnterEvent>(OnMouseEnter);
            //panel.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }
    }

    public void EnableInputMode()
    {
        //_uiStatusManager?.SetInputMode(true);
    }

    public void DisableInputMode()
    {
       // _uiStatusManager?.SetInputMode(false);
    }
    private void OnDisable()
    {
        UnregisterUIElements();
    }
    private void OnDestroy()
    {
        UnregisterUIElements();
    }
}
