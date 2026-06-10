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
    public string NameSignalChuckOn { get; set; } = "CNCChuckOn";
    public bool CNCStart = false;
    public string NameSignalCNCStart { get; set; } = "CNCStart";
    public bool CNCEndWork = false;
    public string NameSignalCNCEndWork { get; set; } = "CNCEndWork";
    public bool DetailInChuck = false;
    public string  NameSignalDetailInChuck { get; set; } = "DetailInChuck";

    public float WorkTime { get; set; } = 10;

    public bool chuckOnContr = false;

    
    void Start()
    {
        
    }


    public override ProviderSaveData CaptureCustomState()
    {
        return new ProviderSaveData
        {
            ProviderType = nameof(CNCPropertyProvider),
            FloatValues = {
                    ["WorkTime"] = WorkTime,
            },
            StringValues =
            {
                ["NameSignalChuckOn"] = NameSignalChuckOn,
                ["NameSignalCNCStart"] = NameSignalCNCStart,
                ["NameSignalCNCEndWork"] = NameSignalCNCEndWork,
                ["NameSignalDetailInChuck"] = NameSignalDetailInChuck
            }

        };
    }

    public override List<CustomProperty> GetCustomProperties()
    {
        return new List<CustomProperty>()
        {
             new CustomProperty("NameSignalChuckOn",
                "Кулачки",
                typeof(string),
                () => NameSignalChuckOn,
                val => NameSignalChuckOn = (string)val),
             new CustomProperty("NameSignalCNCStart",
                "Старт ЧПУ",
                typeof(string),
                () => NameSignalCNCStart,
                val => NameSignalCNCStart = (string)val),
             new CustomProperty("NameSignalCNCEndWork",
                "ЧПУ закончил работу",
                typeof(string),
                () => NameSignalCNCEndWork,
                val => NameSignalCNCEndWork = (string)val),
                new CustomProperty("NameSignalDetailInChuck",
                "Деталь в ЧПУ",
                typeof(string),
                () => NameSignalDetailInChuck,
                val => NameSignalDetailInChuck = (string)val),
                new CustomProperty("WorkTime",
                "Время цикла ЧПУ",
                typeof(float),
                () => WorkTime,
                val => WorkTime = (float)val),
                new CustomProperty("chuckOnContr",
                "Сжать кулачки",
                typeof(bool),
                () => chuckOnContr,
                val => chuckOnContr = (bool)val)
        };
    }
    public override void RestoreCustomState(ProviderSaveData data)
    {
        if (data.StringValues.TryGetValue("NameSignalChuckOn", out var v1))
        {
            NameSignalChuckOn = v1;
        }
        if (data.StringValues.TryGetValue("NameSignalCNCStart", out var v2))
        {
            NameSignalCNCStart = v2;
        }
        if (data.StringValues.TryGetValue("NameSignalCNCEndWork", out var v3))
        {
            NameSignalCNCEndWork = v3;
        }
        if (data.StringValues.TryGetValue("NameSignalDetailInChuck", out var v4))
        {
            NameSignalDetailInChuck = v4;
        }
        if (data.FloatValues.TryGetValue("WorkTime", out var v5))
        {
            WorkTime = v5;
        }

    }

}
