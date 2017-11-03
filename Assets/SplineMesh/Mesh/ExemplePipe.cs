using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        spline.NodesChanged.AddListener(() => toUpdate = true);
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
