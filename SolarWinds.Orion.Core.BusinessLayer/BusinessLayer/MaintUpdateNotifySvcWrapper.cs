using System;
using System.Collections.Generic;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.BusinessLayer.MaintUpdateNotifySvc;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200002A RID: 42
	public class MaintUpdateNotifySvcWrapper
	{
		// Token: 0x0600035D RID: 861 RVA: 0x00014E54 File Offset: 0x00013054
		public static MaintenanceRenewalItemDAL GetNotificationItem(VersionInfo versionInfo)
		{
			MaintenanceRenewalItemDAL maintenanceRenewalItemDAL = new MaintenanceRenewalItemDAL();
			MaintUpdateNotifySvcWrapper.UpdateNotificationItem(maintenanceRenewalItemDAL, versionInfo);
			return maintenanceRenewalItemDAL;
		}

		// Token: 0x0600035E RID: 862 RVA: 0x00014E64 File Offset: 0x00013064
		public static void UpdateNotificationItem(MaintenanceRenewalItemDAL renewal, VersionInfo versionInfo)
		{
			if (string.IsNullOrEmpty(versionInfo.Hotfix))
			{
				renewal.Title = versionInfo.Message.MaintenanceMessage;
			}
			else
			{
				renewal.Title = string.Format("{0} {1}", versionInfo.Message.MaintenanceMessage, versionInfo.Hotfix);
			}
			renewal.Description = versionInfo.ReleaseNotes;
			if (renewal.DateReleased < versionInfo.DateReleased)
			{
				renewal.Ignored = false;
			}
			renewal.Url = versionInfo.Link;
			renewal.SetNotAcknowledged();
			renewal.ProductTag = versionInfo.ProductTag;
			renewal.DateReleased = versionInfo.DateReleased;
			renewal.NewVersion = versionInfo.Version;
		}

		// Token: 0x0600035F RID: 863 RVA: 0x00014F10 File Offset: 0x00013110
		public static ModuleInfo[] GetModules(List<ModuleInfo> listModules)
		{
			ModuleInfo[] array = new ModuleInfo[listModules.Count];
			for (int i = 0; i < listModules.Count; i++)
			{
				array[i] = new ModuleInfo
				{
					ProductDisplayName = listModules[i].ProductDisplayName,
					HotfixVersion = listModules[i].HotfixVersion,
					Version = listModules[i].Version,
					ProductTag = listModules[i].ProductTag,
					LicenseInfo = listModules[i].LicenseInfo
				};
			}
			return array;
		}
	}
}
