using System;
using System.Collections.Generic;
using SolarWinds.Orion.Core.Common.Agent;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x0200008D RID: 141
	public interface IAgentInfoDAL
	{
		// Token: 0x060006D4 RID: 1748
		IEnumerable<AgentInfo> GetAgentsInfo();

		// Token: 0x060006D5 RID: 1749
		IEnumerable<AgentInfo> GetAgentsByNodesFilter(int engineId, string nodesFilter);

		// Token: 0x060006D6 RID: 1750
		AgentInfo GetAgentInfoByNode(int nodeId);

		// Token: 0x060006D7 RID: 1751
		AgentInfo GetAgentInfo(int agentId);

		// Token: 0x060006D8 RID: 1752
		AgentInfo GetAgentInfoByIpOrHostname(string ipAddress, string hostname);

		// Token: 0x060006D9 RID: 1753
		AgentInfo GetAgentInfoByAgentAddress(string address);

		// Token: 0x060006DA RID: 1754
		bool IsUniqueAgentName(string agentName);
	}
}
