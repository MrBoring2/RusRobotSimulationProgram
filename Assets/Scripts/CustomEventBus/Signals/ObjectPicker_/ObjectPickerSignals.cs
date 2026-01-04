using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.CustomEventBus.Signals.ObjectPicker_
{
    public class PickObjectSignal
    {
        public readonly GameObject Object;

        public PickObjectSignal(GameObject @object)
        {
            Object = @object;
        }
    }
    public class UnpickObjectSignal
    {

    }
}
