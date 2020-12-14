using System;
using System.ServiceModel;
using SolarWinds.JobEngine;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.i18n;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000027 RID: 39
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true, AutomaticSessionShutdown = true, ConcurrencyMode = ConcurrencyMode.Multiple)]
	internal abstract class JobSchedulerEventsService : IJobSchedulerEvents
	{
		// Token: 0x0600034E RID: 846 RVA: 0x00014804 File Offset: 0x00012A04
		public JobSchedulerEventsService(CoreBusinessLayerPlugin parent)
		{
			this.parent = parent;
			JobResultsManager jobResultsManager = this.resultsManager;
			jobResultsManager.JobFailure = (JobResultsManager.JobFailureDelegate)Delegate.Combine(jobResultsManager.JobFailure, new JobResultsManager.JobFailureDelegate(this.ProcessJobFailure));
		}

		// Token: 0x0600034F RID: 847 RVA: 0x00014854 File Offset: 0x00012A54
		public void OnJobProgress(JobProgress[] jobProgressInfo)
		{
			using (LocaleThreadState.EnsurePrimaryLocale())
			{
				foreach (JobProgress jobProgress in jobProgressInfo)
				{
					this.ProcessJobProgress(jobProgress);
				}
			}
		}

		// Token: 0x06000350 RID: 848 RVA: 0x000148A0 File Offset: 0x00012AA0
		public void OnJobFinished(FinishedJobInfo[] jobFinishedInfo)
		{
			using (LocaleThreadState.EnsurePrimaryLocale())
			{
				if (this.parent.IsServiceDown)
				{
					JobSchedulerEventsService.log.InfoFormat("Core Service Engine is in an invalid state.  Job results will be discarded.", Array.Empty<object>());
				}
				else
				{
					this.resultsManager.AddJobResults(jobFinishedInfo);
					for (FinishedJobInfo jobResult = this.resultsManager.GetJobResult(); jobResult != null; jobResult = this.resultsManager.GetJobResult())
					{
						try
						{
							this.ProcessJobResult(jobResult);
						}
						catch (Exception ex)
						{
							JobSchedulerEventsService.log.Error("Error processing job", ex);
						}
						finally
						{
							this.resultsManager.FinishProcessingJobResult(jobResult);
						}
					}
				}
			}
		}

		// Token: 0x06000351 RID: 849
		protected abstract void ProcessJobProgress(JobProgress jobProgress);

		// Token: 0x06000352 RID: 850
		protected abstract void ProcessJobFailure(FinishedJobInfo jobResult);

		// Token: 0x06000353 RID: 851
		protected abstract void ProcessJobResult(FinishedJobInfo jobResult);

		// Token: 0x06000354 RID: 852 RVA: 0x00014960 File Offset: 0x00012B60
		protected void RemoveJob(Guid jobId)
		{
			try
			{
				using (IJobSchedulerHelper instance = JobScheduler.GetInstance())
				{
					JobSchedulerEventsService.log.DebugFormat("Removing job {0}", jobId);
					instance.RemoveJob(jobId);
				}
			}
			catch (Exception ex)
			{
				JobSchedulerEventsService.log.ErrorFormat("Error removing job {0}.  Exception: {1}", jobId, ex.ToString());
			}
		}

		// Token: 0x040000A9 RID: 169
		protected static readonly Log log = new Log();

		// Token: 0x040000AA RID: 170
		private readonly CoreBusinessLayerPlugin parent;

		// Token: 0x040000AB RID: 171
		protected JobResultsManager resultsManager = new JobResultsManager();
	}
}
