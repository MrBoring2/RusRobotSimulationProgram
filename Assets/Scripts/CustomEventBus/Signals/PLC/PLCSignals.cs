using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.CustomEventBus.Signals.PLC
{
    public class PLCShowExpressionPanelSignal
    {
        public string Expression { get; set;  }

        public PLCShowExpressionPanelSignal(string expression = "")
        {
            Expression = expression;
        }
    }
}
