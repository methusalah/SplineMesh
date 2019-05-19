using UnityEngine;
using System.Collections;
using UnityEditor;

namespace SplineMesh {
    [CustomEditor(typeof(ExtrudedShape))]
    public class ExtrudedShapeEditor : Editor {
        private const int QUAD_SIZE = 10;
        private Color CURVE_COLOR = new Color(0.8f, 0.8f, 0.8f);
        private bool mustCreateNewNode = false;
        private SerializedProperty vertices;

        private ExtrudedShape es;
        private ExtrusionSegment.Vertex selection = null;

        private void OnEnable() {
            es = (ExtrudedShape)target;
            vertices = serializedObject.FindProperty("shapeVertices");
        }

        void OnSceneGUI() {
            Event e = Event.current;
            if (e.type == EventType.MouseDown) {
                Undo.RegisterCompleteObjectUndo(es, "change extruded shape");
                // if control key pressed, we will have to create a new vertex if position is changed
                if (e.alt) {
                    mustCreateNewNode = true;
                }
            }
            if (e.type == EventType.MouseUp) {
                mustCreateNewNode = false;
            }
            var spline = es.GetComponent<Spline>();

            CurveSample startSample = spline.GetSample(0);
            Quaternion q = startSample.Rotation;
            foreach (ExtrusionSegment.Vertex v in es.shapeVertices) {
                // we create point and normal relative to the spline start where the shape is drawn
                Vector3 point = es.transform.TransformPoint(q * v.point + startSample.location);
                Vector3 normal = es.transform.TransformPoint(q * (v.point + v.normal) + startSample.location);

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
                        Vector2 newVertexPoint = Quaternion.Inverse(q) * (es.transform.InverseTransformPoint(movedPoint) - startSample.location);
                        if (mustCreateNewNode) {
                            // We must create a new node
                            mustCreateNewNode = false;
                            ExtrusionSegment.Vertex newVertex = new ExtrusionSegment.Vertex(newVertexPoint, v.normal, v.uCoord);
                            int i = es.shapeVertices.IndexOf(v);
                            if (i == es.shapeVertices.Count - 1) {
                                es.shapeVertices.Add(newVertex);
                            } else {
                                es.shapeVertices.Insert(i + 1, newVertex);
                            }
                            selection = newVertex;
                        } else {
                            v.point = newVertexPoint;
                            // normal must be updated if point has been moved
                            normal = es.transform.TransformPoint(q * (v.point + v.normal) + startSample.location);
                        }
                        es.RaiseChanged();
                    } else {
                        // vertex position handle hasn't been moved
                        // create a handle for normal
                        Vector3 movedNormal = Handles.Slider2D(normal, startSample.tangent, Vector3.right, Vector3.up, size, Handles.CircleHandleCap, snap);
                        if (movedNormal != normal) {
                            // normal has been moved
                            v.normal = (Vector2)(Quaternion.Inverse(q) * (es.transform.InverseTransformPoint(movedNormal) - startSample.location)) - v.point;
                            es.RaiseChanged();
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
                        selection = v;
                    }
                    Handles.EndGUI();
                }

                // draw an arrow from the vertex location to the normal
                Handles.color = Color.red;
                Handles.DrawLine(point, normal);

                // draw a line between that vertex and the next one
                int index = es.shapeVertices.IndexOf(v);
                int nextIndex = index == es.shapeVertices.Count - 1 ? 0 : index + 1;
                ExtrusionSegment.Vertex next = es.shapeVertices[nextIndex];
                Handles.color = CURVE_COLOR;
                Vector3 vAtSplineEnd = es.transform.TransformPoint(q * next.point + startSample.location);
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
            if (selection == null || es.shapeVertices.Count <= 3) {
                GUI.enabled = false;
            }
            if (GUILayout.Button("Delete selected vertex")) {
                Undo.RegisterCompleteObjectUndo(es, "delete vertex");
                es.shapeVertices.Remove(selection);
                selection = null;
                es.RaiseChanged();
            }
            GUI.enabled = true;

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
