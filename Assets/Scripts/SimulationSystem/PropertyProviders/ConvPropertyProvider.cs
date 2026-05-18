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

public class ConvPropertyProvider : BasePropertyProvider
{

    public string NameSignal { get; set; } = "";
    [Header("Settings")]
    public float Speed { get; set; } = 0.05f;
    public Vector3 Vector = Vector3.forward;

    //test
    public bool isAct = false;

    private void Awake()
    {
        displayScale = false;
    }
    public override ProviderSaveData CaptureCustomState()
    {
        return new ProviderSaveData
        {
            ProviderType = nameof(SensorPropertyProvider),
            FloatValues = {
                    ["Speed"] = Speed,
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
                "Speed",
                "Скорость",
                typeof(float),
                () => Speed,
                val => Speed = (float)val
                )
        };
        return list;
    }

    public override void RestoreCustomState(ProviderSaveData data)
    {
        if (data.StringValues.TryGetValue("NameSignal", out var v1))
            NameSignal = v1;
        if (data.FloatValues.TryGetValue("Speed", out var v2))
            Speed = v2;
    }
}