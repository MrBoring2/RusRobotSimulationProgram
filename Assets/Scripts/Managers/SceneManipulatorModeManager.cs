using Assets.Scripts.CustomEventBus;
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
    public enum SceneManipulatorMode
    {
        Drag,
        Move,
        Rotation,
        JOG
    }
    public class SceneManipulatorModeManager : MonoBehaviour, IService
    {
        private EventBus _eventBus;
        public SceneManipulatorMode Mode { get; private set; }
        public void Init()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
        }
        public void SetManipulatorMode(SceneManipulatorMode mode)
        {
            Mode = mode;
            _eventBus.Invoke(new SetGyzmoManipulatorModeSignal(Mode));
        }
    }
}
