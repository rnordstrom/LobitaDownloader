using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

namespace LobitaDownloader
{
    public class XmlManager : IConfigManager
    {
        private XDocument doc;
        private const string configFile = "lobitaconfig.xml";

        public XmlManager()
        {
            doc = XDocument.Load(Path.Join(Constants.WorkingDirectory, configFile));
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
