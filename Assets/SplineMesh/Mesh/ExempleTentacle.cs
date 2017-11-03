using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[SelectionBase]
public class ExempleTentacle : MonoBehaviour {
    public Mesh mesh;
    public Material material;
    public Vector3 rotation;
    public float startScale = 1, endScale = 1;
    public float startRoll = 0, endRoll = 0;

    private Spline spline = null;
    public List<GameObject> meshes = new List<GameObject>();
    private bool toUpdate = false;

    private void OnEnable() {
        spline = GetComponent<Spline>();
        spline.NodesChanged.AddListener(() => { toUpdate = true; });
    }

    private void OnValidate() {
        if(spline == null)
            return;
        toUpdate = true;
    }

    private void Update() {
        if (toUpdate) {
            toUpdate = false;
            CreateMeshes();
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

        float currentLength = 0;
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

            float startRate = currentLength / spline.Length;
            currentLength += mb.curve.Length;
            float endRate = currentLength / spline.Length;

            mb.SetStartScale(startScale + (endScale - startScale) * startRate, false);
            mb.SetEndScale(startScale + (endScale - startScale) * endRate, false);

            mb.SetStartRoll(startRoll, false);
            mb.SetEndRoll(endRoll);
            meshes.Add(go);
        }
    }
}
