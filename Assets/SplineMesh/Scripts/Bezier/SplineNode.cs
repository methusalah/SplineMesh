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
        /// </summary>
        public Vector3 Position {
            get { return position; }
            set {
                if (position.Equals(value)) return;
                position.x = value.x;
                position.y = value.y;
                position.z = value.z;
                if(Changed != null) Changed(this, EventArgs.Empty);
            }
        }
        [SerializeField]
        private Vector3 position;

        /// <summary>
        /// Node direction
        /// </summary>
        public Vector3 Direction {
            get { return direction; }
            set {
                if (direction.Equals(value)) return;
                direction.x = value.x;
                direction.y = value.y;
                direction.z = value.z;
                if (Changed != null) Changed(this, EventArgs.Empty);
            }
        }
        [SerializeField]
        private Vector3 direction;

        /// <summary>
        /// Up vector to apply at this node.
        /// Usefull to specify the orientation when the tangent blend with the world UP (gimball lock)
        /// This value is not used on the spline itself but is commonly used on bended content.
        /// </summary>
        public Vector3 Up {
            get { return up; }
            set {
                if (up.Equals(value)) return;
                up.x = value.x;
                up.y = value.y;
                up.z = value.z;
                if (Changed != null) Changed(this, EventArgs.Empty);
            }
        }
        [SerializeField]
        private Vector3 up = Vector3.up;

        /// <summary>
        /// Scale to apply at this node.
        /// This value is not used on the spline itself but is commonly used on bended content.
        /// </summary>
        public Vector2 Scale {
            get { return scale; }
            set {
                if (scale.Equals(value)) return;
                scale.x = value.x;
                scale.y = value.y;
                if (Changed != null) Changed(this, EventArgs.Empty);
            }
        }
        [SerializeField]
        private Vector2 scale = Vector2.one;

        /// <summary>
        /// Roll to apply at this node.
        /// This value is not used on the spline itself but is commonly used on bended content.
        /// </summary>
        public float Roll {
            get { return roll; }
            set {
                if (roll == value) return;
                roll = value;
                if (Changed != null) Changed(this, EventArgs.Empty);
            }
        }
        [SerializeField]
        private float roll;

        public SplineNode(Vector3 position, Vector3 direction) {
            Position = position;
            Direction = direction;
        }

        /// <summary>
        /// Event raised when position, direction, scale or roll changes.
        /// </summary>
        [HideInInspector]
        public event EventHandler Changed;
    }
}
