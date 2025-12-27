using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    public class PropertProviderUtils
    {
        public static IPropertyProvider FindProvider(string objectId)
        {
            var obj = GameObject.Find(objectId);
            if (obj != null)
                return obj.GetComponent<IPropertyProvider>();
            return null;
        }

        public static List<IPropertyProvider> FindAllProviders()
        {
            var providers = new List<IPropertyProvider>();
            var objects = GameObject.FindGameObjectsWithTag("SceneObject");

            foreach (var obj in objects)
            {
                var provider = obj.GetComponent<IPropertyProvider>();
                if (provider != null)
                    providers.Add(provider);
            }

            return providers;
        }
    }
}
