using Assets.Scripts.Models;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.Providers
{
    public class LinearPointPropertyProvider : BasePropertyProvider
    {
        //=================== ПАРАМЕТРЫ ===================

        public POINTTYPE PointType { get; set; } = POINTTYPE.LinearPoint;
        //Линейная точка
        public float LinearSpeed { get; set; } = 0.5f; ///_м/с
        public float LinAcceler { get; set; } = 1;
        public float LinBrake { get; set; } = 1;
        public float AngleSpeed { get; set; } = 90; ///_град/с
        public float AngleAcceler { get; set; } = 10;
        public float AngleBrake { get; set; } = 10;
        //точка-точка точка
        public float SpeedPercent { get; set; } = 50;
        public int ConfigPoint { get; set; } = 0;

        //===========================================//
        public bool ShowVisual = true;
        public Material material;
        public float PointSize = 0.3f;
        //===========================================//
        
        public override ProviderSaveData CaptureCustomState()
        {
            return new ProviderSaveData
            {
                ProviderType = nameof(LinearPointPropertyProvider),
                FloatValues =
                {
                    ["Speed"] = LinearSpeed,
                    ["AngleSpeed"] = AngleSpeed
                }
            };
        }

        public override List<CustomProperty> GetCustomProperties()
        {
            return PointType switch
            {
                POINTTYPE.LinearPoint => new List<CustomProperty>()
                {

                    new CustomProperty("PointType",
                    "ТИП ТОЧКИ",
                    typeof(string),
                    () => PointType.ToString(),
                    val => PointType = (POINTTYPE)Enum.Parse(typeof(POINTTYPE), (string)val)),
                    new CustomProperty(
                    "ConfigPoint",
                    "Конфигурация",
                    typeof(string),
                    () => (ConfigPoint+1).ToString(),
                    val => ConfigPoint = int.Parse((string)val)-1
                    ),
                    new CustomProperty("LinearSpeed",
                    "Л Скорость",
                    typeof(float),
                    () => LinearSpeed,
                    val => LinearSpeed = (float)val),
                    new CustomProperty("LinAcceler",
                    " Л ускорение разгона",
                    typeof(float),
                    () => LinAcceler,
                    val => LinAcceler = (float)val),
                    new CustomProperty("LinBrake",
                    "Л ускорение торможения",
                    typeof(float),
                    () => LinBrake,
                    val => LinBrake = (float)val),
                     new CustomProperty("AngleSpeed",
                    "У Скорость",
                    typeof(float),
                    () => AngleSpeed,
                    val => AngleSpeed = (float)val),
                     new CustomProperty("AngleAcceler",
                    "У ускорение разгона",
                    typeof(float),
                    () => AngleAcceler,
                    val => AngleAcceler = (float)val),
                    new CustomProperty("AngleBrake",
                    "У ускорение торможения",
                    typeof(float),
                    () => AngleBrake,
                    val => AngleBrake = (float)val)
                },
                POINTTYPE.PointToPoint => new List<CustomProperty>()
                    {
                        new CustomProperty("PointType",
                        "ТИП ТОЧКИ",
                        typeof(string),
                        () => PointType.ToString(),
                        val => PointType = (POINTTYPE)Enum.Parse(typeof(POINTTYPE), (string)val)),
                        new CustomProperty("SpeedPercent",
                        "Скорость %",
                        typeof(float),
                        () => SpeedPercent,
                        val =>
                        {
                            if((float)val < 0 || (float)val > 100) Notification.ShowError("% скорости может >=0 и <=100");
                            else
                                SpeedPercent = (float)val;
                        })
            },
                _ => new List<CustomProperty>(),
            };
        }

        public override void RestoreCustomState(ProviderSaveData data)
        {
            if (data.FloatValues.TryGetValue("Speed", out var v1))
                LinearSpeed = v1;
            if (data.FloatValues.TryGetValue("AngleSpeed", out var v2))
                AngleSpeed = v2;
            if (data.FloatValues.TryGetValue("LinAcceler", out var v3 ))
                LinAcceler = v3;
            if (data.FloatValues.TryGetValue("LinBrake", out var v4))
                LinBrake = v4;
            if (data.FloatValues.TryGetValue("AngleAcceler", out var v5))
                LinAcceler = v5;
            if (data.FloatValues.TryGetValue("AngleBrake", out var v6))
                LinBrake = v6;
            /*if (data.FloatValues.TryGetValue("AngleBrake", out var v7))
                ConfigPoint = v7;*/
        }
        
    }
}
public enum POINTTYPE
{
    LinearPoint,
    PointToPoint
}
