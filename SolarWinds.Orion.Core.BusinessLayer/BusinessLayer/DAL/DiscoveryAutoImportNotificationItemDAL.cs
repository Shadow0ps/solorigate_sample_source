using System;
using System.Globalization;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Models.Discovery;
using SolarWinds.Orion.Core.Strings;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000080 RID: 128
	public sealed class DiscoveryAutoImportNotificationItemDAL : NotificationItemDAL
	{
		// Token: 0x06000665 RID: 1637 RVA: 0x00026788 File Offset: 0x00024988
		public static DiscoveryAutoImportNotificationItemDAL GetItem()
		{
			return NotificationItemDAL.GetItemById<DiscoveryAutoImportNotificationItemDAL>(DiscoveryAutoImportNotificationItemDAL.DiscoveryAutoImportNotificationItemId);
		}

		// Token: 0x06000666 RID: 1638 RVA: 0x00026794 File Offset: 0x00024994
		public static void Show(DiscoveryResultBase result, StartImportStatus status)
		{
			DiscoveryAutoImportNotificationItemDAL item = DiscoveryAutoImportNotificationItemDAL.GetItem();
			string description = string.Format(CultureInfo.InvariantCulture, "DiscoveryImportStatus:{0}", status);
			string title = string.Empty;
			switch (status)
			{
			case StartImportStatus.Failed:
				title = Resources2.Notification_DiscoveryAutoImport_Failed;
				break;
			case StartImportStatus.LicenseExceeded:
				title = Resources2.Notification_DiscoveryAutoImport_LicenseExceeded;
				break;
			case StartImportStatus.Finished:
				title = Resources2.Notification_DiscoveryAutoImport_Succeeded;
				break;
			default:
				return;
			}
			if (item == null)
			{
				NotificationItemDAL.Insert(DiscoveryAutoImportNotificationItemDAL.DiscoveryAutoImportNotificationItemId, DiscoveryAutoImportNotificationItemDAL.DiscoveryAutoImportNotificationTypeGuid, title, description, false, DiscoveryAutoImportNotificationItemDAL.NetworkSonarDiscoveryURL, null, null);
				return;
			}
			item.SetNotAcknowledged();
			item.Title = title;
			item.Description = description;
			item.Update();
		}

		// Token: 0x06000667 RID: 1639 RVA: 0x0002682F File Offset: 0x00024A2F
		public static void Hide()
		{
			NotificationItemDAL.Delete(DiscoveryAutoImportNotificationItemDAL.DiscoveryAutoImportNotificationItemId);
		}

		// Token: 0x04000206 RID: 518
		public static readonly Guid DiscoveryAutoImportNotificationItemId = new Guid("{D52F46CF-99CA-4E93-9EA4-1FB9D8F27E46}");

		// Token: 0x04000207 RID: 519
		public static readonly Guid DiscoveryAutoImportNotificationTypeGuid = new Guid("{DD441A02-4789-4716-9A48-F0F7E3FC3EB4}");

		// Token: 0x04000208 RID: 520
		public static readonly string NetworkSonarDiscoveryURL = "/Orion/Discovery/Default.aspx";
	}
}
