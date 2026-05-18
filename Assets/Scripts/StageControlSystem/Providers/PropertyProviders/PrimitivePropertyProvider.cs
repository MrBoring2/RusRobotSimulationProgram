using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class PrimitivePropertyProvider : BasePropertyProvider
{
    private Color _color;
    public Color Color
    {
        get
        {
            if (_color == default)
            {
                var _targetRenderer = gameObject?.GetComponent<Renderer>();
                if (_targetRenderer != null)
                {
                    _color = _targetRenderer.material.color;
                }
            }
            return _color;
        }
        set
        {
            _color = value;
            var _targetRenderer = gameObject?.GetComponent<Renderer>();
            if (_targetRenderer != null)
            {
                _targetRenderer.material.color = _color;
            }
        }
    }
    public override ProviderSaveData CaptureCustomState()
    {
        return new ProviderSaveData
        {
            ProviderType = nameof(PrimitivePropertyProvider),
            Color = new ColorObj(Color.r, Color.g, Color.b, Color.a),
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
            val => Color = (Color)val)
        };
    }

    public override void RestoreCustomState(ProviderSaveData data)
    {
        Color = new Color(data.Color.R, data.Color.G, data.Color.B, data.Color.A);
    }
}
