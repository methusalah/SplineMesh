using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SplineMesh {
    public class ExtrudedHalfProfile : MonoBehaviour {
        public event EventHandler Changed;
        public List<ExtrusionSegment.Vertex> shapeVertices = new List<ExtrusionSegment.Vertex>();

        public void RaiseChanged() {
            Changed?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Clear shape vertices, then create three vertices with three normals for the extrusion to be visible
        /// </summary>
        private void Reset() {
            shapeVertices.Clear();
            shapeVertices.Add(new ExtrusionSegment.Vertex(new Vector2(0, 0.5f), new Vector2(0, 1), 0));
            shapeVertices.Add(new ExtrusionSegment.Vertex(new Vector2(1, -0.5f), new Vector2(1, -1), 0.33f));
            shapeVertices.Add(new ExtrusionSegment.Vertex(new Vector2(-1, -0.5f), new Vector2(-1, -1), 0.66f));
        }

        public List<ExtrusionSegment.Vertex> GetHalProfileWithBase() {
            var res = new List<ExtrusionSegment.Vertex>();
            // adding the first base
            var last = new ExtrusionSegment.Vertex(shapeVertices.Last());
            last.normal = Vector2.up;
            res.Add(last);

            foreach (var v in shapeVertices) {
                res.Add(v);
            }

            // adding the second base
            var first = new ExtrusionSegment.Vertex(shapeVertices.First());
            first.normal = Vector2.down;
            res.Add(first);

            return res;
        }

        public List<ExtrusionSegment.Vertex> GetProfile() {
            var res = new List<ExtrusionSegment.Vertex>();
            foreach(var v in shapeVertices) {
                res.Add(v);
            }
            // adding the first base
            var last = new ExtrusionSegment.Vertex(shapeVertices.Last());
            last.normal = Vector2.up;
            res.Add(last);
            res.Add(GetMirrored(last));

            // adding the inversed mirrored side
            var reversed = shapeVertices.ToList();
            reversed.Reverse();
            foreach (var v in reversed) {
                res.Add(GetMirrored(v));
            }

            // adding the second base
            var first = new ExtrusionSegment.Vertex(shapeVertices.First());
            first.normal = Vector2.down;
            res.Add(GetMirrored(first));
            res.Add(first);

            return res;
        }

        public ExtrusionSegment.Vertex GetMirrored(ExtrusionSegment.Vertex vertex) {
            var res = new ExtrusionSegment.Vertex(
                vertex.point,
                vertex.normal,
                vertex.uCoord);
            res.point.x = -res.point.x;
            res.normal.x = -res.normal.x;
            return res;
        }
    }
}
