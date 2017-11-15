using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Exemple of component to bend a mesh along a spline with an offset.
/// 
/// This component can be used as-is but will most likely be a base for your own component. For explanations of the base component, <see cref="ExemplePipe"/>
/// 
/// In this component, we use the MeshBender translation parameter.
/// It allows you move the source mesh on the Y and Z axis, considering that X axis as spline tangent.
/// 
/// This is usefull to align a mesh that is not centered without reworking it in a modeling tool.
/// It is also useful to offset the mesh from the spline, like in the case of raillings on road sides.
/// </summary>
[ExecuteInEditMode]
[SelectionBase]
public class ExempleRailling : MonoBehaviour {

    public Mesh mesh;
    public Material material;
    public Vector3 rotation;
    public float YOffset;
    public float ZOffset;
    public float scale = 1;

    private Spline spline = null;
    public List<GameObject> meshes = new List<GameObject>();
    private bool toUpdate = true;

    private void OnEnable() {
        spline = GetComponent<Spline>();
        spline.NodeCountChanged.AddListener(() => toUpdate = true);
    }

    private void OnValidate() {
        toUpdate = true;
    }

    private void Update() {
        if (toUpdate) {
            CreateMeshes();
            toUpdate = false;
        }
    }

    public void CreateMeshes() {
        foreach (GameObject go in meshes) {
            if (gameObject != null) {
                if (Application.isPlaying) {
                    Destroy(go);
                } else {
                    DestroyImmediate(go);
                }
            }
        }
        meshes.Clear();

        int i = 0;
        foreach (CubicBezierCurve curve in spline.GetCurves()) {
            GameObject go = new GameObject("SplineMesh" + i++, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshBender), typeof(MeshCollider));
            go.transform.parent = transform;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            //go.hideFlags = HideFlags.NotEditable;

            go.GetComponent<MeshRenderer>().material = material;
            MeshBender mb = go.GetComponent<MeshBender>();
            mb.SetSourceMesh(mesh, false);
            mb.SetRotation(Quaternion.Euler(rotation), false);
            mb.SetTranslation(new Vector3(0, YOffset, ZOffset), false);
            mb.SetCurve(curve, false);
            mb.SetStartScale(scale, false);
            mb.SetEndScale(scale);
            meshes.Add(go);
        }
    }
}
