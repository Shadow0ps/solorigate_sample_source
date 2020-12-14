using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using SolarWinds.BusinessLayerHost.Contract;
using SolarWinds.Common.Utility;
using SolarWinds.InformationService.Linq.Plugins.Core.Orion;
using SolarWinds.Logging;
using SolarWinds.Orion.Channels.Security;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Common.Models;
using SolarWinds.Orion.Core.Auditing;
using SolarWinds.Orion.Core.BusinessLayer.Agent;
using SolarWinds.Orion.Core.BusinessLayer.BackgroundInventory;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.BusinessLayer.DowntimeMonitoring;
using SolarWinds.Orion.Core.BusinessLayer.Engines;
using SolarWinds.Orion.Core.BusinessLayer.MaintenanceMode;
using SolarWinds.Orion.Core.BusinessLayer.NodeStatus;
using SolarWinds.Orion.Core.BusinessLayer.OneTimeJobs;
using SolarWinds.Orion.Core.BusinessLayer.Thresholds;
using SolarWinds.Orion.Core.CertificateUpdate;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Configuration;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Common.EntityMonitor;
using SolarWinds.Orion.Core.Common.i18n;
using SolarWinds.Orion.Core.Common.IndicationMonitor;
using SolarWinds.Orion.Core.Common.InformationService;
using SolarWinds.Orion.Core.Common.JobEngine;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Common.Settings;
using SolarWinds.Orion.Core.Common.Swis;
using SolarWinds.Orion.Core.Common.Upgrade;
using SolarWinds.Orion.Core.Discovery;
using SolarWinds.Orion.Core.Discovery.DataAccess;
using SolarWinds.Orion.Core.JobEngine.Routing.ServiceDirectory;
using SolarWinds.Orion.Core.Strings;
using SolarWinds.Orion.ServiceDirectory;
using SolarWinds.Orion.Swis.PubSub.InformationService;
using SolarWinds.ServiceDirectory.Client.Contract;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000020 RID: 32
	[BusinessLayerPlugin]
	public class CoreBusinessLayerPlugin : BusinessLayerPlugin, ISupportFreeEngine, IServiceStateProvider
	{
		// Token: 0x1700005F RID: 95
		// (get) Token: 0x060002D7 RID: 727 RVA: 0x00011B66 File Offset: 0x0000FD66
		public IServiceProvider ServiceContainer
		{
			get
			{
				return CoreBusinessLayerPlugin.serviceContainer;
			}
		}

		// Token: 0x17000060 RID: 96
		// (get) Token: 0x060002D8 RID: 728 RVA: 0x00011B6D File Offset: 0x0000FD6D
		private static bool JobEngineServiceEnabled
		{
			get
			{
				if (CoreBusinessLayerPlugin.jobEngineServiceEnabled == null)
				{
					CoreBusinessLayerPlugin.jobEngineServiceEnabled = new bool?(CoreBusinessLayerPlugin.GetIsJobEngineServiceEnabled());
				}
				return CoreBusinessLayerPlugin.jobEngineServiceEnabled.Value;
			}
		}

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x060002D9 RID: 729 RVA: 0x00011B94 File Offset: 0x0000FD94
		public override string Name
		{
			get
			{
				return "Core Business Layer";
			}
		}

		// Token: 0x17000062 RID: 98
		// (get) Token: 0x060002DA RID: 730 RVA: 0x00011B9C File Offset: 0x0000FD9C
		// (set) Token: 0x060002DB RID: 731 RVA: 0x00011BE0 File Offset: 0x0000FDE0
		public Exception FatalException
		{
			get
			{
				object obj = this.syncRoot;
				Exception result;
				lock (obj)
				{
					result = this.fatalException;
				}
				return result;
			}
			set
			{
				object obj = this.syncRoot;
				lock (obj)
				{
					this.fatalException = value;
				}
			}
		}

		// Token: 0x060002DC RID: 732 RVA: 0x00011C24 File Offset: 0x0000FE24
		public override void Start()
		{
			Log.Configure("SolarWinds.Orion.Core.BusinessLayer.dll.config");
			using (CoreBusinessLayerPlugin.log.Block())
			{
				try
				{
					SWEventLogging.WriteEntry("Starting Core Service [" + Environment.MachineName.ToUpperInvariant() + "]", EventLogEntryType.Information);
					bool flag = RegistrySettings.IsFullOrion();
					bool flag2 = RegistrySettings.IsAdditionalPoller();
					bool flag3 = RegistrySettings.IsFreePoller();
					int num = BusinessLayerSettings.Instance.DBConnectionRetries;
					if (num < 1)
					{
						num = 10;
					}
					int num2 = BusinessLayerSettings.Instance.DBConnectionRetryInterval;
					if (num2 < 10)
					{
						num2 = 30;
					}
					num2 *= 1000;
					int num3 = 0;
					while (!DatabaseFunctions.ValidateDatabaseConnection())
					{
						if (num3 >= num)
						{
							num2 = BusinessLayerSettings.Instance.DBConnectionRetryIntervalOnFail;
							if (num2 < 59)
							{
								num2 = 300;
							}
							num2 *= 1000;
						}
						SWEventLogging.WriteEntry("Core Service [" + Environment.MachineName.ToUpperInvariant() + "] Database connection verification failed", EventLogEntryType.Warning);
						num3++;
						Thread.Sleep(num2);
					}
					MasterEngineInitiator masterEngineInitiator = new MasterEngineInitiator();
					masterEngineInitiator.InitializeEngine();
					this.StartServiceLog();
					this.oneTimeJobManager = new OneTimeJobManager(this);
					this.isDiscoveryJobReschedulingEnabled = ((flag || flag2) && CoreBusinessLayerPlugin.JobEngineServiceEnabled);
					int engineID = masterEngineInitiator.EngineID;
					this.StartEngineServices(masterEngineInitiator);
					this.ScheduleUpdateEngineTable();
					CoreBusinessLayerPlugin.ScheduleMaintenanceRenewals();
					CoreBusinessLayerPlugin.ScheduleCheckOrionProductTeamBlog();
					if (flag)
					{
						CoreBusinessLayerPlugin.ScheduleCheckLicenseSaturation();
						CoreBusinessLayerPlugin.ScheduleMaintananceExpiration();
						CoreBusinessLayerPlugin.ScheduleSaveElementsUsageInfo();
						CoreBusinessLayerPlugin.ScheduleSavePollingCapacityInfo();
						CoreBusinessLayerPlugin.ScheduleCheckPollerLimit();
						CoreBusinessLayerPlugin.ScheduleCheckDatabaseLimit();
						this.ScheduleCheckEvaluationExpiration();
						this.ScheduleCertificateMaintenance();
						this.ScheduleOrionFeatureUpdate();
						CoreBusinessLayerPlugin.ScheduleEnhancedNodeStatusIndications();
						DiscoveryNetObjectStatusManager.Instance.RequestUpdateAsync(null, BusinessLayerSettings.Instance.DiscoveryUpdateNetObjectStatusStartupDelay);
						CoreBusinessLayerPlugin.log.Info("Updating BL Scheduler with Report Jobs");
						List<ReportJobConfiguration> allJobs = ReportJobDAL.GetAllJobs();
						for (int i = 0; i < allJobs.Count; i++)
						{
							ReportJobInitializer.AddActionsToScheduler(allJobs[i], this.businessLayerService);
						}
						CoreBusinessLayerPlugin.log.Info("Preparing partitioned historical tables");
						HistoryTableDdlDAL.EnsureHistoryTables();
						GeolocationJobInitializer.AddActionsToScheduler(this.businessLayerService);
					}
					this.discoveryJobSchedulerCallbackService = new DiscoveryJobSchedulerEventsService(this);
					this.discoveryJobSchedulerCallbackHost = new ServiceHost(this.discoveryJobSchedulerCallbackService, Array.Empty<Uri>());
					this.discoveryJobSchedulerCallbackHost.Open();
					this.orionDiscoveryJobSchedulerCallbackService = new OrionDiscoveryJobSchedulerEventsService(this, this.businessLayerService);
					this.orionDiscoveryJobSchedulerCallbackHost = new ServiceHost(this.orionDiscoveryJobSchedulerCallbackService, Array.Empty<Uri>());
					this.orionDiscoveryJobSchedulerCallbackHost.Open();
					this.oneTimeJobManagerCallbackHost = new ServiceHost(this.oneTimeJobManager, Array.Empty<Uri>());
					string absoluteUri = this.oneTimeJobManagerCallbackHost.Description.Endpoints.First<ServiceEndpoint>().ListenUri.AbsoluteUri;
					this.oneTimeJobManager.SetListenerUri(absoluteUri);
					this.oneTimeJobManagerCallbackHost.Open();
					this.UpdateEngineInfoInDB();
					try
					{
						this.businessLayerService.ForceDiscoveryPluginsToLoadTypes();
					}
					catch (Exception ex)
					{
						CoreBusinessLayerPlugin.log.Error("There was problem while forcing loading discovery plugins.", ex);
					}
					try
					{
						DiscoveryProfileEntry.CheckCrashedJobsAfterStartup();
					}
					catch (Exception ex2)
					{
						CoreBusinessLayerPlugin.log.Error("Failed to check crashed jobs.", ex2);
					}
					if (flag)
					{
						NodeChildStatusParticipationDAL.ResyncAfterStartup();
					}
					this.ScheduleRemoveOldOneTimeJob();
					this.ScheduleBackgroundInventory(engineID);
					this.ScheduleDeleteOldLogs();
					if (flag)
					{
						this.ScheduleLazyUpgradeTask();
						this.ScheduleDBMaintanance();
						this.orionCoreNotificationSubscriber = new OrionCoreNotificationSubscriber(new SqlHelperAdapter());
						this.orionCoreNotificationSubscriber.Start();
						this.auditingNotificationSubscriber = new AuditingNotificationSubscriber();
						this.auditingNotificationSubscriber.Start();
						this.downtimeMonitoringNotificationSubscriber = new DowntimeMonitoringNotificationSubscriber(this.ServiceContainer.GetService<INetObjectDowntimeDAL>());
						this.downtimeMonitoringNotificationSubscriber.Start();
						this.downtimeMonitoringEnableSubscriber = new DowntimeMonitoringEnableSubscriber(this.downtimeMonitoringNotificationSubscriber);
						this.downtimeMonitoringEnableSubscriber.Start();
						this.enhancedNodeStatusCalculationSubscriber = new EnhancedNodeStatusCalculationSubscriber(SubscriptionManager.Instance, new SqlHelperAdapter()).Start();
						this.rollupModeChangedSubscriber = new RollupModeChangedSubscriber(SubscriptionManager.Instance, new SqlHelperAdapter()).Start();
						this.nodeChildStatusParticipationSubscriber = new NodeChildStatusParticipationSubscriber(SubscriptionManager.Instance, new SqlHelperAdapter()).Start();
						if (BusinessLayerSettings.Instance.MaintenanceModeEnabled)
						{
							this.maintenanceIndicationSubscriber = new MaintenanceIndicationSubscriber();
							this.maintenanceIndicationSubscriber.Start();
						}
						OrionReportHelper.InitReportsWatcher();
						try
						{
							OrionReportHelper.SyncLegacyReports();
						}
						catch (Exception ex3)
						{
							CoreBusinessLayerPlugin.log.ErrorFormat("Failed to synchronize reports! Error - {0}", ex3);
						}
						this.ScheduleThresholdsProcessing();
					}
					if (flag || flag2 || flag3)
					{
						Scheduler.Instance.Add(new ScheduledTask("SychronizeSettingsToRegistry", new TimerCallback(this.SynchronizeSettingsToRegistry), null, BusinessLayerSettings.Instance.SettingsToRegistryFrequency));
					}
					Scheduler.Instance.Begin(this.businessLayerService.ShutdownWaitHandle);
					if (SmtpServerDAL.GetDefaultSmtpServer() == null)
					{
						CoreBusinessLayerPlugin.log.ErrorFormat("Default Smtp Server is not defined", Array.Empty<object>());
					}
					CoreBusinessLayerPlugin.log.DebugFormat("{0} started.  Current App Domain: {1}", this.Name, AppDomain.CurrentDomain.FriendlyName);
				}
				catch (Exception ex4)
				{
					SWEventLogging.WriteEntry(string.Format("Unhandled Exception caught in Core Service Engine startup. " + ex4.Message, Array.Empty<object>()), EventLogEntryType.Error, 1024);
					CoreBusinessLayerPlugin.log.Fatal("Unhandled Exception caught in plugin startup.", ex4);
					this.FatalException = ex4;
					throw;
				}
			}
		}

		// Token: 0x060002DD RID: 733 RVA: 0x00012184 File Offset: 0x00010384
		private void StartEngineServices(MasterEngineInitiator masterEngineInitiator)
		{
			int masterEngineId = masterEngineInitiator.EngineID;
			string masterEngineName = masterEngineInitiator.ServerName;
			this.StartMasterEngineService(masterEngineInitiator);
			this.slaveEnginesMonitor = ObservableExtensions.Subscribe(Observable.Do(Observable.SelectMany(Observable.Do(EntityChangeObservableFactory<Engines>.CreateDefaultFactory().CreateObservable((Engines e) => new
			{
				EngineID = e.EngineID,
				ServerName = e.ServerName,
				RemoteAgentGuid = e.EngineProperties.PropertyValue
			}, (Engines e) => e.MasterEngineID == (int?)masterEngineId && e.EngineProperties.PropertyName == "AgentGuid", null), delegate(e)
			{
				this.remoteCollectorAgentStatusProvider.InvalidateCache();
			}), e => e), delegate(e)
			{
				CoreBusinessLayerPlugin.log.Info(string.Format("Slave Engine change detected {0}", e));
			}), delegate(engine)
			{
				if (engine.Entity.EngineID == null)
				{
					return;
				}
				try
				{
					EntityChangeKind kind = engine.Kind;
					if (kind != EntityChangeKind.Create)
					{
						if (kind == EntityChangeKind.Delete)
						{
							this.StopSlaveEngineService(engine.Entity.EngineID.Value);
						}
					}
					else
					{
						this.RegisterSlaveJobEngineRoutes(masterEngineName, engine.Entity.ServerName, engine.Entity.RemoteAgentGuid);
						this.StartSlaveEngineService(engine.Entity.EngineID.Value, engine.Entity.ServerName);
					}
				}
				catch (Exception ex)
				{
					CoreBusinessLayerPlugin.log.Error(string.Format("Slave engine (un)load failed. {0}", engine), ex);
				}
			});
			RemoteCollectorConnectedNotificationSubscriber remoteCollectorConnectedNotificationSubscriber = new RemoteCollectorConnectedNotificationSubscriber(new SwisContextFactory(), new Action<IEngineComponent>(this.RemoteCollectorStatusChangedCallback), masterEngineId);
			SwisSubscriptionDAL swisSubscriptionDAL = new SwisSubscriptionDAL(new SwisConnectionProxyFactory());
			string remoteCollectorConnectedSubscriptionQuery = RemoteCollectorConnectedNotificationSubscriber.RemoteCollectorConnectedSubscriptionQuery;
			this.remoteCollectorConnectedMonitor = new NotificationMonitor(remoteCollectorConnectedNotificationSubscriber, swisSubscriptionDAL, remoteCollectorConnectedSubscriptionQuery);
		}

		// Token: 0x060002DE RID: 734 RVA: 0x00012423 File Offset: 0x00010623
		private void RegisterSlaveJobEngineRoutes(string masterLegacyEngine, string slaveLegacyEngine, string remoteAgentId)
		{
			new RoutingTableRegistrator(ServiceDirectoryClient.Instance).RegisterAsync(masterLegacyEngine, slaveLegacyEngine, remoteAgentId).Wait();
		}

		// Token: 0x060002DF RID: 735 RVA: 0x0001243C File Offset: 0x0001063C
		private void RemoteCollectorStatusChangedCallback(IEngineComponent engineComponent)
		{
			this.remoteCollectorAgentStatusProvider.InvalidateCache();
			if (engineComponent.GetStatus() == EngineComponentStatus.Up)
			{
				this.RunRescheduleEngineDiscoveryJobsTask(engineComponent.EngineId);
			}
		}

		// Token: 0x060002E0 RID: 736 RVA: 0x00012460 File Offset: 0x00010660
		private void StartMasterEngineService(MasterEngineInitiator masterEngineInitiator)
		{
			ConcurrentDictionary<int, CoreBusinessLayerServiceInstance> obj = this.businessLayerServiceInstances;
			lock (obj)
			{
				int engineID = masterEngineInitiator.EngineID;
				this.businessLayerService = new CoreBusinessLayerService(this, this.oneTimeJobManager, engineID);
				this.businessLayerServiceHost = new ServiceHost(this.businessLayerService, Array.Empty<Uri>());
				masterEngineInitiator.InterfacesSupported = this.businessLayerService.AreInterfacesSupported;
				double totalSeconds = BusinessLayerSettings.Instance.RemoteCollectorStatusCacheExpiration.TotalSeconds;
				ISwisConnectionProxyCreator creator = SwisConnectionProxyPool.GetCreator();
				this.remoteCollectorAgentStatusProvider = new RemoteCollectorStatusProvider(creator, engineID, (int)totalSeconds);
				CoreBusinessLayerServiceInstance coreBusinessLayerServiceInstance = new CoreBusinessLayerServiceInstance(engineID, masterEngineInitiator, this.businessLayerService, this.businessLayerServiceHost, this.serviceDirectoryClient);
				if (!this.businessLayerServiceInstances.TryAdd(engineID, coreBusinessLayerServiceInstance))
				{
					CoreBusinessLayerPlugin.log.Warn(string.Format("Unexpected new master Engine detected with id={0}, BusinessLayer service host already exists, new service host creation skipped.", engineID));
				}
				else
				{
					if (!LegacyServicesSettings.Instance.NetPipeWcfEndpointEnabled)
					{
						CoreBusinessLayerPlugin.log.Debug("Net.Pipe endpoint will be removed");
						ServiceEndpoint serviceEndpoint = this.businessLayerServiceHost.Description.Endpoints.FirstOrDefault((ServiceEndpoint n) => n.Binding is NetNamedPipeBinding);
						if (serviceEndpoint != null)
						{
							this.businessLayerServiceHost.Description.Endpoints.Remove(serviceEndpoint);
							CoreBusinessLayerPlugin.log.Debug("Net.Pipe endpoint removed");
						}
					}
					WcfSecurityExtensions.ConfigureCertificateAuthentication(this.businessLayerServiceHost.Credentials);
					this.businessLayerServiceHost.Open();
					if (this.isDiscoveryJobReschedulingEnabled)
					{
						coreBusinessLayerServiceInstance.InitRescheduleEngineDiscoveryJobsTask(true);
					}
				}
			}
		}

		// Token: 0x060002E1 RID: 737 RVA: 0x00012604 File Offset: 0x00010804
		private void StartSlaveEngineService(int engineId, string engineName)
		{
			ConcurrentDictionary<int, CoreBusinessLayerServiceInstance> obj = this.businessLayerServiceInstances;
			lock (obj)
			{
				RemoteCollectorEngineComponent engineComponent = new RemoteCollectorEngineComponent(engineId, this.remoteCollectorAgentStatusProvider);
				RemoteCollectorEngineInitiator engineInitiator = new RemoteCollectorEngineInitiator(engineId, engineName, this.businessLayerService.AreInterfacesSupported, this.engineDal, new ThrottlingStatusProvider(), engineComponent);
				CoreBusinessLayerServiceInstance coreBusinessLayerServiceInstance = new CoreBusinessLayerServiceInstance(engineId, engineInitiator, this.businessLayerService, this.businessLayerServiceHost, this.serviceDirectoryClient);
				if (!this.businessLayerServiceInstances.TryAdd(engineId, coreBusinessLayerServiceInstance))
				{
					CoreBusinessLayerPlugin.log.Warn(string.Format("Unexpected new slave Engine detected with id={0}, BusinessLayer service host already exists, new service host creation skipped.", engineId));
				}
				else
				{
					coreBusinessLayerServiceInstance.RegisterAsync().Wait();
					coreBusinessLayerServiceInstance.InitializeEngine();
					if (this.isDiscoveryJobReschedulingEnabled)
					{
						coreBusinessLayerServiceInstance.InitRescheduleEngineDiscoveryJobsTask(false);
					}
				}
			}
		}

		// Token: 0x060002E2 RID: 738 RVA: 0x000126D4 File Offset: 0x000108D4
		private void StopSlaveEngineService(int engineId)
		{
			ConcurrentDictionary<int, CoreBusinessLayerServiceInstance> obj = this.businessLayerServiceInstances;
			lock (obj)
			{
				CoreBusinessLayerServiceInstance coreBusinessLayerServiceInstance;
				if (!this.businessLayerServiceInstances.TryRemove(engineId, out coreBusinessLayerServiceInstance))
				{
					CoreBusinessLayerPlugin.log.Warn(string.Format("Unexpected disappeared slave Engine detected with id={0}.", engineId));
				}
				else
				{
					coreBusinessLayerServiceInstance.StopRescheduleEngineDiscoveryJobsTask();
					coreBusinessLayerServiceInstance.UnregisterAsync().Wait();
				}
			}
		}

		// Token: 0x060002E3 RID: 739 RVA: 0x0001274C File Offset: 0x0001094C
		internal CoreBusinessLayerServiceInstance GetServiceInstance(int engineId)
		{
			CoreBusinessLayerServiceInstance result;
			if (this.businessLayerServiceInstances.TryGetValue(engineId, out result))
			{
				return result;
			}
			throw new InvalidOperationException(string.Format("The requested engine with EngineId={0} was not found.", engineId));
		}

		// Token: 0x060002E4 RID: 740 RVA: 0x00012780 File Offset: 0x00010980
		internal void AddServiceInstance(CoreBusinessLayerServiceInstance serviceInstance)
		{
			if (!this.businessLayerServiceInstances.TryAdd(serviceInstance.EngineId, serviceInstance))
			{
				CoreBusinessLayerPlugin.log.Warn(string.Format("Unexpected new slave Engine detected with id={0}, BusinessLayer service host already exists, new service host creation skipped.", serviceInstance.EngineId));
			}
		}

		// Token: 0x060002E5 RID: 741 RVA: 0x000127B8 File Offset: 0x000109B8
		internal void RunRescheduleEngineDiscoveryJobsTask(int engineId)
		{
			CoreBusinessLayerServiceInstance coreBusinessLayerServiceInstance;
			if (!this.businessLayerServiceInstances.TryGetValue(engineId, out coreBusinessLayerServiceInstance))
			{
				CoreBusinessLayerPlugin.log.Warn(string.Format("Unexpected disappeared slave Engine detected with id={0}.", engineId));
				return;
			}
			coreBusinessLayerServiceInstance.RunRescheduleEngineDiscoveryJobsTask();
		}

		// Token: 0x060002E6 RID: 742 RVA: 0x000127F6 File Offset: 0x000109F6
		private static bool GetIsJobEngineServiceEnabled()
		{
			return ServicesConfigurationExtensions.IsServiceEnabled(new ServicesConfiguration(), "JobEngineV2");
		}

		// Token: 0x060002E7 RID: 743 RVA: 0x00012807 File Offset: 0x00010A07
		private void ScheduleUpdateEngineTable()
		{
			Scheduler.Instance.Add(new ScheduledTask("UpdateEngineTable", delegate(object o)
			{
				this.UpdateEngineInfoTask();
			}, null, BusinessLayerSettings.Instance.UpdateEngineTimer));
		}

		// Token: 0x060002E8 RID: 744 RVA: 0x00012834 File Offset: 0x00010A34
		private void UpdateEngineInfoTask()
		{
			if (CoreBusinessLayerPlugin.log.IsTraceEnabled)
			{
				CoreBusinessLayerPlugin.log.Trace("Starting scheduled task UpdateEngineTable.");
			}
			foreach (KeyValuePair<int, CoreBusinessLayerServiceInstance> keyValuePair in this.businessLayerServiceInstances)
			{
				keyValuePair.Value.UpdateEngine(CoreBusinessLayerPlugin.JobEngineServiceEnabled);
			}
			if (CoreBusinessLayerPlugin.log.IsTraceEnabled)
			{
				CoreBusinessLayerPlugin.log.Trace("UpdateEngineTable task has finished.");
			}
		}

		// Token: 0x060002E9 RID: 745 RVA: 0x000128C4 File Offset: 0x00010AC4
		private void ScheduleCheckEvaluationExpiration()
		{
			Scheduler.Instance.Add(new ScheduledTask("CheckEvaluationExpiration", new TimerCallback(this.CheckEvaluationExpiration), null, TimeSpan.FromHours((double)BusinessLayerSettings.Instance.EvaluationExpirationCheckIntervalHours)), true);
		}

		// Token: 0x060002EA RID: 746 RVA: 0x000128F8 File Offset: 0x00010AF8
		private static void ScheduleCheckDatabaseLimit()
		{
			Scheduler.Instance.Add(new ScheduledTask("CheckDatabaseLimit", new TimerCallback(CoreBusinessLayerPlugin.CheckDatabaseLimit), "CheckDatabaseLimit", BusinessLayerSettings.Instance.CheckDatabaseLimitTimer));
		}

		// Token: 0x060002EB RID: 747 RVA: 0x00012929 File Offset: 0x00010B29
		private static void ScheduleCheckPollerLimit()
		{
			Scheduler.Instance.Add(new ScheduledTask("CheckPollerLimit", new TimerCallback(CoreBusinessLayerPlugin.CheckPollerLimit), null, BusinessLayerSettings.Instance.PollerLimitTimer));
		}

		// Token: 0x060002EC RID: 748 RVA: 0x00012958 File Offset: 0x00010B58
		private static void ScheduleSavePollingCapacityInfo()
		{
			Scheduler.Instance.Add(new ScheduledTaskInExactTime("SavePollingCapacityInfo", new TimerCallback(CoreBusinessLayerPlugin.SavePollingCapacityInfo), null, DateTime.Today.AddMinutes(2.0), true));
		}

		// Token: 0x060002ED RID: 749 RVA: 0x000129A0 File Offset: 0x00010BA0
		private static void ScheduleSaveElementsUsageInfo()
		{
			Scheduler.Instance.Add(new ScheduledTaskInExactTime("SaveElementsUsageInfo", new TimerCallback(CoreBusinessLayerPlugin.SaveElementsUsageInfo), null, DateTime.Today.AddMinutes(1.0), true));
		}

		// Token: 0x060002EE RID: 750 RVA: 0x000129E8 File Offset: 0x00010BE8
		private static void ScheduleMaintananceExpiration()
		{
			Scheduler.Instance.Add(new ScheduledTaskInExactTime("CheckMaintenanceExpiration", new TimerCallback(CoreBusinessLayerPlugin.CheckMaintenanceExpiration), null, DateTime.Today.AddSeconds(1.0), true));
		}

		// Token: 0x060002EF RID: 751 RVA: 0x00012A2D File Offset: 0x00010C2D
		private static void ScheduleCheckLicenseSaturation()
		{
			Scheduler.Instance.Add(new ScheduledTask("CheckLicenseSaturation", new TimerCallback(CoreBusinessLayerPlugin.CheckLicenseSaturation), null, TimeSpan.FromMinutes((double)BusinessLayerSettings.Instance.LicenseSaturationCheckInterval)));
		}

		// Token: 0x060002F0 RID: 752 RVA: 0x00012A60 File Offset: 0x00010C60
		private static void ScheduleCheckOrionProductTeamBlog()
		{
			if (!Settings.IsProductsBlogDisabled)
			{
				Scheduler.Instance.Add(new ScheduledTask("CheckOrionProductTeamBlog", new TimerCallback(CoreBusinessLayerPlugin.CheckOrionProductTeamBlog), null, Settings.CheckOrionProductTeamBlogTimer));
			}
		}

		// Token: 0x060002F1 RID: 753 RVA: 0x00012A8F File Offset: 0x00010C8F
		private static void ScheduleMaintenanceRenewals()
		{
			if (!Settings.IsMaintenanceRenewalsDisabled)
			{
				Scheduler.Instance.Add(new ScheduledTask("CheckMaintenanceRenewals", new TimerCallback(CoreBusinessLayerPlugin.CheckMaintenanceRenewals), null, Settings.CheckMaintenanceRenewalsTimer));
			}
		}

		// Token: 0x060002F2 RID: 754 RVA: 0x00012ABE File Offset: 0x00010CBE
		private void ScheduleSynchronizeSettingsToRegistry()
		{
			Scheduler.Instance.Add(new ScheduledTask("SychronizeSettingsToRegistry", new TimerCallback(this.SynchronizeSettingsToRegistry), null, BusinessLayerSettings.Instance.SettingsToRegistryFrequency));
		}

		// Token: 0x060002F3 RID: 755 RVA: 0x00012AEC File Offset: 0x00010CEC
		private void ScheduleThresholdsProcessing()
		{
			if (BusinessLayerSettings.Instance.ThresholdsProcessingEnabled)
			{
				Scheduler.Instance.Add(new ScheduledTask("ThresholdsProcessing", delegate(object o)
				{
					ThresholdProcessingManager.Instance.Engine.UpdateThresholds();
				}, null, BusinessLayerSettings.Instance.ThresholdsProcessingDefaultTimer), true);
				CoreBusinessLayerPlugin.log.Info("Threshold processing is enabled.");
				return;
			}
			CoreBusinessLayerPlugin.log.Info("Threshold processing is disabled.");
		}

		// Token: 0x060002F4 RID: 756 RVA: 0x00012B63 File Offset: 0x00010D63
		private void ScheduleDBMaintanance()
		{
			this.subscribtionProvider = InformationServiceSubscriptionProviderShared.Instance();
			Scheduler.Instance.Add(ScheduledTaskFactory.CreateDatabaseMaintenanceTask(this.subscribtionProvider));
		}

		// Token: 0x060002F5 RID: 757 RVA: 0x00012B88 File Offset: 0x00010D88
		private void ScheduleLazyUpgradeTask()
		{
			Scheduler.Instance.Add(new ScheduledTask("LazyUpgradeTask", delegate(object o)
			{
				LazyUpgradeTask.Instance.TryRunLazyUpgrade();
			}, null, TimeSpan.FromMinutes(5.0)));
		}

		// Token: 0x060002F6 RID: 758 RVA: 0x00012BD8 File Offset: 0x00010DD8
		private void ScheduleDeleteOldLogs()
		{
			ScheduledTask scheduledTask = new ScheduledTask("DeleteOldLogs", new TimerCallback(LogHelper.DeleteOldLogs), (from x in ModulesCollector.GetInstalledModules()
			where x.RemoveOldJobResult != null
			select x.RemoveOldJobResult).ToArray<RemoveOldOnetimeJobResultsInfo>(), BusinessLayerSettings.Instance.CheckForOldLogsTimer);
			Scheduler.Instance.Add(scheduledTask);
		}

		// Token: 0x060002F7 RID: 759 RVA: 0x00012C64 File Offset: 0x00010E64
		private void ScheduleBackgroundInventory(int engineId)
		{
			Scheduler.Instance.Add(new ScheduledTask("BackgroundInventoryCheck", new TimerCallback(this.RunBackgroundInventoryCheck), engineId, BusinessLayerSettings.Instance.BackgroundInventoryCheckTimer));
			this.backgroundInventoryPluggable = new InventoryManager(engineId);
			this.backgroundInventoryPluggable.Start(false);
		}

		// Token: 0x060002F8 RID: 760 RVA: 0x00012CBC File Offset: 0x00010EBC
		private void ScheduleRemoveOldOneTimeJob()
		{
			Scheduler.Instance.Add(new ScheduledTask("RemoveOldOnetimeJobResults", delegate(object o)
			{
				CoreBusinessLayerPlugin.log.DebugFormat("Clearing onetime job resuts older than {0}", DiscoverySettings.OneTimeJobResultMaximalAge);
				DiscoveryResultCache.Instance.CrearOldResults(DiscoverySettings.OneTimeJobResultMaximalAge);
			}, null, DiscoverySettings.OneTimeJobResultClearInetrval)
			{
				RethrowExceptions = false
			});
		}

		// Token: 0x060002F9 RID: 761 RVA: 0x00012D0C File Offset: 0x00010F0C
		private void ScheduleCertificateMaintenance()
		{
			this.businessLayerService.StartCertificateMaintenance();
			Scheduler.Instance.Add(new ScheduledTask("CertificateMaintenance", delegate(object o)
			{
				this.CheckCertificateMaintenanceStatus();
			}, null, BusinessLayerSettings.Instance.CertificateMaintenanceTaskCheckInterval, BusinessLayerSettings.Instance.CertificateMaintenanceTaskCheckInterval, TimeSpan.Zero));
		}

		// Token: 0x060002FA RID: 762 RVA: 0x00012D60 File Offset: 0x00010F60
		private void ScheduleOrionFeatureUpdate()
		{
			if (this.orionFeatureResolver == null)
			{
				this.orionFeatureResolver = new OrionFeatureResolver(new OrionFeaturesDAL(), OrionFeatureProviderFactory.CreateInstance());
				CoreBusinessLayerPlugin.serviceContainer.AddService<OrionFeatureResolver>(this.orionFeatureResolver);
			}
			this.RefreshOrionFeatures();
			Scheduler.Instance.Add(new ScheduledTask("RefreshOrionFeatures", delegate(object o)
			{
				this.RefreshOrionFeatures();
			}, null, BusinessLayerSettings.Instance.OrionFeatureRefreshTimer, BusinessLayerSettings.Instance.OrionFeatureRefreshTimer, TimeSpan.Zero), true);
		}

		// Token: 0x060002FB RID: 763 RVA: 0x00012DDC File Offset: 0x00010FDC
		private static void ScheduleEnhancedNodeStatusIndications()
		{
			Scheduler.Instance.Add(new ScheduledTask("EnhancedStatusIndication", delegate(object o)
			{
				CoreBusinessLayerPlugin.EnhancedStatusIndication();
			}, null, TimeSpan.FromSeconds(30.0)));
		}

		// Token: 0x060002FC RID: 764 RVA: 0x00012E2B File Offset: 0x0001102B
		private static void EnhancedStatusIndication()
		{
			new EnhancedNodeStatusIndicator(new SqlHelperAdapter(), IndicationPublisher.CreateV3()).Execute();
		}

		// Token: 0x060002FD RID: 765 RVA: 0x00012E41 File Offset: 0x00011041
		public void StartServiceLog()
		{
			string text = string.Format(Resources.LIBCODE_VB0_3, Environment.MachineName.ToUpperInvariant());
			SWEventLogging.WriteEntry(text, EventLogEntryType.Information);
			BusinessLayerOrionEvent.WriteEvent(text, CoreEventTypes.ServiceStartedEventType);
		}

		// Token: 0x060002FE RID: 766 RVA: 0x00012E68 File Offset: 0x00011068
		public void StopServiceLog()
		{
			string text = string.Format(Resources.LIBCODE_VB0_4, Environment.MachineName.ToUpperInvariant());
			SWEventLogging.WriteEntry(text, EventLogEntryType.Information);
			BusinessLayerOrionEvent.WriteEvent(text, CoreEventTypes.ServiceStoppedEventType);
		}

		// Token: 0x060002FF RID: 767 RVA: 0x00012E90 File Offset: 0x00011090
		private void UpdateEngineInfoInDB()
		{
			string serverName = Environment.MachineName.ToUpperInvariant();
			foreach (ServiceEndpoint serviceEndpoint in this.businessLayerServiceHost.Description.Endpoints)
			{
				if (serviceEndpoint.Binding.Name.Equals("netTcpBinding", StringComparison.InvariantCultureIgnoreCase))
				{
					CoreBusinessLayerPlugin.UpdateEnginePortInDB(serverName, serviceEndpoint.Address.Uri.Port);
					break;
				}
			}
		}

		// Token: 0x06000300 RID: 768 RVA: 0x00012F1C File Offset: 0x0001111C
		private static void UpdateEnginePortInDB(string serverName, int port)
		{
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("Update Engines set BusinessLayerPort = @BusinessLayerPort where ServerName = @ServerName"))
			{
				textCommand.Parameters.Add("@ServerName", SqlDbType.NVarChar).Value = serverName;
				textCommand.Parameters.Add("@BusinessLayerPort", SqlDbType.Int).Value = port;
				SqlHelper.ExecuteNonQuery(textCommand);
			}
		}

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x06000301 RID: 769 RVA: 0x00012F8C File Offset: 0x0001118C
		public bool IsServiceDown
		{
			get
			{
				object obj = this.syncRoot;
				bool result;
				lock (obj)
				{
					result = (this.fatalException != null);
				}
				return result;
			}
		}

		// Token: 0x06000302 RID: 770 RVA: 0x00012FD4 File Offset: 0x000111D4
		private static void CheckMaintenanceRenewals(object state)
		{
			if (SettingsDAL.Get("MaintenanceRenewals-Check").Equals("1"))
			{
				CoreHelper.CheckMaintenanceRenewals(true);
			}
		}

		// Token: 0x06000303 RID: 771 RVA: 0x00012FF2 File Offset: 0x000111F2
		private static void CheckOrionProductTeamBlog(object state)
		{
			if (SettingsDAL.Get("ProductsBlog-EnableContent").Equals("1"))
			{
				CoreHelper.CheckOrionProductTeamBlog();
			}
		}

		// Token: 0x06000304 RID: 772 RVA: 0x00013010 File Offset: 0x00011210
		private static void CheckLicenseSaturation(object state)
		{
			if (!Settings.IsLicenseSaturationDisabled)
			{
				using (LocaleThreadState.EnsurePrimaryLocale())
				{
					LicenseSaturationHelper.CheckLicenseSaturation();
				}
			}
		}

		// Token: 0x06000305 RID: 773 RVA: 0x0001304C File Offset: 0x0001124C
		private static void SaveElementsUsageInfo(object state)
		{
			using (LocaleThreadState.EnsurePrimaryLocale())
			{
				LicenseSaturationHelper.SaveElementsUsageInfo();
			}
		}

		// Token: 0x06000306 RID: 774 RVA: 0x00013080 File Offset: 0x00011280
		private static void CheckMaintenanceExpiration(object state)
		{
			if (SettingsDAL.Get("MaintenanceExpiration-Check").Equals("1"))
			{
				using (LocaleThreadState.EnsurePrimaryLocale())
				{
					MaintenanceExpirationHelper.CheckMaintenanceExpiration();
				}
			}
		}

		// Token: 0x06000307 RID: 775 RVA: 0x000130CC File Offset: 0x000112CC
		private static void CheckPollerLimit(object state)
		{
			if (SettingsDAL.Get("PollerLimit-Check").Equals("1"))
			{
				using (LocaleThreadState.EnsurePrimaryLocale())
				{
					PollerLimitHelper.CheckPollerLimit();
				}
			}
		}

		// Token: 0x06000308 RID: 776 RVA: 0x00013118 File Offset: 0x00011318
		private static void SavePollingCapacityInfo(object state)
		{
			using (LocaleThreadState.EnsurePrimaryLocale())
			{
				PollerLimitHelper.SavePollingCapacityInfo();
			}
		}

		// Token: 0x06000309 RID: 777 RVA: 0x0001314C File Offset: 0x0001134C
		private static void CheckDatabaseLimit(object state)
		{
			using (LocaleThreadState.EnsurePrimaryLocale())
			{
				if (!new DatabaseLimitNotificationItemDAL().CheckNotificationState())
				{
					CoreBusinessLayerPlugin.log.Debug("Removing database limit check");
					Scheduler.Instance.Remove((string)state);
				}
			}
		}

		// Token: 0x0600030A RID: 778 RVA: 0x000131A8 File Offset: 0x000113A8
		private void CheckEvaluationExpiration(object state)
		{
			if (SettingsDAL.Get("EvaluationExpiration-Check").Equals("1"))
			{
				using (LocaleThreadState.EnsurePrimaryLocale())
				{
					new EvaluationExpirationNotificationItemDAL().CheckEvaluationExpiration();
				}
			}
		}

		// Token: 0x0600030B RID: 779 RVA: 0x000131F8 File Offset: 0x000113F8
		public override void Stop()
		{
			using (CoreBusinessLayerPlugin.log.Block())
			{
				this.StopServiceLog();
				CoreBusinessLayerPlugin.log.Debug("Stoping Core Business Layer Plugin");
				Scheduler.Instance.End();
				IDisposable disposable2 = this.slaveEnginesMonitor;
				if (disposable2 != null)
				{
					disposable2.Dispose();
				}
				IDisposable disposable3 = this.remoteCollectorConnectedMonitor;
				if (disposable3 != null)
				{
					disposable3.Dispose();
				}
				if (this.backgroundInventory != null && this.backgroundInventory.IsRunning)
				{
					this.backgroundInventory.Cancel();
				}
				if (this.subscribtionProvider != null)
				{
					this.subscribtionProvider.UnsubscribeAll();
				}
				if (this.orionCoreNotificationSubscriber != null)
				{
					this.orionCoreNotificationSubscriber.Stop();
				}
				if (this.auditingNotificationSubscriber != null)
				{
					this.auditingNotificationSubscriber.Stop();
				}
				if (this.downtimeMonitoringNotificationSubscriber != null)
				{
					this.downtimeMonitoringNotificationSubscriber.Stop();
				}
				if (this.downtimeMonitoringEnableSubscriber != null)
				{
					this.downtimeMonitoringEnableSubscriber.Stop(true);
				}
				if (this.enhancedNodeStatusCalculationSubscriber != null)
				{
					this.enhancedNodeStatusCalculationSubscriber.Dispose();
					this.enhancedNodeStatusCalculationSubscriber = null;
				}
				if (this.rollupModeChangedSubscriber != null)
				{
					this.rollupModeChangedSubscriber.Dispose();
					this.rollupModeChangedSubscriber = null;
				}
				if (this.nodeChildStatusParticipationSubscriber != null)
				{
					this.nodeChildStatusParticipationSubscriber.Dispose();
					this.nodeChildStatusParticipationSubscriber = null;
				}
				if (this.maintenanceIndicationSubscriber != null)
				{
					this.maintenanceIndicationSubscriber.Dispose();
				}
				if (this.backgroundInventoryPluggable != null)
				{
					this.backgroundInventoryPluggable.Stop();
				}
				foreach (KeyValuePair<int, CoreBusinessLayerServiceInstance> keyValuePair in this.businessLayerServiceInstances)
				{
					keyValuePair.Value.StopRescheduleEngineDiscoveryJobsTask();
				}
				this.businessLayerService.Shutdown();
				MessageUtilities.ShutdownCommunicationObject(this.businessLayerServiceHost);
				MessageUtilities.ShutdownCommunicationObject(this.discoveryJobSchedulerCallbackHost);
				MessageUtilities.ShutdownCommunicationObject(this.orionDiscoveryJobSchedulerCallbackHost);
				MessageUtilities.ShutdownCommunicationObject(this.oneTimeJobManagerCallbackHost);
				CoreBusinessLayerPlugin.log.DebugFormat("{0} stopped.  Current App Domain: {1}", this.Name, AppDomain.CurrentDomain.FriendlyName);
			}
		}

		// Token: 0x0600030C RID: 780 RVA: 0x00013410 File Offset: 0x00011610
		public CoreBusinessLayerPlugin() : this(ServiceDirectoryClient.Instance)
		{
		}

		// Token: 0x0600030D RID: 781 RVA: 0x00013420 File Offset: 0x00011620
		internal CoreBusinessLayerPlugin(IServiceDirectoryClient serviceDirectoryClient)
		{
			AppDomain.CurrentDomain.UnhandledException += CoreBusinessLayerPlugin.AppDomain_UnhandledException;
			if (serviceDirectoryClient == null)
			{
				throw new ArgumentNullException("serviceDirectoryClient");
			}
			this.serviceDirectoryClient = serviceDirectoryClient;
			try
			{
				SWEventLogging.EventSource = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileDescription;
			}
			catch
			{
			}
		}

		// Token: 0x0600030E RID: 782 RVA: 0x000134BC File Offset: 0x000116BC
		private static void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Exception ex = e.ExceptionObject as Exception;
			if (ex != null)
			{
				CoreBusinessLayerPlugin.log.Error("Unhandled exception.", ex);
				SWEventLogging.WriteEntry(string.Format("Unhandled exception: {0}", ex.Message), EventLogEntryType.Error);
				return;
			}
			string text = string.Format("Non-exception object of type {0} was thrown and not caught: {1}", e.ExceptionObject.GetType(), e.ExceptionObject);
			CoreBusinessLayerPlugin.log.Error(text);
			SWEventLogging.WriteEntry(text, EventLogEntryType.Error);
		}

		// Token: 0x0600030F RID: 783 RVA: 0x00013530 File Offset: 0x00011730
		private void RunBackgroundInventoryCheck(object state)
		{
			int num = (int)state;
			if (!CoreHelper.IsEngineVersionSameAsOnMain(num))
			{
				CoreBusinessLayerPlugin.log.Warn(string.Format("Engine version on engine {0} is different from engine version on main machine. ", num) + "Background inventory check not run.");
				return;
			}
			int backgroundInventoryRetriesCount = BusinessLayerSettings.Instance.BackgroundInventoryRetriesCount;
			if (CoreBusinessLayerPlugin.log.IsDebugEnabled)
			{
				CoreBusinessLayerPlugin.log.DebugFormat("Running scheduled background inventory check on engine {0}", num);
			}
			if (this.backgroundInventory == null)
			{
				this.backgroundInventory = new BackgroundInventoryManager(BusinessLayerSettings.Instance.BackgroundInventoryParallelTasksCount);
			}
			if (this.backgroundInventory.IsRunning)
			{
				CoreBusinessLayerPlugin.log.Info("Skipping background inventory check, still running");
				return;
			}
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("\nSELECT n.NodeID, s.SettingValue FROM Nodes n \n    JOIN NodeSettings s ON n.NodeID = s.NodeID AND s.SettingName = 'Core.NeedsInventory'\nWHERE (n.EngineID = @engineID OR n.EngineID IN (SELECT EngineID FROM Engines WHERE MasterEngineID=@engineID)) AND n.PolledStatus = 1\nORDER BY n.StatCollection ASC"))
			{
				textCommand.Parameters.Add("@engineID", SqlDbType.Int).Value = num;
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						int @int = dataReader.GetInt32(0);
						string @string = dataReader.GetString(1);
						if (!this.backgroundInventoryTracker.ContainsKey(@int))
						{
							this.backgroundInventoryTracker.Add(@int, 0);
						}
						int num2 = this.backgroundInventoryTracker[@int];
						if (num2 < backgroundInventoryRetriesCount)
						{
							this.backgroundInventoryTracker[@int] = num2 + 1;
							this.backgroundInventory.Enqueue(@int, @string);
						}
						else if (num2 == backgroundInventoryRetriesCount)
						{
							CoreBusinessLayerPlugin.log.WarnFormat("Max inventory retries count for Node {0} reached. Skipping inventoring until next restart of BusinessLayer service.", @int);
							this.backgroundInventoryTracker[@int] = num2 + 1;
						}
					}
				}
			}
			if (this.backgroundInventory.QueueSize > 0)
			{
				this.backgroundInventory.Start();
			}
		}

		// Token: 0x06000310 RID: 784 RVA: 0x000136F4 File Offset: 0x000118F4
		private void SynchronizeSettingsToRegistry(object state)
		{
			new SettingsToRegistry().Synchronize();
		}

		// Token: 0x06000311 RID: 785 RVA: 0x00013700 File Offset: 0x00011900
		private void CheckCertificateMaintenanceStatus()
		{
			CertificateMaintenanceResult certificateMaintenanceStatus = this.businessLayerService.GetCertificateMaintenanceStatus();
			CoreBusinessLayerPlugin.log.InfoFormat("CheckCertificateMaintenanceStatus reports maintenance status: {0}", certificateMaintenanceStatus);
			switch (certificateMaintenanceStatus)
			{
			case 0:
			case 1:
				CoreBusinessLayerPlugin.log.Info("Removing certificate maintenance task.");
				Scheduler.Instance.Remove("CertificateMaintenance");
				return;
			case 2:
				CoreBusinessLayerPlugin.log.Info("Certificate maintenance task needs user confirmation because it can't be finished without breaking changes.");
				this.businessLayerService.RequestUserApprovalForCertificateMaintenance();
				return;
			case 3:
				break;
			case 4:
			case 5:
				CoreBusinessLayerPlugin.log.Info("Certificate maintenance needs retry. Starting new attempt.");
				this.businessLayerService.RetryCertificateMaintenance();
				break;
			default:
				return;
			}
		}

		// Token: 0x06000312 RID: 786 RVA: 0x000137A4 File Offset: 0x000119A4
		private void RefreshOrionFeatures()
		{
			if (this.orionFeatureResolver == null)
			{
				throw new InvalidOperationException("orionFearureResolves was not initialized");
			}
			try
			{
				this.orionFeatureResolver.Resolve();
			}
			catch (Exception ex)
			{
				CoreBusinessLayerPlugin.log.Error(ex);
			}
		}

		// Token: 0x04000081 RID: 129
		private static readonly Log log = new Log();

		// Token: 0x04000082 RID: 130
		private const string CertificateMaintenanceTaskName = "CertificateMaintenance";

		// Token: 0x04000083 RID: 131
		private readonly IEngineDAL engineDal = new EngineDAL();

		// Token: 0x04000084 RID: 132
		private BackgroundInventoryManager backgroundInventory;

		// Token: 0x04000085 RID: 133
		private readonly Dictionary<int, int> backgroundInventoryTracker = new Dictionary<int, int>();

		// Token: 0x04000086 RID: 134
		private Exception fatalException;

		// Token: 0x04000087 RID: 135
		private CoreBusinessLayerService businessLayerService;

		// Token: 0x04000088 RID: 136
		private ServiceHost businessLayerServiceHost;

		// Token: 0x04000089 RID: 137
		private IDisposable slaveEnginesMonitor;

		// Token: 0x0400008A RID: 138
		private IDisposable remoteCollectorConnectedMonitor;

		// Token: 0x0400008B RID: 139
		private IRemoteCollectorAgentStatusProvider remoteCollectorAgentStatusProvider;

		// Token: 0x0400008C RID: 140
		private readonly ConcurrentDictionary<int, CoreBusinessLayerServiceInstance> businessLayerServiceInstances = new ConcurrentDictionary<int, CoreBusinessLayerServiceInstance>();

		// Token: 0x0400008D RID: 141
		private readonly object syncRoot = new object();

		// Token: 0x0400008E RID: 142
		private DiscoveryJobSchedulerEventsService discoveryJobSchedulerCallbackService;

		// Token: 0x0400008F RID: 143
		private ServiceHost discoveryJobSchedulerCallbackHost;

		// Token: 0x04000090 RID: 144
		private OrionDiscoveryJobSchedulerEventsService orionDiscoveryJobSchedulerCallbackService;

		// Token: 0x04000091 RID: 145
		private ServiceHost orionDiscoveryJobSchedulerCallbackHost;

		// Token: 0x04000092 RID: 146
		private readonly IServiceDirectoryClient serviceDirectoryClient;

		// Token: 0x04000093 RID: 147
		private IOneTimeJobManager oneTimeJobManager;

		// Token: 0x04000094 RID: 148
		private ServiceHost oneTimeJobManagerCallbackHost;

		// Token: 0x04000095 RID: 149
		private InformationServiceSubscriptionProviderBase subscribtionProvider;

		// Token: 0x04000096 RID: 150
		internal OrionCoreNotificationSubscriber orionCoreNotificationSubscriber;

		// Token: 0x04000097 RID: 151
		internal AuditingNotificationSubscriber auditingNotificationSubscriber;

		// Token: 0x04000098 RID: 152
		internal DowntimeMonitoringNotificationSubscriber downtimeMonitoringNotificationSubscriber;

		// Token: 0x04000099 RID: 153
		internal DowntimeMonitoringEnableSubscriber downtimeMonitoringEnableSubscriber;

		// Token: 0x0400009A RID: 154
		internal MaintenanceIndicationSubscriber maintenanceIndicationSubscriber;

		// Token: 0x0400009B RID: 155
		internal EnhancedNodeStatusCalculationSubscriber enhancedNodeStatusCalculationSubscriber;

		// Token: 0x0400009C RID: 156
		internal RollupModeChangedSubscriber rollupModeChangedSubscriber;

		// Token: 0x0400009D RID: 157
		internal NodeChildStatusParticipationSubscriber nodeChildStatusParticipationSubscriber;

		// Token: 0x0400009E RID: 158
		private OrionFeatureResolver orionFeatureResolver;

		// Token: 0x0400009F RID: 159
		private const string ConfigurationSectionName = "orion.serviceLocator";

		// Token: 0x040000A0 RID: 160
		private static readonly ServiceContainer serviceContainer = new ServiceContainer("orion.serviceLocator");

		// Token: 0x040000A1 RID: 161
		private static bool? jobEngineServiceEnabled;

		// Token: 0x040000A2 RID: 162
		private bool isDiscoveryJobReschedulingEnabled;

		// Token: 0x040000A3 RID: 163
		private InventoryManager backgroundInventoryPluggable;
	}
}
