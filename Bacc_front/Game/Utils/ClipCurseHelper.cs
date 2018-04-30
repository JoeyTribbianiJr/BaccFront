using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Utils
{
    public class ClipCurseHelper
    {

        [DllImport("user32.dll")]
        public static extern bool ClipCursor(ref RECT lpRect);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowRect")]
        extern static int GetWindowRect(int hwnd, ref RECT lpRect);

        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(Int32 left, Int32 top, Int32 right, Int32 bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }
        }
        /// <summary>
        /// 设置鼠标显示在主屏范围内
        /// </summary>
        /// <returns></returns>
        public static bool SetCursorInPrimaryScreen()
        {
            System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.AllScreens.OrderBy(t => t.WorkingArea.X).First();
            RECT rect = new RECT(screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Right + screen.Bounds.X, screen.Bounds.Bottom);
            return ClipCursor(ref rect);
        }
        /// <summary>
        /// 设置鼠标显示在某个范围
        /// </summary>
        /// <returns></returns>
        public static bool SetCursorInScreen(Screen screen)
        {
            //System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.AllScreens.OrderBy(t => t.WorkingArea.X).First();
            RECT rect = new RECT(screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Right + screen.Bounds.X, screen.Bounds.Bottom);
            return ClipCursor(ref rect);
        }
        /// <summary>
        /// 恢复鼠标显示，可以所以屏幕的任何位置
        /// </summary>
        /// <returns></returns>
        public static bool Default()
        {
            System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.AllScreens.OrderByDescending(t => t.WorkingArea.X).First();
            RECT rect = new RECT(0, 0, screen.Bounds.Right + screen.Bounds.X, screen.Bounds.Bottom);
            return ClipCursor(ref rect);
        }

    }
}
