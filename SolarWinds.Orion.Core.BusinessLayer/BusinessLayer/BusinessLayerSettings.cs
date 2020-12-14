using System;
using SolarWinds.Orion.Core.Common.Configuration;
using SolarWinds.Orion.Core.SharedCredentials.Credentials;
using SolarWinds.Settings;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000047 RID: 71
	internal class BusinessLayerSettings : SettingsBase, IBusinessLayerSettings
	{
		// Token: 0x06000443 RID: 1091 RVA: 0x0001D0A8 File Offset: 0x0001B2A8
		private BusinessLayerSettings()
		{
		}

		// Token: 0x17000081 RID: 129
		// (get) Token: 0x06000444 RID: 1092 RVA: 0x0001D0B0 File Offset: 0x0001B2B0
		public static IBusinessLayerSettings Instance
		{
			get
			{
				return BusinessLayerSettings.Factory();
			}
		}

		// Token: 0x17000082 RID: 130
		// (get) Token: 0x06000445 RID: 1093 RVA: 0x0001D0BC File Offset: 0x0001B2BC
		// (set) Token: 0x06000446 RID: 1094 RVA: 0x0001D0C4 File Offset: 0x0001B2C4
		[Setting(Default = 30, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public int DBConnectionRetryInterval { get; internal set; }

		// Token: 0x17000083 RID: 131
		// (get) Token: 0x06000447 RID: 1095 RVA: 0x0001D0CD File Offset: 0x0001B2CD
		// (set) Token: 0x06000448 RID: 1096 RVA: 0x0001D0D5 File Offset: 0x0001B2D5
		[Setting(Default = 300, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public int DBConnectionRetryIntervalOnFail { get; internal set; }

		// Token: 0x17000084 RID: 132
		// (get) Token: 0x06000449 RID: 1097 RVA: 0x0001D0DE File Offset: 0x0001B2DE
		// (set) Token: 0x0600044A RID: 1098 RVA: 0x0001D0E6 File Offset: 0x0001B2E6
		[Setting(Default = 10, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public int DBConnectionRetries { get; internal set; }

		// Token: 0x17000085 RID: 133
		// (get) Token: 0x0600044B RID: 1099 RVA: 0x0001D0EF File Offset: 0x0001B2EF
		// (set) Token: 0x0600044C RID: 1100 RVA: 0x0001D0F7 File Offset: 0x0001B2F7
		[Setting(Default = "net.pipe://localhost/solarwinds/jobengine/scheduler", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public string JobSchedulerEndpointNetPipe { get; internal set; }

		// Token: 0x17000086 RID: 134
		// (get) Token: 0x0600044D RID: 1101 RVA: 0x0001D100 File Offset: 0x0001B300
		// (set) Token: 0x0600044E RID: 1102 RVA: 0x0001D108 File Offset: 0x0001B308
		[Setting(Default = "net.tcp://{0}:17777/solarwinds/jobengine/scheduler/ssl", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public string JobSchedulerEndpointTcpPipe { get; internal set; }

		// Token: 0x17000087 RID: 135
		// (get) Token: 0x0600044F RID: 1103 RVA: 0x0001D111 File Offset: 0x0001B311
		[Obsolete("use SolarWinds.Orion.Core.Common.Configuration.HttpProxySettings without using WCF")]
		public bool ProxyAvailable
		{
			get
			{
				return !HttpProxySettings.Instance.IsDisabled;
			}
		}

		// Token: 0x17000088 RID: 136
		// (get) Token: 0x06000450 RID: 1104 RVA: 0x0001D120 File Offset: 0x0001B320
		[Obsolete("use SolarWinds.Orion.Core.Common.Configuration.HttpProxySettings without using WCF")]
		public string UserName
		{
			get
			{
				IHttpProxySettings httpProxySettings = HttpProxySettings.Instance;
				if (!httpProxySettings.IsValid)
				{
					return null;
				}
				if (httpProxySettings.UseSystemDefaultProxy)
				{
					return string.Empty;
				}
				UsernamePasswordCredential usernamePasswordCredential = httpProxySettings.Credential as UsernamePasswordCredential;
				if (usernamePasswordCredential == null)
				{
					return null;
				}
				return usernamePasswordCredential.Username;
			}
		}

		// Token: 0x17000089 RID: 137
		// (get) Token: 0x06000451 RID: 1105 RVA: 0x0001D164 File Offset: 0x0001B364
		[Obsolete("use SolarWinds.Orion.Core.Common.Configuration.HttpProxySettings without using WCF")]
		public string Password
		{
			get
			{
				IHttpProxySettings httpProxySettings = HttpProxySettings.Instance;
				if (!httpProxySettings.IsValid)
				{
					return null;
				}
				UsernamePasswordCredential usernamePasswordCredential = httpProxySettings.Credential as UsernamePasswordCredential;
				if (usernamePasswordCredential == null)
				{
					return null;
				}
				return usernamePasswordCredential.Password;
			}
		}

		// Token: 0x1700008A RID: 138
		// (get) Token: 0x06000452 RID: 1106 RVA: 0x0001D198 File Offset: 0x0001B398
		[Obsolete("use SolarWinds.Orion.Core.Common.Configuration.HttpProxySettings without using WCF")]
		public string ProxyAddress
		{
			get
			{
				IHttpProxySettings httpProxySettings = HttpProxySettings.Instance;
				if (!httpProxySettings.IsValid)
				{
					return string.Empty;
				}
				return new Uri(httpProxySettings.Uri).Host;
			}
		}

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x06000453 RID: 1107 RVA: 0x0001D1CC File Offset: 0x0001B3CC
		[Obsolete("use SolarWinds.Orion.Core.Common.Configuration.HttpProxySettings without using WCF")]
		public int ProxyPort
		{
			get
			{
				IHttpProxySettings httpProxySettings = HttpProxySettings.Instance;
				if (!httpProxySettings.IsValid)
				{
					return 0;
				}
				return new Uri(httpProxySettings.Uri).Port;
			}
		}

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x06000454 RID: 1108 RVA: 0x0001D1F9 File Offset: 0x0001B3F9
		// (set) Token: 0x06000455 RID: 1109 RVA: 0x0001D201 File Offset: 0x0001B401
		[Setting(Default = "http://thwackfeeds.solarwinds.com/blogs/orion-product-team-blog/rss.aspx", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public string OrionProductTeamBlogUrl { get; internal set; }

		// Token: 0x1700008D RID: 141
		// (get) Token: 0x06000456 RID: 1110 RVA: 0x0001D20A File Offset: 0x0001B40A
		// (set) Token: 0x06000457 RID: 1111 RVA: 0x0001D212 File Offset: 0x0001B412
		[Setting(Default = 60, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public int LicenseSaturationCheckInterval { get; internal set; }

		// Token: 0x1700008E RID: 142
		// (get) Token: 0x06000458 RID: 1112 RVA: 0x0001D21B File Offset: 0x0001B41B
		// (set) Token: 0x06000459 RID: 1113 RVA: 0x0001D223 File Offset: 0x0001B423
		[Setting(Default = 90, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public int MaintenanceExpirationWarningDays { get; internal set; }

		// Token: 0x1700008F RID: 143
		// (get) Token: 0x0600045A RID: 1114 RVA: 0x0001D22C File Offset: 0x0001B42C
		// (set) Token: 0x0600045B RID: 1115 RVA: 0x0001D234 File Offset: 0x0001B434
		[Setting(Default = 15, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public int MaintenanceExpiredShowAgainAtDays { get; internal set; }

		// Token: 0x17000090 RID: 144
		// (get) Token: 0x0600045C RID: 1116 RVA: 0x0001D23D File Offset: 0x0001B43D
		// (set) Token: 0x0600045D RID: 1117 RVA: 0x0001D245 File Offset: 0x0001B445
		[Setting(Default = "00:05:00", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public TimeSpan PollerLimitTimer { get; internal set; }

		// Token: 0x17000091 RID: 145
		// (get) Token: 0x0600045E RID: 1118 RVA: 0x0001D24E File Offset: 0x0001B44E
		// (set) Token: 0x0600045F RID: 1119 RVA: 0x0001D256 File Offset: 0x0001B456
		[Setting(Default = "00:30:00", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public TimeSpan CheckDatabaseLimitTimer { get; internal set; }

		// Token: 0x17000092 RID: 146
		// (get) Token: 0x06000460 RID: 1120 RVA: 0x0001D25F File Offset: 0x0001B45F
		// (set) Token: 0x06000461 RID: 1121 RVA: 0x0001D267 File Offset: 0x0001B467
		[Setting(Default = "00:05:00", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public TimeSpan CheckForOldLogsTimer { get; internal set; }

		// Token: 0x17000093 RID: 147
		// (get) Token: 0x06000462 RID: 1122 RVA: 0x0001D270 File Offset: 0x0001B470
		// (set) Token: 0x06000463 RID: 1123 RVA: 0x0001D278 File Offset: 0x0001B478
		[Setting(Default = "00:00:30", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public TimeSpan UpdateEngineTimer { get; internal set; }

		// Token: 0x17000094 RID: 148
		// (get) Token: 0x06000464 RID: 1124 RVA: 0x0001D281 File Offset: 0x0001B481
		// (set) Token: 0x06000465 RID: 1125 RVA: 0x0001D289 File Offset: 0x0001B489
		[Setting(Default = "00:02:00", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public TimeSpan RemoteCollectorStatusCacheExpiration { get; internal set; }

		// Token: 0x17000095 RID: 149
		// (get) Token: 0x06000466 RID: 1126 RVA: 0x0001D292 File Offset: 0x0001B492
		// (set) Token: 0x06000467 RID: 1127 RVA: 0x0001D29A File Offset: 0x0001B49A
		[Setting(Default = false, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public bool MaintenanceModeEnabled { get; internal set; }

		// Token: 0x17000096 RID: 150
		// (get) Token: 0x06000468 RID: 1128 RVA: 0x0001D2A3 File Offset: 0x0001B4A3
		// (set) Token: 0x06000469 RID: 1129 RVA: 0x0001D2AB File Offset: 0x0001B4AB
		[Setting(Default = "00:01:00", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public TimeSpan SettingsToRegistryFrequency { get; internal set; }

		// Token: 0x17000097 RID: 151
		// (get) Token: 0x0600046A RID: 1130 RVA: 0x0001D2B4 File Offset: 0x0001B4B4
		// (set) Token: 0x0600046B RID: 1131 RVA: 0x0001D2BC File Offset: 0x0001B4BC
		[Setting(Default = "00:00:10", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public TimeSpan DiscoveryUpdateNetObjectStatusWaitForChangesDelay { get; internal set; }

		// Token: 0x17000098 RID: 152
		// (get) Token: 0x0600046C RID: 1132 RVA: 0x0001D2C5 File Offset: 0x0001B4C5
		// (set) Token: 0x0600046D RID: 1133 RVA: 0x0001D2CD File Offset: 0x0001B4CD
		[Setting(Default = "00:02:00", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public TimeSpan DiscoveryUpdateNetObjectStatusStartupDelay { get; internal set; }

		// Token: 0x17000099 RID: 153
		// (get) Token: 0x0600046E RID: 1134 RVA: 0x0001D2D6 File Offset: 0x0001B4D6
		// (set) Token: 0x0600046F RID: 1135 RVA: 0x0001D2DE File Offset: 0x0001B4DE
		[Setting(Default = true, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public bool EnableLimitationReplacement { get; internal set; }

		// Token: 0x1700009A RID: 154
		// (get) Token: 0x06000470 RID: 1136 RVA: 0x0001D2E7 File Offset: 0x0001B4E7
		// (set) Token: 0x06000471 RID: 1137 RVA: 0x0001D2EF File Offset: 0x0001B4EF
		[Setting(Default = true, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public bool EnableTechnologyPollingAssignmentsChangesAuditing { get; internal set; }

		// Token: 0x1700009B RID: 155
		// (get) Token: 0x06000472 RID: 1138 RVA: 0x0001D2F8 File Offset: 0x0001B4F8
		// (set) Token: 0x06000473 RID: 1139 RVA: 0x0001D300 File Offset: 0x0001B500
		[Setting(Default = 5, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public int LimitationSqlExaggeration { get; internal set; }

		// Token: 0x1700009C RID: 156
		// (get) Token: 0x06000474 RID: 1140 RVA: 0x0001D309 File Offset: 0x0001B509
		// (set) Token: 0x06000475 RID: 1141 RVA: 0x0001D311 File Offset: 0x0001B511
		[Setting(Default = "00:10:00", AllowServerOverride = true)]
		public TimeSpan AgentDiscoveryPluginsDeploymentTimeLimit { get; internal set; }

		// Token: 0x1700009D RID: 157
		// (get) Token: 0x06000476 RID: 1142 RVA: 0x0001D31A File Offset: 0x0001B51A
		// (set) Token: 0x06000477 RID: 1143 RVA: 0x0001D322 File Offset: 0x0001B522
		[Setting(Default = "00:10:00", AllowServerOverride = true)]
		public TimeSpan AgentDiscoveryJobTimeout { get; internal set; }

		// Token: 0x1700009E RID: 158
		// (get) Token: 0x06000478 RID: 1144 RVA: 0x0001D32B File Offset: 0x0001B52B
		// (set) Token: 0x06000479 RID: 1145 RVA: 0x0001D333 File Offset: 0x0001B533
		[Setting(Default = "1.00:00:00", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		}, Description = "Time for which we try to perform safe certificate maintenance. If the certificate maintenance can't be done in a safe way - no damage to the system or data - for given time, we inform user and let him confirm maintenance with knowledge what will break.")]
		public TimeSpan SafeCertificateMaintenanceTrialPeriod { get; internal set; }

		// Token: 0x1700009F RID: 159
		// (get) Token: 0x0600047A RID: 1146 RVA: 0x0001D33C File Offset: 0x0001B53C
		// (set) Token: 0x0600047B RID: 1147 RVA: 0x0001D344 File Offset: 0x0001B544
		[Setting(Default = "00:05:00", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		}, Description = "Frequency with which certificate maintenance task result is checked.")]
		public TimeSpan CertificateMaintenanceTaskCheckInterval { get; internal set; }

		// Token: 0x170000A0 RID: 160
		// (get) Token: 0x0600047C RID: 1148 RVA: 0x0001D34D File Offset: 0x0001B54D
		// (set) Token: 0x0600047D RID: 1149 RVA: 0x0001D355 File Offset: 0x0001B555
		[Setting(Default = "00:05:00", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		}, Description = "After how long a notification about certificate maintenance approval reappears if customer acknowledges it and does not approve certificate maintenance.")]
		public TimeSpan CertificateMaintenanceNotificationReappearPeriod { get; internal set; }

		// Token: 0x170000A1 RID: 161
		// (get) Token: 0x0600047E RID: 1150 RVA: 0x0001D35E File Offset: 0x0001B55E
		// (set) Token: 0x0600047F RID: 1151 RVA: 0x0001D366 File Offset: 0x0001B566
		[Setting(Default = "00:01:00", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		}, Description = "How often Core polls AMS to get fresh status of agent certificate update for certificate maintenance.")]
		public TimeSpan CertificateMaintenanceAgentPollFrequency { get; internal set; }

		// Token: 0x170000A2 RID: 162
		// (get) Token: 0x06000480 RID: 1152 RVA: 0x0001D36F File Offset: 0x0001B56F
		// (set) Token: 0x06000481 RID: 1153 RVA: 0x0001D377 File Offset: 0x0001B577
		[Setting(Default = "00:00:30", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		}, Description = "How long Core waits for results from test jobs.")]
		public TimeSpan TestJobTimeout { get; internal set; }

		// Token: 0x170000A3 RID: 163
		// (get) Token: 0x06000482 RID: 1154 RVA: 0x0001D380 File Offset: 0x0001B580
		// (set) Token: 0x06000483 RID: 1155 RVA: 0x0001D388 File Offset: 0x0001B588
		[Setting(Default = "00:10:00", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public TimeSpan BackgroundInventoryCheckTimer { get; internal set; }

		// Token: 0x170000A4 RID: 164
		// (get) Token: 0x06000484 RID: 1156 RVA: 0x0001D391 File Offset: 0x0001B591
		// (set) Token: 0x06000485 RID: 1157 RVA: 0x0001D399 File Offset: 0x0001B599
		[Setting(Default = 50, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public int BackgroundInventoryParallelTasksCount { get; internal set; }

		// Token: 0x170000A5 RID: 165
		// (get) Token: 0x06000486 RID: 1158 RVA: 0x0001D3A2 File Offset: 0x0001B5A2
		// (set) Token: 0x06000487 RID: 1159 RVA: 0x0001D3AA File Offset: 0x0001B5AA
		[Setting(Default = 10, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public int BackgroundInventoryRetriesCount { get; internal set; }

		// Token: 0x170000A6 RID: 166
		// (get) Token: 0x06000488 RID: 1160 RVA: 0x0001D3B3 File Offset: 0x0001B5B3
		// (set) Token: 0x06000489 RID: 1161 RVA: 0x0001D3BB File Offset: 0x0001B5BB
		[Setting(Default = 10000, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public int ThresholdsProcessingBatchSize { get; internal set; }

		// Token: 0x170000A7 RID: 167
		// (get) Token: 0x0600048A RID: 1162 RVA: 0x0001D3C4 File Offset: 0x0001B5C4
		// (set) Token: 0x0600048B RID: 1163 RVA: 0x0001D3CC File Offset: 0x0001B5CC
		[Setting(Default = "Core_All", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public string ThresholdsProcessingDefaultTimeFrame { get; internal set; }

		// Token: 0x170000A8 RID: 168
		// (get) Token: 0x0600048C RID: 1164 RVA: 0x0001D3D5 File Offset: 0x0001B5D5
		// (set) Token: 0x0600048D RID: 1165 RVA: 0x0001D3DD File Offset: 0x0001B5DD
		[Setting(Default = "00:05:00", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public TimeSpan ThresholdsProcessingDefaultTimer { get; internal set; }

		// Token: 0x170000A9 RID: 169
		// (get) Token: 0x0600048E RID: 1166 RVA: 0x0001D3E6 File Offset: 0x0001B5E6
		// (set) Token: 0x0600048F RID: 1167 RVA: 0x0001D3EE File Offset: 0x0001B5EE
		[Setting(Default = true, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public bool ThresholdsProcessingEnabled { get; internal set; }

		// Token: 0x170000AA RID: 170
		// (get) Token: 0x06000490 RID: 1168 RVA: 0x0001D3F7 File Offset: 0x0001B5F7
		// (set) Token: 0x06000491 RID: 1169 RVA: 0x0001D3FF File Offset: 0x0001B5FF
		[Setting(Default = "${USE_BASELINE}", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public string ThresholdsUseBaselineCalculationMacro { get; internal set; }

		// Token: 0x170000AB RID: 171
		// (get) Token: 0x06000492 RID: 1170 RVA: 0x0001D408 File Offset: 0x0001B608
		// (set) Token: 0x06000493 RID: 1171 RVA: 0x0001D410 File Offset: 0x0001B610
		[Setting(Default = "${USE_BASELINE_WARNING}", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public string ThresholdsUseBaselineWarningCalculationMacro { get; internal set; }

		// Token: 0x170000AC RID: 172
		// (get) Token: 0x06000494 RID: 1172 RVA: 0x0001D419 File Offset: 0x0001B619
		// (set) Token: 0x06000495 RID: 1173 RVA: 0x0001D421 File Offset: 0x0001B621
		[Setting(Default = "${USE_BASELINE_CRITICAL}", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public string ThresholdsUseBaselineCriticalCalculationMacro { get; internal set; }

		// Token: 0x170000AD RID: 173
		// (get) Token: 0x06000496 RID: 1174 RVA: 0x0001D42A File Offset: 0x0001B62A
		// (set) Token: 0x06000497 RID: 1175 RVA: 0x0001D432 File Offset: 0x0001B632
		[Setting(Default = "${MEAN}+2*${STD_DEV}", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public string ThresholdsDefaultWarningFormulaForGreater { get; internal set; }

		// Token: 0x170000AE RID: 174
		// (get) Token: 0x06000498 RID: 1176 RVA: 0x0001D43B File Offset: 0x0001B63B
		// (set) Token: 0x06000499 RID: 1177 RVA: 0x0001D443 File Offset: 0x0001B643
		[Setting(Default = "${MEAN}-2*${STD_DEV}", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public string ThresholdsDefaultWarningFormulaForLess { get; internal set; }

		// Token: 0x170000AF RID: 175
		// (get) Token: 0x0600049A RID: 1178 RVA: 0x0001D44C File Offset: 0x0001B64C
		// (set) Token: 0x0600049B RID: 1179 RVA: 0x0001D454 File Offset: 0x0001B654
		[Setting(Default = "${MEAN}+3*${STD_DEV}", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public string ThresholdsDefaultCriticalFormulaForGreater { get; internal set; }

		// Token: 0x170000B0 RID: 176
		// (get) Token: 0x0600049C RID: 1180 RVA: 0x0001D45D File Offset: 0x0001B65D
		// (set) Token: 0x0600049D RID: 1181 RVA: 0x0001D465 File Offset: 0x0001B665
		[Setting(Default = "${MEAN}-3*${STD_DEV}", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public string ThresholdsDefaultCriticalFormulaForLess { get; internal set; }

		// Token: 0x170000B1 RID: 177
		// (get) Token: 0x0600049E RID: 1182 RVA: 0x0001D46E File Offset: 0x0001B66E
		// (set) Token: 0x0600049F RID: 1183 RVA: 0x0001D476 File Offset: 0x0001B676
		[Setting(Default = 50, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public int ThresholdsHistogramChartIntervalsCount { get; internal set; }

		// Token: 0x170000B2 RID: 178
		// (get) Token: 0x060004A0 RID: 1184 RVA: 0x0001D47F File Offset: 0x0001B67F
		// (set) Token: 0x060004A1 RID: 1185 RVA: 0x0001D487 File Offset: 0x0001B687
		[Setting(Default = 12, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public int EvaluationExpirationCheckIntervalHours { get; internal set; }

		// Token: 0x170000B3 RID: 179
		// (get) Token: 0x060004A2 RID: 1186 RVA: 0x0001D490 File Offset: 0x0001B690
		// (set) Token: 0x060004A3 RID: 1187 RVA: 0x0001D498 File Offset: 0x0001B698
		[Setting(Default = 14, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public int EvaluationExpirationNotificationDays { get; internal set; }

		// Token: 0x170000B4 RID: 180
		// (get) Token: 0x060004A4 RID: 1188 RVA: 0x0001D4A1 File Offset: 0x0001B6A1
		// (set) Token: 0x060004A5 RID: 1189 RVA: 0x0001D4A9 File Offset: 0x0001B6A9
		[Setting(Default = 7, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public int EvaluationExpirationShowAgainAtDays { get; internal set; }

		// Token: 0x170000B5 RID: 181
		// (get) Token: 0x060004A6 RID: 1190 RVA: 0x0001D4B2 File Offset: 0x0001B6B2
		// (set) Token: 0x060004A7 RID: 1191 RVA: 0x0001D4BA File Offset: 0x0001B6BA
		[Setting(Default = 7, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public int CachedWebImageExpirationPeriodDays { get; internal set; }

		// Token: 0x170000B6 RID: 182
		// (get) Token: 0x060004A8 RID: 1192 RVA: 0x0001D4C3 File Offset: 0x0001B6C3
		// (set) Token: 0x060004A9 RID: 1193 RVA: 0x0001D4CB File Offset: 0x0001B6CB
		[Setting(Default = "00:10:00", AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		}, Description = "How often we synchronize Orion.Features.")]
		public TimeSpan OrionFeatureRefreshTimer { get; internal set; }

		// Token: 0x0400010D RID: 269
		private static readonly Lazy<IBusinessLayerSettings> instance = new Lazy<IBusinessLayerSettings>(() => new BusinessLayerSettings());

		// Token: 0x0400010E RID: 270
		public static Func<IBusinessLayerSettings> Factory = () => BusinessLayerSettings.instance.Value;
	}
}
