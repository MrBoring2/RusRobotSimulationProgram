using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class CustomFoldout : VisualElement
{
    public event Action<bool> OnExpandedChanged;
    public bool IsExpanded { get; private set; }
    public List<VisualElement> Childrens { get; private set; } = new List<VisualElement>();
    private VisualElement header;
    public VisualElement Header => header;
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
    public void ClearContent(bool expand = false)
    {
        content.Clear();
        IsExpanded = expand;
        UpdateVisualState();
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
    public void AddChild(VisualElement element)
    {
        Childrens.Add(element);
        content.Add(element);
        //IsExpanded = true;
        UpdateVisualState();
    }
    
    public void SetExpanded(bool expanded)
    {
        if (IsExpanded != expanded)
        {
            IsExpanded = expanded;
            UpdateVisualState();

            // Вызываем событие при изменении состояния
            OnExpandedChanged?.Invoke(IsExpanded);
        }
    }
    private void Toggle()
    {
        IsExpanded = !IsExpanded;
        UpdateVisualState();
        // Вызываем событие только если состояние изменилось

        OnExpandedChanged?.Invoke(IsExpanded);

    }
    private void UpdateVisualState()
    {
        bool hasChildren = content.childCount > 0;

        content.style.display =
            (IsExpanded && hasChildren)
            ? DisplayStyle.Flex
            : DisplayStyle.None;

        toggleButton.text = (IsExpanded && hasChildren) ? "-" : "+";
    }
}
