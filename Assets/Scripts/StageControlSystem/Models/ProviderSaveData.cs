using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Models
{
    [System.Serializable]
    public class ProviderSaveData
    {
        public string ProviderType;
        public Dictionary<string, float> FloatValues = new();
        public Dictionary<string, bool> BoolValues = new();
        public ColorObj Color;
    }
}
