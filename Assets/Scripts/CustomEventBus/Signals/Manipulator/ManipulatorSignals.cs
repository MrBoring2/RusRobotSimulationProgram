using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.CustomEventBus.Signals.Manipulator
{
    public class SetGyzmoManipulatorModeSignal
    {
        public readonly SceneManipulatorMode Mode;

        public SetGyzmoManipulatorModeSignal(SceneManipulatorMode mode)
        {
            Mode = mode;
        }
    }
    public class SetJOGManipulatorModeSignal
    {

    }
}
