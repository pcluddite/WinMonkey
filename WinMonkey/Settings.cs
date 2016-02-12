using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;

namespace WinMonkey {
    public class Settings {

        public string Path { get; set; }
        
        private XmlDocument doc;

        public XmlNode DocumentElement {
            get {
                return doc.DocumentElement;
            }
        }

        public XmlDocument Document {
            get {
                return doc;
            }
        }

        private const string ROOT = "config";

        public Settings() {
            doc = new XmlDocument();
        }

        public void Load(string path) {
            Path = path;
            Load();
        }

        public void Load() {
            try {
                doc.Load(Path);
            }
            catch (Exception ex) {
                if (ex is IOException || ex is XmlException) {
                    doc.LoadXml(string.Format("<{0}></{0}>", ROOT));
                }
                else {
                    throw;
                }
            }
        }

        public string GetString(string xpath) {
            return GetString(xpath, null);
        }

        public string GetString(string xpath, string defaultValue) {
            try {
                XmlNode n = doc.SelectSingleNode(ROOT + "/" + xpath);
                if (n == null) {
                    return defaultValue;
                }
                return n.InnerText;
            }
            catch (XPathException) {
                return defaultValue;
            }
        }

        public int GetInt(string xpath) {
            return GetInt(xpath, 0);
        }

        public int GetInt(string xpath, int defaultValue) {
            try {
                return int.Parse(GetString(xpath, ""));
            }
            catch (FormatException) {
                return defaultValue;
            }
        }

        public double GetDouble(string xpath) {
            return GetDouble(xpath, 0.0);
        }

        public double GetDouble(string xpath, double defaultValue) {
            try {
                return double.Parse(GetString(xpath, ""));
            }
            catch (FormatException) {
                return defaultValue;
            }
        }

        public bool GetBool(string xpath) {
            return GetBool(xpath, false);
        }

        public bool GetBool(string xpath, bool defaultValue) {
            try {
                return bool.Parse(GetString(xpath, ""));
            }
            catch (FormatException) {
                return defaultValue;
            }
        }

        public void Save() {
            doc.Save(Path);
        }

        public void Save(string path) {
            doc.Save(Path = path);
        }

        public void Set(string xpath, string s) {
            XmlNode node = FindOrCreate(doc.DocumentElement, xpath.Split('/'), 0);
            node.InnerText = s;
        }

        public void Set(string xpath, object o) {
            Set(xpath, o + "");
        }

        private XmlNode FindOrCreate(XmlNode root, string[] path, int index) {
            XmlNode n = root.SelectSingleNode(path[index]);
            if (n == null) {
                n = doc.CreateElement(path[index]);
                root.AppendChild(n);
            }
            if (++index < path.Length) {
                return FindOrCreate(n, path, index);
            }
            else {
                return n;
            }
        }
    }
}
