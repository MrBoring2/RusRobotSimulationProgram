using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.UndoRedoSystem;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

namespace Assets.Scripts.Managers
{
    public class UndoRedoManager : MonoBehaviour, IService
    {
        //private static UndoRedoSystem _instance;
        //private static object syncRoot = new object();
        //public static UndoRedoSystem Instance
        //{
        //    get
        //    {
        //        if (_instance == null)
        //        {
        //            lock (syncRoot)
        //            {
        //                if (_instance == null)
        //                    _instance = new UndoRedoSystem();
        //            }
        //        }
        //        return _instance;
        //    }
        //}
        public bool IsRecording { get; private set; } = true;
        private EventBus _eventBus;

        public void Init()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
        }
        //public event Action<ICommand> OnCommandExecuted;
        //public event Action<ICommand> OnCommandUndone;
        private readonly Stack<ICommand> undoStack = new();
        private readonly Stack<ICommand> redoStack = new();
        public void Execute(ICommand command)
        {
            if (redoStack.Count > 0)
            {
                //CleanupRedoStack();
                redoStack.Clear();
            }
            command.Execute();
            if (!IsRecording)
                return;


            undoStack.Push(command);
            _eventBus.Invoke(new ExecuteCommandSignal(command));
            //OnCommandExecuted?.Invoke(command);
            //redoStack.Clear();
        }

        public void Undo()
        {
            if (undoStack.Count == 0) return;

            var cmd = undoStack.Pop();
            cmd.Undo();
            redoStack.Push(cmd);
            _eventBus.Invoke(new UndoneCommandSignal(cmd));
            //OnCommandUndone?.Invoke(cmd);
        }

        public void Redo()
        {
            if (redoStack.Count == 0) return;

            var cmd = redoStack.Pop();
            cmd.Execute();
            undoStack.Push(cmd);
            _eventBus.Invoke(new ExecuteCommandSignal(cmd));
            //OnCommandExecuted?.Invoke(cmd);
        }
        private void SetRecording(bool value)
        {
            IsRecording = value;
        }
        private void CleanupRedoStack()
        {
            foreach (var cmd in redoStack)
            {
                if (cmd is IDestructiveCommand destructive)
                    destructive.FinalizeDestroy();
            }
        }
        public void BeginExternalOperation()
        {
            SetRecording(false);
            ClearHistory();
        }

        public void EndExternalOperation()
        {
            SetRecording(true);
        }
        public void ClearHistory()
        {
            undoStack.Clear();
            redoStack.Clear();
        }
    }
}
