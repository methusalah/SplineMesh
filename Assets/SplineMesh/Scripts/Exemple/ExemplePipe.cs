using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Exemple of component to bend a mesh along a spline. This component can be used as-is but will most likely be a base for your own component.
/// 
/// In this basic exemple, you only specify rotation and scale to adapt to the provided mesh.
/// Scale is useful because most of the time, the modeling tools don't use the scaling you want.
/// Rotation is often mandatory because MeshBender will always bend along the X axis and your mesh may be oriented differently.
/// 
/// One children GameObject is created for each spline curve here, with a MeshBender and a MeshFilter on each. The list of GameObject is stored for later cleanup.
/// Each time the spline nodes are changed (a node is added or removed), the stored object are cleaned and the entire process is redone.
/// 
/// The MeshBender listen the curve to detect itself if the nodes it is connected to are moved or rotated. You don't have to manage that yourself here.
/// </summary>
[ExecuteInEditMode]
[SelectionBase]
public class ExemplePipe : MonoBehaviour {
    public Mesh mesh;
    public Material material;
    public Vector3 rotation;
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
        foreach(GameObject go in meshes) {
            if(gameObject != null) {
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
            mb.SetCurve(curve, false);
            mb.SetStartScale(scale, false);
            mb.SetEndScale(scale);
            meshes.Add(go);
        }
    }
}
