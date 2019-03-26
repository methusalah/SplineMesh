using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SplineMesh {
    public class MeshUtility {

        public static int[] GetReversedTriangles(Mesh mesh) {
            var res = mesh.triangles.ToArray();
            var triangleCount = res.Length / 3;
            for (var i = 0; i < triangleCount; i++) {
                var tmp = res[i * 3];
                res[i * 3] = res[i * 3 + 1];
                res[i * 3 + 1] = tmp;
            }
            return res;
        }

        public static void Update(Mesh mesh,
            IEnumerable<int> triangles,
            IEnumerable<Vector3> vertices,
            IEnumerable<Vector3> normals,
            IEnumerable<Vector4> tangents = null,
            IEnumerable<Vector2> uv = null,
            IEnumerable<Vector2> uv2 = null,
            IEnumerable<Vector2> uv3 = null,
            IEnumerable<Vector2> uv4 = null,
            IEnumerable<Vector2> uv5 = null,
            IEnumerable<Vector2> uv6 = null,
            IEnumerable<Vector2> uv7 = null,
            IEnumerable<Vector2> uv8 = null) {
            mesh.triangles = new int[0];
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            if(tangents != null) mesh.tangents = tangents.ToArray();
            if (uv != null) mesh.uv = uv.ToArray();
            if (uv2 != null) mesh.uv2 = uv2.ToArray();
            if (uv3 != null) mesh.uv3 = uv3.ToArray();
            if (uv4 != null) mesh.uv4 = uv4.ToArray();
            if (uv5 != null) mesh.uv5 = uv5.ToArray();
            if (uv6 != null) mesh.uv6 = uv6.ToArray();
            if (uv7 != null) mesh.uv7 = uv7.ToArray();
            if (uv8 != null) mesh.uv8 = uv8.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateBounds();
        }
    }
}
