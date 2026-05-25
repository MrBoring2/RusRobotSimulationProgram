using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.SimulationSystem.PropertyProviders
{
    public class RodsHeaterPropertyProvider : BasePropertyProvider
    {
        private void Awake()
        {
            displayScale = false;
        }
        public override ProviderSaveData CaptureCustomState()
        {
            return new ProviderSaveData
            {
                ProviderType = nameof(RodsHeaterPropertyProvider),
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
