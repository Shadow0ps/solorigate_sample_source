using System;
using System.Collections.Generic;
using System.IdentityModel.Selectors;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Threading;
using System.Xml;
using SolarWinds.JobEngine;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.JobEngine;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000025 RID: 37
	internal class JobScheduler : IJobSchedulerHelper, IJobScheduler, IDisposable
	{
		// Token: 0x0600032A RID: 810 RVA: 0x00013F98 File Offset: 0x00012198
		public static IJobSchedulerHelper GetInstance()
		{
			return new JobScheduler(new JobEngineWcfCredentialProvider());
		}

		// Token: 0x0600032B RID: 811 RVA: 0x00013FA4 File Offset: 0x000121A4
		public JobScheduler(IJobEngineWcfCredentialProvider wcfCredentialProvider)
		{
			if (wcfCredentialProvider == null)
			{
				throw new ArgumentNullException("wcfCredentialProvider");
			}
			this.wcfCredentialProvider = wcfCredentialProvider;
		}

		// Token: 0x0600032C RID: 812 RVA: 0x00013FC2 File Offset: 0x000121C2
		private T ExecuteJobSchedulerOperation<T>(string operationName, JobScheduler.JobSchedulerOperation<T> operation)
		{
			return this.ExecuteJobSchedulerOperation<T>(operationName, operation, true);
		}

		// Token: 0x0600032D RID: 813 RVA: 0x00013FD0 File Offset: 0x000121D0
		private T ExecuteJobSchedulerOperation<T>(string operationName, JobScheduler.JobSchedulerOperation<T> operation, bool retryOnError)
		{
			int num = 0;
			int i = 0;
			while (i < JobScheduler.Settings.RetryCount)
			{
				num++;
				try
				{
					this.Instantiate();
					return operation();
				}
				catch (TimeoutException ex)
				{
					JobScheduler.log.WarnFormat("{0}: JobScheduler channel threw TimeoutException. Retries: {1}", operationName, i);
					JobScheduler.log.Warn("Job Scheduler Exception:", ex);
					Thread.Sleep(JobScheduler.Settings.RetryInterval);
				}
				catch (ActionNotSupportedException ex2)
				{
					JobScheduler.log.WarnFormat("{0}: JobScheduler channel threw ActionNotSupportedException", operationName);
					JobScheduler.log.Warn("Job Scheduler Exception:", ex2);
					throw;
				}
				catch (FaultException ex3)
				{
					JobScheduler.log.WarnFormat("{0}: JobScheduler channel threw FaultException", operationName);
					JobScheduler.log.Warn("Job Scheduler Exception:", ex3);
					throw;
				}
				catch (CommunicationException ex4)
				{
					JobScheduler.log.WarnFormat("{0}: JobScheduler channel threw CommunicationException. Retries: {1}", operationName, i);
					JobScheduler.log.Warn("Job Scheduler Exception:", ex4);
					Thread.Sleep(JobScheduler.Settings.RetryInterval);
				}
				if (retryOnError)
				{
					i++;
					continue;
				}
				break;
			}
			JobScheduler.log.ErrorFormat("{0}: Could not reach JobScheduler service at {1} after {2} retries", operationName, BusinessLayerSettings.Instance.JobSchedulerEndpointNetPipe, num);
			throw new ApplicationException(string.Format("Could not reach JobScheduler service after {0} retries", num));
		}

		// Token: 0x0600032E RID: 814 RVA: 0x0001412C File Offset: 0x0001232C
		public void CancelJob(Guid jobId)
		{
			this.ExecuteJobSchedulerOperation<int>("CancelJob", delegate()
			{
				this.schedulerChannel.CancelJob(jobId);
				return 0;
			});
		}

		// Token: 0x0600032F RID: 815 RVA: 0x00014168 File Offset: 0x00012368
		public void Clear(string productNamespace)
		{
			this.ExecuteJobSchedulerOperation<int>("Clear", delegate()
			{
				this.schedulerChannel.Clear(productNamespace);
				return 0;
			});
		}

		// Token: 0x06000330 RID: 816 RVA: 0x000141A4 File Offset: 0x000123A4
		public void AddExecutionEngine(Uri executionEngineUri, bool enabled)
		{
			this.ExecuteJobSchedulerOperation<int>("AddExecutionEngine", delegate()
			{
				this.schedulerChannel.AddExecutionEngine(executionEngineUri, enabled);
				return 0;
			});
		}

		// Token: 0x06000331 RID: 817 RVA: 0x000141E4 File Offset: 0x000123E4
		public Guid AddJob(ScheduledJob jobConfiguration)
		{
			return this.ExecuteJobSchedulerOperation<Guid>("AddJob", () => this.schedulerChannel.AddJob(jobConfiguration));
		}

		// Token: 0x06000332 RID: 818 RVA: 0x0001421C File Offset: 0x0001241C
		public void AddPolicy(XmlElement policy)
		{
			this.ExecuteJobSchedulerOperation<int>("AddPolicy", delegate()
			{
				this.schedulerChannel.AddPolicy(policy);
				return 0;
			});
		}

		// Token: 0x06000333 RID: 819 RVA: 0x00014258 File Offset: 0x00012458
		public void RemoveExecutionEngine(Uri executionEngineUri)
		{
			this.ExecuteJobSchedulerOperation<int>("RemoveExecutionEngine", delegate()
			{
				this.schedulerChannel.RemoveExecutionEngine(executionEngineUri);
				return 0;
			});
		}

		// Token: 0x06000334 RID: 820 RVA: 0x00014294 File Offset: 0x00012494
		public void RemoveJob(Guid jobId)
		{
			try
			{
				this.ExecuteJobSchedulerOperation<int>("RemoveJob", delegate()
				{
					this.schedulerChannel.RemoveJob(jobId);
					return 0;
				});
			}
			catch (Exception ex)
			{
				JobScheduler.log.WarnFormat("Exception while removing job {0}.  Exception: {1}", jobId, ex);
			}
		}

		// Token: 0x06000335 RID: 821 RVA: 0x00014300 File Offset: 0x00012500
		public void RemoveJobs(Guid[] jobIds)
		{
			try
			{
				this.ExecuteJobSchedulerOperation<int>("RemoveJobs", delegate()
				{
					this.schedulerChannel.RemoveJobs(jobIds);
					return 0;
				});
			}
			catch (Exception ex)
			{
				JobScheduler.log.WarnFormat("Exception while removing jobs {0}.  Exception: {1}", jobIds, ex);
			}
		}

		// Token: 0x06000336 RID: 822 RVA: 0x00014364 File Offset: 0x00012564
		public void RemovePolicy(string policyId)
		{
			this.ExecuteJobSchedulerOperation<int>("RemovePolicy", delegate()
			{
				this.schedulerChannel.RemovePolicy(policyId);
				return 0;
			});
		}

		// Token: 0x06000337 RID: 823 RVA: 0x000143A0 File Offset: 0x000125A0
		public bool ExecutionEngineExists(Uri executionEngineUri)
		{
			return this.ExecuteJobSchedulerOperation<bool>("ExecutionEngineExists", () => this.schedulerChannel.ExecutionEngineExists(executionEngineUri), false);
		}

		// Token: 0x06000338 RID: 824 RVA: 0x000143DC File Offset: 0x000125DC
		public bool PolicyExists(string policyId)
		{
			return this.ExecuteJobSchedulerOperation<bool>("PolicyExists", () => this.schedulerChannel.PolicyExists(policyId));
		}

		// Token: 0x06000339 RID: 825 RVA: 0x00014414 File Offset: 0x00012614
		public void UpdateJob(Guid jobId, ScheduledJob job, bool executeImmediately)
		{
			this.ExecuteJobSchedulerOperation<int>("UpdateJob", delegate()
			{
				this.schedulerChannel.UpdateJob(jobId, job, executeImmediately);
				return 0;
			});
		}

		// Token: 0x0600033A RID: 826 RVA: 0x0001445B File Offset: 0x0001265B
		public string GetPublicKey()
		{
			return this.ExecuteJobSchedulerOperation<string>("GetPublicKey", () => this.schedulerChannel.GetPublicKey());
		}

		// Token: 0x0600033B RID: 827 RVA: 0x00014474 File Offset: 0x00012674
		public IList<ExecutionEngineInfo> EnumerateExecutionEngines()
		{
			return this.ExecuteJobSchedulerOperation<IList<ExecutionEngineInfo>>("EnumerateExecutionEngines", () => this.schedulerChannel.EnumerateExecutionEngines());
		}

		// Token: 0x0600033C RID: 828 RVA: 0x0001448D File Offset: 0x0001268D
		public ScheduledJobInfo[] EnumerateScheduledJobs()
		{
			return this.ExecuteJobSchedulerOperation<ScheduledJobInfo[]>("EnumerateScheduledJobs", () => this.schedulerChannel.EnumerateScheduledJobs());
		}

		// Token: 0x0600033D RID: 829 RVA: 0x000144A6 File Offset: 0x000126A6
		public IList<XmlElement> EnumeratePolicies()
		{
			return this.ExecuteJobSchedulerOperation<IList<XmlElement>>("EnumeratePolicies", () => this.schedulerChannel.EnumeratePolicies());
		}

		// Token: 0x0600033E RID: 830 RVA: 0x000144C0 File Offset: 0x000126C0
		public void ResumeExecutionEngine(Uri executionEngineUri)
		{
			this.ExecuteJobSchedulerOperation<int>("ResumeExecutionEngine", delegate()
			{
				this.schedulerChannel.ResumeExecutionEngine(executionEngineUri);
				return 0;
			});
		}

		// Token: 0x0600033F RID: 831 RVA: 0x000144FC File Offset: 0x000126FC
		public void SuspendExecutionEngine(Uri executionEngineUri)
		{
			this.ExecuteJobSchedulerOperation<int>("SuspendExecutionEngine", delegate()
			{
				this.schedulerChannel.SuspendExecutionEngine(executionEngineUri);
				return 0;
			});
		}

		// Token: 0x06000340 RID: 832 RVA: 0x00014538 File Offset: 0x00012738
		public void UpdatePolicy(XmlElement policy)
		{
			this.ExecuteJobSchedulerOperation<int>("UpdatePolicy", delegate()
			{
				this.schedulerChannel.UpdatePolicy(policy);
				return 0;
			});
		}

		// Token: 0x06000341 RID: 833 RVA: 0x00014574 File Offset: 0x00012774
		public Stream GetJobResultStream(Guid jobId, string streamName)
		{
			return this.ExecuteJobSchedulerOperation<Stream>("GetJobResultStream", () => this.schedulerChannel.GetJobResultStream(jobId, streamName));
		}

		// Token: 0x06000342 RID: 834 RVA: 0x000145B4 File Offset: 0x000127B4
		public void DeleteJobResult(Guid jobId)
		{
			this.ExecuteJobSchedulerOperation<int>("DeleteJobResult", delegate()
			{
				this.schedulerChannel.DeleteJobResult(jobId);
				return 0;
			});
		}

		// Token: 0x06000343 RID: 835 RVA: 0x000145F0 File Offset: 0x000127F0
		~JobScheduler()
		{
			this.Dispose(false);
		}

		// Token: 0x06000344 RID: 836 RVA: 0x00014620 File Offset: 0x00012820
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06000345 RID: 837 RVA: 0x0001462F File Offset: 0x0001282F
		protected void Dispose(bool disposing)
		{
			if (this.schedulerChannel == null)
			{
				return;
			}
			MessageUtilities.ShutdownCommunicationObject((ICommunicationObject)this.schedulerChannel);
		}

		// Token: 0x06000346 RID: 838 RVA: 0x0001464C File Offset: 0x0001284C
		private void Instantiate()
		{
			string text = WebSettingsDAL.Get("JobSchedulerHost");
			ChannelFactory<IJobScheduler> channelFactory;
			if (string.IsNullOrEmpty(text) || Environment.MachineName.Equals(text, StringComparison.OrdinalIgnoreCase))
			{
				channelFactory = this.MakeNamedPipeChannelFactory();
			}
			else
			{
				channelFactory = this.MakeTcpChannelFactory(text);
			}
			if (this.schedulerChannel != null)
			{
				this.Dispose(false);
			}
			this.schedulerChannel = channelFactory.CreateChannel();
		}

		// Token: 0x06000347 RID: 839 RVA: 0x000146A8 File Offset: 0x000128A8
		private ChannelFactory<IJobScheduler> MakeTcpChannelFactory(string host)
		{
			Binding binding = new NetTcpBinding("Core.NetTcpBinding.ToJobScheduler");
			string text = string.Format(BusinessLayerSettings.Instance.JobSchedulerEndpointTcpPipe, host);
			JobScheduler.log.DebugFormat("Channel created to {0}", text);
			EndpointAddress remoteAddress = new EndpointAddress(new Uri(text), EndpointIdentity.CreateDnsIdentity("SolarWinds JobEngine Security"), Array.Empty<AddressHeader>());
			ChannelFactory<IJobScheduler> channelFactory = new ChannelFactory<IJobScheduler>(binding, remoteAddress);
			channelFactory.Credentials.UserName.UserName = this.wcfCredentialProvider.UserName;
			channelFactory.Credentials.UserName.Password = this.wcfCredentialProvider.Password;
			X509ChainPolicy x509ChainPolicy = new X509ChainPolicy();
			x509ChainPolicy.VerificationFlags = (X509VerificationFlags.IgnoreNotTimeValid | X509VerificationFlags.AllowUnknownCertificateAuthority);
			channelFactory.Credentials.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.Custom;
			channelFactory.Credentials.ServiceCertificate.Authentication.CustomCertificateValidator = X509CertificateValidator.CreateChainTrustValidator(true, x509ChainPolicy);
			return channelFactory;
		}

		// Token: 0x06000348 RID: 840 RVA: 0x00014778 File Offset: 0x00012978
		private ChannelFactory<IJobScheduler> MakeNamedPipeChannelFactory()
		{
			Binding binding = new NetNamedPipeBinding("Core.NamedPipeClientBinding.ToJobScheduler");
			string jobSchedulerEndpointNetPipe = BusinessLayerSettings.Instance.JobSchedulerEndpointNetPipe;
			JobScheduler.log.DebugFormat("Channel created to {0}", jobSchedulerEndpointNetPipe);
			EndpointAddress remoteAddress = new EndpointAddress(new Uri(jobSchedulerEndpointNetPipe), Array.Empty<AddressHeader>());
			return new ChannelFactory<IJobScheduler>(binding, remoteAddress);
		}

		// Token: 0x040000A6 RID: 166
		private static readonly Log log = new Log();

		// Token: 0x040000A7 RID: 167
		private readonly IJobEngineWcfCredentialProvider wcfCredentialProvider;

		// Token: 0x040000A8 RID: 168
		private IJobScheduler schedulerChannel;

		// Token: 0x02000116 RID: 278
		// (Invoke) Token: 0x06000AAF RID: 2735
		private delegate T JobSchedulerOperation<T>();

		// Token: 0x02000117 RID: 279
		internal static class Settings
		{
			// Token: 0x040003B7 RID: 951
			public static int RetryCount = 3;

			// Token: 0x040003B8 RID: 952
			public static TimeSpan RetryInterval = TimeSpan.FromSeconds(1.0);
		}
	}
}
