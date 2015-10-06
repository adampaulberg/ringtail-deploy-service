using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using InstallerService;
using InstallerService.Helpers;
using System.ServiceModel;

namespace InstallerServiceTests.HelperTests
{
    [TestClass]
    public class SecurityModelTests
    {
        [TestMethod]
        public void Create_NoKeys_UseSecurityFalse()
        {
            var configs = new Dictionary<string, string>();
            var result = SecurityModel.Create(configs);
            Assert.IsFalse(result.UseSecurity);
        }

        [TestMethod]
        public void Create_SecurityEnabledFalse_UseSecurityFalse()
        {
            var configs = new Dictionary<string, string>() 
            { 
                { EnvironmentInfo.KeySecurityEnabled, "false" }
            };
            var result = SecurityModel.Create(configs);
            Assert.IsFalse(result.UseSecurity);
        }

        [TestMethod]
        public void Create_SecurityEnabledTrue_UseSecurityTrue()
        {
            var configs = new Dictionary<string, string>() 
            { 
                { EnvironmentInfo.KeySecurityEnabled, "true" }
            };
            var result = SecurityModel.Create(configs);
            Assert.IsTrue(result.UseSecurity);
        }

        [TestMethod]
        public void Create_SecurityModeSet_SetsSecurityMode()
        {
            var configs = new Dictionary<string, string>() 
            { 
                { EnvironmentInfo.KeySecurityEnabled, "true" },
                { EnvironmentInfo.KeySecurityMode, "Ntlm" }
            };
            var result = SecurityModel.Create(configs);
            Assert.AreEqual(HttpClientCredentialType.Ntlm, result.SecurityMode);
        }

        [TestMethod]
        public void Create_SecurityModeUnset_DefaultsSecurityMode()
        {
            var configs = new Dictionary<string, string>() 
            { 
                { EnvironmentInfo.KeySecurityEnabled, "true" },
                { EnvironmentInfo.KeySecurityMode, "" }
            };
            var result = SecurityModel.Create(configs);
            Assert.AreEqual(HttpClientCredentialType.Basic, result.SecurityMode);
        }

        [TestMethod]
        public void Create_SecurityModeBad_DefaultsSecurityMode()
        {
            var configs = new Dictionary<string, string>() 
            { 
                { EnvironmentInfo.KeySecurityEnabled, "true" },
                { EnvironmentInfo.KeySecurityMode, "ASDF" }
            };
            var result = SecurityModel.Create(configs);
            Assert.AreEqual(HttpClientCredentialType.Basic, result.SecurityMode);
        }

        [TestMethod]
        public void Create_UseSSLTrue_SSLEnabled()
        {
            var configs = new Dictionary<string, string>() 
            { 
                { EnvironmentInfo.KeySecurityEnabled, "true" },
                { EnvironmentInfo.KeySSLEnabled, "true" }
            };
            var result = SecurityModel.Create(configs);
            Assert.IsTrue(result.UseSSL);
        }

        [TestMethod]
        public void Create_UseSSLUnset_SSLEnabled()
        {
            var configs = new Dictionary<string, string>() 
            { 
                { EnvironmentInfo.KeySecurityEnabled, "true" },
                { EnvironmentInfo.KeySSLEnabled, "" }
            };
            var result = SecurityModel.Create(configs);
            Assert.IsTrue(result.UseSSL);
        }

        [TestMethod]
        public void Create_UseSSLFalse_SSLDisabled()
        {
            var configs = new Dictionary<string, string>() 
            { 
                { EnvironmentInfo.KeySecurityEnabled, "true" },
                { EnvironmentInfo.KeySSLEnabled, "false" }
            };
            var result = SecurityModel.Create(configs);
            Assert.IsFalse(result.UseSSL);
        }
    }
}
