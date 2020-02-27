using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

namespace SplineMesh {
    /// <summary>
    /// A curved line made of oriented nodes.
    /// Each segment is a cubic Bézier curve connected to spline nodes.
    /// It provides methods to get positions and tangent along the spline, specifying a distance or a ratio, plus the curve length.
    /// The spline and the nodes raise events each time something is changed.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class Spline : MonoBehaviour {
        /// <summary>
        /// The spline nodes.
        /// Warning, this collection shouldn't be changed manualy. Use specific methods to add and remove nodes.
        /// It is public only for the user to enter exact values of position and direction in the inspector (and serialization purposes).
        /// </summary>
        public List<SplineNode> nodes = new List<SplineNode>();

        /// <summary>
        /// The generated curves. Should not be changed in any way, use nodes instead.
        /// </summary>
        [HideInInspector]
        public List<CubicBezierCurve> curves = new List<CubicBezierCurve>();

        /// <summary>
        /// The spline length in world units.
        /// </summary>
        public float Length;

        [SerializeField]
        private bool isLoop;

        public bool IsLoop {
            get { return isLoop; }
            set {
                isLoop = value;
                updateLoopBinding();
            }
        }

        /// <summary>
        /// Event raised when the node collection changes
        /// </summary>
        public event ListChangeHandler<SplineNode> NodeListChanged;

        /// <summary>
        /// Event raised when one of the curve changes.
        /// </summary>
        [HideInInspector]
        public UnityEvent CurveChanged = new UnityEvent();

        /// <summary>
        /// Clear the nodes and curves, then add two default nodes for the reset spline to be visible in editor.
        /// </summary>
        private void Reset() {
            nodes.Clear();
            curves.Clear();
            AddNode(new SplineNode(new Vector3(5, 0, 0), new Vector3(5, 0, -3)));
            AddNode(new SplineNode(new Vector3(10, 0, 0), new Vector3(10, 0, 3)));
            RaiseNodeListChanged(new ListChangedEventArgs<SplineNode>() {
                type = ListChangeType.clear
            });
            UpdateAfterCurveChanged();
        }

        private void OnEnable() {
            RefreshCurves();
        }

        public ReadOnlyCollection<CubicBezierCurve> GetCurves() {
            return curves.AsReadOnly();
        }

        private void RaiseNodeListChanged(ListChangedEventArgs<SplineNode> args) {
            if (NodeListChanged != null)
                NodeListChanged.Invoke(this, args);
        }

        private void UpdateAfterCurveChanged() {
            Length = 0;
            foreach (var curve in curves) {
                Length += curve.Length;
            }
            CurveChanged.Invoke();
        }

        /// <summary>
        /// Returns an interpolated sample of the spline, containing all curve data at this time.
        /// Time must be between 0 and the number of nodes.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public CurveSample GetSample(float t) {
            int index = GetNodeIndexForTime(t);
            return curves[index].GetSample(t - index);
        }

        /// <summary>
        /// Returns the curve at the given time.
        /// Time must be between 0 and the number of nodes.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public CubicBezierCurve GetCurve(float t) {
            return curves[GetNodeIndexForTime(t)];
        }

        private int GetNodeIndexForTime(float t) {
            if (t < 0 || t > nodes.Count - 1) {
                throw new ArgumentException(string.Format("Time must be between 0 and last node index ({0}). Given time was {1}.", nodes.Count - 1, t));
            }
            int res = Mathf.FloorToInt(t);
            if (res == nodes.Count - 1)
                res--;
            return res;
        }
		
	/// <summary>
	/// Refreshes the spline's internal list of curves.
	// </summary>
	public void RefreshCurves() {
            curves.Clear();
            for (int i = 0; i < nodes.Count - 1; i++) {
                SplineNode n = nodes[i];
                SplineNode next = nodes[i + 1];

                CubicBezierCurve curve = new CubicBezierCurve(n, next);
                curve.Changed.AddListener(UpdateAfterCurveChanged);
                curves.Add(curve);
            }
            RaiseNodeListChanged(new ListChangedEventArgs<SplineNode>() {
                type = ListChangeType.clear
            });
            UpdateAfterCurveChanged();
        }

        /// <summary>
        /// Returns an interpolated sample of the spline, containing all curve data at this distance.
        /// Distance must be between 0 and the spline length.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public CurveSample GetSampleAtDistance(float d) {
            if (d < 0 || d > Length)
                throw new ArgumentException(string.Format("Distance must be between 0 and spline length ({0}). Given distance was {1}.", Length, d));
            foreach (CubicBezierCurve curve in curves) {
                // test if distance is approximatly equals to curve length, because spline
                // length may be greater than cumulated curve length due to float precision
                if(d > curve.Length && d < curve.Length + 0.0001f) {
                    d = curve.Length;
                }
                if (d > curve.Length) {
                    d -= curve.Length;
                } else {
                    return curve.GetSampleAtDistance(d);
                }
            }
            throw new Exception("Something went wrong with GetSampleAtDistance.");
        }

        /// <summary>
        /// Adds a node at the end of the spline.
        /// </summary>
        /// <param name="node"></param>
        public void AddNode(SplineNode node) {
            nodes.Add(node);
            if (nodes.Count != 1) {
                SplineNode previousNode = nodes[nodes.IndexOf(node) - 1];
                CubicBezierCurve curve = new CubicBezierCurve(previousNode, node);
                curve.Changed.AddListener(UpdateAfterCurveChanged);
                curves.Add(curve);
            }
            RaiseNodeListChanged(new ListChangedEventArgs<SplineNode>() {
                type = ListChangeType.Add,
                newItems = new List<SplineNode>() { node }
            });

            UpdateAfterCurveChanged();
            updateLoopBinding();
        }

        /// <summary>
        /// Insert the given node in the spline at index. Index must be greater than 0 and less than node count.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="node"></param>
        public void InsertNode(int index, SplineNode node) {
            if (index == 0)
                throw new Exception("Can't insert a node at index 0");

            SplineNode previousNode = nodes[index - 1];
            SplineNode nextNode = nodes[index];

            nodes.Insert(index, node);

            curves[index - 1].ConnectEnd(node);

            CubicBezierCurve curve = new CubicBezierCurve(node, nextNode);
            curve.Changed.AddListener(UpdateAfterCurveChanged);
            curves.Insert(index, curve);
            RaiseNodeListChanged(new ListChangedEventArgs<SplineNode>() {
                type = ListChangeType.Insert,
                newItems = new List<SplineNode>() { node },
                insertIndex = index
            });
            UpdateAfterCurveChanged();
            updateLoopBinding();
        }

        /// <summary>
        /// Remove the given node from the spline. The given node must exist and the spline must have more than 2 nodes.
        /// </summary>
        /// <param name="node"></param>
        public void RemoveNode(SplineNode node) {
            int index = nodes.IndexOf(node);

            if (nodes.Count <= 2) {
                throw new Exception("Can't remove the node because a spline needs at least 2 nodes.");
            }

            CubicBezierCurve toRemove = index == nodes.Count - 1 ? curves[index - 1] : curves[index];
            if (index != 0 && index != nodes.Count - 1) {
                SplineNode nextNode = nodes[index + 1];
                curves[index - 1].ConnectEnd(nextNode);
            }

            nodes.RemoveAt(index);
            toRemove.Changed.RemoveListener(UpdateAfterCurveChanged);
            curves.Remove(toRemove);

            RaiseNodeListChanged(new ListChangedEventArgs<SplineNode>() {
                type = ListChangeType.Remove,
                removedItems = new List<SplineNode>() { node },
                removeIndex = index
            });
            UpdateAfterCurveChanged();
            updateLoopBinding();
        }

        SplineNode start, end;
        private void updateLoopBinding() {
            if(start != null) {
                start.Changed -= StartNodeChanged;
            }
            if(end != null) {
                end.Changed -= EndNodeChanged;
            }
            if (isLoop) {
                start = nodes[0];
                end = nodes[nodes.Count - 1];
                start.Changed += StartNodeChanged;
                end.Changed += EndNodeChanged;
                StartNodeChanged(null, null);
            } else {
                start = null;
                end = null;
            }
        }

        private void StartNodeChanged(object sender, EventArgs e) {
            end.Changed -= EndNodeChanged;
            end.Position = start.Position;
            end.Direction = start.Direction;
            end.Roll = start.Roll;
            end.Scale = start.Scale;
            end.Up = start.Up;
            end.Changed += EndNodeChanged;
        }

        private void EndNodeChanged(object sender, EventArgs e) {
            start.Changed -= StartNodeChanged;
            start.Position = end.Position;
            start.Direction = end.Direction;
            start.Roll = end.Roll;
            start.Scale = end.Scale;
            start.Up = end.Up;
            start.Changed += StartNodeChanged;
        }

        public CurveSample GetProjectionSample(Vector3 pointToProject) {
            CurveSample closest = default(CurveSample);
            float minSqrDistance = float.MaxValue;
            foreach (var curve in curves) {
                var projection = curve.GetProjectionSample(pointToProject);
                if (curve == curves[0]) {
                    closest = projection;
                    minSqrDistance = (projection.location - pointToProject).sqrMagnitude;
                    continue;
                }
                var sqrDist = (projection.location - pointToProject).sqrMagnitude;
                if (sqrDist < minSqrDistance) {
                    minSqrDistance = sqrDist;
                    closest = projection;
                }
            }
            return closest;
        }
    }

    public enum ListChangeType {
        Add,
        Insert,
        Remove,
        clear,
    }
    public class ListChangedEventArgs<T> : EventArgs {
        public ListChangeType type;
        public List<T> newItems;
        public List<T> removedItems;
        public int insertIndex, removeIndex;
    }
    public delegate void ListChangeHandler<T2>(object sender, ListChangedEventArgs<T2> args);

}
