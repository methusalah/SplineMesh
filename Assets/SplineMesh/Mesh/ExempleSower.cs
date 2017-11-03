using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[SelectionBase]
public class ExempleSower : MonoBehaviour {

    public GameObject prefab = null;
    public float scale = 1, scaleRange = 0;
    public float spacing = 1, spacingRange = 0;
    public float offset = 0, offsetRange = 0;
    public int randomSeed = 0;

    [HideInInspector]
    public List<GameObject> meshes = new List<GameObject>();

    private Spline spline = null;
    private bool toUpdate = true;


    private void OnEnable() {
        spline = GetComponent<Spline>();
        spline.NodesChanged.AddListener(() => {
            toUpdate = true;
            foreach (CubicBezierCurve curve in spline.GetCurves()) {
                curve.Changed.AddListener(() => toUpdate = true);
            }
        });
        foreach (CubicBezierCurve curve in spline.GetCurves()) {
            curve.Changed.AddListener(() => toUpdate = true);
        }
    }

    private void OnValidate() {
        toUpdate = true;
    }

    private void Update() {
        if (toUpdate) {
            Sow();
            toUpdate = false;
        }
    }

    public void Sow() {
        foreach (GameObject go in meshes) {
            DestroyImmediate(go);
        }
        meshes.Clear();

        UnityEngine.Random.InitState(randomSeed);
        if (spacing + spacingRange <= 0 ||
            prefab == null)
            return;

        float distance = 0;
        while (distance <= spline.Length) {
            GameObject go = Instantiate(prefab, transform);
            // move along spline, according to spacing + random
            go.transform.position = spline.GetLocationAlongSplineAtDistance(distance);
            // apply scale + random
            float rangedScale = scale + UnityEngine.Random.Range(0, scaleRange);
            go.transform.localScale = new Vector3(rangedScale, rangedScale, rangedScale);
            // rotate with random yaw
            go.transform.Rotate(0, 0, UnityEngine.Random.Range(-180, 180));
            // move orthogonaly to the spline, according to offset + random
            Vector3 binormal = spline.GetTangentAlongSplineAtDistance(distance);
            binormal = Quaternion.LookRotation(Vector3.right, Vector3.up) * binormal;
            binormal *= offset + UnityEngine.Random.Range(0, offsetRange * Math.Sign(offset));
            go.transform.position += binormal;

            meshes.Add(go);

            distance += spacing + UnityEngine.Random.Range(0, spacingRange);
        }
    }
}
