using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Exemple of component to places assets along a spline. This component can be used as-is but will most likely be a base for your own component.
/// 
/// In this exemple, the user gives the prefab to place, a spacing value between two placements, the prefab scale and an horizontal offset to the spline.
/// These three last values have an additional range, allowing to add some randomness. for each placement, the computed value will be between value and value+range.
/// 
/// Prefabs are placed from the start of the spline at computed spacing, unitl there is no lentgh remaining. Prefabs are stored, destroyed
/// and built again each time the spline or one of its curves change.
/// 
/// A random seed is used to obtain the same random numbers at each update. The user can specify the seed to test some other random number set.
/// 
/// Place prefab along a spline and deform it easily have a lot of usages if you have some imagination : 
///  - place trees along a road
///  - create a rocky bridge
///  - create a footstep track with decals
///  - create a path of firefly in the dark
///  - create a natural wall with overlapping rocks
///  - etc.
/// </summary>
[ExecuteInEditMode]
[SelectionBase]
public class ExempleSower : MonoBehaviour {

    public GameObject prefab = null;
    public float scale = 1, scaleRange = 0;
    public float spacing = 1, spacingRange = 0;
    public float offset = 0, offsetRange = 0;
    public bool isRandomYaw = false;
    public int randomSeed = 0;

    [HideInInspector]
    public List<GameObject> prefabs = new List<GameObject>();

    private Spline spline = null;
    private bool toUpdate = true;


    private void OnEnable() {
        spline = GetComponent<Spline>();
        spline.NodeCountChanged.AddListener(() => {
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
        foreach (GameObject go in prefabs) {
            if (gameObject != null) {
                if (Application.isPlaying) {
                    Destroy(go);
                } else {
                    DestroyImmediate(go);
                }
            }
        }
        prefabs.Clear();

        UnityEngine.Random.InitState(randomSeed);
        if (spacing + spacingRange <= 0 ||
            prefab == null)
            return;

        float distance = 0;
        while (distance <= spline.Length) {
            GameObject go = Instantiate(prefab, transform);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;

            // move along spline, according to spacing + random
            go.transform.localPosition = spline.GetLocationAlongSplineAtDistance(distance);
            // apply scale + random
            float rangedScale = scale + UnityEngine.Random.Range(0, scaleRange);
            go.transform.localScale = new Vector3(rangedScale, rangedScale, rangedScale);
            // rotate with random yaw
            if (isRandomYaw) {
                go.transform.Rotate(0, 0, UnityEngine.Random.Range(-180, 180));
            } else {
                Vector3 horTangent = spline.GetTangentAlongSplineAtDistance(distance);
                horTangent.y = 0;
                go.transform.rotation = Quaternion.LookRotation(horTangent) * Quaternion.LookRotation(Vector3.left, Vector3.up);
            }
            // move orthogonaly to the spline, according to offset + random
            Vector3 binormal = spline.GetTangentAlongSplineAtDistance(distance);
            binormal = Quaternion.LookRotation(Vector3.right, Vector3.up) * binormal;
            binormal *= offset + UnityEngine.Random.Range(0, offsetRange * Math.Sign(offset));
            go.transform.position += binormal;

            prefabs.Add(go);

            distance += spacing + UnityEngine.Random.Range(0, spacingRange);
        }
    }
}
