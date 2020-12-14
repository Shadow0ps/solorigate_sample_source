using System;
using System.Collections.Generic;
using System.Linq;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Models.Interfaces;
using SolarWinds.Orion.Core.Models.Technology;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200003E RID: 62
	public class TechnologyPollingByPollers : ITechnologyPolling
	{
		// Token: 0x0600040D RID: 1037 RVA: 0x0001BCF4 File Offset: 0x00019EF4
		public TechnologyPollingByPollers(string technologyID, string technologyPollingID, string displayName, int priority, string[] pollerTypePatterns)
		{
			this.TechnologyID = technologyID;
			this.TechnologyPollingID = technologyPollingID;
			this.DisplayName = displayName;
			this.Priority = priority;
			this.pollerTypePatterns = pollerTypePatterns;
		}

		// Token: 0x17000078 RID: 120
		// (get) Token: 0x0600040E RID: 1038 RVA: 0x0001BD2C File Offset: 0x00019F2C
		// (set) Token: 0x0600040F RID: 1039 RVA: 0x0001BD34 File Offset: 0x00019F34
		public string TechnologyID { get; set; }

		// Token: 0x17000079 RID: 121
		// (get) Token: 0x06000410 RID: 1040 RVA: 0x0001BD3D File Offset: 0x00019F3D
		// (set) Token: 0x06000411 RID: 1041 RVA: 0x0001BD45 File Offset: 0x00019F45
		public string TechnologyPollingID { get; set; }

		// Token: 0x1700007A RID: 122
		// (get) Token: 0x06000412 RID: 1042 RVA: 0x0001BD4E File Offset: 0x00019F4E
		// (set) Token: 0x06000413 RID: 1043 RVA: 0x0001BD56 File Offset: 0x00019F56
		public string DisplayName { get; set; }

		// Token: 0x1700007B RID: 123
		// (get) Token: 0x06000414 RID: 1044 RVA: 0x0001BD5F File Offset: 0x00019F5F
		// (set) Token: 0x06000415 RID: 1045 RVA: 0x0001BD67 File Offset: 0x00019F67
		public int Priority { get; set; }

		// Token: 0x06000416 RID: 1046 RVA: 0x0001BD70 File Offset: 0x00019F70
		public int[] EnableDisableAssignment(bool enable, int[] netObjectIDs)
		{
			return (from p in this.pollersDAL.UpdatePollersStatus(this.pollerTypePatterns, enable, netObjectIDs)
			select p.NetObjectID).Distinct<int>().ToArray<int>();
		}

		// Token: 0x06000417 RID: 1047 RVA: 0x0001BDC0 File Offset: 0x00019FC0
		public int[] EnableDisableAssignment(bool enable)
		{
			return (from p in this.pollersDAL.UpdatePollersStatus(this.pollerTypePatterns, enable, null)
			select p.NetObjectID).Distinct<int>().ToArray<int>();
		}

		// Token: 0x06000418 RID: 1048 RVA: 0x0001BE0E File Offset: 0x0001A00E
		public IEnumerable<TechnologyPollingAssignment> GetAssignments()
		{
			return this.GetAssignments(null);
		}

		// Token: 0x06000419 RID: 1049 RVA: 0x0001BE17 File Offset: 0x0001A017
		public IEnumerable<TechnologyPollingAssignment> GetAssignments(int[] netObjectIDs)
		{
			return this.pollersDAL.GetPollersAssignmentsGroupedByNetObjectId(this.TechnologyPollingID, this.pollerTypePatterns, netObjectIDs);
		}

		// Token: 0x040000EE RID: 238
		private PollersDAL pollersDAL = new PollersDAL();

		// Token: 0x040000EF RID: 239
		protected string[] pollerTypePatterns;
	}
}
