//using Assets.Scripts.Models;
//using Assets.Scripts.Providers;
//using System.Collections.Generic;
//using UnityEngine;

//public class JOGPropertyProvider : BasePropertyProvider
//{
//    string Id { get; set; }
//    string Name { get; set; }
//    Vector3 Position { get; set; }
//    Vector3 Rotation { get; set; }
//    Vector3 Scale { get; set; }
//    bool DisplayName { get; set; }
//    bool DisplayPosition { get; set; }
//    bool DisplayRotation { get; set; }
//    bool DisplayScale { get; set; }

//    IEnumerable<CustomProperty> GetCustomProperties()
//    {
//        yield return new CustomProperty(
//               "Speed",
//               "Скорость",
//               typeof(float),
//               () => Speed,
//               val => Speed = (float)val
//           );
//    }
//    ProviderSaveData CaptureCustomState()
//    {

//    }
//    void RestoreCustomState(ProviderSaveData data)
//    {

//    }
//}
