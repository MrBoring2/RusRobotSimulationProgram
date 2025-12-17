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
        public UndoRedoSystem()
        {
            
        }
        public event Action<ICommand> OnCommandExecuted;
        public event Action<ICommand> OnCommandUndone;
        private readonly Stack<ICommand> undoStack = new();
        private readonly Stack<ICommand> redoStack = new();
        public void Execute(ICommand command)
        {
            command.Execute();
            undoStack.Push(command);
            OnCommandExecuted?.Invoke(command);
            redoStack.Clear();
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

        public override bool Equals(object obj)
        {
            return obj is UndoRedoSystem system;
        }
    }
}
