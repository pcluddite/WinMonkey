/**
 *     WinMonkey
 *     Copyright (C) 2014-2021 Timothy Baxendale
 *     
 *     This program is free software; you can redistribute it and/or modify
 *     it under the terms of the GNU General Public License as published by
 *     the Free Software Foundation; either version 2 of the License, or
 *     (at your option) any later version.
 *     
 *     This program is distributed in the hope that it will be useful,
 *     but WITHOUT ANY WARRANTY; without even the implied warranty of
 *     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *     GNU General Public License for more details.
 *     
 *     You should have received a copy of the GNU General Public License along
 *     with this program; if not, write to the Free Software Foundation, Inc.,
 *     51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 *     
**/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;
using WinMonkey.Forms;
using WinMonkey.Properties;

namespace WinMonkey
{
    internal class AppRuntime : ApplicationContext
    {
        private const string RUN_KEY_PATH = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private readonly string SettingsPath = Path.Combine(Application.StartupPath, "config.xml");

        public ConfigureForm ConfigureForm { get; private set; }
        public NotifyIcon Icon { get; private set; }
        
        public Settings SettingsFile { get; private set; } = new Settings();
        public ScriptManager ScriptManager { get; private set; }

        public bool ShowNotifications
        {
            get => menuItemShowNotification.Checked;
        }

        public bool RunOnStartup
        {
            get {
                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH)) {
                    return runKey.GetValueNames().Contains("WinMonkey");
                }
            }
            set {
                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, true)) {
                    if (value) {
                        runKey.SetValue("WinMonkey", "\"" + Application.ExecutablePath + "\" --startup");
                    }
                    else if (runKey.GetValueNames().Contains("WinMonkey")) {
                        runKey.DeleteValue("WinMonkey");
                    }
                }
            }
        }

        private readonly ToolStripMenuItem menuItemConfigure;
        private readonly ToolStripMenuItem menuItemShowNotification;
        private readonly ToolStripMenuItem menuItemRunOnStartup;
        private readonly ToolStripMenuItem menuItemExit;

        public AppRuntime()
            : this(showOnStart: false)
        {
        }

        public AppRuntime(bool showOnStart)
        {
            SettingsFile.Load(SettingsPath);

            ContextMenuStrip contextMenu = new ContextMenuStrip();

            menuItemConfigure = new ToolStripMenuItem() {
                Text = "&Configure",
                CheckOnClick = false
            };
            menuItemConfigure.Click += MenuItemConfigure_Click;
            contextMenu.Items.Add(menuItemConfigure);

            menuItemShowNotification = new ToolStripMenuItem() {
                Text = "&Show Notifications",
                CheckOnClick = true,
                CheckState = SettingsFile.GetBool("notify") ? CheckState.Checked : CheckState.Unchecked
            };
            contextMenu.Items.Add(menuItemShowNotification);

            menuItemRunOnStartup = new ToolStripMenuItem() {
                Text = "&Run on Startup",
                CheckOnClick = true,
                CheckState = RunOnStartup ? CheckState.Checked : CheckState.Unchecked
            };
            menuItemRunOnStartup.CheckedChanged += MenuItemRunOnStartup_CheckedChanged;
            
            contextMenu.Items.Add(menuItemRunOnStartup);

            contextMenu.Items.Add(new ToolStripSeparator());

            menuItemExit = new ToolStripMenuItem() {
                Text = "E&xit WinMonkey",
                CheckOnClick = false
            };
            menuItemExit.Click += MenuItemExit_Click;
            contextMenu.Items.Add(menuItemExit);

            Icon = new NotifyIcon() {
                Icon = Resources.IconGear,
                ContextMenuStrip = contextMenu,
                Visible = true
            };

            LoadScripts();

            if (showOnStart) {
                ShowConfig();
            }
        }

        public void LoadScripts()
        {
            if (ScriptManager != null) {
                ScriptManager.Watcher.EndWatch();
            }
            IList<Script> scripts = new List<Script>();
            foreach (XmlNode nodeScript in SettingsFile.DocumentElement.SelectNodes("scripts/script")) {
                Script script = Script.FromXmlNode(nodeScript);
                if (script != null) {
                    scripts.Add(script);
                }
            }
            ScriptManager = new ScriptManager(Icon, scripts);
        }

        public void ShowConfig()
        {
            if (ConfigureForm == null) {
                ConfigureForm = new ConfigureForm(this);
                ConfigureForm.FormClosed += ConfigureForm_FormClosed;
                ConfigureForm.Show();
            }
            else {
                ConfigureForm.BringToFront();
                ConfigureForm.Focus();
            }
        }

        private void ConfigureForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            ConfigureForm.FormClosed -= ConfigureForm_FormClosed;
            ConfigureForm = null;
        }

        private void MenuItemConfigure_Click(object sender, EventArgs e)
        {
            ShowConfig();
        }

        private void MenuItemRunOnStartup_CheckedChanged(object sender, EventArgs e)
        {
            try {
                RunOnStartup = ((ToolStripMenuItem)sender).Checked;
            }
            catch(SecurityException) {
                ShowWarning("You do not have permission to run this program on startup.");
            }
        }

        private void MenuItemExit_Click(object sender, EventArgs e)
        {
            if (AskYesNo("If you close WindowMonkey, it will be unable to listen to events and run scripts automatically. Are you sure you want to quit?") == DialogResult.Yes) {
                SettingsFile.Save();
                ConfigureForm?.Close();
                Icon.Visible = false;
                Icon.Dispose();
                ExitThread();
            }
        }

        private DialogResult AskYesNo(string text)
        {
            return MessageBox.Show(ConfigureForm, text, Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }

        private void ShowWarning(string text)
        {
            MessageBox.Show(ConfigureForm, text, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
