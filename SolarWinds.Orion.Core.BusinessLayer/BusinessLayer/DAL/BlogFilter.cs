using System;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000096 RID: 150
	public class BlogFilter : NotificationItemFilter
	{
		// Token: 0x170000F7 RID: 247
		// (get) Token: 0x06000760 RID: 1888 RVA: 0x00032F58 File Offset: 0x00031158
		// (set) Token: 0x06000761 RID: 1889 RVA: 0x00032F60 File Offset: 0x00031160
		public int MaxResults { get; set; }

		// Token: 0x06000762 RID: 1890 RVA: 0x00032F69 File Offset: 0x00031169
		public BlogFilter(bool includeAcknowledged, bool includeIgnored, int maxResults) : base(includeAcknowledged, includeIgnored)
		{
			this.MaxResults = maxResults;
		}

		// Token: 0x06000763 RID: 1891 RVA: 0x00032F7A File Offset: 0x0003117A
		public BlogFilter() : this(false, false, -1)
		{
		}
	}
}
