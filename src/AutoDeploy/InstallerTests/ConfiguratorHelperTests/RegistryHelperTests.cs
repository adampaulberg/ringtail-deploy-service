using ConfiguratorHelper.App;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallerTests.ConfiguratorHelperTests
{
    [TestClass]
    public class RegistryHelperTests
    {
        [TestMethod]
        public void RegistryHelperTests_GetDBVersion()
        {
            var dbVersion = RegistryHelper.GetDBVersion();

            //Assert.AreEqual("08.0619.0128", dbVersion);
        }

        [TestMethod]
        public void RegistryHelperTests_GetCBVersion()
        {
            var dbVersion = RegistryHelper.GetCBVersion();

            //Assert.AreEqual("8.6.005.1560", dbVersion);
        }
    }
}
