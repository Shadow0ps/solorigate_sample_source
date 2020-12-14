using System;
using System.ServiceModel;
using SolarWinds.JobEngine;
using SolarWinds.Orion.Core.BusinessLayer.Discovery;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.BusinessLayer;
using SolarWinds.Orion.Core.Common.Proxy.BusinessLayer;
using SolarWinds.ServiceDirectory.Client.Contract;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200000F RID: 15
	internal class CoreBusinessLayerServiceInstance : BusinessLayerServiceInstanceBase<CoreBusinessLayerService>
	{
		// Token: 0x06000249 RID: 585 RVA: 0x0000FDEC File Offset: 0x0000DFEC
		public CoreBusinessLayerServiceInstance(int engineId, IEngineInitiator engineInitiator, CoreBusinessLayerService serviceInstance, ServiceHostBase serviceHost, IServiceDirectoryClient serviceDirectoryClient) : base(engineId, engineInitiator.ServerName, serviceInstance, serviceHost, serviceDirectoryClient)
		{
			this._engineInitiator = engineInitiator;
			this.Service = serviceInstance;
			this.ServiceLogicalInstanceId = CoreBusinessLayerConfiguration.GetLogicalInstanceId(base.EngineId);
		}

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x0600024A RID: 586 RVA: 0x0000FE1F File Offset: 0x0000E01F
		public CoreBusinessLayerService Service { get; }

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x0600024B RID: 587 RVA: 0x0000FE27 File Offset: 0x0000E027
		protected override string ServiceId
		{
			get
			{
				return "Core.BusinessLayer";
			}
		}

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x0600024C RID: 588 RVA: 0x0000FE2E File Offset: 0x0000E02E
		protected override string ServiceLogicalInstanceId { get; }

		// Token: 0x0600024D RID: 589 RVA: 0x0000FE36 File Offset: 0x0000E036
		public void RouteJobToEngine(JobDescription jobDescription)
		{
			if (!string.IsNullOrEmpty(jobDescription.LegacyEngine))
			{
				return;
			}
			jobDescription.LegacyEngine = base.EngineName;
		}

		// Token: 0x0600024E RID: 590 RVA: 0x0000FE54 File Offset: 0x0000E054
		public void StopRescheduleEngineDiscoveryJobsTask()
		{
			using (this._discoveryJobRescheduler)
			{
				this._discoveryJobRescheduler = null;
			}
		}

		// Token: 0x0600024F RID: 591 RVA: 0x0000FE8C File Offset: 0x0000E08C
		public void InitRescheduleEngineDiscoveryJobsTask(bool isMaster)
		{
			bool keepRunning = !isMaster;
			TimeSpan periodicRetryInterval = isMaster ? TimeSpan.FromSeconds(10.0) : TimeSpan.FromMinutes(10.0);
			this._discoveryJobRescheduler = new RescheduleDiscoveryJobsTask(new Func<int, bool>(this.Service.UpdateDiscoveryJobs), base.EngineId, keepRunning, periodicRetryInterval);
			this._discoveryJobRescheduler.StartPeriodicRescheduleTask();
		}

		// Token: 0x06000250 RID: 592 RVA: 0x0000FEEF File Offset: 0x0000E0EF
		public void RunRescheduleEngineDiscoveryJobsTask()
		{
			RescheduleDiscoveryJobsTask discoveryJobRescheduler = this._discoveryJobRescheduler;
			if (discoveryJobRescheduler == null)
			{
				return;
			}
			discoveryJobRescheduler.QueueRescheduleAttempt();
		}

		// Token: 0x06000251 RID: 593 RVA: 0x0000FF01 File Offset: 0x0000E101
		public void InitializeEngine()
		{
			this._engineInitiator.InitializeEngine();
		}

		// Token: 0x06000252 RID: 594 RVA: 0x0000FF0E File Offset: 0x0000E10E
		public void UpdateEngine(bool updateJobEngineThrottleInfo)
		{
			this._engineInitiator.UpdateInfo(updateJobEngineThrottleInfo);
		}

		// Token: 0x04000064 RID: 100
		private RescheduleDiscoveryJobsTask _discoveryJobRescheduler;

		// Token: 0x04000065 RID: 101
		private readonly IEngineInitiator _engineInitiator;
	}
}
