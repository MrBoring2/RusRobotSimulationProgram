using Assets.Scripts.Models;
using Assets.Scripts.StageControlSystem.Utils;
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
    public class SpawnPropertyProvider : BasePropertyProvider
    {
        //=================== ПАРАМЕТРЫ ===================
        public string DetailName { get; set; } = "Деталь 1";
        public float SpawnInterval { get; set; } = 5f; // Время между спавнами
        public bool SpawnClick { get; set; } = false; // Спавнить по клику
        public bool SpawnOn { get; set; } = false; // Спавнить вкпХлючено/выключено
        public Vector3 RotateSpawnDetail = Vector3.zero;
        //===========================================//
        protected override void Start()
        {
            base.Start();
            GameObject[] prefabs = Resources.LoadAll<GameObject>($"Prefabs/Workpieces");
            foreach (GameObject pref in prefabs)
            {
                if(pref.name != "Генератор деталей")
                    detailsList.Add(new Deatil { itemPrefab = pref, Name = pref.name }); 
            }
        }
        public List<Deatil> detailsList { get; private set; } = new List<Deatil>();

        public override ProviderSaveData CaptureCustomState()
        {
            return new ProviderSaveData
            {
                ProviderType = nameof(SpawnPropertyProvider),
                FloatValues =
                {
                    ["SpawnInterval"] = SpawnInterval
                },
                BoolValues =
                {
                    ["SpawnClick"] = SpawnClick,
                    ["SpawnOn"] = SpawnOn
                },
                StringValues =
                {
                    ["DetailName"] = DetailName
                }
            };
        }

        public override List<CustomProperty> GetCustomProperties()
        {
            return new List<CustomProperty>() {
                /*new CustomProperty("DetailName",
                       "Деталь",
                       typeof(string),
                       () => DetailName,
                       val => val.ToString())
                       .WithAttribute(new DropdownOptionsAttribute(
                           detailsList.Select(d => d.Name).ToArray(),
                           detailsList.Select(d => d.Name).ToArray(),
                           "Name")),*/
                new CustomProperty("DetailName",
                    "Деталь",
                    typeof(string),
                    () => DetailName,
                    val => DetailName = val.ToString()),
                new CustomProperty("SpawnInterval",
                    "Интервал появления",
                    typeof(float),
                    () => SpawnInterval,
                    val => SpawnInterval = (float)val),
                new CustomProperty("SpawnClick",
                    "Появление по клику",
                    typeof(bool),
                    () => SpawnClick,
                    val => SpawnClick = (bool)val),
                new CustomProperty("SpawnOn",
                    "Непрерывное появление",
                    typeof(bool),
                    () => SpawnOn,
                    val => SpawnOn = (bool)val),
            };
        }

        public override void RestoreCustomState(ProviderSaveData data)
        {
            if (data.FloatValues.TryGetValue("SpawnInterval", out var v1))
                SpawnInterval = v1;
            if (data.BoolValues.TryGetValue("SpawnClick", out var v2))
                SpawnClick = v2;
            if (data.BoolValues.TryGetValue("SpawnOn", out var v3))
                SpawnOn = v3;
            if (data.StringValues.TryGetValue(key: "DetailName", out var v4))
                DetailName = v4;
        }

    }
    [Serializable]
    public class Deatil
    {
        [SerializeField]
        public GameObject itemPrefab;
        [SerializeField]
        public string Name;
    }
}
