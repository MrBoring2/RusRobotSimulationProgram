using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.CustomEventBus.Signals.Manipulator
{
    public class SetGyzmoManipulatorModeSignal
    {
        public readonly IManipulatorMode Mode;

        public SetGyzmoManipulatorModeSignal(IManipulatorMode mode)
        {
            Mode = mode;
        }
    }
    public class SetJOGManipulatorModeSignal
    {

    }
}
