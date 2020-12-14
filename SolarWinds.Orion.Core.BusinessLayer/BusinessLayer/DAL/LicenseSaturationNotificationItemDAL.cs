using System;
using System.Collections.Generic;
using System.Linq;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Strings;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000094 RID: 148
	public sealed class LicenseSaturationNotificationItemDAL : NotificationItemDAL
	{
		// Token: 0x170000F6 RID: 246
		// (get) Token: 0x0600070C RID: 1804 RVA: 0x0002CFC8 File Offset: 0x0002B1C8
		private static string NotificationMessge
		{
			get
			{
				return string.Format(Resources.LIBCODE_PCC_24, Array.Empty<object>()) + " " + string.Format(Resources.LIBCODE_PCC_25, Array.Empty<object>());
			}
		}

		// Token: 0x0600070E RID: 1806 RVA: 0x0002CFF2 File Offset: 0x0002B1F2
		protected override Guid GetNotificationItemTypeId()
		{
			return GenericNotificationItem.LicenseSaturationNotificationTypeGuid;
		}

		// Token: 0x0600070F RID: 1807 RVA: 0x0002CFF9 File Offset: 0x0002B1F9
		public static LicenseSaturationNotificationItemDAL GetItem()
		{
			return NotificationItemDAL.GetItemById<LicenseSaturationNotificationItemDAL>(LicenseSaturationNotificationItemDAL.LicenseSaturationNotificationItemId);
		}

		// Token: 0x06000710 RID: 1808 RVA: 0x0002D008 File Offset: 0x0002B208
		public static void Show(IEnumerable<string> elementsOverLimit)
		{
			string text = string.Join(";", elementsOverLimit.ToArray<string>());
			LicenseSaturationNotificationItemDAL item = LicenseSaturationNotificationItemDAL.GetItem();
			if (item == null)
			{
				NotificationItemDAL.Insert<LicenseSaturationNotificationItemDAL>(LicenseSaturationNotificationItemDAL.LicenseSaturationNotificationItemId, LicenseSaturationNotificationItemDAL.NotificationMessge, text, false, LicenseSaturationNotificationItemDAL.popupCallFunction, null, null);
				return;
			}
			if (text == item.Description)
			{
				return;
			}
			if (string.IsNullOrEmpty(item.Description) || elementsOverLimit.Except(item.Description.Split(new char[]
			{
				';'
			})).Count<string>() > 0)
			{
				item.SetNotAcknowledged();
			}
			item.Description = text;
			item.Update();
		}

		// Token: 0x06000711 RID: 1809 RVA: 0x0002D0AA File Offset: 0x0002B2AA
		public static void Hide()
		{
			NotificationItemDAL.Delete(LicenseSaturationNotificationItemDAL.LicenseSaturationNotificationItemId);
		}

		// Token: 0x04000232 RID: 562
		public static readonly Guid LicenseSaturationNotificationItemId = new Guid("{B138550D-824C-482d-9CBB-D82A6C95EC3B}");

		// Token: 0x04000233 RID: 563
		private static readonly string popupCallFunction = "javascript:SW.Core.SalesTrigger.ShowLicensePopupAsync();";
	}
}
