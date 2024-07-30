using System;
using UnityEngine;

namespace Diagram
{
    [Serializable]
    public class Canvas4Data
    {
        public RectTransform FullRect;
        public RectTransform LeftRect;
        public RectTransform RightRect;
        public RectTransform TopRect;
        public RectTransform BottomRect;
    }

    [Serializable]
    public class CanvasSystemData
    {
        public Canvas MyCanvas;
        public Canvas4Data FullPlaneTop;
        public Canvas4Data FullPlaneBottom;
    }

    public class CanvasSystem : MonoBehaviour
    {
        public CanvasSystemData MySortingRect;

        private void Reset()
        {
            MySortingRect = new();
            MySortingRect.MyCanvas = this.SeekComponent<Canvas>();
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
