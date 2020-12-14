using System;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x020000B0 RID: 176
	public class MaintenanceRenewalFilter : NotificationItemFilter
	{
		// Token: 0x1700011B RID: 283
		// (get) Token: 0x060008B3 RID: 2227 RVA: 0x0003EE30 File Offset: 0x0003D030
		// (set) Token: 0x060008B4 RID: 2228 RVA: 0x0003EE38 File Offset: 0x0003D038
		public string ProductTag { get; set; }

		// Token: 0x060008B5 RID: 2229 RVA: 0x0003EE41 File Offset: 0x0003D041
		public MaintenanceRenewalFilter(bool includeAcknowledged, bool includeIgnored, string productTag) : base(includeAcknowledged, includeIgnored)
		{
			this.ProductTag = productTag;
		}

		// Token: 0x060008B6 RID: 2230 RVA: 0x0003EE52 File Offset: 0x0003D052
		public MaintenanceRenewalFilter() : this(false, false, null)
		{
		}
	}
}
