using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

namespace Assets.Scripts.Models
{
    public class CommandsContainer
    {
        private readonly Dictionary<string, List<RobotProgramObject>> _subProgramsBySource;
        private readonly Dictionary<string, SceneObject> _indexById = new Dictionary<string, SceneObject>();
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
            _indexById[subProgram.Id] = subProgram;
        }

        public IEnumerable<RobotProgramObject> GetAllPrograms()
        {
            foreach (var kvp in _subProgramsBySource)
            {
                foreach (var p in kvp.Value)
                    yield return p;
            }
        }
        public IEnumerable<CommandObject> GetAllCommands()
        {
            foreach (var kvp in _subProgramsBySource)
            {
                foreach (var p in kvp.Value)
                {
                    foreach (var c in p.Items)
                        yield return c;
                }
            }
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



        public void AddCommand(string sourceId, string subProgramId, CommandObject command)
        {
            var subProgram = GetSubProgram(sourceId, subProgramId);
            if (subProgram == null)
                throw new InvalidOperationException($"Подпрограмма {subProgramId} не найдена в устройстве {sourceId}");

            subProgram.Items.Add(command);
            _indexById[command.Id] = command;
        }


        public SceneObject FindElementById(string id, bool getOnlyActive = true)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            if (!_indexById.TryGetValue(id, out var obj))
                return null;

            if (getOnlyActive && obj.Reference != null && !obj.Reference.activeInHierarchy)
                return null;

            return obj;
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

        /// <summary>
        /// Удаляет подпрограмму (программу) робота
        /// </summary>
        public bool RemoveSubProgram(string robotId, string programId)
        {
            if (!_subProgramsBySource.TryGetValue(robotId, out var programs))
                return false;

            var program = programs.FirstOrDefault(p => p.Id == programId);
            if (program == null)
                return false;

            // Удаляем все команды из индекса
            foreach (var command in program.Items.ToList())
            {
                _indexById.Remove(command.Id);
            }

            // Очищаем список команд (без вызова RemoveCommand)
            program.Items.Clear();

            // Удаляем программу из индекса
            _indexById.Remove(programId);

            // Удаляем саму программу
            programs.Remove(program);

            if (programs.Count == 0)
            {
                _subProgramsBySource.Remove(robotId);
            }

            return true;
        }

        /// <summary>
        /// Удаляет команду из подпрограммы
        /// </summary>
        public bool RemoveCommand(string robotId, string programId, string commandId)
        {
            if (!_subProgramsBySource.TryGetValue(robotId, out var programs))
                return false;

            var program = programs.FirstOrDefault(p => p.Id == programId);
            if (program == null)
                return false;

            var command = program.Items.FirstOrDefault(c => c.Id == commandId);
            if (command == null)
                return false;
            _indexById.Remove(commandId);
            return program.Items.Remove(command);
        }

        /// <summary>
        /// Полностью очищает все данные для указанного робота
        /// </summary>
        public void ClearRobotData(string robotId)
        {
            if (_subProgramsBySource.TryGetValue(robotId, out var programs))
            {
                // Удаляем все GameObject команды и программы
                foreach (var program in programs)
                {
                    foreach (var command in program.Items)
                    {
                        if (command.Reference != null)
                            GameObject.Destroy(command.Reference);
                    }
                    if (program.Reference != null)
                        GameObject.Destroy(program.Reference);
                }

                _subProgramsBySource.Remove(robotId);
            }
        }

        /// <summary>
        /// Удаляет объект по ID (без уничтожения GameObject)
        /// </summary>
        public bool RemoveById(string id)
        {
            SceneObject removedObject = null;
            if (_indexById.Remove(id))
            {
                foreach (var kvp in _subProgramsBySource)
                {
                    var programs = kvp.Value;

                    for (int i = programs.Count - 1; i >= 0; i--)
                    {
                        if (programs[i].Id == id)
                        {
                            removedObject = programs[i];
                            programs.RemoveAt(i);
                            return true;
                        }

                        for (int j = programs[i].Items.Count - 1; j >= 0; j--)
                        {
                            if (programs[i].Items[j].Id == id)
                            {
                                removedObject = programs[i].Items[j];
                                programs[i].Items.RemoveAt(j);
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}