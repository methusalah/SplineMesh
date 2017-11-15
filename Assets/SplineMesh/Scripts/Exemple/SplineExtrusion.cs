using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Special component to extrude shape along a spline.
/// 
/// Note : This component is not lightweight and should be used as-is mostly for prototyping. It allows to quickly create meshes by
/// drawing only the 2D shape to extrude along the spline. The result is not intended to be used in a production context and you will most likely
/// create eventuelly the mesh you need in a modeling tool to save performances and have better control.
/// 
/// The special editor of this component allow you to draw a 2D shape with vertices, normals and U texture coordinate. The V coordinate is set
/// for the whole spline, by setting the number of times the texture must be repeated.
/// 
/// All faces of the resulting mesh are smoothed. If you want to obtain an edge without smoothing, you will have to overlap two vertices and set two normals.
/// 
/// You can expand the vertices list in the inspector to access data and enter precise values.
/// 
/// This component doesn't offer much control as Unity is not a modeling tool. That said, you should be able to create your own version easily.
/// /// </summary>
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

    /// <summary>
    /// Clear shape vertices, then create three vertices with three normals for the extrusion to be visible
    /// </summary>
    private void Reset() {
        ShapeVertices.Clear();
        ShapeVertices.Add(new Vertex(new Vector2(0, 0.5f), new Vector2(0, 1), 0));
        ShapeVertices.Add(new Vertex(new Vector2(1, -0.5f), new Vector2(1, -1), 0.33f));
        ShapeVertices.Add(new Vertex(new Vector2(-1, -0.5f), new Vector2(-1, -1), 0.66f));
        toUpdate = true;
        OnEnable();
    }

    private void OnValidate() {
        toUpdate = true;
    }

    private void OnEnable() {
        mf = GetComponent<MeshFilter>();
        spline = GetComponent<Spline>();
        if (mf.sharedMesh == null) {
            mf.sharedMesh = new Mesh();
        }
        spline.NodeCountChanged.AddListener(() => toUpdate = true);
        spline.CurveChanged.AddListener(() => toUpdate = true);
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

    public void GenerateMesh() {
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
        public Vector2 point;
        public Vector2 normal;
        public float uCoord;

        public Vertex(Vector2 point, Vector2 normal, float uCoord)
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
