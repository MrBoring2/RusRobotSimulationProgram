using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Providers
{
    public abstract class BasePropertyProvider : MonoBehaviour, IPropertyProvider
    {
        public string Id { get => id; set => id = value; }

        public string Name { get => gameObject.name; set { gameObject.name = value; } }
        public Vector3 LocalPosition { get => transform.position; set => transform.position = value; }
        protected Vector3 rotationEuler;
        protected string id;
        protected bool displayName = true;
        protected bool displayPosition = true;
        protected bool displayRotation = true;
        protected bool displayScale = true;
        public bool IsReadondly { get; set; }
   
        public Vector3 Rotation
        {
            get => transform.rotation.eulerAngles;
            set
            {
                rotationEuler = new Vector3(
                    Mathf.Repeat(value.x, 361f),
                    Mathf.Repeat(value.y, 361f),
                    Mathf.Repeat(value.z, 361f)
                );
                transform.rotation = Quaternion.Euler(rotationEuler);
            }
        }

        public Vector3 Scale { get => transform.localScale; set => transform.localScale = value; }
        public bool DisplayName { get => displayName; set => displayName = value; }
        public bool DisplayPosition { get => displayPosition; set => displayPosition = value; }
        public bool DisplayRotation { get => displayRotation; set => displayRotation = value; }
        public bool DisplayScale { get => displayScale; set => displayScale = value; }

        private void Awake()
        {
            rotationEuler = transform.eulerAngles;
        }


        public abstract ProviderSaveData CaptureCustomState();

        public abstract IEnumerable<CustomProperty> GetCustomProperties();

        public abstract void RestoreCustomState(ProviderSaveData data);
    }
}
