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
    public class ApplicationPoolHelperTests
    {

        [TestMethod]
        public void FindApplicationPools()
        {
            int x = AppPoolHelper.GetIisMajorVersion();

            //Assert.AreEqual(7, x);

            var b = AppPoolHelper.AppPoolAlreadyExists("DefaultAppPool");

            //Assert.IsTrue(b);
        }
    }
}
