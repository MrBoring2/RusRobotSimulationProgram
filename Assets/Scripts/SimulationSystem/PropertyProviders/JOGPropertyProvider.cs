using Assets.Scripts.Models;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

namespace Assets.Scripts.Providers
{
    public class JOGPropertyProvider : MonoBehaviour, IPropertyProvider
    {
            
        public string Id
        {
            get => _robotPropertyProvider.Id;
            set => _robotPropertyProvider.Id = value;
        }
        private RobotPropertyProvider _robotPropertyProvider;

        
        private Vector3 rotationEuler;
        public Vector3 Rotation
        {
            get => new Vector3((float)Math.Round(transform.rotation.eulerAngles.x, 2),
                               (float)Math.Round(transform.rotation.eulerAngles.y, 2),
                               (float)Math.Round(transform.rotation.eulerAngles.z, 2));
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

        public Vector3 Position { get => transform.localPosition; set => transform.localPosition = value; }
        public Quaternion LocalRotationQ { get => transform.localRotation; set => transform.localRotation = value; }
        public Vector3 GlobalPosition { get => transform.position; set => transform.position = value; }
        public Quaternion GlobalRotationQ{ get => transform.rotation; set => transform.rotation = value; }
        
        public RobotPropertyProvider RobotPropertyProvider => _robotPropertyProvider;
        private bool EndEffectorOn
        {
            get => _robotPropertyProvider.EndEffectorOn;
            set => _robotPropertyProvider.EndEffectorOn = value;
        }
        public bool IsReadondly { get; set; }
        public string Name { get => gameObject.name; set { gameObject.name = value; } }
        public Vector3 Scale { get => transform.localScale; set => transform.localScale = value; }
        public bool DisplayName { get => displayName; set => displayName = value; }
        public bool DisplayPosition { get => displayPosition; set => displayPosition = value; }
        public bool DisplayRotation { get => displayRotation; set => displayRotation = value; }
        public bool DisplayScale { get => displayScale; set => displayScale = value; }

        

        public bool ShowVisual = true;
        public Material material;
        public float PointSize = 0.3f;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh sphereMesh;
        //public Vector3 PositionScale { get => transform.localPosition * 1000; set => transform.localPosition = value / 1000; }
       
        public Quaternion RotationQ
        {
            get => transform.rotation;
            set => transform.rotation = value;
        }

        private void Awake()
        {
            _robotPropertyProvider = transform.parent.GetComponent<RobotPropertyProvider>();
            displayScale = false;
        }

        public ProviderSaveData CaptureCustomState()
        {
            return new ProviderSaveData
            {
                ProviderType = nameof(RobotPropertyProvider),
                BoolValues = {
                    ["EndEffectorOn"] = EndEffectorOn
                }
            };
        }

        public  List<CustomProperty> GetCustomProperties()
        {
            var list = new List<CustomProperty>();
            var endEffect = new CustomProperty(
                "EndEffectorOn",
                "Статус захвата",
                typeof(bool),
                () => EndEffectorOn,
                val => EndEffectorOn = (bool)val
            );
            list.Add(endEffect);
            return list;
        }

        public void RestoreCustomState(ProviderSaveData data)
        {
            if (data.BoolValues.TryGetValue("EndEffectorOn", out var v))
                EndEffectorOn = v;
        }

        protected string id;
        protected bool displayName = true;
        protected bool displayPosition = true;
        protected bool displayRotation = true;
        protected bool displayScale = true;
    }



}
