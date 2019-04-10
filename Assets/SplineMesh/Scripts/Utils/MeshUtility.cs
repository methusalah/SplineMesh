using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SplineMesh {
    public class MeshUtility {

        /// <summary>
        /// Returns a mesh with reserved triangles to turn back the face culling.
        /// This is usefull when a mesh needs to have a negative scale.
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns a mesh similar to the given source plus given optionnal parameters.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="source"></param>
        /// <param name="triangles"></param>
        /// <param name="vertices"></param>
        /// <param name="normals"></param>
        /// <param name="uv"></param>
        /// <param name="uv2"></param>
        /// <param name="uv3"></param>
        /// <param name="uv4"></param>
        /// <param name="uv5"></param>
        /// <param name="uv6"></param>
        /// <param name="uv7"></param>
        /// <param name="uv8"></param>
        public static void Update(Mesh mesh,
            Mesh source,
            IEnumerable<int> triangles = null,
            IEnumerable<Vector3> vertices = null,
            IEnumerable<Vector3> normals = null,
            IEnumerable<Vector2> uv = null,
            IEnumerable<Vector2> uv2 = null,
            IEnumerable<Vector2> uv3 = null,
            IEnumerable<Vector2> uv4 = null,
            IEnumerable<Vector2> uv5 = null,
            IEnumerable<Vector2> uv6 = null,
            IEnumerable<Vector2> uv7 = null,
            IEnumerable<Vector2> uv8 = null) {
            mesh.hideFlags = source.hideFlags;
#if UNITY_2017_3_OR_NEWER
            mesh.indexFormat = source.indexFormat;
#endif

            mesh.triangles = new int[0];
            mesh.vertices = vertices == null ? source.vertices : vertices.ToArray();
            mesh.normals = normals == null ? source.normals : normals.ToArray();
            mesh.uv = uv == null? source.uv : uv.ToArray();
            mesh.uv2 = uv2 == null ? source.uv2 : uv2.ToArray();
            mesh.uv3 = uv3 == null ? source.uv3 : uv3.ToArray();
            mesh.uv4 = uv4 == null ? source.uv4 : uv4.ToArray();
#if UNITY_2018_2_OR_NEWER
            mesh.uv5 = uv5 == null ? source.uv5 : uv5.ToArray();
            mesh.uv6 = uv6 == null ? source.uv6 : uv6.ToArray();
            mesh.uv7 = uv7 == null ? source.uv7 : uv7.ToArray();
            mesh.uv8 = uv8 == null ? source.uv8 : uv8.ToArray();
#endif
            mesh.triangles = triangles == null ? source.triangles : triangles.ToArray();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
        }
    }
}
