using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security;
using System.Windows.Forms;
using System.Xml;

namespace WinMonkey
{
    public partial class Configure : Form
    {
        private ScriptManager scriptMan;
        private PipeWatcher pipeWatcher;
        private Settings settings;

        private bool showform;
        private bool skipCloseWarning = false;

        private readonly string SettingsPath = Path.Combine(Application.StartupPath, "Settings.xml");
        private const string RUN_KEY_PATH = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public Configure(bool show)
        {
            showform = show;
            pipeWatcher = new PipeWatcher(this);
            pipeWatcher.Begin();
            settings = new Settings();
            InitializeComponent();
            settings.Load(SettingsPath);

            showTrayMessagesToolStripMenuItem.Checked = settings.GetBool("notify", true);

            checkBox1.Checked = IsStartup();
            List<Script> scripts = new List<Script>();
            foreach (XmlNode nodeScript in settings.DocumentElement.SelectNodes("scripts/script")) {
                Script script = Script.FromXmlNode(nodeScript);
                if (script != null) {
                    ListViewItem item = new ListViewItem();
                    item.Text = script.TriggerName;
                    item.Tag = script.FilePath;
                    item.SubItems.Add(script.TriggerEvent.Name);
                    item.SubItems.Add(Path.GetFileName(script.FilePath));
                    scriptListView.Items.Add(item);
                    scripts.Add(script);
                }
            }

            if (showTrayMessagesToolStripMenuItem.Checked) {
                scriptMan = new ScriptManager(notifyIcon, scripts);
            }
            else {
                scriptMan = new ScriptManager(null, scripts);
            }

            enableButton_Click(null, EventArgs.Empty);
        }

        private void Configure_Load(object sender, EventArgs e)
        {

        }

        protected override void SetVisibleCore(bool value)
        {
            if (showform) {
                base.SetVisibleCore(value);
            }
            else {
                base.SetVisibleCore(false);
                showform = true;
            }
        }

        private void Configure_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!skipCloseWarning && e.CloseReason == CloseReason.UserClosing) {
                DialogResult result = MessageBox.Show(this, "If you close WindowMonkey, it will be unable to listen to events and run scripts automatically. Are you sure you want to quit?", "WindowMonkey", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No) {
                    e.Cancel = true;
                    skipCloseWarning = false;
                    return;
                }
            }
            RunOnStartup(checkBox1.Checked);

            settings.Set("notify", showTrayMessagesToolStripMenuItem.Checked);

            XmlNode scriptsNode = settings.DocumentElement.SelectSingleNode("scripts");
            if (scriptsNode == null) {
                scriptsNode = settings.Document.CreateElement("scripts");
                settings.DocumentElement.AppendChild(scriptsNode);
            }
            else {
                scriptsNode.RemoveAll();
            }

            foreach (Script s in scriptMan.GetScripts()) {
                scriptsNode.AppendChild(s.ToXmlNode(settings.Document));
            }

            try {
                settings.Save(SettingsPath);
            }
            catch (Exception ex) {
                if (MessageBox.Show(this, ex.Message + "\nBasically, there was a problem saving your settings file. If you exit now, your settings won't be saved. Do you still want to quit?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.No) {
                    e.Cancel = true;
                    return;
                }
            }
            notifyIcon.Visible = false;
        }

        private void Configure_FormClosed(object sender, FormClosedEventArgs e)
        {
            try {
                scriptMan.Watcher.EndWatch();
                PipeWatcher.SendBreakRequest();
            }
            catch {
            }
            finally {
                //Environment.Exit(0);
            }
        }

        private void RunOnStartup(bool doRun)
        {
            try {
                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, true)) {
                    if (doRun) {
                        runKey.SetValue("WinMonkey", "\"" + Application.ExecutablePath + "\" -startup");
                    }
                    else if (runKey.GetValueNames().Contains("WinMonkey")) {
                        runKey.DeleteValue("WinMonkey");
                    }
                }
            }
            catch (SecurityException) {
                MessageBox.Show("You do not have permission to run this program on startup. Try running as an administrator.", "WindowMonkey", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool IsStartup()
        {
            using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH)) {
                return runKey.GetValueNames().Contains("WinMonkey");
            }
        }

        private void enableButton_Click(object sender, EventArgs e)
        {
            if (scriptMan.Watcher.Running) {
                scriptMan.Watcher.EndWatch();
                enableButton.BackColor = Color.FromArgb(0, 192, 0);
                enableButton.Text = "Enable";
            }
            else {
                scriptMan.Watcher.BeginWatch();
                enableButton.BackColor = Color.FromArgb(192, 0, 0);
                enableButton.Text = "Disable";
            }
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            if (scriptListView.SelectedItems.Count == 1) {
                DialogResult result = MessageBox.Show(this, "Are you sure you want to delete this event?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes) {
                    scriptListView.Items.Remove(scriptListView.SelectedItems[0]);
                }
            }
            else if (scriptListView.SelectedItems.Count > 1) {
                DialogResult result = MessageBox.Show(this, "Are you sure you want to delete these " +
                    scriptListView.SelectedItems.Count +
                " events?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes) {
                    List<ListViewItem> toRemove = new List<ListViewItem>();
                    foreach (ListViewItem item in scriptListView.SelectedItems) {
                        toRemove.Add(item);
                    }
                    foreach (ListViewItem item in toRemove) {
                        scriptListView.Items.Remove(item);
                    }
                }
            }
            ResetAssociations();
        }

        private void Configure_SizeChanged(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized) {
                Hide();
                WindowState = FormWindowState.Normal;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void exitWindowMonkeyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            skipCloseWarning = true;
            Close();
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void runOnWindowsStartupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            checkBox1.Checked = runOnWindowsStartupToolStripMenuItem.Checked;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            runOnWindowsStartupToolStripMenuItem.Checked = checkBox1.Checked;
        }

        private void newButton_Click(object sender, EventArgs e)
        {
            ListViewItem item = new ListViewItem("");
            item.SubItems.Add("");
            item.SubItems.Add("");
            EditScript scriptEditor = new EditScript(item);
            if (scriptEditor.ShowDialog() == DialogResult.Yes) {
                scriptListView.Items.Add(item);
                ResetAssociations();
            }
        }

        private void editMenuItem_Click(object sender, EventArgs e)
        {
            if (scriptListView.SelectedItems.Count > 0) {
                EditScript scriptEditor = new EditScript(scriptListView.SelectedItems[0]);
                if (scriptEditor.ShowDialog() == DialogResult.Yes) {
                    ResetAssociations();
                }
            }
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            editMenuItem_Click(sender, e);
        }

        private void ResetAssociations()
        {
            List<Script> scripts = new List<Script>();
            foreach (ListViewItem item in scriptListView.Items) {
                scripts.Add(FromListViewItem(item));
            }
            scriptMan.ResetAssociations(scripts);
        }

        private static Script FromListViewItem(ListViewItem item)
        {
            return new Script(
                    item.Tag + "",
                    item.Text,
                    SysEvent.GetFromShort(item.SubItems[1].Text)
                    );

        }

        private void showTrayMessagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            scriptMan.NotifyIcon = showTrayMessagesToolStripMenuItem.Checked ? notifyIcon : null;
        }

        private void contextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (scriptListView.SelectedItems.Count == 0) {
                toolStripSeparator2.Visible = false;
                deleteEventToolStripMenuItem.Visible = false;
                editToolStripMenuItem.Visible = false;
            }
            else if (scriptListView.SelectedItems.Count == 1) {
                toolStripSeparator2.Visible = true;
                deleteEventToolStripMenuItem.Visible = true;
                editToolStripMenuItem.Visible = true;
            }
            else {
                toolStripSeparator2.Visible = true;
                deleteEventToolStripMenuItem.Visible = true;
                editToolStripMenuItem.Visible = false;
            }
        }
    }
}