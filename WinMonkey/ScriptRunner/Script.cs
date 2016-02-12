using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Xml;
using Timer = System.Windows.Forms.Timer;

namespace WinMonkey
{
    public class Script
    {
        private static readonly string AutoItPath = Path.Combine(Application.StartupPath, "AutoIt3.exe");

        public ScriptLanguage Language
        {
            get; set;
        }

        public string FilePath
        {
            get; set;
        }

        public string TriggerName { get; set; }

        public SysEvent TriggerEvent { get; set; }

        public Script(string file)
        {
            FilePath = file;
            switch (Path.GetExtension(file).ToUpper()) {
                case ".AU3":
                    Language = ScriptLanguage.AutoIt3;
                    break;
                case ".JS":
                    Language = ScriptLanguage.JavaScript;
                    break;
                case ".VBS":
                    Language = ScriptLanguage.VBScript;
                    break;
                case ".TBASIC":
                case ".TBS":
                    Language = ScriptLanguage.TBASIC;
                    break;
                case ".BAT":
                case ".NT":
                case ".CMD":
                    Language = ScriptLanguage.Batch;
                    break;
                default:
                    Language = ScriptLanguage.Native;
                    break;
            }
        }

        public Script(string file, string triggerName, SysEvent triggerEvent)
            : this(file)
        {
            TriggerEvent = triggerEvent;
            TriggerName = triggerName;
        }

        public Script(string file, ScriptLanguage lang)
        {
            FilePath = file;
            Language = lang;
        }

        public Script(string file, ScriptLanguage lang, string triggerName, SysEvent triggerEvent)
            : this(file, lang)
        {
            TriggerEvent = triggerEvent;
            TriggerName = triggerName;
        }

        public void Run(NotifyIcon icon)
        {
            if (CheckSafety(icon)) {
                if (icon != null) {
                    icon.ShowBalloonTip(3000, "Window Monkey",
                        string.Format("Started '{0}' because a {1} with the name '{2}' {3}",
                                Path.GetFileName(FilePath),
                                (TriggerEvent.Name.Contains("Window") ? "window" : "process"),
                                TriggerName,
                                TriggerEvent.PlainText.Past
                        ),
                        ToolTipIcon.Info);
                }
                Run();
            }
        }

        private Timer graceTimer = null;
        private int last = 0;
        private bool bad()
        {
            if (last == 0) {
                graceTimer = new Timer();
                graceTimer.Interval = 2000; // how long to wait before starting new scripts
                graceTimer.Tick += time_Tick;
                graceTimer.Start();
            }
            return (Language == ScriptLanguage.Native && TriggerEvent == SysEvent.OnProcessStart &&
                Path.GetFileNameWithoutExtension(FilePath).Equals(TriggerName, StringComparison.OrdinalIgnoreCase))
                || (last++ > 3);
        }

        void time_Tick(object sender, EventArgs e)
        {
            last = 0;
            graceTimer.Stop();
            graceTimer.Dispose();
            graceTimer = null;
        }

        private bool CheckSafety(NotifyIcon icon)
        {
            if (bad()) {
                string warning = string.Format(
                    "A seriously bad thing was prevented here. '{0}' wasn't started because it looks like processes are going to be created forever, probably crashing your computer.",
                    Path.GetFileName(FilePath));
                if (icon == null) {
                    MessageBox.Show(warning, "Window Monkey", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else {
                    icon.ShowBalloonTip(3000, "Window Monkey",
                            warning,
                            ToolTipIcon.Warning
                            );
                }
                return false;
            }
            return true;
        }

        public void Run()
        {
            switch (Language) {
                case ScriptLanguage.AutoIt3:
                    RunAu3();
                    break;
                default:
                    RunNative(FilePath, "");
                    break;
            }
        }

        private void RunAu3()
        {
            if (File.Exists(AutoItPath)) {
                RunNative(AutoItPath, '"' + FilePath + '"');
            }
            else {
                MessageBox.Show("There was a problem starting your script. AutoIt3.exe is required to run it, and it couldn't be found.",
                    "Window Monkey", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RunNative(string filename, string args)
        {
            using (Process scriptProc = new Process()) {
                scriptProc.StartInfo.FileName = filename;
                scriptProc.StartInfo.Arguments = args;
                scriptProc.Start();
            }
        }

        public static Script FromXmlNode(XmlNode node)
        {

            Script script = new Script(node.SelectSingleNode("path").InnerText, ScriptLanguage.Native);

            try {
                script.Language = (ScriptLanguage)Enum.Parse(typeof(ScriptLanguage), node.SelectSingleNode("lang").InnerText);
            }
            catch {
                script.Language = ScriptLanguage.Native;
            }

            XmlNode nodeTrig = node.SelectSingleNode("trigger");

            int eventId = int.Parse(nodeTrig.Attributes["event"].InnerText);

            script.TriggerEvent = SysEvent.FromId(eventId);
            script.TriggerName = nodeTrig.InnerText;

            return script;
        }

        public XmlNode ToXmlNode(XmlDocument doc)
        {
            XmlElement xmlScript = doc.CreateElement("script");

            XmlElement xmlPath = doc.CreateElement("path");
            xmlPath.InnerText = FilePath;

            xmlScript.AppendChild(xmlPath);

            XmlElement xmlLang = doc.CreateElement("lang");
            xmlLang.InnerText = Language.ToString();

            xmlScript.AppendChild(xmlLang);

            XmlElement xmlTrigger = doc.CreateElement("trigger");
            xmlTrigger.SetAttribute("event", TriggerEvent.Id.ToString());
            xmlTrigger.InnerText = TriggerName;

            xmlScript.AppendChild(xmlTrigger);

            return xmlScript;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("path", FilePath);
            info.AddValue("lang", Language);
            info.AddValue("trigger", TriggerName);
            info.AddValue("event", TriggerEvent.Id);
        }
    }

    public enum ScriptLanguage
    {
        AutoIt3, JavaScript, TBASIC, VBScript, Batch, Native
    }
}