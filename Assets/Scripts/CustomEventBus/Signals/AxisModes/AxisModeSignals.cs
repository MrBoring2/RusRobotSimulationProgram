using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.CustomEventBus.Signals.AxisModes
{
    public class SetAxisModeSignal
    {
        public readonly AxisMode Mode;

        public SetAxisModeSignal(AxisMode mode)
        {
            Mode = mode;
        }
    }
}
