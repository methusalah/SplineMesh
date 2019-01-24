using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SplineMesh {
    /// <summary>
    /// Mathematical object for cubic Bézier curve definition.
    /// It is made of two spline nodes which hold the four needed control points : two positions and two directions
    /// It provides methods to get positions and tangent along the curve, specifying a distance or a ratio, plus the curve length.
    /// 
    /// Note that a time of 0.5 and half the total distance won't necessarily define the same curve point as the curve curvature is not linear.
    /// </summary>
    [Serializable]
    public class CubicBezierCurve {

        private const int STEP_COUNT = 30;
        private const float T_STEP = 1.0f / STEP_COUNT;

        public SplineNode n1, n2;

        /// <summary>
        /// Length of the curve in world unit.
        /// </summary>
        public float Length { get; private set; }
        private readonly List<CurveSample> samples = new List<CurveSample>(STEP_COUNT);

        /// <summary>
        /// This event is raised when of of the control points has moved.
        /// </summary>
        public UnityEvent Changed = new UnityEvent();

        /// <summary>
        /// Build a new cubic Bézier curve between two given spline node.
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        public CubicBezierCurve(SplineNode n1, SplineNode n2) {
            this.n1 = n1;
            this.n2 = n2;
            n1.Changed.AddListener(ComputeSamples);
            n2.Changed.AddListener(ComputeSamples);
            ComputeSamples();
        }

        /// <summary>
        /// Change the start node of the curve.
        /// </summary>
        /// <param name="n1"></param>
        public void ConnectStart(SplineNode n1) {
            this.n1.Changed.RemoveListener(ComputeSamples);
            this.n1 = n1;
            n1.Changed.AddListener(ComputeSamples);
            ComputeSamples();
        }

        /// <summary>
        /// Change the end node of the curve.
        /// </summary>
        /// <param name="n2"></param>
        public void ConnectEnd(SplineNode n2) {
            this.n2.Changed.RemoveListener(ComputeSamples);
            this.n2 = n2;
            n2.Changed.AddListener(ComputeSamples);
            ComputeSamples();
        }

        /// <summary>
        /// Convinent method to get the third control point of the curve, as the direction of the end spline node indicates the starting tangent of the next curve.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetInverseDirection() {
            return (2 * n2.Position) - n2.Direction;
        }

        /// <summary>
        /// Returns point on curve at given time. Time must be between 0 and 1.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private Vector3 GetLocation(float t) {
            AssertTimeInBounds(t);
            float omt = 1f - t;
            float omt2 = omt * omt;
            float t2 = t * t;
            return
                n1.Position * (omt2 * omt) +
                n1.Direction * (3f * omt2 * t) +
                GetInverseDirection() * (3f * omt * t2) +
                n2.Position * (t2 * t);
        }

        /// <summary>
        /// Returns tangent of curve at given time. Time must be between 0 and 1.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private Vector3 GetTangent(float t) {
            AssertTimeInBounds(t);
            float omt = 1f - t;
            float omt2 = omt * omt;
            float t2 = t * t;
            Vector3 tangent =
                n1.Position * (-omt2) +
                n1.Direction * (3 * omt2 - 2 * omt) +
                GetInverseDirection() * (-3 * t2 + 2 * t) +
                n2.Position * (t2);
            return tangent.normalized;
        }

        public Vector3 GetUp(float t) {
            return Vector3.Lerp(n1.Up, n2.Up, t);
        }

        public Vector2 GetScale(float t) {
            return Vector2.Lerp(n1.Scale, n2.Scale, t);
        }

        public float GetRoll(float t) {
            return Mathf.Lerp(n1.Roll, n2.Roll, t);
        }

        private void ComputeSamples() {
            samples.Clear();
            Length = 0;
            Vector3 previousPosition = GetLocation(0);
            for (float t = 0; t < 1; t += T_STEP) {
                CurveSample sample = new CurveSample();
                sample.location = GetLocation(t);
                sample.tangent = GetTangent(t);
                sample.up = GetUp(t);
                sample.roll = GetRoll(t);

                //Vector3.Cross(sample.tangent, Vector3.Cross(Quaternion.AngleAxis(GetRoll(t), Vector3.forward) * previousUp, sample.tangent).normalized);
                //sample.up = Quaternion.AngleAxis(GetRoll(t), Vector3.forward) * previousUp;
                Length += Vector3.Distance(previousPosition, sample.location);
                sample.distance = Length;

                previousPosition = sample.location;
                samples.Add(sample);
            }
            CurveSample lastSample = new CurveSample();
            lastSample.location = GetLocation(1);
            lastSample.tangent = GetTangent(1);
            lastSample.up = GetUp(1);
            lastSample.roll = GetRoll(1);

            //lastSample.upPitch = Vector3.Cross(lastSample.tangent, Vector3.Cross(Quaternion.AngleAxis(GetRoll(1), Vector3.forward) * previousUp, lastSample.tangent).normalized);
            Length += Vector3.Distance(previousPosition, lastSample.location);
            lastSample.distance = Length;
            samples.Add(lastSample);

            if (Changed != null)
                Changed.Invoke();
        }

        private CurveSample getCurvePointAtDistance(float d) {

            AssertTimeInBounds(time);
            CurveSample previous = samples[0];
            CurveSample next = null;
            foreach (CurveSample cp in samples) {
                if (cp.distance >= d) {
                    next = cp;
                    break;
                }
                previous = cp;
            }
            if (next == null) {
                throw new Exception("Can't find curve samples.");
            }
            float t = next == previous ? 0 : (d - previous.distance) / (next.distance - previous.distance);

            CurveSample res = new CurveSample();
            res.distance = d;
            res.location = Vector3.Lerp(previous.location, next.location, t);
            res.tangent = Vector3.Lerp(previous.tangent, next.tangent, t).normalized;
            res.up = Vector3.Lerp(previous.up, next.up, t);
            res.roll = Mathf.Lerp(previous.roll, next.roll, t);
            return res;
        }

        /// <summary>
        /// Returns point on curve at distance. Distance must be between 0 and curve length.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public Vector3 GetLocationAtDistance(float d) {
            return getCurvePointAtDistance(d).location;
        }

        /// <summary>
        /// Returns tangent of curve at distance. Distance must be between 0 and curve length.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public Vector3 GetTangentAtDistance(float d) {
            return getCurvePointAtDistance(d).tangent;
        }

        public Vector3 GetUpAtDistance(float d) {
            return getCurvePointAtDistance(d).up;
        }

        private class CurveSample {
            public Vector3 location;
            public Vector3 tangent;
            public Vector3 up;
            public float roll;
            public float distance;
        }

        /// <summary>
        /// Convenient method that returns a quaternion used rotate an object in the tangent direction, considering Y-axis as up vector.
        /// </summary>
        /// <param name="Tangent"></param>
        /// <returns></returns>
        public static Quaternion GetRotationFromTangent(Vector3 Tangent, float roll = 0) {
            if (Tangent == Vector3.zero)
                return Quaternion.identity;
            return Quaternion.LookRotation(Tangent, Vector3.Cross(Tangent, Vector3.Cross(Quaternion.AngleAxis(roll, Vector3.forward)* Vector3.up, Tangent).normalized));
        }

        public Quaternion GetRotation(float t) {
            var sample = new CurveSample();
            sample.location = GetLocation(t);
            sample.tangent = GetTangent(t);
            sample.up = GetUp(t);
            sample.roll = GetRoll(t);
            return GetRotation(sample);
        }

        public Quaternion GetRotationAtDistance(float d) {
            return GetRotation(getCurvePointAtDistance(d));
        }

        private Quaternion GetRotation(CurveSample sample) {
            var upVector = Vector3.Cross(sample.tangent, Vector3.Cross(Quaternion.AngleAxis(sample.roll, Vector3.forward) * sample.up, sample.tangent).normalized);
            return Quaternion.LookRotation(sample.tangent, upVector);
        }
    }
}
