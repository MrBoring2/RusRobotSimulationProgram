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

       
    }
}