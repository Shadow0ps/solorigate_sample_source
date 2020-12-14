using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.Common.Agent;
using SolarWinds.Orion.Core.Discovery;

namespace SolarWinds.Orion.Core.BusinessLayer.Agent
{
	// Token: 0x020000B8 RID: 184
	internal class AgentDeploymentWatcher
	{
		// Token: 0x06000906 RID: 2310 RVA: 0x00004FE1 File Offset: 0x000031E1
		private AgentDeploymentWatcher()
		{
		}

		// Token: 0x06000907 RID: 2311 RVA: 0x0004129D File Offset: 0x0003F49D
		private AgentDeploymentWatcher(IAgentInfoDAL agentInfoDal)
		{
			this.AgentInfoDal = agentInfoDal;
			this.Items = new Dictionary<int, AgentDeploymentWatcher.CacheItem>();
			this.NotificationSubscriber = new AgentNotificationSubscriber(new Action<int>(this.AgentNotification));
		}

		// Token: 0x17000123 RID: 291
		// (get) Token: 0x06000908 RID: 2312 RVA: 0x000412CE File Offset: 0x0003F4CE
		// (set) Token: 0x06000909 RID: 2313 RVA: 0x000412D6 File Offset: 0x0003F4D6
		private IAgentInfoDAL AgentInfoDal { get; set; }

		// Token: 0x17000124 RID: 292
		// (get) Token: 0x0600090A RID: 2314 RVA: 0x000412DF File Offset: 0x0003F4DF
		// (set) Token: 0x0600090B RID: 2315 RVA: 0x000412E7 File Offset: 0x0003F4E7
		private AgentNotificationSubscriber NotificationSubscriber { get; set; }

		// Token: 0x17000125 RID: 293
		// (get) Token: 0x0600090C RID: 2316 RVA: 0x000412F0 File Offset: 0x0003F4F0
		// (set) Token: 0x0600090D RID: 2317 RVA: 0x000412F8 File Offset: 0x0003F4F8
		private Dictionary<int, AgentDeploymentWatcher.CacheItem> Items { get; set; }

		// Token: 0x0600090E RID: 2318 RVA: 0x00041304 File Offset: 0x0003F504
		public static AgentDeploymentWatcher GetInstance(IAgentInfoDAL agentInfoDal)
		{
			if (AgentDeploymentWatcher.instance != null)
			{
				return AgentDeploymentWatcher.instance;
			}
			object obj = AgentDeploymentWatcher.syncLockInstance;
			lock (obj)
			{
				if (AgentDeploymentWatcher.instance == null)
				{
					AgentDeploymentWatcher.instance = new AgentDeploymentWatcher(agentInfoDal);
				}
			}
			return AgentDeploymentWatcher.instance;
		}

		// Token: 0x0600090F RID: 2319 RVA: 0x00041364 File Offset: 0x0003F564
		public void Start()
		{
			AgentDeploymentWatcher.log.Debug("AgentDeploymentWatcher.Start");
			object obj = AgentDeploymentWatcher.syncLockItems;
			lock (obj)
			{
				this.CheckWatcher();
			}
		}

		// Token: 0x06000910 RID: 2320 RVA: 0x000413B4 File Offset: 0x0003F5B4
		public void AddOrUpdateDeploymentInfo(AgentDeploymentInfo deploymentInfo)
		{
			if (deploymentInfo == null)
			{
				throw new ArgumentNullException("deploymentInfo");
			}
			if (deploymentInfo.Agent == null)
			{
				throw new ArgumentNullException("deploymentInfo.Agent");
			}
			AgentDeploymentWatcher.log.DebugFormat("AddOrUpdateDeploymentInfo started, agentId:{0}, status:{1}", deploymentInfo.Agent.AgentId, deploymentInfo.StatusInfo.Status);
			object obj = AgentDeploymentWatcher.syncLockItems;
			lock (obj)
			{
				AgentDeploymentWatcher.CacheItem cacheItem;
				if (this.Items.TryGetValue(deploymentInfo.Agent.AgentId, out cacheItem))
				{
					AgentDeploymentWatcher.log.Debug("AddOrUpdateDeploymentInfo - item found in cache, updating");
					cacheItem.DeploymentInfo = deploymentInfo;
				}
				else
				{
					AgentDeploymentWatcher.log.Debug("AddOrUpdateDeploymentInfo - item not found in cache, creating new item");
					cacheItem = new AgentDeploymentWatcher.CacheItem
					{
						DeploymentInfo = deploymentInfo
					};
					cacheItem.LastChecked = DateTime.Now;
					this.Items[deploymentInfo.Agent.AgentId] = cacheItem;
				}
				cacheItem.LastUpdated = DateTime.Now;
				cacheItem.RefreshNeeded = false;
				this.CheckWatcher();
			}
		}

		// Token: 0x06000911 RID: 2321 RVA: 0x000414C8 File Offset: 0x0003F6C8
		public void SetOnFinishedCallback(int agentId, Action<AgentDeploymentStatus> onFinished)
		{
			AgentDeploymentWatcher.log.DebugFormat("SetOnFinishedCallback entered, agentId:{0}", agentId);
			object obj = AgentDeploymentWatcher.syncLockItems;
			lock (obj)
			{
				AgentDeploymentWatcher.CacheItem cacheItem;
				if (this.Items.TryGetValue(agentId, out cacheItem))
				{
					cacheItem.OnFinishedCallback = onFinished;
				}
			}
		}

		// Token: 0x06000912 RID: 2322 RVA: 0x00041530 File Offset: 0x0003F730
		public AgentDeploymentInfo GetAgentDeploymentInfo(int agentId)
		{
			AgentDeploymentWatcher.log.DebugFormat("GetAgentDeploymentInfo started, agentId:{0}", agentId);
			AgentDeploymentInfo agentDeploymentInfo = null;
			object obj = AgentDeploymentWatcher.syncLockItems;
			lock (obj)
			{
				AgentDeploymentWatcher.CacheItem cacheItem;
				if (this.Items.TryGetValue(agentId, out cacheItem))
				{
					agentDeploymentInfo = cacheItem.DeploymentInfo;
					cacheItem.LastChecked = DateTime.Now;
					AgentDeploymentWatcher.log.DebugFormat("GetAgentDeploymentInfo - item found in cache, agentId:{0}, status:{1}", agentId, agentDeploymentInfo.StatusInfo.Status);
				}
			}
			if (agentDeploymentInfo == null)
			{
				agentDeploymentInfo = this.LoadAgentDeploymentInfo(agentId);
				AgentDeploymentWatcher.log.DebugFormat("GetAgentDeploymentInfo - item not found in cache, loading from db, agentId:{0}, status:{1}", agentId, agentDeploymentInfo.StatusInfo.Status);
			}
			return agentDeploymentInfo;
		}

		// Token: 0x06000913 RID: 2323 RVA: 0x000415F8 File Offset: 0x0003F7F8
		private void CheckWatcher()
		{
			AgentDeploymentWatcher.log.Debug("Checking Watcher status");
			if (!this.NotificationSubscriber.IsSubscribed())
			{
				AgentDeploymentWatcher.log.Debug("Starting NotificationSubscriber");
				this.NotificationSubscriber.Subscribe();
			}
			if (this.watcherTimer == null)
			{
				AgentDeploymentWatcher.log.Debug("Starting Watcher Timer");
				this.watcherTimer = new Timer(new TimerCallback(this.CheckItems), null, TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0));
			}
		}

		// Token: 0x06000914 RID: 2324 RVA: 0x00041686 File Offset: 0x0003F886
		private void StopWatcher()
		{
			if (this.watcherTimer != null)
			{
				this.watcherTimer.Dispose();
			}
			this.watcherTimer = null;
			if (this.NotificationSubscriber != null)
			{
				this.NotificationSubscriber.Unsubscribe();
			}
		}

		// Token: 0x06000915 RID: 2325 RVA: 0x000416B8 File Offset: 0x0003F8B8
		private void CheckItems(object state)
		{
			AgentDeploymentWatcher.log.Debug("CheckItems started");
			DateTime ExpireTime = DateTime.Now.Subtract(TimeSpan.FromSeconds(120.0));
			new List<Action<AgentDeploymentStatus>>();
			object obj = AgentDeploymentWatcher.syncLockItems;
			IEnumerable<AgentDeploymentWatcher.CacheItem> enumerable;
			IEnumerable<int> enumerable2;
			lock (obj)
			{
				AgentDeploymentWatcher.log.Debug("CheckItems - looking for items to remove");
				enumerable = (from item in this.Items
				where item.Value.DeploymentInfo.StatusInfo.Status == AgentDeploymentStatus.Finished || item.Value.DeploymentInfo.StatusInfo.Status == AgentDeploymentStatus.Failed || item.Value.LastChecked < ExpireTime
				select item.Value).ToArray<AgentDeploymentWatcher.CacheItem>();
				foreach (AgentDeploymentWatcher.CacheItem cacheItem in enumerable)
				{
					AgentDeploymentWatcher.log.DebugFormat("CheckItems - removing item, AgentId:{0}", cacheItem.DeploymentInfo.Agent.AgentId);
					this.Items.Remove(cacheItem.DeploymentInfo.Agent.AgentId);
				}
				if (this.Items.Count == 0)
				{
					AgentDeploymentWatcher.log.Debug("CheckItems - No remaining items in the cache, stopping Watcher");
					this.StopWatcher();
				}
				AgentDeploymentWatcher.log.Debug("CheckItems - looking for items to refresh");
				enumerable2 = (from item in this.Items
				where item.Value.RefreshNeeded || item.Value.LastUpdated < ExpireTime
				select item.Key).ToArray<int>();
			}
			foreach (int num in enumerable2)
			{
				AgentDeploymentWatcher.log.DebugFormat("CheckItems - refreshing item AgentId:{0}", num);
				AgentDeploymentInfo agentDeploymentInfo = this.LoadAgentDeploymentInfo(num);
				obj = AgentDeploymentWatcher.syncLockItems;
				lock (obj)
				{
					AgentDeploymentWatcher.CacheItem cacheItem2;
					if (this.Items.TryGetValue(agentDeploymentInfo.Agent.AgentId, out cacheItem2))
					{
						AgentDeploymentWatcher.log.DebugFormat("CheckItems - updating item AgentId:{0}, Status:{1}", num, agentDeploymentInfo.StatusInfo.Status);
						cacheItem2.DeploymentInfo = agentDeploymentInfo;
						cacheItem2.LastUpdated = DateTime.Now;
						cacheItem2.RefreshNeeded = false;
					}
					else
					{
						AgentDeploymentWatcher.log.Debug("CheckItems - item not found in the cache");
					}
				}
			}
			foreach (AgentDeploymentWatcher.CacheItem cacheItem3 in enumerable)
			{
				if (cacheItem3.OnFinishedCallback != null && (cacheItem3.DeploymentInfo.StatusInfo.Status == AgentDeploymentStatus.Finished || cacheItem3.DeploymentInfo.StatusInfo.Status == AgentDeploymentStatus.Failed))
				{
					cacheItem3.OnFinishedCallback(cacheItem3.DeploymentInfo.StatusInfo.Status);
				}
			}
		}

		// Token: 0x06000916 RID: 2326 RVA: 0x00041A28 File Offset: 0x0003FC28
		private void AgentNotification(int agentId)
		{
			AgentDeploymentWatcher.log.DebugFormat("AgentNotification started, AgentId:{0}", agentId);
			object obj = AgentDeploymentWatcher.syncLockItems;
			lock (obj)
			{
				AgentDeploymentWatcher.CacheItem cacheItem;
				if (this.Items.TryGetValue(agentId, out cacheItem))
				{
					AgentDeploymentWatcher.log.Debug("AgentNotification - item found, set refresh flag.");
					cacheItem.RefreshNeeded = true;
				}
				else
				{
					AgentDeploymentWatcher.log.Debug("AgentNotification - item not found");
				}
			}
		}

		// Token: 0x06000917 RID: 2327 RVA: 0x00041AB0 File Offset: 0x0003FCB0
		private AgentDeploymentInfo LoadAgentDeploymentInfo(int agentId)
		{
			AgentInfo agentInfo = new AgentManager(this.AgentInfoDal).GetAgentInfo(agentId);
			if (agentInfo != null)
			{
				string[] agentDiscoveryPluginIds = DiscoveryHelper.GetAgentDiscoveryPluginIds();
				return AgentDeploymentInfo.Calculate(agentInfo, agentDiscoveryPluginIds, null);
			}
			return new AgentDeploymentInfo
			{
				Agent = new AgentInfo
				{
					AgentId = agentId
				},
				StatusInfo = new AgentDeploymentStatusInfo(agentId, AgentDeploymentStatus.Failed, "Agent not found.")
			};
		}

		// Token: 0x04000290 RID: 656
		private static readonly Log log = new Log();

		// Token: 0x04000291 RID: 657
		private const int MaxItemAge = 120;

		// Token: 0x04000292 RID: 658
		private const int CheckPeriod = 5;

		// Token: 0x04000295 RID: 661
		private Timer watcherTimer;

		// Token: 0x04000297 RID: 663
		private static readonly object syncLockItems = new object();

		// Token: 0x04000298 RID: 664
		private static AgentDeploymentWatcher instance;

		// Token: 0x04000299 RID: 665
		private static readonly object syncLockInstance = new object();

		// Token: 0x020001B0 RID: 432
		private class CacheItem
		{
			// Token: 0x1700016B RID: 363
			// (get) Token: 0x06000CBE RID: 3262 RVA: 0x0004B20F File Offset: 0x0004940F
			// (set) Token: 0x06000CBF RID: 3263 RVA: 0x0004B217 File Offset: 0x00049417
			public AgentDeploymentInfo DeploymentInfo { get; set; }

			// Token: 0x1700016C RID: 364
			// (get) Token: 0x06000CC0 RID: 3264 RVA: 0x0004B220 File Offset: 0x00049420
			// (set) Token: 0x06000CC1 RID: 3265 RVA: 0x0004B228 File Offset: 0x00049428
			public DateTime LastUpdated { get; set; }

			// Token: 0x1700016D RID: 365
			// (get) Token: 0x06000CC2 RID: 3266 RVA: 0x0004B231 File Offset: 0x00049431
			// (set) Token: 0x06000CC3 RID: 3267 RVA: 0x0004B239 File Offset: 0x00049439
			public DateTime LastChecked { get; set; }

			// Token: 0x1700016E RID: 366
			// (get) Token: 0x06000CC4 RID: 3268 RVA: 0x0004B242 File Offset: 0x00049442
			// (set) Token: 0x06000CC5 RID: 3269 RVA: 0x0004B24A File Offset: 0x0004944A
			public bool RefreshNeeded { get; set; }

			// Token: 0x1700016F RID: 367
			// (get) Token: 0x06000CC6 RID: 3270 RVA: 0x0004B253 File Offset: 0x00049453
			// (set) Token: 0x06000CC7 RID: 3271 RVA: 0x0004B25B File Offset: 0x0004945B
			public Action<AgentDeploymentStatus> OnFinishedCallback { get; set; }
		}
	}
}
