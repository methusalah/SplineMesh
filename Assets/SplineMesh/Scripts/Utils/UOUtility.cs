using UnityEngine;
using UnityEditor;
using System.Linq;

namespace SplineMesh {
    public static class UOUtility {
        public static GameObject Create(string name, GameObject parent) {
            var res = new GameObject(name);
            res.transform.parent = parent.transform;
            res.transform.localPosition = Vector3.zero;
            res.transform.localScale = Vector3.one;
            res.transform.localRotation = Quaternion.identity;
            return res;
        }

        public static void Destroy(GameObject go) {
            if (Application.isPlaying) {
                Object.Destroy(go);
            } else {
                Object.DestroyImmediate(go);
            }
        }

        public static void DestroyChildren(GameObject go) {
            var childList = go.transform.Cast<Transform>().ToList();
            foreach (Transform childTransform in childList) {
                Destroy(childTransform.gameObject);
            }
        }
    }
}
