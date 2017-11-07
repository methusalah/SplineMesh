using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(SplineExtrusion))]
public class SplineExtrusionEditor : Editor
{
    private const int QUAD_SIZE = 10;
    private Color CURVE_COLOR = new Color(0.8f, 0.8f, 0.8f);
    private bool mustCreateNewNode = false;
    private SerializedProperty textureScale;
    private SerializedProperty vertices;

    private SplineExtrusion se;
    private SplineExtrusion.Vertex selectedVertex = null;

    private void OnEnable() {
        se = (SplineExtrusion)target;
        textureScale = serializedObject.FindProperty("TextureScale");
        vertices = serializedObject.FindProperty("ShapeVertices");
    }

    void OnSceneGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.mouseDown) {
            Undo.RegisterCompleteObjectUndo(se, "change extruded shape");
            // if control key pressed, we will have to create a new vertex if position is changed
            if (e.alt) {
                mustCreateNewNode = true;
            }
        }
        if (e.type == EventType.mouseUp) {
            mustCreateNewNode = false;
        }

        // listen for delete key
        if (e.type == EventType.keyDown && e.keyCode == KeyCode.Delete) {
            Undo.RegisterCompleteObjectUndo(se, "delete spline node");
            se.ShapeVertices.Remove(selectedVertex);
            selectedVertex = null;
            se.GenerateMesh();
            e.Use();
        }


        Vector3 splineStartTangent = se.spline.GetTangentAlongSpline(0);
        Vector3 splineStart = se.spline.GetLocationAlongSpline(0);
        Quaternion q = CubicBezierCurve.GetRotationFromTangent(splineStartTangent);

        foreach (SplineExtrusion.Vertex v in se.ShapeVertices) {
            // we create point and normal relative to the spline start where the shape is drawn
            Vector3 point = se.transform.TransformPoint(q * v.point + splineStart);
            Vector3 normal = se.transform.TransformPoint(q * (v.point + v.normal) + splineStart);

            if (v == selectedVertex) {
                // draw the handles for selected vertex position and normal
                float size = HandleUtility.GetHandleSize(point) * 0.3f;
                float snap = 0.1f;

                // create a handle for the vertex position
                Vector3 movedPoint = Handles.Slider2D(point, splineStartTangent, Vector3.right, Vector3.up, size, Handles.CircleHandleCap, snap);
                if(movedPoint != point) {
                    // position has been moved
                    Vector2 newVertexPoint = Quaternion.Inverse(q) * (se.transform.InverseTransformPoint(movedPoint) - splineStart);
                    if (mustCreateNewNode) {
                        // We must create a new node
                        mustCreateNewNode = false;
                        SplineExtrusion.Vertex newVertex = new SplineExtrusion.Vertex(newVertexPoint, v.normal, v.uCoord);
                        int i = se.ShapeVertices.IndexOf(v);
                        if(i == se.ShapeVertices.Count - 1) {
                            se.ShapeVertices.Add(newVertex);
                        } else {
                            se.ShapeVertices.Insert(i + 1, newVertex);
                        }
                        selectedVertex = newVertex;
                    } else {
                        v.point = newVertexPoint;
                        // normal must be updated if point has been moved
                        normal = se.transform.TransformPoint(q * (v.point + v.normal) + splineStart);
                    }
                    se.GenerateMesh();
                } else {
                    // vertex position handle hasn't been moved
                    // create a handle for normal
                    Vector3 movedNormal = Handles.Slider2D(normal, splineStartTangent, Vector3.right, Vector3.up, size, Handles.CircleHandleCap, snap);
                    if(movedNormal != normal) {
                        // normal has been moved
                        v.normal = (Vector2)(Quaternion.Inverse(q) * (se.transform.InverseTransformPoint(movedNormal) - splineStart)) - v.point;
                        se.GenerateMesh();
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
                    selectedVertex = v;
                }
                Handles.EndGUI();
            }

            // draw an arrow from the vertex location to the normal
            Handles.color = Color.red;
            Handles.DrawLine(point, normal);

            // draw a line between that vertex and the next one
            int index = se.ShapeVertices.IndexOf(v);
            int nextIndex = index == se.ShapeVertices.Count - 1 ? 0 : index + 1;
            SplineExtrusion.Vertex next = se.ShapeVertices[nextIndex];
            Handles.color = CURVE_COLOR;
            Vector3 vAtSplineEnd = se.transform.TransformPoint(q * next.point + splineStart);
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
        EditorGUILayout.HelpBox("Hold Alt and drag a vertex to create a new one.\nPress del to delete selected vertex.", MessageType.Info);
        EditorGUILayout.PropertyField(textureScale, true);
        EditorGUILayout.PropertyField(vertices, true);
        serializedObject.ApplyModifiedProperties();
    }

}
