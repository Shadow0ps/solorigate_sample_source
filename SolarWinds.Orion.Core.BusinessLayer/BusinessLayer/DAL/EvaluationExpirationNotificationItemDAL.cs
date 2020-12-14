using System;
using System.Collections.Generic;
using System.Linq;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Strings;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x0200008B RID: 139
	internal sealed class EvaluationExpirationNotificationItemDAL : NotificationItemDAL
	{
		// Token: 0x060006C9 RID: 1737 RVA: 0x0002B200 File Offset: 0x00029400
		public static EvaluationExpirationNotificationItemDAL GetItem()
		{
			return NotificationItemDAL.GetItemById<EvaluationExpirationNotificationItemDAL>(EvaluationExpirationNotificationItemDAL.EvaluationExpirationNotificationItemId);
		}

		// Token: 0x060006CA RID: 1738 RVA: 0x0002B20C File Offset: 0x0002940C
		public void Show(IEnumerable<ModuleLicenseInfo> expiringModules)
		{
			if (expiringModules == null)
			{
				throw new ArgumentNullException("expiringModules");
			}
			Dictionary<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo> dictionary = new Dictionary<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo>();
			foreach (ModuleLicenseInfo moduleLicenseInfo in expiringModules)
			{
				dictionary[moduleLicenseInfo.ModuleName] = new EvaluationExpirationNotificationItemDAL.ExpirationInfo
				{
					ModuleName = moduleLicenseInfo.ModuleName,
					LastRemindMeLater = null,
					DaysToExpire = moduleLicenseInfo.DaysRemaining
				};
			}
			this.Show(dictionary);
		}

		// Token: 0x060006CB RID: 1739 RVA: 0x0002B2A0 File Offset: 0x000294A0
		private void Show(IDictionary<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo> expirations)
		{
			EvaluationExpirationNotificationItemDAL item = EvaluationExpirationNotificationItemDAL.GetItem();
			if (item == null)
			{
				string description = EvaluationExpirationNotificationItemDAL.Serialize(expirations);
				NotificationItemDAL.Insert(EvaluationExpirationNotificationItemDAL.EvaluationExpirationNotificationItemId, EvaluationExpirationNotificationItemDAL.EvaluationExpirationNotificationTypeGuid, Resources.LIBCODE_LC0_1, description, false, "javascript:SW.Core.SalesTrigger.ShowEvalPopupAsync();", null, null);
				return;
			}
			Dictionary<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo> previousExpirations = EvaluationExpirationNotificationItemDAL.Deserialize(item.Description);
			int showExpiredAgainAt = BusinessLayerSettings.Instance.EvaluationExpirationShowAgainAtDays;
			IEnumerable<KeyValuePair<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo>> previousExpirations2 = previousExpirations;
			Func<KeyValuePair<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo>, bool> <>9__1;
			Func<KeyValuePair<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo>, bool> predicate;
			if ((predicate = <>9__1) == null)
			{
				predicate = (<>9__1 = ((KeyValuePair<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo> previousExpiration) => expirations.ContainsKey(previousExpiration.Key)));
			}
			foreach (KeyValuePair<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo> keyValuePair in previousExpirations2.Where(predicate))
			{
				expirations[keyValuePair.Key].LastRemindMeLater = keyValuePair.Value.LastRemindMeLater;
			}
			int daysFromLastRemindMeLater = (int)DateTime.UtcNow.Subtract(item.AcknowledgedAt ?? DateTime.UtcNow).TotalDays;
			DateTime? acknowledgedAt = item.AcknowledgedAt;
			if (expirations.Any((KeyValuePair<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo> module) => !previousExpirations.ContainsKey(module.Key) || (previousExpirations.ContainsKey(module.Key) && module.Value.DaysToExpire > 0 && daysFromLastRemindMeLater == showExpiredAgainAt) || (previousExpirations.ContainsKey(module.Key) && previousExpirations[module.Key].DaysToExpire > 0 && module.Value.DaysToExpire <= 0) || (int)DateTime.UtcNow.Subtract(module.Value.LastRemindMeLater ?? DateTime.UtcNow).TotalDays == showExpiredAgainAt))
			{
				item.SetNotAcknowledged();
			}
			if (acknowledgedAt != null)
			{
				IEnumerable<KeyValuePair<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo>> expirations2 = expirations;
				Func<KeyValuePair<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo>, bool> <>9__2;
				Func<KeyValuePair<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo>, bool> predicate2;
				if ((predicate2 = <>9__2) == null)
				{
					predicate2 = (<>9__2 = ((KeyValuePair<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo> m) => m.Value.DaysToExpire <= 0 && m.Value.LastRemindMeLater == null && previousExpirations.ContainsKey(m.Key) && previousExpirations[m.Key].DaysToExpire <= 0));
				}
				foreach (KeyValuePair<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo> keyValuePair2 in expirations2.Where(predicate2))
				{
					keyValuePair2.Value.LastRemindMeLater = acknowledgedAt;
				}
			}
			item.TypeId = EvaluationExpirationNotificationItemDAL.EvaluationExpirationNotificationTypeGuid;
			item.Description = EvaluationExpirationNotificationItemDAL.Serialize(expirations);
			item.Url = "javascript:SW.Core.SalesTrigger.ShowEvalPopupAsync();";
			item.Title = Resources.LIBCODE_LC0_1;
			item.Update();
		}

		// Token: 0x060006CC RID: 1740 RVA: 0x0002B4B4 File Offset: 0x000296B4
		public void Hide()
		{
			NotificationItemDAL.Delete(EvaluationExpirationNotificationItemDAL.EvaluationExpirationNotificationItemId);
		}

		// Token: 0x060006CD RID: 1741 RVA: 0x0002B4C4 File Offset: 0x000296C4
		public void CheckEvaluationExpiration()
		{
			ILicensingDAL licensing = new LicensingDAL();
			List<LicenseInfoModel> list = (from license in licensing.GetLicenses()
			where license.IsEvaluation && (license.IsExpired || license.DaysRemainingCount <= BusinessLayerSettings.Instance.EvaluationExpirationNotificationDays) && !string.Equals("DPI", license.ProductName, StringComparison.OrdinalIgnoreCase)
			select license).ToList<LicenseInfoModel>();
			licensing.FilterHiddenEvalLicenses(list);
			if (list.All((LicenseInfoModel lic) => licensing.DefaultLicenseFilter.Any((string module) => string.Equals(module, lic.ProductName, StringComparison.OrdinalIgnoreCase))))
			{
				this.Hide();
				return;
			}
			Dictionary<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo> dictionary = new Dictionary<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo>();
			foreach (LicenseInfoModel licenseInfoModel in list)
			{
				dictionary[licenseInfoModel.LicenseName] = new EvaluationExpirationNotificationItemDAL.ExpirationInfo
				{
					ModuleName = licenseInfoModel.LicenseName,
					LastRemindMeLater = null,
					DaysToExpire = licenseInfoModel.DaysRemainingCount
				};
			}
			this.Show(dictionary);
		}

		// Token: 0x060006CE RID: 1742 RVA: 0x0002B5C4 File Offset: 0x000297C4
		private static string Serialize(IDictionary<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo> moduleExpirations)
		{
			return string.Join("|", (from m in moduleExpirations
			select string.Format("{0};{1};{2};{3}", new object[]
			{
				m.Key,
				m.Value.DaysToExpire,
				m.Value.ModuleName,
				m.Value.LastRemindMeLater
			})).ToArray<string>());
		}

		// Token: 0x060006CF RID: 1743 RVA: 0x0002B5FC File Offset: 0x000297FC
		private static Dictionary<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo> Deserialize(string moduleExpirations)
		{
			Dictionary<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo> dictionary = new Dictionary<string, EvaluationExpirationNotificationItemDAL.ExpirationInfo>();
			if (!string.IsNullOrEmpty(moduleExpirations))
			{
				foreach (string text in moduleExpirations.Split(new char[]
				{
					'|'
				}))
				{
					try
					{
						string[] array2 = text.Split(new char[]
						{
							';'
						});
						EvaluationExpirationNotificationItemDAL.ExpirationInfo expirationInfo = new EvaluationExpirationNotificationItemDAL.ExpirationInfo();
						expirationInfo.DaysToExpire = Convert.ToInt32(array2[1]);
						if (array2.Length > 2 && !string.IsNullOrWhiteSpace(array2[2]))
						{
							expirationInfo.ModuleName = array2[2];
						}
						if (array2.Length > 3 && !string.IsNullOrWhiteSpace(array2[3]))
						{
							expirationInfo.LastRemindMeLater = new DateTime?(DateTime.Parse(array2[3]));
						}
						dictionary[array2[0]] = expirationInfo;
					}
					catch (Exception ex)
					{
						NotificationItemDAL.log.Warn("Unable to parse evaluation expiration notification panel data", ex);
					}
				}
			}
			return dictionary;
		}

		// Token: 0x0400021F RID: 543
		public static readonly Guid EvaluationExpirationNotificationItemId = new Guid("{AFA69A0B-2313-48C6-A8EA-BF6A0A256A1C}");

		// Token: 0x04000220 RID: 544
		public static readonly Guid EvaluationExpirationNotificationTypeGuid = new Guid("{6EE3D05F-7555-4E3E-9338-AA338834FE36}");

		// Token: 0x0200017C RID: 380
		private class ExpirationInfo
		{
			// Token: 0x1700015E RID: 350
			// (get) Token: 0x06000C1F RID: 3103 RVA: 0x00049E77 File Offset: 0x00048077
			// (set) Token: 0x06000C20 RID: 3104 RVA: 0x00049E7F File Offset: 0x0004807F
			public string ModuleName { get; set; }

			// Token: 0x1700015F RID: 351
			// (get) Token: 0x06000C21 RID: 3105 RVA: 0x00049E88 File Offset: 0x00048088
			// (set) Token: 0x06000C22 RID: 3106 RVA: 0x00049E90 File Offset: 0x00048090
			public DateTime? LastRemindMeLater { get; set; }

			// Token: 0x17000160 RID: 352
			// (get) Token: 0x06000C23 RID: 3107 RVA: 0x00049E99 File Offset: 0x00048099
			// (set) Token: 0x06000C24 RID: 3108 RVA: 0x00049EA1 File Offset: 0x000480A1
			public int DaysToExpire { get; set; }
		}
	}
}
