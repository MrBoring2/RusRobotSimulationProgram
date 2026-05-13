using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UIElements;

namespace Assets.UI.CustomElements
{
    public class SliderCompoundField : BaseField<object>
    {
        private Slider _slider;
        private FloatField _floatField;

        public SliderCompoundField(Slider slider, FloatField floatField) : base(null, null)
        {
            _slider = slider;
            _floatField = floatField;
        }

        public override object value
        {
            get => _slider.value;
            set
            {
                var v = (float)value;
                _slider.value = v;
                _floatField.value = v;
            }
        }
    }
}
