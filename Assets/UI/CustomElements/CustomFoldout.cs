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
    private Image imageContainer;

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
    public Texture2D HeaderImage
    {
        get => imageContainer.image as Texture2D;
        set
        {
            if (value != null)
            {
                imageContainer.image = value;
                imageContainer.style.display = DisplayStyle.Flex;
            }
            else
            {
                imageContainer.style.display = DisplayStyle.None;
            }
        }
    }
    public CustomFoldout()
    {
        // Применяем класс для кастомного foldout
        this.AddToClassList("custom-foldout");

        // Заголовок (header)
        header = new VisualElement();
        header.name = "foldout-header";
        header.AddToClassList("header");
        header.style.flexDirection = FlexDirection.Row;
        Add(header);

        // Кнопка для раскрытия
        toggleButton = new Button(() => Toggle()) { text = "+" };
        toggleButton.AddToClassList("toggle-button");
        header.Add(toggleButton);

        imageContainer = new Image();
        imageContainer.AddToClassList("header-image");
        imageContainer.style.width = 16;
        imageContainer.style.height = 16;
        imageContainer.style.marginRight = 8;
        imageContainer.style.paddingLeft = 4;
        imageContainer.style.display = DisplayStyle.None; // По умолчанию скрыто
        header.Add(imageContainer);

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
    public void SetHeaderImage(Texture2D texture, int width = 16, int height = 16)
    {
        if (texture != null)
        {
            imageContainer.style.backgroundImage = new StyleBackground(texture);
            imageContainer.style.width = width;
            imageContainer.style.height = height;
            imageContainer.style.display = DisplayStyle.Flex;
        }
        else
        {
            imageContainer.style.display = DisplayStyle.None;
        }
    }
    public void AddImage(VisualElement element)
    {
        header.Add(element);
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
