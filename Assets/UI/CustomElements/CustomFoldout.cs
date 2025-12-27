using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class CustomFoldout : VisualElement
{

    public bool IsExpanded { get; private set; }
    private VisualElement header;
    private Label label;
    private Button toggleButton;
    private VisualElement content;
    private string text;

    [UxmlAttribute]
    public string Text
    {
        get => text;
        set
        {
            text = value;
            label.text = Text;
        }
    }

    public CustomFoldout()
    {
        // Применяем класс для кастомного foldout
        this.AddToClassList("custom-foldout");

        // Заголовок (header)
        header = new VisualElement();
        header.AddToClassList("header");
        header.style.flexDirection = FlexDirection.Row;
        Add(header);

        // Кнопка для раскрытия
        toggleButton = new Button(() => Toggle()) { text = "+" };
        toggleButton.AddToClassList("toggle-button");
        header.Add(toggleButton);

        // Метка с текстом
        label = new Label(text);
        label.style.flexGrow = 1;
        header.Add(label);

        // Контент
        content = new VisualElement();
        content.AddToClassList("content");
        content.style.display = DisplayStyle.None;
        Add(content);
    }
    public void ClearContent()
    {
        content.Clear();
    }
    public void SetSelected(bool selected)
    {
        if (selected)
        {
            header.AddToClassList("selected");
        }
        else
        {
            header.RemoveFromClassList("selected");
        }
    }
    public void AddContent(VisualElement element)
    {
        content.Add(element);
        Disp();
    }

    private void Disp()
    {
        if (content.childCount > 0)
        {
            content.style.display = DisplayStyle.Flex;
            IsExpanded = true;
            toggleButton.text = IsExpanded ? "-" : "+";
        }
        else
        {
            content.style.display = DisplayStyle.None;
            toggleButton.text = IsExpanded ? "-" : "+";
        }
        
    }
    private void Toggle()
    {
        IsExpanded = !IsExpanded;
        if (content.childCount > 0)
        {
            content.style.display = IsExpanded ? DisplayStyle.Flex : DisplayStyle.None;
        }
        toggleButton.text = IsExpanded ? "-" : "+";
    }
}
