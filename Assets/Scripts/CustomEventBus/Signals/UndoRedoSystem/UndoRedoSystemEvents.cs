using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.CustomEventBus.Signals.UndoRedoSystem
{
    public class ExecuteCommandSignal
    {
        public readonly ICommand Command;

        public ExecuteCommandSignal(ICommand command)
        {
            Command = command;
        }
    }
    public class UndoneCommandSignal
    {
        public readonly ICommand Command;

        public UndoneCommandSignal(ICommand command)
        {
            Command = command;
        }
    }
}
