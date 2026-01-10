using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class WaitPropertyProvider : BasePropertyProvider
{
    public float Time { get; set; } = 0;
    private void Awake()
    {
        displayScale = false;
        displayPosition = false;
        displayRotation = false;
    }
    public float Get()
    {
        return Time;
    }
    public override ProviderSaveData CaptureCustomState()
    {
        return new ProviderSaveData
        {
            ProviderType = nameof(WaitPropertyProvider),
            FloatValues =
            {
                ["Time"] = Time
            }
        };
    }

    public override IEnumerable<CustomProperty> GetCustomProperties()
    {
        yield return new CustomProperty(
            "Time",
            "Время",
            typeof(float),
            () => Time,
            val => Time = (float)val
        );
    }

    public override void RestoreCustomState(ProviderSaveData data)
    {
        if (data.FloatValues.TryGetValue("Time", out var v))
            Time = v;
    }


}
