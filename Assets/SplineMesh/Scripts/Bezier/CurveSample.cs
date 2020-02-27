using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SplineMesh {
    /// <summary>
    /// Imutable class containing all data about a point on a cubic bezier curve.
    /// </summary>
    public struct CurveSample
    {
        public readonly Vector3 location;
        public readonly Vector3 tangent;
        public readonly Vector3 up;
        public readonly Vector2 scale;
        public readonly float roll;
        public readonly float distanceInCurve;
        public readonly float timeInCurve;

        private Quaternion rotation;

        /// <summary>
        /// Rotation is a look-at quaternion calculated from the tangent, roll and up vector. Mixing non zero roll and custom up vector is not advised.
        /// </summary>
        public Quaternion Rotation {
            get {
                if (rotation == Quaternion.identity) {
                    var upVector = Vector3.Cross(tangent, Vector3.Cross(Quaternion.AngleAxis(roll, Vector3.forward) * up, tangent).normalized);
                    rotation = Quaternion.LookRotation(tangent, upVector);
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
            rotation = Quaternion.identity;
        }

        public override bool Equals(object obj) {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }
            CurveSample other = (CurveSample)obj;
            return location == other.location &&
                tangent == other.tangent &&
                up == other.up &&
                scale == other.scale &&
                roll == other.roll &&
                distanceInCurve == other.distanceInCurve &&
                timeInCurve == other.timeInCurve;

        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public static bool operator ==(CurveSample cs1, CurveSample cs2) {
            return cs1.Equals(cs2);
        }

        public static bool operator !=(CurveSample cs1, CurveSample cs2) {
            return !cs1.Equals(cs2);
        }

        /// <summary>
        /// Linearly interpolates between two curve samples.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
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

        public MeshVertex GetBent(MeshVertex vert) {
            var res = new MeshVertex(vert.position, vert.normal, vert.uv);

            // application of scale
            res.position = Vector3.Scale(res.position, new Vector3(0, scale.y, scale.x));

            // application of roll
            res.position = Quaternion.AngleAxis(roll, Vector3.right) * res.position;
            res.normal = Quaternion.AngleAxis(roll, Vector3.right) * res.normal;

            // reset X value
            res.position.x = 0;

            // application of the rotation + location
            Quaternion q = Rotation * Quaternion.Euler(0, -90, 0);
            res.position = q * res.position + location;
            res.normal = q * res.normal;
            return res;
        }
    }
}
