using System;
using UnityEngine;

namespace Diagram
{
    [Serializable]
    public class CameraSystemData
    {
        public Canvas MyCanvas;
        public Canvas4Data FullPlaneTop;
        public Canvas4Data FullPlaneBottom;
    }

    [Serializable]
    public class CameraSystem :
#if PLATFORM_STANDALONE_WIN&&CAMERA_WINDOWS_3D
        PlayerCamera3DBase
#else
        LineBehaviour
#endif
    {
        [Header("Camera System Data")]
        public CameraSystemData MySortingRect;

        public Camera GetCamera() => this.SeekComponent<Camera>();
        public void SetPerspective() => GetCamera().orthographic = false;
        public void SetOrthographic() => GetCamera().orthographic = true;
        public void SetFieldOfView(float value) => GetCamera().fieldOfView = value;
        public void SetOrthographicSize(float value) => GetCamera().orthographicSize = value;
        public void SetNear(float near) => GetCamera().nearClipPlane = near;
        public void SetFar(float far) => GetCamera().farClipPlane = far;

        public override void Reset()
        {
            MySortingRect = new()
            {
                MyCanvas = this.GetComponentInChildren<Canvas>()
            };
            if (MySortingRect.MyCanvas == null) return;
            for (int i = 0, e = MySortingRect.MyCanvas.transform.childCount; i < e; i++)
            {
                var cur = MySortingRect.MyCanvas.transform.GetChild(i);
                if (cur.name.Contains("Full"))
                {
                    if (MySortingRect.FullPlaneTop == null)
                    {
                        MySortingRect.FullPlaneTop = new() { FullRect = cur.GetComponent<RectTransform>() };
                        for (int j = 0, ex = MySortingRect.FullPlaneTop.FullRect.transform.childCount; j < ex; j++)
                        {
                            var curchildcur = MySortingRect.FullPlaneTop.FullRect.GetChild(j);
                            if (curchildcur.name.Contains("Left"))
                                MySortingRect.FullPlaneTop.LeftRect = curchildcur as RectTransform;
                            if (curchildcur.name.Contains("Right"))
                                MySortingRect.FullPlaneTop.RightRect = curchildcur as RectTransform;
                            if (curchildcur.name.Contains("Top"))
                                MySortingRect.FullPlaneTop.TopRect = curchildcur as RectTransform;
                            if (curchildcur.name.Contains("Bottom"))
                                MySortingRect.FullPlaneTop.BottomRect = curchildcur as RectTransform;
                        }
                    }
                    else if (MySortingRect.FullPlaneBottom == null)
                    {
                        MySortingRect.FullPlaneBottom = new() { FullRect = cur.GetComponent<RectTransform>() };
                        for (int j = 0, ex = MySortingRect.FullPlaneBottom.FullRect.transform.childCount; j < ex; j++)
                        {
                            var curchildcur = MySortingRect.FullPlaneBottom.FullRect.GetChild(j);
                            if (curchildcur.name.Contains("Left"))
                                MySortingRect.FullPlaneBottom.LeftRect = curchildcur as RectTransform;
                            if (curchildcur.name.Contains("Right"))
                                MySortingRect.FullPlaneBottom.RightRect = curchildcur as RectTransform;
                            if (curchildcur.name.Contains("Top"))
                                MySortingRect.FullPlaneBottom.TopRect = curchildcur as RectTransform;
                            if (curchildcur.name.Contains("Bottom"))
                                MySortingRect.FullPlaneBottom.BottomRect = curchildcur as RectTransform;
                        }
                        break;
                    }
                }
            }
        }
    }
}
