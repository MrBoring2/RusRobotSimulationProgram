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

public class SensorPropertyProvider : BasePropertyProvider
{
    public bool InvertSignal = false;//нормально открытый или нормально зыкратый датчик

    public string NameSignal = "";
    public bool IsActive = true;
    [Header("Detection Settings")]
    [SerializeField] public float DetectionLength = 0.05f;
    [SerializeField] public float DetectionHeight = 0.01f;
    [Header("Visual Settings")]
    [SerializeField] public Color OnColor = Color.green;
    [SerializeField] public bool _showVisualization = true;
    [SerializeField] public Material _visualizationMaterial;

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