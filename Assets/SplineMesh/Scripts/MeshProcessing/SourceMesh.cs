using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace SplineMesh {
    /// <summary>
    /// This class returns a transformed version of a given source mesh, plus others
    /// informations to help bending the mesh along a curve.
    /// It is imutable to ensure better performances.
    /// 
    /// To obtain an instance, call the static method <see cref="Build(Mesh)"/>.
    /// The building is made in a fluent way.
    /// </summary>
    public struct SourceMesh {
        private Vector3 translation;
        private Quaternion rotation;
        private Vector3 scale;

        internal Mesh Mesh { get; }

        private List<MeshVertex> vertices;
        internal List<MeshVertex> Vertices {
            get {
                if (vertices == null) BuildData();
                return vertices;
            }
        }

        private int[] triangles;
        internal int[] Triangles {
            get {
                if (vertices == null) BuildData();
                return triangles;
            }
        }

        private float minX;
        internal float MinX {
            get {
                if (vertices == null) BuildData();
                return minX;
            }
        }

        private float length;
        internal float Length {
            get {
                if (vertices == null) BuildData();
                return length;
            }
        }

        /// <summary>
        /// constructor is private to enable fluent builder pattern.
        /// Use <see cref="Build(Mesh)"/> to obtain an instance.
        /// </summary>
        /// <param name="mesh"></param>
        private SourceMesh(Mesh mesh) {
            Mesh = mesh;
            translation = default(Vector3);
            rotation = default(Quaternion);
            scale = default(Vector3);
            vertices = null;
            triangles = null;
            minX = 0;
            length = 0;
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="other"></param>
        private SourceMesh(SourceMesh other) {
            Mesh = other.Mesh;
            translation = other.translation;
            rotation = other.rotation;
            scale = other.scale;
            vertices = null;
            triangles = null;
            minX = 0;
            length = 0;
        }

        public static SourceMesh Build(Mesh mesh) {
            return new SourceMesh(mesh);
        }

        public SourceMesh Translate(Vector3 translation) {
            var res = new SourceMesh(this) {
                translation = translation
            };
            return res;
        }

        public SourceMesh Translate(float x, float y, float z) {
            return Translate(new Vector3(x, y, z));
        }

        public SourceMesh Rotate(Quaternion rotation) {
            var res = new SourceMesh(this) {
                rotation = rotation
            };
            return res;
        }

        public SourceMesh Scale(Vector3 scale) {
            var res = new SourceMesh(this) {
                scale = scale
            };
            return res;
        }

        public SourceMesh Scale(float x, float y, float z) {
            return Scale(new Vector3(x, y, z));
        }

        private void BuildData() {
            // if the mesh is reversed by scale, we must change the culling of the faces by inversing all triangles.
            // the mesh is reverse only if the number of resersing axes is impair.
            bool reversed = scale.x < 0;
            if (scale.y < 0) reversed = !reversed;
            if (scale.z < 0) reversed = !reversed;
            triangles = reversed ? MeshUtility.GetReversedTriangles(Mesh) : Mesh.triangles;

            // we transform the source mesh vertices according to rotation/translation/scale
            int i = 0;
            vertices = new List<MeshVertex>(Mesh.vertexCount);
            foreach (Vector3 vert in Mesh.vertices) {
                var transformed = new MeshVertex(vert, Mesh.normals[i++]);
                //  application of rotation
                if (rotation != Quaternion.identity) {
                    transformed.position = rotation * transformed.position;
                    transformed.normal = rotation * transformed.normal;
                }
                if (scale != Vector3.one) {
                    transformed.position = Vector3.Scale(transformed.position, scale);
                    transformed.normal = Vector3.Scale(transformed.normal, scale);
                }
                if (translation != Vector3.zero) {
                    transformed.position += translation;
                }
                vertices.Add(transformed);
            }

            // find the bounds along x
            minX = float.MaxValue;
            float maxX = float.MinValue;
            foreach (var vert in vertices) {
                Vector3 p = vert.position;
                maxX = Math.Max(maxX, p.x);
                minX = Math.Min(minX, p.x);
            }
            length = Math.Abs(maxX - minX);
        }

        public override bool Equals(object obj) {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }
            var other = (SourceMesh)obj;
            return Mesh == other.Mesh &&
                translation == other.translation &&
                rotation == other.rotation &&
                scale == other.scale;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public static bool operator ==(SourceMesh sm1, SourceMesh sm2) {
            return sm1.Equals(sm2);
        }
        public static bool operator !=(SourceMesh sm1, SourceMesh sm2) {
            return sm1.Equals(sm2);
        }
    }
}
