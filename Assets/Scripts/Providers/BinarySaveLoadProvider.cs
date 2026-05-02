using Assets.Scripts.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class BinarySaveLoadProvider : ISaveLoadProvider
{
    
    public void Save(string path, List<SceneObject> objects, CommandsContainer commands, PLCData plcData)
    {
        SceneData sceneData = new SceneData();
        foreach (var obj in objects)
        {
            var marker = obj.Reference.GetComponent<SceneObjectMarker>();
            var provider = obj.Reference.GetComponent<IPropertyProvider>();
            ObjectInfo objectInfo = new ObjectInfo(obj.Id, provider.Name, 
                                                    marker.sourcePath,
                                                    marker.type, 
                                                    provider.LocalPosition,
                                                    Quaternion.Euler(provider.Rotation), 
                                                    provider.Scale, 
                                                    obj.ParentId,
                                                    provider?.CaptureCustomState());
            sceneData.objectsData.Add(objectInfo);
        }

        sceneData.CommandsData = new CommandsContainerData();

        // Получаем всех роботов
        var robots = objects.Where(o => o.Type == ObjectType.Robot).ToList();
        foreach (var robot in robots)
        {
            var robotData = new RobotCommandsData();
            robotData.RobotId = robot.Id;

            var programs = commands.GetSubPrograms(robot.Id);
            foreach (var program in programs)
            {
                var programData = new ProgramData();
                programData.ProgramId = program.Id;

                foreach (var cmd in program.Items)
                {
                    var cmdProvider = cmd.Reference.GetComponent<IPropertyProvider>();
                    var cmdMarker = cmd.Reference.GetComponent<SceneObjectMarker>();

                    var cmdSaveData = new CommandSaveData
                    {
                        Id = cmd.Id,
                        Name = cmdProvider.Name,
                        CommandType = cmd.Type,
                        SourcePath = cmdMarker.sourcePath,
                        ProviderData = cmdProvider.CaptureCustomState()
                    };
                    programData.Commands.Add(cmdSaveData);
                }
                robotData.Programs.Add(programData);
            }
            sceneData.CommandsData.RobotsCommands.Add(robotData);
        }

        sceneData.PLCData = plcData;

        BinaryFormatter formatter = new BinaryFormatter();

        using (FileStream fs = new FileStream(path, FileMode.Create))
        {
            formatter.Serialize(fs, sceneData);
        }
        
    }
    
    public SceneData Load(string path)
    {
        if (!File.Exists(path)) return null;
        BinaryFormatter formatter = new BinaryFormatter();
        SceneData sceneData;
        using (var fs = new FileStream(path, FileMode.Open))
        {
            sceneData = (SceneData)formatter.Deserialize(fs);
        }
        return sceneData;
    }
}
