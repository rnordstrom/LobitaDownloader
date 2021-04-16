using System;
using System.IO;
using System.Xml;

namespace LobitaDownloader
{
    public class XmlConfigManager : IConfigManager
    {
        private XmlDocument doc = new XmlDocument();
        private string _fileName;

        public XmlConfigManager(string dirName, string fileName)
        {
            _fileName = fileName;

            doc.Load(Path.Join(Environment.GetEnvironmentVariable("CONFIG_LOCATION"), dirName, fileName));
        }

        public AutoMode CheckAutoMode(string cmdHandle)
        {
            XmlNode root = doc.SelectSingleNode("commands");
            XmlNodeList commands = root.SelectNodes("command");
            string docHandle;
            string mode = "";

            foreach (XmlElement e in commands)
            {
                docHandle = e.SelectSingleNode("handle").InnerText;

                if (docHandle == cmdHandle)
                {
                    mode = e.SelectSingleNode("automode").InnerText;
                }
            }

            if (mode == "AUTO")
            {
                return AutoMode.AUTO;
            }
            else if (mode == "MANUAL")
            {
                return AutoMode.MANUAL;
            }
            else
            {
                return AutoMode.INDETERMINATE;
            }
        }

        public string GetItemByName(string name)
        {
            XmlNode root = doc.SelectSingleNode("items");

            return root.SelectSingleNode(name).InnerText;
        }

        public void ChangeItemByName(string name, string value)
        {
            XmlNode root = doc.SelectSingleNode("items");
            XmlNode oldNode = root.SelectSingleNode(name);
            XmlNode newNode = doc.CreateElement(string.Empty, name, string.Empty);

            newNode.AppendChild(doc.CreateTextNode(value));

            root.ReplaceChild(newNode, oldNode);
            doc.Save(_fileName);
        }
    }
}
