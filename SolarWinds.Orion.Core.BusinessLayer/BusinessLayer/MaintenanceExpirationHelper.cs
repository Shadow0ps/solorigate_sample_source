using System;
using System.Collections.Generic;
using System.Linq;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.DALs;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000029 RID: 41
	internal static class MaintenanceExpirationHelper
	{
		// Token: 0x0600035B RID: 859 RVA: 0x00014CC0 File Offset: 0x00012EC0
		internal static void CheckMaintenanceExpiration()
		{
			try
			{
				MaintenanceExpirationHelper.log.Debug("Check Maintenance expiration");
				int maintenanceExpirationWarningDays = BusinessLayerSettings.Instance.MaintenanceExpirationWarningDays;
				Dictionary<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo> dictionary = new Dictionary<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo>();
				ILicensingDAL licensing = new LicensingDAL();
				foreach (LicenseInfoModel licenseInfoModel in from lic in licensing.GetLicenses()
				where !lic.IsHidden && !lic.IsEvaluation && !licensing.DefaultLicenseFilter.Contains(lic.ProductName, StringComparer.OrdinalIgnoreCase)
				select lic)
				{
					if (MaintenanceExpirationHelper.log.IsDebugEnabled)
					{
						MaintenanceExpirationHelper.log.Debug(string.Format("Module:{0} MaintenanceTo:{1} DaysLeft:{2}", licenseInfoModel.LicenseName, licenseInfoModel.MaintenanceExpiration.Date, licenseInfoModel.DaysRemainingCount));
					}
					if (licenseInfoModel.DaysRemainingCount <= maintenanceExpirationWarningDays)
					{
						MaintenanceExpirationNotificationItemDAL.ExpirationInfo value = new MaintenanceExpirationNotificationItemDAL.ExpirationInfo
						{
							DaysToExpire = licenseInfoModel.DaysRemainingCount,
							ActivationKey = licenseInfoModel.LicenseKey
						};
						dictionary[licenseInfoModel.LicenseName] = value;
					}
				}
				if (dictionary.Count > 0)
				{
					MaintenanceExpirationHelper.log.Debug(string.Format("{0} products found to be notified", dictionary.Count));
					MaintenanceExpirationNotificationItemDAL.Show(dictionary);
				}
				else
				{
					MaintenanceExpirationNotificationItemDAL.Hide();
				}
			}
			catch (Exception ex)
			{
				MaintenanceExpirationHelper.log.Warn("Exception while checking maintenance expiration status: ", ex);
			}
		}

		// Token: 0x040000AD RID: 173
		private static readonly Log log = new Log();
	}
}
