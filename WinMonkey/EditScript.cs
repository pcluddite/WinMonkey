using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Forms = System.Windows.Forms;
using System.IO;

namespace WinMonkey {
    public partial class EditScript : Form {

        private ListViewItem item;

        public EditScript(ListViewItem item) {
            this.item = item;
            InitializeComponent();
        }

        private void EditScript_Load(object sender, EventArgs e) {
            textBox1.Text = item.SubItems[2].Text;
            if (item.Tag != null) {
                openFileDialog1.FileName = item.Tag.ToString();
                label4.Text = "Script Configuration";
            }
            comboBox2.SelectedIndex = 1;
            if (!item.SubItems[1].Text.Equals("")) {
                if (item.SubItems[1].Text.Contains("Process")) {
                    comboBox2.SelectedIndex = 0;
                }
                comboBox1.SelectedItem =
                    SysEvent.GetFromShort(item.SubItems[1].Text).PlainText.Present;
            }
            textBox2.Text = item.Text;
        }

        private void browseButton_Click(object sender, EventArgs e) {
            if (openFileDialog1.ShowDialog() == DialogResult.OK) {
                textBox1.Text = Path.GetFileName(openFileDialog1.FileName);
            }
        }

        private void cancelButton_Click(object sender, EventArgs e) {
            DialogResult = Forms.DialogResult.No;
            Close();
        }

        private void saveButton_Click(object sender, EventArgs e) {
            if (!File.Exists(openFileDialog1.FileName)) {
                toolTip1.Show("This file does not exist.", textBox1);
            }
            else if (comboBox1.SelectedItem == null) {
                toolTip1.Show("Scripts must be associated with an event.", comboBox1);
            }
            else if (textBox2.Text.Equals("")) {
                toolTip1.Show("The window/process must have a name", textBox2);
            }
            else {
                DialogResult = Forms.DialogResult.Yes;
                item.Text = textBox2.Text;
                item.Tag = openFileDialog1.FileName;
                item.SubItems[1].Text =  SysEvent.GetFromPlainText(comboBox1.SelectedItem.ToString()).Name;
                item.SubItems[2].Text = textBox1.Text;
                Close();
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e) {
            comboBox1.Items.Clear();
            if (comboBox2.SelectedIndex >= 0) {
                string text = comboBox2.SelectedItem.ToString();
                if (text.Equals("Window")) {
                    foreach (SysEvent v in SysEvent.AllEvents) {
                        if (v.Name.StartsWith("OnWindow")) {
                            comboBox1.Items.Add(v.PlainText.Present);
                        }
                    }
                }
                else {
                    foreach (SysEvent v in SysEvent.AllEvents) {
                        if (v.Name.StartsWith("OnProcess")) {
                            comboBox1.Items.Add(v.PlainText.Present);
                        }
                    }
                }
            }
        }
    }
}
