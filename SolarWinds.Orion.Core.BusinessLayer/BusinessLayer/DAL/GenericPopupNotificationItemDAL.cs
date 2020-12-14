using System;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x0200009B RID: 155
	public class GenericPopupNotificationItemDAL : NotificationItemDAL
	{
		// Token: 0x06000799 RID: 1945 RVA: 0x0002683C File Offset: 0x00024A3C
		protected GenericPopupNotificationItemDAL()
		{
		}

		// Token: 0x0600079A RID: 1946 RVA: 0x000347D3 File Offset: 0x000329D3
		protected virtual Guid GetPopupNotificationItemId()
		{
			return Guid.Empty;
		}

		// Token: 0x0600079B RID: 1947 RVA: 0x000347DC File Offset: 0x000329DC
		protected static TNotificationItem Create<TNotificationItem>(string title, string url) where TNotificationItem : GenericPopupNotificationItemDAL, new()
		{
			Guid popupNotificationItemId = Activator.CreateInstance<TNotificationItem>().GetPopupNotificationItemId();
			if (popupNotificationItemId == Guid.Empty)
			{
				throw new ArgumentException("Can't obtain Popup Notification Item GUID", "TNotificationItem");
			}
			TNotificationItem itemById = NotificationItemDAL.GetItemById<TNotificationItem>(popupNotificationItemId);
			if (itemById == null)
			{
				return NotificationItemDAL.Insert<TNotificationItem>(popupNotificationItemId, title, null, false, url, null, null);
			}
			itemById.Title = title;
			itemById.Description = null;
			itemById.Url = url;
			itemById.CreatedAt = DateTime.UtcNow;
			itemById.SetNotAcknowledged();
			itemById.Ignored = false;
			if (!itemById.Update())
			{
				return default(TNotificationItem);
			}
			return itemById;
		}
	}
}
