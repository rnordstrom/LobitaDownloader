using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LobitaDownloader.Tests
{
    [TestClass]
    public class ConfigTests
    {
        [TestMethod]
        public void TestCheckAutoMode()
        {
            IConfigManager cm = new XmlConfigManager("lobitaconfig.xml");

            foreach (string cmd in Resources.ImageCmdHandles)
            {
                Assert.IsTrue(cm.CheckAutoMode(cmd) == AutoMode.AUTO || cm.CheckAutoMode(cmd) == AutoMode.MANUAL);
            }
        }

        [TestMethod]
        public void TestGetSetByName()
        {
            IConfigManager cm = new XmlConfigManager("indexconfig.xml");
            string elementName = "NumThreads";

            int numThreads = int.Parse(cm.GetItemByName(elementName));
            
            if (numThreads > 2)
            {
                numThreads /= 2;
            }
            else
            {
                numThreads *= 2;
            }

            cm.ChangeItemByName(elementName, numThreads.ToString());

            int newNumThreads = int.Parse(cm.GetItemByName(elementName));

            Assert.AreEqual(numThreads, newNumThreads);
        }
    }
}
