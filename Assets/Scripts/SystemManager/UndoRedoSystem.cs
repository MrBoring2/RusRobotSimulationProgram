using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.SystemManager
{
    public class UndoRedoSystem
    {
        private static UndoRedoSystem _instance;
        private static object syncRoot = new object();
        public static UndoRedoSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncRoot)
                    {
                        if (_instance == null)
                            _instance = new UndoRedoSystem();
                    }
                }
                return _instance;
            }
        }
        public bool IsRecording { get; private set; } = true;

      
        public UndoRedoSystem()
        {
            
        }
        public event Action<ICommand> OnCommandExecuted;
        public event Action<ICommand> OnCommandUndone;
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
            OnCommandExecuted?.Invoke(command);
            //redoStack.Clear();
        }

        public void Undo()
        {
            if (undoStack.Count == 0) return;

            var cmd = undoStack.Pop();
            cmd.Undo();
            redoStack.Push(cmd);
            OnCommandUndone?.Invoke(cmd);
        }

        public void Redo()
        {
            if (redoStack.Count == 0) return;

            var cmd = redoStack.Pop();
            cmd.Execute();
            undoStack.Push(cmd);
            OnCommandExecuted?.Invoke(cmd);
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
