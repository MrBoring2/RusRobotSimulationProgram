using Assets.Scripts.Models;
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
        public readonly SceneObject Object;

        public PickObjectSignal(SceneObject @object)
        {
            Object = @object;
        }
    }
    public class UnpickObjectSignal
    {

    }
    public class PickCommandSignal
    {
        public readonly SceneObject Point;
        public PickCommandSignal(SceneObject point)
        {
            Point = point;
        }
    }
}
