using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.CustomEventBus.Signals.Robot
{
    //Команды
    public class AddProgram{ }
    public class AddCommand { }


    //Симуляция
    public class StartProgramm { }
    public class PauseProgramm { }
    public class StopProgramm { }
    

    public class RobotEndMove
    {
        public String RoboID;
    }

}
