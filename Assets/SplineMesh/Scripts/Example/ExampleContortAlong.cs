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
    public class ExampleContortAlong : MonoBehaviour {
        private Spline spline;
        private float rate = 0;
        private MeshBender meshBender;

        [HideInInspector]
        public GameObject generated;

        public Mesh mesh;
        public Material material;
        public Vector3 rotation;
        public Vector3 scale;

        public float DurationInSecond;

        private void OnEnable() {
            rate = 0;
            if (generated == null) {
                generated = new GameObject("generated contortionist",
                    typeof(MeshFilter),
                    typeof(MeshRenderer),
                    typeof(MeshBender));
                generated.GetComponent<MeshRenderer>().material = material;
                generated.transform.parent = transform;
            }
            generated.transform.localRotation = Quaternion.identity;
            generated.transform.localPosition = Vector3.zero;
            generated.transform.localScale = Vector3.one;

            meshBender = generated.GetComponent<MeshBender>();
            meshBender.Source = SourceMesh.Build(mesh)
                .Rotate(Quaternion.Euler(rotation))
                .Scale(scale);

            spline = GetComponent<Spline>(); 
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
            meshBender.Source = SourceMesh.Build(mesh)
                .Rotate(Quaternion.Euler(rotation))
                .Scale(scale);
        }

        void EditorUpdate() {
            rate += Time.deltaTime / DurationInSecond;
            if (rate > 1) {
                rate --;
            }

            Contort();
        }

        private void Contort() {
            if (generated != null) {
                meshBender.SetInterval(spline, spline.Length * rate);
                meshBender.Compute();
            }
        }
    }
}
