using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.AxisModes;
using Assets.Scripts.CustomEventBus.Signals.Manipulator;
using Assets.Scripts.CustomServiceManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    public class AxisModeManager : MonoBehaviour, IService
    {
        private EventBus _eventBus;
        public AxisMode Mode { get; private set; }
        public void Init()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
        }
        public void SetAxisMode(AxisMode mode)
        {
            Mode = mode;
            _eventBus.Invoke(new SetAxisModeSignal(mode));
        }
    }
}
