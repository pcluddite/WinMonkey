using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WinMonkey
{
    public class Window : IEquatable<Window>
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private Queue<SysEvent> pending;

        public IntPtr Handle { get; private set; }

        private string title;
        public string Title
        {
            get {
                return title;
            }
            private set {
                if (!title.Equals(value)) {
                    title = value;
                    pending.Enqueue(SysEvent.OnWindowTitleChange);
                }
            }
        }

        private bool minimized;
        public bool Minimized
        {
            get {
                return minimized;
            }
            private set {
                if (!minimized && value) {
                    pending.Enqueue(SysEvent.OnWindowMinimize);
                }
                minimized = value;
            }
        }

        private bool maximized;
        public bool Maximized
        {
            get {
                return maximized;
            }
            private set {
                if (!maximized && value) {
                    pending.Enqueue(SysEvent.OnWindowMaximize);
                }
                maximized = value;
            }
        }

        private bool visible;
        public bool Visible
        {
            get {
                return visible;
            }
            private set {
                if (visible != value) {
                    if (value) {
                        pending.Enqueue(SysEvent.OnWindowShow);
                    }
                    else {
                        pending.Enqueue(SysEvent.OnWindowHide);
                    }
                    visible = value;
                }
            }
        }

        private bool foreground;
        public bool Foreground
        {
            get {
                return foreground;
            }
            private set {
                if (foreground != value) {
                    if (value) {
                        pending.Enqueue(SysEvent.OnWindowFocus);
                    }
                    else {
                        pending.Enqueue(SysEvent.OnWindowNoFocus);
                    }
                    foreground = value;
                }
            }
        }

        private bool exists;
        public bool Exists
        {
            get {
                return exists;
            }
            private set {
                if (exists != value) {
                    if (value) {
                        pending.Enqueue(SysEvent.OnWindowOpen);
                    }
                    else {
                        pending.Enqueue(SysEvent.OnWindowClose);
                    }
                    exists = value;
                }
            }
        }

        public int QueueCount
        {
            get {
                return pending.Count;
            }
        }

        public Window(IntPtr hwnd, bool foreground)
        {
            pending = new Queue<SysEvent>();
            Handle = hwnd;
            exists = true;
            visible = IsWindowVisible(hwnd);
            //enabled = IsWindowEnabled(hwnd);
            this.foreground = foreground;

            WINDOWPLACEMENT plac = new WINDOWPLACEMENT();
            plac.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
            GetWindowPlacement(hwnd, ref plac);

            minimized = (plac.showCmd == 2);
            maximized = (plac.showCmd == 3);

            title = GetTitle();
        }

        public void Update(bool foreground)
        {
            Foreground = foreground;
            RefreshState(Handle);
        }

        private string GetTitle()
        {
            int len = GetWindowTextLength(Handle) + 1;
            if (len > 0) {
                StringBuilder sb = new StringBuilder(len);
                GetWindowText(Handle, sb, len);
                return sb.ToString();
            }
            else {
                return "";
            }
        }

        public SysEvent Dequeue()
        {
            return pending.Dequeue();
        }

        public bool IsTitleEmpty()
        {
            return Title == null || Title.Equals("");
        }

        private void RefreshState(IntPtr hwnd)
        {
            Visible = IsWindowVisible(hwnd);
            //Enabled = IsWindowEnabled(hwnd);

            WINDOWPLACEMENT plac = new WINDOWPLACEMENT();
            plac.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
            GetWindowPlacement(hwnd, ref plac);

            Minimized = (plac.showCmd == 2);
            Maximized = (plac.showCmd == 3);
            Title = GetTitle();
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        public bool Equals(Window other)
        {
            return Handle == other.Handle;
        }

        public override bool Equals(object obj)
        {
            if (obj is Window) {
                return Equals((Window)obj);
            }
            else {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        /*[DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowEnabled(IntPtr hWnd);*/
    }

    public struct W_STATES
    {
        const UInt32 SW_HIDE = 0;
        const UInt32 SW_SHOWNORMAL = 1;
        const UInt32 SW_NORMAL = 1;
        const UInt32 SW_SHOWMINIMIZED = 2;
        const UInt32 SW_SHOWMAXIMIZED = 3;
        const UInt32 SW_MAXIMIZE = 3;
        const UInt32 SW_SHOWNOACTIVATE = 4;
        const UInt32 SW_SHOW = 5;
        const UInt32 SW_MINIMIZE = 6;
        const UInt32 SW_SHOWMINNOACTIVE = 7;
        const UInt32 SW_SHOWNA = 8;
        const UInt32 SW_RESTORE = 9;
    }
}