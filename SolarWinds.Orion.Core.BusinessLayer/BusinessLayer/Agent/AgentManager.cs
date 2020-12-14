using System;
using System.Collections.Generic;
using System.Net;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Agent;
using SolarWinds.Orion.Core.Common.Swis;

namespace SolarWinds.Orion.Core.BusinessLayer.Agent
{
	// Token: 0x020000B9 RID: 185
	internal class AgentManager
	{
		// Token: 0x06000919 RID: 2329 RVA: 0x00041B2A File Offset: 0x0003FD2A
		public AgentManager(IAgentInfoDAL agentInfoDal)
		{
			this._agentInfoDal = agentInfoDal;
		}

		// Token: 0x0600091A RID: 2330 RVA: 0x00041B39 File Offset: 0x0003FD39
		public AgentInfo GetAgentInfo(int agentId)
		{
			return this._agentInfoDal.GetAgentInfo(agentId);
		}

		// Token: 0x0600091B RID: 2331 RVA: 0x00041B47 File Offset: 0x0003FD47
		public AgentInfo GetAgentInfoByNodeId(int nodeId)
		{
			return this._agentInfoDal.GetAgentInfoByNode(nodeId);
		}

		// Token: 0x0600091C RID: 2332 RVA: 0x00041B55 File Offset: 0x0003FD55
		public AgentInfo DetectAgent(string ipAddress, string hostname)
		{
			if (string.IsNullOrWhiteSpace(ipAddress) && string.IsNullOrWhiteSpace(hostname))
			{
				throw new ArgumentException("ipAddress or hostname must be specified");
			}
			if (hostname.Equals("localhost", StringComparison.InvariantCultureIgnoreCase))
			{
				hostname = Dns.GetHostName();
			}
			return this._agentInfoDal.GetAgentInfoByIpOrHostname(ipAddress, hostname);
		}

		// Token: 0x0600091D RID: 2333 RVA: 0x00041B94 File Offset: 0x0003FD94
		public int StartDeployingAgent(AgentDeploymentSettings settings)
		{
			if (settings == null)
			{
				throw new ArgumentNullException("settings");
			}
			if (string.IsNullOrWhiteSpace(settings.IpAddress) && string.IsNullOrWhiteSpace(settings.Hostname))
			{
				throw new ArgumentException("ipAddress or hostname must be specified");
			}
			int result;
			using (SwisConnectionProxyFactory swisConnectionProxyFactory = new SwisConnectionProxyFactory())
			{
				using (IInformationServiceProxy2 informationServiceProxy = swisConnectionProxyFactory.Create())
				{
					string text = (!string.IsNullOrEmpty(settings.Hostname)) ? settings.Hostname : settings.IpAddress;
					string text2 = text;
					int num = 1;
					while (!this._agentInfoDal.IsUniqueAgentName(text2))
					{
						text2 = string.Format("{0}-{1}", text, ++num);
					}
					int num2 = informationServiceProxy.Invoke<int>("Orion.AgentManagement.Agent", "Deploy", new object[]
					{
						settings.EngineId,
						text2,
						text,
						settings.IpAddress,
						settings.Credentials.Username,
						settings.Credentials.Password,
						settings.Credentials.AdditionalUsername ?? "",
						settings.Credentials.AdditionalPassword ?? "",
						settings.Credentials.PasswordIsPrivateKey,
						settings.Credentials.PrivateKeyPassword ?? "",
						0,
						settings.InstallPackageId ?? ""
					});
					this.UpdateAgentNodeId(num2, 0);
					result = num2;
				}
			}
			return result;
		}

		// Token: 0x0600091E RID: 2334 RVA: 0x00041D50 File Offset: 0x0003FF50
		public void StartDeployingPlugin(int agentId, string pluginId)
		{
			if (string.IsNullOrEmpty(pluginId))
			{
				throw new ArgumentNullException("pluginId", "Plugin Id must be specified.");
			}
			using (SwisConnectionProxyFactory swisConnectionProxyFactory = new SwisConnectionProxyFactory())
			{
				using (IInformationServiceProxy2 informationServiceProxy = swisConnectionProxyFactory.Create())
				{
					informationServiceProxy.Invoke<object>("Orion.AgentManagement.Agent", "DeployPlugin", new object[]
					{
						agentId,
						pluginId
					});
				}
			}
		}

		// Token: 0x0600091F RID: 2335 RVA: 0x00041DD8 File Offset: 0x0003FFD8
		public void StartRedeployingPlugin(int agentId, string pluginId)
		{
			if (string.IsNullOrEmpty(pluginId))
			{
				throw new ArgumentNullException("pluginId", "Plugin Id must be specified.");
			}
			using (SwisConnectionProxyFactory swisConnectionProxyFactory = new SwisConnectionProxyFactory())
			{
				using (IInformationServiceProxy2 informationServiceProxy = swisConnectionProxyFactory.Create())
				{
					informationServiceProxy.Invoke<object>("Orion.AgentManagement.Agent", "RedeployPlugin", new object[]
					{
						agentId,
						pluginId
					});
				}
			}
		}

		// Token: 0x06000920 RID: 2336 RVA: 0x00041E60 File Offset: 0x00040060
		public void ApproveUpdate(int agentId)
		{
			using (SwisConnectionProxyFactory swisConnectionProxyFactory = new SwisConnectionProxyFactory())
			{
				using (IInformationServiceProxy2 informationServiceProxy = swisConnectionProxyFactory.Create())
				{
					informationServiceProxy.Invoke<object>("Orion.AgentManagement.Agent", "ApproveUpdate", new object[]
					{
						agentId
					});
				}
			}
		}

		// Token: 0x06000921 RID: 2337 RVA: 0x00041ECC File Offset: 0x000400CC
		private void UpdateAgentNodeId(int agentId, int nodeId, IInformationServiceProxy2 proxy)
		{
			AgentInfo agentInfo = this._agentInfoDal.GetAgentInfo(agentId);
			if (agentInfo != null)
			{
				proxy.Update(agentInfo.Uri, new Dictionary<string, object>
				{
					{
						"NodeId",
						nodeId
					}
				});
				return;
			}
			AgentManager.log.WarnFormat("Agent Id={0} not found.", agentId);
		}

		// Token: 0x06000922 RID: 2338 RVA: 0x00041F24 File Offset: 0x00040124
		public void UpdateAgentNodeId(int agentId, int nodeId)
		{
			using (SwisConnectionProxyFactory swisConnectionProxyFactory = new SwisConnectionProxyFactory())
			{
				using (IInformationServiceProxy2 informationServiceProxy = swisConnectionProxyFactory.Create())
				{
					this.UpdateAgentNodeId(agentId, nodeId, informationServiceProxy);
				}
			}
		}

		// Token: 0x06000923 RID: 2339 RVA: 0x00041F7C File Offset: 0x0004017C
		public void ResetAgentNodeId(int nodeId)
		{
			using (SwisConnectionProxyFactory swisConnectionProxyFactory = new SwisConnectionProxyFactory())
			{
				using (IInformationServiceProxy2 informationServiceProxy = swisConnectionProxyFactory.Create())
				{
					AgentInfo agentInfoByNode = this._agentInfoDal.GetAgentInfoByNode(nodeId);
					if (agentInfoByNode != null)
					{
						informationServiceProxy.Update(agentInfoByNode.Uri, new Dictionary<string, object>
						{
							{
								"NodeId",
								nodeId
							}
						});
					}
					else
					{
						AgentManager.log.WarnFormat("Agent for NodeId={0} not found", nodeId);
					}
				}
			}
		}

		// Token: 0x0400029A RID: 666
		private static readonly Log log = new Log();

		// Token: 0x0400029B RID: 667
		private readonly IAgentInfoDAL _agentInfoDal;

		// Token: 0x0400029C RID: 668
		private const string AgentEntityName = "Orion.AgentManagement.Agent";
	}
}
