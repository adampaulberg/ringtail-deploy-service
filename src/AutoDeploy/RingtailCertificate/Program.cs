using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.AccessControl;
using System.IO;

namespace RingtailCertificate
{
	public class Certificates
	{
		public static void Main(string[] args)
		{
			if (args.Length == 2)
			{
				string certPath = args[0];
				string Pass = args[1];
				Certificates cert = new Certificates();
				if (!cert.Test())
				{
					cert.Import(certPath, Pass);
					Environment.Exit(0);
				}
				else
				{
					Console.WriteLine("Already done!");
					Environment.Exit(0);
				}
			}
			else
			{
				Console.WriteLine("Please enter both arguments");
				Environment.Exit(1);
			}
		}
		public void Import(string certPath, string Pass)
		{
			//string SecurePassword = System.Convert.FromBase64String(System.Convert.FromBase64String(Pass).ToString()).ToString();
			string result = "";
			X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
			X509Store storeTrusted = new X509Store(StoreName.TrustedPeople, StoreLocation.LocalMachine);
			if (!File.Exists(certPath)) { result += " [ File doesnt exist: " + certPath + " ] "; }
			try
			{
				store.Open(OpenFlags.MaxAllowed);
			}
			catch
			{
				result += "Could not connect to store";
			}
			try
			{
				X509Certificate2 cert;
				cert = new X509Certificate2(
					certPath,
					Pass,
					X509KeyStorageFlags.Exportable |
					X509KeyStorageFlags.MachineKeySet |
					X509KeyStorageFlags.PersistKeySet
				);
				try
				{
					store.Add(cert);
				}
				catch
				{
					result += " [ Could not add cert to Store ] ";
					Console.WriteLine(result);
				}
			}
			catch
			{
				result += "Failed to Import to Store";
				Console.WriteLine(result);
				Environment.Exit(1);
			}
			finally
			{
				store.Close();
				result += " [ Imported Store Successfully ] ";
			}
			try
			{
				storeTrusted.Open(OpenFlags.MaxAllowed);
			}
			catch
			{
				result += "Could not connect to storeTrusted";
				Console.WriteLine(result);
				Environment.Exit(1);
			}
			try
			{
				X509Certificate2 cert;
				cert = new X509Certificate2(
					certPath,
					Pass,
					X509KeyStorageFlags.Exportable |
					X509KeyStorageFlags.MachineKeySet |
					X509KeyStorageFlags.PersistKeySet
				);
				storeTrusted.Add(cert);
			}
			catch
			{
				result += "Failed to Import to StoreTrusted";
				Console.WriteLine(result);
				Environment.Exit(1);
			}
			finally
			{
				result += " [ Imported StoreTrusted Successfully ] ";
				storeTrusted.Close();
			}
			SetPermissions(certPath, result);
		}
		public void SetPermissions(string certPath, string result)
		{
			X509Certificate2 cert = null;
			X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
			try
			{
				try
				{
					store.Open(OpenFlags.MaxAllowed);
				}
				catch
				{
					result += " [ Could not open Store in Set Permissions ] ";
					Console.WriteLine(result);
					Environment.Exit(1);
				}
				X509Certificate2Collection Collection = store.Certificates.Find(X509FindType.FindBySubjectName, "RingtailSTS", false);
				result += " [ Results: " + Collection.Count + " ] ";
				cert = Collection[0];
				RSACryptoServiceProvider rsa = cert.PrivateKey as RSACryptoServiceProvider;
				string ProgramData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
				string keypath = ProgramData + "\\Microsoft\\Crypto\\RSA\\MachineKeys\\" + rsa.CspKeyContainerInfo.UniqueKeyContainerName;
				FileSystemAccessRule rules = new FileSystemAccessRule("IIS_IUSRS", FileSystemRights.Read, AccessControlType.Allow);
				FileInfo file = new FileInfo(keypath);
				FileSecurity sec = file.GetAccessControl();
				try
				{
					sec.AddAccessRule(rules);
					try
					{
						sec.SetAccessRule(rules);
						file.SetAccessControl(sec);
					}
					catch
					{
						result += " [ Could not set access rule ] ";
						Console.WriteLine(result);
						Environment.Exit(1);
					}
					result += "[ Set access rules successfully ]";
				}
				catch
				{
					result += "[ Could not set permissions on IIS_IUSRS for ]" + keypath;
					Console.WriteLine(result);
					Environment.Exit(1);
				}
			}
			catch
			{
				store.Open(OpenFlags.MaxAllowed);
				X509Certificate2Collection Collection = store.Certificates.Find(X509FindType.FindBySubjectName, "CN=RingtailSTS", false);
				cert = Collection[0];
				int certCT = Collection.Count;
				RSACryptoServiceProvider rsa = cert.PrivateKey as RSACryptoServiceProvider;
				string ProgramData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
				string keypath = ProgramData + "\\Microsoft\\Crypto\\RSA\\MachineKeys\\" + rsa.CspKeyContainerInfo.UniqueKeyContainerName;
				store.Close();
				result += "[ Failed to Set Permissions ]";
				result += "[ Total Collection: " + certCT + " ]";
				if (!File.Exists(keypath))
				{
					result += "[ Key not found in path: " + keypath + " ]";
				}
				Console.WriteLine(result);
				Environment.Exit(1);
			}
			finally
			{
				store.Close();
				Console.WriteLine(result);
			}
		}
		public bool Test()
		{
			X509Certificate2 cert = null;
			X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
			try
			{
				store.Open(OpenFlags.MaxAllowed);
				X509Certificate2Collection Collection = store.Certificates.Find(X509FindType.FindBySubjectName, "CN=RingtailSTS", false);
				cert = Collection[0];
				if (string.IsNullOrEmpty(cert.ToString()))
				{
					return false;
				}
				else
				{
					return true;
				}
			}
			catch
			{
				return false;
			}
			finally
			{
				store.Close();
			}

		}
	}
}
