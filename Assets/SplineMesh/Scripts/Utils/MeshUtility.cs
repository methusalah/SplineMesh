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
    }
}
