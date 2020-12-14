using System;
using System.Collections.Generic;
using System.Linq;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Models.Interfaces;
using SolarWinds.Orion.Core.Models.Technology;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000017 RID: 23
	public class TechnologyPollingByVlanPollers : ITechnologyPolling
	{
		// Token: 0x06000295 RID: 661 RVA: 0x00010132 File Offset: 0x0000E332
		public TechnologyPollingByVlanPollers(string technologyID, string technologyPollingID, string displayName, int priority, string[] pollerTypePatterns)
		{
			this.TechnologyID = technologyID;
			this.TechnologyPollingID = technologyPollingID;
			this.DisplayName = displayName;
			this.Priority = priority;
			this.pollerTypePatterns = pollerTypePatterns;
		}

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x06000296 RID: 662 RVA: 0x0001016A File Offset: 0x0000E36A
		// (set) Token: 0x06000297 RID: 663 RVA: 0x00010172 File Offset: 0x0000E372
		public string TechnologyID { get; set; }

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x06000298 RID: 664 RVA: 0x0001017B File Offset: 0x0000E37B
		// (set) Token: 0x06000299 RID: 665 RVA: 0x00010183 File Offset: 0x0000E383
		public string TechnologyPollingID { get; set; }

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x0600029A RID: 666 RVA: 0x0001018C File Offset: 0x0000E38C
		// (set) Token: 0x0600029B RID: 667 RVA: 0x00010194 File Offset: 0x0000E394
		public string DisplayName { get; set; }

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x0600029C RID: 668 RVA: 0x0001019D File Offset: 0x0000E39D
		// (set) Token: 0x0600029D RID: 669 RVA: 0x000101A5 File Offset: 0x0000E3A5
		public int Priority { get; set; }

		// Token: 0x0600029E RID: 670 RVA: 0x000101B0 File Offset: 0x0000E3B0
		public int[] EnableDisableAssignment(bool enable, int[] netObjectIDs)
		{
			return (from p in this.pollersDAL.UpdatePollersStatus(this.pollerTypePatterns, enable, netObjectIDs)
			select p.NetObjectID).Distinct<int>().ToArray<int>();
		}

		// Token: 0x0600029F RID: 671 RVA: 0x00010200 File Offset: 0x0000E400
		public int[] EnableDisableAssignment(bool enable)
		{
			return (from p in this.pollersDAL.UpdatePollersStatus(this.pollerTypePatterns, enable, null)
			select p.NetObjectID).Distinct<int>().ToArray<int>();
		}

		// Token: 0x060002A0 RID: 672 RVA: 0x0001024E File Offset: 0x0000E44E
		public IEnumerable<TechnologyPollingAssignment> GetAssignments()
		{
			return this.GetAssignments(null);
		}

		// Token: 0x060002A1 RID: 673 RVA: 0x00010257 File Offset: 0x0000E457
		public IEnumerable<TechnologyPollingAssignment> GetAssignments(int[] netObjectIDs)
		{
			return this.pollersDAL.GetPollersAssignmentsGroupedByNetObjectIdMatchingAllPollerTypes(this.TechnologyPollingID, this.pollerTypePatterns, netObjectIDs);
		}

		// Token: 0x0400006C RID: 108
		private PollersDAL pollersDAL = new PollersDAL();

		// Token: 0x0400006D RID: 109
		protected string[] pollerTypePatterns;
	}
}
