using System;
using SolarWinds.AgentManagement.Contract;

namespace SolarWinds.Orion.Core.BusinessLayer.Agent
{
	// Token: 0x020000BB RID: 187
	public interface IRemoteCollectorAgentStatusProvider
	{
		// Token: 0x0600092C RID: 2348
		AgentStatus GetStatus(int engineId);

		// Token: 0x0600092D RID: 2349
		void InvalidateCache();
	}
}
