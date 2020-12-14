using System;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x020000A8 RID: 168
	public sealed class ScheduledDiscoveryNotificationItemDAL : GenericPopupNotificationItemDAL
	{
		// Token: 0x06000861 RID: 2145 RVA: 0x0003BF8C File Offset: 0x0003A18C
		public ScheduledDiscoveryNotificationItemDAL GetItem()
		{
			return NotificationItemDAL.GetItemById<ScheduledDiscoveryNotificationItemDAL>(ScheduledDiscoveryNotificationItemDAL.ScheduledDiscoveryNotificationItemId);
		}

		// Token: 0x06000862 RID: 2146 RVA: 0x0003BF98 File Offset: 0x0003A198
		protected override Guid GetNotificationItemTypeId()
		{
			return GenericNotificationItem.ScheduledDiscoveryNotificationTypeGuid;
		}

		// Token: 0x06000863 RID: 2147 RVA: 0x0003BF9F File Offset: 0x0003A19F
		protected override Guid GetPopupNotificationItemId()
		{
			return ScheduledDiscoveryNotificationItemDAL.ScheduledDiscoveryNotificationItemId;
		}

		// Token: 0x06000864 RID: 2148 RVA: 0x0003BFA6 File Offset: 0x0003A1A6
		public static ScheduledDiscoveryNotificationItemDAL Create(string title, string url)
		{
			return GenericPopupNotificationItemDAL.Create<ScheduledDiscoveryNotificationItemDAL>(title, url);
		}

		// Token: 0x04000263 RID: 611
		public static readonly Guid ScheduledDiscoveryNotificationItemId = new Guid("3D28249D-EFE1-462e-B1A7-C55273D09AE8");
	}
}
