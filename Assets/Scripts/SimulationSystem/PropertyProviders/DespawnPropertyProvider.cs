using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.Providers.PropertyProviders;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.SimulationSystem.PropertyProviders
{
    public class DespawnPropertyProvider : BasePropertyProvider
    {
        [Header("Detection Settings")]
        public float DetectionX { get; set; } = 0.14f;
        public float DetectionY { get; set; } = 0.1f;
        public float DetectionZ { get; set; } = 0.5f;
        public bool IsActive { get; set; } = false;
        private void Awake()
        {
            displayScale = false;
        }
        public override ProviderSaveData CaptureCustomState()
        {
            return new ProviderSaveData
            {
                ProviderType = nameof(DespawnPropertyProvider),
                BoolValues =
                {
                    ["IsActive"] = IsActive
                },
                FloatValues =
                {
                   ["DetectionX"] = DetectionX,
                   ["DetectionY"] = DetectionY,
                   ["DetectionZ"] = DetectionZ
                }
            };
        }

        public override List<CustomProperty> GetCustomProperties()
        {
            return new List<CustomProperty>{
                new CustomProperty(
                "IsActive",
                "Включить удаление",
                typeof(bool),
                () => IsActive,
                val => IsActive = (bool)val),
                new CustomProperty(
                "DetectionX",
                "Длина по X",
                typeof(float),
                () => DetectionX,
                val => DetectionX = (float)val),
                new CustomProperty(
                "DetectionY",
                "Длина по Y",
                typeof(float),
                () => DetectionY,
                val => DetectionY = (float)val),
                new CustomProperty(
                "DetectionZ",
                "Длина по Z",
                typeof(float),
                () => DetectionZ,
                val => DetectionZ = (float)val),
            };
        }


        public override void RestoreCustomState(ProviderSaveData data)
        {
            if (data.BoolValues.TryGetValue("IsActive", out var v))
                IsActive = v;
            if (data.FloatValues.TryGetValue("DetectionX", out var v2))
                DetectionX = v2;
            if (data.FloatValues.TryGetValue("DetectionY", out var v3))
                DetectionY = v3;
            if (data.FloatValues.TryGetValue("DetectionZ", out var v4))
                DetectionZ = v4;

        }
    }
}
