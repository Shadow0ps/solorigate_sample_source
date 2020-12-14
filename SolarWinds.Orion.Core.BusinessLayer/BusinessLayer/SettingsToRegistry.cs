using System;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200003A RID: 58
	internal class SettingsToRegistry
	{
		// Token: 0x060003FB RID: 1019 RVA: 0x0001B938 File Offset: 0x00019B38
		public void Synchronize()
		{
			this.Synchronize(new SettingItem("SWNetPerfMon-Settings-SNMP-SocketRecyclingInterval"), "SNMP_SocketRecyclingInterval");
			this.Synchronize(new SettingItem("SWNetPerfMon-Settings-SNMP-SocketKeepAliveInterval"), "SNMP_SocketKeepAliveInterval");
		}

		// Token: 0x060003FC RID: 1020 RVA: 0x0001B964 File Offset: 0x00019B64
		public void Synchronize(SynchronizeItem item, string registryValueName)
		{
			try
			{
				SettingsToRegistry.log.VerboseFormat("Synchronize ... {0}", new object[]
				{
					item
				});
				object databaseValue = item.GetDatabaseValue();
				SettingsToRegistry.log.VerboseFormat("Synchronize ... {0} - value {1}", new object[]
				{
					item,
					databaseValue
				});
				if (databaseValue != null)
				{
					this.WriteToRegistry(registryValueName, databaseValue);
				}
			}
			catch (Exception ex)
			{
				SettingsToRegistry.log.Error(string.Format("Failed to synchronize {0}", item), ex);
				if (this.ThrowExceptions)
				{
					throw;
				}
			}
		}

		// Token: 0x040000E4 RID: 228
		private static readonly Log log = new Log();

		// Token: 0x040000E5 RID: 229
		internal Action<string, object> WriteToRegistry = delegate(string a, object b)
		{
			OrionConfiguration.SetSetting(a, b);
		};

		// Token: 0x040000E6 RID: 230
		internal bool ThrowExceptions;
	}
}
