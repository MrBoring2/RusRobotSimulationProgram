using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Providers.PropertyProviders
{
    public class WorkpiecePropertyProvider : BasePropertyProvider
    {
        public bool IsKinematic
        {
            get => gameObject.GetComponent<Rigidbody>().isKinematic;
            set => gameObject.GetComponent<Rigidbody>().isKinematic = value;
        }

        //private void Awake()
        //{
        //    displayScale = false;
        //    IsKinematic = true;
        //}
        void Start()
        {
            displayScale = false;
        }
        public override ProviderSaveData CaptureCustomState()
        {
            return new ProviderSaveData
            {
                ProviderType = nameof(StateEndEffectorPropertyProvider),
                BoolValues =
                {
                    ["IsKinematic"] = IsKinematic
                }
            };
        }

        public override List<CustomProperty> GetCustomProperties()
        {
            return new List<CustomProperty>{ new CustomProperty(
                "IsKinematic",
                "Фиксировать деталь",
                typeof(bool),
                () => IsKinematic,
                val => IsKinematic = (bool)val)
            };
        }

        public override void RestoreCustomState(ProviderSaveData data)
        {
            if (data.BoolValues.TryGetValue("IsKinematic", out var v))
                IsKinematic = v;
        }
    }
}
