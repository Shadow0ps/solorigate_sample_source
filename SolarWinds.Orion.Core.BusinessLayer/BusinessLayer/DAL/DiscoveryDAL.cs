using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Discovery.DataAccess;
using SolarWinds.Orion.Core.Models.OldDiscoveryModels;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000091 RID: 145
	public static class DiscoveryDAL
	{
		// Token: 0x060006F9 RID: 1785 RVA: 0x0002C5FE File Offset: 0x0002A7FE
		[Obsolete("This method belongs to old discovery process.", true)]
		public static StartImportStatus ImportDiscoveryResults(Guid importID, List<DiscoveryResult> discoveryResults)
		{
			return StartImportStatus.NothingToImport;
		}

		// Token: 0x060006FA RID: 1786 RVA: 0x0000AB3C File Offset: 0x00008D3C
		[Obsolete("This method belongs to old discovery process.", true)]
		public static bool IsImportInProgress(int discoveryProfileID)
		{
			return false;
		}

		// Token: 0x060006FB RID: 1787 RVA: 0x0002C601 File Offset: 0x0002A801
		[Obsolete("This method belongs to old discovery process.", true)]
		public static string GetCPUPollerTypeByOID(string oid)
		{
			return string.Empty;
		}

		// Token: 0x060006FC RID: 1788 RVA: 0x0002C608 File Offset: 0x0002A808
		[Obsolete("This method belongs to old discovery process.", true)]
		public static Intervals GetEnginesPollingIntervals(int engineID)
		{
			return new Intervals();
		}

		// Token: 0x060006FD RID: 1789 RVA: 0x0002C610 File Offset: 0x0002A810
		public static Intervals GetSettingsPollingIntervals()
		{
			return new Intervals
			{
				RediscoveryInterval = int.Parse(SettingsDAL.Get("SWNetPerfMon-Settings-Default Rediscovery Interval")),
				NodePollInterval = int.Parse(SettingsDAL.Get("SWNetPerfMon-Settings-Default Node Poll Interval")),
				VolumePollInterval = int.Parse(SettingsDAL.Get("SWNetPerfMon-Settings-Default Volume Poll Interval")),
				NodeStatPollInterval = int.Parse(SettingsDAL.Get("SWNetPerfMon-Settings-Default Node Stat Poll Interval")),
				VolumeStatPollInterval = int.Parse(SettingsDAL.Get("SWNetPerfMon-Settings-Default Volume Stat Poll Interval"))
			};
		}

		// Token: 0x060006FE RID: 1790 RVA: 0x0002C68C File Offset: 0x0002A88C
		public static List<SnmpEntry> GetAllCredentials()
		{
			SqlCommand textCommand = SqlHelper.GetTextCommand("\r\n    Select Distinct 1 As SnmpVersion, CommunityString, Null as SNMPUser, Null as Context, Null as AuthPassword, Null as EncryptPassword, \r\n0 as AuthLevel, Null as AuthMethod, 0 as EncryptMethod From dbo.DiscoverySNMPCredentials\r\nUnion\r\n(\r\n\tSELECT 3 As SnmpVersion, Null as CommunityString, SNMPUser, Context, AuthPassword, EncryptPassword, AuthLevel, AuthMethod, EncryptMethod \r\n\tFROM DiscoverySNMPCredentialsV3\r\n)");
			List<SnmpEntry> list = new List<SnmpEntry>
			{
				new SnmpEntry
				{
					Name = "public",
					Version = SNMPVersion.SNMP1,
					Selected = true
				},
				new SnmpEntry
				{
					Name = "private",
					Version = SNMPVersion.SNMP1,
					Selected = true
				}
			};
			using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
			{
				while (dataReader.Read())
				{
					string @string = DatabaseFunctions.GetString(dataReader, "CommunityString");
					int @int = DatabaseFunctions.GetInt32(dataReader, "SnmpVersion");
					if (!@string.Equals("public", StringComparison.OrdinalIgnoreCase) && !@string.Equals("private", StringComparison.OrdinalIgnoreCase))
					{
						if (@int == 3)
						{
							DiscoverySNMPCredentialsV3Entry.AuthenticationMethods int2 = DatabaseFunctions.GetInt32(dataReader, "AuthMethod");
							DiscoverySNMPCredentialsV3Entry.EncryptionMethods int3 = DatabaseFunctions.GetInt32(dataReader, "EncryptMethod");
							SnmpEntry snmpEntry = new SnmpEntry
							{
								UserName = DatabaseFunctions.GetString(dataReader, "SNMPUser"),
								Context = DatabaseFunctions.GetString(dataReader, "Context"),
								AuthPassword = DatabaseFunctions.GetString(dataReader, "AuthPassword"),
								PrivPassword = DatabaseFunctions.GetString(dataReader, "EncryptPassword"),
								AuthLevel = DatabaseFunctions.GetInt32(dataReader, "AuthLevel"),
								AuthMethod = ((int2 == 2) ? SnmpAuthMethod.SHA : SnmpAuthMethod.MD5),
								Version = SNMPVersion.SNMP3,
								Selected = true
							};
							switch (int3)
							{
							case 2:
								snmpEntry.PrivMethod = SnmpPrivMethod.AES;
								break;
							case 3:
								snmpEntry.PrivMethod = SnmpPrivMethod.AES192;
								break;
							case 4:
								snmpEntry.PrivMethod = SnmpPrivMethod.AES256;
								break;
							default:
								snmpEntry.PrivMethod = SnmpPrivMethod.DES;
								break;
							}
							list.Add(snmpEntry);
						}
						else
						{
							list.Add(new SnmpEntry
							{
								Name = @string,
								Version = SNMPVersion.SNMP1,
								Selected = true
							});
						}
					}
				}
			}
			return list;
		}

		// Token: 0x0400022C RID: 556
		public const string subTypeICMP = "ICMP";

		// Token: 0x0400022D RID: 557
		public const string subTypeSNMP = "SNMP";

		// Token: 0x0400022E RID: 558
		public const int UNDEFINED_VALUE = -2;
	}
}
