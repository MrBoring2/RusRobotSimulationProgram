using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PrimitivePropertyProvider : BasePropertyProvider
{
    public Color Color { get; set; }
    public float Test { get; set; }
    public override ProviderSaveData CaptureCustomState()
    {
        return new ProviderSaveData
        {
            ProviderType = nameof(PrimitivePropertyProvider),
            Color = new ColorObj(Color.r, Color.g, Color.b, Color.a),
            FloatValues =
            {
                ["Test"] = Test
            }
        };
    }

    public override List<CustomProperty> GetCustomProperties()
    {
        return new List<CustomProperty>
        {
            new CustomProperty(
            "Color",
            "Цвет",
            typeof(Color),
            () => Color,
            val => Color = (Color)val),
            new CustomProperty(

            "Test",
            "test",
            typeof(float),
            () => Test,
            val => Test = (float)val)
        };
    }

    public override void RestoreCustomState(ProviderSaveData data)
    {
        Color = new Color(data.Color.R, data.Color.G, data.Color.B, data.Color.A);
        Test = 0;
    }
}
