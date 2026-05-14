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
    public class SpawnPropertyProvider : BasePropertyProvider
    {
        //=================== ПАРАМЕТРЫ ===================
        [SerializeField]
        public string DetailName = "тип1";
        public float spawnInterval = 5f; // Время между спавнами
        public bool spawnClick = false; // Спавнить по клику
        public bool spawnOn = false; // Спавнить включено/выключено
        public Vector3 RotateSpawnDetail = Vector3.zero;
        //===========================================//

        //===========================================//

        [SerializeField]
        public List<Deatil> detailsList = new List<Deatil>();


        public override ProviderSaveData CaptureCustomState()
        {
            return new ProviderSaveData
            {
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
    [Serializable]
    public class Deatil
    {
        [SerializeField]
        public GameObject itemPrefab;
        [SerializeField]
        public string Name;
    }
}
