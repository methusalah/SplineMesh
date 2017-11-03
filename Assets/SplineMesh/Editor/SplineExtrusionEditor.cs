using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(SplineExtrusion))]
public class SplineExtrusionEditor : Editor
{
    private const int QUAD_SIZE = 10;
    private Color CURVE_COLOR = new Color(0.8f, 0.8f, 0.8f);

    private SplineExtrusion se;
    private SplineExtrusion.Vertex selectedVertex = null;

    private void OnEnable() {
        se = (SplineExtrusion)target;
    }

    void OnSceneGUI()
    {
        Vector3 tangent = se.spline.GetTangentAlongSpline(0);
        Vector3 location = se.spline.GetLocationAlongSpline(0);
        Quaternion q = CubicBezierCurve.GetRotationFromTangent(tangent);

        foreach (SplineExtrusion.Vertex v in se.ShapeVertices) {
            Vector3 vAtSplineStart = se.transform.TransformPoint(q * v.point + location);
            Vector3 nAtSplineStart = se.transform.TransformPoint(q * (v.point + v.normal) + location);

            if (v == selectedVertex) {
                // draw the handles for selected vertex position and normal
                var savedPoint = v.point;
                var savedNormal = v.normal;

                float size = HandleUtility.GetHandleSize(vAtSplineStart) * 0.3f;
                float snap = 0.1f;

                v.point = Quaternion.Inverse(q) * (se.transform.InverseTransformPoint(Handles.Slider2D(vAtSplineStart, tangent, Vector3.right, Vector3.up, size, Handles.CircleHandleCap, snap)) - location);
                nAtSplineStart = se.transform.TransformPoint(q * (v.point + v.normal) + location);
                v.normal = Quaternion.Inverse(q) * (se.transform.InverseTransformPoint(Handles.Slider2D(nAtSplineStart, tangent, Vector3.right, Vector3.up, size, Handles.CircleHandleCap, snap)) - location) - v.point;

                if (savedNormal != v.normal || savedPoint != v.point) {
                    se.GenerateMesh();
                }

                Handles.BeginGUI();
                DrawQuad(HandleUtility.WorldToGUIPoint(vAtSplineStart), CURVE_COLOR);
                DrawQuad(HandleUtility.WorldToGUIPoint(nAtSplineStart), Color.red);
                Handles.EndGUI();
            } else {
                // we draw a button to allow selection of the vertex
                Handles.BeginGUI();
                Vector2 p = HandleUtility.WorldToGUIPoint(vAtSplineStart);
                if (GUI.Button(new Rect(p - new Vector2(QUAD_SIZE / 2, QUAD_SIZE / 2), new Vector2(QUAD_SIZE, QUAD_SIZE)), GUIContent.none)) {
                    selectedVertex = v;
                }
                Handles.EndGUI();
            }

            // draw an arrow from the vertex location to the normal
            Handles.color = Color.red;
            Handles.DrawLine(vAtSplineStart, nAtSplineStart);

            // draw a line between that vertex and the next one
            int index = se.ShapeVertices.IndexOf(v);
            int nextIndex = index == se.ShapeVertices.Count - 1 ? 0 : index + 1;
            SplineExtrusion.Vertex next = se.ShapeVertices[nextIndex];
            Handles.color = CURVE_COLOR;
            Vector3 vAtSplineEnd = se.transform.TransformPoint(q * next.point + location);
            Handles.DrawLine(vAtSplineStart, vAtSplineEnd);
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
}
