using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Xml;
using SolarWinds.JobEngine;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Common.JobEngine;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Models.Discovery;
using SolarWinds.Orion.Discovery.Contract.DiscoveryPlugin;
using SolarWinds.Orion.Discovery.Framework.Interfaces;
using SolarWinds.Orion.Discovery.Framework.Pluggability;
using SolarWinds.Orion.Discovery.Job;
using SolarWinds.Serialization.Json;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200002D RID: 45
	public class OrionDiscoveryJobFactory : IJobFactory
	{
		// Token: 0x06000373 RID: 883 RVA: 0x0001592E File Offset: 0x00013B2E
		public OrionDiscoveryJobFactory() : this(new EngineDAL())
		{
		}

		// Token: 0x06000374 RID: 884 RVA: 0x0001593B File Offset: 0x00013B3B
		internal OrionDiscoveryJobFactory(IEngineDAL engineDal)
		{
			if (engineDal == null)
			{
				throw new ArgumentNullException("engineDal");
			}
			this.engineDAL = engineDal;
		}

		// Token: 0x06000375 RID: 885 RVA: 0x00015958 File Offset: 0x00013B58
		public string GetOrionDiscoveryJobDescriptionString(OrionDiscoveryJobDescription discoveryJobDescription, List<DiscoveryPluginInfo> pluginInfos, bool jsonFormat = false)
		{
			if (jsonFormat)
			{
				return SerializationHelper.ToJson(discoveryJobDescription);
			}
			DiscoveryPluginInfoCollection discoveryPluginInfoCollection = new DiscoveryPluginInfoCollection
			{
				PluginInfos = pluginInfos
			};
			List<Type> list = new List<Type>();
			foreach (DiscoveryPluginJobDescriptionBase discoveryPluginJobDescriptionBase in discoveryJobDescription.DiscoveryPluginJobDescriptions)
			{
				if (!list.Contains(discoveryPluginJobDescriptionBase.GetType()))
				{
					list.Add(discoveryPluginJobDescriptionBase.GetType());
				}
			}
			return SerializationHelper.XmlWrap(new List<string>
			{
				SerializationHelper.ToXmlString(discoveryPluginInfoCollection),
				SerializationHelper.ToXmlString(discoveryJobDescription, list)
			});
		}

		// Token: 0x06000376 RID: 886 RVA: 0x00015A00 File Offset: 0x00013C00
		public void GetOrionDiscoveryJobDescriptionXml(OrionDiscoveryJobDescription discoveryJobDescription, List<DiscoveryPluginInfo> pluginInfos, XmlWriter xmlWriter)
		{
			IEnumerable<Type> enumerable = (from pjd in discoveryJobDescription.DiscoveryPluginJobDescriptions
			select pjd.GetType()).Distinct<Type>();
			SerializationHelper.XmlWrap(new XmlReader[]
			{
				SerializationHelper.ToXmlReader(new DiscoveryPluginInfoCollection
				{
					PluginInfos = pluginInfos
				}),
				SerializationHelper.ToXmlReader(discoveryJobDescription, enumerable)
			}, xmlWriter);
		}

		// Token: 0x06000377 RID: 887 RVA: 0x00015A67 File Offset: 0x00013C67
		public ScheduledJob CreateDiscoveryJob(DiscoveryConfiguration configuration)
		{
			return this.CreateDiscoveryJob(configuration, new DiscoveryPluginFactory());
		}

		// Token: 0x06000378 RID: 888 RVA: 0x00015A78 File Offset: 0x00013C78
		internal static DiscoveryPollingEngineType? GetDiscoveryPollingEngineType(int engineId, IEngineDAL engineDal = null)
		{
			engineDal = (engineDal ?? new EngineDAL());
			Engine engine = engineDal.GetEngine(engineId);
			if (engine.ServerType.Equals("BranchOffice"))
			{
				engine.ServerType = "RemoteCollector";
			}
			DiscoveryPollingEngineType value;
			if (Enum.TryParse<DiscoveryPollingEngineType>(engine.ServerType, true, out value))
			{
				return new DiscoveryPollingEngineType?(value);
			}
			if (OrionDiscoveryJobFactory.log.IsErrorEnabled)
			{
				OrionDiscoveryJobFactory.log.Error("Unable to determine DiscoveryPollingEngineType value for engine server type '" + engine.ServerType + "'");
			}
			return null;
		}

		// Token: 0x06000379 RID: 889 RVA: 0x00015B01 File Offset: 0x00013D01
		internal static bool IsDiscoveryPluginSupportedForDiscoveryPollingEngineType(IDiscoveryPlugin plugin, DiscoveryPollingEngineType discovryPollingEngineType, IDictionary<IDiscoveryPlugin, DiscoveryPluginInfo> pluginInfoPairs)
		{
			return pluginInfoPairs.ContainsKey(plugin) && pluginInfoPairs[plugin].SupportedPollingEngineTypes.Contains(discovryPollingEngineType);
		}

		// Token: 0x0600037A RID: 890 RVA: 0x00015B20 File Offset: 0x00013D20
		public ScheduledJob CreateDiscoveryJob(DiscoveryConfiguration configuration, IDiscoveryPluginFactory pluginFactory)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}
			Engine engine = this.engineDAL.GetEngine(configuration.EngineId);
			DiscoveryPollingEngineType? discoveryPollingEngineType = OrionDiscoveryJobFactory.GetDiscoveryPollingEngineType(configuration.EngineID, this.engineDAL);
			int maxSnmpReplies;
			if (!int.TryParse(SettingsDAL.Get("SWNetPerfMon-Settings-SNMP MaxReps"), out maxSnmpReplies))
			{
				maxSnmpReplies = 5;
			}
			OrionDiscoveryJobDescription orionDiscoveryJobDescription = new OrionDiscoveryJobDescription
			{
				ProfileId = configuration.ProfileId,
				EngineId = configuration.EngineId,
				HopCount = configuration.HopCount,
				IcmpTimeout = configuration.SearchTimeout,
				SnmpConfiguration = new DiscoveryCommonSnmpConfiguration
				{
					MaxSnmpReplies = maxSnmpReplies,
					SnmpRetries = configuration.SnmpRetries,
					SnmpTimeout = configuration.SnmpTimeout,
					SnmpPort = configuration.SnmpPort,
					PreferredSnmpVersion = configuration.PreferredSnmpVersion
				},
				DisableICMP = configuration.DisableICMP,
				PreferredPollingMethod = configuration.GetDiscoveryPluginConfiguration<CoreDiscoveryPluginConfiguration>().PreferredPollingMethod,
				VulnerabilityCheckDisabled = (SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-VulnerabilityCheckDisabled", 0) == 1),
				MaxThreadsInDetectionPhase = SettingsDAL.GetCurrentInt("Discovery-MaxThreadsInDetectionPhase", 5),
				MaxThreadsInInventoryPhase = SettingsDAL.GetCurrentInt("Discovery-MaxThreadsInInventoryPhase", 5),
				PreferredDnsAddressFamily = SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-Default Preferred AddressFamily DHCP", 4),
				TagFilter = configuration.TagFilter,
				DefaultProbes = configuration.DefaultProbes
			};
			List<DiscoveryPluginInfo> discoveryPluginInfos = DiscoveryPluginFactory.GetDiscoveryPluginInfos();
			IList<IDiscoveryPlugin> plugins = pluginFactory.GetPlugins(discoveryPluginInfos);
			List<DiscoveryPluginInfo> list = new List<DiscoveryPluginInfo>();
			IDictionary<IDiscoveryPlugin, DiscoveryPluginInfo> dictionary = DiscoveryPluginHelper.CreatePairsPluginAndInfo(plugins, discoveryPluginInfos);
			bool flag = RegistrySettings.IsFreePoller();
			foreach (IDiscoveryPlugin discoveryPlugin in plugins)
			{
				if (flag && !(discoveryPlugin is ISupportFreeEngine))
				{
					OrionDiscoveryJobFactory.log.DebugFormat("Discovery plugin {0} is not supported on FPE machine", discoveryPlugin);
				}
				else if (configuration.ProfileId == null && !(discoveryPlugin is IOneTimeJobSupport))
				{
					OrionDiscoveryJobFactory.log.DebugFormat("Plugin {0} is not supporting one time job and it's description woun't be added.", discoveryPlugin.GetType().FullName);
				}
				else
				{
					if (configuration.TagFilter != null && configuration.TagFilter.Any<string>())
					{
						IDiscoveryPluginTags discoveryPluginTags = discoveryPlugin as IDiscoveryPluginTags;
						if (discoveryPluginTags == null)
						{
							OrionDiscoveryJobFactory.log.DebugFormat("Discovery job for tags requested, however plugin {0} doesn't support tags, skipping.", discoveryPlugin);
							continue;
						}
						if (!configuration.TagFilter.Intersect(discoveryPluginTags.Tags ?? Enumerable.Empty<string>(), StringComparer.InvariantCultureIgnoreCase).Any<string>())
						{
							OrionDiscoveryJobFactory.log.DebugFormat("Discovery job for tags [{0}], however plugin {1} doesn't support any of the tags requested, skipping.", string.Join(",", configuration.TagFilter), discoveryPlugin);
							continue;
						}
					}
					if (configuration.IsAgentJob)
					{
						IAgentPluginJobSupport agentPluginJobSupport = discoveryPlugin as IAgentPluginJobSupport;
						if (agentPluginJobSupport == null || !configuration.AgentPlugins.Contains(agentPluginJobSupport.PluginId))
						{
							OrionDiscoveryJobFactory.log.DebugFormat("Plugin {0} is not contained in supported agent plugins and will not be used.", discoveryPlugin.GetType().FullName);
							continue;
						}
					}
					if (discoveryPollingEngineType != null && !OrionDiscoveryJobFactory.IsDiscoveryPluginSupportedForDiscoveryPollingEngineType(discoveryPlugin, discoveryPollingEngineType.Value, dictionary))
					{
						if (OrionDiscoveryJobFactory.log.IsDebugEnabled)
						{
							OrionDiscoveryJobFactory.log.DebugFormat(string.Format("Plugin {0} is not supported for polling engine {1}", discoveryPlugin.GetType().FullName, configuration.EngineID), Array.Empty<object>());
						}
					}
					else
					{
						DiscoveryPluginJobDescriptionBase discoveryPluginJobDescriptionBase = null;
						Exception ex = null;
						try
						{
							discoveryPluginJobDescriptionBase = discoveryPlugin.GetJobDescription(configuration);
						}
						catch (Exception ex2)
						{
							discoveryPluginJobDescriptionBase = null;
							ex = ex2;
						}
						if (discoveryPluginJobDescriptionBase == null)
						{
							string text = "Plugin " + discoveryPlugin.GetType().FullName + " was not able found valid job description.";
							if (ex != null)
							{
								OrionDiscoveryJobFactory.log.Warn(text, ex);
							}
							else
							{
								OrionDiscoveryJobFactory.log.Warn(text);
							}
						}
						else
						{
							orionDiscoveryJobDescription.DiscoveryPluginJobDescriptions.Add(discoveryPluginJobDescriptionBase);
							DiscoveryPluginInfo item = dictionary[discoveryPlugin];
							list.Add(item);
						}
					}
				}
			}
			JobDescription jobDescription = new JobDescription
			{
				TypeName = typeof(OrionDiscoveryJob).AssemblyQualifiedName,
				JobDetailConfiguration = this.GetOrionDiscoveryJobDescriptionString(orionDiscoveryJobDescription, list, configuration.UseJsonFormat),
				JobNamespace = "orion",
				ResultTTL = TimeSpan.FromMinutes(10.0),
				TargetNode = new HostAddress(IPAddressHelper.ToStringIp(engine.IP), 4),
				LegacyEngine = engine.ServerName.ToLowerInvariant(),
				EndpointAddress = (configuration.IsAgentJob ? configuration.AgentAddress : null),
				SupportedRoles = 7
			};
			jobDescription.Timeout = OrionDiscoveryJobFactory.GetDiscoveryJobTimeout(configuration);
			ScheduledJob scheduledJob;
			if (configuration.CronSchedule != null)
			{
				bool flag2 = false;
				string text2 = configuration.CronSchedule.CronExpression;
				if (string.IsNullOrWhiteSpace(text2))
				{
					DateTime t = configuration.CronSchedule.StartTime.ToLocalTime();
					if (t < DateTime.Now)
					{
						OrionDiscoveryJobFactory.log.InfoFormat("Profile (ID={0}) with past Once(Cron) schedule. We should not create job for it.", configuration.ProfileID);
						return null;
					}
					text2 = string.Format("{0} {1} {2} {3} *", new object[]
					{
						t.Minute,
						t.Hour,
						t.Day,
						t.Month
					});
					flag2 = true;
				}
				scheduledJob = new ScheduledJob(jobDescription, text2, "net.pipe://localhost/orion/core/scheduleddiscoveryjobsevents2", configuration.ProfileID.ToString());
				scheduledJob.RunOnce = flag2;
				scheduledJob.TimeZoneInfo = TimeZoneInfo.Local;
				if (!flag2)
				{
					scheduledJob.Start = configuration.CronSchedule.StartTime.ToUniversalTime();
					DateTime? endTime = configuration.CronSchedule.EndTime;
					DateTime maxValue = DateTime.MaxValue;
					if ((endTime == null || (endTime != null && endTime.GetValueOrDefault() != maxValue)) && configuration.CronSchedule.EndTime != null)
					{
						scheduledJob.End = configuration.CronSchedule.EndTime.Value.ToUniversalTime();
					}
				}
			}
			else if (!configuration.ScheduleRunAtTime.Equals(DateTime.MinValue))
			{
				scheduledJob = new ScheduledJob(jobDescription, configuration.ScheduleRunAtTime, "net.pipe://localhost/orion/core/scheduleddiscoveryjobsevents2", configuration.ProfileID.ToString());
			}
			else
			{
				scheduledJob = new ScheduledJob(jobDescription, configuration.ScheduleRunFrequency, "net.pipe://localhost/orion/core/scheduleddiscoveryjobsevents2", configuration.ProfileID.ToString());
			}
			return scheduledJob;
		}

		// Token: 0x0600037B RID: 891 RVA: 0x0001619C File Offset: 0x0001439C
		private static TimeSpan GetDiscoveryJobTimeout(DiscoveryConfiguration configuration)
		{
			if (configuration.IsAgentJob)
			{
				return BusinessLayerSettings.Instance.AgentDiscoveryJobTimeout;
			}
			if (configuration.JobTimeout == TimeSpan.Zero || configuration.JobTimeout == TimeSpan.MinValue)
			{
				return TimeSpan.FromMinutes(60.0);
			}
			return configuration.JobTimeout;
		}

		// Token: 0x0600037C RID: 892 RVA: 0x000161F8 File Offset: 0x000143F8
		private Guid SubmitScheduledJobToScheduler(Guid jobId, ScheduledJob job, bool executeImmediately, bool useLocal)
		{
			Guid result;
			using (IJobSchedulerHelper jobSchedulerHelper = useLocal ? JobScheduler.GetLocalInstance() : JobScheduler.GetMainInstance())
			{
				if (jobId == Guid.Empty)
				{
					OrionDiscoveryJobFactory.log.Debug("Adding new job to Job Engine");
					result = jobSchedulerHelper.AddJob(job);
				}
				else
				{
					try
					{
						OrionDiscoveryJobFactory.log.DebugFormat("Updating job definition in Job Engine ({0})", jobId);
						jobSchedulerHelper.UpdateJob(jobId, job, executeImmediately);
						return jobId;
					}
					catch (FaultException<JobEngineConnectionFault>)
					{
						OrionDiscoveryJobFactory.log.DebugFormat("Unable to update job definition in Job Engine({0}", jobId);
						throw;
					}
					catch (Exception)
					{
						OrionDiscoveryJobFactory.log.DebugFormat("Unable to update job definition in Job Engine({0}", jobId);
					}
					OrionDiscoveryJobFactory.log.Debug("Adding new job to Job Engine");
					result = jobSchedulerHelper.AddJob(job);
				}
			}
			return result;
		}

		// Token: 0x0600037D RID: 893 RVA: 0x000162DC File Offset: 0x000144DC
		public Guid SubmitScheduledJob(Guid jobId, ScheduledJob job, bool executeImmediately)
		{
			return this.SubmitScheduledJobToScheduler(jobId, job, executeImmediately, false);
		}

		// Token: 0x0600037E RID: 894 RVA: 0x000162E8 File Offset: 0x000144E8
		public Guid SubmitScheduledJobToLocalEngine(Guid jobId, ScheduledJob job, bool executeImmediately)
		{
			return this.SubmitScheduledJobToScheduler(jobId, job, executeImmediately, true);
		}

		// Token: 0x0600037F RID: 895 RVA: 0x000162F4 File Offset: 0x000144F4
		public bool DeleteJob(Guid jobId)
		{
			bool result;
			using (IJobSchedulerHelper localInstance = JobScheduler.GetLocalInstance())
			{
				try
				{
					localInstance.RemoveJob(jobId);
					result = true;
				}
				catch
				{
					OrionDiscoveryJobFactory.log.DebugFormat("Unable to delete job in Job Engine({0}", jobId);
					result = false;
				}
			}
			return result;
		}

		// Token: 0x040000B6 RID: 182
		private static readonly Log log = new Log();

		// Token: 0x040000B7 RID: 183
		private const int DefaultJobTimeout = 60;

		// Token: 0x040000B8 RID: 184
		private const string ListenerUri = "net.pipe://localhost/orion/core/scheduleddiscoveryjobsevents2";

		// Token: 0x040000B9 RID: 185
		private readonly IEngineDAL engineDAL;
	}
}
