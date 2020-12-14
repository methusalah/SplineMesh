using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SplineMesh
{
    /// <summary>
    /// Spline node storing a position and a direction (tangent).
    /// Note : you shouldn't modify position and direction manualy but use dedicated methods instead, to insure event raising.
    /// </summary>
    [Serializable]
    public class SplineNode
    {


        /// <summary>
        /// Node position
        /// </summary>
        public Vector3 Position
        {
            get { return position; }
            set
            {
                if (position.Equals(value)) return;
                position.x = value.x;
                position.y = value.y;
                position.z = value.z;
                if (Changed != null) Changed(this, EventArgs.Empty);
            }
        }
        [SerializeField]
        private Vector3 position;


        /// <summary>
        /// Tangent vector type of the node
        /// - Mirrored (default): The in and out tangent vectors are symetrical around the node.
        /// - Aligned: the in and out tangent vectors are also opposed but they can have different lengths, hence different curvature
        /// ( to be implemented )
        /// - Free: the in and out vectors can be completly independant.
        /// </summary>
        public enum TangentType { Mirrored, Free };

        /// <summary>
        /// Node tangent type
        /// </summary>
        public TangentType DirectionType
        {
            get { return directionType; }
            set
            {
                if (directionType.Equals(value)) return;
                directionType = value;
                if (Changed != null) Changed(this, EventArgs.Empty);
            }
        }

        [SerializeField]
        public TangentType directionType = TangentType.Mirrored;

        /// <summary>
        /// Node direction;
        /// this parameter could be removed in a future version, use
        /// directionIn / directionOut
        /// USE DirectionOut and DirectionIn
        /// </summary>
        public Vector3 Direction
        {
            get
            {                
                return DirectionOut;
            }
            set
            {
                DirectionOut = value;
            }
        }


        [SerializeField]
        private Vector3 direction;



        public Vector3 DirectionIn
        {
            get {
                /*
                 * for backward compatibility:
                 * saved nodes will have direction not zero, and directionout == Vector3.zero
                 * 
                 * if this situation occurs, we update directionOut
                 */

                if (directionIn == Vector3.zero && direction != Vector3.zero)
                {
                    directionIn.x = 2 * position.x - direction.x;
                    directionIn.y = 2 * position.y - direction.y;
                    directionIn.z = 2 * position.z - direction.z;
                }

                return directionIn;
            }
            set
            {

                if (directionIn.Equals(value)) return;
                directionIn.x = value.x;
                directionIn.y = value.y;
                directionIn.z = value.z;


                // we are setting DirectionIn, but the TangentType is mirrored:
                // we need that directionOut is altered too, with the opposite value

                if (directionType == TangentType.Mirrored)
                {
                    directionOut.x = 2 * position.x - directionIn.x;
                    directionOut.y = 2 * position.y - directionIn.y;
                    directionOut.z = 2 * position.z - directionIn.z;
                }


                if (Changed != null) Changed(this, EventArgs.Empty);
            }
        }

        [SerializeField]
        private Vector3 directionIn;




        public Vector3 DirectionOut
        {
            get {
                /*
                 * for backward compatibility:
                 * saved nodes will have direction not zero, and directionout == Vector3.zero
                 * 
                 * if this situation occurs, we update directionOut
                 */

                if(directionOut == Vector3.zero && direction != Vector3.zero)
                {
                    directionOut = direction;
                }

                return directionOut;
            }
            set
            {
                if (directionOut.Equals(value)) return;
                directionOut.x = value.x;
                directionOut.y = value.y;
                directionOut.z = value.z;


                // we are setting DirectionOut, but the TangentType is mirrored:
                // we need that directionIn is altered too, with the opposite value
                if (directionType == TangentType.Mirrored)
                {

                    directionIn.x = 2 * position.x - directionOut.x;
                    directionIn.y = 2 * position.y - directionOut.y;
                    directionIn.z = 2 * position.z - directionOut.z;

                }

                if (Changed != null) Changed(this, EventArgs.Empty);
            }
        }

        [SerializeField]
        private Vector3 directionOut;


        /// <summary>
        /// Up vector to apply at this node.
        /// Usefull to specify the orientation when the tangent blend with the world UP (gimball lock)
        /// This value is not used on the spline itself but is commonly used on bended content.
        /// </summary>
        public Vector3 Up
        {
            get { return up; }
            set
            {
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
        public Vector2 Scale
        {
            get { return scale; }
            set
            {
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
        public float Roll
        {
            get { return roll; }
            set
            {
                if (roll == value) return;
                roll = value;
                if (Changed != null) Changed(this, EventArgs.Empty);
            }
        }
        [SerializeField]
        private float roll;

        public SplineNode(Vector3 position, Vector3 direction)
        {
            Position = position;
            Direction = direction;
        }

        public SplineNode(Vector3 position, Vector3 new_directionOut, Vector3 new_directionIn)
        {


            // this causes a side effect in DirectionOut that I could not solve
            //DirectionOut = directionOut;            
            //DirectionIn = directionIn;

            directionOut = new_directionOut;
            directionIn = new_directionIn;


            // due to the previous issue, we set position as last item so that the event handler is notified
            Position = position;


        }

        /// <summary>
        /// Event raised when position, direction, scale or roll changes.
        /// </summary>
        [HideInInspector]
        public event EventHandler Changed;
    }
}
