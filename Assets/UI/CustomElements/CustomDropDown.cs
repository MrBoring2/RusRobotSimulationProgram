using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI.MessageBox;

namespace Assets.UI.CustomElements
{
    public class CustomDropdown : VisualElement
    {
        private Label _label;
        private Label _arrow;
        private List<string> _options;
        private List<object> _values;
        private object _currentValue;
        private string _displayProperty;

        public event Action<object> OnValueChanged;
        public event Action OnDropdownOpen;
        public event Action OnDropdownClose;

        public object value
        {
            get => _currentValue;
            set
            {
                _currentValue = value;
                UpdateLabel();
            }
        }

        public CustomDropdown() : this(new List<string>(), new List<object>(), null) { }

        public CustomDropdown(List<string> options, List<object> values, object currentValue, string displayProperty = null)
        {
            _options = options ?? new List<string>();
            _values = values ?? new List<object>();
            _currentValue = currentValue;
            _displayProperty = displayProperty;

            this.AddToClassList("custom-dropdown");
            style.flexDirection = FlexDirection.Row;
            style.height = 24;
            style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
            style.paddingLeft = 8;
            style.paddingRight = 4;

            _label = new Label();
            _label.style.flexGrow = 1;
            _label.style.unityTextAlign = TextAnchor.MiddleLeft;
            _label.style.color = Color.white;
            Add(_label);

            _arrow = new Label("▼");
            _arrow.style.width = 20;
            _arrow.style.unityTextAlign = TextAnchor.MiddleCenter;
            _arrow.style.color = Color.white;
            Add(_arrow);

            UpdateLabel();

            this.RegisterCallback<ClickEvent>(evt =>
            {
                var menu = new GenericDropdownMenu();
                for (int i = 0; i < _options.Count; i++)
                {
                    int index = i;
                    menu.AddItem(_options[i], false, () =>
                    {
                        _currentValue = _values[index];
                        _label.text = _options[index];
                        OnValueChanged?.Invoke(_currentValue);
                        OnDropdownClose?.Invoke();
                    });
                }
                menu.DropDown(this.worldBound, this, DropdownMenuSizeMode.Auto);
                OnDropdownOpen?.Invoke();
                evt.StopPropagation();
            });

            this.focusable = true;
        }

        private void UpdateLabel()
        {
            var idx = _values.IndexOf(_currentValue);
            if (idx >= 0)
                _label.text = GetDisplayValue(_values[idx]);
            else
                _label.text = _currentValue?.ToString() ?? "";
        }

        private string GetDisplayValue(object obj)
        {
            if (obj == null) return "";
            if (string.IsNullOrEmpty(_displayProperty)) return obj.ToString();

            var prop = obj.GetType().GetProperty(_displayProperty);
            if (prop != null) return prop.GetValue(obj)?.ToString() ?? obj.ToString();

            var field = obj.GetType().GetField(_displayProperty);
            if (field != null) return field.GetValue(obj)?.ToString() ?? obj.ToString();

            return obj.ToString();
        }
    }
}