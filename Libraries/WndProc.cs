using System;
using System.Runtime.InteropServices;
using WinRT.Interop;
using WinRT;

namespace CodeBlocks.Core
{
    public static class WindowProc
    {
        public delegate IntPtr WinProc(IntPtr hWnd, PInvoke.User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam);
        private static WinProc newWndProc = null;
        private static IntPtr oldWndProc = IntPtr.Zero;

        private static IntPtr hwnd;
        private static int MinWidth = 0;
        private static int MinHeight = 0;

        [DllImport("user32.dll")]
        static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, PInvoke.User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam);

        public static void SetWndMinSize(IntPtr _hwnd, int _width, int _height)
        {
            hwnd = _hwnd;
            MinWidth = _width;
            MinHeight = _height;
            SubClassing();
        }

        private static void SubClassing()
        {
            if (hwnd == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to get window handler: error code {error}");
            }

            newWndProc = new(NewWindowProc);

            // Here we use the NativeMethods class
            oldWndProc = NativeMethods.SetWindowLong(hwnd, PInvoke.User32.WindowLongIndexFlags.GWL_WNDPROC, newWndProc);
            if (oldWndProc == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to set GWL_WNDPROC: error code {error}");
            }
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
            public static IntPtr SetWindowLong(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, WinProc newProc)
            {
                if (IntPtr.Size == 4) // 32-bit process
                {
                    return SetWindowLong32(hWnd, nIndex, newProc);
                }
                else // 64-bit process
                {
                    return SetWindowLong64(hWnd, nIndex, newProc);
                }
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
        
        private static IntPtr NewWindowProc(IntPtr hWnd, PInvoke.User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam)
        {
            switch (Msg)
            {
                case PInvoke.User32.WindowMessage.WM_GETMINMAXINFO:
                    var dpi = PInvoke.User32.GetDpiForWindow(hWnd);
                    float scale = (float)dpi / 96;

                    MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    minMaxInfo.ptMinTrackSize.x = (int)(MinWidth * scale);
                    minMaxInfo.ptMinTrackSize.y = (int)(MinHeight * scale);
                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                    break;

            }
            return CallWindowProc(oldWndProc, hWnd, Msg, wParam, lParam);
        }
    }
}
