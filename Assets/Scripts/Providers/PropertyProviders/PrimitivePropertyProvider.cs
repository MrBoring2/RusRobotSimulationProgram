using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PrimitivePropertyProvider : BasePropertyProvider
{

    public override ProviderSaveData CaptureCustomState()
    {
        return new ProviderSaveData
        {
            ProviderType = nameof(PrimitivePropertyProvider)
        };
    }

    public override IEnumerable<CustomProperty> GetCustomProperties()
    {
        return null;
    }

    public override void RestoreCustomState(ProviderSaveData data)
    {
       
    }
}
