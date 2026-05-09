using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class WaitPropertyProvider : BasePropertyProvider
{
    public float Time { get; set; }
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
        Debug.Log($"[GetCustomProperties] instance {GetInstanceID()} Time = {Time}");

        yield return new CustomProperty(
            "Time",
            "Время",
            typeof(float),
            () =>
            {
                Debug.Log($"[Getter] instance {GetInstanceID()} Time = {Time}");
                return Time;
            },
            val => Time = (float)val
        );
    }

    public override void RestoreCustomState(ProviderSaveData data)
    {
        Debug.Log($"[Restore] instance {GetInstanceID()} BEFORE = {Time}");

        if (data.FloatValues.TryGetValue("Time", out var v))
            Time = v;

        Debug.Log($"[Restore] instance {GetInstanceID()} AFTER = {Time}");
    }


}
