using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Diagram
{
    public class CanvasVertexFocusDraw : MonoBehaviour
    {
#if UNITY_EDITOR
        public Canvas From, To;
        public float RayFarMut = 1;

        private void OnDrawGizmos()
        {
            Vector3[] rect0 = From.GetComponent<RectTransform>().GetRect(), rect1 = To.GetComponent<RectTransform>().GetRect();
            for (int i = 0; i < 4; i++)
            {
                Vector3 ray = rect1[i] - rect0[i];
                Debug.DrawLine(rect0[i], rect1[i] + ray * RayFarMut * 0.01f, Color.red);
            }
        }
#endif
    }
}
