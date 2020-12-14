using System;
using SolarWinds.Orion.Discovery.Job;

namespace SolarWinds.Orion.Core.BusinessLayer.Discovery
{
	// Token: 0x0200007A RID: 122
	public interface IPartialDiscoveryResultsPersistence
	{
		// Token: 0x06000634 RID: 1588
		bool SaveResult(Guid jobId, OrionDiscoveryJobResult result);

		// Token: 0x06000635 RID: 1589
		OrionDiscoveryJobResult LoadResult(Guid jobId);

		// Token: 0x06000636 RID: 1590
		void DeleteResult(Guid jobId);

		// Token: 0x06000637 RID: 1591
		void ClearStore();
	}
}
