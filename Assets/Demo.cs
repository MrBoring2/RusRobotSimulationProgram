using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class Demo : EditorWindow
{
    [MenuItem("Window/ContextualMenuDemo")]
    public static void ShowExample()
    {
        Demo wnd = GetWindow<Demo>();
        wnd.titleContent = new GUIContent("ContextualMenuDemo");
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        VisualElement label = new Label("Right click me!");
        root.Add(label);

        AddANewContextMenu(label);
        InsertIntoAnExistingMenu(label);

        VisualElement second = new Label("Click me also!");
        root.Add(second);

        AddANewContextMenu(second);
        InsertIntoAnExistingMenu(second);

        // Override the default behavior by clearing the menu.
        ReplaceContextMenu(second);
    }

    void AddANewContextMenu(VisualElement element)
    {
        // The manipulator handles the right click and sends a ContextualMenuPopulateEvent to the target element.
        // The callback argument passed to the constructor is automatically registered on the target element.
        element.AddManipulator(new ContextualMenuManipulator((evt) =>
        {
            evt.menu.AppendAction("First menu item", (x) => Debug.Log("First!!!!"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Second menu item", (x) => Debug.Log("Second!!!!"), DropdownMenuAction.AlwaysEnabled);
        }));
    }

    void InsertIntoAnExistingMenu(VisualElement element)
    {
        element.RegisterCallback<ContextualMenuPopulateEvent>((evt) =>
        {
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Another action", (x) => Debug.Log("Another Action!!!!"), DropdownMenuAction.AlwaysEnabled);
        });
    }

    void ReplaceContextMenu(VisualElement element)
    {
        element.RegisterCallback<ContextualMenuPopulateEvent>((evt) =>
        {
            evt.menu.ClearItems();
            evt.menu.AppendAction("The only action", (x) => Debug.Log("The only action!"), DropdownMenuAction.AlwaysEnabled);
        });
    }

}