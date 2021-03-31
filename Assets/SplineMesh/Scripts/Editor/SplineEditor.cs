using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SplineMesh {
    [CustomEditor(typeof(Spline))]
    public class SplineEditor : Editor {

        private const int QUAD_SIZE = 12;
        private static Color CURVE_COLOR = new Color(0.8f, 0.8f, 0.8f);
        private static Color CURVE_BUTTON_COLOR = new Color(0.8f, 0.8f, 0.8f);
        private static Color DIRECTION_COLOR = Color.red;
        private static Color DIRECTION_BUTTON_COLOR = Color.red;
        private static Color UP_BUTTON_COLOR = Color.green;

        private static bool showUpVector = false;

        private enum SelectionType {
            Node,
            Direction,
            InverseDirection,
            Up
        }

        private SplineNode selection;
        private SelectionType selectionType;
        private bool mustCreateNewNode = false;
        private SerializedProperty nodesProp { get { return serializedObject.FindProperty("nodes"); } }
        private Spline spline { get { return (Spline)serializedObject.targetObject; } }

        private GUIStyle nodeButtonStyle, directionButtonStyle, upButtonStyle;

        private void OnEnable() {
            Texture2D t = new Texture2D(1, 1);
            t.SetPixel(0, 0, CURVE_BUTTON_COLOR);
            t.Apply();
            nodeButtonStyle = new GUIStyle();
            nodeButtonStyle.normal.background = t;

            t = new Texture2D(1, 1);
            t.SetPixel(0, 0, DIRECTION_BUTTON_COLOR);
            t.Apply();
            directionButtonStyle = new GUIStyle();
            directionButtonStyle.normal.background = t;

            t = new Texture2D(1, 1);
            t.SetPixel(0, 0, UP_BUTTON_COLOR);
            t.Apply();
            upButtonStyle = new GUIStyle();
            upButtonStyle.normal.background = t;
            selection = null;
			
            Undo.undoRedoPerformed -= spline.RefreshCurves;
            Undo.undoRedoPerformed += spline.RefreshCurves;
        }

        SplineNode AddClonedNode(SplineNode node) {
            int index = spline.nodes.IndexOf(node);
            SplineNode res = new SplineNode(node.Position, node.Direction);
            if (index == spline.nodes.Count - 1) {
                spline.AddNode(res);
            } else {
                spline.InsertNode(index + 1, res);
            }
            return res;
        }

        void OnSceneGUI() {
            // disable game object transform gyzmo
            // if the spline script is active
            if (Selection.activeGameObject == spline.gameObject) {
                if (!spline.enabled) {
                    Tools.current = Tool.Move;
                } else {
                    Tools.current = Tool.None;
                    if (selection == null && spline.nodes.Count > 0)
                        selection = spline.nodes[0];
                }
            }

            // draw a bezier curve for each curve in the spline
            foreach (CubicBezierCurve curve in spline.GetCurves()) {
                Handles.DrawBezier(spline.transform.TransformPoint(curve.n1.Position),
                    spline.transform.TransformPoint(curve.n2.Position),
                    spline.transform.TransformPoint(curve.n1.Direction),
                    spline.transform.TransformPoint(curve.GetInverseDirection()),
                    CURVE_COLOR,
                    null,
                    3);
            }

            if (!spline.enabled)
                return;

            // draw the selection handles
            switch (selectionType) {
                case SelectionType.Node:
                    // place a handle on the node and manage position change

                    // TODO place the handle depending on user params (local or world)
                    Vector3 newPosition = spline.transform.InverseTransformPoint(Handles.PositionHandle(spline.transform.TransformPoint(selection.Position), spline.transform.rotation));
                    if (newPosition != selection.Position) {
                        // position handle has been moved
                        if (mustCreateNewNode) {
                            mustCreateNewNode = false;
                            selection = AddClonedNode(selection);
                            selection.Direction += newPosition - selection.Position;
                            selection.Position = newPosition;
                        } else {
                            selection.Direction += newPosition - selection.Position;
                            selection.Position = newPosition;
                        }
                    }
                    break;
                case SelectionType.Direction:
                    var result = Handles.PositionHandle(spline.transform.TransformPoint(selection.Direction), Quaternion.identity);
                    selection.Direction = spline.transform.InverseTransformPoint(result);
                    break;
                case SelectionType.InverseDirection:
                    result = Handles.PositionHandle(2 * spline.transform.TransformPoint(selection.Position) - spline.transform.TransformPoint(selection.Direction), Quaternion.identity);
                    selection.Direction = 2 * selection.Position - spline.transform.InverseTransformPoint(result);
                    break;
                case SelectionType.Up:
                    result = Handles.PositionHandle(spline.transform.TransformPoint(selection.Position + selection.Up), Quaternion.LookRotation(selection.Direction - selection.Position));
                    selection.Up = (spline.transform.InverseTransformPoint(result) - selection.Position).normalized;
                    break;
            }

            // draw the handles of all nodes, and manage selection motion
            Handles.BeginGUI();
            foreach (SplineNode n in spline.nodes) {
                var dir = spline.transform.TransformPoint(n.Direction);
                var pos = spline.transform.TransformPoint(n.Position);
                var invDir = spline.transform.TransformPoint(2 * n.Position - n.Direction);
                var up = spline.transform.TransformPoint(n.Position + n.Up);
                // first we check if at least one thing is in the camera field of view
                if (!(CameraUtility.IsOnScreen(pos) ||
                    CameraUtility.IsOnScreen(dir) ||
                    CameraUtility.IsOnScreen(invDir) ||
                    (showUpVector && CameraUtility.IsOnScreen(up)))) {
                    continue;
                }

                Vector3 guiPos = HandleUtility.WorldToGUIPoint(pos);
                if (n == selection) {
                    Vector3 guiDir = HandleUtility.WorldToGUIPoint(dir);
                    Vector3 guiInvDir = HandleUtility.WorldToGUIPoint(invDir);
                    Vector3 guiUp = HandleUtility.WorldToGUIPoint(up);

                    // for the selected node, we also draw a line and place two buttons for directions
                    Handles.color = DIRECTION_COLOR;
                    Handles.DrawLine(guiDir, guiInvDir);

                    // draw quads direction and inverse direction if they are not selected
                    if (selectionType != SelectionType.Node) {
                        if (Button(guiPos, directionButtonStyle)) {
                            selectionType = SelectionType.Node;
                        }
                    }
                    if (selectionType != SelectionType.Direction) {
                        if (Button(guiDir, directionButtonStyle)) {
                            selectionType = SelectionType.Direction;
                        }
                    }
                    if (selectionType != SelectionType.InverseDirection) {
                        if (Button(guiInvDir, directionButtonStyle)) {
                            selectionType = SelectionType.InverseDirection;
                        }
                    }
                    if (showUpVector) {
                        Handles.color = Color.green;
                        Handles.DrawLine(guiPos, guiUp);
                        if (selectionType != SelectionType.Up) {
                            if (Button(guiUp, upButtonStyle)) {
                                selectionType = SelectionType.Up;
                            }
                        }
                    }
                } else {
                    if (Button(guiPos, nodeButtonStyle)) {
                        selection = n;
                        selectionType = SelectionType.Node;
                    }
                }
            }
            Handles.EndGUI();

            if (GUI.changed)
                EditorUtility.SetDirty(target);
        }

        bool Button(Vector2 position, GUIStyle style) {
            return GUI.Button(new Rect(position - new Vector2(QUAD_SIZE / 2, QUAD_SIZE / 2), new Vector2(QUAD_SIZE, QUAD_SIZE)), GUIContent.none, style);
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            if(spline.nodes.IndexOf(selection) < 0) {
                selection = null;
            }

            // add button
            if (selection == null) {
                GUI.enabled = false;
            }
            if (GUILayout.Button("Add node after selected")) {
                Undo.RecordObject(spline, "add spline node");
                SplineNode newNode = new SplineNode(selection.Direction, selection.Direction + selection.Direction - selection.Position);
                var index = spline.nodes.IndexOf(selection);
                if(index == spline.nodes.Count - 1) {
                    spline.AddNode(newNode);
                } else {
                    spline.InsertNode(index + 1, newNode);
                }
                selection = newNode;
                serializedObject.Update();
            }
            GUI.enabled = true;

            // delete button
            if (selection == null || spline.nodes.Count <= 2) {
                GUI.enabled = false;
            }
            if (GUILayout.Button("Delete selected node")) {
                Undo.RecordObject(spline, "delete spline node");
                spline.RemoveNode(selection);
                selection = null;
                serializedObject.Update();
            }
            GUI.enabled = true;

            showUpVector = GUILayout.Toggle(showUpVector, "Show up vector");
            spline.IsLoop = GUILayout.Toggle(spline.IsLoop, "Is loop");

            // nodes
            GUI.enabled = false;
            EditorGUILayout.PropertyField(nodesProp);
            GUI.enabled = true;

            if (selection != null) {
                int index = spline.nodes.IndexOf(selection);
                SerializedProperty nodeProp = nodesProp.GetArrayElementAtIndex(index);

                EditorGUILayout.LabelField("Selected node (node " + index + ")");

                EditorGUI.indentLevel++;
                DrawNodeData(nodeProp, selection);
                EditorGUI.indentLevel--;
            } else {
                EditorGUILayout.LabelField("No selected node");
            }
        }

        private void DrawNodeData(SerializedProperty nodeProperty, SplineNode node) {
            var positionProp = nodeProperty.FindPropertyRelative("position");
            var directionProp = nodeProperty.FindPropertyRelative("direction");
            var upProp = nodeProperty.FindPropertyRelative("up");
            var scaleProp = nodeProperty.FindPropertyRelative("scale");
            var rollProp = nodeProperty.FindPropertyRelative("roll");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(positionProp, new GUIContent("Position"));
            EditorGUILayout.PropertyField(directionProp, new GUIContent("Direction"));
            EditorGUILayout.PropertyField(upProp, new GUIContent("Up"));
            EditorGUILayout.PropertyField(scaleProp, new GUIContent("Scale"));
            EditorGUILayout.PropertyField(rollProp, new GUIContent("Roll"));

            if (EditorGUI.EndChangeCheck()) {
                node.Position = positionProp.vector3Value;
                node.Direction = directionProp.vector3Value;
                node.Up = upProp.vector3Value;
                node.Scale = scaleProp.vector2Value;
                node.Roll = rollProp.floatValue;
                serializedObject.Update();
            }
        }

        [MenuItem("GameObject/3D Object/Spline")]
        public static void CreateSpline() {
            new GameObject("Spline", typeof(Spline));
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        static void DisplayUnselected(Spline spline, GizmoType gizmoType) {
            foreach (CubicBezierCurve curve in spline.GetCurves()) {
                Handles.DrawBezier(spline.transform.TransformPoint(curve.n1.Position),
                    spline.transform.TransformPoint(curve.n2.Position),
                    spline.transform.TransformPoint(curve.n1.Direction),
                    spline.transform.TransformPoint(curve.GetInverseDirection()),
                    CURVE_COLOR,
                    null,
                    3);
            }
        }
    }
}
