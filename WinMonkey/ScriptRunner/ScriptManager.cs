using System.Collections.Generic;
using System.Windows.Forms;

namespace WinMonkey {
    public class ScriptManager {

        public SysWatcher Watcher { get; private set; }
        public NotifyIcon NotifyIcon {
            get {
                return SysEvent.Icon;
            }
            set {
                SysEvent.Icon = value;
            }
        }
        
        private List<SysEvent> allEvents;

        public ScriptManager(NotifyIcon icon, IEnumerable<Script> scripts) {
            NotifyIcon = icon;
            Watcher = new SysWatcher();
            allEvents = new List<SysEvent>(); 
            foreach (Script s in scripts) {
                AddScript(s);
            }
        }
        
        public void AddScript(Script script) {
            if (!allEvents.Contains(script.TriggerEvent)) {
                SysEvent info = script.TriggerEvent;
                switch (script.TriggerEvent.Id) {
                    case SysEvent.PROCEXIT:
                        Watcher.OnProcessExit += info.OnEvent;
                        break;
                    case SysEvent.PROCSTART:
                        Watcher.OnProcessStart += info.OnEvent;
                        break;
                    case SysEvent.WINCLOSE:
                        Watcher.OnWindowClose += info.OnEvent;
                        break;
                    case SysEvent.WINFOCUS:
                        Watcher.OnWindowFocus += info.OnEvent;
                        break;
                    case SysEvent.WINHIDE:
                        Watcher.OnWindowHide += info.OnEvent;
                        break;
                    case SysEvent.WINMAXIMIZE:
                        Watcher.OnWindowMaximize += info.OnEvent;
                        break;
                    case SysEvent.WINMINIMIZE:
                        Watcher.OnWindowMinimize += info.OnEvent;
                        break;
                    case SysEvent.WINNOFOCUS:
                        Watcher.OnWindowNoFocus += info.OnEvent;
                        break;
                    case SysEvent.WINOPEN:
                        Watcher.OnWindowOpen += info.OnEvent;
                        break;
                    case SysEvent.WINSHOW:
                        Watcher.OnWindowShow += info.OnEvent;
                        break;
                    case SysEvent.WINTITLECHANGE:
                        Watcher.OnWindowTitleChange += info.OnEvent;
                        break;
                }
                allEvents.Add(info);
            }
            script.TriggerEvent.AddScript(script);
        }

        public IEnumerable<Script> GetScripts() {
            List<Script> scripts = new List<Script>();
            foreach (SysEvent einfo in allEvents) {
                scripts.AddRange(einfo.GetScripts());
            }
            return scripts;
        }

        public void ResetAssociations(IEnumerable<Script> scripts) {
            Watcher.UnregisterEvents();
            foreach (SysEvent einfo in allEvents) {
                einfo.ClearScripts();
            }
            allEvents.Clear();
            foreach (Script s in scripts) {
                AddScript(s);
            }
        }
    }
}
