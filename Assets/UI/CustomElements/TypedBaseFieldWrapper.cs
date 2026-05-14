using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine.UIElements;

namespace Assets.UI.CustomElements
{

    public class TypedBaseFieldWrapper<T> : BaseField<object>
    {
        private BaseField<T> _inner;

        public TypedBaseFieldWrapper(BaseField<T> inner) : base(null, null)
        {
            _inner = inner;
        }

        public override object value
        {
            get => _inner.value;
            set => _inner.value = (T)value;
        }
    }
}
