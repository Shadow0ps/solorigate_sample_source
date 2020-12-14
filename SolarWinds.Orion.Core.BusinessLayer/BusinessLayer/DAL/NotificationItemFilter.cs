using System;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x020000A4 RID: 164
	public class NotificationItemFilter
	{
		// Token: 0x17000102 RID: 258
		// (get) Token: 0x06000807 RID: 2055 RVA: 0x00039B21 File Offset: 0x00037D21
		// (set) Token: 0x06000808 RID: 2056 RVA: 0x00039B29 File Offset: 0x00037D29
		public bool IncludeAcknowledged { get; set; }

		// Token: 0x17000103 RID: 259
		// (get) Token: 0x06000809 RID: 2057 RVA: 0x00039B32 File Offset: 0x00037D32
		// (set) Token: 0x0600080A RID: 2058 RVA: 0x00039B3A File Offset: 0x00037D3A
		public bool IncludeIgnored { get; set; }

		// Token: 0x0600080B RID: 2059 RVA: 0x00039B43 File Offset: 0x00037D43
		public NotificationItemFilter(bool includeAcknowledged, bool includeIgnored)
		{
			this.IncludeAcknowledged = includeAcknowledged;
			this.IncludeIgnored = includeIgnored;
		}

		// Token: 0x0600080C RID: 2060 RVA: 0x00039B59 File Offset: 0x00037D59
		public NotificationItemFilter() : this(false, false)
		{
		}
	}
}
