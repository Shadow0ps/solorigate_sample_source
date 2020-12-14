using System;
using System.Linq;
using SolarWinds.AgentManagement.Contract;
using SolarWinds.InformationService.Contract2;
using SolarWinds.InformationService.Contract2.PubSub;
using SolarWinds.InformationService.Linq.Plugins;
using SolarWinds.InformationService.Linq.Plugins.Core.Orion;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.BusinessLayer.Engines;
using SolarWinds.Orion.Core.Common.Swis;

namespace SolarWinds.Orion.Core.BusinessLayer.Agent
{
	// Token: 0x020000BC RID: 188
	internal sealed class RemoteCollectorConnectedNotificationSubscriber : INotificationSubscriber
	{
		// Token: 0x0600092E RID: 2350 RVA: 0x000421C4 File Offset: 0x000403C4
		public RemoteCollectorConnectedNotificationSubscriber(ISwisContextFactory swisContextFactory, Action<IEngineComponent> onRemoteCollectorStatusChanged, int masterEngineId)
		{
			if (swisContextFactory == null)
			{
				throw new ArgumentNullException("swisContextFactory");
			}
			this._swisContextFactory = swisContextFactory;
			if (onRemoteCollectorStatusChanged == null)
			{
				throw new ArgumentNullException("onRemoteCollectorStatusChanged");
			}
			this._onRemoteCollectorStatusChanged = onRemoteCollectorStatusChanged;
			this._masterEngineId = masterEngineId;
		}

		// Token: 0x0600092F RID: 2351 RVA: 0x00042200 File Offset: 0x00040400
		public void OnIndication(string subscriptionId, string indicationType, PropertyBag indicationProperties, PropertyBag sourceInstanceProperties)
		{
			try
			{
				AgentStatus agentStatus;
				string agentId;
				if (RemoteCollectorConnectedNotificationSubscriber.GetRequiredProperties(sourceInstanceProperties, out agentId, out agentStatus))
				{
					RemoteCollectorConnectedNotificationSubscriber.Log.DebugFormat("Remote Collector on Agent #{0} has changed status", agentId);
					using (CoreSwisContext coreSwisContext = this._swisContextFactory.Create())
					{
						int? num = (from x in coreSwisContext.Entity<EngineProperties>()
						where x.PropertyName == "AgentId" && x.PropertyValue == agentId && x.Engine.MasterEngineID == (int?)this._masterEngineId
						select x.EngineID).FirstOrDefault<int?>();
						if (num != null)
						{
							RemoteCollectorConnectedNotificationSubscriber.Log.DebugFormat("Remote Collector engine #{0} on Agent #{1} changes status to {2}", num, agentId, agentStatus);
							RemoteCollectorConnectedNotificationSubscriber.EngineComponent obj = new RemoteCollectorConnectedNotificationSubscriber.EngineComponent(num.Value, agentStatus);
							this._onRemoteCollectorStatusChanged(obj);
						}
						else
						{
							RemoteCollectorConnectedNotificationSubscriber.Log.DebugFormat("Remote Collector on Agent #{0} in not connected to local master engine #{1}", agentId, this._masterEngineId);
						}
						goto IL_212;
					}
				}
				RemoteCollectorConnectedNotificationSubscriber.Log.DebugFormat("Remote Collector Agent connection indication does not have all the required properties (AgentId and AgentStatus)", Array.Empty<object>());
				IL_212:;
			}
			catch (Exception ex)
			{
				RemoteCollectorConnectedNotificationSubscriber.Log.Error("Unexpected error in processing Agent Remote Collector Agent connection indication", ex);
			}
		}

		// Token: 0x06000930 RID: 2352 RVA: 0x0004246C File Offset: 0x0004066C
		private static bool GetRequiredProperties(PropertyBag sourceInstanceProperties, out string agentId, out AgentStatus agentStatus)
		{
			agentId = null;
			agentStatus = 0;
			if (sourceInstanceProperties == null)
			{
				return false;
			}
			object obj;
			if (!sourceInstanceProperties.TryGetValue("AgentId", out obj) || obj == null)
			{
				return false;
			}
			object obj2;
			if (!sourceInstanceProperties.TryGetValue("AgentStatus", out obj2) || obj2 == null)
			{
				return false;
			}
			agentId = obj.ToString();
			agentStatus = (AgentStatus)obj2;
			return true;
		}

		// Token: 0x040002A2 RID: 674
		private static readonly Log Log = new Log();

		// Token: 0x040002A3 RID: 675
		internal static readonly string RemoteCollectorConnectedSubscriptionQuery = "SUBSCRIBE CHANGES TO Orion.AgentManagement.Agent INCLUDE AgentId, AgentStatus WHEN (ADDED OR DELETED OR AgentStatus CHANGED OR Type CHANGED)" + string.Format(" AND (Type = {0} OR PREVIOUS(Type) = {1})", 2, 2);

		// Token: 0x040002A4 RID: 676
		private readonly ISwisContextFactory _swisContextFactory;

		// Token: 0x040002A5 RID: 677
		private readonly Action<IEngineComponent> _onRemoteCollectorStatusChanged;

		// Token: 0x040002A6 RID: 678
		private readonly int _masterEngineId;

		// Token: 0x020001B3 RID: 435
		private class EngineComponent : IEngineComponent
		{
			// Token: 0x06000CD0 RID: 3280 RVA: 0x0004B303 File Offset: 0x00049503
			public EngineComponent(int engineId, AgentStatus agentStatus)
			{
				this.EngineId = engineId;
				this.AgentStatus = agentStatus;
			}

			// Token: 0x17000170 RID: 368
			// (get) Token: 0x06000CD1 RID: 3281 RVA: 0x0004B319 File Offset: 0x00049519
			private AgentStatus AgentStatus { get; }

			// Token: 0x17000171 RID: 369
			// (get) Token: 0x06000CD2 RID: 3282 RVA: 0x0004B321 File Offset: 0x00049521
			public int EngineId { get; }

			// Token: 0x06000CD3 RID: 3283 RVA: 0x0004B329 File Offset: 0x00049529
			public EngineComponentStatus GetStatus()
			{
				if (this.AgentStatus != 1)
				{
					return EngineComponentStatus.Down;
				}
				return EngineComponentStatus.Up;
			}
		}
	}
}
