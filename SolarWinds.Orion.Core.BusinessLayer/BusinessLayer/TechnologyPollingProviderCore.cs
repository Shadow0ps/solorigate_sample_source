using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using SolarWinds.Orion.Core.Models.Interfaces;
using SolarWinds.Orion.Core.Pollers.Node.ResponseTime;
using SolarWinds.Orion.Core.Strings;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000041 RID: 65
	[Export(typeof(ITechnologyPollingProvider))]
	public sealed class TechnologyPollingProviderCore : ITechnologyPollingProvider
	{
		// Token: 0x1700007C RID: 124
		// (get) Token: 0x06000428 RID: 1064 RVA: 0x0001C58F File Offset: 0x0001A78F
		public IEnumerable<ITechnologyPolling> Items
		{
			get
			{
				yield return new TechnologyPollingByPollers("Node.CpuAndMemory", "Core.Node.CpuAndMemory", Resources.LIBCODE_MD0_01, 100, new string[]
				{
					"N.Cpu.%",
					"N.Memory.%"
				});
				yield return new TechnologyPollingByPollers("Node.NodeDetails", "Core.Node.NodeDetails", Resources.LIBCODE_GK0_1, 100, new string[]
				{
					"N.Details.%"
				});
				yield return new TechnologyPollingByPollers("Node.StatusResponseTime", "Core.Node.StatusResponseTime.Icmp", Resources.LIBCODE_ET0_04, 110, new string[]
				{
					NodeResponseTimeIcmpPoller.PollerTypeStatus,
					NodeResponseTimeIcmpPoller.PollerTypeResponse
				});
				yield return new TechnologyPollingByPollers("Node.StatusResponseTime", "Core.Node.StatusResponseTime.Snmp", Resources.LIBCODE_ET0_05, 100, new string[]
				{
					NodeResponseTimeSnmpPoller.PollerTypeStatus,
					NodeResponseTimeSnmpPoller.PollerTypeResponse
				});
				yield return new TechnologyPollingByPollers("Node.StatusResponseTime", "Core.Node.StatusResponseTime.Agent", Resources.LIBCODE_JT0_1, 120, new string[]
				{
					NodeResponseTimeAgentPoller.PollerTypeStatus,
					NodeResponseTimeAgentPoller.PollerTypeResponse
				});
				yield break;
			}
		}
	}
}
