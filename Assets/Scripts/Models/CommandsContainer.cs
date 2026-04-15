using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Models
{
    public class CommandsContainer
    {
        private readonly Dictionary<string, List<RobotProgramObject>> _subProgramsBySource;

        public CommandsContainer()
        {
            _subProgramsBySource = new Dictionary<string, List<RobotProgramObject>>();
        }

        // Добавление подпрограммы
        public void AddSubProgram(string robotId, RobotProgramObject subProgram)
        {
            if (subProgram == null)
                throw new ArgumentNullException(nameof(subProgram));

            if (string.IsNullOrEmpty(robotId))
                throw new ArgumentException("SourceId не может быть пустым");

            if (!_subProgramsBySource.TryGetValue(robotId, out var subPrograms))
            {
                subPrograms = new List<RobotProgramObject>();
                _subProgramsBySource[robotId] = subPrograms;
            }

            subPrograms.Add(subProgram);
        }

        // Вставка подпрограммы на конкретную позицию
        public void InsertSubProgram(string sourceId, int index, RobotProgramObject subProgram)
        {
            if (subProgram == null)
                throw new ArgumentNullException(nameof(subProgram));

            if (!_subProgramsBySource.TryGetValue(sourceId, out var subPrograms))
            {
                subPrograms = new List<RobotProgramObject>();
                _subProgramsBySource[sourceId] = subPrograms;
            }

            if (index < 0 || index > subPrograms.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            subPrograms.Insert(index, subProgram);
        }

        // Получение всех подпрограмм для устройства в порядке добавления
        public List<RobotProgramObject> GetSubPrograms(string sourceId, bool getOnlyActive = true)
        {
            if (_subProgramsBySource.TryGetValue(sourceId, out var subPrograms))
            {
                if (getOnlyActive)
                {
                    return subPrograms.Where(p => p.Reference.activeInHierarchy).ToList();
                }
                return subPrograms;
            }

            return new List<RobotProgramObject>();
        }
        public List<CommandObject> GetCommandsFromSubProgram(string robotId, string programId, bool getOnlyActive = true)
        {
            if (_subProgramsBySource.TryGetValue(robotId, out var subPrograms))
            {
                var program = getOnlyActive
                    ? subPrograms.FirstOrDefault(p => p.Id == programId && p.Reference.activeInHierarchy)
                    : subPrograms.FirstOrDefault(p => p.Id == programId);

                if (program != null)
                {
                    return getOnlyActive
                        ? program.Items.Where(cmd => cmd.Reference.activeInHierarchy).ToList()
                        : program.Items;
                }
            }

            return null;
        }

         public IEnumerable<CommandObject> GetAllCommands(string sourceId, bool getOnlyActive = true)
         {
             var subPrograms = GetSubPrograms(sourceId, getOnlyActive);
             foreach (var subProgram in subPrograms)
             {
                 List<CommandObject> commands;
                 if (getOnlyActive)
                 {
                     commands = subProgram.Items.Where(p => p.Reference.activeInHierarchy).ToList();
                 }
                 else commands = subProgram.Items;
                 foreach (var command in commands)
                 {
                     yield return command;
                 }
             }
         }
        // Перемещение подпрограммы внутри списка устройства
        public void MoveSubProgram(string sourceId, string subProgramId, int newIndex)
        {
            if (!_subProgramsBySource.TryGetValue(sourceId, out var subPrograms))
                throw new InvalidOperationException($"SourceId {sourceId} не найден");

            var subProgram = subPrograms.FirstOrDefault(sp => sp.Id == subProgramId);
            if (subProgram == null)
                throw new InvalidOperationException($"Подпрограмма {subProgramId} не найдена");

            int oldIndex = subPrograms.IndexOf(subProgram);
            if (oldIndex == newIndex)
                return;

            subPrograms.RemoveAt(oldIndex);

            if (newIndex > subPrograms.Count)
                newIndex = subPrograms.Count;

            subPrograms.Insert(newIndex, subProgram);
        }

        public void AddCommand(string sourceId, string subProgramId, CommandObject command)
        {
            var subProgram = GetSubProgram(sourceId, subProgramId);
            if (subProgram == null)
                throw new InvalidOperationException($"Подпрограмма {subProgramId} не найдена в устройстве {sourceId}");

            subProgram.Items.Add(command);
        }

        // Вставка команды в подпрограмму на конкретную позицию
        public void InsertCommand(string sourceId, string subProgramId, int index, CommandObject command)
        {
            var subProgram = GetSubProgram(sourceId, subProgramId);
            if (subProgram == null)
                throw new InvalidOperationException($"Подпрограмма {subProgramId} не найдена в устройстве {sourceId}");

            if (index < 0 || index > subProgram.Items.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            subProgram.Items.Insert(index, command);
        }

        public SceneObject FindElementById(string id, bool getOnlyActive = true)
        {
            foreach (var kvp in _subProgramsBySource)
            {
                foreach (var subProgram in kvp.Value)
                {
                    if (subProgram.Id == id)
                    {
                        if (!getOnlyActive || (getOnlyActive && subProgram.Reference.activeInHierarchy))
                            return subProgram;
                    }

                    foreach (var command in subProgram.Items)
                    {
                        if (command.Id == id)
                        {
                            if (!getOnlyActive || (getOnlyActive && command.Reference.activeInHierarchy))
                                return command;
                        }
                    }
                }
            }
            return null;
        }

        // Перемещение команды внутри подпрограммы
        public void MoveCommand(string sourceId, string subProgramId, string commandId, int newIndex)
        {
            var subProgram = GetSubProgram(sourceId, subProgramId);
            if (subProgram == null)
                throw new InvalidOperationException($"Подпрограмма {subProgramId} не найдена в устройстве {sourceId}");

            var command = subProgram.Items.FirstOrDefault(c => c.Id == commandId);
            if (command == null)
                throw new InvalidOperationException($"Команда {commandId} не найдена");

            int oldIndex = subProgram.Items.IndexOf(command);
            if (oldIndex == newIndex)
                return;

            subProgram.Items.RemoveAt(oldIndex);

            if (newIndex > subProgram.Items.Count)
                newIndex = subProgram.Items.Count;

            subProgram.Items.Insert(newIndex, command);
        }
        // Получение подпрограммы по ID
        public RobotProgramObject GetSubProgram(string sourceId, string subProgramId, bool getOnlyActive = true)
        {
            var subPrograms = GetSubPrograms(sourceId);

            if (getOnlyActive)
            {
                return subPrograms.FirstOrDefault(sp => sp.Id == subProgramId && sp.Reference.activeInHierarchy);
            }
            else
            {
                return subPrograms.FirstOrDefault(sp => sp.Id == subProgramId);
            }
        }

        public RobotProgramObject GetSubProgram(string subProgramId, bool getOnlyActive = true)
        {
            foreach (var kvp in _subProgramsBySource)
            {
                foreach (var subProgram in kvp.Value)
                {
                    if (subProgram.Id == subProgramId)
                    {
                        if (!getOnlyActive || (getOnlyActive && subProgram.Reference.activeInHierarchy))
                            return subProgram;
                    }
                }
            }
            return null;
        }

        // Перемещение команды из одной подпрограммы в другую
        public void MoveCommandToAnotherSubProgram(string sourceId, string fromSubProgramId, string toSubProgramId, string commandId, int newIndex)
        {
            var fromSubProgram = GetSubProgram(sourceId, fromSubProgramId);
            if (fromSubProgram == null)
                throw new InvalidOperationException($"Подпрограмма {fromSubProgramId} не найдена");

            var toSubProgram = GetSubProgram(sourceId, toSubProgramId);
            if (toSubProgram == null)
                throw new InvalidOperationException($"Подпрограмма {toSubProgramId} не найдена");

            var command = fromSubProgram.Items.FirstOrDefault(c => c.Id == commandId);
            if (command == null)
                throw new InvalidOperationException($"Команда {commandId} не найдена");

            fromSubProgram.Items.Remove(command);

            if (newIndex < 0 || newIndex > toSubProgram.Items.Count)
                toSubProgram.Items.Add(command);
            else
                toSubProgram.Items.Insert(newIndex, command);
        }
    }
}