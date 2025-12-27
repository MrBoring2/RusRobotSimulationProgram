using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.CustomEventBus.Signals.Lines
{
    public class StartLineDrawer
    {
        public string ProgramId { get; }

        public StartLineDrawer(string programId)
        {
            ProgramId = programId;
        }
    }
    public class StopLineDrawer
    {
        public StopLineDrawer()
        {
        }
    }
    public class UpdateLineDrawer
    {

    }
}
