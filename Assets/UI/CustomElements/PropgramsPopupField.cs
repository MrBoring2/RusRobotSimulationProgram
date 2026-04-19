using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UIElements;

namespace Assets.UI.CustomElements
{
    public class ProgramsPopupField : PopupField<RobotProgramObject>
    {
        public new class UxmlFactory : UxmlFactory<ProgramsPopupField, UxmlTraits> { }

        public new class UxmlTraits : PopupField<RobotProgramObject>.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new UxmlStringAttributeDescription { name = "label" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var element = (ProgramsPopupField)ve;
                element.label = m_Label.GetValueFromBag(bag, cc);
                element.formatSelectedValueCallback = FormatItem;
                element.formatListItemCallback = FormatItem;
            }
        }
        public ProgramsPopupField() : base()
        {
            formatSelectedValueCallback = FormatItem;
            formatListItemCallback = FormatItem;
        }

        public ProgramsPopupField(string label, List<RobotProgramObject> choices, int defaultIndex = 0)
            : base(label, choices, defaultIndex, FormatItem, FormatItem)
        {
        }

        private static string FormatItem(RobotProgramObject item)
        {
            if (item?.PropertyProvider == null)
                return "Не выбрано";

            return item.PropertyProvider.Name;
        }
    }
}
