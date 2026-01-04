using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.CustomEventBus.Signals.Camera
{
    public class RotateCameraSingal
    {
        public readonly Vector3 Rotation;

        public RotateCameraSingal(Vector3 rotation)
        {
            Rotation = rotation;
        }
    }
    public class ToggleOrthographicSignal
    {

    }
}
