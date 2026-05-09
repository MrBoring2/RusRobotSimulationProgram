using Assets.Scripts.Models;
using System.Collections.Generic;
using UnityEngine;

public interface ISaveLoadProvider
{
    void Save(string path, List<SceneObject> objects, CommandsContainer commands, PLCData plcData);
    SceneData Load(string path);
}
