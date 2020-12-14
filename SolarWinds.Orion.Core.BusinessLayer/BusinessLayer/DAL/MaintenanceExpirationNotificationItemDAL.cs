using System;
using System.Collections.Generic;
using System.Linq;
using SolarWinds.Orion.Core.Strings;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000092 RID: 146
	public sealed class MaintenanceExpirationNotificationItemDAL : NotificationItemDAL
	{
		// Token: 0x060006FF RID: 1791 RVA: 0x0002C86C File Offset: 0x0002AA6C
		public static MaintenanceExpirationNotificationItemDAL GetItem()
		{
			return NotificationItemDAL.GetItemById<MaintenanceExpirationNotificationItemDAL>(MaintenanceExpirationNotificationItemDAL.MaintenanceExpirationNotificationItemId);
		}

		// Token: 0x06000700 RID: 1792 RVA: 0x0002C878 File Offset: 0x0002AA78
		public static void Show(Dictionary<string, int> moduleExpirations)
		{
			Dictionary<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo> dictionary = new Dictionary<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo>();
			foreach (KeyValuePair<string, int> keyValuePair in moduleExpirations)
			{
				dictionary[keyValuePair.Key] = new MaintenanceExpirationNotificationItemDAL.ExpirationInfo
				{
					LicenseName = string.Empty,
					DaysToExpire = keyValuePair.Value,
					LastRemindMeLaterDate = null
				};
			}
			MaintenanceExpirationNotificationItemDAL.Show(dictionary);
		}

		// Token: 0x06000701 RID: 1793 RVA: 0x0002C904 File Offset: 0x0002AB04
		internal static void Show(Dictionary<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo> moduleExpirations)
		{
			bool flag = moduleExpirations.Any((KeyValuePair<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo> m) => m.Value.DaysToExpire <= 0);
			int daysToExpire = moduleExpirations.Min((KeyValuePair<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo> x) => x.Value.DaysToExpire);
			string url = "javascript:SW.Core.SalesTrigger.ShowMaintenancePopupAsync();";
			Guid typeId = flag ? MaintenanceExpirationNotificationItemDAL.MaintenanceExpiredNotificationTypeGuid : MaintenanceExpirationNotificationItemDAL.MaintenanceExpirationWarningNotificationTypeGuid;
			int maintenanceExpiredShowAgainAtDays = BusinessLayerSettings.Instance.MaintenanceExpiredShowAgainAtDays;
			MaintenanceExpirationNotificationItemDAL item = MaintenanceExpirationNotificationItemDAL.GetItem();
			if (item == null)
			{
				string description = MaintenanceExpirationNotificationItemDAL.Serialize(moduleExpirations);
				NotificationItemDAL.Insert(MaintenanceExpirationNotificationItemDAL.MaintenanceExpirationNotificationItemId, typeId, MaintenanceExpirationNotificationItemDAL.GetNotificationMessage(flag, daysToExpire), description, false, url, null, null);
				return;
			}
			Dictionary<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo> previousExpirations = MaintenanceExpirationNotificationItemDAL.Deserialize(item.Description);
			IEnumerable<KeyValuePair<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo>> previousExpirations2 = previousExpirations;
			Func<KeyValuePair<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo>, bool> <>9__2;
			Func<KeyValuePair<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo>, bool> predicate;
			if ((predicate = <>9__2) == null)
			{
				predicate = (<>9__2 = ((KeyValuePair<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo> previousExpiration) => moduleExpirations.ContainsKey(previousExpiration.Key)));
			}
			foreach (KeyValuePair<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo> keyValuePair in previousExpirations2.Where(predicate))
			{
				moduleExpirations[keyValuePair.Key].LastRemindMeLaterDate = keyValuePair.Value.LastRemindMeLaterDate;
			}
			DateTime utcNow = DateTime.UtcNow;
			int num = (int)utcNow.Subtract(item.AcknowledgedAt ?? DateTime.UtcNow).TotalDays;
			DateTime? acknowledgedAt = item.AcknowledgedAt;
			foreach (KeyValuePair<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo> keyValuePair2 in moduleExpirations)
			{
				if ((previousExpirations.ContainsKey(keyValuePair2.Key) || num != maintenanceExpiredShowAgainAtDays) && (!previousExpirations.ContainsKey(keyValuePair2.Key) || keyValuePair2.Value.DaysToExpire <= 0 || num != maintenanceExpiredShowAgainAtDays) && (!previousExpirations.ContainsKey(keyValuePair2.Key) || previousExpirations[keyValuePair2.Key].DaysToExpire <= 0 || keyValuePair2.Value.DaysToExpire > 0))
				{
					utcNow = DateTime.UtcNow;
					if ((int)utcNow.Subtract(keyValuePair2.Value.LastRemindMeLaterDate ?? DateTime.UtcNow).TotalDays != maintenanceExpiredShowAgainAtDays)
					{
						continue;
					}
				}
				item.SetNotAcknowledged();
				break;
			}
			if (acknowledgedAt != null)
			{
				IEnumerable<KeyValuePair<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo>> moduleExpirations2 = moduleExpirations;
				Func<KeyValuePair<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo>, bool> <>9__3;
				Func<KeyValuePair<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo>, bool> predicate2;
				if ((predicate2 = <>9__3) == null)
				{
					predicate2 = (<>9__3 = ((KeyValuePair<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo> m) => m.Value.DaysToExpire <= 0 && m.Value.LastRemindMeLaterDate == null && previousExpirations.ContainsKey(m.Key) && previousExpirations[m.Key].DaysToExpire <= 0));
				}
				foreach (KeyValuePair<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo> keyValuePair3 in moduleExpirations2.Where(predicate2))
				{
					keyValuePair3.Value.LastRemindMeLaterDate = acknowledgedAt;
				}
			}
			item.TypeId = typeId;
			item.Description = MaintenanceExpirationNotificationItemDAL.Serialize(moduleExpirations);
			item.Url = url;
			item.Title = MaintenanceExpirationNotificationItemDAL.GetNotificationMessage(flag, daysToExpire);
			item.Update();
		}

		// Token: 0x06000702 RID: 1794 RVA: 0x0002CC74 File Offset: 0x0002AE74
		public static void Hide()
		{
			NotificationItemDAL.Delete(MaintenanceExpirationNotificationItemDAL.MaintenanceExpirationNotificationItemId);
		}

		// Token: 0x06000703 RID: 1795 RVA: 0x0002CC81 File Offset: 0x0002AE81
		private static string GetNotificationMessage(bool expired, int daysToExpire)
		{
			if (!expired)
			{
				return string.Format(Resources.COREBUSINESSLAYERDAL_CODE_YK0_4, daysToExpire, "http://www.solarwinds.com/embedded_in_products/productLink.aspx?id=online_quote");
			}
			return string.Format(Resources.COREBUSINESSLAYERDAL_CODE_YK0_3, "http://www.solarwinds.com/embedded_in_products/productLink.aspx?id=online_quote");
		}

		// Token: 0x06000704 RID: 1796 RVA: 0x0002CCAB File Offset: 0x0002AEAB
		private static string Serialize(Dictionary<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo> moduleExpirations)
		{
			return string.Join("|", (from m in moduleExpirations
			select string.Format("{0};{1};{2};{3};{4}", new object[]
			{
				m.Key,
				m.Value.DaysToExpire,
				m.Value.LicenseName,
				m.Value.LastRemindMeLaterDate,
				m.Value.ActivationKey
			})).ToArray<string>());
		}

		// Token: 0x06000705 RID: 1797 RVA: 0x0002CCE4 File Offset: 0x0002AEE4
		private static Dictionary<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo> Deserialize(string moduleExpirations)
		{
			Dictionary<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo> dictionary = new Dictionary<string, MaintenanceExpirationNotificationItemDAL.ExpirationInfo>();
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
						MaintenanceExpirationNotificationItemDAL.ExpirationInfo expirationInfo = new MaintenanceExpirationNotificationItemDAL.ExpirationInfo();
						expirationInfo.DaysToExpire = Convert.ToInt32(array2[1]);
						if (array2.Length > 2 && !string.IsNullOrWhiteSpace(array2[2]))
						{
							expirationInfo.LicenseName = array2[2];
						}
						if (array2.Length > 3 && !string.IsNullOrWhiteSpace(array2[3]))
						{
							expirationInfo.LastRemindMeLaterDate = new DateTime?(DateTime.Parse(array2[3]));
						}
						if (array2.Length > 4 && !string.IsNullOrWhiteSpace(array2[4]))
						{
							expirationInfo.ActivationKey = array2[4];
						}
						dictionary[array2[0]] = expirationInfo;
					}
					catch (Exception ex)
					{
						NotificationItemDAL.log.Warn("Unable to parse maintenance expiration notification panel data", ex);
					}
				}
			}
			return dictionary;
		}

		// Token: 0x0400022F RID: 559
		public static readonly Guid MaintenanceExpirationNotificationItemId = new Guid("{561BE782-187F-4977-B5C4-B8666E73E582}");

		// Token: 0x04000230 RID: 560
		public static readonly Guid MaintenanceExpirationWarningNotificationTypeGuid = new Guid("{93465286-2E85-411D-8980-EFD32F04F0EE}");

		// Token: 0x04000231 RID: 561
		public static readonly Guid MaintenanceExpiredNotificationTypeGuid = new Guid("{ED77CD80-345D-4D51-B6A7-4AB3728F2200}");

		// Token: 0x02000185 RID: 389
		internal class ExpirationInfo
		{
			// Token: 0x17000163 RID: 355
			// (get) Token: 0x06000C3D RID: 3133 RVA: 0x0004A369 File Offset: 0x00048569
			// (set) Token: 0x06000C3E RID: 3134 RVA: 0x0004A37B File Offset: 0x0004857B
			[Obsolete("Use LicenseName instead", true)]
			public string ModuleName
			{
				get
				{
					return this._moduleNameLegacy ?? this.LicenseName;
				}
				set
				{
					this._moduleNameLegacy = value;
				}
			}

			// Token: 0x17000164 RID: 356
			// (get) Token: 0x06000C3F RID: 3135 RVA: 0x0004A384 File Offset: 0x00048584
			// (set) Token: 0x06000C40 RID: 3136 RVA: 0x0004A38C File Offset: 0x0004858C
			public string LicenseName { get; set; }

			// Token: 0x17000165 RID: 357
			// (get) Token: 0x06000C41 RID: 3137 RVA: 0x0004A395 File Offset: 0x00048595
			// (set) Token: 0x06000C42 RID: 3138 RVA: 0x0004A39D File Offset: 0x0004859D
			public int DaysToExpire { get; set; }

			// Token: 0x17000166 RID: 358
			// (get) Token: 0x06000C43 RID: 3139 RVA: 0x0004A3A6 File Offset: 0x000485A6
			// (set) Token: 0x06000C44 RID: 3140 RVA: 0x0004A3AE File Offset: 0x000485AE
			public DateTime? LastRemindMeLaterDate { get; set; }

			// Token: 0x17000167 RID: 359
			// (get) Token: 0x06000C45 RID: 3141 RVA: 0x0004A3B7 File Offset: 0x000485B7
			// (set) Token: 0x06000C46 RID: 3142 RVA: 0x0004A3BF File Offset: 0x000485BF
			public string ActivationKey { get; set; }

			// Token: 0x040004ED RID: 1261
			private string _moduleNameLegacy;
		}
	}
}
