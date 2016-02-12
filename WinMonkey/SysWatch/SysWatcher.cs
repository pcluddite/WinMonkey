using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.ComponentModel;

namespace WinMonkey
{
    public class SysWatcher : IDisposable
    {
        public event EventHandler OnWindowOpen;
        public event EventHandler OnWindowClose;
        public event EventHandler OnWindowFocus;
        public event EventHandler OnWindowNoFocus;
        public event EventHandler OnWindowShow;
        public event EventHandler OnWindowHide;
        public event EventHandler OnWindowMinimize;
        public event EventHandler OnWindowMaximize;
        public event EventHandler OnWindowTitleChange;
        public event EventHandler OnProcessStart;
        public event EventHandler OnProcessExit;

        public bool Running
        {
            get {
                if (winListener == null || procListener == null) {
                    return false;
                }
                else {
                    return winListener.IsBusy || procListener.IsBusy;
                }
            }
        }

        public bool RequestingExit
        {
            get {
                if (!Running) {
                    return false;
                }
                return winListener.CancellationPending || procListener.CancellationPending;
            }
        }

        public bool Cancel { get; set; }

        private BackgroundWorker winListener, procListener;

        public void BeginWatch()
        {
            winListener = new BackgroundWorker() { WorkerSupportsCancellation = true };
            procListener = new BackgroundWorker() { WorkerSupportsCancellation = true };
            winListener.DoWork += WinWatcher;
            procListener.DoWork += ProcWatcher;
            winListener.RunWorkerAsync();
            procListener.RunWorkerAsync();
        }

        ~SysWatcher()
        {
            Dispose();
        }

        public void Dispose()
        {
            EndWatch();
            winListener.Dispose();
            procListener.Dispose();
        }

        public void EndWatch()
        {
            if (Running) {
                winListener.CancelAsync();
                procListener.CancelAsync();
                Cancel = true;
            }
        }

        public void UnregisterEvents()
        {
            OnProcessExit = null;
            OnProcessStart = null;
            OnWindowClose = null;
            OnWindowFocus = null;
            OnWindowHide = null;
            OnWindowMaximize = null;
            OnWindowMinimize = null;
            OnWindowNoFocus = null;
            OnWindowOpen = null;
            OnWindowShow = null;
            OnWindowTitleChange = null;
        }

        private void WinWatcher(object sender, DoWorkEventArgs e)
        {
            Dictionary<IntPtr, Window> knownWindows = GetWindows();

            while (!Cancel) {
                Thread.Sleep(100); // no cpu hogging
                IntPtr top = GetForegroundWindow();
                Stack<IntPtr> openWindows = WinEnum.Enum();

                var closed = knownWindows.Keys.Except(openWindows).ToArray(); // 3-21-15 It needs to be copied. Don't use the ExceptIterator

                if (OnWindowClose == null) {
                    foreach (IntPtr hwnd in closed) {
                        knownWindows.Remove(hwnd);
                    }
                }
                else {
                    foreach (IntPtr hwnd in closed) {
                        Window win = knownWindows[hwnd];
                        if (!win.IsTitleEmpty()) {
                            OnWindowClose.Invoke(win, EventArgs.Empty);
                        }
                        knownWindows.Remove(hwnd);
                    }
                }

                while (openWindows.Count > 0) {
                    IntPtr hwnd = openWindows.Pop();
                    Window win;
                    if (knownWindows.TryGetValue(hwnd, out win)) {
                        win.Update(top == hwnd);
                        while (win.QueueCount > 0) {
                            SysEvent sysEvent = win.Dequeue();
                            if (OnWindowMinimize != null && sysEvent == SysEvent.OnWindowMinimize) {
                                OnWindowMinimize.Invoke(win, EventArgs.Empty);
                            }
                            if (OnWindowMaximize != null && sysEvent == SysEvent.OnWindowMaximize) {
                                OnWindowMaximize.Invoke(win, EventArgs.Empty);
                            }
                            if (OnWindowTitleChange != null && sysEvent == SysEvent.OnWindowTitleChange) {
                                OnWindowTitleChange.Invoke(win, EventArgs.Empty);
                            }
                            if (OnWindowFocus != null && sysEvent == SysEvent.OnWindowFocus) {
                                OnWindowFocus.Invoke(win, EventArgs.Empty);
                            }
                            if (OnWindowNoFocus != null && sysEvent == SysEvent.OnWindowNoFocus) {
                                OnWindowNoFocus.Invoke(win, EventArgs.Empty);
                            }
                            if (OnWindowHide != null && sysEvent == SysEvent.OnWindowHide) {
                                OnWindowHide.Invoke(win, EventArgs.Empty);
                            }
                            if (OnWindowShow != null && sysEvent == SysEvent.OnWindowShow) {
                                OnWindowShow.Invoke(win, EventArgs.Empty);
                            }
                        }
                    }
                    else {
                        win = new Window(hwnd, top == hwnd);
                        if (OnWindowOpen != null && !win.IsTitleEmpty()) {
                            OnWindowOpen.Invoke(win, EventArgs.Empty);
                        }
                        knownWindows.Add(hwnd, win);
                    }
                }
            }
            e.Cancel = true;
        }

        private void ProcWatcher(object sender, DoWorkEventArgs e)
        {
            Dictionary<int, MonkeyProc> knownProcs = new Dictionary<int, MonkeyProc>();

            foreach (MonkeyProc p in MonkeyProc.Enum()) {
                knownProcs.Add(p.Id, p);
            }

            while (!Cancel) {
                Thread.Sleep(100); // don't hog all the cpu

                var current = MonkeyProc.Enum();

                var closed = knownProcs.Values.Except(current, MonkeyProc.ProcessComparer).ToArray(); // 3-21-15 Again, make a copy. The foreach iterator won't be const otherwise

                if (OnProcessExit == null) {
                    foreach (MonkeyProc p in closed) {
                        knownProcs.Remove(p.Id);
                    }
                }
                else {
                    foreach (MonkeyProc p in closed) {
                        OnProcessExit.Invoke(p, EventArgs.Empty);
                        knownProcs.Remove(p.Id);
                    }
                }

                if (OnProcessStart != null) {
                    foreach (MonkeyProc p in current) {
                        if (!knownProcs.ContainsKey(p.Id)) {
                            OnProcessStart.Invoke(p, EventArgs.Empty);
                            knownProcs.Add(p.Id, p);
                        }
                    }
                }
            }
            e.Cancel = true;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private Dictionary<IntPtr, Window> GetWindows()
        {
            Dictionary<IntPtr, Window> windows = new Dictionary<IntPtr, Window>();
            Stack<IntPtr> current = WinEnum.Enum();
            IntPtr top = GetForegroundWindow();
            while (current.Count > 0) {
                IntPtr hwnd = current.Pop();
                windows.Add(hwnd, new Window(hwnd, top == hwnd));
            }
            return windows;
        }

        internal class WinEnum
        {
            [DllImport("user32.dll")]
            private static extern int EnumWindows(CallBackPtr callPtr, int lPar);

            private delegate bool CallBackPtr(IntPtr hwnd, int lParam);

            private Stack<IntPtr> stack;
            private static CallBackPtr callBackPtr;

            private WinEnum()
            {
                stack = new Stack<IntPtr>();
            }

            private bool Report(IntPtr hwnd, int lParam)
            {
                stack.Push(hwnd);
                return true;
            }

            private Stack<IntPtr> GetWindows()
            {
                stack = new Stack<IntPtr>();
                callBackPtr = new CallBackPtr(Report);
                EnumWindows(callBackPtr, 0);
                return stack;
            }
            
            public static Stack<IntPtr> Enum()
            {
                return (new WinEnum()).GetWindows();
            }
        }
    }
}