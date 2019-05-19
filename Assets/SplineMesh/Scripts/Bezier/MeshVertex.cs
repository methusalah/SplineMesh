using UnityEngine;
using UnityEditor;
using System;

namespace SplineMesh {
    [Serializable]
    public class MeshVertex {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;

        public MeshVertex(Vector3 position, Vector3 normal, Vector2 uv) {
            this.position = position;
            this.normal = normal;
            this.uv = uv;
        }

        public MeshVertex(Vector3 position, Vector3 normal)
            : this(position, normal, Vector2.zero)
        {
        }
    }
}