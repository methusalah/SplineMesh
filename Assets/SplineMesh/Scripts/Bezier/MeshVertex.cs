using UnityEngine;
using UnityEditor;
using System;

namespace SplineMesh {
    [Serializable]
    public class MeshVertex {
        public Vector3 position;
        public Vector3 normal;

        public MeshVertex(Vector2 position, Vector2 normal) {
            this.position = position;
            this.normal = normal;
        }
    }
}