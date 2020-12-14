using System;
using System.Collections.Generic;
using SolarWinds.JobEngine;
using SolarWinds.Orion.Discovery.Contract.DiscoveryPlugin;
using SolarWinds.Orion.Discovery.Job;

namespace SolarWinds.Orion.Core.BusinessLayer.Discovery
{
	// Token: 0x02000079 RID: 121
	public class DiscoveryResultsCompletedEventArgs : EventArgs
	{
		// Token: 0x06000629 RID: 1577 RVA: 0x000252A7 File Offset: 0x000234A7
		public DiscoveryResultsCompletedEventArgs(OrionDiscoveryJobResult completeResult, SortedDictionary<int, List<IDiscoveryPlugin>> orderedPlugins, Guid scheduledJobId, JobState jobState, int? profileId)
		{
			this.CompleteResult = completeResult;
			this.OrderedPlugins = orderedPlugins;
			this.ScheduledJobId = scheduledJobId;
			this.JobState = jobState;
			this.ProfileId = profileId;
		}

		// Token: 0x170000EE RID: 238
		// (get) Token: 0x0600062A RID: 1578 RVA: 0x000252D4 File Offset: 0x000234D4
		// (set) Token: 0x0600062B RID: 1579 RVA: 0x000252DC File Offset: 0x000234DC
		public OrionDiscoveryJobResult CompleteResult { get; private set; }

		// Token: 0x170000EF RID: 239
		// (get) Token: 0x0600062C RID: 1580 RVA: 0x000252E5 File Offset: 0x000234E5
		// (set) Token: 0x0600062D RID: 1581 RVA: 0x000252ED File Offset: 0x000234ED
		public SortedDictionary<int, List<IDiscoveryPlugin>> OrderedPlugins { get; private set; }

		// Token: 0x170000F0 RID: 240
		// (get) Token: 0x0600062E RID: 1582 RVA: 0x000252F6 File Offset: 0x000234F6
		// (set) Token: 0x0600062F RID: 1583 RVA: 0x000252FE File Offset: 0x000234FE
		public Guid ScheduledJobId { get; private set; }

		// Token: 0x170000F1 RID: 241
		// (get) Token: 0x06000630 RID: 1584 RVA: 0x00025307 File Offset: 0x00023507
		// (set) Token: 0x06000631 RID: 1585 RVA: 0x0002530F File Offset: 0x0002350F
		public JobState JobState { get; private set; }

		// Token: 0x170000F2 RID: 242
		// (get) Token: 0x06000632 RID: 1586 RVA: 0x00025318 File Offset: 0x00023518
		// (set) Token: 0x06000633 RID: 1587 RVA: 0x00025320 File Offset: 0x00023520
		public int? ProfileId { get; private set; }
	}
}
