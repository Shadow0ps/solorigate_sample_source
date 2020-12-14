using System;
using System.Collections.Generic;
using System.Linq;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.Common.Agent;

namespace SolarWinds.Orion.Core.BusinessLayer.Agent
{
	// Token: 0x020000B7 RID: 183
	internal class AgentDeployer
	{
		// Token: 0x060008FF RID: 2303 RVA: 0x00040F60 File Offset: 0x0003F160
		public AgentDeployer() : this(new AgentInfoDAL())
		{
		}

		// Token: 0x06000900 RID: 2304 RVA: 0x00040F6D File Offset: 0x0003F16D
		internal AgentDeployer(IAgentInfoDAL agentInfoDal)
		{
			this.agentInfoDal = agentInfoDal;
		}

		// Token: 0x06000901 RID: 2305 RVA: 0x00040F7C File Offset: 0x0003F17C
		public int StartDeployingAgent(AgentDeploymentSettings settings)
		{
			AgentDeployer.log.Debug("StartDeployingAgent entered");
			AgentDeploymentWatcher instance = AgentDeploymentWatcher.GetInstance(this.agentInfoDal);
			instance.Start();
			int num = this.DeployAgent(settings);
			AgentInfo agentInfo = new AgentManager(this.agentInfoDal).GetAgentInfo(num);
			instance.AddOrUpdateDeploymentInfo(AgentDeploymentInfo.Calculate(agentInfo, settings.RequiredPlugins, null));
			this.DeployMissingPlugins(num, settings.RequiredPlugins);
			return num;
		}

		// Token: 0x06000902 RID: 2306 RVA: 0x00040FE4 File Offset: 0x0003F1E4
		public void StartDeployingPlugins(int agentId, IEnumerable<string> requiredPlugins, Action<AgentDeploymentStatus> onFinishedCallback = null)
		{
			AgentDeployer.log.Debug("StartDeployingPlugins entered");
			AgentDeploymentInfo agentDeploymentInfo = AgentDeploymentInfo.Calculate(new AgentManager(this.agentInfoDal).GetAgentInfo(agentId), requiredPlugins, null);
			agentDeploymentInfo.StatusInfo = new AgentDeploymentStatusInfo(agentId, AgentDeploymentStatus.InProgress);
			AgentDeploymentWatcher instance = AgentDeploymentWatcher.GetInstance(this.agentInfoDal);
			instance.AddOrUpdateDeploymentInfo(agentDeploymentInfo);
			if (onFinishedCallback != null)
			{
				instance.SetOnFinishedCallback(agentId, onFinishedCallback);
			}
			this.DeployMissingPlugins(agentId, requiredPlugins);
		}

		// Token: 0x06000903 RID: 2307 RVA: 0x0004104C File Offset: 0x0003F24C
		private void DeployMissingPlugins(int agentId, IEnumerable<string> requiredPlugins)
		{
			AgentDeployer.log.DebugFormat("DeployMissingPlugins started, AgentId:{0}, RequiredPlugins:{1}", agentId, string.Join(",", requiredPlugins));
			AgentManager agentManager = new AgentManager(this.agentInfoDal);
			AgentInfo agentInfo = agentManager.GetAgentInfo(agentId);
			if (agentInfo == null)
			{
				throw new ArgumentException(string.Format("Agent with Id:{0} not found", agentId));
			}
			foreach (string text in (from p in agentInfo.Plugins
			where p.Status == 5 || p.Status == 12
			select p.PluginId).ToArray<string>())
			{
				AgentDeployer.log.DebugFormat("DeployMissingPlugins - Redeploying plugin {0}", text);
				agentManager.StartRedeployingPlugin(agentId, text);
			}
			foreach (string text2 in from requiredPluginId in requiredPlugins
			where agentInfo.Plugins.All((AgentPluginInfo installedPlugin) => installedPlugin.PluginId != requiredPluginId)
			select requiredPluginId)
			{
				AgentDeployer.log.DebugFormat("DeployMissingPlugins - Deploying plugin {0}", text2);
				agentManager.StartDeployingPlugin(agentId, text2);
			}
			agentInfo = agentManager.GetAgentInfo(agentId);
			if (agentInfo != null)
			{
				if (agentInfo.AgentStatus != 8)
				{
					if (!agentInfo.Plugins.Any((AgentPluginInfo p) => p.Status == 3))
					{
						if (!agentInfo.Plugins.Any((AgentPluginInfo p) => p.Status == 13))
						{
							return;
						}
					}
				}
				AgentDeployer.log.Debug("DeployMissingPlugins - Approve update");
				agentManager.ApproveUpdate(agentId);
			}
		}

		// Token: 0x06000904 RID: 2308 RVA: 0x00041240 File Offset: 0x0003F440
		private int DeployAgent(AgentDeploymentSettings settings)
		{
			AgentDeployer.log.Debug("DeployAgent started");
			AgentManager agentManager = new AgentManager(this.agentInfoDal);
			int num = agentManager.StartDeployingAgent(settings);
			agentManager.UpdateAgentNodeId(num, settings.NodeId);
			AgentDeployer.log.DebugFormat("DeployAgent finished, AgentId:{0}", num);
			return num;
		}

		// Token: 0x0400028E RID: 654
		private static readonly Log log = new Log();

		// Token: 0x0400028F RID: 655
		private readonly IAgentInfoDAL agentInfoDal;
	}
}
