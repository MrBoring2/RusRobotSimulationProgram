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
        public int ConfigPoint { get; set; }
        public bool VerificationAngles { get; set; }
        public bool AngleMode { get; set; }
        private bool EndEffectorOn
        {
            get
            {
                if (_robotPropertyProvider == null)
                    _robotPropertyProvider = transform.parent?.GetComponent<RobotPropertyProvider>();

                return _robotPropertyProvider != null ? _robotPropertyProvider.EndEffectorOn : false;
            }
            set
            {
                if (_robotPropertyProvider == null)
                    _robotPropertyProvider = transform.parent?.GetComponent<RobotPropertyProvider>();

                if (_robotPropertyProvider != null)
                    _robotPropertyProvider.EndEffectorOn = value;
            }
        }

        public string Id
        {
            get => _robotPropertyProvider?.Id;
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


        public Quaternion LRotationQ { get => transform.localRotation; set => transform.localRotation = value; }
        public Vector3 GlobalPosition { get => transform.position; set => transform.position = value; }
        public Quaternion GlobalRotationQ { get => transform.rotation; set => transform.rotation = value; }
        public Vector3 LocalPosition { get => transform.localPosition; set => transform.localPosition = value; }
        public RobotPropertyProvider RobotPropertyProvider => _robotPropertyProvider;

        public bool IsReadondly { get; set; }
        public string Name { get => gameObject.name; set { gameObject.name = value; } }
        public Vector3 Scale { get => transform.localScale; set => transform.localScale = value; }
        public bool DisplayName { get => displayName; set => displayName = value; }
        public bool DisplayPosition { get => displayPosition; set => displayPosition = value; }
        public bool DisplayRotation { get => displayRotation; set => displayRotation = value; }
        public bool DisplayScale { get => displayScale; set => displayScale = value; }
        public float J1Angle { get => _robotPropertyProvider.J1AngleUI; set => _robotPropertyProvider.J1AngleUI = value; }
        public float J2Angle { get => _robotPropertyProvider.J2AngleUI; set => _robotPropertyProvider.J2AngleUI = value; }
        public float J3Angle { get => _robotPropertyProvider.J3AngleUI; set => _robotPropertyProvider.J3AngleUI = value; }
        public float J4Angle { get => _robotPropertyProvider.J4AngleUI; set => _robotPropertyProvider.J4AngleUI = value; }
        public float J5Angle { get => _robotPropertyProvider.J5AngleUI; set => _robotPropertyProvider.J5AngleUI = value; }
        public float J6Angle { get => _robotPropertyProvider.J6AngleUI; set => _robotPropertyProvider.J6AngleUI = value; }
        public bool NameReadOnly { get; set; }

        public bool ShowVisual = true;
        public Material material;
        public float PointSize = 0.3f;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh sphereMesh;

        public Quaternion RotationQ
        {
            get => transform.rotation;
            set => transform.rotation = value;
        }

        private void Awake()
        {
            if (_robotPropertyProvider == null)
                _robotPropertyProvider = transform.parent?.GetComponent<RobotPropertyProvider>();
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

        public List<CustomProperty> GetCustomProperties()
        {
            var list = new List<CustomProperty>()
            {
                new CustomProperty(
                "EndEffectorOn",
                "Статус захвата",
                typeof(bool),
                () => EndEffectorOn,
                val => EndEffectorOn = (bool)val
                ),
                new CustomProperty(
                "AngleMode",
                "Режим углов",
                typeof(bool),
                () => AngleMode,
                val => AngleMode = (bool)val
                ),
                new CustomProperty(
                "ConfigPoint",
                "Конфигурация",
                typeof(int),
                () => (ConfigPoint+1),
                val => ConfigPoint = (int)val-1
                ),
                new CustomProperty(
                "VerificationAngles",
                "Проверка углов",
                typeof(bool),
                () => VerificationAngles,
                val => VerificationAngles = (bool)val
                ),
                 new CustomProperty("J1Angle",
                "Ось 1",
                typeof(float),
                () => J1Angle,
                val =>J1Angle = (float)val)
                .WithAttribute(new RangeAttribute(_robotPropertyProvider.AnglesLimitUI[0], _robotPropertyProvider.AnglesLimitUI[1])),
                new CustomProperty("J2Angle",
                "Ось 2",
                typeof(float),
                () => J2Angle,
                val => J2Angle = (float)val)
                .WithAttribute(new RangeAttribute( _robotPropertyProvider.AnglesLimitUI[2],  _robotPropertyProvider.AnglesLimitUI[3])),
                new CustomProperty("J3Angle",
                "Ось 3",
                typeof(float),
                () => J3Angle,
                val =>J3Angle = (float)val)
                .WithAttribute(new RangeAttribute( _robotPropertyProvider.AnglesLimitUI[4],  _robotPropertyProvider.AnglesLimitUI[5])),
                new CustomProperty("J4Angle",
                "Ось 4",
                typeof(float),
                () => J4Angle,
                val =>J4Angle = (float)val)
                .WithAttribute(new RangeAttribute(_robotPropertyProvider.AnglesLimitUI[6],  _robotPropertyProvider.AnglesLimitUI[7])),
                new CustomProperty("J5Angle",
                "Ось 5",
                typeof(float),
                () => J5Angle,
                val => J5Angle = (float)val)
                .WithAttribute(new RangeAttribute( _robotPropertyProvider.AnglesLimitUI[8],  _robotPropertyProvider.AnglesLimitUI[9])),
                new CustomProperty("J6Angle",
                "Ось 6",
                typeof(float),
                () => J6Angle,
                val => J6Angle = (float)val)
                .WithAttribute(new RangeAttribute( _robotPropertyProvider.AnglesLimitUI[10],  _robotPropertyProvider.AnglesLimitUI[11]))

            };

            return list;
        }


        public void RestoreCustomState(ProviderSaveData data)
        {
            if (data.BoolValues.TryGetValue("EndEffectorOn", out var v1))
                EndEffectorOn = v1;
            if (data.FloatValues.TryGetValue("ConfigPoint", out var v2))
                ConfigPoint = (int)v2;
            if (data.BoolValues.TryGetValue("VerificationAngles", out var v3))
                VerificationAngles = (bool)v3;
        }

        protected string id;
        protected bool displayName = true;
        protected bool displayPosition = true;
        protected bool displayRotation = true;
        protected bool displayScale = true;
    }



}
