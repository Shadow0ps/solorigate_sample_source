using System;
using System.Globalization;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Strings;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000089 RID: 137
	internal sealed class DatabaseLimitNotificationItemDAL : NotificationItemDAL
	{
		// Token: 0x060006BF RID: 1727 RVA: 0x0002ADFC File Offset: 0x00028FFC
		public static DatabaseLimitNotificationItemDAL GetItem()
		{
			return NotificationItemDAL.GetItemById<DatabaseLimitNotificationItemDAL>(DatabaseLimitNotificationItemDAL.DatabaseLimitNotificationItemId);
		}

		// Token: 0x060006C0 RID: 1728 RVA: 0x0002AE08 File Offset: 0x00029008
		public void Show(double databaseSize, double percent)
		{
			string description = string.Format(CultureInfo.InvariantCulture, "{0}|{1}", databaseSize, percent);
			bool flag = percent < 90.0;
			Guid typeId = flag ? DatabaseLimitNotificationItemDAL.warningNotificationTypeGuid : DatabaseLimitNotificationItemDAL.reachedNotificationTypeGuid;
			DatabaseLimitNotificationItemDAL item = DatabaseLimitNotificationItemDAL.GetItem();
			if (item == null)
			{
				NotificationItemDAL.Insert(DatabaseLimitNotificationItemDAL.DatabaseLimitNotificationItemId, typeId, this.GetNotificationMessage(flag), description, false, null, null, null);
				return;
			}
			bool flag2 = double.Parse(item.Description.Split(new char[]
			{
				'|'
			})[1], CultureInfo.InvariantCulture) < 90.0;
			if (flag2 == flag)
			{
				return;
			}
			if (flag2 != flag)
			{
				item.SetNotAcknowledged();
			}
			item.TypeId = typeId;
			item.Title = this.GetNotificationMessage(flag);
			item.Description = description;
			item.Update();
		}

		// Token: 0x060006C1 RID: 1729 RVA: 0x0002AEDB File Offset: 0x000290DB
		public void Hide()
		{
			NotificationItemDAL.Delete(DatabaseLimitNotificationItemDAL.DatabaseLimitNotificationItemId);
		}

		// Token: 0x060006C2 RID: 1730 RVA: 0x0002AEE8 File Offset: 0x000290E8
		private string GetNotificationMessage(bool isWarning)
		{
			if (!isWarning)
			{
				return this.LimitReachedTitle;
			}
			return this.WarningTitle;
		}

		// Token: 0x060006C3 RID: 1731 RVA: 0x0002AEFA File Offset: 0x000290FA
		public DatabaseLimitNotificationItemDAL() : this(new DatabaseInfoDAL())
		{
		}

		// Token: 0x060006C4 RID: 1732 RVA: 0x0002AF08 File Offset: 0x00029108
		public DatabaseLimitNotificationItemDAL(IDatabaseInfoDAL databaseInfoDAL)
		{
			if (databaseInfoDAL == null)
			{
				throw new ArgumentNullException("databaseInfoDAL");
			}
			this.databaseInfoDAL = databaseInfoDAL;
		}

		// Token: 0x060006C5 RID: 1733 RVA: 0x0002AFB4 File Offset: 0x000291B4
		public bool CheckNotificationState()
		{
			double databaseLimitInMegabytes = this.databaseInfoDAL.GetDatabaseLimitInMegabytes();
			if (databaseLimitInMegabytes < 0.0)
			{
				this.Hide();
				return false;
			}
			double databaseSizeInMegaBytes = this.databaseInfoDAL.GetDatabaseSizeInMegaBytes();
			double num = databaseSizeInMegaBytes / databaseLimitInMegabytes * 100.0;
			NotificationItemDAL.log.DebugFormat("Database limit is {0} MB. Database size is {1} MB", databaseLimitInMegabytes, databaseSizeInMegaBytes);
			if (num >= 80.0)
			{
				this.Show(databaseSizeInMegaBytes, num);
			}
			else
			{
				this.Hide();
			}
			return true;
		}

		// Token: 0x04000216 RID: 534
		public const double CriticalPercentLimit = 90.0;

		// Token: 0x04000217 RID: 535
		public const double WarningPercentLimit = 80.0;

		// Token: 0x04000218 RID: 536
		public static readonly Guid DatabaseLimitNotificationItemId = new Guid("71475071-459F-4844-B689-6F210B0D416F");

		// Token: 0x04000219 RID: 537
		public static readonly Guid warningNotificationTypeGuid = new Guid("{7E8C21EF-61B1-4B7C-9122-B9A7E807B272}");

		// Token: 0x0400021A RID: 538
		public static readonly Guid reachedNotificationTypeGuid = new Guid("{7EA47379-CB96-48A3-89C4-84C18559351B}");

		// Token: 0x0400021B RID: 539
		private static readonly string linkWrapper = "&nbsp;&nbsp;<span style='font-weight:normal'>&#0187;&nbsp;<a href='{1}'>{0}</a></span>";

		// Token: 0x0400021C RID: 540
		private string WarningTitle = string.Format(Resources.COREBUSINESSLAYERDAL_CODE_1, 80.0) + string.Format(DatabaseLimitNotificationItemDAL.linkWrapper, Resources.COREBUSINESSLAYERDAL_CODE_3, string.Format("http://www.solarwinds.com/documentation/kbloader.aspx?lang={0}&kb=3545", Resources.CurrentHelpLanguage));

		// Token: 0x0400021D RID: 541
		private string LimitReachedTitle = string.Format(Resources.COREBUSINESSLAYERDAL_CODE_2, 90.0) + string.Format(DatabaseLimitNotificationItemDAL.linkWrapper, Resources.COREBUSINESSLAYERDAL_CODE_3, string.Format("http://www.solarwinds.com/documentation/kbloader.aspx?lang={0}&kb=3545", Resources.CurrentHelpLanguage));

		// Token: 0x0400021E RID: 542
		private readonly IDatabaseInfoDAL databaseInfoDAL;
	}
}
