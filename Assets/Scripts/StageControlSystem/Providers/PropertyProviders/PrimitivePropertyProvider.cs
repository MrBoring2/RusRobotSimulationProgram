using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using System.Collections.Generic;
using UnityEngine;

public class PrimitivePropertyProvider : BasePropertyProvider
{
    public Color Color { get; set; }
    public override ProviderSaveData CaptureCustomState()
    {
        return new ProviderSaveData
        {
            ProviderType = nameof(PrimitivePropertyProvider),
            Color = new ColorObj(Color.r, Color.g, Color.b, Color.a)
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
            val => Color = (Color)val
        )
        };
    }

    public override void RestoreCustomState(ProviderSaveData data)
    {
        Color = new Color(data.Color.R, data.Color.G, data.Color.B, data.Color.A);
    }
}
