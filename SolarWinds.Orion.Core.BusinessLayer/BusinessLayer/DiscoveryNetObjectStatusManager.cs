using System;
using System.Collections.Generic;
using System.Linq;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common.i18n;
using SolarWinds.Orion.Core.Discovery;
using SolarWinds.Orion.Core.Discovery.DAL;
using SolarWinds.Orion.Core.Models.Discovery;
using SolarWinds.Orion.Discovery.Contract.DiscoveryPlugin;
using SolarWinds.Orion.Discovery.Framework.Interfaces;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200001A RID: 26
	internal class DiscoveryNetObjectStatusManager : IDiscoveryNetObjectStatusManager
	{
		// Token: 0x1700005E RID: 94
		// (get) Token: 0x060002B4 RID: 692 RVA: 0x00010E42 File Offset: 0x0000F042
		// (set) Token: 0x060002B5 RID: 693 RVA: 0x00010E49 File Offset: 0x0000F049
		public static IDiscoveryNetObjectStatusManager Instance
		{
			get
			{
				return DiscoveryNetObjectStatusManager.instance;
			}
			internal set
			{
				DiscoveryNetObjectStatusManager.instance = value;
			}
		}

		// Token: 0x060002B6 RID: 694 RVA: 0x00010E51 File Offset: 0x0000F051
		internal DiscoveryNetObjectStatusManager(DiscoveryNetObjectStatusManager.Scheduler scheduler)
		{
			this.scheduler = scheduler;
		}

		// Token: 0x060002B7 RID: 695 RVA: 0x00010E81 File Offset: 0x0000F081
		internal DiscoveryNetObjectStatusManager() : this(new DiscoveryNetObjectStatusManager.Scheduler(new Action<int>(DiscoveryNetObjectStatusManager.UpdateRoutine), 5, TimeSpan.FromSeconds(10.0), TimeSpan.FromMilliseconds(100.0)))
		{
		}

		// Token: 0x060002B8 RID: 696 RVA: 0x00010EB7 File Offset: 0x0000F0B7
		public void BeginOrionDatabaseChanges()
		{
			this.scheduler.BeginChanges();
		}

		// Token: 0x060002B9 RID: 697 RVA: 0x00010EC4 File Offset: 0x0000F0C4
		public void EndOrionDatabaseChanges()
		{
			this.scheduler.EndChanges();
		}

		// Token: 0x060002BA RID: 698 RVA: 0x00010ED4 File Offset: 0x0000F0D4
		public void RequestUpdateAsync(Action updateFinishedCallback, TimeSpan waitForChangesDelay)
		{
			if (DiscoveryNetObjectStatusManager.log.IsDebugEnabled)
			{
				DiscoveryNetObjectStatusManager.log.DebugFormat("Global Status Update requested, waiting for changes for {0}", waitForChangesDelay);
			}
			List<int> allProfileIDs = this.discoveryDALProvider.GetAllProfileIDs();
			this.RequestUpdateInternal(allProfileIDs, updateFinishedCallback, waitForChangesDelay);
		}

		// Token: 0x060002BB RID: 699 RVA: 0x00010F17 File Offset: 0x0000F117
		public void RequestUpdateForProfileAsync(int profileID, Action updateFinishedCallback, TimeSpan waitForChangesDelay)
		{
			if (DiscoveryNetObjectStatusManager.log.IsDebugEnabled)
			{
				DiscoveryNetObjectStatusManager.log.DebugFormat("Status Update for single discovery result requested, waiting for changes for {0}", waitForChangesDelay);
			}
			this.RequestUpdateInternal(new List<int>
			{
				profileID
			}, updateFinishedCallback, waitForChangesDelay);
		}

		// Token: 0x060002BC RID: 700 RVA: 0x00010F50 File Offset: 0x0000F150
		private void RequestUpdateInternal(List<int> profileIDs, Action updateFinishedCallback, TimeSpan waitForChangesDelay)
		{
			UpdateTaskScheduler<int, Guid>.ScheduledTaskCallback callback = null;
			if (updateFinishedCallback != null)
			{
				Guid guid = Guid.NewGuid();
				object obj = this.syncLock;
				lock (obj)
				{
					this.awaitingCallbacks.Add(guid, new DiscoveryNetObjectStatusManager.CallbackInfo(updateFinishedCallback, profileIDs.Count));
				}
				callback = new UpdateTaskScheduler<int, Guid>.ScheduledTaskCallback(new Action<UpdateTaskScheduler<int, Guid>.ScheduledTaskCallbackEventArgs>(this.CallbackRoutine), guid);
				if (DiscoveryNetObjectStatusManager.log.IsDebugEnabled)
				{
					DiscoveryNetObjectStatusManager.log.DebugFormat("Registering awaiting callback for profiles {0}, request: {1}", string.Join(", ", (from p in profileIDs
					select p.ToString()).ToArray<string>()), guid);
				}
			}
			foreach (int taskKey in profileIDs)
			{
				this.scheduler.RequestUpdateAsync(taskKey, callback, waitForChangesDelay);
			}
		}

		// Token: 0x060002BD RID: 701 RVA: 0x00011060 File Offset: 0x0000F260
		internal void CallbackRoutine(UpdateTaskScheduler<int, Guid>.ScheduledTaskCallbackEventArgs state)
		{
			Action action = null;
			object obj = this.syncLock;
			lock (obj)
			{
				DiscoveryNetObjectStatusManager.CallbackInfo callbackInfo;
				if (this.awaitingCallbacks.TryGetValue(state.State, out callbackInfo))
				{
					if (callbackInfo.AwaitingCallsCount > 1)
					{
						callbackInfo.AwaitingCallsCount--;
						if (DiscoveryNetObjectStatusManager.log.IsDebugEnabled)
						{
							DiscoveryNetObjectStatusManager.log.DebugFormat("Supressing callback for profile {0}, update request {1}, waiting for {2} more", state.TaskKey, state.State, callbackInfo.AwaitingCallsCount);
						}
					}
					else
					{
						action = callbackInfo.CallbackRoutine;
						this.awaitingCallbacks.Remove(state.State);
						if (DiscoveryNetObjectStatusManager.log.IsDebugEnabled)
						{
							DiscoveryNetObjectStatusManager.log.DebugFormat("Firing callback for profile {0}, update request {1}", state.TaskKey, state.State);
						}
					}
				}
				else
				{
					DiscoveryNetObjectStatusManager.log.ErrorFormat("Callback for profile {0} with unknown update request {1} received", state.TaskKey, state.State);
				}
			}
			if (action != null)
			{
				try
				{
					action();
				}
				catch (Exception ex)
				{
					DiscoveryNetObjectStatusManager.log.Error(string.Format("Callback handling routine for profile {0}, update request {1} failed", state.TaskKey, state.State), ex);
				}
			}
		}

		// Token: 0x060002BE RID: 702 RVA: 0x000111C4 File Offset: 0x0000F3C4
		internal static void UpdateRoutine(int profileID)
		{
			if (profileID <= 0)
			{
				throw new ArgumentException("Invalid ProfileID", "profileID");
			}
			using (LocaleThreadState.EnsurePrimaryLocale())
			{
				try
				{
					IEnumerable<IScheduledDiscoveryPlugin> enumerable = DiscoveryHelper.GetOrderedDiscoveryPlugins().OfType<IScheduledDiscoveryPlugin>();
					if (enumerable.Count<IScheduledDiscoveryPlugin>() > 0)
					{
						DiscoveryResultBase discoveryResult = DiscoveryResultManager.GetDiscoveryResult(profileID, enumerable.Cast<IDiscoveryPlugin>().ToList<IDiscoveryPlugin>());
						using (IEnumerator<IScheduledDiscoveryPlugin> enumerator = enumerable.GetEnumerator())
						{
							Action<string, double> <>9__0;
							while (enumerator.MoveNext())
							{
								IScheduledDiscoveryPlugin scheduledDiscoveryPlugin = enumerator.Current;
								DiscoveryResultBase discoveryResultBase = discoveryResult;
								Action<string, double> action;
								if ((action = <>9__0) == null)
								{
									action = (<>9__0 = delegate(string message, double phaseProgress)
									{
										if (DiscoveryNetObjectStatusManager.log.IsInfoEnabled)
										{
											DiscoveryNetObjectStatusManager.log.InfoFormat("Updating Discovered Net Object Statuses for profile {0}: {1} - {2}", profileID, phaseProgress, message);
										}
									});
								}
								scheduledDiscoveryPlugin.UpdateImportStatuses(discoveryResultBase, action);
							}
							goto IL_CC;
						}
					}
					if (DiscoveryNetObjectStatusManager.log.IsInfoEnabled)
					{
						DiscoveryNetObjectStatusManager.log.InfoFormat("Skipping Discovered Net Object Status update for profile {0}", profileID);
					}
					IL_CC:;
				}
				catch (Exception ex)
				{
					DiscoveryNetObjectStatusManager.log.Error(string.Format("Update Discovered Net Object Statuses for profile {0} failed", profileID), ex);
				}
			}
		}

		// Token: 0x04000075 RID: 117
		private static Log log = new Log();

		// Token: 0x04000076 RID: 118
		private static IDiscoveryNetObjectStatusManager instance = new DiscoveryNetObjectStatusManager();

		// Token: 0x04000077 RID: 119
		internal IDiscoveryDAL discoveryDALProvider = new DiscoveryProfileDAL();

		// Token: 0x04000078 RID: 120
		private readonly DiscoveryNetObjectStatusManager.Scheduler scheduler;

		// Token: 0x04000079 RID: 121
		private object syncLock = new object();

		// Token: 0x0400007A RID: 122
		private Dictionary<Guid, DiscoveryNetObjectStatusManager.CallbackInfo> awaitingCallbacks = new Dictionary<Guid, DiscoveryNetObjectStatusManager.CallbackInfo>();

		// Token: 0x0200010C RID: 268
		internal class Scheduler : UpdateTaskScheduler<int, Guid>
		{
			// Token: 0x06000A8A RID: 2698 RVA: 0x00047C0D File Offset: 0x00045E0D
			internal Scheduler(Action<int> routine, int maxRunningTasks, TimeSpan postponeTaskDelay, TimeSpan mandatorySchedulerDelay) : base(routine, maxRunningTasks, postponeTaskDelay, mandatorySchedulerDelay)
			{
			}
		}

		// Token: 0x0200010D RID: 269
		private class CallbackInfo
		{
			// Token: 0x06000A8B RID: 2699 RVA: 0x00047C1A File Offset: 0x00045E1A
			public CallbackInfo(Action routine, int avaitingCallsCount)
			{
				this.CallbackRoutine = routine;
				this.AwaitingCallsCount = avaitingCallsCount;
			}

			// Token: 0x04000396 RID: 918
			public Action CallbackRoutine;

			// Token: 0x04000397 RID: 919
			public int AwaitingCallsCount;
		}
	}
}
