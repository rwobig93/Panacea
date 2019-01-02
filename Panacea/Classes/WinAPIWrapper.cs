using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.ComponentModel;
using HWND = System.IntPtr;
using INTER = System.Windows.Interop;

namespace Panacea.Classes
{
    public class WinAPIWrapper
    {
        #region Windows/Handles

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

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

        public static class SWP
        {
            public static readonly int
            NOSIZE = 0x0001,
            NOMOVE = 0x0002,
            NOZORDER = 0x0004,
            NOREDRAW = 0x0008,
            NOACTIVATE = 0x0010,
            DRAWFRAME = 0x0020,
            FRAMECHANGED = 0x0020,
            SHOWWINDOW = 0x0040,
            HIDEWINDOW = 0x0080,
            NOCOPYBITS = 0x0100,
            NOOWNERZORDER = 0x0200,
            NOREPOSITION = 0x0200,
            NOSENDCHANGING = 0x0400,
            DEFERERASE = 0x2000,
            ASYNCWINDOWPOS = 0x4000;
        }

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int smIndex);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern bool SetWindowPos(IntPtr hwnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        // Delegate to filter which windows to include 
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary> Get the text for the window pointed to by hWnd </summary>
        public static string GetWindowText(IntPtr hWnd)
        {
            int size = GetWindowTextLength(hWnd);
            if (size > 0)
            {
                var builder = new StringBuilder(size + 1);
                GetWindowText(hWnd, builder, builder.Capacity);
                return builder.ToString();
            }

            return String.Empty;
        }

        /// <summary> Find all windows that match the given filter </summary>
        /// <param name="filter"> A delegate that returns true for windows
        ///    that should be returned and false for windows that should
        ///    not be returned </param>
        public static IEnumerable<IntPtr> FindWindows(EnumWindowsProc filter)
        {
            IntPtr found = IntPtr.Zero;
            List<IntPtr> windows = new List<IntPtr>();

            EnumWindows(delegate (IntPtr wnd, IntPtr param)
            {
                if (filter(wnd, param))
                {
                    // only add the windows that pass the filter
                    windows.Add(wnd);
                }

                // but return true here so that we iterate all windows
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        /// <summary> Find all windows that contain the given title text </summary>
        /// <param name="titleText"> The text that the window title must contain. </param>
        public static IEnumerable<IntPtr> FindWindowsWithText(string titleText)
        {
            return FindWindows(delegate (IntPtr wnd, IntPtr param)
            {
                return GetWindowText(wnd).Contains(titleText);
            });
        }

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
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx", CharSet = CharSet.Auto)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        public static List<IntPtr> GetAllChildrenWindowHandles(IntPtr hParent, int maxCount)
        {
            List<IntPtr> result = new List<IntPtr>();
            int ct = 0;
            IntPtr prevChild = IntPtr.Zero;
            IntPtr currChild = IntPtr.Zero;
            while (true && ct < maxCount)
            {
                currChild = FindWindowEx(hParent, prevChild, null, null);
                if (currChild == IntPtr.Zero) break;
                result.Add(currChild);
                prevChild = currChild;
                ++ct;
            }
            return result;
        }

        public const uint WM_GETTEXT = 0x000D;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam,
            StringBuilder lParam);

        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        public static IEnumerable<IntPtr> EnumerateProcessWindowHandles(int processId)
        {
            var handles = new List<IntPtr>();
            foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
                EnumThreadWindows(thread.Id,
                    (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);
            return handles;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        public static Process GetProcessFromWinItem(WindowItem windowItem)
        {
            if (windowItem.WindowInfo.Title == "*")
                return Process.GetProcesses().ToList().Find(x => x.ProcessName == windowItem.WindowInfo.Name && x.MainModule.FileName == windowItem.WindowInfo.FileName && x.MainModule.ModuleName == windowItem.WindowInfo.ModName);
            else
                return Process.GetProcesses().ToList().Find(x => x.ProcessName == windowItem.WindowInfo.Name && x.MainWindowTitle == windowItem.WindowInfo.Title && x.MainModule.FileName == windowItem.WindowInfo.FileName && x.MainModule.ModuleName == windowItem.WindowInfo.ModName);
        }

        public static List<Process> GetProcessListFromWinItem(WindowItem windowItem)
        {
            List<Process> procList = new List<Process>();
            if (windowItem.WindowInfo.Title == "*")
                foreach (var proc in Process.GetProcesses().ToList())
                {
                    if (proc.ProcessName == windowItem.WindowInfo.Name &&
                        proc.MainModule.FileName == windowItem.WindowInfo.FileName &&
                        proc.MainModule.ModuleName == windowItem.WindowInfo.ModName)
                        procList.Add(proc);
                }
            else
                foreach (var proc in Process.GetProcesses().ToList())
                {
                    if (proc.ProcessName == windowItem.WindowInfo.Name &&
                        proc.MainWindowTitle == windowItem.WindowInfo.Title &&
                        proc.MainModule.FileName == windowItem.WindowInfo.FileName &&
                        proc.MainModule.ModuleName == windowItem.WindowInfo.ModName)
                        procList.Add(proc);
                }
            return procList;
        }

        public static bool ProcIsWhatWeAreLookingFor(WindowItem windowItem, Process proc)
        {
            if (windowItem.WindowInfo.Title == "*")
            {
                if (proc.ProcessName == windowItem.WindowInfo.Name &&
                    proc.MainModule.FileName == windowItem.WindowInfo.FileName &&
                    proc.MainModule.ModuleName == windowItem.WindowInfo.ModName)
                    return true;
                else
                    return false;
            }
            else
            {
                if (proc.ProcessName == windowItem.WindowInfo.Name &&
                    proc.MainWindowTitle == windowItem.WindowInfo.Title &&
                    proc.MainModule.FileName == windowItem.WindowInfo.FileName &&
                    proc.MainModule.ModuleName == windowItem.WindowInfo.ModName)
                    return true;
                else
                    return false;
            }
        }

        public static Rectangle GetWindowRect(IntPtr hWnd)
        {
            Debug.Assert(hWnd != IntPtr.Zero);
            RECT rect = new RECT();
            if (GetWindowRect(hWnd, ref rect) == false)
                throw new Exception("GetWindowRect failed");
            return rect.ToRectangle();
        }

        public static bool EnumProc(IntPtr hWnd, ref SearchData data)
        {
            // Check classname and title
            // This is different from FindWindow() in that the code below allows partial matches
            StringBuilder sb = new StringBuilder(1024);
            GetClassName(hWnd, sb, sb.Capacity);
            if (sb.ToString().StartsWith(data.Wndclass))
            {
                sb = new StringBuilder(1024);
                GetWindowText(hWnd, sb, sb.Capacity);
                if (sb.ToString().StartsWith(data.Title))
                {
                    data.hWnd = hWnd;
                    return false;    // Found the wnd, halt enumeration
                }
            }
            return true;
        }

        private delegate bool EnumWindowsProcc(IntPtr hWnd, ref SearchData data);

        public class SearchData
        {
            public string Wndclass;
            public string Title;
            public IntPtr hWnd;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProcc lpEnumFunc, ref SearchData data);


        /// <summary>Contains functionality to get all the open windows.</summary>
        public static class OpenWindowGetter
        {
            /// <summary>Returns a dictionary that contains the handle and title of all the open windows.</summary>
            /// <returns>A dictionary that contains the handle and title of all the open windows.</returns>
            public static IDictionary<HWND, string> GetOpenWindows()
            {
                HWND shellWindow = GetShellWindow();
                Dictionary<HWND, string> windows = new Dictionary<HWND, string>();

                EnumWindows(delegate (HWND hWnd, int lParam)
                {
                    if (hWnd == shellWindow) return true;
                    if (!IsWindowVisible(hWnd)) return true;

                    int length = GetWindowTextLength(hWnd);
                    if (length == 0) return true;

                    StringBuilder builder = new StringBuilder(length);
                    GetWindowText(hWnd, builder, length + 1);

                    windows[hWnd] = builder.ToString();
                    return true;

                }, 0);

                return windows;
            }

            private delegate bool EnumWindowsProc(HWND hWnd, int lParam);

            [DllImport("USER32.DLL")]
            private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

            [DllImport("USER32.DLL")]
            private static extern int GetWindowText(HWND hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("USER32.DLL")]
            private static extern int GetWindowTextLength(HWND hWnd);

            [DllImport("USER32.DLL")]
            private static extern bool IsWindowVisible(HWND hWnd);

            [DllImport("USER32.DLL")]
            private static extern IntPtr GetShellWindow();

            public static List<SystemHandleInformation> GetHandles(Process process)
            {
                var nHandleInfoSize = 0x10000;
                var ipHandlePointer = Marshal.AllocHGlobal(nHandleInfoSize);
                var nLength = 0;
                IntPtr ipHandle;

                while ((NtQuerySystemInformation(CnstSystemHandleInformation, ipHandlePointer, nHandleInfoSize, ref nLength)) == StatusInfoLengthMismatch)
                {
                    nHandleInfoSize = nLength;
                    Marshal.FreeHGlobal(ipHandlePointer);
                    ipHandlePointer = Marshal.AllocHGlobal(nLength);
                }

                byte[] baTemp = new byte[nLength];
                CopyMemory(baTemp, ipHandlePointer, (uint)nLength);

                long lHandleCount;
                if (Is64Bits())
                {
                    lHandleCount = Marshal.ReadInt64(ipHandlePointer);
                    ipHandle = new IntPtr(ipHandlePointer.ToInt64() + 8);
                }
                else
                {
                    lHandleCount = Marshal.ReadInt32(ipHandlePointer);
                    ipHandle = new IntPtr(ipHandlePointer.ToInt32() + 4);
                }

                SystemHandleInformation shHandle;
                List<SystemHandleInformation> lstHandles = new List<SystemHandleInformation>();

                for (long lIndex = 0; lIndex < lHandleCount; lIndex++)
                {
                    shHandle = new SystemHandleInformation();
                    if (Is64Bits())
                    {
                        shHandle = (SystemHandleInformation)Marshal.PtrToStructure(ipHandle, shHandle.GetType());
                        ipHandle = new IntPtr(ipHandle.ToInt64() + Marshal.SizeOf(shHandle) + 8);
                    }
                    else
                    {
                        ipHandle = new IntPtr(ipHandle.ToInt64() + Marshal.SizeOf(shHandle));
                        shHandle = (SystemHandleInformation)Marshal.PtrToStructure(ipHandle, shHandle.GetType());
                    }
                    if (shHandle.ProcessID != process.Id) continue;
                    lstHandles.Add(shHandle);
                }
                return lstHandles;

            }

            static bool Is64Bits()
            {
                return Marshal.SizeOf(typeof(IntPtr)) == 8;
            }

            [DllImport("ntdll.dll")]
            public static extern uint NtQuerySystemInformation(
            int systemInformationClass,
            IntPtr systemInformation,
            int systemInformationLength,
            ref int returnLength);

            [DllImport("kernel32.dll", EntryPoint = "RtlCopyMemory")]
            static extern void CopyMemory(byte[] destination, IntPtr source, uint length);

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SystemHandleInformation
            { // Information Class 16
                public int ProcessID;
                public byte ObjectTypeNumber;
                public byte Flags; // 0x01 = PROTECT_FROM_CLOSE, 0x02 = INHERIT
                public ushort Handle;
                public int Object_Pointer;
                public UInt32 GrantedAccess;
            }

            const int CnstSystemHandleInformation = 16;
            const uint StatusInfoLengthMismatch = 0xc0000004;
        }

        #endregion

        #region Mouse

        public static System.Drawing.Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new System.Drawing.Point(w32Mouse.X, w32Mouse.Y);
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
        public static IntPtr SearchForWindow(string wndclass, string title)
        {
            SearchData sd = new SearchData { Wndclass = wndclass, Title = title };
            EnumWindows(new EnumWindowsProcc(EnumProc), ref sd);
            return sd.hWnd;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        #endregion

        #region HotKey

        [DllImport("User32.dll")]
        public static extern bool RegisterHotKey([In] IntPtr hWnd, [In] int id, [In] uint fsModifiers, [In] uint vk);

        [DllImport("User32.dll")]
        public static extern bool UnregisterHotKey([In] IntPtr hWnd, [In] int id);

        public const int HOTKEY_ID = 9000;

        #endregion
    }
}
