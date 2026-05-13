using Assets.Scripts.Models;
using Assets.UI.CustomElements;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.StageControlSystem.Utils
{
    public static class PropertyFieldFactory
    {

        public delegate (VisualElement container, BaseField<object> field) FieldCreator(
            CustomProperty property,
            object currentValue);

        private static Dictionary<Type, FieldCreator> _creators = new()
        {

            [typeof(float)] = (prop, val) =>
            {
                var container = new VisualElement();
                container.AddToClassList("base-property");
                container.Add(new Label(prop.DisplayName));

                var field = new FloatField();
                field.value = (float)val;
                container.Add(field);
                return (container, ToBaseField(field));
            },


            [typeof(int)] = (prop, val) =>
            {
                var container = new VisualElement();
                container.AddToClassList("base-property");
                container.Add(new Label(prop.DisplayName));

                var field = new IntegerField();
                field.value = (int)val;
                container.Add(field);
                return (container, ToBaseField(field));
            },

        
            [typeof(bool)] = (prop, val) =>
            {
                var container = new VisualElement();
                container.AddToClassList("base-bool-property");
                container.Add(new Label(prop.DisplayName));

                var field = new Toggle();
                field.value = (bool)val;
                container.Add(field);
                return (container, ToBaseField(field));
            },

     
            [typeof(string)] = (prop, val) =>
            {
                var container = new VisualElement();
                container.AddToClassList("base-property");
                container.Add(new Label(prop.DisplayName));

                var field = new TextField();
                field.value = (string)val;
                container.Add(field);
                return (container, ToBaseField(field));
            },

        
            //[typeof(Vector3)] = (prop, val) =>
            //{
            //    var v = (Vector3)val;
            //    var container = new VisualElement();
            //    container.AddToClassList("base-property");
            //    container.Add(new Label(prop.DisplayName));

            //    var row = new VisualElement();
            //    row.style.flexDirection = FlexDirection.Row;

            //    var xField = new FloatField { value = v.x };
            //    var yField = new FloatField { value = v.y };
            //    var zField = new FloatField { value = v.z };

            //    row.Add(xField);
            //    row.Add(yField);
            //    row.Add(zField);
            //    container.Add(row);

            //    // Создаём составное поле
            //    var compoundField = new Vector3CompoundField(xField, yField, zField);
            //    return (container, compoundField);
            //},

          
            [typeof(Enum)] = (prop, val) =>
            {
                var container = new VisualElement();
                container.AddToClassList("base-property");
                container.Add(new Label(prop.DisplayName));

                var field = new EnumField((Enum)val);
                container.Add(field);
                return (container, ToBaseField(field));
            },

           
            //[typeof(Color)] = (prop, val) =>
            //{
            //    var container = new VisualElement();
            //    container.AddToClassList("base-property");
            //    container.Add(new Label(prop.DisplayName));

            //    // Используй свой ColorField или стандартный
            //    var field = new UnityEditor.UIElements.ColorField();
            //    field.value = (Color)val;
            //    container.Add(field);
            //    return (container, ToBaseField(field));
            //}
        };

        // Регистрация новых типов извне
        public static void RegisterCreator(Type type, FieldCreator creator)
        {
            _creators[type] = creator;
        }

        // Создание элемента по типу свойства
        public static (VisualElement container, BaseField<object> field) CreateField(CustomProperty property)
        {
            var type = property.PropertyType;
            var currentValue = property.Getter();
            Debug.Log($"[FACTORY] Property: {property.Name}, Type: {type}, Value: {currentValue}");
            if (type == typeof(float) && property.TryGetAttribute<RangeAttribute>(out var rangeAttr))
            {
                Debug.Log($"[FACTORY] RangeAttribute найден! min={rangeAttr.min}, max={rangeAttr.max}");
                return CreateSliderField(property, (float)currentValue, rangeAttr.min, rangeAttr.max);
            }
            // Прямой поиск
            

            // Enum — проверяем базовый тип
            if (type.IsEnum && _creators.TryGetValue(typeof(Enum), out var enumCreator))
                return enumCreator(property, currentValue);

            // Слайдер для float с атрибутом Range
            if (_creators.TryGetValue(type, out var creator))
                return creator(property, currentValue);

            return _creators[typeof(string)](property, currentValue?.ToString() ?? "");
        }

        // Слайдер + числовое поле
        private static (VisualElement, BaseField<object>) CreateSliderField(
            CustomProperty property, float current, float min, float max)
        {
            var container = new VisualElement();
            container.AddToClassList("base-property");
            container.Add(new Label(property.DisplayName));

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;

            var slider = new Slider(min, max);
            slider.value = current;
            slider.style.flexGrow = 1;

            var floatField = new FloatField();
            floatField.value = current;
            floatField.style.width = 60;

            // Синхронизация
            slider.RegisterValueChangedCallback(evt => floatField.SetValueWithoutNotify(evt.newValue));
            floatField.RegisterValueChangedCallback(evt => slider.SetValueWithoutNotify(evt.newValue));

            row.Add(slider);
            row.Add(floatField);
            container.Add(row);

            return (container, new SliderCompoundField(slider, floatField));
        }

        // Вспомогательные методы
        private static BaseField<object> ToBaseField<T>(BaseField<T> field)
        {
            return new TypedBaseFieldWrapper<T>(field);
        }
    }
}
