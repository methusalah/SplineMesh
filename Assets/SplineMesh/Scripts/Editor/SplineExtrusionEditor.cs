using UnityEngine;
using System.Collections;
using UnityEditor;

namespace SplineMesh {
    [CustomEditor(typeof(SplineExtrusion))]
    public class SplineExtrusionEditor : Editor {
        private const int QUAD_SIZE = 10;
        private Color CURVE_COLOR = new Color(0.8f, 0.8f, 0.8f);
        private bool mustCreateNewNode = false;
        private SerializedProperty textureScale;
        private SerializedProperty sampleSpacing;
        private SerializedProperty material;
        private SerializedProperty vertices;

        private SplineExtrusion se;
        private ExtrusionSegment.Vertex selection = null;
        private int selectionIndex = -1;

        private void OnEnable() {
            se = (SplineExtrusion)target;
            textureScale = serializedObject.FindProperty("textureScale");
            sampleSpacing = serializedObject.FindProperty("sampleSpacing");
            material = serializedObject.FindProperty("material");
            vertices = serializedObject.FindProperty("shapeVertices");

            if (se.shapeVertices.Count > 0) {
                if (se.selectedVertexIndex < 0 || se.selectedVertexIndex > se.shapeVertices.Count - 1) {
                    se.selectedVertexIndex = 0;
                }
                selection = se.shapeVertices[se.selectedVertexIndex];
                selectionIndex = se.selectedVertexIndex;
            } else {
                selection = null;
                selectionIndex = -1;
            }
        }

        void OnSceneGUI() {
            if (se.shapeVertices.Count > 0 && (selection == null || selectionIndex != se.selectedVertexIndex)) {
                if (se.selectedVertexIndex < 0 || se.selectedVertexIndex > se.shapeVertices.Count - 1) {
                    se.selectedVertexIndex = 0;
                }
                selection = se.shapeVertices[se.selectedVertexIndex];
                selectionIndex = se.selectedVertexIndex;
            }

            Event e = Event.current;
            if (e.type == EventType.MouseDown) {
                // if alt key pressed, we will have to create a new vertex if position is changed
                if (e.alt) {
                    mustCreateNewNode = true;
                }
            }
            if (e.type == EventType.MouseUp) {
                mustCreateNewNode = false;
            }
            var spline = se.GetComponent<Spline>();

            CurveSample startSample = spline.GetSample(0);
            Quaternion q = startSample.Rotation;
            int vIdx = -1;
            foreach (ExtrusionSegment.Vertex v in se.shapeVertices) {
                ++vIdx;
                // we create point and normal relative to the spline start where the shape is drawn
                Vector3 point = se.transform.TransformPoint(q * v.point + startSample.location);
                Vector3 normal = se.transform.TransformPoint(q * (v.point + v.normal) + startSample.location);

                // first we check if at least one thing is in the camera field of view
                if (!CameraUtility.IsOnScreen(point) && !CameraUtility.IsOnScreen(normal)) continue;

                if (v == selection) {
                    // draw the handles for selected vertex position and normal
                    float size = HandleUtility.GetHandleSize(point) * 0.3f;
                    float snap = 0.1f;

                    // create a handle for the vertex position
                    Vector3 movedPoint = Handles.Slider2D(0, point, startSample.tangent, Vector3.right, Vector3.up, size, Handles.CircleHandleCap, new Vector2(snap, snap));
                    if (movedPoint != point) {
                        // position has been moved
                        Vector2 newVertexPoint = Quaternion.Inverse(q) * (se.transform.InverseTransformPoint(movedPoint) - startSample.location);
                        if (mustCreateNewNode) {
                            Undo.RecordObject(se, $"Created new vertex {vIdx+1} in {se.name}");

                            // We must create a new node
                            mustCreateNewNode = false;
                            ExtrusionSegment.Vertex newVertex = new ExtrusionSegment.Vertex(newVertexPoint, v.normal, v.uCoord);
                            if (vIdx == se.shapeVertices.Count - 1) {
                                se.shapeVertices.Add(newVertex);
                            } else {
                                se.shapeVertices.Insert(vIdx + 1, newVertex);
                            }
                            selection = newVertex;
                            se.selectedVertexIndex = vIdx + 1;
                            selectionIndex = vIdx + 1;
                        } else {
                            Undo.RecordObject(se, $"Moved vertex {vIdx} in {se.name}");

                            v.point = newVertexPoint;
                            // normal must be updated if point has been moved
                            normal = se.transform.TransformPoint(q * (v.point + v.normal) + startSample.location);
                        }
                        se.SetToUpdate();
                    } else {
                        // vertex position handle hasn't been moved
                        // create a handle for normal
                        Vector3 movedNormal = Handles.Slider2D(normal, startSample.tangent, Vector3.right, Vector3.up, size, Handles.CircleHandleCap, snap);
                        if (movedNormal != normal) {
                            Undo.RecordObject(se, $"Moved normal of vertex {vIdx} in {se.name}");

                            // normal has been moved
                            v.normal = (Vector2)(Quaternion.Inverse(q) * (se.transform.InverseTransformPoint(movedNormal) - startSample.location)) - v.point;
                            se.SetToUpdate();
                        }
                    }

                    Handles.BeginGUI();
                    DrawQuad(HandleUtility.WorldToGUIPoint(point), CURVE_COLOR);
                    DrawQuad(HandleUtility.WorldToGUIPoint(normal), Color.red);
                    Handles.EndGUI();
                } else {
                    // we draw a button to allow selection of the vertex
                    Handles.BeginGUI();
                    Vector2 p = HandleUtility.WorldToGUIPoint(point);
                    if (GUI.Button(new Rect(p - new Vector2(QUAD_SIZE / 2, QUAD_SIZE / 2), new Vector2(QUAD_SIZE, QUAD_SIZE)), GUIContent.none)) {
                        Undo.RecordObject(se, $"Selected vertex {vIdx} of {se.name}");
                        selection = v;
                        se.selectedVertexIndex = vIdx;
                        selectionIndex = vIdx;
                    }
                    Handles.EndGUI();
                }

                // draw an arrow from the vertex location to the normal
                Handles.color = Color.red;
                Handles.DrawLine(point, normal);

                // draw a line between that vertex and the next one
                int index = se.shapeVertices.IndexOf(v);
                int nextIndex = index == se.shapeVertices.Count - 1 ? 0 : index + 1;
                ExtrusionSegment.Vertex next = se.shapeVertices[nextIndex];
                Handles.color = CURVE_COLOR;
                Vector3 vAtSplineEnd = se.transform.TransformPoint(q * next.point + startSample.location);
                Handles.DrawLine(point, vAtSplineEnd);
            }
        }

        void DrawQuad(Rect rect, Color color) {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            GUI.skin.box.normal.background = texture;
            GUI.Box(rect, GUIContent.none);
        }

        void DrawQuad(Vector2 position, Color color) {
            DrawQuad(new Rect(position - new Vector2(QUAD_SIZE / 2, QUAD_SIZE / 2), new Vector2(QUAD_SIZE, QUAD_SIZE)), color);
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            // Add vertex hint
            EditorGUILayout.HelpBox("Hold Alt and drag a vertex to create a new one.", MessageType.Info);

            // Delete vertex button
            if (selection == null || se.shapeVertices.Count <= 3) {
                GUI.enabled = false;
            }
            if (GUILayout.Button("Delete selected vertex")) {
                Undo.RegisterCompleteObjectUndo(se, "delete vertex");
                se.shapeVertices.Remove(selection);
                selection = null;
                se.selectedVertexIndex = -1;
                selectionIndex = -1;
                se.SetToUpdate();
            }
            GUI.enabled = true;

            // Properties
            EditorGUILayout.PropertyField(textureScale, true);
            EditorGUILayout.PropertyField(sampleSpacing, true);
            EditorGUILayout.PropertyField(material, true);

            EditorGUILayout.PropertyField(vertices);
            EditorGUI.indentLevel += 1;
            if (vertices.isExpanded) {
                for (int i = 0; i < vertices.arraySize; i++) {
                    EditorGUILayout.PropertyField(vertices.GetArrayElementAtIndex(i), new GUIContent("Vertex " + i), true);
                }
            }
            EditorGUI.indentLevel -= 1;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
