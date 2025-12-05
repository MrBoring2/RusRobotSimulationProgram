using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    [Header("UI Files")]
    public VisualTreeAsset perspectivePanel;
    public VisualTreeAsset hierarchyPanel;
    public VisualTreeAsset propertiesPanel;
    public VisualTreeAsset menuBar;

    void Start()
    {
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // Находим контейнеры
        var perspectiveContainer = root.Q<VisualElement>("perspective-container");
        var hierarchyContainer = root.Q<VisualElement>("hierarchy-container");
        var propertiesContainer = root.Q<VisualElement>("properties-container");
        var menuBarContainer = root.Q<VisualElement>("menu-bar-container");

        // Загружаем панели в контейнеры
        perspectivePanel.CloneTree(perspectiveContainer);
        hierarchyPanel.CloneTree(hierarchyContainer);
        propertiesPanel.CloneTree(propertiesContainer);
        menuBar.CloneTree(menuBarContainer);
    }
}
