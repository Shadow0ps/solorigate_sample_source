using System;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000010 RID: 16
	public interface IBusinessLayerSettings
	{
		// Token: 0x17000024 RID: 36
		// (get) Token: 0x06000253 RID: 595
		int DBConnectionRetryInterval { get; }

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000254 RID: 596
		int DBConnectionRetryIntervalOnFail { get; }

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x06000255 RID: 597
		int DBConnectionRetries { get; }

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x06000256 RID: 598
		string JobSchedulerEndpointNetPipe { get; }

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x06000257 RID: 599
		string JobSchedulerEndpointTcpPipe { get; }

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x06000258 RID: 600
		bool ProxyAvailable { get; }

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x06000259 RID: 601
		string UserName { get; }

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x0600025A RID: 602
		string Password { get; }

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x0600025B RID: 603
		string ProxyAddress { get; }

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x0600025C RID: 604
		int ProxyPort { get; }

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x0600025D RID: 605
		string OrionProductTeamBlogUrl { get; }

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x0600025E RID: 606
		int LicenseSaturationCheckInterval { get; }

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x0600025F RID: 607
		int MaintenanceExpirationWarningDays { get; }

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x06000260 RID: 608
		int MaintenanceExpiredShowAgainAtDays { get; }

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x06000261 RID: 609
		TimeSpan PollerLimitTimer { get; }

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x06000262 RID: 610
		TimeSpan CheckDatabaseLimitTimer { get; }

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x06000263 RID: 611
		TimeSpan CheckForOldLogsTimer { get; }

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x06000264 RID: 612
		TimeSpan UpdateEngineTimer { get; }

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x06000265 RID: 613
		TimeSpan RemoteCollectorStatusCacheExpiration { get; }

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x06000266 RID: 614
		bool MaintenanceModeEnabled { get; }

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x06000267 RID: 615
		TimeSpan SettingsToRegistryFrequency { get; }

		// Token: 0x17000039 RID: 57
		// (get) Token: 0x06000268 RID: 616
		TimeSpan DiscoveryUpdateNetObjectStatusWaitForChangesDelay { get; }

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x06000269 RID: 617
		TimeSpan DiscoveryUpdateNetObjectStatusStartupDelay { get; }

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x0600026A RID: 618
		bool EnableLimitationReplacement { get; }

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x0600026B RID: 619
		bool EnableTechnologyPollingAssignmentsChangesAuditing { get; }

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x0600026C RID: 620
		int LimitationSqlExaggeration { get; }

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x0600026D RID: 621
		TimeSpan AgentDiscoveryPluginsDeploymentTimeLimit { get; }

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x0600026E RID: 622
		TimeSpan AgentDiscoveryJobTimeout { get; }

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x0600026F RID: 623
		TimeSpan SafeCertificateMaintenanceTrialPeriod { get; }

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x06000270 RID: 624
		TimeSpan CertificateMaintenanceTaskCheckInterval { get; }

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x06000271 RID: 625
		TimeSpan CertificateMaintenanceNotificationReappearPeriod { get; }

		// Token: 0x17000043 RID: 67
		// (get) Token: 0x06000272 RID: 626
		TimeSpan CertificateMaintenanceAgentPollFrequency { get; }

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x06000273 RID: 627
		TimeSpan TestJobTimeout { get; }

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x06000274 RID: 628
		TimeSpan OrionFeatureRefreshTimer { get; }

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x06000275 RID: 629
		TimeSpan BackgroundInventoryCheckTimer { get; }

		// Token: 0x17000047 RID: 71
		// (get) Token: 0x06000276 RID: 630
		int BackgroundInventoryParallelTasksCount { get; }

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x06000277 RID: 631
		int BackgroundInventoryRetriesCount { get; }

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x06000278 RID: 632
		int ThresholdsProcessingBatchSize { get; }

		// Token: 0x1700004A RID: 74
		// (get) Token: 0x06000279 RID: 633
		string ThresholdsProcessingDefaultTimeFrame { get; }

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x0600027A RID: 634
		TimeSpan ThresholdsProcessingDefaultTimer { get; }

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x0600027B RID: 635
		bool ThresholdsProcessingEnabled { get; }

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x0600027C RID: 636
		string ThresholdsUseBaselineCalculationMacro { get; }

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x0600027D RID: 637
		string ThresholdsUseBaselineWarningCalculationMacro { get; }

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x0600027E RID: 638
		string ThresholdsUseBaselineCriticalCalculationMacro { get; }

		// Token: 0x17000050 RID: 80
		// (get) Token: 0x0600027F RID: 639
		string ThresholdsDefaultWarningFormulaForGreater { get; }

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x06000280 RID: 640
		string ThresholdsDefaultWarningFormulaForLess { get; }

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x06000281 RID: 641
		string ThresholdsDefaultCriticalFormulaForGreater { get; }

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x06000282 RID: 642
		string ThresholdsDefaultCriticalFormulaForLess { get; }

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x06000283 RID: 643
		int ThresholdsHistogramChartIntervalsCount { get; }

		// Token: 0x17000055 RID: 85
		// (get) Token: 0x06000284 RID: 644
		int EvaluationExpirationCheckIntervalHours { get; }

		// Token: 0x17000056 RID: 86
		// (get) Token: 0x06000285 RID: 645
		int EvaluationExpirationNotificationDays { get; }

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x06000286 RID: 646
		int EvaluationExpirationShowAgainAtDays { get; }

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x06000287 RID: 647
		int CachedWebImageExpirationPeriodDays { get; }
	}
}
