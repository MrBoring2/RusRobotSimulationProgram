using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.StageControlSystem.Providers.PropertyProviders
{
    public class StaticPropertyProvider : BasePropertyProvider
    {
        private void Awake()
        {
            displayScale = false;
        }
        public override ProviderSaveData CaptureCustomState()
        {
            return new ProviderSaveData
            {
                ProviderType = nameof(StaticPropertyProvider),
            };
        }

        public override List<CustomProperty> GetCustomProperties()
        {
            return null;
        }

        public override void RestoreCustomState(ProviderSaveData data)
        {
            
        }
    }
}
