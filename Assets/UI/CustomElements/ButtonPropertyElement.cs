using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI.MessageBox;

namespace Assets.UI.CustomElements
{
    public class ButtonPropertyElement : VisualElement
    {
        private Button _button;

        public event Action OnClicked;

        public ButtonPropertyElement(string text)
        {
            AddToClassList("base-property");
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.height = 28;

            _button = new Button();
            _button.text = text;
            _button.style.flexGrow = 1;
            _button.style.height = 26;
            _button.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            _button.style.color = Color.white;
            _button.style.fontSize = 13;
            _button.style.borderTopLeftRadius = 4;
            _button.style.borderTopRightRadius = 4;
            _button.style.borderBottomLeftRadius = 4;
            _button.style.borderBottomRightRadius = 4;
            _button.style.borderTopWidth = 1;
            _button.style.borderBottomWidth = 1;
            _button.style.borderLeftWidth = 1;
            _button.style.borderRightWidth = 1;
            _button.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f);
            _button.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
            _button.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f);
            _button.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);

            _button.clicked += () => OnClicked?.Invoke();

            Add(_button);
        }

        public void SetText(string text)
        {
            _button.text = text;
        }
    }
}