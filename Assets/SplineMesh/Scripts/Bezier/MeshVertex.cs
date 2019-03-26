using UnityEngine;
using UnityEditor;
using System;

namespace SplineMesh {
    [Serializable]
    public class MeshVertex {
        public Vector3 position;
        public Vector3 normal;

        public MeshVertex(Vector3 position, Vector3 normal) {
            this.position = position;
            this.normal = normal;
        }
    }
}