using System;
using System.Data.OleDb;
using System.IO;
using System.Text;
using SolarWinds.Logging;
using SolarWinds.NPM.Common.Models;
using SolarWinds.Orion.Core.Common.Models.Mib;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200002B RID: 43
	public static class MibHelper
	{
		// Token: 0x17000064 RID: 100
		// (get) Token: 0x06000361 RID: 865 RVA: 0x00014FA0 File Offset: 0x000131A0
		// (set) Token: 0x06000362 RID: 866 RVA: 0x00014FE0 File Offset: 0x000131E0
		private static OleDbConnection CurrentConnection
		{
			get
			{
				object obj = MibHelper.connectionLock;
				OleDbConnection result;
				lock (obj)
				{
					result = MibHelper.connection;
				}
				return result;
			}
			set
			{
				object obj = MibHelper.connectionLock;
				lock (obj)
				{
					MibHelper.connection = value;
				}
			}
		}

		// Token: 0x06000363 RID: 867 RVA: 0x00015020 File Offset: 0x00013220
		public static void ForceConnectionClose()
		{
			OleDbConnection.ReleaseObjectPool();
			MibHelper.CurrentConnection.Close();
		}

		// Token: 0x06000364 RID: 868 RVA: 0x00015034 File Offset: 0x00013234
		public static void CleanupDescription(Oid oid)
		{
			oid.Description = oid.Description.Replace("\r\n", "\n");
			oid.Description = oid.Description.Replace("\r", "\n");
			oid.Description = oid.Description.Replace("\n", "\r\n");
		}

		// Token: 0x06000365 RID: 869 RVA: 0x00015094 File Offset: 0x00013294
		public static void SetTypeInfo(Oid oid)
		{
			if (oid.Enums.Count > 0)
			{
				oid.PollType = CustomPollerType.RawValue;
				oid.VariableType = OidVariableType.HexValue;
				return;
			}
			string text = oid.StringType.ToUpper();
			uint num = <PrivateImplementationDetails>.ComputeStringHash(text);
			if (num <= 1675043358U)
			{
				if (num <= 1134674926U)
				{
					if (num <= 513955505U)
					{
						if (num != 80939203U)
						{
							if (num != 105277099U)
							{
								if (num != 513955505U)
								{
									goto IL_3FF;
								}
								if (!(text == "UNSIGNEDINTEGER32"))
								{
									goto IL_3FF;
								}
							}
							else if (!(text == "UINTEGER32"))
							{
								goto IL_3FF;
							}
						}
						else
						{
							if (!(text == "COUNTER"))
							{
								goto IL_3FF;
							}
							goto IL_3C1;
						}
					}
					else if (num != 562998736U)
					{
						if (num != 876943732U)
						{
							if (num != 1134674926U)
							{
								goto IL_3FF;
							}
							if (!(text == "OBJECT-IDENTIFIER"))
							{
								goto IL_3FF;
							}
							goto IL_3A3;
						}
						else
						{
							if (!(text == "IP ADDRESS"))
							{
								goto IL_3FF;
							}
							goto IL_3B2;
						}
					}
					else
					{
						if (!(text == "GAUGE"))
						{
							goto IL_3FF;
						}
						goto IL_3D0;
					}
				}
				else if (num <= 1435346685U)
				{
					if (num != 1318471353U)
					{
						if (num != 1353787078U)
						{
							if (num != 1435346685U)
							{
								goto IL_3FF;
							}
							if (!(text == "OBJECT IDENTIFIER"))
							{
								goto IL_3FF;
							}
							goto IL_3A3;
						}
						else
						{
							if (!(text == "COUNTER32"))
							{
								goto IL_3FF;
							}
							goto IL_3C1;
						}
					}
					else
					{
						if (!(text == "BITS"))
						{
							goto IL_3FF;
						}
						goto IL_394;
					}
				}
				else if (num != 1471704030U)
				{
					if (num != 1583614854U)
					{
						if (num != 1675043358U)
						{
							goto IL_3FF;
						}
						if (!(text == "OPAQUE"))
						{
							goto IL_3FF;
						}
						goto IL_394;
					}
					else
					{
						if (!(text == "OCTECTSTRING"))
						{
							goto IL_3FF;
						}
						goto IL_394;
					}
				}
				else
				{
					if (!(text == "TIMETICKS"))
					{
						goto IL_3FF;
					}
					oid.PollType = CustomPollerType.RawValue;
					oid.VariableType = OidVariableType.TimeTicks;
					return;
				}
			}
			else if (num <= 2976928712U)
			{
				if (num <= 2367501349U)
				{
					if (num != 1726884399U)
					{
						if (num != 1793657564U)
						{
							if (num != 2367501349U)
							{
								goto IL_3FF;
							}
							if (!(text == "INTEGER"))
							{
								goto IL_3FF;
							}
						}
						else
						{
							if (!(text == "IP"))
							{
								goto IL_3FF;
							}
							goto IL_3B2;
						}
					}
					else
					{
						if (!(text == "DISPLAY_STRING"))
						{
							goto IL_3FF;
						}
						goto IL_394;
					}
				}
				else if (num != 2562616708U)
				{
					if (num != 2815734233U)
					{
						if (num != 2976928712U)
						{
							goto IL_3FF;
						}
						if (!(text == "DISPLAYSTRING"))
						{
							goto IL_3FF;
						}
						goto IL_394;
					}
					else
					{
						if (!(text == "OBJECTIDENTIFIER"))
						{
							goto IL_3FF;
						}
						goto IL_3A3;
					}
				}
				else
				{
					if (!(text == "OBJECT_IDENTIFIER"))
					{
						goto IL_3FF;
					}
					goto IL_3A3;
				}
			}
			else if (num <= 3592098225U)
			{
				if (num != 3153626600U)
				{
					if (num != 3471757159U)
					{
						if (num != 3592098225U)
						{
							goto IL_3FF;
						}
						if (!(text == "IP-ADDRESS"))
						{
							goto IL_3FF;
						}
						goto IL_3B2;
					}
					else
					{
						if (!(text == "IP_ADDRESS"))
						{
							goto IL_3FF;
						}
						goto IL_3B2;
					}
				}
				else
				{
					if (!(text == "SEQUENCE"))
					{
						goto IL_3FF;
					}
					oid.PollType = CustomPollerType.RawValue;
					oid.VariableType = OidVariableType.ByteValue;
					return;
				}
			}
			else if (num != 3794377841U)
			{
				if (num != 3904720641U)
				{
					if (num != 4262624195U)
					{
						goto IL_3FF;
					}
					if (!(text == "OID"))
					{
						goto IL_3FF;
					}
					goto IL_3A3;
				}
				else
				{
					if (!(text == "COUNTER64"))
					{
						goto IL_3FF;
					}
					goto IL_3C1;
				}
			}
			else
			{
				if (!(text == "GAUGE32"))
				{
					goto IL_3FF;
				}
				goto IL_3D0;
			}
			oid.PollType = CustomPollerType.Rate;
			oid.VariableType = OidVariableType.Gauge;
			return;
			IL_394:
			oid.PollType = CustomPollerType.RawValue;
			oid.VariableType = OidVariableType.Text;
			return;
			IL_3A3:
			oid.PollType = CustomPollerType.RawValue;
			oid.VariableType = OidVariableType.Text;
			return;
			IL_3B2:
			oid.PollType = CustomPollerType.RawValue;
			oid.VariableType = OidVariableType.IPAddress;
			return;
			IL_3C1:
			oid.PollType = CustomPollerType.Counter;
			oid.VariableType = OidVariableType.Counter;
			return;
			IL_3D0:
			oid.PollType = CustomPollerType.Rate;
			oid.VariableType = OidVariableType.Gauge;
			return;
			IL_3FF:
			oid.PollType = CustomPollerType.RawValue;
			oid.VariableType = OidVariableType.None;
		}

		// Token: 0x06000366 RID: 870 RVA: 0x000154B0 File Offset: 0x000136B0
		public static OleDbConnection GetDBConnection()
		{
			if (string.IsNullOrEmpty(MibHelper.dbConnectionString))
			{
				StringBuilder stringBuilder = new StringBuilder("Provider=Microsoft.Jet.OLEDB.4.0;");
				stringBuilder.Append("Data Source=");
				stringBuilder.Append(MibHelper.FindMibDbPath() + "MIBs.cfg");
				stringBuilder.Append(";Mode=Read;OLE DB Services=-1;Persist Security Info=False;Jet OLEDB:Database ");
				stringBuilder.Append("Password=SW_MIBs");
				MibHelper.dbConnectionString = stringBuilder.ToString();
			}
			return MibHelper.CurrentConnection = new OleDbConnection(MibHelper.dbConnectionString);
		}

		// Token: 0x06000367 RID: 871 RVA: 0x00015528 File Offset: 0x00013728
		private static string FindMibDbPath()
		{
			string text = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\SolarWinds\\";
			if (File.Exists(text + "MIBs.cfg"))
			{
				return text;
			}
			MibHelper.log.DebugFormat("Could not find MIBs Database. Please, download MIBs Database from http://solarwinds.s3.amazonaws.com/solarwinds/Release/MIB-Database/MIBs.zip and decompress the MIBs.cfg file to " + text + " to correct this problem", Array.Empty<object>());
			throw new ApplicationException("Unable to determine Mibs.cfg location");
		}

		// Token: 0x06000368 RID: 872 RVA: 0x00015584 File Offset: 0x00013784
		public static bool IsMIBDatabaseAvailable()
		{
			bool result;
			try
			{
				MibHelper.FindMibDbPath();
				result = true;
			}
			catch (ApplicationException)
			{
				result = false;
			}
			return result;
		}

		// Token: 0x06000369 RID: 873 RVA: 0x000155B4 File Offset: 0x000137B4
		public static string FormatSearchCriteria(string searchCriteria)
		{
			string text = string.Empty;
			foreach (string text2 in searchCriteria.Split(new char[]
			{
				' '
			}))
			{
				if (!string.IsNullOrEmpty(text2.Trim()))
				{
					text = text + text2.Replace("*", "%") + " ";
				}
			}
			return text.TrimEnd(Array.Empty<char>());
		}

		// Token: 0x040000AE RID: 174
		private static OleDbConnection connection;

		// Token: 0x040000AF RID: 175
		private static object connectionLock = new object();

		// Token: 0x040000B0 RID: 176
		private static string dbConnectionString;

		// Token: 0x040000B1 RID: 177
		private static Log log = new Log();
	}
}
