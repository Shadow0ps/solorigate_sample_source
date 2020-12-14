using System;

namespace SolarWinds.Orion.Core.BusinessLayer.Thresholds
{
	// Token: 0x02000051 RID: 81
	internal class Bucket
	{
		// Token: 0x060004E2 RID: 1250 RVA: 0x0001EB5C File Offset: 0x0001CD5C
		public Bucket(double minValue, double maxValue)
		{
			this.MinValue = minValue;
			this.MaxValue = maxValue;
		}

		// Token: 0x170000BB RID: 187
		// (get) Token: 0x060004E3 RID: 1251 RVA: 0x0001EB72 File Offset: 0x0001CD72
		// (set) Token: 0x060004E4 RID: 1252 RVA: 0x0001EB7A File Offset: 0x0001CD7A
		public double MinValue { get; set; }

		// Token: 0x170000BC RID: 188
		// (get) Token: 0x060004E5 RID: 1253 RVA: 0x0001EB83 File Offset: 0x0001CD83
		// (set) Token: 0x060004E6 RID: 1254 RVA: 0x0001EB8B File Offset: 0x0001CD8B
		public double MaxValue { get; set; }
	}
}
