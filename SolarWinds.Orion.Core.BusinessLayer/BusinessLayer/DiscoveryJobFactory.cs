using System;
using SolarWinds.Logging;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000023 RID: 35
	public static class DiscoveryJobFactory
	{
		// Token: 0x06000322 RID: 802 RVA: 0x00013DD8 File Offset: 0x00011FD8
		public static bool DeleteJob(Guid jobId)
		{
			bool result;
			using (IJobSchedulerHelper instance = JobScheduler.GetInstance())
			{
				try
				{
					instance.RemoveJob(jobId);
					result = true;
				}
				catch
				{
					DiscoveryJobFactory.log.DebugFormat("Unable to delete job in Job Engine({0}", jobId);
					result = false;
				}
			}
			return result;
		}

		// Token: 0x040000A5 RID: 165
		private static readonly Log log = new Log();
	}
}
