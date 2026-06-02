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
        /// Преобразование из внутренней структуры Unity в структуру компилятора.
        /// </summary>
        /// <param name="robotName">ИМЯ робота (не ID)</param>
        /// <param name="programs">Список программных объектов робота.</param>
        /// <returns>Объект RobotProgramData, готовый для использования компилятором.</returns>
        public static RobotProgramData ToCompilerData(string robotName, List<RobotProgramObject> programs)
        {
            var data = new RobotProgramData(robotName);

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

        /// <summary>
        /// Преобразует отдельную команду Unity в объект команды компилятора.
        /// </summary>
        /// <param name="command">Исходная команда Unity.</param>
        /// <returns>Объект команды компилятора (RobotMoveCommand, RobotWaitCommand, RobotEffectorCommand) или null.</returns>
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
        /// Преобразование из структуры компилятора во внутреннюю структуру Unity.
        /// </summary>
        /// <param name="sceneManager">Менеджер сцены для создания объектов.</param>
        /// <param name="robotId">РЕАЛЬНЫЙ ID робота в Unity.</param>
        /// <param name="data">Данные программы из компилятора.</param>
        public static void UpdateFromCompilerData(
            SceneObjectsManager sceneManager,
            string robotId,
            RobotProgramData data)
        {
            var existingPrograms = sceneManager.Commands.GetSubPrograms(robotId, false);

            foreach (var program in existingPrograms)
            {
                sceneManager.Remove(program.Id);
            }
            existingPrograms = sceneManager.Commands.GetSubPrograms(robotId, false);

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

                if (program.PropertyProvider != null)
                {
                    program.PropertyProvider.Name = subroutine.Name.Replace("_", " ");
                }
                program.Reference.name = subroutine.Name.Replace("_", " ");

                foreach (var cmd in subroutine.Commands)
                {
                    CreateCommand(sceneManager, cmd, program.Id);
                }
            }
        }

        /// <summary>
        /// Создает объект команды Unity на основе данных компилятора.
        /// </summary>
        /// <param name="sceneManager">Менеджер сцены.</param>
        /// <param name="command">Команда из структуры компилятора.</param>
        /// <param name="programId">ID родительской программы.</param>
        private static void CreateCommand(SceneObjectsManager sceneManager, object command, string programId)
        {
            GameObject prefab = null;
            ObjectType type = ObjectType.Unknown;

            switch (command)
            {
                case RobotMoveCommand _:
                    prefab = Resources.Load<GameObject>("Prefabs/Program/Линейная точка");
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