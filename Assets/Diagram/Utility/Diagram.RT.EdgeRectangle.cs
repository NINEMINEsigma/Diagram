using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Diagram
{
    /// <summary>
    /// UI边缘枚举
    /// </summary>
    public enum UIEdge
    {
        None,
        Up,
        Down,
        Left,
        Right,
        TopRightCorner,
        BottomRightCorner,
        TopLeftCorner,
        BottomLeftCorner
    }
    public static class UIEdgeRectangle
    {

        public static float areaPixel = 10.0f;//边缘检测区域大小
        private static Vector3[] corners = new Vector3[4];

        /// <summary>
        /// UI边缘检测算法，获取UI边缘枚举
        /// </summary>
        /// <param name="mousePos">鼠标点</param>
        /// <param name="rect">UI矩形框</param>
        /// <returns></returns>
        public static UIEdge GetUIEdge(Vector2 mousePos, RectTransform rect)
        {
            UIEdge uiEdge = UIEdge.None;//默认是None类型：表示中心区域
            rect.GetLocalCorners(corners);//获取UI矩形四个边缘角的局部坐标
                                          //上方矩形区域点
            var up0 = new Vector2(corners[1].x + areaPixel, corners[1].y - areaPixel);//与左边矩形区域共用
            var up1 = new Vector2(corners[1].x + areaPixel, corners[1].y);
            var up2 = new Vector2(corners[2].x - areaPixel, corners[2].y);
            var up3 = new Vector2(corners[2].x - areaPixel, corners[2].y - areaPixel);//与右边矩形区域共用
                                                                                      //下方矩形区域点
            var down0 = new Vector2(corners[0].x + areaPixel, corners[0].y);
            var down1 = new Vector2(corners[0].x + areaPixel, corners[0].y + areaPixel);//与左边矩形区域共用
            var down2 = new Vector2(corners[3].x - areaPixel, corners[3].y + areaPixel);//与右边矩形区域共用
            var down3 = new Vector2(corners[3].x - areaPixel, corners[3].y);
            //左边矩形区域点
            var left0 = new Vector2(corners[0].x, corners[0].y + areaPixel);
            var left1 = new Vector2(corners[1].x, corners[1].y - areaPixel);
            //右边矩形区域点
            var right0 = new Vector2(corners[2].x, corners[2].y - areaPixel);
            var right1 = new Vector2(corners[3].x, corners[3].y + areaPixel);
            if (IsPointInRectangle(up0, up1, up2, up3, mousePos))
            {
                uiEdge = UIEdge.Up;//鼠标点在up0、up1、up2和up3组成的矩形区域时返回枚举类型up：表示上方
            }
            else if (IsPointInRectangle(down0, down1, down2, down3, mousePos))
            {
                uiEdge = UIEdge.Down;//鼠标点在down0、down1、down2和down3组成的矩形区域时返回枚举类型down：表示下方
            }
            else if (IsPointInRectangle(down1, left0, left1, up0, mousePos))
            {
                uiEdge = UIEdge.Left;//鼠标点在down1、left0、left1和up0组成的矩形区域时返回枚举类型left：表示左边
            }
            else if (IsPointInRectangle(up3, right0, right1, down2, mousePos))
            {
                uiEdge = UIEdge.Right;//鼠标点在up3、right0、right1和down2组成的矩形区域时返回枚举类型right：表示下方
            }
            else if (IsPointInRectangle(up3, up2, corners[2], right0, mousePos))
            {
                uiEdge = UIEdge.TopRightCorner;//鼠标点在up3、up2、corners[2]和right0组成的矩形区域时返回枚举类型TopRightCorner：表示下方
            }
            else if (IsPointInRectangle(down2, right1, corners[3], down3, mousePos))
            {
                uiEdge = UIEdge.BottomRightCorner;//鼠标点在down2、right1、corners[3]和down3组成的矩形区域时返回枚举类型BottomRightCorner：表示下方
            }
            else if (IsPointInRectangle(left1, corners[1], up1, up0, mousePos))
            {
                uiEdge = UIEdge.TopLeftCorner;//鼠标点在left1、corners[1]、up1和up0组成的矩形区域时返回枚举类型TopLeftCorner：表示下方
            }
            else if (IsPointInRectangle(corners[0], left0, down1, down0, mousePos))
            {
                uiEdge = UIEdge.BottomLeftCorner;//鼠标点在corners[0]、left1、down1和down0组成的矩形区域时返回枚举类型BottomLeftCorner：表示下方
            }
            return uiEdge;
        }

        // 计算两个向量的叉积
        public static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - b.x * a.y;
        }

        // 判断点E是否在ABCD组成的矩形框内
        public static bool IsPointInRectangle(Vector2 A, Vector2 B, Vector2 C, Vector2 D, Vector2 E)
        {
            //计算E与ABCD矩形的叉积
            bool value = Cross(A - B, A - E) * Cross(C - D, C - E) >= 0 && Cross(A - D, A - E) * Cross(C - B, C - E) >= 0;
            return value;
        }
    }

}
