using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.StageControlSystem.Models;
using Assets.UI.CustomElements;
using Assets.UI.CustomElements.ColorField;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.StageControlSystem.Utils
{
    public static class PropertyFieldFactory
    {
        public static VisualElement CreateField(CustomProperty property, out Action<object> setValue, out Func<object> getValue, out VisualElement fieldElement)
        {
            var type = property.PropertyType;
            var currentValue = property.Getter();

            if (property.TryGetAttribute<DropdownOptionsAttribute>(out var dropdownAttr))
                return CreateDropdownField(property, dropdownAttr.Options.ToArray(), dropdownAttr.Values.ToArray(), currentValue, out setValue, out getValue, out fieldElement);

            if (type == typeof(float) && property.TryGetAttribute<RangeAttribute>(out var rangeAttr))
                return CreateSliderField(property, (float)currentValue, rangeAttr.min, rangeAttr.max, out setValue, out getValue, out fieldElement);

            if (type == typeof(float))
                return CreateFloatField(property, (float)currentValue, out setValue, out getValue, out fieldElement);

            if (type == typeof(int))
                return CreateIntField(property, (int)currentValue, out setValue, out getValue, out fieldElement);

            if (type == typeof(bool))
                return CreateBoolField(property, (bool)currentValue, out setValue, out getValue, out fieldElement);

            if (type == typeof(string))
                return CreateStringField(property, (string)currentValue, out setValue, out getValue, out fieldElement);

            if (type.IsEnum)
                return CreateEnumField(property, (Enum)currentValue, out setValue, out getValue, out fieldElement);

            if (type == typeof(Color))
            {
                return CreateColorField(property, (Color)(currentValue ?? Color.white), out setValue, out getValue, out fieldElement);
            }

            if (property is ButtonProperty buttonProp)
            {
                return CreateButtonField(buttonProp, out setValue, out getValue, out fieldElement);
            }
            return CreateStringField(property, currentValue?.ToString() ?? "", out setValue, out getValue, out fieldElement);
        }

        private static VisualElement CreateSliderField(CustomProperty prop, float current, float min, float max,
            out Action<object> setValue, out Func<object> getValue, out VisualElement fieldElement)
        {
            var container = new VisualElement();
            container.AddToClassList("custom-slider");
            container.AddToClassList("base-property");
            container.Add(new Label(prop.DisplayName));

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;

            var slider = new Slider(min, max) { value = current, style = { flexGrow = 1 } };
            var floatField = new FloatField() { value = current, style = { width = 60 } };
            slider.RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                slider.Blur();
            });
            //slider.RegisterCallback<KeyDownEvent>(evt =>
            //{
            //    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            //    {
            //        slider.Blur();

            //        slider.RemoveFromClassList("unity-base-slider--movable");
            //        container.Focus();
            //    }
            //});

            slider.RegisterValueChangedCallback(evt =>
            {
                floatField.SetValueWithoutNotify(evt.newValue);
                prop.Setter(evt.newValue);
            });
            floatField.RegisterValueChangedCallback(evt => slider.SetValueWithoutNotify(evt.newValue));

            row.Add(slider);
            row.Add(floatField);
            container.Add(row);

            setValue = val => { floatField.value = (float)val; slider.value = (float)val; };
            getValue = () => floatField.value;
            fieldElement = floatField;
            fieldElement.name = "slider-field";
            return container;
        }
        private static VisualElement CreateButtonField(
    ButtonProperty property,
    out Action<object> setValue,
    out Func<object> getValue,
    out VisualElement fieldElement)
        {
            var container = new VisualElement();
            container.AddToClassList("base-property");
            container.Add(new Label(property.DisplayName));

            var button = new Button();
            button.text = property.ButtonText;
            button.clicked += () => property.OnClick?.Invoke();
            container.Add(button);

            setValue = val => { };
            getValue = () => null;
            fieldElement = button;

            return container;
        }
        private static VisualElement CreateFloatField(CustomProperty prop, float current,
            out Action<object> setValue, out Func<object> getValue, out VisualElement fieldElement)
        {
            var container = new VisualElement();
            container.AddToClassList("base-property");
            container.Add(new Label(prop.DisplayName));
            var field = new FloatField { value = current };
            container.Add(field);
            setValue = val => field.value = (float)val;
            getValue = () => field.value;
            fieldElement = field;
            return container;
        }

        private static VisualElement CreateIntField(CustomProperty prop, int current,
            out Action<object> setValue, out Func<object> getValue, out VisualElement fieldElement)
        {
            var container = new VisualElement();
            container.AddToClassList("base-property");
            container.Add(new Label(prop.DisplayName));
            var field = new IntegerField { value = current };
            container.Add(field);
            setValue = val => field.value = (int)val;
            getValue = () => field.value;
            fieldElement = field;
            return container;
        }

        private static VisualElement CreateBoolField(CustomProperty prop, bool current,
            out Action<object> setValue, out Func<object> getValue, out VisualElement fieldElement)
        {
            var container = new VisualElement();
            container.AddToClassList("base-bool-property");
            container.Add(new Label(prop.DisplayName));
            var field = new Toggle { value = current };
            container.Add(field);
            setValue = val => field.value = (bool)val;
            getValue = () => field.value;
            fieldElement = field;
            return container;
        }

        private static VisualElement CreateStringField(CustomProperty prop, string current,
            out Action<object> setValue, out Func<object> getValue, out VisualElement fieldElement)
        {
            var container = new VisualElement();
            container.AddToClassList("base-property");
            container.Add(new Label(prop.DisplayName));
            var field = new TextField { value = current };
            container.Add(field);
            setValue = val => field.value = (string)val;
            getValue = () => field.value;
            fieldElement = field;
            return container;
        }

        private static VisualElement CreateEnumField(CustomProperty prop, Enum current,
            out Action<object> setValue, out Func<object> getValue, out VisualElement fieldElement)
        {
            var container = new VisualElement();
            container.AddToClassList("base-property");
            container.Add(new Label(prop.DisplayName));
            var field = new EnumField(current);
            container.Add(field);
            setValue = val => field.value = (Enum)val;
            getValue = () => field.value;
            fieldElement = field;
            return container;
        }
        private static VisualElement CreateDropdownField(CustomProperty prop, string[] options, object[] values, object current,
   out Action<object> setValue, out Func<object> getValue, out VisualElement fieldElement)
        {
            var container = new VisualElement();
       
            container.AddToClassList("base-property");
            container.Add(new Label(prop.DisplayName));

            var dropdown = new DropdownField(options.ToList(), 0);
            dropdown.AddToClassList("custom-dropdown");
            var currentStr = current?.ToString() ?? "";
            var index = Array.FindIndex(values, v => v?.ToString() == currentStr);
            if (index >= 0) dropdown.index = index;

            container.Add(dropdown);

            setValue = val =>
            {
                var idx = Array.FindIndex(values, v => v?.ToString() == val?.ToString());
                if (idx >= 0) dropdown.index = idx;
            };
            getValue = () => values[dropdown.index];
            fieldElement = dropdown;
            return container;
        }
        public static VisualElement CreateColorField(
            CustomProperty property,
            Color currentColor,
            out Action<object> setValue,
            out Func<object> getValue,
            out VisualElement fieldElement)
        {
            var container = new VisualElement();
            container.AddToClassList("base-property");
            container.Add(new Label(property.DisplayName));

            var colorField = new ColorFieldElement();
            colorField.CurrentColor = currentColor;
            container.Add(colorField);

            Color capturedColor = currentColor;
            var modalWindowService = ServiceManager.Current.Get<ModalWindowServiceManager>();
            // При клике открываем окно
            colorField.OnClicked += (field) =>
            {
                var parameters = new ModalParameters();
                parameters.Set("initialColor", capturedColor);

                modalWindowService.ShowWindow<ColorPickerResult>(
                    "color-picker-window",
                    "Выбор цвета",
                    parameters,
                    (result) =>
                    {
                        if (result != null && result.IsApplied)
                        {
                            capturedColor = result.Color;
                            colorField.CurrentColor = capturedColor;
                            property.Setter(capturedColor);
                        }
                    });
            };

            setValue = val =>
            {
                if (val is Color color)
                {
                    capturedColor = color;
                    colorField.CurrentColor = color;
                }
            };

            getValue = () => capturedColor;
            fieldElement = colorField;

            return container;
        }
    }
}
