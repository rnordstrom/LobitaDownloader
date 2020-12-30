using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LobitaDownloader.Tests
{
    [TestClass]
    public class ConfigTests
    {
        [TestMethod]
        public void TestCheckAutoMode()
        {
            IConfigManager cm = new XmlConfigManager();

            foreach (string cmd in Resources.ImageCmdHandles)
            {
                Assert.IsTrue(cm.CheckAutoMode(cmd) == AutoMode.AUTO || cm.CheckAutoMode(cmd) == AutoMode.MANUAL);
            }
        }
    }
}
