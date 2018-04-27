using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;
using System.ComponentModel;

namespace Panacea.Classes
{
    public class WinAPIWrapper
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public Rectangle ToRectangle()
            {
                return new Rectangle(Left, Top, Right - Left, Bottom - Top);
            }
        }

        public enum GetSystem_Metrics : int
        {
            SM_CXBORDER = 5,
            SM_CXFULLSCREEN = 16,
            SM_CYFULLSCREEN = 17
        }
        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int smIndex);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(Point point);

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr handle, ref Point point);

        [DllImport("user32.dll")]
        public static extern IntPtr ChildWindowFromPointEx(IntPtr hWndParent, Point pt, uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hwnd, ref Point lpPoint);

        [DllImport("kernel32.dll")]
        public static extern int GetProcessId(IntPtr handle);

        [DllImport("user32.dll")]
        public static extern bool IsChild(IntPtr hWndParent, IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        public enum GetWindow_Cmd : uint
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        public static System.Drawing.Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new System.Drawing.Point(w32Mouse.X, w32Mouse.Y);
        }

        public static Process GetProcessFromWinItem(WindowItem windowItem)
        {
            if (windowItem.WindowInfo.Title == "*")
                return Process.GetProcesses().ToList().Find(x => x.ProcessName == windowItem.WindowInfo.Name && x.MainModule.FileName == windowItem.WindowInfo.FileName && x.MainModule.ModuleName == windowItem.WindowInfo.ModName);
            else
                return Process.GetProcesses().ToList().Find(x => x.ProcessName == windowItem.WindowInfo.Name && x.MainWindowTitle == windowItem.WindowInfo.Title && x.MainModule.FileName == windowItem.WindowInfo.FileName && x.MainModule.ModuleName == windowItem.WindowInfo.ModName);
        }

        public static Rectangle GetWindowRect(IntPtr hWnd)
        {
            Debug.Assert(hWnd != IntPtr.Zero);
            RECT rect = new RECT();
            if (GetWindowRect(hWnd, ref rect) == false)
                throw new Exception("GetWindowRect failed");
            return rect.ToRectangle();
        }

        internal static class MouseHook
        {
            private delegate int HookProc(int nCode, int wParam, IntPtr lParam);
            private static int _mouseHookHandle;
            private static HookProc _mouseDelegate;

            private static event MouseUpEventHandler MouseUp;
            public static event MouseUpEventHandler OnMouseUp
            {
                add
                {
                    Subscribe();
                    MouseUp += value;
                }
                remove
                {
                    MouseUp -= value;
                    Unsubscribe();
                }
            }

            private static void Unsubscribe()
            {
                if (_mouseHookHandle != 0)
                {
                    int result = UnhookWindowsHookEx(_mouseHookHandle);
                    _mouseHookHandle = 0;
                    _mouseDelegate = null;
                    if (result == 0)
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        throw new Win32Exception(errorCode);
                    }
                }
            }

            private static void Subscribe()
            {
                if (_mouseHookHandle == 0)
                {
                    _mouseDelegate = MouseHookProc;
                    _mouseHookHandle = SetWindowsHookEx(WH_MOUSE_LL,
                        _mouseDelegate,
                        GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName),
                        0);
                    if (_mouseHookHandle == 0)
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        throw new Win32Exception(errorCode);
                    }
                }
            }

            private static int MouseHookProc(int nCode, int wParam, IntPtr lParam)
            {
                if (nCode >= 0)
                {
                    MSLLHOOKSTRUCT mouseHookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    if (wParam == WM_LBUTTONUP)
                    {
                        if (MouseUp != null)
                        {
                            MouseUp.Invoke(null, new Point(mouseHookStruct.pt.x, mouseHookStruct.pt.y));
                        }
                    }
                }
                return CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
            }

            private const int WH_MOUSE_LL = 14;
            private const int WM_LBUTTONUP = 0x0202;

            [StructLayout(LayoutKind.Sequential)]
            private struct POINT
            {
                public int x;
                public int y;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct MSLLHOOKSTRUCT
            {
                public POINT pt;
                public uint mouseData;
                public uint flags;
                public uint time;
                public IntPtr dwExtraInfo;
            }

            [DllImport("user32.dll", CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall, SetLastError = true)]
            private static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

            [DllImport("user32.dll", CharSet = CharSet.Auto,
               CallingConvention = CallingConvention.StdCall, SetLastError = true)]
            private static extern int UnhookWindowsHookEx(int idHook);

            [DllImport("user32.dll", CharSet = CharSet.Auto,
                 CallingConvention = CallingConvention.StdCall)]
            private static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);

            [DllImport("kernel32.dll")]
            public static extern IntPtr GetModuleHandle(string name);
        }

        public delegate void MouseUpEventHandler(object sender, Point p);
    }
}
