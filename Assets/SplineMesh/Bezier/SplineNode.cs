using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class SplineNode {

    public Vector3 position;
    /// <summary>
    /// The point
    /// </summary>
    public Vector3 Position {
        get {
            return position;
        }
        set {
            if (!position.Equals(value)) {
                position.x = value.x;
                position.y = value.y;
                position.z = value.z;
                if (Changed != null)
                    Changed.Invoke();
            }
        }
    }

    public Vector3 direction;
    /// <summary>
    /// The tangent
    /// </summary>
    public Vector3 Direction {
        get {
            return direction;
        }
        set {
            if (!direction.Equals(value)) {
                direction.x = value.x;
                direction.y = value.y;
                direction.z = value.z;
                if (Changed != null)
                    Changed.Invoke();
            }
        }
    }

    [HideInInspector]
    public UnityEvent Changed = new UnityEvent();
}
