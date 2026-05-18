using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Robot;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.Providers.PropertyProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;

public class SensorPropertyProvider : BasePropertyProvider
{
    public bool InvertSignal { get; set; } = false;
    public string NameSignal { get; set; } = "";
    public bool IsActive { get; set; } = false;
    [Header("Detection Settings")]
    [SerializeField] public float DetectionLength = 0.05f;
    [SerializeField] public float DetectionHeight = 0.01f;
    [Header("Visual Settings")]
    [SerializeField] public Color OnColor = Color.green;
    [SerializeField] public bool _showVisualization = true;
    [SerializeField] public Material _visualizationMaterial;

    public override ProviderSaveData CaptureCustomState()
    {
        return new ProviderSaveData
        {
            ProviderType = nameof(SensorPropertyProvider),
            BoolValues = {
                    ["EndEffectorOn"] = InvertSignal,
                    ["IsActive"] = IsActive
                },
            StringValues =
            {
                ["NameSignal"] = NameSignal,
            }
            
        };
    }

    public override List<CustomProperty> GetCustomProperties()
    {
        var list = new List<CustomProperty>()
            {   
            new CustomProperty(
                "NameSignal",
                "Название сигнала",
                typeof(string),
                () => NameSignal,
                val => NameSignal = val.ToString()
                ),
                new CustomProperty(
                "IsActive",
                "Активен",
                typeof(bool),
                () => IsActive,
                val => IsActive = (bool)val
                ),
                new CustomProperty(
                "InvertSignal",
                "Инвертировать сигнал",
                typeof(bool),
                () => InvertSignal,
                val => InvertSignal = (bool)val
                ),
        };
        return list;
    }

    public override void RestoreCustomState(ProviderSaveData data)
    {
        if (data.StringValues.TryGetValue("NameSignal", out var v1))
            NameSignal = v1;
        if (data.BoolValues.TryGetValue("IsActive", out var v2))
            IsActive = v2;
        if (data.BoolValues.TryGetValue("VerificationAngles", out var v3))
            InvertSignal = v3;
    }
}