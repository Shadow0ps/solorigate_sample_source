using System;
using SolarWinds.JobEngine;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200001C RID: 28
	public interface IJobFactory
	{
		// Token: 0x060002C6 RID: 710
		Guid SubmitScheduledJob(Guid jobId, ScheduledJob job, bool executeImmediately);

		// Token: 0x060002C7 RID: 711
		ScheduledJob CreateDiscoveryJob(DiscoveryConfiguration configuration);

		// Token: 0x060002C8 RID: 712
		bool DeleteJob(Guid jobId);

		// Token: 0x060002C9 RID: 713
		Guid SubmitScheduledJobToLocalEngine(Guid jobId, ScheduledJob job, bool executeImmediately);
	}
}
