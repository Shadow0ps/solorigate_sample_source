using System;
using System.IO;

namespace SolarWinds.Orion.Core.BusinessLayer.OneTimeJobs
{
	// Token: 0x0200006B RID: 107
	public struct OneTimeJobRawResult : IDisposable
	{
		// Token: 0x170000E1 RID: 225
		// (get) Token: 0x060005AB RID: 1451 RVA: 0x00022594 File Offset: 0x00020794
		// (set) Token: 0x060005AC RID: 1452 RVA: 0x0002259C File Offset: 0x0002079C
		public bool Success { get; set; }

		// Token: 0x170000E2 RID: 226
		// (get) Token: 0x060005AD RID: 1453 RVA: 0x000225A5 File Offset: 0x000207A5
		// (set) Token: 0x060005AE RID: 1454 RVA: 0x000225AD File Offset: 0x000207AD
		public string Error { get; set; }

		// Token: 0x170000E3 RID: 227
		// (get) Token: 0x060005AF RID: 1455 RVA: 0x000225B6 File Offset: 0x000207B6
		// (set) Token: 0x060005B0 RID: 1456 RVA: 0x000225BE File Offset: 0x000207BE
		public Stream JobResultStream { get; set; }

		// Token: 0x170000E4 RID: 228
		// (get) Token: 0x060005B1 RID: 1457 RVA: 0x000225C7 File Offset: 0x000207C7
		// (set) Token: 0x060005B2 RID: 1458 RVA: 0x000225CF File Offset: 0x000207CF
		public Exception ExceptionFromJob { get; set; }

		// Token: 0x060005B3 RID: 1459 RVA: 0x000225D8 File Offset: 0x000207D8
		public void Dispose()
		{
			Stream jobResultStream = this.JobResultStream;
			if (jobResultStream == null)
			{
				return;
			}
			jobResultStream.Dispose();
		}
	}
}
