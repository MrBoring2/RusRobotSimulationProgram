using Assets.Scripts.Models;
using System.Collections.Generic;
using UnityEngine;

public interface ISaveLoadProvider
{
    void Save(string path, List<SceneObject> objects);
    SceneData Load(string path);
}
