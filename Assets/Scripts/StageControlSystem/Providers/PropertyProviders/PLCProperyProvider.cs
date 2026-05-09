using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using System.Collections.Generic;
using UnityEngine;

public class PLCProperyProvider : BasePropertyProvider
{
    private void Start()
    {
        displayScale = false;
    }
    public override ProviderSaveData CaptureCustomState()
    {
        return new ProviderSaveData
        {
            ProviderType = nameof(PLCProperyProvider)
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
