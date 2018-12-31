using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SplineMesh {
    /// <summary>
    /// Spline node storing a position and a direction (tangent).
    /// Note : you shouldn't modify position and direction manualy but use dedicated methods instead, to insure event raising.
    /// </summary>
    [Serializable]
    public class SplineNode {

        /// <summary>
        /// Node position
        /// Note : you shouldn't modify position and direction manualy but use dedicated methods instead, to insure event raising.
        /// </summary>
        [SerializeField]
        private Vector3 position;

        public Vector3 Position {
            get { return position; }
            set {
                if (position.Equals(value)) return;
                position.x = value.x;
                position.y = value.y;
                position.z = value.z;
                if (Changed != null)
                    Changed.Invoke();
            }
        }


        /// <summary>
        /// Node direction
        /// Note : you shouldn't modify position and direction manualy but use dedicated methods instead, to insure event raising.
        /// </summary>
        [SerializeField]
        private Vector3 direction;

        public Vector3 Direction {
            get { return direction; }
            set {
                if (direction.Equals(value)) return;
                direction.x = value.x;
                direction.y = value.y;
                direction.z = value.z;
                if (Changed != null)
                    Changed.Invoke();
            }
        }


        public SplineNode(Vector3 position, Vector3 direction) {
            Position = position;
            Direction = direction;
        }

        /// <summary>
        /// Event raised when position or direct changes.
        /// </summary>
        [HideInInspector]
        public UnityEvent Changed = new UnityEvent();
    }
}
