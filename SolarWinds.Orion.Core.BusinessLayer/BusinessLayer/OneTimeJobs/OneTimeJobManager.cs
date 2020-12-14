using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Threading;
using SolarWinds.Common.IO;
using SolarWinds.JobEngine;
using SolarWinds.JobEngine.Security;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common.JobEngine;
using SolarWinds.Orion.Core.Strings;

namespace SolarWinds.Orion.Core.BusinessLayer.OneTimeJobs
{
	// Token: 0x0200006A RID: 106
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true, AutomaticSessionShutdown = true, ConcurrencyMode = ConcurrencyMode.Multiple)]
	public class OneTimeJobManager : JobSchedulerEventServicev2, IOneTimeJobManager
	{
		// Token: 0x14000009 RID: 9
		// (add) Token: 0x0600059D RID: 1437 RVA: 0x00021F04 File Offset: 0x00020104
		// (remove) Token: 0x0600059E RID: 1438 RVA: 0x00021F3C File Offset: 0x0002013C
		public event EventHandler<EventArgs> JobStarted;

		// Token: 0x0600059F RID: 1439 RVA: 0x00021F71 File Offset: 0x00020171
		public OneTimeJobManager() : this(null)
		{
		}

		// Token: 0x060005A0 RID: 1440 RVA: 0x00021F7A File Offset: 0x0002017A
		public OneTimeJobManager(IServiceStateProvider parent) : this(parent, () => JobScheduler.GetLocalInstance(), TimeSpan.FromSeconds(10.0))
		{
		}

		// Token: 0x060005A1 RID: 1441 RVA: 0x00021FB0 File Offset: 0x000201B0
		internal OneTimeJobManager(IServiceStateProvider parent, Func<IJobSchedulerHelper> jobSchedulerHelperFactory, TimeSpan jobTimeoutTolerance) : base(parent)
		{
			this.jobSchedulerHelperFactory = jobSchedulerHelperFactory;
			this.jobTimeoutTolerance = jobTimeoutTolerance;
		}

		// Token: 0x060005A2 RID: 1442 RVA: 0x00022007 File Offset: 0x00020207
		public void SetListenerUri(string listenerUri)
		{
			this.listenerUri = listenerUri;
		}

		// Token: 0x060005A3 RID: 1443 RVA: 0x00022010 File Offset: 0x00020210
		private RSACryptoServiceProvider CreateCrypoService()
		{
			if (string.IsNullOrEmpty(this.schedulerPublicKey))
			{
				using (IJobSchedulerHelper jobSchedulerHelper = this.jobSchedulerHelperFactory())
				{
					this.schedulerPublicKey = jobSchedulerHelper.GetPublicKey();
				}
			}
			RSACryptoServiceProvider rsacryptoServiceProvider = new RSACryptoServiceProvider();
			rsacryptoServiceProvider.FromXmlString(this.schedulerPublicKey);
			return rsacryptoServiceProvider;
		}

		// Token: 0x060005A4 RID: 1444 RVA: 0x00022070 File Offset: 0x00020270
		private Credential EncryptCredentials(CredentialBase creds)
		{
			if (creds == null)
			{
				return Credential.Empty;
			}
			Credential result;
			using (RSACryptoServiceProvider rsacryptoServiceProvider = this.CreateCrypoService())
			{
				result = new Credential(creds, rsacryptoServiceProvider);
			}
			return result;
		}

		// Token: 0x060005A5 RID: 1445 RVA: 0x000220B4 File Offset: 0x000202B4
		private Guid SubmitScheduledJobToScheduler(ScheduledJob job)
		{
			Guid result;
			using (IJobSchedulerHelper jobSchedulerHelper = this.jobSchedulerHelperFactory())
			{
				OneTimeJobManager.Logger.Debug("Adding new job to Job Engine");
				result = jobSchedulerHelper.AddJob(job);
			}
			return result;
		}

		// Token: 0x060005A6 RID: 1446 RVA: 0x00022104 File Offset: 0x00020304
		public OneTimeJobRawResult ExecuteJob(JobDescription jobDescription, CredentialBase jobCredential = null)
		{
			if (this.listenerUri == string.Empty)
			{
				JobSchedulerEventServicev2.log.Error("ListenerUri remains uninitialized");
				OneTimeJobRawResult result = new OneTimeJobRawResult
				{
					Success = false,
					Error = Resources.TestErrorJobFailed
				};
				return result;
			}
			if (jobCredential != null)
			{
				jobDescription.Credential = this.EncryptCredentials(jobCredential);
			}
			if (jobDescription.SupportedRoles == null)
			{
				jobDescription.SupportedRoles = 7;
			}
			ScheduledJob job = new ScheduledJob
			{
				NotificationAddress = this.listenerUri,
				State = "CoreOneTimeJob",
				RunOnce = true,
				IsOneShot = true,
				Job = jobDescription
			};
			Guid guid;
			try
			{
				guid = this.SubmitScheduledJobToScheduler(job);
				OneTimeJobManager.Logger.DebugFormat("Job {0} scheduled", guid);
			}
			catch (Exception ex)
			{
				OneTimeJobManager.Logger.ErrorFormat("Failed to submit job: {0}", ex);
				OneTimeJobRawResult result = default(OneTimeJobRawResult);
				result.Success = false;
				result.Error = Resources.TestErrorJobFailed;
				result.ExceptionFromJob = ex;
				return result;
			}
			TimeSpan timeSpan = jobDescription.Timeout.Add(this.jobTimeoutTolerance);
			OneTimeJobManager.PendingJobItem pendingJobItem = new OneTimeJobManager.PendingJobItem();
			this.pendingJobs[guid] = pendingJobItem;
			if (this.JobStarted != null)
			{
				this.JobStarted(this, new OneTimeJobManager.JobStartedEventArgs(guid));
			}
			OneTimeJobRawResult result2;
			if (pendingJobItem.WaitHandle.WaitOne(timeSpan))
			{
				result2 = pendingJobItem.RawResult;
			}
			else
			{
				OneTimeJobManager.Logger.ErrorFormat("No result from job {0} received before timeout ({1})", guid, timeSpan);
				result2 = new OneTimeJobRawResult
				{
					Success = false,
					Error = Resources.TestErrorTimeout
				};
			}
			this.pendingJobs.TryRemove(guid, out pendingJobItem);
			return result2;
		}

		// Token: 0x060005A7 RID: 1447 RVA: 0x000222BC File Offset: 0x000204BC
		protected override void ProcessJobProgress(JobProgress jobProgress)
		{
			OneTimeJobManager.Logger.InfoFormat("Progress from job {0}: {1}", jobProgress.JobId, jobProgress.Progress);
		}

		// Token: 0x060005A8 RID: 1448 RVA: 0x000222E0 File Offset: 0x000204E0
		protected override void ProcessJobFailure(FinishedJobInfo jobResult)
		{
			Guid scheduledJobId = jobResult.ScheduledJobId;
			OneTimeJobManager.PendingJobItem pendingJobItem;
			if (this.pendingJobs.TryGetValue(scheduledJobId, out pendingJobItem))
			{
				OneTimeJobRawResult result = new OneTimeJobRawResult
				{
					Success = false,
					Error = Resources.TestErrorJobFailed
				};
				OneTimeJobManager.Logger.WarnFormat("Job {0} failed with error: {1}", scheduledJobId, jobResult.Result.Error);
				pendingJobItem.Done(result);
				return;
			}
			OneTimeJobManager.Logger.ErrorFormat("Failure of unknown job {0} received", scheduledJobId);
		}

		// Token: 0x060005A9 RID: 1449 RVA: 0x00022360 File Offset: 0x00020560
		protected override void ProcessJobResult(FinishedJobInfo jobResult)
		{
			Guid scheduledJobId = jobResult.ScheduledJobId;
			OneTimeJobManager.PendingJobItem pendingJobItem;
			if (this.pendingJobs.TryGetValue(scheduledJobId, out pendingJobItem))
			{
				OneTimeJobRawResult result = default(OneTimeJobRawResult);
				try
				{
					result.Success = (jobResult.Result.State == 6 && string.IsNullOrEmpty(jobResult.Result.Error));
					if (jobResult.Result.IsResultStreamed)
					{
						using (IJobSchedulerHelper jobSchedulerHelper = this.jobSchedulerHelperFactory())
						{
							using (Stream jobResultStream = jobSchedulerHelper.GetJobResultStream(jobResult.Result.JobId, "JobResult"))
							{
								result.JobResultStream = new DynamicStream();
								jobResultStream.CopyTo(result.JobResultStream);
								result.JobResultStream.Position = 0L;
							}
							jobSchedulerHelper.DeleteJobResult(jobResult.Result.JobId);
							goto IL_100;
						}
					}
					if (jobResult.Result.Output != null && jobResult.Result.Output.Length != 0)
					{
						result.JobResultStream = new MemoryStream(jobResult.Result.Output);
					}
					IL_100:
					result.Error = jobResult.Result.Error;
					OneTimeJobManager.Logger.InfoFormat("Result of one time job {0} received", scheduledJobId);
				}
				catch (Exception ex)
				{
					result.Success = false;
					result.Error = Resources.TestErrorInvalidResult;
					OneTimeJobManager.Logger.ErrorFormat("Failed to process result of one time job {0}: {1}", scheduledJobId, ex);
				}
				pendingJobItem.Done(result);
				return;
			}
			OneTimeJobManager.Logger.ErrorFormat("Result of unknown job {0} received", scheduledJobId);
			if (jobResult.Result != null && jobResult.Result.IsResultStreamed)
			{
				using (IJobSchedulerHelper jobSchedulerHelper2 = this.jobSchedulerHelperFactory())
				{
					jobSchedulerHelper2.DeleteJobResult(jobResult.Result.JobId);
				}
			}
		}

		// Token: 0x040001A1 RID: 417
		private static readonly Log Logger = new Log();

		// Token: 0x040001A2 RID: 418
		private string schedulerPublicKey = string.Empty;

		// Token: 0x040001A3 RID: 419
		private string listenerUri = string.Empty;

		// Token: 0x040001A4 RID: 420
		private ConcurrentDictionary<Guid, OneTimeJobManager.PendingJobItem> pendingJobs = new ConcurrentDictionary<Guid, OneTimeJobManager.PendingJobItem>();

		// Token: 0x040001A5 RID: 421
		private readonly Func<IJobSchedulerHelper> jobSchedulerHelperFactory;

		// Token: 0x040001A6 RID: 422
		private readonly TimeSpan jobTimeoutTolerance = TimeSpan.FromSeconds(10.0);

		// Token: 0x02000159 RID: 345
		public class JobStartedEventArgs : EventArgs
		{
			// Token: 0x17000148 RID: 328
			// (get) Token: 0x06000B8E RID: 2958 RVA: 0x00049204 File Offset: 0x00047404
			// (set) Token: 0x06000B8F RID: 2959 RVA: 0x0004920C File Offset: 0x0004740C
			public Guid JobId { get; private set; }

			// Token: 0x06000B90 RID: 2960 RVA: 0x00049215 File Offset: 0x00047415
			public JobStartedEventArgs(Guid jobId)
			{
				this.JobId = jobId;
			}
		}

		// Token: 0x0200015A RID: 346
		private class PendingJobItem
		{
			// Token: 0x17000149 RID: 329
			// (get) Token: 0x06000B91 RID: 2961 RVA: 0x00049224 File Offset: 0x00047424
			public ManualResetEvent WaitHandle
			{
				get
				{
					return this.waitHandle;
				}
			}

			// Token: 0x1700014A RID: 330
			// (get) Token: 0x06000B92 RID: 2962 RVA: 0x0004922C File Offset: 0x0004742C
			// (set) Token: 0x06000B93 RID: 2963 RVA: 0x00049234 File Offset: 0x00047434
			public OneTimeJobRawResult RawResult { get; private set; }

			// Token: 0x06000B94 RID: 2964 RVA: 0x0004923D File Offset: 0x0004743D
			public void Done(OneTimeJobRawResult result)
			{
				this.RawResult = result;
				this.waitHandle.Set();
			}

			// Token: 0x04000469 RID: 1129
			private ManualResetEvent waitHandle = new ManualResetEvent(false);
		}
	}
}
