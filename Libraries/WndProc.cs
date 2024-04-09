using System;
using System.Runtime.InteropServices;

namespace CodeBlocks.Core
{
    public static class WindowProc
    {
        public delegate IntPtr WinProc(IntPtr hWnd, PInvoke.User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam);
        private static IntPtr oldWndProc = IntPtr.Zero;

        private static IntPtr hwnd;
        private static int MinWidth = -1;
        private static int MinHeight = -1;
        private static int MaxWidth = -1;
        private static int MaxHeight = -1;

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, PInvoke.User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam);

        public static void SetWndMinSize(IntPtr _hwnd, int _width, int _height)
        {
            hwnd = _hwnd;
            MinWidth = _width;
            MinHeight = _height;
            SubClassing();
        }

        public static void SetWndMinMaxSize(IntPtr _hwnd, int _min_width, int _min_height, int _max_width, int _max_height)
        {
            hwnd = _hwnd;
            MinWidth = _min_width;
            MinHeight = _min_height;
            MaxWidth = _max_width;
            MaxHeight = _max_height;
            SubClassing();
        }

        private static void SubClassing()
        {
            if (hwnd == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to get window handler.");
            }

            oldWndProc = NativeMethods.SetWindowLong(hwnd, PInvoke.User32.WindowLongIndexFlags.GWL_WNDPROC);
            if (oldWndProc == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to set GWL_WNDPROC.");
            }
        }

        private static IntPtr NewWindowProc(IntPtr hWnd, PInvoke.User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam)
        {
            switch (Msg)
            {
                case PInvoke.User32.WindowMessage.WM_GETMINMAXINFO:
                    var dpi = PInvoke.User32.GetDpiForWindow(hWnd);
                    float scale = (float)dpi / 96;

                    MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    if (MinWidth != -1) minMaxInfo.ptMinTrackSize.x = (int)(MinWidth * scale);
                    if (MinHeight != -1) minMaxInfo.ptMinTrackSize.y = (int)(MinHeight * scale);
                    if (MaxWidth != -1) minMaxInfo.ptMaxTrackSize.x = (int)(MaxWidth * scale);
                    if (MaxHeight != -1) minMaxInfo.ptMaxTrackSize.y = (int)(MaxHeight * scale);
                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                    break;

            }
            return CallWindowProc(oldWndProc, hWnd, Msg, wParam, lParam);
        }

        public static class NativeMethods
        {
            // We have to handle the 32-bit and 64-bit functions separately.
            // 'SetWindowLongPtr' is the 64-bit version of 'SetWindowLong', and isn't available in user32.dll for 32-bit processes.
            [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
            private static extern IntPtr SetWindowLong32(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, WinProc newProc);

            [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
            private static extern IntPtr SetWindowLong64(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, WinProc newProc);

            // This does the selection for us, based on the process architecture.
            public static IntPtr SetWindowLong(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex)
            {
                if (IntPtr.Size == 4) // 32-bit process
                {
                    return SetWindowLong32(hWnd, nIndex, NewWindowProc);
                }
                if (IntPtr.Size == 8) // 64-bit process
                {
                    return SetWindowLong64(hWnd, nIndex, NewWindowProc);
                }
                return IntPtr.Zero;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MINMAXINFO
        {
            public PInvoke.POINT ptReserved;
            public PInvoke.POINT ptMaxSize;
            public PInvoke.POINT ptMaxPosition;
            public PInvoke.POINT ptMinTrackSize;
            public PInvoke.POINT ptMaxTrackSize;
        }
    }
}