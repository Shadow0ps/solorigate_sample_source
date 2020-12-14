using System;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.BusinessLayer.MaintUpdateNotifySvc;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.DALs;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000022 RID: 34
	public class CustomerEnvironmentManager
	{
		// Token: 0x06000320 RID: 800 RVA: 0x00013D4C File Offset: 0x00011F4C
		public static CustomerEnvironmentInfoPack GetEnvironmentInfoPack()
		{
			CustomerEnvironmentInfoPack customerEnvironmentInfoPack = new CustomerEnvironmentInfoPack();
			OperatingSystem osversion = Environment.OSVersion;
			customerEnvironmentInfoPack.OSVersion = osversion.VersionString;
			MaintenanceRenewalsCheckStatusDAL checkStatus = MaintenanceRenewalsCheckStatusDAL.GetCheckStatus();
			customerEnvironmentInfoPack.LastUpdateCheck = ((checkStatus != null && checkStatus.LastUpdateCheck != null) ? checkStatus.LastUpdateCheck.Value : DateTime.MinValue);
			customerEnvironmentInfoPack.OrionDBVersion = DatabaseInfoDAL.GetOrionDBVersion();
			customerEnvironmentInfoPack.SQLVersion = DatabaseInfoDAL.GetSQLEngineVersion();
			customerEnvironmentInfoPack.Modules = MaintUpdateNotifySvcWrapper.GetModules(ModulesCollector.GetInstalledModules());
			customerEnvironmentInfoPack.CustomerUniqueId = ModulesCollector.GetCustomerUniqueId();
			return customerEnvironmentInfoPack;
		}
	}
}
