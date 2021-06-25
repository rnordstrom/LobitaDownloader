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
            _fileName = Path.Join(Environment.GetEnvironmentVariable("CONFIG_LOCATION"), dirName, fileName);

            doc.Load(_fileName);
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
