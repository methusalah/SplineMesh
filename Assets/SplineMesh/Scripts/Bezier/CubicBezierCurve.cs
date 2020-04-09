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

        private readonly List<CurveSample> samples = new List<CurveSample>(STEP_COUNT);

        public SplineNode n1, n2;

        /// <summary>
        /// Length of the curve in world unit.
        /// </summary>
        public float Length { get; private set; }

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
            n1.Changed += ComputeSamples;
            n2.Changed += ComputeSamples;
            ComputeSamples(null, null);
        }

        /// <summary>
        /// Change the start node of the curve.
        /// </summary>
        /// <param name="n1"></param>
        public void ConnectStart(SplineNode n1) {
            this.n1.Changed -= ComputeSamples;
            this.n1 = n1;
            n1.Changed += ComputeSamples;
            ComputeSamples(null, null);
        }

        /// <summary>
        /// Change the end node of the curve.
        /// </summary>
        /// <param name="n2"></param>
        public void ConnectEnd(SplineNode n2) {
            this.n2.Changed -= ComputeSamples;
            this.n2 = n2;
            n2.Changed += ComputeSamples;
            ComputeSamples(null, null);
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

        private Vector3 GetUp(float t) {
            return Vector3.Lerp(n1.Up, n2.Up, t);
        }

        private Vector2 GetScale(float t) {
            return Vector2.Lerp(n1.Scale, n2.Scale, t);
        }

        private float GetRoll(float t) {
            return Mathf.Lerp(n1.Roll, n2.Roll, t);
        }

        private void ComputeSamples(object sender, EventArgs e) {
            samples.Clear();
            Length = 0;
            Vector3 previousPosition = GetLocation(0);
            for (float t = 0; t < 1; t += T_STEP) {
                Vector3 position = GetLocation(t);
                Length += Vector3.Distance(previousPosition, position);
                previousPosition = position;
                samples.Add(CreateSample(Length, t));
            }
            Length += Vector3.Distance(previousPosition, GetLocation(1));
            samples.Add(CreateSample(Length, 1));

            if (Changed != null) Changed.Invoke();
        }

        private CurveSample CreateSample(float distance, float time) {
            return new CurveSample(
                GetLocation(time),
                GetTangent(time),
                GetUp(time),
                GetScale(time),
                GetRoll(time),
                distance,
                time,
                this);
        }

        /// <summary>
        /// Returns an interpolated sample of the curve, containing all curve data at this time.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public CurveSample GetSample(float time) {
            AssertTimeInBounds(time);
            CurveSample previous = samples[0];
            CurveSample next = default(CurveSample);
            bool found = false;
            foreach (CurveSample cp in samples) {
                if (cp.timeInCurve >= time) {
                    next = cp;
                    found = true;
                    break;
                }
                previous = cp;
            }
            if (!found) throw new Exception("Can't find curve samples.");
            float t = next == previous ? 0 : (time - previous.timeInCurve) / (next.timeInCurve - previous.timeInCurve);

            return CurveSample.Lerp(previous, next, t);
        }

        /// <summary>
        /// Returns an interpolated sample of the curve, containing all curve data at this distance.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public CurveSample GetSampleAtDistance(float d) {
            if (d < 0 || d > Length)
                throw new ArgumentException("Distance must be positive and less than curve length. Length = " + Length + ", given distance was " + d);

            CurveSample previous = samples[0];
            CurveSample next = default(CurveSample);
            bool found = false;
            foreach (CurveSample cp in samples) {
                if (cp.distanceInCurve >= d) {
                    next = cp;
                    found = true;
                    break;
                }
                previous = cp;
            }
            if (!found) throw new Exception("Can't find curve samples.");
            float t = next == previous ? 0 : (d - previous.distanceInCurve) / (next.distanceInCurve - previous.distanceInCurve);

            return CurveSample.Lerp(previous, next, t);
        }

        private static void AssertTimeInBounds(float time) {
            if (time < 0 || time > 1) throw new ArgumentException("Time must be between 0 and 1 (was " + time + ").");
        }

        public CurveSample GetProjectionSample(Vector3 pointToProject) {
            float minSqrDistance = float.PositiveInfinity;
            int closestIndex = -1;
            int i = 0;
            foreach (var sample in samples) {
                float sqrDistance = (sample.location - pointToProject).sqrMagnitude;
                if (sqrDistance < minSqrDistance) {
                    minSqrDistance = sqrDistance;
                    closestIndex = i;
                }
                i++;
            }
            CurveSample previous, next;
            if(closestIndex == 0) {
                previous = samples[closestIndex];
                next = samples[closestIndex + 1];
            } else if(closestIndex == samples.Count - 1) {
                previous = samples[closestIndex - 1];
                next = samples[closestIndex];
            } else {
                var toPreviousSample = (pointToProject - samples[closestIndex - 1].location).sqrMagnitude;
                var toNextSample = (pointToProject - samples[closestIndex + 1].location).sqrMagnitude;
                if (toPreviousSample < toNextSample) {
                    previous = samples[closestIndex - 1];
                    next = samples[closestIndex];
                } else {
                    previous = samples[closestIndex];
                    next = samples[closestIndex + 1];
                }
            }

            var onCurve = Vector3.Project(pointToProject - previous.location, next.location - previous.location) + previous.location;
            var rate = (onCurve - previous.location).sqrMagnitude / (next.location - previous.location).sqrMagnitude;
            rate = Mathf.Clamp(rate, 0, 1);
            var result = CurveSample.Lerp(previous, next, rate);
            return result;
        }
    }
}
