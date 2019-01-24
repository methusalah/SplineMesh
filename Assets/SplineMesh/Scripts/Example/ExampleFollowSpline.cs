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
    public class ExampleFollowSpline : MonoBehaviour {

        public GameObject Follower;
        public float DurationInSecond;

        [HideInInspector]
        public GameObject go;

        private Spline spline;
        private float rate = 0;

        private void OnEnable() {
            rate = 0;
            if (go == null) {
                go = Instantiate(Follower, transform);
            }

            go.transform.localRotation = Quaternion.identity;
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;

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

        void EditorUpdate() {
            rate += Time.deltaTime / DurationInSecond;
            if (rate > spline.nodes.Count - 1) {
                rate -= spline.nodes.Count - 1;
            }
            PlaceFollower();
        }

        private void PlaceFollower() {
            if (go != null) {
                CurveSample sample = spline.GetSample(rate);
                go.transform.localPosition = sample.location;
                go.transform.localRotation = sample.Rotation;
            }
        }
    }
}
