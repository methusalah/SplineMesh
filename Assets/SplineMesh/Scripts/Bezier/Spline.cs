using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

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
    /// It is public only for the user to enter exact values of position and direction in the inspector (end serialization purposes).
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

    /// <summary>
    /// Event raised when the node collection changes
    /// </summary>
    [HideInInspector]
    public UnityEvent NodeCountChanged = new UnityEvent();

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
        RaiseNodeCountChanged();
        UpdateAfterCurveChanged();
    }

    private void OnEnable() {
        curves.Clear();
        for (int i = 0; i < nodes.Count - 1; i++) {
            SplineNode n = nodes[i];
            SplineNode next = nodes[i + 1];

            CubicBezierCurve curve = new CubicBezierCurve(n, next);
            curve.Changed.AddListener(() => UpdateAfterCurveChanged());
            curves.Add(curve);
        }
        RaiseNodeCountChanged();
        UpdateAfterCurveChanged();
    }

    public ReadOnlyCollection<CubicBezierCurve> GetCurves() {
        return curves.AsReadOnly();
    }

    private void RaiseNodeCountChanged() {
        if (NodeCountChanged != null)
            NodeCountChanged.Invoke();
    }

    private void UpdateAfterCurveChanged() {
        Length = 0;
        foreach (var curve in curves) {
            Length += curve.Length;
        }
        if (CurveChanged != null) {
            CurveChanged.Invoke();
        }
    }

    /// <summary>
    /// Returns the point on spline at time. Time must be between 0 and the nodes count.
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public Vector3 GetLocationAlongSpline(float t)
    {
        int index = GetNodeIndexForTime(t);
        return curves[index].GetLocation(t - index);
    }

    /// <summary>
    /// Returns the tangent of spline at time. Time must be between 0 and the nodes count.
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public Vector3 GetTangentAlongSpline(float t)
    {
        int index = GetNodeIndexForTime(t);
        return curves[index].GetTangent(t - index);
    }

    private int GetNodeIndexForTime(float t) {
        if (t < 0 || t > nodes.Count - 1) {
            throw new ArgumentException(string.Format("Time must be between 0 and last node index ({0}). Given time was {1}.", nodes.Count-1, t));
        }
        int res = Mathf.FloorToInt(t);
        if (res == nodes.Count - 1)
            res--;
        return res;
    }

    /// <summary>
    /// Returns the point on spline at distance. Distance must be between 0 and spline length.
    /// </summary>
    /// <param name="d"></param>
    /// <returns></returns>
    public Vector3 GetLocationAlongSplineAtDistance(float d) {
        if(d < 0 || d > Length)
            throw new ArgumentException(string.Format("Distance must be between 0 and spline length ({0}). Given distance was {1}.", Length, d));
        foreach (CubicBezierCurve curve in curves) {
            if (d > curve.Length) {
                d -= curve.Length;
            } else {
                return curve.GetLocationAtDistance(d);
            }
        }
        throw new Exception("Something went wrong with GetLocationAlongSplineAtDistance");
    }

    /// <summary>
    /// Returns the tangent of spline at distance. Distance must be between 0 and spline length.
    /// </summary>
    /// <param name="d"></param>
    /// <returns></returns>
    public Vector3 GetTangentAlongSplineAtDistance(float d) {
        if (d < 0 || d > Length)
            throw new ArgumentException(string.Format("Distance must be between 0 and spline length ({0}). Given distance was {1}.", Length, d));
        foreach (CubicBezierCurve curve in curves) {
            if (d > curve.Length) {
                d -= curve.Length;
            } else {
                return curve.GetTangentAtDistance(d);
            }
        }
        throw new Exception("Something went wrong with GetTangentAlongSplineAtDistance");
    }

    /// <summary>
    /// Adds a node at the end of the spline.
    /// </summary>
    /// <param name="node"></param>
    public void AddNode(SplineNode node)
    {
        nodes.Add(node);
        if (nodes.Count != 1) {
            SplineNode previousNode = nodes[nodes.IndexOf(node)-1];
            CubicBezierCurve curve = new CubicBezierCurve(previousNode, node);
            curve.Changed.AddListener(() => UpdateAfterCurveChanged());
            curves.Add(curve);
        }
        RaiseNodeCountChanged();
        UpdateAfterCurveChanged();
    }

    /// <summary>
    /// Insert the given node in the spline at index. Index must be greater than 0 and less than node count.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="node"></param>
    public void InsertNode(int index, SplineNode node)
    {
        if (index == 0)
            throw new Exception("Can't insert a node at index 0");

        SplineNode previousNode = nodes[index - 1];
        SplineNode nextNode = nodes[index];

        nodes.Insert(index, node);
        
        curves[index-1].ConnectEnd(node);

        CubicBezierCurve curve = new CubicBezierCurve(node, nextNode);
        curve.Changed.AddListener(() => UpdateAfterCurveChanged());
        curves.Insert(index, curve);
        RaiseNodeCountChanged();
        UpdateAfterCurveChanged();
    }

    /// <summary>
    /// Remove the given node from the spline. The given node must exist and the spline must have more than 2 nodes.
    /// </summary>
    /// <param name="node"></param>
    public void RemoveNode(SplineNode node)
    {
        int index = nodes.IndexOf(node);

        if(nodes.Count <= 2) {
            throw new Exception("Can't remove the node because a spline needs at least 2 nodes.");
        }

        CubicBezierCurve toRemove = index == nodes.Count - 1? curves[index - 1] : curves[index];
        if (index != 0 && index != nodes.Count - 1) {
            SplineNode nextNode = nodes[index + 1];
            curves[index - 1].ConnectEnd(nextNode);
        }

        nodes.RemoveAt(index);
        toRemove.Changed.RemoveListener(() => UpdateAfterCurveChanged());
        curves.Remove(toRemove);

        RaiseNodeCountChanged();
        UpdateAfterCurveChanged();
    }
}
