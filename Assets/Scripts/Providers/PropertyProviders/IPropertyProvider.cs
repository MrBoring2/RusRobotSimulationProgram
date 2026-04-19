using Assets.Scripts.Models;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


public interface IPropertyProvider
{
    string Id { get; set; }
    string Name { get; set;}
    Vector3 LocalPosition { get; set;}
    Vector3 Rotation { get; set;}
    Vector3 Scale { get; set;}
    bool DisplayName { get; set;  }
    bool DisplayPosition { get; set; }
    bool DisplayRotation { get; set; }
    bool DisplayScale { get; set; }
    bool NameReadOnly { get; set; }

    IEnumerable<CustomProperty> GetCustomProperties();
    ProviderSaveData CaptureCustomState();
    void RestoreCustomState(ProviderSaveData data);
}
