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
        public Vector3 Position
        {
            get => transform.localPosition;
            set=> transform.localPosition = value;
        }
        //public float PersentSpeed { get; set; } = 100f;
        public float Speed { get; set;} = 0.1f;
       // public TypePoint pointType = TypePoint.LIN;
        //магнит
       // public MagnitS magnitStatus = MagnitS.NotControl;

        //в точке
        //public float delay = 0;
        private void Awake()
        {
            displayScale = false;
            if (ShowVisual)
            {
                //CreateMeshVisual();
            }
        }

        public override ProviderSaveData CaptureCustomState()
        {
            return new ProviderSaveData
            {
                ProviderType = nameof(LinearPointPropertyProvider),
                FloatValues =
                {
                    ["Speed"] = Speed
                }
            };
        }

        public override IEnumerable<CustomProperty> GetCustomProperties()
        {
            yield return new CustomProperty(
                "Speed",
                "Скорость",
                typeof(float),
                () => Speed,
                val => Speed = (float)val
            );
        }

        public override void RestoreCustomState(ProviderSaveData data)
        {
            if (data.FloatValues.TryGetValue("Speed", out var v))
                Speed = v;
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
