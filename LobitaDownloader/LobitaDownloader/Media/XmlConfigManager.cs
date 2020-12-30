using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

namespace LobitaDownloader
{
    public class XmlConfigManager : IConfigManager
    {
        private XDocument doc;
        private const string configFile = "lobitaconfig.xml";

        public XmlConfigManager()
        {
            doc = XDocument.Load(Path.Join(Resources.WorkingDirectory, configFile));
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
