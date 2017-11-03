using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class MeshBender : MonoBehaviour
{
    private Mesh source, result;
    private readonly List<Vertex> vertices = new List<Vertex>();

    private Quaternion sourceRotation;
    private Vector3 sourceTranslation;

    public CubicBezierCurve curve;
    private float startScale = 1, endScale = 1;
    private float startRoll, endRoll;

    private void OnEnable() {
        result = new Mesh();
        GetComponent<MeshFilter>().sharedMesh = result;
    }

    public void SetCurve(CubicBezierCurve curve, bool update = true)
    {
        if(this.curve != null) {
            this.curve.Changed.RemoveListener(() => Compute());
        }
        this.curve = curve;
        curve.Changed.AddListener(() => Compute());
        if (update) Compute();
    }

    public void SetStartScale(float scale, bool update = true)
    {
        this.startScale = scale;
        if (update) Compute();
    }

    public void SetEndScale(float scale, bool update = true)
    {
        this.endScale = scale;
        if (update) Compute();
    }

    public void SetStartRoll(float roll, bool update = true)
    {
        this.startRoll = roll;
        if (update) Compute();
    }

    public void SetEndRoll(float roll, bool update = true) {
        this.endRoll = roll;
        if (update) Compute();
    }

    public void SetSourceMesh(Mesh mesh, bool update = true) {
        if(source != mesh) {
            this.source = mesh;
            vertices.Clear();
            int i = 0;
            foreach (Vector3 vert in source.vertices) {
                Vertex v = new Vertex();
                v.v = vert;
                v.n = source.normals[i++];
                vertices.Add(v);
            }
        }
        if (update) Compute();

    }

    public void SetRotation(Quaternion rotation, bool update = true) {
        this.sourceRotation = rotation;
        if (update) Compute();
    }

    public void SetTranslation(Vector3 translation, bool update = true) {
        sourceTranslation = translation;
        if (update) Compute();
    }

    private void Compute()
    {
        if (source == null)
            return;
        int nbVert = source.vertices.Length;
        // find the bounds along x
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        foreach (Vertex vert in vertices) {
            Vector3 p = vert.v;
            if (sourceRotation != Quaternion.identity) {
                p = sourceRotation * p;
            }
            if(sourceTranslation != Vector3.zero) {
                p += sourceTranslation;
            }
            maxX = Math.Max(maxX, p.x);
            minX = Math.Min(minX, p.x);
        }
        float length = Math.Abs(maxX - minX);

        List<Vector3> deformedVerts = new List<Vector3>(nbVert);
        List<Vector3> deformedNormals = new List<Vector3>(nbVert);
        // for each mesh vertex, we found its projection on the curve
        foreach (Vertex vert in vertices) {
            Vector3 p = vert.v;
            Vector3 n = vert.n;
            //  application of rotation
            if (sourceRotation != Quaternion.identity) {
                p = sourceRotation * p;
                n = sourceRotation * n;
            }
            if (sourceTranslation != Vector3.zero) {
                p += sourceTranslation;
            }
            float distanceRate = Math.Abs(p.x - minX) / length;

            Vector3 curvePoint = curve.GetLocationAtDistance(curve.Length * distanceRate);
            Vector3 curveTangent = curve.GetTangentAtDistance(curve.Length * distanceRate);
            Quaternion q = CubicBezierCurve.GetRotationFromTangent(curveTangent) * Quaternion.Euler(0, -90, 0);

            // application of scale
            float scaleAtDistance = startScale + (endScale - startScale) * distanceRate;
            p *= scaleAtDistance;

            // application of roll
            float rollAtDistance = startRoll + (endRoll - startRoll) * distanceRate;
            p = Quaternion.AngleAxis(rollAtDistance, Vector3.right) * p;
            n = Quaternion.AngleAxis(rollAtDistance, Vector3.right) * n;

            // reset X value of p
            p = new Vector3(0, p.y, p.z);

            deformedVerts.Add(q * p + curvePoint);
            deformedNormals.Add(q * n);
        }

        result.vertices = deformedVerts.ToArray();
        result.normals = deformedNormals.ToArray();
        result.uv = source.uv;
        result.triangles = source.triangles;
        GetComponent<MeshFilter>().mesh = result;
    }

    private struct Vertex {
        public Vector3 v;
        public Vector3 n;
    }

    private void OnDestroy() {
        curve.Changed.RemoveListener(() => Compute());
    }

}