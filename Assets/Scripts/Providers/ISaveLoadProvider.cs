using System.Collections.Generic;
using UnityEngine;

public interface ISaveLoadProvider
{
    void Save(string path, List<GameObject> objects);
    SceneData Load(string path);
}
