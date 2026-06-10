using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.Providers.PropertyProviders;
using RobotLanguageCompiler.Robot;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.UI.CodeEditor
{
    public static class RobotDataAdapter
    {
        /// <summary>
        /// Преобразование из внутренней структуры Unity в структуру компилятора
        /// </summary>
        /// <param name="robotName">ИМЯ робота (не ID)</param>
        public static RobotProgramData ToCompilerData(string robotName, List<RobotProgramObject> programs)
        {
            var data = new RobotProgramData(robotName); // Здесь robotName будет использован как ID в компиляторе

            foreach (var program in programs)
            {
                string programName = program.PropertyProvider?.Name ?? program.Reference.name;

                var subroutine = new RobotSubroutine(programName.Replace(" ", "_"));
                subroutine.Line = 0;
                subroutine.Column = 0;

                foreach (var command in program.Items)
                {
                    var compilerCommand = ConvertCommand(command);
                    if (compilerCommand != null)
                    {
                        subroutine.Commands.Add(compilerCommand);
                    }
                }

                data.Subroutines.Add(subroutine);
            }

            return data;
        }

        private static object ConvertCommand(CommandObject command)
        {
            switch (command.Type)
            {
                case ObjectType.LinearMoveCommand:
                    var pointProvider = command.PropertyProvider as PointPropertyProvider;
                    if (pointProvider == null) return null;

                    bool isPtp = (pointProvider.PointType == POINTTYPE.PointToPoint);
                    string pointName = command.PropertyProvider?.Name ?? command.Reference.name;

                    return new RobotMoveCommand(isPtp, pointName.Replace(" ", "_"));

                case ObjectType.WaitCommand:
                    var waitProvider = command.PropertyProvider as WaitPropertyProvider;
                    if (waitProvider == null) return null;

                    return new RobotWaitCommand(waitProvider.Time);

                case ObjectType.StateEndEffectorCommand:
                    var effectorProvider = command.PropertyProvider as StateEndEffectorPropertyProvider;
                    if (effectorProvider == null) return null;

                    return new RobotEffectorCommand(effectorProvider.StatusEndEffector);

                default:
                    return null;
            }
        }

        /// <summary>
        /// Преобразование из структуры компилятора во внутреннюю структуру Unity
        /// </summary>
        /// <param name="robotId">РЕАЛЬНЫЙ ID робота в Unity</param>
        /// <returns>Словарь соответствия имени программы и её ID</returns>
        public static Dictionary<string, string> UpdateFromCompilerData(
            SceneObjectsManager sceneManager,
            string robotId,
            RobotProgramData data)
        {
            var programIdMap = new Dictionary<string, string>(); // имя программы → ID (с заменой _ на пробелы)

            // 1. СОБИРАЕМ ВСЕ СУЩЕСТВУЮЩИЕ ТОЧКИ РОБОТА (с их свойствами)
            var existingPoints = new Dictionary<string, CommandObject>(); // key: имя точки
            var existingPrograms = sceneManager.Commands.GetSubPrograms(robotId, false);

            foreach (var program in existingPrograms)
            {
                foreach (var command in program.Items)
                {
                    if (command.Type == ObjectType.LinearMoveCommand)
                    {
                        string pointName = command.PropertyProvider?.Name ?? command.Reference.name;
                        existingPoints[pointName] = command;
                    }
                }
            }

            // 2. УДАЛЯЕМ ВСЕ ПРОГРАММЫ И КОМАНДЫ (кроме точек, которые будем переиспользовать)
            for (int j = existingPrograms.Count - 1; j >= 0; j--)
            {
                var commands = new List<CommandObject>(existingPrograms[j].Items);
                for (int i = commands.Count - 1; i >= 0; i--)
                {
                    if (!existingPoints.ContainsValue(commands[i]))
                    {
                        sceneManager.Remove(commands[i].Id, true);
                    }
                }
                sceneManager.Remove(existingPrograms[j].Id, true);
            }

            // 3. СОЗДАЁМ НОВЫЕ ПРОГРАММЫ
            foreach (var subroutine in data.Subroutines)
            {
                var programPrefab = Resources.Load<GameObject>("Prefabs/Program/Программа");
                if (programPrefab == null)
                {
                    Debug.LogError("Не найден префаб программы: Prefabs/Program/Программа");
                    continue;
                }

                var program = sceneManager.CreateCommand(
                    programPrefab,
                    Vector3.zero,
                    Quaternion.identity,
                    ObjectType.Program,
                    parentId: robotId
                ) as RobotProgramObject;

                if (program == null) continue;

                // Восстанавливаем имя программы (подчёркивания → пробелы)
                string programName = subroutine.Name.Replace("_", " ");

                if (program.PropertyProvider != null)
                {
                    program.PropertyProvider.Name = programName;
                }
                program.Reference.name = programName;

                // Сохраняем связь: имя программы → её ID (оригинальное имя из компилятора с _)
                programIdMap[subroutine.Name] = program.Id;

                foreach (var cmd in subroutine.Commands)
                {
                    // Пытаемся найти существующую точку
                    if (cmd is RobotMoveCommand moveCmd && existingPoints.TryGetValue(moveCmd.PointName.Replace("_", " "), out var existingPoint))
                    {
                        // Переиспользуем существующую точку
                        var pointProvider = existingPoint.Reference.GetComponent<PointPropertyProvider>();
                        if (pointProvider != null)
                        {
                            // Обновляем тип движения (PTP или Linear)
                            pointProvider.PointType = moveCmd.IsPtp
                                ? POINTTYPE.PointToPoint
                                : POINTTYPE.LinearPoint;
                        }

                        // Перемещаем точку в новую программу
                        var moveCommand = sceneManager.CreateCommand(
                            existingPoint.Reference,
                            existingPoint.Reference.transform.position,
                            existingPoint.Reference.transform.rotation,
                            ObjectType.LinearMoveCommand,
                            parentId: program.Id
                        );

                        if (moveCommand != null)
                        {
                            string pointName = moveCmd.PointName.Replace("_", " ");
                            moveCommand.PropertyProvider.Name = pointName;
                            moveCommand.Reference.name = pointName;

                            // Копируем все параметры из существующей точки в новую команду
                            var newPointProvider = moveCommand.Reference.GetComponent<PointPropertyProvider>();
                            if (newPointProvider != null && pointProvider != null)
                            {
                                // Копируем все параметры
                                newPointProvider.PointType = pointProvider.PointType;
                                newPointProvider.LinAcceler = pointProvider.LinAcceler;
                                newPointProvider.LinBrake = pointProvider.LinBrake;
                                newPointProvider.AngleSpeed = pointProvider.AngleSpeed;
                                newPointProvider.AngleAcceler = pointProvider.AngleAcceler;
                                newPointProvider.AngleBrake = pointProvider.AngleBrake;
                                newPointProvider.SpeedPercent = pointProvider.SpeedPercent;
                                newPointProvider.ConfigPoint = pointProvider.ConfigPoint;

                                // Копируем позицию и поворот
                                moveCommand.Reference.transform.position = existingPoint.Reference.transform.position;
                                moveCommand.Reference.transform.rotation = existingPoint.Reference.transform.rotation;
                                moveCommand.Reference.transform.localScale = existingPoint.Reference.transform.localScale;
                            }
                        }
                    }
                    else
                    {
                        // Создаём новую команду
                        CreateCommand(sceneManager, cmd, program.Id);
                    }
                }
            }

            // 4. УДАЛЯЕМ НЕИСПОЛЬЗУЕМЫЕ ТОЧКИ
            foreach (var point in existingPoints.Values)
            {
                if (point.Reference.transform.parent == null)
                {
                    sceneManager.Remove(point.Id, true);
                }
            }

            return programIdMap;
        }

        private static void CreateCommand(SceneObjectsManager sceneManager, object command, string programId)
        {
            GameObject prefab = null;
            ObjectType type = ObjectType.Unknown;

            switch (command)
            {
                case RobotMoveCommand _:
                    prefab = Resources.Load<GameObject>("Prefabs/Program/Точка перемещения");
                    type = ObjectType.LinearMoveCommand;
                    break;

                case RobotWaitCommand _:
                    prefab = Resources.Load<GameObject>("Prefabs/Program/Ожидание");
                    type = ObjectType.WaitCommand;
                    break;

                case RobotEffectorCommand _:
                    prefab = Resources.Load<GameObject>("Prefabs/Program/Задать состояние захвата");
                    type = ObjectType.StateEndEffectorCommand;
                    break;

                default:
                    return;
            }

            if (prefab == null)
            {
                Debug.LogError($"Не найден префаб для команды типа {command.GetType()}");
                return;
            }

            var cmdObject = sceneManager.CreateCommand(
                prefab,
                Vector3.zero,
                Quaternion.identity,
                type,
                parentId: programId
            );

            if (cmdObject == null) return;

            switch (command)
            {
                case RobotMoveCommand moveCmd:
                    if (cmdObject.PropertyProvider != null)
                    {
                        cmdObject.PropertyProvider.Name = moveCmd.PointName.Replace("_", " ");
                    }
                    cmdObject.Reference.name = moveCmd.PointName.Replace("_", " ");

                    var pointProvider = cmdObject.Reference.GetComponent<PointPropertyProvider>();
                    if (pointProvider != null)
                    {
                        pointProvider.PointType = moveCmd.IsPtp
                            ? POINTTYPE.PointToPoint
                            : POINTTYPE.LinearPoint;
                        pointProvider.Position = new Vector3(1, 1, 1);
                        pointProvider.Rotation = new Vector3(180, 0, 0);
                    }
                    break;

                case RobotWaitCommand waitCmd:
                    var waitProvider = cmdObject.Reference.GetComponent<WaitPropertyProvider>();
                    if (waitProvider != null)
                    {
                        waitProvider.Time = waitCmd.Seconds;
                    }
                    break;

                case RobotEffectorCommand effectorCmd:
                    var effectorProvider = cmdObject.Reference.GetComponent<StateEndEffectorPropertyProvider>();
                    if (effectorProvider != null)
                    {
                        effectorProvider.StatusEndEffector = effectorCmd.IsClosed;
                    }
                    break;
            }
        }
    }
}