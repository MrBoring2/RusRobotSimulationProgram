using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Robot;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.Providers.PropertyProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;

public class ConvPropertyProvider : BasePropertyProvider
{

    public string NameSignal = "";
    [Header("Settings")]
    [SerializeField] 
    public float Speed = 0.05f;
    public Vector3 Vector = Vector3.forward;

    //test
    public bool isAct = false;

    private void Start()
    {

    }



    //------------------------------------------------------------------------------------------------------------------------//

    public override ProviderSaveData CaptureCustomState()
    {
        return new ProviderSaveData
        {
            ProviderType = nameof(RobotPropertyProvider),
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