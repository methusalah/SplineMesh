using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SplineMesh {
    /// <summary>
    /// Example of component to show that the spline is an independant mathematical component and can be used for other purposes than mesh deformation.
    /// 
    /// This component is only for demo purpose and is not intended to be used as-is.
    /// 
    /// We only move an object along the spline. Imagine a camera route, a ship patrol...
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Spline))]
    public class ExampleGrowingRoot : MonoBehaviour {
        private Spline spline;
        private float rate = 0;
        private MeshBender meshBender;

        [HideInInspector]
        public GameObject generated;

        public Mesh mesh;
        public Material material;
        public Vector3 rotation;
        public Vector3 scale;

        public float startScale = 1;

        public float DurationInSecond;

        private void OnEnable() {
            rate = 0;
            if (generated == null) {
                generated = new GameObject("generated root",
                    typeof(MeshFilter),
                    typeof(MeshRenderer),
                    typeof(MeshBender));
                generated.transform.parent = transform;
            }
            generated.transform.localRotation = Quaternion.identity;
            generated.transform.localPosition = Vector3.zero;
            generated.transform.localScale = Vector3.one;
            Init();
#if UNITY_EDITOR
            EditorApplication.update += EditorUpdate;
#endif
        }

        void OnDisable() {
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
#endif
        }

        private void OnValidate() {
            Init();
        }

        private void Update() {
            if (Application.isPlaying) EditorUpdate();
        }

        void EditorUpdate() {
            rate += Time.deltaTime / DurationInSecond;
            if (rate > 1) {
                rate --;
            }
            Contort();
        }

        private void Contort() {
            float nodeDistance = 0;
            int i = 0;
            foreach (var n in spline.nodes) {
                float nodeDistanceRate = nodeDistance / spline.Length;
                float nodeScale = startScale * (rate - nodeDistanceRate);
                n.Scale = new Vector2(nodeScale, nodeScale);
                if (i < spline.curves.Count) {
                    nodeDistance += spline.curves[i++].Length;
                }
            }

            if (generated != null) {
                meshBender.SetInterval(spline, 0, spline.Length * rate);
                meshBender.ComputeIfNeeded();
            }
        }

        private void Init() {
            generated.GetComponent<MeshRenderer>().material = material;

            meshBender = generated.GetComponent<MeshBender>();
            spline = GetComponent<Spline>();

            meshBender.Source = SourceMesh.Build(mesh)
                .Rotate(Quaternion.Euler(rotation))
                .Scale(scale);
            meshBender.Mode = MeshBender.FillingMode.StretchToInterval;
            meshBender.SetInterval(spline, 0, 0.01f);
        }
    }
}
