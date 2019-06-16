using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

namespace SplineMesh {
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    [RequireComponent(typeof(Spline))]
    public class SplineSmoother : MonoBehaviour {
        private Spline spline;

        [Range(0, 1f)] public float curvature = 0.3f;

        private void Awake() {
            spline = GetComponent<Spline>();
        }

        private void OnValidate() {
            Smooth(null, null);
        }

        private void OnEnable() {
            spline.NodeListChanged += Spline_NodeListChanged;
            foreach(var node in spline.nodes) {
                node.Changed += Smooth;
            }
        }

        private void OnDisable() {
            spline.NodeListChanged -= Spline_NodeListChanged;
            foreach (var node in spline.nodes) {
                node.Changed -= Smooth;
            }
        }

        private void Spline_NodeListChanged(object sender, ListChangedEventArgs<SplineNode> args) {
            foreach(var node in args.newItems) {
                node.Changed += Smooth;
            }
            foreach(var node in args.removedItems) {
                node.Changed -= Smooth;
            }
        }

        private void Smooth(object sender, EventArgs e) {
            int i = 0;
            foreach(var node in spline.nodes) {
                var pos = node.Position;
                // For the direction, we need to compute a smooth vector.
                // Orientation is obtained by substracting the vectors to the previous and next way points,
                // which give an acceptable tangent in most situations.
                // Then we apply a part of the average magnitude of these two vectors, according to the smoothness we want.
                var dir = Vector3.zero;
                float averageMagnitude = 0;
                if (i != 0) {
                    var previousPos = spline.nodes[i - 1].Position;
                    var toPrevious = pos - previousPos;
                    averageMagnitude += toPrevious.magnitude;
                    dir += toPrevious.normalized;
                }
                if (i != spline.nodes.Count - 1) {
                    var nextPos = spline.nodes[i + 1].Position;
                    var toNext = pos - nextPos;
                    averageMagnitude += toNext.magnitude;
                    dir -= toNext.normalized;
                }
                averageMagnitude *= 0.5f;
                // This constant should vary between 0 and 0.5, and allows to add more or less smoothness.
                dir = dir.normalized * averageMagnitude * curvature;

                // In SplineMesh, the node direction is not relative to the node position. 
                var controlPoint = dir + pos;

                // We only set one direction at each spline node because SplineMesh only support mirrored direction between curves.
                node.Direction = controlPoint;
                i++;
            }
        }
    }
}
