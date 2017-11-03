using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Spline))]
public class SplineExtrusion : MonoBehaviour {

    private MeshFilter mf;

    public Spline spline;
    public float TextureScale = 1;
    public List<Vertex> ShapeVertices = new List<Vertex>();

    private bool toUpdate = true;

    private void OnEnable() {
        mf = GetComponent<MeshFilter>();
        spline = GetComponent<Spline>();
        if (mf.sharedMesh == null) {
            mf.sharedMesh = new Mesh();
        }
        spline.NodesChanged.AddListener(() => toUpdate = true);
        foreach(CubicBezierCurve curve in spline.GetCurves()) {
            curve.Changed.AddListener(() => toUpdate = true);
        }
    }

    private void Update() {
        if (toUpdate) {
            GenerateMesh();
            toUpdate = false;
        }
    }

    private List<OrientedPoint> GetPath()
    {
        var path = new List<OrientedPoint>();
        for (float t = 0; t < spline.nodes.Count-1; t += 1/10.0f)
        {
            var point = spline.GetLocationAlongSpline(t);
            var rotation = CubicBezierCurve.GetRotationFromTangent(spline.GetTangentAlongSpline(t));
            path.Add(new OrientedPoint(point, rotation));
        }
        return path;
    }

    public void GenerateMesh()
    {
        List<OrientedPoint> path = GetPath();

        int vertsInShape = ShapeVertices.Count;
        int segments = path.Count - 1;
        int edgeLoops = path.Count;
        int vertCount = vertsInShape * edgeLoops;

        var triangleIndices = new List<int>(vertsInShape * 2 * segments * 3);
        var vertices = new Vector3[vertCount];
        var normals = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];

        int index = 0;
        foreach(OrientedPoint op in path) {
            foreach(Vertex v in ShapeVertices) {
                vertices[index] = op.LocalToWorld(v.point);
                normals[index] = op.LocalToWorldDirection(v.normal);
                uvs[index] = new Vector2(v.uCoord, path.IndexOf(op) / ((float)edgeLoops)* TextureScale);
                index++;
            }
        }
        index = 0;
        for (int i = 0; i < segments; i++) {
            for (int j = 0; j < ShapeVertices.Count; j++) {
                int offset = j == ShapeVertices.Count - 1 ? -(ShapeVertices.Count - 1) : 1;
                int a = index + ShapeVertices.Count;
                int b = index;
                int c = index + offset;
                int d = index + offset + ShapeVertices.Count;
                triangleIndices.Add(c);
                triangleIndices.Add(b);
                triangleIndices.Add(a);
                triangleIndices.Add(a);
                triangleIndices.Add(d);
                triangleIndices.Add(c);
                index++;
            }
        }

        mf.sharedMesh.Clear();
        mf.sharedMesh.vertices = vertices;
        mf.sharedMesh.normals = normals;
        mf.sharedMesh.uv = uvs;
        mf.sharedMesh.triangles = triangleIndices.ToArray();
    }

    [Serializable]
    public class Vertex
    {
        public Vector3 point;
        public Vector3 normal;
        public float uCoord;

        public Vertex(Vector3 point, Vector3 normal, float uCoord)
        {
            this.point = point;
            this.normal = normal;
            this.uCoord = uCoord;
        }
    }

    public struct OrientedPoint
    {
        public Vector3 position;
        public Quaternion rotation;

        public OrientedPoint(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }

        public Vector3 LocalToWorld(Vector3 point)
        {
            return position + rotation * point;
        }

        public Vector3 LocalToWorldDirection(Vector3 dir)
        {
            return rotation * dir;
        }
    }
}
