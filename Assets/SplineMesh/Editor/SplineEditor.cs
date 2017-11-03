using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Spline))]
public class SplineEditor : Editor {

    private const int QUAD_SIZE = 12;
    private Color CURVE_COLOR = new Color(0.8f, 0.8f, 0.8f);
    private Color CURVE_BUTTON_COLOR = new Color(0.8f, 0.8f, 0.8f);
    private Color DIRECTION_COLOR = Color.red;
    private Color DIRECTION_BUTTON_COLOR = Color.red;

    private enum SelectionType {
        Node,
        Direction,
        InverseDirection
    }

    private SplineNode selection;
    private SelectionType selectionType;
    private bool mustCreateNewNode = false;
    private SerializedProperty nodes;
    private Spline spline;

    private GUIStyle nodeButtonStyle, directionButtonStyle;

    private void OnEnable() {
        spline = (Spline)target;
        nodes = serializedObject.FindProperty("nodes");

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
    }

    SplineNode AddClonedNode(SplineNode node) {
        int index = spline.nodes.IndexOf(node);
        SplineNode res = new SplineNode() {
            Position = node.Position,
            Direction = node.Direction
        };
        if (index == spline.nodes.Count - 1) {
            spline.AddNode(res);
        } else {
            spline.InsertNode(index + 1, res);
        }
        return res;
    }

    void DeleteNode(SplineNode node)
    {
        if (spline.nodes.Count > 1)
            spline.RemoveNode(node);
    }

    void OnSceneGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.mouseDown)
        {
            Undo.RegisterCompleteObjectUndo(spline, "change spline topography");
            // if control key pressed, we will have to create a new node if node position is changed
            if (e.alt) {
                mustCreateNewNode = true;
            }
        }
        if (e.type == EventType.mouseUp)
        {
            mustCreateNewNode = false;
        }

        // listen for delete key
        if (e.type == EventType.keyDown && e.keyCode == KeyCode.Delete) {
            Undo.RegisterCompleteObjectUndo(spline, "delete spline node");
            DeleteNode(selection);
            selection = null;
            e.Use();
        }

        // disable game object transform gyzmo
        if (Selection.activeGameObject == spline.gameObject) {
            Tools.current = Tool.None;
            if (selection == null && spline.nodes.Count > 0)
                selection = spline.nodes[0];
        }

        // draw a bezier curve for each curve in the spline
        foreach (CubicBezierCurve curve in spline.GetCurves()) {
            Handles.DrawBezier(spline.transform.TransformPoint(curve.n1.position),
                spline.transform.TransformPoint(curve.n2.position),
                spline.transform.TransformPoint(curve.n1.direction),
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
                selection.Direction = spline.transform.InverseTransformPoint(Handles.PositionHandle(spline.transform.TransformPoint(selection.Direction), Quaternion.identity));
                break;
            case SelectionType.InverseDirection:
                selection.Direction = 2 * selection.Position - spline.transform.InverseTransformPoint(Handles.PositionHandle(2 * spline.transform.TransformPoint(selection.Position) - spline.transform.TransformPoint(selection.Direction), Quaternion.identity));
                break;
        }

        // draw the handles of all nodes, and manage selection motion
        Handles.BeginGUI();
        foreach (SplineNode n in spline.nodes)
        {
            Vector3 guiPos = HandleUtility.WorldToGUIPoint(spline.transform.TransformPoint(n.Position));
            if (n == selection) {
                Vector3 guiDir = HandleUtility.WorldToGUIPoint(spline.transform.TransformPoint(n.Direction));
                Vector3 guiInvDir = HandleUtility.WorldToGUIPoint(spline.transform.TransformPoint(2 * n.Position - n.Direction));

                // for the selected node, we also draw a line and place two buttons for directions
                Handles.color = Color.red;
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
        EditorGUILayout.HelpBox("Hold Alt and drag a node to create a new one.\nPress del to delete selected node.", MessageType.Info);
        EditorGUILayout.PropertyField(nodes, true);
        serializedObject.ApplyModifiedProperties();
    }

    [MenuItem("GameObject/3D Object/Spline")]
    public static void CreateSpline() {
        new GameObject("Spline", typeof(Spline));
    }
}
