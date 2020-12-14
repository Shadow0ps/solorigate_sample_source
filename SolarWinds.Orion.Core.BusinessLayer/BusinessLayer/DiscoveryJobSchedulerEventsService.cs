using System;
using System.ServiceModel;
using System.Text;
using SolarWinds.JobEngine;
using SolarWinds.Orion.Core.Strings;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000024 RID: 36
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true, AutomaticSessionShutdown = true, ConcurrencyMode = ConcurrencyMode.Multiple)]
	internal class DiscoveryJobSchedulerEventsService : JobSchedulerEventsService
	{
		// Token: 0x06000324 RID: 804 RVA: 0x00013E44 File Offset: 0x00012044
		public DiscoveryJobSchedulerEventsService(CoreBusinessLayerPlugin parent) : base(parent)
		{
			this.resultsManager.HandleResultsOfCancelledJobs = true;
		}

		// Token: 0x06000325 RID: 805 RVA: 0x00013E59 File Offset: 0x00012059
		protected override void ProcessJobProgress(JobProgress jobProgress)
		{
			this.RemoveOldDiscoveryJob(jobProgress.JobId);
		}

		// Token: 0x06000326 RID: 806 RVA: 0x00013E67 File Offset: 0x00012067
		protected override void ProcessJobResult(FinishedJobInfo jobInfo)
		{
			this.RemoveOldDiscoveryJob(jobInfo.ScheduledJobId);
		}

		// Token: 0x06000327 RID: 807 RVA: 0x00013E67 File Offset: 0x00012067
		protected override void ProcessJobFailure(FinishedJobInfo jobInfo)
		{
			this.RemoveOldDiscoveryJob(jobInfo.ScheduledJobId);
		}

		// Token: 0x06000328 RID: 808 RVA: 0x00013E78 File Offset: 0x00012078
		private string ComposeNotificationMessage(int newNodes, int changedNodes)
		{
			StringBuilder stringBuilder = new StringBuilder(Resources.LIBCODE_PCC_18);
			stringBuilder.Append(" ");
			if (newNodes == 1)
			{
				stringBuilder.Append(Resources.LIBCODE_PCC_19);
			}
			else if (newNodes > 1)
			{
				stringBuilder.AppendFormat(Resources.LIBCODE_PCC_20, newNodes);
			}
			if (changedNodes > 0)
			{
				if (newNodes >= 0)
				{
					stringBuilder.Append(" ");
				}
				if (changedNodes == 1)
				{
					stringBuilder.Append(Resources.LIBCODE_PCC_21);
				}
				else
				{
					stringBuilder.AppendFormat(Resources.LIBCODE_PCC_22, changedNodes);
				}
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06000329 RID: 809 RVA: 0x00013F04 File Offset: 0x00012104
		private void RemoveOldDiscoveryJob(Guid jobId)
		{
			if (jobId == Guid.Empty)
			{
				JobSchedulerEventsService.log.ErrorFormat("Unable to identify id of old discovery job to delete.", Array.Empty<object>());
				return;
			}
			try
			{
				JobSchedulerEventsService.log.InfoFormat("Deleting old discovery job [{0}]", jobId);
				if (!DiscoveryJobFactory.DeleteJob(jobId))
				{
					JobSchedulerEventsService.log.ErrorFormat("Error when deleting old discovery job [{0}]", jobId);
				}
			}
			catch (Exception ex)
			{
				JobSchedulerEventsService.log.Error(string.Format("Exception occured when deleting old discovery job [{0}]", jobId), ex);
			}
		}
	}
}
