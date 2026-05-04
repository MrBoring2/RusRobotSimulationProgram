using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UIElements;

namespace Assets.UI.CustomElements
{
    public class UniversalPopupField<T> : PopupField<T>
    {
        public new class UxmlFactory : UxmlFactory<UniversalPopupField<T>, UxmlTraits> { }

        public new class UxmlTraits : PopupField<T>.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new UxmlStringAttributeDescription { name = "label" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var element = (UniversalPopupField<T>)ve;
                element.label = m_Label.GetValueFromBag(bag, cc);
                element.formatSelectedValueCallback = FormatItem;
                element.formatListItemCallback = FormatItem;
            }
        }

        public UniversalPopupField() : base()
        {
            formatSelectedValueCallback = FormatItem;
            formatListItemCallback = FormatItem;
        }

        public UniversalPopupField(string label, List<T> choices, int defaultIndex = 0)
            : base(label, choices, defaultIndex, FormatItem, FormatItem)
        {
        }

        private static string FormatItem(T item)
        {
            if (item == null)
                return "Не выбрано";

            // Если тип - string, просто возвращаем строку
            if (typeof(T) == typeof(string))
                return item.ToString();

            // Если тип - RobotProgramObject и имеет PropertyProvider
            if (item is RobotProgramObject robotProgram && robotProgram.PropertyProvider != null)
                return robotProgram.PropertyProvider.Name;

            // Если тип имеет свойство Name (рефлексия)
            var nameProperty = typeof(T).GetProperty("Name");
            if (nameProperty != null)
                return nameProperty.GetValue(item)?.ToString() ?? item.ToString();

            // Fallback
            return item.ToString();
        }
    }

    // Сохраните обратную совместимость с старым кодом
    public class ProgramsPopupField : UniversalPopupField<RobotProgramObject>
    {
        public new class UxmlFactory : UxmlFactory<ProgramsPopupField, UxmlTraits> { }
    }

    public class StringPopupField : UniversalPopupField<string>
    {
        public new class UxmlFactory : UxmlFactory<StringPopupField, UxmlTraits> { }
    }
    public class VariablePopupField : UniversalPopupField<Variable>
    {
        public new class UxmlFactory : UxmlFactory<VariablePopupField, UxmlTraits> { }
    }
}

