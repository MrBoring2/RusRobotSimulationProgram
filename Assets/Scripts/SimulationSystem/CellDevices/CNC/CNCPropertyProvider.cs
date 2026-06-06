using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.StageControlSystem.UI;
using System.Collections.Generic;
using UnityEngine;

public class CNCPropertyProvider : BasePropertyProvider
{
    public bool ChuckOn = false;
    public string NameSignalChuckOn = "CNCChuckOn";
    public bool CNCStart = false;
    public string NameSignalCNCStart = "CNCStart";
    public bool CNCEndWork = false;
    public string NameSignalCNCEndWork = "CNCEndWork";
    public bool DetailInChuck = false;
    public string  NameSignalDetailInChuck = "DetailInChuck";

    public float WorkTime = 10;
    public string NameWorkTime = "WorkTime";

    public bool chuckOnContr = false;

    
    void Start()
    {
        
    }


    public override ProviderSaveData CaptureCustomState()
    {
        return new ProviderSaveData
        {
            ProviderType = nameof(RobotPropertyProvider),
        };
    }

    public override List<CustomProperty> GetCustomProperties()
    {
        return new List<CustomProperty>()
        {

        };
    }
    public override void RestoreCustomState(ProviderSaveData data)
    { 
    
    }

}
