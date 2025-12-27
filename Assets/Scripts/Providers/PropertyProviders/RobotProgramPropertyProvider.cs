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
            throw new NotImplementedException();
        }

        public override IEnumerable<CustomProperty> GetCustomProperties()
        {
            return null;
        }

        public override void RestoreCustomState(ProviderSaveData data)
        {
            throw new NotImplementedException();
        }
    }
}
