using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Providers.PropertyProviders
{
    public class StateEndEffectorPropertyProvider : BasePropertyProvider
    {
        public bool StatusEndEffector { get; set; } = false;
        private void Awake()
        {
            displayScale = false;
            displayPosition = false;
            displayRotation = false;
        }
        public bool Get()
        {
            return StatusEndEffector;
        }
        public override ProviderSaveData CaptureCustomState()
        {
            return new ProviderSaveData
            {
                ProviderType = nameof(StateEndEffectorPropertyProvider),
                BoolValues =
                {
                    ["StatusEndEffector"] = StatusEndEffector
                }
            };
        }

        public override IEnumerable<CustomProperty> GetCustomProperties()
        {
            yield return new CustomProperty(
                "StatusEndEffector",
                "Захват закрыт",
                typeof(bool),
                () => StatusEndEffector,
                val => StatusEndEffector = (bool)val
            );
        }

        public override void RestoreCustomState(ProviderSaveData data)
        {
            if (data.BoolValues.TryGetValue("StatusEndEffector", out var v))
                StatusEndEffector = v;
        }
    }
}
