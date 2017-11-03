using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

[ExecuteInEditMode]
public class Spline : MonoBehaviour {
    public List<SplineNode> nodes = new List<SplineNode>();
    [HideInInspector]
    public List<CubicBezierCurve> curves = new List<CubicBezierCurve>();

    /// <summary>
    /// Clear the nodes and curves, then add two default nodes for the spline to be visible in editor.
    /// </summary>
    private void Reset() {
        nodes.Clear();
        curves.Clear();
        AddNode(new SplineNode() {
            Position = new Vector3(5, 0, 0),
            Direction = new Vector3(5, 0, -3)
        });
        AddNode(new SplineNode() {
            Position = new Vector3(10, 0, 0),
            Direction = new Vector3(10, 0, 3)
        });
    }

    /// <summary>
    /// The length of the spline
    /// </summary>
    public float Length {
        get {
            return length;
        }
        private set {
            length = value;
            if (LengthChanged != null) {
                LengthChanged.Invoke();
            }
        }
    }
    private float length = 0;

    [HideInInspector]
    public UnityEvent NodesChanged = new UnityEvent();

    [HideInInspector]
    public UnityEvent LengthChanged = new UnityEvent();

    private void OnEnable() {
        curves.Clear();
        for (int i = 0; i < nodes.Count - 1; i++) {
            SplineNode n = nodes[i];
            SplineNode next = nodes[i + 1];

            CubicBezierCurve curve = new CubicBezierCurve(n, next);
            curve.Changed.AddListener(() => UpdateLength());
            curves.Add(curve);
        }
        NotifyChange();
        UpdateLength();
    }

    public ReadOnlyCollection<CubicBezierCurve> GetCurves() {
        return curves.AsReadOnly();
    }

    private void NotifyChange() {
        if (NodesChanged != null)
            NodesChanged.Invoke();
    }

    private void UpdateLength() {
        float newLength = 0;
        foreach (var curve in curves) {
            newLength += curve.Length;
        }
        if (newLength != Length) {
            Length = newLength;
        }
    }

    public Vector3 GetLocationAlongSpline(float t)
    {
        if (t < 0 || t > nodes.Count)
            throw new ArgumentException(string.Format("Time must be between 0 and node count ({0}). Given time was {1}.", nodes.Count, t));

        int index =(int)t;
        if (index == nodes.Count)
            index--;
        return curves[index].GetLocation(t - index);
    }

    public Vector3 GetTangentAlongSpline(float t)
    {
        if (t < 0 || t > nodes.Count)
            throw new ArgumentException(string.Format("Time must be between 0 and node count ({0}). Given time was {1}.", nodes.Count, t));

        int index = (int)t;
        if (index == nodes.Count)
            index--;
        return curves[index].GetTangent(t - index);
    }

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

    public void AddNode(SplineNode node)
    {
        nodes.Add(node);
        if (nodes.Count != 1) {
            SplineNode previousNode = nodes[nodes.IndexOf(node)-1];
            CubicBezierCurve curve = new CubicBezierCurve(previousNode, node);
            curve.Changed.AddListener(() => UpdateLength());
            curves.Add(curve);
        }
        NotifyChange();
        UpdateLength();
    }

    public void InsertNode(int index, SplineNode node)
    {
        if (index == 0)
            throw new Exception("Can't insert a node at index 0");

        SplineNode previousNode = nodes[index - 1];
        SplineNode nextNode = nodes[index];

        nodes.Insert(index, node);
        
        curves[index-1].ConnectEnd(node);

        CubicBezierCurve curve = new CubicBezierCurve(node, nextNode);
        curve.Changed.AddListener(() => UpdateLength());
        curves.Insert(index, curve);
        NotifyChange();
        UpdateLength();
    }

    public void RemoveNode(SplineNode node)
    {
        int index = nodes.IndexOf(node);

        if(nodes.Count <= 2) {
            throw new Exception("Can't remove the node because a spline needs at least 2 nodes.");
        }

        if(index != nodes.Count - 1) {
            SplineNode nextNode = nodes[index + 1];
            curves[index - 1].ConnectEnd(nextNode);
        }

        nodes.RemoveAt(index);
        curves[index].Changed.RemoveListener(() => UpdateLength());
        curves.RemoveAt(index);

        NotifyChange();
        UpdateLength();
    }
}
