using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SplineMesh {
    public class ExtrudedShape : MonoBehaviour {
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
    }
}
