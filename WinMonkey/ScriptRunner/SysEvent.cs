using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WinMonkey
{
    public class SysEvent
    {
        public const int NONE = 0;
        public const int WINOPEN = 1;
        public const int WINCLOSE = 2;
        public const int WINMINIMIZE = 3;
        public const int WINMAXIMIZE = 4;
        public const int WINTITLECHANGE = 5;
        public const int WINSHOW = 6;
        public const int WINHIDE = 7;
        public const int WINFOCUS = 8;
        public const int WINNOFOCUS = 9;
        public const int PROCSTART = 10;
        public const int PROCEXIT = 11;

        public static readonly SysEvent None = new SysEvent(NONE, "N/A", null);
        public static readonly SysEvent OnWindowOpen = new SysEvent(WINOPEN, "OnWindowOpen", new PassiveVerb("openned"));
        public static readonly SysEvent OnWindowClose = new SysEvent(WINCLOSE, "OnWindowClose", new PassiveVerb("closed"));
        public static readonly SysEvent OnWindowMinimize = new SysEvent(WINMINIMIZE, "OnWindowMinimize", new PassiveVerb("minimized"));
        public static readonly SysEvent OnWindowMaximize = new SysEvent(WINMAXIMIZE, "OnWindowMaximize", new PassiveVerb("maximized"));
        public static readonly SysEvent OnWindowTitleChange = new SysEvent(WINTITLECHANGE, "OnWindowTitleChange", new Verb("changes", "changed") { DirectObject = "its title" });
        public static readonly SysEvent OnWindowShow = new SysEvent(WINSHOW, "OnWindowShow", new PassiveVerb("shown"));
        public static readonly SysEvent OnWindowHide = new SysEvent(WINHIDE, "OnWindowHide", new PassiveVerb("hidden"));
        public static readonly SysEvent OnWindowFocus = new SysEvent(WINFOCUS, "OnWindowFocus", new Verb("gains", "gained") { DirectObject = "focus" });
        public static readonly SysEvent OnWindowNoFocus = new SysEvent(WINNOFOCUS, "OnWindowNoFocus", new Verb("loses", "lost") { DirectObject = "focus" });
        public static readonly SysEvent OnProcessStart = new SysEvent(PROCSTART, "OnProcessStart", new Verb("starts", "started"));
        public static readonly SysEvent OnProcessExit = new SysEvent(PROCEXIT, "OnProcessExit", new Verb("exits", "exitted"));


        public static readonly SysEvent[] AllEvents = {
                None,
                OnWindowOpen,
                OnWindowClose,
                OnWindowMinimize,
                OnWindowMaximize,
                OnWindowTitleChange,
                OnWindowShow,
                OnWindowHide,
                OnWindowFocus,
                OnWindowNoFocus,
                OnProcessStart,
                OnProcessExit
        };

        public static SysEvent GetFromShort(string name)
        {
            return (from v in AllEvents
                    where v.Name.Equals(name)
                    select v).First();
        }

        public static SysEvent GetFromPlainText(string verb)
        {
            return (from v in AllEvents
                    where v.PlainText != null && v.PlainText.Equals(verb)
                    select v).First();
        }

        public static string GetShortName(int opt)
        {
            return (from v in AllEvents
                    where v.Id == opt
                    select v).First().Name;
        }

        public static Verb GetPlainText(int opt)
        {
            return (from v in AllEvents
                    where v.Id == opt
                    select v).First().PlainText;
        }

        public static SysEvent FromId(int id)
        {
            return (from v in AllEvents
                    where v.Id == id
                    select v).First();
        }

        private List<Script> scripts;

        public static NotifyIcon Icon { get; set; }
        public int Id { get; private set; }
        public Verb PlainText { get; private set; }
        public string Name { get; private set; }

        private SysEvent(int id, string _name, Verb _verb)
        {
            Id = id;
            PlainText = _verb;
            Name = _name;
            scripts = new List<Script>();
        }
        public void ClearScripts()
        {
            scripts.Clear();
        }

        public void AddScript(Script s)
        {
            scripts.Add(s);
        }

        public void OnEvent(object sender, EventArgs e)
        {
            if (sender is Window) {
                Window w = (Window)sender;
                foreach (Script s in scripts) {
                    if (w.Title.Equals(s.TriggerName)) {
                        s.Run(Icon);
                    }
                }
            }
            else if (sender is MonkeyProc) {
                MonkeyProc p = (MonkeyProc)sender;
                foreach (Script s in scripts) {
                    if (p.Name.Equals(s.TriggerName)) {
                        s.Run(Icon);
                    }
                }
            }
        }

        public Script[] GetScripts()
        {
            return scripts.ToArray();
        }


        public class Verb : IEquatable<string>
        {

            private string pres;
            public virtual string Present
            {
                get {
                    return pres + " " + DirectObject;
                }
                set {
                    pres = value;
                }
            }
            private string past;
            public virtual string Past
            {
                get {
                    return past + " " + DirectObject;
                }
                set {
                    past = value;
                }
            }
            public virtual string DirectObject { get; set; }

            protected Verb()
            {
            }

            public Verb(string _present, string _past)
            {
                Present = _present;
                Past = _past;
            }

            public override string ToString()
            {
                return Present + " " + DirectObject;
            }

            public bool Equals(string other)
            {
                return other.Equals(Present) || other.Equals(Past);
            }
        }

        public class PassiveVerb : Verb
        {

            public string PresentAuxiliary { get; set; }
            public string PastAuxiliary { get; set; }
            public string PassiveParticple { get; set; }

            public override string Present
            {
                get {
                    return PresentAuxiliary + " " + PassiveParticple;
                }
                set {
                    throw new NotImplementedException();
                }
            }

            public override string Past
            {
                get {
                    return PastAuxiliary + " " + PassiveParticple;
                }
                set {
                    throw new NotImplementedException();
                }
            }

            public PassiveVerb(string _pparticple)
                : this("is", "was", _pparticple)
            {
            }

            public PassiveVerb(string _presentAux, string _pastAux, string _pparticiple)
            {
                PresentAuxiliary = _presentAux;
                PastAuxiliary = _pastAux;
                PassiveParticple = _pparticiple;
            }

            public override string ToString()
            {
                return PresentAuxiliary + " " + PassiveParticple;
            }
        }
    }
}