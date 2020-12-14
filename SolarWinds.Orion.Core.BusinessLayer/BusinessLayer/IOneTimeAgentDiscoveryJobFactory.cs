using System;
using System.Collections.Generic;
using SolarWinds.Orion.Core.SharedCredentials;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200001D RID: 29
	public interface IOneTimeAgentDiscoveryJobFactory
	{
		// Token: 0x060002CA RID: 714
		Guid CreateOneTimeAgentDiscoveryJob(int nodeId, int engineId, int? profileId, List<Credential> credentials);
	}
}
