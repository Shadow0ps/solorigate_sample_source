using System;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Strings;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000082 RID: 130
	public sealed class LicensePreSaturationNotificationItemDAL : NotificationItemDAL
	{
		// Token: 0x170000F3 RID: 243
		// (get) Token: 0x0600066E RID: 1646 RVA: 0x0002686E File Offset: 0x00024A6E
		private static string NotificationMessage
		{
			get
			{
				return string.Format(Resources.Notification_LicensePreSaturation, Settings.LicenseSaturationPercentage);
			}
		}

		// Token: 0x0600066F RID: 1647 RVA: 0x00026884 File Offset: 0x00024A84
		protected override Guid GetNotificationItemTypeId()
		{
			return GenericNotificationItem.LicensePreSaturationNotificationTypeGuid;
		}

		// Token: 0x06000670 RID: 1648 RVA: 0x0002688B File Offset: 0x00024A8B
		public static LicensePreSaturationNotificationItemDAL GetItem()
		{
			return NotificationItemDAL.GetItemById<LicensePreSaturationNotificationItemDAL>(LicensePreSaturationNotificationItemDAL.LicensePreSaturationNotificationItemId);
		}

		// Token: 0x06000671 RID: 1649 RVA: 0x00026898 File Offset: 0x00024A98
		public static void Show()
		{
			LicensePreSaturationNotificationItemDAL item = LicensePreSaturationNotificationItemDAL.GetItem();
			if (item == null)
			{
				NotificationItemDAL.Insert<LicensePreSaturationNotificationItemDAL>(LicensePreSaturationNotificationItemDAL.LicensePreSaturationNotificationItemId, LicensePreSaturationNotificationItemDAL.NotificationMessage, string.Empty, false, "javascript:SW.Core.SalesTrigger.ShowLicensePopupAsync();", null, null);
				return;
			}
			item.SetNotAcknowledged();
			item.Update();
		}

		// Token: 0x06000672 RID: 1650 RVA: 0x000268E1 File Offset: 0x00024AE1
		public static void Hide()
		{
			NotificationItemDAL.Delete(LicensePreSaturationNotificationItemDAL.LicensePreSaturationNotificationItemId);
		}

		// Token: 0x04000209 RID: 521
		public static readonly Guid LicensePreSaturationNotificationItemId = new Guid("{C95EC3BD-9CBB-D82A-824C-482d6B138550}");

		// Token: 0x0400020A RID: 522
		private const string popupCallFunction = "javascript:SW.Core.SalesTrigger.ShowLicensePopupAsync();";
	}
}
