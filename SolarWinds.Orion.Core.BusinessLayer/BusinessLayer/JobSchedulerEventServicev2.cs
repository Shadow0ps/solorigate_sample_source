using System;
using System.ServiceModel;
using SolarWinds.JobEngine;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.JobEngine;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200001E RID: 30
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true, AutomaticSessionShutdown = true, ConcurrencyMode = ConcurrencyMode.Multiple)]
	public abstract class JobSchedulerEventServicev2 : IJobSchedulerEvents
	{
		// Token: 0x060002CB RID: 715 RVA: 0x00011704 File Offset: 0x0000F904
		public JobSchedulerEventServicev2() : this(null)
		{
		}

		// Token: 0x060002CC RID: 716 RVA: 0x00011710 File Offset: 0x0000F910
		public JobSchedulerEventServicev2(IServiceStateProvider parentService)
		{
			this.parentService = parentService;
			JobResultsManagerV2 jobResultsManagerV = this.resultsManager;
			jobResultsManagerV.JobFailure = (JobResultsManagerV2.JobFailureDelegate)Delegate.Combine(jobResultsManagerV.JobFailure, new JobResultsManagerV2.JobFailureDelegate(this.ProcessJobFailure));
		}

		// Token: 0x060002CD RID: 717 RVA: 0x00011760 File Offset: 0x0000F960
		public void OnJobProgress(JobProgress[] jobProgressInfo)
		{
			foreach (JobProgress jobProgress in jobProgressInfo)
			{
				this.ProcessJobProgress(jobProgress);
			}
		}

		// Token: 0x060002CE RID: 718 RVA: 0x00011788 File Offset: 0x0000F988
		public void OnJobFinished(FinishedJobInfo[] jobFinishedInfo)
		{
			IServiceStateProvider serviceStateProvider = this.parentService;
			if (serviceStateProvider != null && serviceStateProvider.IsServiceDown)
			{
				JobSchedulerEventServicev2.log.InfoFormat("Parent Service Engine is in an invalid state.  Job results will be discarded.", Array.Empty<object>());
				return;
			}
			this.resultsManager.AddJobResults(jobFinishedInfo);
			for (FinishedJobInfo jobResult = this.resultsManager.GetJobResult(); jobResult != null; jobResult = this.resultsManager.GetJobResult())
			{
				try
				{
					this.ProcessJobResult(jobResult);
				}
				catch (Exception ex)
				{
					JobSchedulerEventServicev2.log.Error("Error processing job", ex);
				}
				finally
				{
					this.resultsManager.FinishProcessingJobResult(jobResult);
				}
			}
		}

		// Token: 0x060002CF RID: 719
		protected abstract void ProcessJobProgress(JobProgress jobProgress);

		// Token: 0x060002D0 RID: 720
		protected abstract void ProcessJobFailure(FinishedJobInfo jobResult);

		// Token: 0x060002D1 RID: 721
		protected abstract void ProcessJobResult(FinishedJobInfo jobResult);

		// Token: 0x060002D2 RID: 722 RVA: 0x00011830 File Offset: 0x0000FA30
		protected void RemoveJob(Guid jobId)
		{
			try
			{
				using (IJobSchedulerHelper localInstance = JobScheduler.GetLocalInstance())
				{
					JobSchedulerEventServicev2.log.DebugFormat("Removing job {0}", jobId);
					localInstance.RemoveJob(jobId);
				}
			}
			catch (Exception ex)
			{
				JobSchedulerEventServicev2.log.ErrorFormat("Error removing job {0}.  Exception: {1}", jobId, ex.ToString());
			}
		}

		// Token: 0x0400007C RID: 124
		protected static readonly Log log = new Log();

		// Token: 0x0400007D RID: 125
		private readonly IServiceStateProvider parentService;

		// Token: 0x0400007E RID: 126
		protected JobResultsManagerV2 resultsManager = new JobResultsManagerV2();
	}
}
