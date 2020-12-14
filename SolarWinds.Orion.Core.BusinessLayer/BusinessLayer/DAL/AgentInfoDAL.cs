using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SolarWinds.AgentManagement.Contract.Models;
using SolarWinds.InformationService.InformationServiceClient;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Agent;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000087 RID: 135
	public class AgentInfoDAL : IAgentInfoDAL
	{
		// Token: 0x060006A7 RID: 1703 RVA: 0x00029B48 File Offset: 0x00027D48
		public IEnumerable<AgentInfo> GetAgentsInfo()
		{
			return this.GetAgentsInfo(null, null);
		}

		// Token: 0x060006A8 RID: 1704 RVA: 0x00029B52 File Offset: 0x00027D52
		public AgentInfo GetAgentInfoByNode(int nodeId)
		{
			return this.GetAgentsInfo("a.NodeID = @NodeID", new Dictionary<string, object>
			{
				{
					"NodeID",
					nodeId
				}
			}).FirstOrDefault<AgentInfo>();
		}

		// Token: 0x060006A9 RID: 1705 RVA: 0x00029B7A File Offset: 0x00027D7A
		public AgentInfo GetAgentInfo(int agentId)
		{
			return this.GetAgentsInfo("a.AgentId = @AgentId", new Dictionary<string, object>
			{
				{
					"AgentId",
					agentId
				}
			}).FirstOrDefault<AgentInfo>();
		}

		// Token: 0x060006AA RID: 1706 RVA: 0x00029BA4 File Offset: 0x00027DA4
		public AgentInfo GetAgentInfoByAgentAddress(string address)
		{
			Guid guid;
			if (!Guid.TryParse(address, out guid))
			{
				return null;
			}
			return this.GetAgentsInfo("a.AgentGuid = @AgentGuid", new Dictionary<string, object>
			{
				{
					"AgentGuid",
					guid
				}
			}).FirstOrDefault<AgentInfo>();
		}

		// Token: 0x060006AB RID: 1707 RVA: 0x00029BE4 File Offset: 0x00027DE4
		public AgentInfo GetAgentInfoByIpOrHostname(string ipAddress, string hostname)
		{
			if (string.IsNullOrWhiteSpace(ipAddress) && string.IsNullOrWhiteSpace(hostname))
			{
				throw new ArgumentException("ipAddress or hostname must be specified");
			}
			List<string> list = new List<string>();
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (!string.IsNullOrWhiteSpace(ipAddress))
			{
				list.Add("a.IP = @ipAddress");
				dictionary.Add("ipAddress", ipAddress);
			}
			if (!string.IsNullOrWhiteSpace(hostname))
			{
				list.Add("a.Hostname = @hostname");
				dictionary.Add("hostname", hostname);
			}
			return this.GetAgentsInfo(string.Format("({0}) AND (n.ObjectSubType IS NULL OR n.ObjectSubType <> 'Agent')", string.Join(" OR ", list)), dictionary).FirstOrDefault<AgentInfo>();
		}

		// Token: 0x060006AC RID: 1708 RVA: 0x00029C78 File Offset: 0x00027E78
		public IEnumerable<AgentInfo> GetAgentsByNodesFilter(int engineId, string nodesWhereClause)
		{
			List<AgentInfo> source = new List<AgentInfo>();
			using (InformationServiceConnection informationServiceConnection = InformationServiceConnectionProvider.CreateSystemConnectionV3())
			{
				using (InformationServiceCommand informationServiceCommand = informationServiceConnection.CreateCommand())
				{
					informationServiceCommand.CommandText = string.Format("SELECT agents.AgentId, \r\n                            agents.AgentGuid, \r\n                            agents.NodeId, \r\n                            nodes.ObjectSubType,\r\n                            agents.Uri,\r\n                            agents.Name, \r\n                            agents.Hostname, \r\n                            agents.IP, \r\n                            agents.PollingEngineId,\r\n                            agents.AgentStatus, \r\n                            agents.AgentStatusMessage, \r\n                            agents.ConnectionStatus, \r\n                            agents.ConnectionStatusMessage,\r\n                            agents.OSType,\r\n                            agents.OSDistro,\r\n                            p.PluginId, \r\n                            p.Status, \r\n                            p.StatusMessage\r\n                    FROM Orion.AgentManagement.Agent agents\r\n                    INNER JOIN Orion.Nodes Nodes ON Nodes.NodeID = agents.NodeID\r\n                    LEFT JOIN Orion.AgentManagement.AgentPlugin p ON agents.AgentId = p.AgentId \r\n                    WHERE agents.PollingEngineId=@engineId AND agents.ConnectionStatus = @connectionStatus {0}\r\n                    ORDER BY agents.AgentId", (!string.IsNullOrWhiteSpace(nodesWhereClause)) ? (" AND " + nodesWhereClause) : string.Empty);
					informationServiceCommand.Parameters.AddWithValue("connectionStatus", 1);
					informationServiceCommand.Parameters.AddWithValue("engineId", engineId);
					using (IDataReader dataReader = informationServiceCommand.ExecuteReader())
					{
						source = this.GetAgentsFromReader(dataReader);
					}
				}
			}
			return from x in source
			where x.NodeID != null && x.NodeSubType == "Agent"
			select x;
		}

		// Token: 0x060006AD RID: 1709 RVA: 0x00029D6C File Offset: 0x00027F6C
		private IEnumerable<AgentInfo> GetAgentsInfo(string whereClause, IDictionary<string, object> parameters)
		{
			List<AgentInfo> result = new List<AgentInfo>();
			using (InformationServiceConnection informationServiceConnection = InformationServiceConnectionProvider.CreateSystemConnectionV3())
			{
				using (InformationServiceCommand informationServiceCommand = informationServiceConnection.CreateCommand())
				{
					informationServiceCommand.CommandText = string.Format("SELECT a.AgentId, \r\n                            a.AgentGuid, \r\n                            a.NodeId, \r\n                            n.ObjectSubType,\r\n                            a.Uri,\r\n                            a.Name, \r\n                            a.Hostname, \r\n                            a.IP, \r\n                            a.PollingEngineId,\r\n                            a.AgentStatus, \r\n                            a.AgentStatusMessage, \r\n                            a.ConnectionStatus, \r\n                            a.ConnectionStatusMessage,\r\n                            p.PluginId, \r\n                            p.Status, \r\n                            p.StatusMessage,\r\n                            a.OSType,\r\n                            a.OSDistro\r\n                    FROM Orion.AgentManagement.Agent a\r\n                    LEFT JOIN Orion.Nodes n ON n.NodeID = a.NodeID\r\n                    LEFT JOIN Orion.AgentManagement.AgentPlugin p ON a.AgentId = p.AgentId \r\n                    {0}\r\n                    ORDER BY a.AgentId", (!string.IsNullOrEmpty(whereClause)) ? ("WHERE " + whereClause) : string.Empty);
					if (parameters != null)
					{
						foreach (KeyValuePair<string, object> keyValuePair in parameters)
						{
							informationServiceCommand.Parameters.AddWithValue(keyValuePair.Key, keyValuePair.Value);
						}
					}
					using (IDataReader dataReader = informationServiceCommand.ExecuteReader())
					{
						result = this.GetAgentsFromReader(dataReader);
					}
				}
			}
			return result;
		}

		// Token: 0x060006AE RID: 1710 RVA: 0x00029E60 File Offset: 0x00028060
		private List<AgentInfo> GetAgentsFromReader(IDataReader reader)
		{
			List<AgentInfo> list = new List<AgentInfo>();
			AgentInfo agentInfo = null;
			while (reader.Read())
			{
				OsType osType;
				if (!Enum.TryParse<OsType>(DatabaseFunctions.GetString(reader, "OSType"), true, out osType))
				{
					osType = 0;
				}
				AgentInfo agentInfo2 = new AgentInfo
				{
					AgentId = DatabaseFunctions.GetInt32(reader, "AgentId"),
					AgentGuid = DatabaseFunctions.GetGuid(reader, "AgentGuid"),
					NodeID = DatabaseFunctions.GetNullableInt32(reader, "NodeId"),
					NodeSubType = DatabaseFunctions.GetString(reader, "ObjectSubType", null),
					Uri = DatabaseFunctions.GetString(reader, "Uri"),
					PollingEngineId = DatabaseFunctions.GetInt32(reader, "PollingEngineId"),
					AgentStatus = DatabaseFunctions.GetInt32(reader, "AgentStatus"),
					AgentStatusMessage = DatabaseFunctions.GetString(reader, "AgentStatusMessage"),
					ConnectionStatus = DatabaseFunctions.GetInt32(reader, "ConnectionStatus"),
					ConnectionStatusMessage = DatabaseFunctions.GetString(reader, "ConnectionStatusMessage"),
					OsType = osType,
					OsDistro = DatabaseFunctions.GetString(reader, "OSDistro")
				};
				agentInfo2.Name = DatabaseFunctions.GetString(reader, "Name");
				agentInfo2.HostName = DatabaseFunctions.GetString(reader, "HostName");
				agentInfo2.IPAddress = DatabaseFunctions.GetString(reader, "IP");
				if (agentInfo == null || agentInfo.AgentId != agentInfo2.AgentId)
				{
					list.Add(agentInfo2);
					agentInfo = agentInfo2;
				}
				AgentPluginInfo agentPluginInfo = new AgentPluginInfo
				{
					PluginId = DatabaseFunctions.GetString(reader, "PluginId", null)
				};
				if (agentPluginInfo.PluginId != null)
				{
					agentPluginInfo.Status = DatabaseFunctions.GetInt32(reader, "Status");
					agentPluginInfo.StatusMessage = DatabaseFunctions.GetString(reader, "StatusMessage");
					agentInfo.AddPlugin(agentPluginInfo);
				}
			}
			return list;
		}

		// Token: 0x060006AF RID: 1711 RVA: 0x0002A004 File Offset: 0x00028204
		public bool IsUniqueAgentName(string agentName)
		{
			bool result;
			using (InformationServiceConnection informationServiceConnection = InformationServiceConnectionProvider.CreateSystemConnectionV3())
			{
				using (InformationServiceCommand informationServiceCommand = informationServiceConnection.CreateCommand())
				{
					informationServiceCommand.CommandText = "SELECT COUNT(AgentId) AS Cnt FROM Orion.AgentManagement.Agent WHERE Name = @Name";
					informationServiceCommand.Parameters.AddWithValue("Name", agentName);
					using (InformationServiceDataReader informationServiceDataReader = informationServiceCommand.ExecuteReader())
					{
						if (informationServiceDataReader.Read())
						{
							result = ((int)informationServiceDataReader[0] == 0);
						}
						else
						{
							result = true;
						}
					}
				}
			}
			return result;
		}
	}
}
