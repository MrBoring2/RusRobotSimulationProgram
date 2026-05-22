using Assets.Scripts.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

/// <summary>
/// Провайдер сохранения и загрузки данных сцены в бинарном формате.
/// Использует BinaryFormatter для сериализации данных в файл.
/// Реализует интерфейс ISaveLoadProvider.
/// </summary>
public class BinarySaveLoadProvider : ISaveLoadProvider
{
    /// <summary>
    /// Сохраняет данные сцены в бинарный файл.
    /// </summary>
    /// <param name="path">Путь к файлу для сохранения</param>
    /// <param name="objects">Список всех объектов сцены</param>
    /// <param name="commands">Контейнер с командами роботов</param>
    /// <param name="plcData">Данные ПЛК (программируемого логического контроллера)</param>
    public void Save(string path, List<SceneObject> objects, CommandsContainer commands, PLCData plcData)
    {
        SceneData sceneData = new SceneData();
        // Сохранение всех объектов сцены
        foreach (var obj in objects)
        {
            var marker = obj.Reference.GetComponent<SceneObjectMarker>();
            var provider = obj.Reference.GetComponent<IPropertyProvider>();
            // Создаем информационный объект для сохранения
            ObjectInfo objectInfo = new ObjectInfo(obj.Id, provider.Name, 
                                                    marker.sourcePath,
                                                    marker.type, 
                                                    provider.LocalPosition,
                                                    Quaternion.Euler(provider.Rotation), 
                                                    provider.Scale, 
                                                    obj.ParentId,
                                                    provider?.CaptureCustomState());
            // Добавляем информацию об объекте в список
            sceneData.objectsData.Add(objectInfo);
        }

        // Сохранение структуры команд роботов
        sceneData.CommandsData = new CommandsContainerData();
        // Находим всех роботов на сцене
        var robots = objects.Where(o => o.Type == ObjectType.Robot).ToList();

        foreach (var robot in robots)
        {
            var robotData = new RobotCommandsData();
            robotData.RobotId = robot.Id;

            // Получаем все программы, принадлежащие этому роботу
            var programs = commands.GetSubPrograms(robot.Id);
            foreach (var program in programs)
            {
                var programData = new ProgramData();
                programData.ProgramId = program.Id;
                programData.Position = new SerializableTransform(program.Reference.transform.localPosition);
                programData.Rotation = new SerializableQuaternion(program.Reference.transform.localRotation);

                foreach (var cmd in program.Items)
                {
                    var cmdProvider = cmd.Reference.GetComponent<IPropertyProvider>();
                    var cmdMarker = cmd.Reference.GetComponent<SceneObjectMarker>();
                    // Создаем контейнер данных для команды
                    var cmdSaveData = new CommandSaveData
                    {
                        Id = cmd.Id,
                        Name = cmdProvider.Name,
                        CommandType = cmd.Type,
                        SourcePath = cmdMarker.sourcePath,
                        Position = new SerializableTransform(cmdProvider.LocalPosition), 
                        Rotation = new SerializableQuaternion(Quaternion.Euler(cmdProvider.Rotation)),
                        ProviderData = cmdProvider.CaptureCustomState()
                    };
                    programData.Commands.Add(cmdSaveData);
                }
                robotData.Programs.Add(programData);
            }
            sceneData.CommandsData.RobotsCommands.Add(robotData);
        }

        // Сохранение данных ПЛК 
        sceneData.PLCData = plcData;

        // Сериализация в бинарный файл
        BinaryFormatter formatter = new BinaryFormatter();

        // Записываем данные в файл
        using (FileStream fs = new FileStream(path, FileMode.Create))
        {
            formatter.Serialize(fs, sceneData);
        }  
    }

    /// <summary>
    /// Загружает данные сцены из бинарного файла.
    /// </summary>
    /// <param name="path">Путь к файлу для загрузки</param>
    /// <returns>Загруженные данные сцены или null, если файл не найден</returns>
    public SceneData Load(string path)
    {
        // Проверяем существование файла
        if (!File.Exists(path)) return null;
        BinaryFormatter formatter = new BinaryFormatter();
        SceneData sceneData;
        // Читаем и десериализуем данные из файла
        using (var fs = new FileStream(path, FileMode.Open))
        {
            sceneData = (SceneData)formatter.Deserialize(fs);
        }
        return sceneData;
    }
}
