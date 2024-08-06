using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Diagram
{
    [Serializable]
    public class RT_Drag_Helper
    {
        public RT_Drag_Helper() { }
        [_Init_]
        public RT_Drag_Helper([_In_]RectTransform target)
        {
            this.TargetRectTransform = target;
        }

        RectTransform LimitContainer => TargetRectTransform.parent as RectTransform;
        public Canvas ParentCanvas
        {
            get
            {
                Transform current = TargetRectTransform;
                Canvas canvas = null;
                do current = current.transform.parent; while (current != null && (canvas = current.SeekComponent<Canvas>()) == null);
                return canvas;
            }
        }
        [SerializeField][_Must_] private RectTransform TargetRectTransform;
        private Vector3 offset = Vector3.zero;
        private float minX, maxX, minY, maxY;

        public void OnDrag(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(TargetRectTransform, eventData.position, eventData.pressEventCamera, out Vector3 globalMousePos))
            {
                TargetRectTransform.position = DragRangeLimit(globalMousePos + offset);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(TargetRectTransform, eventData.position, eventData.enterEventCamera, out Vector3 globalMousePos))
            {
                offset = TargetRectTransform.position - globalMousePos;
                SetDragRange();
            }
        }

        void SetDragRange()
        {
            // 最小x坐标 = 容器当前x坐标 - 容器轴心距离左边界的距离 + UI轴心距离左边界的距离
            minX = LimitContainer.position.x
                - LimitContainer.pivot.x * LimitContainer.rect.width * ParentCanvas.scaleFactor
                + TargetRectTransform.rect.width * ParentCanvas.scaleFactor * TargetRectTransform.pivot.x;
            // 最大x坐标 = 容器当前x坐标 + 容器轴心距离右边界的距离 - UI轴心距离右边界的距离
            maxX = LimitContainer.position.x
                + (1 - LimitContainer.pivot.x) * LimitContainer.rect.width * ParentCanvas.scaleFactor
                - TargetRectTransform.rect.width * ParentCanvas.scaleFactor * (1 - TargetRectTransform.pivot.x);

            // 最小y坐标 = 容器当前y坐标 - 容器轴心距离底边的距离 + UI轴心距离底边的距离
            minY = LimitContainer.position.y
                - LimitContainer.pivot.y * LimitContainer.rect.height * ParentCanvas.scaleFactor
                + TargetRectTransform.rect.height * ParentCanvas.scaleFactor * TargetRectTransform.pivot.y;

            // 最大y坐标 = 容器当前x坐标 + 容器轴心距离顶边的距离 - UI轴心距离顶边的距离
            maxY = LimitContainer.position.y
                + (1 - LimitContainer.pivot.y) * LimitContainer.rect.height * ParentCanvas.scaleFactor
                - TargetRectTransform.rect.height * ParentCanvas.scaleFactor * (1 - TargetRectTransform.pivot.y);
        }
        // 限制坐标范围
        Vector3 DragRangeLimit(Vector3 pos)
        {
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            return pos;
        }
    }
}
