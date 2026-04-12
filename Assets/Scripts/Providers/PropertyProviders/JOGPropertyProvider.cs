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

        private RobotPropertyProvider _robotPropertyProvider;

        /// <summary>
        /// Óãë ïîâîðîòà ãåò ñåò èç ìåíþ
        /// </summary>
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

        public Vector3 LocalPosition { get => transform.localPosition; set => transform.localPosition = value; }
        public Quaternion LocalRotationQ { get => transform.localRotation; set => transform.localRotation = value; }
        public Vector3 GlobalPosition { get => transform.position; set => transform.position = value; }
        public Quaternion GlobalRotationQ{ get => transform.rotation; set => transform.rotation = value; }
        

        private bool EndEffectorOn
        {
            get => _robotPropertyProvider.EndEffectorOn;
            set => _robotPropertyProvider.EndEffectorOn = value;
        }

        public string Id { get => id; set => id = value; }
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
        public Vector3 Position { get => transform.localPosition * 1000; set => transform.localPosition = value / 1000; }
        public Vector3 GlobalPosition { get => transform.position; set => transform.position = value; }
        public Quaternion RotationQ
        {
            get => transform.rotation;
            set => transform.rotation = value;
        }
        public float A1
        {
            get => _robotPropertyProvider.L1;
            set => _robotPropertyProvider.L1 = value;
        }
        public float A2
        {
            get => _robotPropertyProvider.L2;
            set => _robotPropertyProvider.L2 = value;
        }
        public float A3
        {
            get => _robotPropertyProvider.L3;
            set => _robotPropertyProvider.L3 = value;
        }
        public float A4
        {
            get => _robotPropertyProvider.L4;
            set => _robotPropertyProvider.L5 = value;
        }
        public float A5
        {
            get => _robotPropertyProvider.L5;
            set => _robotPropertyProvider.L5 = value;
        }
        public float A6
        {
            get => _robotPropertyProvider.L6;
            set => _robotPropertyProvider.L6 = value;
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

        public IEnumerable<CustomProperty> GetCustomProperties()
        {
            var list = new List<CustomProperty>();
            var endEffect = new CustomProperty(
                "EndEffectorOn",
                "Ñîñòîÿíèå çàõâàòà",
                typeof(bool),
                () => EndEffectorOn,
                val => EndEffectorOn = (bool)val
            );

            //var a1 = new CustomProperty(
            //    "A1",
            //    "Îñü A1",
            //    typeof(float),
            //    () => A1,
            //    val => A1 = (float)val
            //);
            //var a2 = new CustomProperty(
            //     "A2",
            //     "Îñü A2",
            //     typeof(float),
            //     () => A2,
            //     val => A2 = (float)val
            // );
            //var a3 = new CustomProperty(
            //     "A3",
            //     "Îñü A3",
            //     typeof(float),
            //     () => A3,
            //     val => A3 = (float)val
            // );
            //var a4 = new CustomProperty(
            //     "A4",
            //     "Îñü A4",
            //     typeof(float),
            //     () => A4,
            //     val => A4 = (float)val
            // );
            //var a5 = new CustomProperty(
            //     "A5",
            //     "Îñü A5",
            //     typeof(float),
            //     () => A5,
            //     val => A5 = (float)val
            // );
            //var a6 = new CustomProperty(
            //     "A6",
            //     "Îñü A6",
            //     typeof(float),
            //     () => A6,
            //     val => A6 = (float)val
            // );
          
            //list.Add(a1);
            //list.Add(a2);
            //list.Add(a3);
            //list.Add(a4);
            //list.Add(a5);
            //list.Add(a6);
            list.Add(endEffect);
            return list;
        }

        public void RestoreCustomState(ProviderSaveData data)
        {
            if (data.BoolValues.TryGetValue("EndEffectorOn", out var v))
                EndEffectorOn = v;
        }

        private void CreateMeshVisual()
        {
            sphereMesh = CreateSphereMesh(PointSize, 16, 16);

            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

            meshFilter.mesh = sphereMesh;

            meshRenderer.material = material;
        }

        private Mesh CreateSphereMesh(float radius, int segmentsU, int segmentsV)
        {
            Mesh mesh = new Mesh();

            int vertexCount = (segmentsU + 1) * (segmentsV + 1);
            Vector3[] vertices = new Vector3[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];
            Vector2[] uv = new Vector2[vertexCount];
            int[] triangles = new int[segmentsU * segmentsV * 6];

            int index = 0;

            for (int v = 0; v <= segmentsV; v++)
            {
                float vAngle = Mathf.PI * v / segmentsV;
                float sinV = Mathf.Sin(vAngle);
                float cosV = Mathf.Cos(vAngle);

                for (int u = 0; u <= segmentsU; u++)
                {
                    float uAngle = 2 * Mathf.PI * u / segmentsU;
                    float sinU = Mathf.Sin(uAngle);
                    float cosU = Mathf.Cos(uAngle);

                    vertices[index] = new Vector3(
                        radius * sinV * cosU,
                        radius * cosV,
                        radius * sinV * sinU
                    );

                    normals[index] = vertices[index].normalized;
                    uv[index] = new Vector2((float)u / segmentsU, (float)v / segmentsV);
                    index++;
                }
            }

            index = 0;
            for (int v = 0; v < segmentsV; v++)
            {
                for (int u = 0; u < segmentsU; u++)
                {
                    int current = v * (segmentsU + 1) + u;
                    int next = current + segmentsU + 1;

                    triangles[index++] = current;
                    triangles[index++] = next;
                    triangles[index++] = current + 1;

                    triangles[index++] = current + 1;
                    triangles[index++] = next;
                    triangles[index++] = next + 1;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;

            return mesh;
        }

        protected string id;
        protected bool displayName = true;
        protected bool displayPosition = true;
        protected bool displayRotation = true;
        protected bool displayScale = true;
    }



}
