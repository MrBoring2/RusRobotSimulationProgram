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

public class OvenPropertyProvider : BasePropertyProvider
{
    public bool G1 { get; set; } = false;
    public bool G2 { get; set; } = false;
    public bool G3 { get; set; } = false;
    public bool G4 { get; set; } = false;
    public string NameSignal1 { get; set; } = "";
    public string NameSignal2 { get; set; } = "";
    public string NameSignal3 { get; set; } = "";
    public string NameSignal4 { get; set; } = "";
    [Header("Settings")]
    public float TimeHeating { get; set; } =  10;//секунды

    public override ProviderSaveData CaptureCustomState()
    {
        return new ProviderSaveData
        {
            StringValues =
            {
                ["NameSignal1"] = NameSignal1,
                ["NameSignal2"] = NameSignal2,
                ["NameSignal3"] = NameSignal3,
                ["NameSignal4"] = NameSignal4
            },
            FloatValues =
            {
                ["TimeHeating"] = TimeHeating
            }
            
        };
    }

    public override List<CustomProperty> GetCustomProperties()
    {
        var list = new List<CustomProperty>()
            {
            new CustomProperty(
                "NameSignal1",
                "Название сигнала1",
                typeof(string),
                () => NameSignal1,
                val => NameSignal1 = val.ToString()
                ),
            new CustomProperty(
                "NameSignal2",
                "Название сигнала2",
                typeof(string),
                () => NameSignal2,
                val => NameSignal2 = val.ToString()
                ),
            new CustomProperty(
                "NameSignal3",
                "Название сигнала3",
                typeof(string),
                () => NameSignal3,
                val => NameSignal3 = val.ToString()
                ),
            new CustomProperty(
                "NameSignal4",
                "Название сигнала4",
                typeof(string),
                () => NameSignal4,
                val => NameSignal4 = val.ToString()
                ),
            new CustomProperty(
                "TimeHeating",
                "Время нагрева",
                typeof(float),
                () => TimeHeating,
                val => TimeHeating = (float)val
                )
        };
        return list;
    }

    public override void RestoreCustomState(ProviderSaveData data)
    {
        if (data.StringValues.TryGetValue("NameSignal1", out var v1))
            NameSignal1 = v1;
        if (data.StringValues.TryGetValue("NameSignal2", out var v2))
            NameSignal2 = v2;
        if (data.StringValues.TryGetValue("NameSignal3", out var v3))
            NameSignal3 = v3;
        if (data.StringValues.TryGetValue("NameSignal4", out var v4))
            NameSignal4 = v4;
        if (data.FloatValues.TryGetValue("TimeHeating", out var v5))
        {
            TimeHeating = v5;
        }
    }
}