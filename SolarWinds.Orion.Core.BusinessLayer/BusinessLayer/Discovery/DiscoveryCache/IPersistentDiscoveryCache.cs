using System;
using SolarWinds.Orion.Core.Discovery;

namespace SolarWinds.Orion.Core.BusinessLayer.Discovery.DiscoveryCache
{
	// Token: 0x0200007D RID: 125
	public interface IPersistentDiscoveryCache
	{
		// Token: 0x06000659 RID: 1625
		DiscoveryResultItem GetResultForNode(int nodeId);

		// Token: 0x0600065A RID: 1626
		void StoreResultForNode(int nodeId, DiscoveryResultItem result);
	}
}
