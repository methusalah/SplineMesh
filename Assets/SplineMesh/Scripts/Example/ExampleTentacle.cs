using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SplineMesh {
    /// <summary>
    /// Example of component to bend a mesh along a spline with some interpolation of scales and rolls. This component can be used as-is but will most likely be a base for your own component.
    /// 
    /// For explanations of the base component, <see cref="ExamplePipe"/>
    /// 
    /// In this component, we have added properties to make scale and roll vary between spline start and end.
    /// Intermediate scale and roll values are calculated at each spline node accordingly to the distance, then given to the MeshBenders component.
    /// MeshBender applies scales and rolls values by interpollation if they differ from strat to end of the curve.
    /// 
    /// You can easily imagine a list of scales to apply to each node independantly to create your own variation.
    /// </summary>
    [ExecuteInEditMode]
    [SelectionBase]
    public class ExampleTentacle : MonoBehaviour {
        public Mesh mesh;
        public Material material;
        public Vector3 rotation;
        public float startScale = 1, endScale = 1;
        public float startRoll = 0, endRoll = 0;

        private Spline spline = null;
        public List<GameObject> meshes = new List<GameObject>();
        private bool toUpdate = false;

        private void OnEnable() {
            spline = GetComponent<Spline>();
            spline.NodeListChanged += (s, e) => { toUpdate = true; };
        }

        private void OnValidate() {
            if (spline == null)
                return;

            // apply scale and roll at each node
            float currentLength = 0;
            foreach (CubicBezierCurve curve in spline.GetCurves()) {
                float startRate = currentLength / spline.Length;
                currentLength += curve.Length;
                float endRate = currentLength / spline.Length;

                curve.n1.Scale = Vector2.one * (startScale + (endScale - startScale) * startRate);
                curve.n2.Scale = Vector2.one * (startScale + (endScale - startScale) * endRate);

                curve.n1.Roll = startRoll + (endRoll - startRoll) * startRate;
                curve.n2.Roll = startRoll + (endRoll - startRoll) * endRate;
            }
            toUpdate = true;
        }

        private void Update() {
            if (toUpdate) {
                toUpdate = false;
                CreateMeshes();
            }
        }

        public void CreateMeshes() {
            foreach (GameObject go in meshes) {
                if (gameObject != null) {
                    if (Application.isPlaying) {
                        Destroy(go);
                    } else {
                        DestroyImmediate(go);
                    }
                }
            }
            meshes.Clear();

            int i = 0;
            foreach (CubicBezierCurve curve in spline.GetCurves()) {
                GameObject go = new GameObject("SplineMesh" + i++, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshBender), typeof(MeshCollider));
                go.transform.parent = transform;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                //go.hideFlags = HideFlags.NotEditable;

                go.GetComponent<MeshRenderer>().material = material;
                MeshBender mb = go.GetComponent<MeshBender>();
                mb.Source = mesh;
                mb.Rotation = Quaternion.Euler(rotation);
                mb.Curve = curve;
                mb.ComputeIfNeeded();
                meshes.Add(go);
            }
        }
    }
}
