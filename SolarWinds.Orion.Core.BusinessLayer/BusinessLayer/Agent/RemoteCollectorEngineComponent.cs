using System;
using System.Linq;
using System.Runtime.CompilerServices;
using SolarWinds.AgentManagement.Contract;
using SolarWinds.Orion.Core.BusinessLayer.Engines;

namespace SolarWinds.Orion.Core.BusinessLayer.Agent
{
	// Token: 0x020000BD RID: 189
	internal class RemoteCollectorEngineComponent : IEngineComponent
	{
		// Token: 0x06000932 RID: 2354 RVA: 0x000424EE File Offset: 0x000406EE
		public RemoteCollectorEngineComponent(int engineId, IRemoteCollectorAgentStatusProvider agentStatusProvider)
		{
			if (agentStatusProvider == null)
			{
				throw new ArgumentNullException("agentStatusProvider");
			}
			this._agentStatusProvider = agentStatusProvider;
			this.EngineId = engineId;
		}

		// Token: 0x17000126 RID: 294
		// (get) Token: 0x06000933 RID: 2355 RVA: 0x00042513 File Offset: 0x00040713
		public int EngineId { get; }

		// Token: 0x06000934 RID: 2356 RVA: 0x0004251B File Offset: 0x0004071B
		public EngineComponentStatus GetStatus()
		{
			return RemoteCollectorEngineComponent.ToEngineStatus(this._agentStatusProvider.GetStatus(this.EngineId));
		}

		// Token: 0x06000935 RID: 2357 RVA: 0x00042533 File Offset: 0x00040733
		private static EngineComponentStatus ToEngineStatus(AgentStatus agentStatus)
		{
			if (!RemoteCollectorEngineComponent.EngineUpStatuses.Contains(agentStatus))
			{
				return EngineComponentStatus.Down;
			}
			return EngineComponentStatus.Up;
		}

		// Token: 0x06000936 RID: 2358 RVA: 0x00042545 File Offset: 0x00040745
		// Note: this type is marked as 'beforefieldinit'.
		static RemoteCollectorEngineComponent()
		{
			AgentStatus[] array = new AgentStatus[9];
			RuntimeHelpers.InitializeArray(array, fieldof(<PrivateImplementationDetails>.AEC16ABA6566EC8C564489327314ABDEDB0431AB).FieldHandle);
			RemoteCollectorEngineComponent.EngineUpStatuses = array;
		}

		// Token: 0x040002A7 RID: 679
		private static readonly AgentStatus[] EngineUpStatuses;

		// Token: 0x040002A8 RID: 680
		private readonly IRemoteCollectorAgentStatusProvider _agentStatusProvider;
	}
}
