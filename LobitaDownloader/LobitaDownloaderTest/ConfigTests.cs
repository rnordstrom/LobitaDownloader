using Microsoft.VisualStudio.TestTools.UnitTesting;
using LobitaDownloader;

namespace LobitaDownloaderTest
{
    [TestClass]
    public class ConfigTests
    {
        [TestMethod]
        public void TestCheckAutoMode()
        {
            IConfigManager cm = new XmlManager();

            foreach (string cmd in Constants.ImageCmdHandles)
            {
                Assert.IsTrue(cm.CheckAutoMode(cmd) == AutoMode.AUTO || cm.CheckAutoMode(cmd) == AutoMode.MANUAL);
            }
        }
    }
}
