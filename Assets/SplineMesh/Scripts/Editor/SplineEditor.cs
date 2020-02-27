using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SplineMesh {
    [CustomEditor(typeof(Spline))]
    public class SplineEditor : Editor {

        private const int QUAD_SIZE = 12;
        private Color CURVE_COLOR = new Color(0.8f, 0.8f, 0.8f);
        private Color CURVE_BUTTON_COLOR = new Color(0.8f, 0.8f, 0.8f);
        private Color DIRECTION_COLOR = Color.red;
        private Color DIRECTION_BUTTON_COLOR = Color.red;
        private Color UP_BUTTON_COLOR = Color.green;

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
            Event e = Event.current;
            if (e.type == EventType.MouseDown) {
                Undo.RegisterCompleteObjectUndo(spline, "change spline topography");
                // if alt key pressed, we will have to create a new node if node position is changed
                if (e.alt) {
                    mustCreateNewNode = true;
                }
            }
            if (e.type == EventType.MouseUp) {
                mustCreateNewNode = false;
            }

            // disable game object transform gyzmo
            if (Selection.activeGameObject == spline.gameObject) {
                Tools.current = Tool.None;
                if (selection == null && spline.nodes.Count > 0)
                    selection = spline.nodes[0];
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

            // draw the selection handles
            switch (selectionType) {
                case SelectionType.Node:
                    // place a handle on the node and manage position change
                    Vector3 newPosition = spline.transform.InverseTransformPoint(Handles.PositionHandle(spline.transform.TransformPoint(selection.Position), Quaternion.identity));
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
            // hint
            EditorGUILayout.HelpBox("Hold Alt and drag a node to create a new one.", MessageType.Info);

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
            spline.IsLoop = GUILayout.Toggle(spline.IsLoop, "Is loop (experimental)");

            // nodes
            EditorGUILayout.PropertyField(nodesProp);
            EditorGUI.indentLevel++;
            if (nodesProp.isExpanded) {
                for (int i = 0; i < nodesProp.arraySize; i++) {
                    SerializedProperty nodeProp = nodesProp.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(nodeProp);
                    EditorGUI.indentLevel++;
                    if (nodeProp.isExpanded) {
                        drawNodeData(nodeProp, spline.nodes[i]);
                    }
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.indentLevel--;

            if (selection != null) {
                int index = spline.nodes.IndexOf(selection);
                SerializedProperty nodeProp = nodesProp.GetArrayElementAtIndex(index);
                EditorGUILayout.LabelField("Selected node (node " + index + ")");
                EditorGUI.indentLevel++;
                drawNodeData(nodeProp, selection);
                EditorGUI.indentLevel--;
            } else {
                EditorGUILayout.LabelField("No selected node");
            }
        }

        private void drawNodeData(SerializedProperty nodeProperty, SplineNode node) {
            using (var check = new EditorGUI.ChangeCheckScope()) {
                var positionProp = nodeProperty.FindPropertyRelative("position");
                EditorGUILayout.PropertyField(positionProp, new GUIContent("Position"));
                if (check.changed) {
                    node.Position = positionProp.vector3Value;
                }
            }

            using (var check = new EditorGUI.ChangeCheckScope()) {
                var directionProp = nodeProperty.FindPropertyRelative("direction");
                EditorGUILayout.PropertyField(directionProp, new GUIContent("Direction"));
                if (check.changed) {
                    node.Direction = directionProp.vector3Value;
                }
            }

            using (var check = new EditorGUI.ChangeCheckScope()) {
                var upProp = nodeProperty.FindPropertyRelative("up");
                EditorGUILayout.PropertyField(upProp, new GUIContent("Up"));
                if (check.changed) {
                    node.Up = upProp.vector3Value;
                }
            }

            using (var check = new EditorGUI.ChangeCheckScope()) {
                var scaleProp = nodeProperty.FindPropertyRelative("scale");
                EditorGUILayout.PropertyField(scaleProp, new GUIContent("Scale"));
                if (check.changed) {
                    node.Scale = scaleProp.vector2Value;
                }
            }

            using (var check = new EditorGUI.ChangeCheckScope()) {
                var rollProp = nodeProperty.FindPropertyRelative("roll");
                EditorGUILayout.PropertyField(rollProp, new GUIContent("Roll"));
                if (check.changed) {
                    node.Roll = rollProp.floatValue;
                }
            }
        }

        [MenuItem("GameObject/3D Object/Spline")]
        public static void CreateSpline() {
            new GameObject("Spline", typeof(Spline));
        }
    }
}
