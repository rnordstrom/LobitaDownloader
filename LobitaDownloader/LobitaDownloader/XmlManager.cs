using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

namespace LobitaDownloader
{
    class XmlManager : IConfigManager
    {
        private XElement doc;
        private const string configDir = "config";
        private const string configFile = "lobitaconfig.xml";

        public XmlManager(string workingDir)
        {
            doc = XElement.Load(Path.Join(workingDir, configDir, configFile));
        }

        public AutoMode CheckAutoMode(string cmdHandle)
        {
            var element = doc.XPathSelectElement($"/commands/command/handle[text()='{cmdHandle}']/../automode");
            string mode = element.Value;

            if(mode == "AUTO")
            {
                return AutoMode.AUTO;
            }
            else
            {
                return AutoMode.MANUAL;
            }
        }
    }
}
