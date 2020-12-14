using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SolarWinds.InformationService.Linq.Plugins;
using SolarWinds.InformationService.Linq.Plugins.Core.Orion;
using SolarWinds.JobEngine;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Common.IO;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.BusinessLayer.Discovery;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Agent;
using SolarWinds.Orion.Core.Common.i18n;
using SolarWinds.Orion.Core.Common.JobEngine;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Common.Swis;
using SolarWinds.Orion.Core.Discovery;
using SolarWinds.Orion.Core.Discovery.DAL;
using SolarWinds.Orion.Core.Discovery.DataAccess;
using SolarWinds.Orion.Core.Models.DiscoveredObjects;
using SolarWinds.Orion.Core.Models.Discovery;
using SolarWinds.Orion.Core.Models.Interfaces;
using SolarWinds.Orion.Core.SharedCredentials;
using SolarWinds.Orion.Core.Strings;
using SolarWinds.Orion.Discovery.Contract.DiscoveryPlugin;
using SolarWinds.Orion.Discovery.Framework.Pluggability;
using SolarWinds.Orion.Discovery.Job;
using SolarWinds.Serialization.Json;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200002E RID: 46
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true, AutomaticSessionShutdown = true, ConcurrencyMode = ConcurrencyMode.Multiple)]
	public class OrionDiscoveryJobSchedulerEventsService : JobSchedulerEventServicev2
	{
		// Token: 0x06000381 RID: 897 RVA: 0x00016360 File Offset: 0x00014560
		private static CultureInfo GetPrimaryLocale()
		{
			try
			{
				string primaryLocale = LocaleConfiguration.PrimaryLocale;
				JobSchedulerEventServicev2.log.Verbose("Primary locale set to " + primaryLocale);
				return new CultureInfo(primaryLocale);
			}
			catch (Exception ex)
			{
				JobSchedulerEventServicev2.log.Error("Error while getting primary locale CultureInfo", ex);
			}
			return null;
		}

		// Token: 0x06000382 RID: 898 RVA: 0x000163B8 File Offset: 0x000145B8
		public OrionDiscoveryJobSchedulerEventsService(CoreBusinessLayerPlugin parent, IOneTimeAgentDiscoveryJobFactory oneTimeAgentJobFactory) : this(parent, oneTimeAgentJobFactory, new AgentInfoDAL())
		{
		}

		// Token: 0x06000383 RID: 899 RVA: 0x000163C8 File Offset: 0x000145C8
		public OrionDiscoveryJobSchedulerEventsService(CoreBusinessLayerPlugin parent, IOneTimeAgentDiscoveryJobFactory oneTimeAgentJobFactory, IAgentInfoDAL agentInfoDal) : base(parent)
		{
			this.oneTimeAgentJobFactory = oneTimeAgentJobFactory;
			this.agentInfoDal = agentInfoDal;
			this.resultsManager.HandleResultsOfCancelledJobs = true;
			this.PrimaryLocale = OrionDiscoveryJobSchedulerEventsService.GetPrimaryLocale();
			this.partialResultsContainer = new PartialDiscoveryResultsContainer();
			this.partialResultsContainer.DiscoveryResultsComplete += this.partialResultsContainer_DiscoveryResultsComplete;
			this.partialResultsContainer.ClearStore();
			this.JobSchedulerHelperFactory = (() => JobScheduler.GetLocalInstance());
		}

		// Token: 0x06000384 RID: 900 RVA: 0x00016484 File Offset: 0x00014684
		private void partialResultsContainer_DiscoveryResultsComplete(object sender, DiscoveryResultsCompletedEventArgs e)
		{
			this.ProcessMergedPartialResults(e.CompleteResult, e.OrderedPlugins, e.ScheduledJobId, e.JobState, e.ProfileId);
		}

		// Token: 0x06000385 RID: 901 RVA: 0x000164AC File Offset: 0x000146AC
		protected override void ProcessJobProgress(JobProgress jobProgress)
		{
			Thread.CurrentThread.CurrentUICulture = (this.PrimaryLocale ?? Thread.CurrentThread.CurrentUICulture);
			if (!string.IsNullOrEmpty(jobProgress.Progress))
			{
				try
				{
					OrionDiscoveryJobProgressInfo orionDiscoveryJobProgressInfo = SerializationHelper.FromXmlString<OrionDiscoveryJobProgressInfo>(jobProgress.Progress);
					orionDiscoveryJobProgressInfo.JobId = jobProgress.JobId;
					StringBuilder stringBuilder = new StringBuilder();
					foreach (KeyValuePair<string, int> keyValuePair in orionDiscoveryJobProgressInfo.DiscoveredNetObjects)
					{
						stringBuilder.AppendFormat(" {0} {1};", keyValuePair.Value, keyValuePair.Key);
					}
					if (orionDiscoveryJobProgressInfo.ProfileID != null)
					{
						bool flag = OrionDiscoveryJobSchedulerEventsService.UpdateProgress(orionDiscoveryJobProgressInfo) != null;
						JobSchedulerEventServicev2.log.DebugFormat("Got Discovery progress for profile {0} Status: {1} Discovered: {2} ", orionDiscoveryJobProgressInfo.ProfileID, orionDiscoveryJobProgressInfo.Status.Status, stringBuilder);
						if (!flag)
						{
							JobSchedulerEventServicev2.log.DebugFormat("First progress of discovery profile {0}", orionDiscoveryJobProgressInfo.ProfileID.Value);
							DiscoveryProfileEntry profileByID = DiscoveryProfileEntry.GetProfileByID(orionDiscoveryJobProgressInfo.ProfileID.Value);
							if (profileByID != null)
							{
								profileByID.Status = new DiscoveryComplexStatus(1, string.Empty);
								profileByID.LastRun = DateTime.Now.ToUniversalTime();
								profileByID.Update();
							}
						}
					}
					else
					{
						JobSchedulerEventServicev2.log.DebugFormat("Recieved progress for one shot discovery job [{0}] Discovered: {1} ", jobProgress.JobId, stringBuilder);
						DiscoveryResultItem discoveryResultItem;
						if (DiscoveryResultCache.Instance.TryGetResultItem(jobProgress.JobId, ref discoveryResultItem))
						{
							discoveryResultItem.Progress = orionDiscoveryJobProgressInfo;
						}
						else
						{
							JobSchedulerEventServicev2.log.ErrorFormat("Unable to get result item {0}", jobProgress.JobId);
						}
					}
					return;
				}
				catch (Exception ex)
				{
					JobSchedulerEventServicev2.log.Error("Exception occured when parsing job progress info.", ex);
					return;
				}
			}
			JobSchedulerEventServicev2.log.Error("Job progress not found");
		}

		// Token: 0x06000386 RID: 902 RVA: 0x000166C0 File Offset: 0x000148C0
		protected override void ProcessJobFailure(FinishedJobInfo jobResult)
		{
			Thread.CurrentThread.CurrentUICulture = (this.PrimaryLocale ?? Thread.CurrentThread.CurrentUICulture);
			if (this.partialResultsContainer.IsResultExpected(jobResult.ScheduledJobId))
			{
				JobSchedulerEventServicev2.log.WarnFormat("Partial agent discovery job {0} failed with error '{1}'. It will be removed from discovery results.", jobResult.ScheduledJobId, jobResult.Result.Error);
				this.partialResultsContainer.RemoveExpectedPartialResult(jobResult.ScheduledJobId);
				return;
			}
			int num;
			if (int.TryParse(jobResult.State, out num))
			{
				string text = string.Format("A Network Discovery job has failed to complete.\r\nState: {0}\r\nProfile id: {1}.\r\nThe Job Scheduler is reporting the following error:\r\n{2}", jobResult.Result.State, jobResult.State, jobResult.Result.Error);
				JobSchedulerEventServicev2.log.Error(text);
				OrionDiscoveryJobSchedulerEventsService.RemoveProgressInfo(num);
				try
				{
					DiscoveryProfileEntry profile = this.GetProfile(num, jobResult.ScheduledJobId);
					if (profile != null)
					{
						if (!profile.IsScheduled)
						{
							profile.JobID = Guid.Empty;
						}
						profile.RuntimeInSeconds = 0;
						profile.Status = new DiscoveryComplexStatus(3, string.Format(Resources.LIBCODE_AK0_8, jobResult.Result.State, jobResult.State, jobResult.Result.Error));
						profile.Update();
						if (profile.IsHidden)
						{
							this.discoveryLogic.DeleteOrionDiscoveryProfile(profile.ProfileID);
						}
					}
				}
				catch (Exception ex)
				{
					JobSchedulerEventServicev2.log.Error(string.Format("Unable to update profile {0}", num), ex);
				}
			}
		}

		// Token: 0x06000387 RID: 903 RVA: 0x0001683C File Offset: 0x00014A3C
		protected override void ProcessJobResult(FinishedJobInfo jobResult)
		{
			Thread.CurrentThread.CurrentUICulture = (this.PrimaryLocale ?? Thread.CurrentThread.CurrentUICulture);
			JobSchedulerEventServicev2.log.DebugFormat("Recieved discovery results", Array.Empty<object>());
			if (jobResult.Result.State == 5)
			{
				JobSchedulerEventServicev2.log.Error("Job failed");
				return;
			}
			if (jobResult.Result.State != 6 && jobResult.Result.State != 4)
			{
				return;
			}
			int? num = null;
			using (IJobSchedulerHelper jobSchedulerHelper = this.JobSchedulerHelperFactory())
			{
				try
				{
					Stream resultStream = this.GetResultStream(jobResult, jobSchedulerHelper);
					List<DiscoveryPluginInfo> discoveryPluginInfos = DiscoveryPluginFactory.GetDiscoveryPluginInfos();
					List<IDiscoveryPlugin> list = new List<IDiscoveryPlugin>(new DiscoveryPluginFactory().GetPlugins(discoveryPluginInfos));
					List<Type> knownTypes = new List<Type>();
					Action<Type> <>9__1;
					list.ForEach(delegate(IDiscoveryPlugin plugin)
					{
						List<Type> knownTypes = plugin.GetKnownTypes();
						Action<Type> action;
						if ((action = <>9__1) == null)
						{
							action = (<>9__1 = delegate(Type t)
							{
								knownTypes.Add(t);
							});
						}
						knownTypes.ForEach(action);
					});
					SortedDictionary<int, List<IDiscoveryPlugin>> orderedPlugins = DiscoveryPluginHelper.GetOrderedPlugins(list, discoveryPluginInfos);
					List<IDiscoveryPlugin> orderedPluginList = DiscoveryPluginHelper.GetOrderedPluginList(orderedPlugins);
					if (resultStream != null && resultStream.Length > 0L)
					{
						int num2 = resultStream.ReadByte();
						resultStream.Position = 0L;
						OrionDiscoveryJobResult orionDiscoveryJobResult;
						if (num2 == 123 || num2 == 91)
						{
							orionDiscoveryJobResult = SerializationHelper.Deserialize<OrionDiscoveryJobResult>(resultStream);
						}
						else
						{
							orionDiscoveryJobResult = SerializationHelper.FromXmlStream<OrionDiscoveryJobResult>(resultStream, knownTypes);
						}
						if (orionDiscoveryJobResult.ProfileId != null)
						{
							num = new int?(orionDiscoveryJobResult.ProfileId.Value);
						}
						if (OrionDiscoveryJobSchedulerEventsService.resultLog.IsDebugEnabled)
						{
							if (resultStream.CanSeek)
							{
								resultStream.Seek(0L, SeekOrigin.Begin);
							}
							OrionDiscoveryJobSchedulerEventsService.resultLog.DebugFormat("Discovery Job {0} Result for ProfileID = {1}:", jobResult.ScheduledJobId, num ?? -1);
							OrionDiscoveryJobSchedulerEventsService.resultLog.Debug(new StreamReader(resultStream, Encoding.UTF8).ReadToEnd());
						}
						if (orionDiscoveryJobResult.DiscoverAgentNodes)
						{
							this.PersistResultsAndDiscoverAgentNodes(orionDiscoveryJobResult, orderedPlugins, jobResult.ScheduledJobId, jobResult.Result.State, orionDiscoveryJobResult.AgentsFilterQuery);
						}
						else
						{
							this.ProcessDiscoveryJobResult(orionDiscoveryJobResult, orderedPlugins, jobResult.ScheduledJobId, jobResult.Result.State);
						}
					}
					else
					{
						JobSchedulerEventServicev2.log.Error("Job result is empty, job was killed before it was able to report results.");
						this.UpdateTimeoutedProfile(jobResult.ScheduledJobId, orderedPluginList);
					}
					if (num != null)
					{
						JobSchedulerEventServicev2.log.DebugFormat("Processing of discovery results for profile {0} completed", num);
					}
					else
					{
						JobSchedulerEventServicev2.log.DebugFormat("Processing of discovery results for one time job {0} completed", jobResult.ScheduledJobId);
					}
				}
				catch (Exception ex)
				{
					JobSchedulerEventServicev2.log.Error("Exception occured when parsing job result.", ex);
					try
					{
						if (num != null)
						{
							JobSchedulerEventServicev2.log.DebugFormat("Updating discovery prfile with ID {0}", num);
							DiscoveryProfileEntry profile = this.GetProfile(num.Value, jobResult.ScheduledJobId);
							if (profile != null)
							{
								profile.Status = new DiscoveryComplexStatus(3, "Parsing of discovery result failed.");
								profile.Update();
							}
						}
					}
					catch (Exception ex2)
					{
						JobSchedulerEventServicev2.log.Error(string.Format("Exception updating discovery profile {0}", num), ex2);
					}
				}
				finally
				{
					jobSchedulerHelper.DeleteJobResult(jobResult.Result.JobId);
				}
			}
		}

		// Token: 0x06000388 RID: 904 RVA: 0x00016BAC File Offset: 0x00014DAC
		private Stream GetResultStream(FinishedJobInfo jobResult, IJobSchedulerHelper scheduler)
		{
			Stream stream = null;
			if (jobResult.Result.IsResultStreamed)
			{
				using (Stream jobResultStream = scheduler.GetJobResultStream(jobResult.Result.JobId, "SolarWinds.Orion.Discovery.Job.Results"))
				{
					stream = new DynamicStream();
					jobResultStream.CopyTo(stream);
					stream.Position = 0L;
					return stream;
				}
			}
			if (jobResult.Result.Output != null)
			{
				stream = new MemoryStream(jobResult.Result.Output);
			}
			return stream;
		}

		// Token: 0x06000389 RID: 905 RVA: 0x00016C30 File Offset: 0x00014E30
		private void PersistResultsAndDiscoverAgentNodes(OrionDiscoveryJobResult result, SortedDictionary<int, List<IDiscoveryPlugin>> orderedPlugins, Guid scheduledJobId, JobState state, string agentsFilterQuery)
		{
			JobSchedulerEventServicev2.log.DebugFormat("Received discovery job result {0} that requests agent nodes discovery. Persisting partial result and scheduling agent discovery jobs.", scheduledJobId);
			this.partialResultsContainer.CreatePartialResult(scheduledJobId, result, orderedPlugins, state);
			List<AgentInfo> list = this.agentInfoDal.GetAgentsByNodesFilter(result.EngineId, result.AgentsFilterQuery).ToList<AgentInfo>();
			if (result.AgentsAddresses.Count > 0)
			{
				list = (from x in list
				where result.AgentsAddresses.Contains(x.AgentGuid.ToString(), StringComparer.InvariantCultureIgnoreCase)
				select x).ToList<AgentInfo>();
			}
			List<Task> list2 = new List<Task>(list.Count);
			Action<object> <>9__1;
			foreach (AgentInfo agentInfo in list)
			{
				List<Task> list3 = list2;
				TaskFactory factory = Task.Factory;
				Action<object> action;
				if ((action = <>9__1) == null)
				{
					action = (<>9__1 = delegate(object data)
					{
						this.ScheduleAgentDiscoveryJob((OrionDiscoveryJobSchedulerEventsService.AgentDiscoveryJobSchedulingData)data);
					});
				}
				list3.Add(factory.StartNew(action, new OrionDiscoveryJobSchedulerEventsService.AgentDiscoveryJobSchedulingData(scheduledJobId, result.EngineId, result.ProfileId, agentInfo)));
			}
			Task.WaitAll(list2.ToArray());
			this.partialResultsContainer.AllExpectedResultsRegistered(scheduledJobId);
		}

		// Token: 0x0600038A RID: 906 RVA: 0x00016D7C File Offset: 0x00014F7C
		private void ScheduleAgentDiscoveryJob(OrionDiscoveryJobSchedulerEventsService.AgentDiscoveryJobSchedulingData data)
		{
			try
			{
				if (data == null)
				{
					throw new ArgumentNullException("data");
				}
				if (data.AgentInfo.NodeID == null)
				{
					throw new ArgumentException("AgentInfo does not contain valid NodeID. Discovery job will not be scheduled.");
				}
				Guid scheduledJobId = this.oneTimeAgentJobFactory.CreateOneTimeAgentDiscoveryJob(data.AgentInfo.NodeID.Value, data.EngineId, data.ProfileId, new List<Credential>());
				this.partialResultsContainer.ExpectPartialResult(data.MainJobId, scheduledJobId, this.agentPartialResultsTimeout);
			}
			catch (Exception ex)
			{
				JobSchedulerEventServicev2.log.WarnFormat("Can't create one-time discovery job for agent {0}. Agent is probably not accessible. {1}", (data != null) ? data.AgentInfo.AgentGuid.ToString() : "unknown", ex);
			}
		}

		// Token: 0x0600038B RID: 907 RVA: 0x00016E48 File Offset: 0x00015048
		private void ProcessMergedPartialResults(OrionDiscoveryJobResult result, SortedDictionary<int, List<IDiscoveryPlugin>> orderedPlugins, Guid scheduledJobId, JobState jobState, int? profileId)
		{
			JobSchedulerEventServicev2.log.DebugFormat("Received all partial results for discovery job {0}. Triggering result processing.", scheduledJobId);
			try
			{
				this.ProcessDiscoveryJobResult(result, orderedPlugins, scheduledJobId, jobState);
			}
			catch (Exception ex)
			{
				JobSchedulerEventServicev2.log.Error("Exception occured when parsing job result.", ex);
				try
				{
					if (profileId != null)
					{
						JobSchedulerEventServicev2.log.DebugFormat("Updating discovery profile with ID {0}", profileId);
						DiscoveryProfileEntry profile = this.GetProfile(profileId.Value, scheduledJobId);
						if (profile != null)
						{
							profile.Status = new DiscoveryComplexStatus(3, "Parsing of discovery result failed.");
							profile.Update();
						}
					}
				}
				catch (Exception ex2)
				{
					JobSchedulerEventServicev2.log.Error(string.Format("Exception updating discovery profile {0}", profileId), ex2);
				}
			}
		}

		// Token: 0x0600038C RID: 908 RVA: 0x00016F10 File Offset: 0x00015110
		private bool ResultForPluginIsContained(IDiscoveryPlugin plugin, OrionDiscoveryJobResult result)
		{
			bool result2;
			try
			{
				string pluginTypeName = plugin.GetType().FullName;
				result2 = result.PluginResults.Any((DiscoveryPluginResultBase pluginRes) => pluginTypeName.Equals(pluginRes.PluginTypeName, StringComparison.OrdinalIgnoreCase));
			}
			catch (Exception ex)
			{
				JobSchedulerEventServicev2.log.Error(ex);
				result2 = false;
			}
			return result2;
		}

		// Token: 0x0600038D RID: 909 RVA: 0x00016F70 File Offset: 0x00015170
		private void ProcessPluginsWithInterface<T>(string actionName, OrionDiscoveryJobResult result, SortedDictionary<int, List<IDiscoveryPlugin>> orderedPlugins, Action<T> processAction)
		{
			if (result == null || result.PluginResults == null)
			{
				JobSchedulerEventServicev2.log.Error("Empty discovery result received. Nothing to process.");
				return;
			}
			result.PluginResults.RemoveAll(delegate(DiscoveryPluginResultBase pluginResult)
			{
				bool result2;
				try
				{
					pluginResult.GetDiscoveredObjects();
					result2 = false;
				}
				catch (Exception ex2)
				{
					JobSchedulerEventServicev2.log.ErrorFormat("Failed to get discovered objects from plugin result {0} ({1}), result will be discarded: {2}", pluginResult.GetType().Name, pluginResult.PluginTypeName, ex2);
					result2 = true;
				}
				return result2;
			});
			JobSchedulerEventServicev2.log.DebugFormat("Result processing [{0}] - Start", actionName);
			foreach (int num in orderedPlugins.Keys)
			{
				JobSchedulerEventServicev2.log.DebugFormat("Processing level {0} plugins", num);
				foreach (IDiscoveryPlugin discoveryPlugin in orderedPlugins[num])
				{
					string text = discoveryPlugin.GetType().AssemblyQualifiedName.Split(new char[]
					{
						','
					}).First<string>();
					if (discoveryPlugin is T)
					{
						JobSchedulerEventServicev2.log.DebugFormat("Plugin {0} is of type {1}", discoveryPlugin, typeof(T));
						if (this.ResultForPluginIsContained(discoveryPlugin, result))
						{
							try
							{
								JobSchedulerEventServicev2.log.DebugFormat("Processing {0}", text);
								processAction((T)((object)discoveryPlugin));
								continue;
							}
							catch (Exception ex)
							{
								JobSchedulerEventServicev2.log.Error(string.Format("Processing of discovery result for profile {0} failed for plugin {1}", result.ProfileId, discoveryPlugin.GetType()), ex);
								continue;
							}
						}
						JobSchedulerEventServicev2.log.WarnFormat("Result for plugin {0} doesnt exist.", text);
					}
					else
					{
						JobSchedulerEventServicev2.log.DebugFormat("Plugin {0} is not of type {1}", discoveryPlugin, typeof(T));
					}
				}
			}
			JobSchedulerEventServicev2.log.DebugFormat("Result processing [{0}] - End", actionName);
		}

		// Token: 0x0600038E RID: 910 RVA: 0x00017178 File Offset: 0x00015378
		internal void ProcessDiscoveryJobResult(OrionDiscoveryJobResult result, SortedDictionary<int, List<IDiscoveryPlugin>> orderedPlugins, Guid jobId, JobState jobState)
		{
			if (result == null)
			{
				throw new ArgumentNullException("result");
			}
			JobSchedulerEventServicev2.log.DebugFormat("Processing discovery result for profile {0}", result.ProfileId);
			if (this.partialResultsContainer.IsResultExpected(jobId))
			{
				this.partialResultsContainer.AddExpectedPartialResult(jobId, result);
				return;
			}
			if (result.ProfileId != null && result.IsFromAgent)
			{
				JobSchedulerEventServicev2.log.DebugFormat("Received job result from Agent discovery job {0} that is no longer expected. Discarding.", jobId);
				return;
			}
			this.ProcessPluginsWithInterface<IBussinessLayerPostProcessing>("ProcessDiscoveryResult", result, orderedPlugins, delegate(IBussinessLayerPostProcessing p)
			{
				p.ProcessDiscoveryResult(result);
			});
			if (result.ProfileId == null)
			{
				this.ImportOneShotDiscovery(result, orderedPlugins, jobId, jobState);
				return;
			}
			this.ImportProfileResults(result, orderedPlugins, jobId, jobState);
		}

		// Token: 0x0600038F RID: 911 RVA: 0x00017274 File Offset: 0x00015474
		private void ImportOneShotDiscovery(OrionDiscoveryJobResult result, SortedDictionary<int, List<IDiscoveryPlugin>> orderedPlugins, Guid jobId, JobState jobState)
		{
			OrionDiscoveryJobSchedulerEventsService.<>c__DisplayClass23_0 CS$<>8__locals1 = new OrionDiscoveryJobSchedulerEventsService.<>c__DisplayClass23_0();
			if (!DiscoveryResultCache.Instance.TryGetResultItem(jobId, ref CS$<>8__locals1.resultItem))
			{
				JobSchedulerEventServicev2.log.ErrorFormat("Unable to find resultItem for job {0} in cache", jobId);
				return;
			}
			List<IDiscoveredObjectGroup> groups = new List<IDiscoveredObjectGroup>();
			List<ISelectionType> selectionTypes = new List<ISelectionType>();
			this.ProcessPluginsWithInterface<IOneTimeJobSupport>("GetDiscoveredObjectGroups, GetSelectionTypes", result, orderedPlugins, delegate(IOneTimeJobSupport p)
			{
				groups.AddRange(p.GetDiscoveredObjectGroups());
				selectionTypes.AddRange(p.GetSelectionTypes());
			});
			TechnologyManager instance = TechnologyManager.Instance;
			groups.AddRange(DiscoveryFilterResultByTechnology.GetDiscoveryGroups(instance));
			DiscoveredObjectTree resultTree = new DiscoveredObjectTree(result, groups, selectionTypes);
			foreach (DiscoveredVolume discoveredVolume in resultTree.GetAllTreeObjectsOfType<DiscoveredVolume>())
			{
				if (discoveredVolume.VolumeType == 5 || discoveredVolume.VolumeType == 7 || discoveredVolume.VolumeType == 6 || discoveredVolume.VolumeType == null)
				{
					discoveredVolume.IsSelected = false;
				}
			}
			if (CS$<>8__locals1.resultItem.nodeId == null)
			{
				this.ProcessPluginsWithInterface<IDefaultTreeState>("SetTreeDefault", result, orderedPlugins, delegate(IDefaultTreeState p)
				{
					p.SetTreeDefaultState(resultTree);
				});
				DiscoveryFilterResultByTechnology.FilterByPriority(result, instance);
			}
			else
			{
				this.ProcessPluginsWithInterface<IOneTimeJobSupport>("GetDiscoveredResourcesManagedStatus", result, orderedPlugins, delegate(IOneTimeJobSupport p)
				{
					p.GetDiscoveredResourcesManagedStatus(resultTree, CS$<>8__locals1.resultItem.nodeId.Value);
				});
				DiscoveryFilterResultByTechnology.FilterMandatoryByPriority(result, instance);
			}
			CS$<>8__locals1.resultItem.ResultTree = resultTree;
			this.UpdateOneTimeJobResultProgress(CS$<>8__locals1.resultItem);
		}

		// Token: 0x06000390 RID: 912 RVA: 0x00017410 File Offset: 0x00015610
		private void UpdateOneTimeJobResultProgress(DiscoveryResultItem item)
		{
			if (item.ResultTree == null)
			{
				return;
			}
			if (item.Progress == null)
			{
				item.Progress = new OrionDiscoveryJobProgressInfo
				{
					JobId = item.JobId
				};
			}
			item.Progress.Status = new DiscoveryComplexStatus(8, "Ready for import", 1);
		}

		// Token: 0x06000391 RID: 913 RVA: 0x0001745C File Offset: 0x0001565C
		private void ImportProfileResults(OrionDiscoveryJobResult result, SortedDictionary<int, List<IDiscoveryPlugin>> orderedPlugins, Guid jobId, JobState jobState)
		{
			this.ProcessPluginsWithInterface<IDiscoveryPlugin>("StoreDiscoveryResult", result, orderedPlugins, delegate(IDiscoveryPlugin p)
			{
				p.StoreDiscoveryResult(result);
			});
			JobSchedulerEventServicev2.log.DebugFormat("Updating information about ignored items stored in profile {0}", result.ProfileId.Value);
			DiscoveryIgnoredDAL.UpdateIgnoreInformationForProfile(result.ProfileId.Value);
			bool flag = true;
			DiscoveryLogs discoveryLog = new DiscoveryLogs();
			DiscoveryProfileEntry discoveryProfileEntry = null;
			try
			{
				JobSchedulerEventServicev2.log.DebugFormat("Updating discovery profile with ID {0}", result.ProfileId.Value);
				discoveryProfileEntry = this.GetProfile(result.ProfileId.Value, jobId);
				if (discoveryProfileEntry != null)
				{
					if (!discoveryProfileEntry.IsScheduled)
					{
						discoveryProfileEntry.JobID = Guid.Empty;
					}
					DateTime utcNow = DateTime.UtcNow;
					discoveryLog.FinishedTimeStamp = utcNow.AddTicks(-(utcNow.Ticks % 10000000L));
					discoveryLog.ProfileID = discoveryProfileEntry.ProfileID;
					discoveryLog.AutoImport = discoveryProfileEntry.IsAutoImport;
					discoveryLog.Result = 0;
					discoveryLog.ResultDescription = Resources2.DiscoveryLogResult_DiscoveryFinished;
					discoveryLog.BatchID = new Guid?(Guid.NewGuid());
					discoveryProfileEntry.RuntimeInSeconds = (int)(DateTime.Now - discoveryProfileEntry.LastRun.ToLocalTime()).TotalSeconds;
					if (jobState == 4)
					{
						discoveryLog.Result = 1;
						discoveryLog.ResultDescription = Resources2.DiscoveryLogResult_DiscoveryFailed;
						discoveryLog.ErrorMessage = Resources2.DiscoveryLogError_Cancelled;
						this.UpdateCanceledProfileStatus(discoveryProfileEntry);
						discoveryProfileEntry.Update();
						OrionDiscoveryJobSchedulerEventsService.RemoveProgressInfo(discoveryProfileEntry.ProfileID);
					}
					else if (discoveryProfileEntry.IsAutoImport)
					{
						flag = false;
						discoveryProfileEntry.Status = new DiscoveryComplexStatus(discoveryProfileEntry.Status.Status, discoveryProfileEntry.Status.Description, 4);
						discoveryProfileEntry.Update();
						JobSchedulerEventServicev2.log.InfoFormat("Starting AutoImport of Profile:{0}", discoveryProfileEntry.ProfileID);
						bool isHidden = discoveryProfileEntry.IsHidden;
						this.discoveryLogic.ImportDiscoveryResultForProfile(discoveryProfileEntry.ProfileID, isHidden, delegate(DiscoveryResultBase _result, Guid importJobID, StartImportStatus StartImportStatus)
						{
							DiscoveryAutoImportNotificationItemDAL.Show(_result, StartImportStatus);
							this.ImportResultFinished(_result, importJobID, StartImportStatus);
							DiscoveryImportManager.FillDiscoveryLogEntity(discoveryLog, _result, StartImportStatus);
							JobSchedulerEventServicev2.log.InfoFormat("AutoImport of Profile:{0} finished with result:{1}", discoveryLog.ProfileID, discoveryLog.Result.ToString());
							try
							{
								using (CoreSwisContext coreSwisContext2 = SwisContextFactory.CreateSystemContext())
								{
									discoveryLog.Create(coreSwisContext2);
								}
								JobSchedulerEventServicev2.log.InfoFormat("DiscoveryLog created for ProfileID:{0}", discoveryLog.ProfileID);
							}
							catch (Exception ex2)
							{
								JobSchedulerEventServicev2.log.Error("Unable to create discovery import log", ex2);
							}
						}, true, new Guid?(jobId));
					}
					else
					{
						if (discoveryProfileEntry.IsScheduled)
						{
							discoveryProfileEntry.Status = new DiscoveryComplexStatus(5, string.Empty);
						}
						else
						{
							discoveryProfileEntry.Status = new DiscoveryComplexStatus(2, string.Empty);
						}
						OrionDiscoveryJobSchedulerEventsService.GenerateDiscoveryFinishedEvent(discoveryProfileEntry.EngineID, discoveryProfileEntry.Name);
						OrionDiscoveryJobSchedulerEventsService.RemoveProgressInfo(discoveryProfileEntry.ProfileID);
						discoveryProfileEntry.Update();
					}
				}
			}
			catch (Exception ex)
			{
				JobSchedulerEventServicev2.log.Error(string.Format("Unable to update profile {0}", result.ProfileId.Value), ex);
				if (discoveryProfileEntry != null)
				{
					OrionDiscoveryJobSchedulerEventsService.GenerateDiscoveryFailedEvent(discoveryProfileEntry.EngineID, discoveryProfileEntry.Name);
				}
				if (flag)
				{
					discoveryLog.Result = 1;
					discoveryLog.ResultDescription = Resources2.DiscoveryLogResult_DiscoveryFailed;
					discoveryLog.ErrorMessage = Resources2.DiscoveryLogError_SeeLog;
				}
			}
			finally
			{
				if (discoveryProfileEntry != null)
				{
					if (discoveryProfileEntry.Status.Status == 5)
					{
						DiscoveryNetObjectStatusManager.Instance.RequestUpdateForProfileAsync(discoveryProfileEntry.ProfileID, new Action(OrionDiscoveryJobSchedulerEventsService.FireScheduledDiscoveryNotification), TimeSpan.Zero);
					}
					else
					{
						DiscoveryNetObjectStatusManager.Instance.RequestUpdateForProfileAsync(discoveryProfileEntry.ProfileID, null, TimeSpan.Zero);
					}
				}
				if (flag)
				{
					using (CoreSwisContext coreSwisContext = SwisContextFactory.CreateSystemContext())
					{
						discoveryLog.Create(coreSwisContext);
					}
				}
			}
		}

		// Token: 0x06000392 RID: 914 RVA: 0x0001783C File Offset: 0x00015A3C
		private void ImportResultFinished(DiscoveryResultBase result, Guid importJobID, StartImportStatus status)
		{
			if (result == null)
			{
				throw new ArgumentNullException("result");
			}
			DiscoveryProfileEntry profileByID = DiscoveryProfileEntry.GetProfileByID(result.ProfileID);
			if (profileByID == null)
			{
				return;
			}
			if (status == StartImportStatus.Failed)
			{
				profileByID.Status = new DiscoveryComplexStatus(2, string.Empty);
				OrionDiscoveryJobSchedulerEventsService.GenerateImportFailedEvent(result.EngineId, profileByID.Name, status);
			}
			else if (status == StartImportStatus.LicenseExceeded)
			{
				profileByID.Status = new DiscoveryComplexStatus(8, string.Empty);
				OrionDiscoveryJobSchedulerEventsService.GenerateImportFailedEvent(result.EngineId, profileByID.Name, status);
			}
			else
			{
				if (profileByID.IsScheduled)
				{
					profileByID.Status = new DiscoveryComplexStatus(5, string.Empty);
				}
				else
				{
					profileByID.Status = new DiscoveryComplexStatus(2, string.Empty);
				}
				OrionDiscoveryJobSchedulerEventsService.GenerateImportFinishedEvent(result.EngineId, profileByID.Name);
			}
			profileByID.Update();
			OrionDiscoveryJobSchedulerEventsService.RemoveProgressInfo(profileByID.ProfileID);
		}

		// Token: 0x06000393 RID: 915 RVA: 0x00017908 File Offset: 0x00015B08
		private static void GenerateImportFinishedEvent(int engineId, string profileName)
		{
			EventsDAL.InsertEvent(0, 0, string.Empty, 70, string.Format(Resources.Discovery_Succeeded_Text_Run_Import, profileName), engineId);
		}

		// Token: 0x06000394 RID: 916 RVA: 0x00017924 File Offset: 0x00015B24
		private static void GenerateImportFailedEvent(int engineId, string profileName, StartImportStatus importStatus)
		{
			if (importStatus == StartImportStatus.LicenseExceeded)
			{
				EventsDAL.InsertEvent(0, 0, string.Empty, 71, string.Format(Resources.Discovery_Failed_Text_Import_License, profileName), engineId);
				return;
			}
			EventsDAL.InsertEvent(0, 0, string.Empty, 71, string.Format(Resources.Discovery_Failed_Text_Import, profileName), engineId);
		}

		// Token: 0x06000395 RID: 917 RVA: 0x0001795F File Offset: 0x00015B5F
		private static void GenerateDiscoveryFinishedEvent(int engineId, string profileName)
		{
			EventsDAL.InsertEvent(0, 0, string.Empty, 70, string.Format(Resources.Discovery_Succeeded_Text_Run, profileName), engineId);
		}

		// Token: 0x06000396 RID: 918 RVA: 0x0001797B File Offset: 0x00015B7B
		private static void GenerateDiscoveryFailedEvent(int engineId, string profileName)
		{
			EventsDAL.InsertEvent(0, 0, string.Empty, 71, string.Format(Resources.Discovery_Failed_Text_Run, profileName), engineId);
		}

		// Token: 0x06000397 RID: 919 RVA: 0x00017998 File Offset: 0x00015B98
		private static void FireScheduledDiscoveryNotification()
		{
			int countOfNodes = DiscoveryNodeEntry.GetCountOfNodes(56);
			JobSchedulerEventServicev2.log.DebugFormat("SD: New nodes found: {0}", countOfNodes);
			int countOfNodes2 = DiscoveryNodeEntry.GetCountOfNodes(42);
			JobSchedulerEventServicev2.log.DebugFormat("SD: Changed nodes found: {0}", countOfNodes2);
			string url = string.Format("/Orion/Discovery/Results/ScheduledDiscoveryResults.aspx?Status={0}", 58);
			ScheduledDiscoveryNotificationItemDAL.Create(OrionDiscoveryJobSchedulerEventsService.ComposeNotificationMessage(countOfNodes, countOfNodes2), url);
		}

		// Token: 0x06000398 RID: 920 RVA: 0x00017A00 File Offset: 0x00015C00
		private static string ComposeNotificationMessage(int newNodes, int changedNodes)
		{
			StringBuilder stringBuilder = new StringBuilder(Resources.LIBCODE_PCC_18);
			stringBuilder.Append(" ");
			if (newNodes == 1)
			{
				stringBuilder.Append(Resources.LIBCODE_PCC_19);
			}
			else if (newNodes >= 0)
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

		// Token: 0x06000399 RID: 921 RVA: 0x00017A8C File Offset: 0x00015C8C
		public void UpdateTimeoutedProfile(Guid jobId, List<IDiscoveryPlugin> orderedPlugins)
		{
			DiscoveryProfileEntry profile = (from p in DiscoveryProfileEntry.GetAllProfiles()
			where p.JobID == jobId
			select p).FirstOrDefault<DiscoveryProfileEntry>();
			if (profile == null)
			{
				JobSchedulerEventServicev2.log.ErrorFormat("Unable to find profile with job id {0}", jobId);
				return;
			}
			try
			{
				orderedPlugins.ForEach(delegate(IDiscoveryPlugin p)
				{
					if (p is IResultManagement)
					{
						((IResultManagement)p).DeleteResultsForProfile(profile.ProfileID);
					}
				});
				if (!profile.IsScheduled)
				{
					profile.JobID = Guid.Empty;
				}
				this.UpdateCanceledProfileStatus(profile);
				profile.Update();
				OrionDiscoveryJobSchedulerEventsService.RemoveProgressInfo(profile.ProfileID);
			}
			catch (Exception ex)
			{
				JobSchedulerEventServicev2.log.Error(string.Format("Unhandled exception occured when updating profile {0}", profile.ProfileID), ex);
			}
		}

		// Token: 0x0600039A RID: 922 RVA: 0x00017B7C File Offset: 0x00015D7C
		public static OrionDiscoveryJobProgressInfo GetProgressInfo(int profileId)
		{
			Dictionary<int, OrionDiscoveryJobProgressInfo> obj = OrionDiscoveryJobSchedulerEventsService.profileProgress;
			OrionDiscoveryJobProgressInfo result;
			lock (obj)
			{
				if (OrionDiscoveryJobSchedulerEventsService.profileProgress.ContainsKey(profileId))
				{
					OrionDiscoveryJobProgressInfo orionDiscoveryJobProgressInfo = OrionDiscoveryJobSchedulerEventsService.profileProgress[profileId];
					orionDiscoveryJobProgressInfo.ImportProgress = DiscoveryImportManager.GetImportProgress(orionDiscoveryJobProgressInfo.JobId);
					if (orionDiscoveryJobProgressInfo.ImportProgress != null)
					{
						orionDiscoveryJobProgressInfo.Status = new DiscoveryComplexStatus(1, string.Empty, 4);
					}
					result = orionDiscoveryJobProgressInfo;
				}
				else
				{
					result = null;
				}
			}
			return result;
		}

		// Token: 0x0600039B RID: 923 RVA: 0x00017C00 File Offset: 0x00015E00
		public static OrionDiscoveryJobProgressInfo UpdateProgress(OrionDiscoveryJobProgressInfo progress)
		{
			OrionDiscoveryJobProgressInfo orionDiscoveryJobProgressInfo = null;
			object syncRoot = ((ICollection)OrionDiscoveryJobSchedulerEventsService.profileProgress).SyncRoot;
			lock (syncRoot)
			{
				OrionDiscoveryJobSchedulerEventsService.profileProgress.TryGetValue(progress.ProfileID.Value, out orionDiscoveryJobProgressInfo);
				if (orionDiscoveryJobProgressInfo == null)
				{
					OrionDiscoveryJobSchedulerEventsService.profileProgress[progress.ProfileID.Value] = progress;
				}
				else if (!orionDiscoveryJobProgressInfo.CanceledByUser)
				{
					orionDiscoveryJobProgressInfo.MergeWithNewProgress(progress);
				}
			}
			return orionDiscoveryJobProgressInfo;
		}

		// Token: 0x0600039C RID: 924 RVA: 0x00017C8C File Offset: 0x00015E8C
		public static void RemoveProgressInfo(int profileID)
		{
			object syncRoot = ((ICollection)OrionDiscoveryJobSchedulerEventsService.profileProgress).SyncRoot;
			lock (syncRoot)
			{
				OrionDiscoveryJobSchedulerEventsService.profileProgress.Remove(profileID);
			}
		}

		// Token: 0x0600039D RID: 925 RVA: 0x00017CD8 File Offset: 0x00015ED8
		private DiscoveryProfileEntry GetProfile(int profileID, Guid scheduledJobID)
		{
			JobSchedulerEventServicev2.log.DebugFormat("Loading info for profile {0}.", profileID);
			DiscoveryProfileEntry discoveryProfileEntry = DiscoveryProfileEntry.GetProfileByID(profileID);
			if (discoveryProfileEntry == null)
			{
				JobSchedulerEventServicev2.log.ErrorFormat("Profile: {0} doesn't exists. Deleting job ID: {1}", profileID, scheduledJobID);
			}
			else if (discoveryProfileEntry.JobID != scheduledJobID)
			{
				JobSchedulerEventServicev2.log.ErrorFormat("Profile: {0} exists but has different JobId: {1}. Deleting job ID: {2}", profileID, discoveryProfileEntry.JobID, scheduledJobID);
				discoveryProfileEntry = null;
			}
			if (discoveryProfileEntry == null)
			{
				if (!new OrionDiscoveryJobFactory().DeleteJob(scheduledJobID))
				{
					JobSchedulerEventServicev2.log.Error("Error when deleting job: " + scheduledJobID);
				}
				JobSchedulerEventServicev2.log.ErrorFormat("Job ID: {0} for ProfileID: {1} was deleted.", scheduledJobID, profileID);
			}
			else
			{
				JobSchedulerEventServicev2.log.DebugFormat("Profile info for profile {0} loaded.", profileID);
			}
			return discoveryProfileEntry;
		}

		// Token: 0x0600039E RID: 926 RVA: 0x00017DB4 File Offset: 0x00015FB4
		internal static void CancelDiscoveryJob(int profileID)
		{
			DiscoveryProfileEntry profileByID = DiscoveryProfileEntry.GetProfileByID(profileID);
			Dictionary<int, OrionDiscoveryJobProgressInfo> obj = OrionDiscoveryJobSchedulerEventsService.profileProgress;
			lock (obj)
			{
				OrionDiscoveryJobProgressInfo orionDiscoveryJobProgressInfo;
				if (!OrionDiscoveryJobSchedulerEventsService.profileProgress.TryGetValue(profileID, out orionDiscoveryJobProgressInfo))
				{
					orionDiscoveryJobProgressInfo = new OrionDiscoveryJobProgressInfo();
					orionDiscoveryJobProgressInfo.Status = profileByID.Status;
					OrionDiscoveryJobSchedulerEventsService.profileProgress.Add(profileID, orionDiscoveryJobProgressInfo);
				}
				orionDiscoveryJobProgressInfo.CanceledByUser = true;
			}
		}

		// Token: 0x0600039F RID: 927 RVA: 0x00017E28 File Offset: 0x00016028
		private void UpdateCanceledProfileStatus(DiscoveryProfileEntry profile)
		{
			if (profile.Status.Status == 7)
			{
				profile.Status = new DiscoveryComplexStatus(6, "WEBDATA_TP0_DISCOVERY_CANCELLED_BY_USER");
				return;
			}
			profile.Status = new DiscoveryComplexStatus(6, "WEBDATA_TP0_DISCOVERY_INTERRUPTED_BY_TIMEOUT");
		}

		// Token: 0x040000BA RID: 186
		protected static readonly Log resultLog = new Log("DiscoveryResultLog");

		// Token: 0x040000BB RID: 187
		private readonly IOneTimeAgentDiscoveryJobFactory oneTimeAgentJobFactory;

		// Token: 0x040000BC RID: 188
		private readonly IAgentInfoDAL agentInfoDal;

		// Token: 0x040000BD RID: 189
		private readonly PartialDiscoveryResultsContainer partialResultsContainer;

		// Token: 0x040000BE RID: 190
		private readonly TimeSpan agentPartialResultsTimeout = BusinessLayerSettings.Instance.AgentDiscoveryJobTimeout.Add(TimeSpan.FromMinutes(1.0));

		// Token: 0x040000BF RID: 191
		private static Dictionary<int, OrionDiscoveryJobProgressInfo> profileProgress = new Dictionary<int, OrionDiscoveryJobProgressInfo>();

		// Token: 0x040000C0 RID: 192
		private readonly DiscoveryLogic discoveryLogic = new DiscoveryLogic();

		// Token: 0x040000C1 RID: 193
		private readonly CultureInfo PrimaryLocale;

		// Token: 0x040000C2 RID: 194
		internal Func<IJobSchedulerHelper> JobSchedulerHelperFactory;

		// Token: 0x0200012C RID: 300
		private class AgentDiscoveryJobSchedulingData
		{
			// Token: 0x06000ADC RID: 2780 RVA: 0x000480FD File Offset: 0x000462FD
			public AgentDiscoveryJobSchedulingData(Guid mainJobId, int engineId, int? profileId, AgentInfo agentInfo)
			{
				this.MainJobId = mainJobId;
				this.EngineId = engineId;
				this.ProfileId = profileId;
				this.AgentInfo = agentInfo;
			}

			// Token: 0x17000135 RID: 309
			// (get) Token: 0x06000ADD RID: 2781 RVA: 0x00048122 File Offset: 0x00046322
			// (set) Token: 0x06000ADE RID: 2782 RVA: 0x0004812A File Offset: 0x0004632A
			public Guid MainJobId { get; private set; }

			// Token: 0x17000136 RID: 310
			// (get) Token: 0x06000ADF RID: 2783 RVA: 0x00048133 File Offset: 0x00046333
			// (set) Token: 0x06000AE0 RID: 2784 RVA: 0x0004813B File Offset: 0x0004633B
			public int EngineId { get; private set; }

			// Token: 0x17000137 RID: 311
			// (get) Token: 0x06000AE1 RID: 2785 RVA: 0x00048144 File Offset: 0x00046344
			// (set) Token: 0x06000AE2 RID: 2786 RVA: 0x0004814C File Offset: 0x0004634C
			public int? ProfileId { get; set; }

			// Token: 0x17000138 RID: 312
			// (get) Token: 0x06000AE3 RID: 2787 RVA: 0x00048155 File Offset: 0x00046355
			// (set) Token: 0x06000AE4 RID: 2788 RVA: 0x0004815D File Offset: 0x0004635D
			public AgentInfo AgentInfo { get; private set; }
		}
	}
}
