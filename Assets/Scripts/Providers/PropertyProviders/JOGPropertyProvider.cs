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



        public string Id { get => id; set => id = value; }
        public bool IsReadondly { get; set; }
        public string Name { get => gameObject.name; set { gameObject.name = value; } }
        protected Vector3 rotationEuler;
        protected string id;
        protected bool displayName = true;
        protected bool displayPosition = true;
        protected bool displayRotation = true;
        protected bool displayScale = true;
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

        private RobotPropertyProvider _robotPropertyProvider;
        private bool EndEffectorOn
        {
            get => _robotPropertyProvider.EndEffectorOn;
            set => _robotPropertyProvider.EndEffectorOn = value;
         }




        public bool ShowVisual = true;
        public Material material;
        public float PointSize = 0.3f;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh sphereMesh;
        public Vector3 Position { get => transform.localPosition*1000; set => transform.localPosition = value/1000; }
        public Vector3 GlobalPosition { get => transform.position; set => transform.position = value; }
        public Quaternion RotationQ
        {
            get => transform.rotation;
            set => transform.rotation = value;
        }

        private void Awake()
        {
            _robotPropertyProvider = transform.parent.GetComponent<RobotPropertyProvider>();
            displayScale = false;
            if (ShowVisual)
            {
                //CreateMeshVisual();
            }
        }

        public ProviderSaveData CaptureCustomState()
        {
            return new ProviderSaveData
            {
                ProviderType = nameof(RobotPropertyProvider),
                BoolValues = {
                ["EndEffectorOn"] = EndEffectorOn  //Изм. на буферизацию компонента!
            }
            };
        }

        public  IEnumerable<CustomProperty> GetCustomProperties()
        {
            yield return new CustomProperty(
            "EndEffectorOn",
            "Состояние захвата",
            typeof(bool),
            () => EndEffectorOn,
            val => EndEffectorOn = (bool)val //Изм. на буферизацию компонента!
        );
        }

        public  void RestoreCustomState(ProviderSaveData data)
        {
            if (data.BoolValues.TryGetValue("EndEffectorOn", out var v))
                EndEffectorOn = v;  //Изм. на буферизацию компонента!
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
        //public void UpdateVisual(Color newColor, float newSize)
        //{
        //    if (meshRenderer != null && meshRenderer.material != null)
        //    {
        //        meshRenderer.material.color = newColor;
        //    }

        //    if (sphereMesh != null && meshFilter != null)
        //    {
        //        var vertices = sphereMesh.vertices;
        //        for (int i = 0; i < vertices.Length; i++)
        //        {
        //            vertices[i] = vertices[i].normalized * newSize;
        //        }
        //        sphereMesh.vertices = vertices;
        //        sphereMesh.RecalculateBounds();
        //    }
        //}
    }
}
