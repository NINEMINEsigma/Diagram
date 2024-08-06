using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Diagram.UI
{
    public class WindowUIBox : LineBehaviour, IDragHandler, IPointerExitHandler
    {
        private Image img;//拖拽的对象
        public Camera uicam;//渲染UI的相机
        private RectTransform rect;//拖拽对象的矩形对象
        private UIEdge currentUiEdge;//当前鼠标所在UI的边缘枚举
        [SerializeField] private Vector2 MinSizeData = new(10, 10);

        private void Start()
        {
            CursorManager.instance.LoadCursor();//初始化加载鼠标光标
            img = GetComponent<Image>();
            rect = img.GetComponent<RectTransform>();
        }
        /// <summary>
        /// 鼠标移出UI，恢复默认光标
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerExit(PointerEventData eventData)
        {
            CursorManager.instance.SetDefaultCursor();
        }
        /// <summary>
        /// 拖拽过程中，根据鼠标所在边缘区域动态修改该UI图片的右上偏移offsetMax或左下偏移offsetMin
        /// </summary>
        /// <param name="eventData"></param>
        public void OnDrag(PointerEventData eventData)
        {
            switch (currentUiEdge)
            {
                case UIEdge.None:
                    break;
                case UIEdge.Up://UI上方
                    rect.offsetMax = new Vector2(rect.offsetMax.x, rect.offsetMax.y + eventData.delta.y);
                    break;
                case UIEdge.Down://UI下方
                    rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y + eventData.delta.y);
                    break;
                case UIEdge.Left://UI左边
                    rect.offsetMin = new Vector2(rect.offsetMin.x + eventData.delta.x, rect.offsetMin.y);
                    break;
                case UIEdge.Right://UI右边
                    rect.offsetMax = new Vector2(rect.offsetMax.x + eventData.delta.x, rect.offsetMax.y);
                    break;
                case UIEdge.TopRightCorner://UI右上角
                    rect.offsetMax += eventData.delta;
                    break;
                case UIEdge.BottomRightCorner://UI右下角
                    rect.offsetMax = new Vector2(rect.offsetMax.x + eventData.delta.x, rect.offsetMax.y);
                    rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y + eventData.delta.y);
                    break;
                case UIEdge.TopLeftCorner://UI左上角
                    rect.offsetMax = new Vector2(rect.offsetMax.x, rect.offsetMax.y + eventData.delta.y);
                    rect.offsetMin = new Vector2(rect.offsetMin.x + eventData.delta.x, rect.offsetMin.y);
                    break;
                case UIEdge.BottomLeftCorner://UI左下角
                    rect.offsetMin += eventData.delta;
                    break;
            }
            if (rect.sizeDelta.x < MinSizeData.x)
            {
                rect.sizeDelta = new(MinSizeData.x, rect.sizeDelta.y);
            }
            if (rect.sizeDelta.y < MinSizeData.y)
            {
                rect.sizeDelta = new(rect.sizeDelta.x, MinSizeData.y);
            }
        }
        private void Update()
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, uicam))
            {
                //将鼠标的屏幕位置转为在该UI图片下的ui坐标
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, Input.mousePosition, uicam, out Vector2 mousePos);
                //进行UI边缘检测，判断鼠标位置在UI边缘的哪个位置，返回其边缘类型枚举
                currentUiEdge = UIEdgeRectangle.GetUIEdge(mousePos, rect);
                switch (currentUiEdge)//根据边缘类型的情况切换鼠标光标
                {
                    case UIEdge.None:
                        CursorManager.instance.SetDefaultCursor();
                        break;
                    case UIEdge.Up:
                        CursorManager.instance.SetCursor(CursorType.UpDown);
                        break;
                    case UIEdge.Down:
                        CursorManager.instance.SetCursor(CursorType.UpDown);
                        break;
                    case UIEdge.Left:
                        CursorManager.instance.SetCursor(CursorType.LeftRight);
                        break;
                    case UIEdge.Right:
                        CursorManager.instance.SetCursor(CursorType.LeftRight);
                        break;
                    case UIEdge.TopRightCorner:
                        CursorManager.instance.SetCursor(CursorType.RightOblique);
                        break;
                    case UIEdge.BottomRightCorner:
                        CursorManager.instance.SetCursor(CursorType.LeftOblique);
                        break;
                    case UIEdge.TopLeftCorner:
                        CursorManager.instance.SetCursor(CursorType.LeftOblique);
                        break;
                    case UIEdge.BottomLeftCorner:
                        CursorManager.instance.SetCursor(CursorType.RightOblique);
                        break;
                }
            }
        }
        public override void Reset()
        {
            base.Reset();
            MinSizeData = new(10, 10);
        }
    }

}
