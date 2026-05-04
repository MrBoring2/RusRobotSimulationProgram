using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Providers
{
    public class RobotProgramPropertyProvider : BasePropertyProvider
    {
        private void Start()
        {
            displayPosition = false;
            displayRotation = false;
            displayScale = false;
        }                                   
        public override ProviderSaveData CaptureCustomState()
        {
            return new ProviderSaveData
            {
                ProviderType = nameof(LinearPointPropertyProvider)
            };
        }

        public override List<CustomProperty> GetCustomProperties()
        {
            return new List<CustomProperty>();
        }

        public override void RestoreCustomState(ProviderSaveData data)
        {
           
        }
    }
}
