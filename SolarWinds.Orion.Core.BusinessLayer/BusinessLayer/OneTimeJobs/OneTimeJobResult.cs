using System;

namespace SolarWinds.Orion.Core.BusinessLayer.OneTimeJobs
{
	// Token: 0x0200006C RID: 108
	public class OneTimeJobResult<T>
	{
		// Token: 0x170000E5 RID: 229
		// (get) Token: 0x060005B4 RID: 1460 RVA: 0x000225EA File Offset: 0x000207EA
		// (set) Token: 0x060005B5 RID: 1461 RVA: 0x000225F2 File Offset: 0x000207F2
		public bool Success { get; set; }

		// Token: 0x170000E6 RID: 230
		// (get) Token: 0x060005B6 RID: 1462 RVA: 0x000225FB File Offset: 0x000207FB
		// (set) Token: 0x060005B7 RID: 1463 RVA: 0x00022603 File Offset: 0x00020803
		public string Message { get; set; }

		// Token: 0x170000E7 RID: 231
		// (get) Token: 0x060005B8 RID: 1464 RVA: 0x0002260C File Offset: 0x0002080C
		// (set) Token: 0x060005B9 RID: 1465 RVA: 0x00022614 File Offset: 0x00020814
		public T Value { get; set; }
	}
}
