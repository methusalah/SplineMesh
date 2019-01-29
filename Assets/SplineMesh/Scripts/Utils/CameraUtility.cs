using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SplineMesh {
    public static class CameraUtility {
        public static bool IsOnScreen(Vector3 position) {
            Vector3 onScreen = Camera.current.WorldToViewportPoint(position);
            return onScreen.z > 0 && onScreen.x > 0 && onScreen.y > 0 && onScreen.x < 1 && onScreen.y < 1;
        }
    }
}
