using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SplineMesh {
    public class CurveSample {
        public readonly Vector3 location;
        public readonly Vector3 tangent;
        public readonly Vector3 up;
        public readonly Vector2 scale;
        public readonly float roll;
        public readonly float distanceInCurve;
        public readonly float timeInCurve;

        private Quaternion rotation = Quaternion.identity;

        public Quaternion Rotation {
            get {
                if (rotation.Equals(Quaternion.identity)) {
                    var upVector = Vector3.Cross(tangent, Vector3.Cross(Quaternion.AngleAxis(roll, Vector3.forward) * up, tangent).normalized);
                    rotation =  Quaternion.LookRotation(tangent, upVector);
                }
                return rotation;
            }
        }

        public CurveSample(Vector3 location, Vector3 tangent, Vector3 up, Vector2 scale, float roll, float distanceInCurve, float timeInCurve) {
            this.location = location;
            this.tangent = tangent;
            this.up = up;
            this.roll = roll;
            this.scale = scale;
            this.distanceInCurve = distanceInCurve;
            this.timeInCurve = timeInCurve;
        }

        public static CurveSample Lerp(CurveSample a, CurveSample b, float t) {
            return new CurveSample(
                Vector3.Lerp(a.location, b.location, t),
                Vector3.Lerp(a.tangent, b.tangent, t).normalized,
                Vector3.Lerp(a.up, b.up, t),
                Vector2.Lerp(a.scale, b.scale, t),
                Mathf.Lerp(a.roll, b.roll, t),
                Mathf.Lerp(a.distanceInCurve, b.distanceInCurve, t),
                Mathf.Lerp(a.timeInCurve, b.timeInCurve, t));
        }
    }
}
