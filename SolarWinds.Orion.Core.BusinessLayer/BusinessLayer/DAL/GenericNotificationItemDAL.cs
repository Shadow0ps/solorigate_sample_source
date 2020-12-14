using System;
using System.Collections.Generic;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x0200009F RID: 159
	public class GenericNotificationItemDAL : NotificationItemDAL
	{
		// Token: 0x060007B7 RID: 1975 RVA: 0x00037410 File Offset: 0x00035610
		protected override Guid GetNotificationItemTypeId()
		{
			return GenericNotificationItem.GenericNotificationTypeGuid;
		}

		// Token: 0x060007B8 RID: 1976 RVA: 0x00037417 File Offset: 0x00035617
		public static GenericNotificationItemDAL GetItemById(Guid itemId)
		{
			return NotificationItemDAL.GetItemById<GenericNotificationItemDAL>(itemId);
		}

		// Token: 0x060007B9 RID: 1977 RVA: 0x0003741F File Offset: 0x0003561F
		public static GenericNotificationItemDAL GetLatestItem()
		{
			return NotificationItemDAL.GetLatestItem<GenericNotificationItemDAL>(new NotificationItemFilter(false, false));
		}

		// Token: 0x060007BA RID: 1978 RVA: 0x0003742D File Offset: 0x0003562D
		public static ICollection<GenericNotificationItemDAL> GetItems(NotificationItemFilter filter)
		{
			return NotificationItemDAL.GetItems<GenericNotificationItemDAL>(filter);
		}

		// Token: 0x060007BB RID: 1979 RVA: 0x00037435 File Offset: 0x00035635
		public static int GetNotificationItemsCount()
		{
			return NotificationItemDAL.GetNotificationsCount<GenericNotificationItemDAL>(new NotificationItemFilter(false, false));
		}

		// Token: 0x060007BC RID: 1980 RVA: 0x00037443 File Offset: 0x00035643
		public static GenericNotificationItemDAL Insert(Guid notificationId, string title, string description, bool ignored, string url, DateTime? acknowledgedAt, string acknowledgedBy)
		{
			return NotificationItemDAL.Insert<GenericNotificationItemDAL>(notificationId, title, description, ignored, url, acknowledgedAt, acknowledgedBy);
		}
	}
}
