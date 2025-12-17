using Assets.Scripts.Models;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public interface IPropertyProvider
{
    string Name { get; set;}
    Vector3 Position { get; set;}
    Vector3 Rotation { get; set;}
    Vector3 Scale { get; set;}

    IEnumerable<CustomProperty> GetCustomProperties();
}
