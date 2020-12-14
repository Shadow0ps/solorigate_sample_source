using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Common.EntityManager;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Common.PackageManager;
using SolarWinds.Orion.Core.Strings;
using SolarWinds.Orion.MacroProcessor;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x020000A2 RID: 162
	public class NodeBLDAL : INodeBLDAL
	{
		// Token: 0x060007CE RID: 1998 RVA: 0x00037750 File Offset: 0x00035950
		public static Node CleanInsertNode(Node node)
		{
			node = NodeDAL.InsertNode(node, false);
			return node;
		}

		// Token: 0x060007CF RID: 1999 RVA: 0x0003775C File Offset: 0x0003595C
		public static void UpdateEntityType(int nodeId, string entityType)
		{
			NodeDAL.UpdateEntityType(nodeId, entityType);
		}

		// Token: 0x060007D0 RID: 2000 RVA: 0x00037765 File Offset: 0x00035965
		public static void CleanUpdateNode(Node node)
		{
			NodeDAL.UpdateNode(node, false);
		}

		// Token: 0x060007D1 RID: 2001 RVA: 0x00037770 File Offset: 0x00035970
		public static Nodes GetNodesWithIPs(string[] addresses)
		{
			StringBuilder stringBuilder = new StringBuilder("SELECT * FROM dbo.Nodes \r\nWHERE Nodes.IP_Address IN ('");
			stringBuilder.Append(string.Join("','", addresses));
			stringBuilder.Append("')");
			Nodes nodes = Collection<int, Node>.FillCollection<Nodes, bool>(new Collection<int, Node>.CreateElementWithParams<bool>(NodeDAL.CreateNode), stringBuilder.ToString(), null, new bool[2]);
			if (nodes.Count > 0)
			{
				foreach (Interface @interface in InterfaceBLDAL.GetNodesInterfaces(nodes.NodesIDs))
				{
					Node node = nodes.FindByID(@interface.NodeID);
					if (node != null)
					{
						node.Interfaces.Add(@interface);
					}
				}
				foreach (Volume volume in VolumeDAL.GetNodesVolumes(nodes.NodesIDs))
				{
					Node node2 = nodes.FindByID(volume.NodeID);
					if (node2 != null)
					{
						node2.Volumes.Add(volume);
					}
				}
			}
			return nodes;
		}

		// Token: 0x060007D2 RID: 2002 RVA: 0x00037890 File Offset: 0x00035A90
		public static Nodes GetNodes()
		{
			return NodeBLDAL.GetNodes(true, true);
		}

		// Token: 0x060007D3 RID: 2003 RVA: 0x0003789C File Offset: 0x00035A9C
		public static Nodes GetNodes(bool includeInterfaces, bool includeVolumes)
		{
			string commandString = "SELECT * FROM dbo.Nodes";
			Nodes nodes = Collection<int, Node>.FillCollection<Nodes, bool>(new Collection<int, Node>.CreateElementWithParams<bool>(NodeBLDAL.CreateNode), commandString, null, new bool[2]);
			if (includeInterfaces && NodeBLDAL._areInterfacesAllowed)
			{
				foreach (Interface @interface in InterfaceBLDAL.GetInterfaces())
				{
					Node node = nodes.FindByID(@interface.NodeID);
					if (node != null)
					{
						node.Interfaces.Add(@interface);
					}
				}
			}
			if (includeVolumes)
			{
				foreach (Volume volume in VolumeDAL.GetVolumes())
				{
					Node node2 = nodes.FindByID(volume.NodeID);
					if (node2 != null)
					{
						node2.Volumes.Add(volume);
					}
				}
			}
			return nodes;
		}

		// Token: 0x060007D4 RID: 2004 RVA: 0x0003798C File Offset: 0x00035B8C
		public static Nodes GetNodesByIds(int[] nodeIds)
		{
			StringBuilder stringBuilder = new StringBuilder();
			string arg = string.Empty;
			foreach (int num in nodeIds)
			{
				stringBuilder.AppendFormat("{0}{1}", arg, num);
				arg = ",";
			}
			return Collection<int, Node>.FillCollection<Nodes, bool>(new Collection<int, Node>.CreateElementWithParams<bool>(NodeBLDAL.CreateNode), string.Format("SELECT * FROM dbo.Nodes WHERE dbo.Nodes.NodeID in ({0})", stringBuilder), null, new bool[2]);
		}

		// Token: 0x060007D5 RID: 2005 RVA: 0x000379F8 File Offset: 0x00035BF8
		public static int GetNodeCount()
		{
			int result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT COUNT(*) FROM Nodes WITH (NOLOCK)"))
			{
				result = (int)SqlHelper.ExecuteScalar(textCommand);
			}
			return result;
		}

		// Token: 0x060007D6 RID: 2006 RVA: 0x00037A3C File Offset: 0x00035C3C
		public static List<string> GetFields()
		{
			return NodeBLDAL._fields;
		}

		// Token: 0x060007D7 RID: 2007 RVA: 0x00037A44 File Offset: 0x00035C44
		public static Dictionary<string, string> GetVendors()
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT DISTINCT Vendor, SysObjectID FROM Nodes WITH (NOLOCK)"))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						string @string = DatabaseFunctions.GetString(dataReader, "SysObjectID");
						if (!dictionary.ContainsKey(@string))
						{
							dictionary.Add(@string, DatabaseFunctions.GetString(dataReader, "Vendor"));
						}
					}
				}
			}
			return dictionary;
		}

		// Token: 0x060007D8 RID: 2008 RVA: 0x00037AD0 File Offset: 0x00035CD0
		public static List<int> GetSortedNodeIDs()
		{
			List<int> list = new List<int>();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT NodeID FROM Nodes WITH (NOLOCK) ORDER BY Vendor, Caption"))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						list.Add(DatabaseFunctions.GetInt32(dataReader, "NodeID"));
					}
				}
			}
			return list;
		}

		// Token: 0x060007D9 RID: 2009 RVA: 0x00037B44 File Offset: 0x00035D44
		public static Node GetNode(int nodeId)
		{
			string commandString = "Select * from dbo.Nodes WHERE Nodes.NodeID=@NodeId";
			SqlParameter[] sqlParamList = new SqlParameter[]
			{
				new SqlParameter("@NodeId", nodeId)
			};
			return Collection<int, Node>.GetCollectionItem<Nodes, bool>(new Collection<int, Node>.CreateElementWithParams<bool>(NodeBLDAL.CreateNode), commandString, sqlParamList, new bool[]
			{
				true,
				true
			});
		}

		// Token: 0x060007DA RID: 2010 RVA: 0x00037B92 File Offset: 0x00035D92
		public static Node CreateNode(IDataReader reader, params bool[] getObjects)
		{
			if (getObjects.Length > 1)
			{
				return NodeBLDAL.CreateNode(reader, getObjects[0], getObjects[1]);
			}
			return null;
		}

		// Token: 0x060007DB RID: 2011 RVA: 0x00037BA8 File Offset: 0x00035DA8
		private static Node CreateNode(IDataReader reader, bool getInterfaces, bool getVolumes)
		{
			Node node = NodeDAL.CreateNode(reader, new bool[]
			{
				getInterfaces,
				getVolumes
			});
			if (getInterfaces)
			{
				node.Interfaces = InterfaceBLDAL.GetNodeInterfaces(node.ID);
			}
			if (getVolumes)
			{
				node.Volumes = VolumeDAL.GetNodeVolumes(node.ID);
			}
			return node;
		}

		// Token: 0x060007DC RID: 2012 RVA: 0x00037BF4 File Offset: 0x00035DF4
		public static Node GetNodeWithOptions(int nodeId, bool includeInterfaces, bool includeVolumes)
		{
			string commandString = "Select * from dbo.Nodes WHERE Nodes.NodeID=@NodeId";
			SqlParameter[] sqlParamList = new SqlParameter[]
			{
				new SqlParameter("@NodeId", nodeId)
			};
			Node collectionItem = Collection<int, Node>.GetCollectionItem<Nodes, bool>(new Collection<int, Node>.CreateElementWithParams<bool>(NodeBLDAL.CreateNode), commandString, sqlParamList, new bool[]
			{
				includeInterfaces,
				includeVolumes
			});
			if (collectionItem != null)
			{
				return collectionItem;
			}
			throw new ArgumentOutOfRangeException("nodeId", string.Format("Node Id {0} does not exist", nodeId));
		}

		// Token: 0x060007DD RID: 2013 RVA: 0x00037C62 File Offset: 0x00035E62
		public static Node InsertNode(Node node, bool allowDuplicates = false)
		{
			node = NodeDAL.InsertNode(node, allowDuplicates);
			new NodesCustomPropertyDAL().UpdateCustomProperties(node);
			NodeSettingsDAL.InsertSettings(node.ID, node.NodeSettings);
			return node;
		}

		// Token: 0x060007DE RID: 2014 RVA: 0x00037C8A File Offset: 0x00035E8A
		public static void UpdateNode(Node node)
		{
			if (NodeDAL.UpdateNode(node, false) > 0)
			{
				NodeSettingsDAL.InsertSettings(node.ID, node.NodeSettings);
				new NodesCustomPropertyDAL().UpdateCustomProperties(node);
			}
		}

		// Token: 0x060007DF RID: 2015 RVA: 0x00037CB4 File Offset: 0x00035EB4
		public static void DeleteNode(int nodeId)
		{
			Node node = NodeBLDAL.GetNode(nodeId);
			if (node != null)
			{
				NodeBLDAL.DeleteNode(node);
				NodeSettingsDAL.DeleteNodeSettings(nodeId);
				NodeNotesDAL.DeleteNodeNotes(nodeId);
				return;
			}
			throw new ArgumentOutOfRangeException("nodeId", string.Format("Node Id {0} does not exist", nodeId));
		}

		// Token: 0x060007E0 RID: 2016 RVA: 0x00037CF8 File Offset: 0x00035EF8
		public static void DeleteNode(Node node)
		{
			NodeDAL.DeleteNode(node);
			if (node.Volumes != null)
			{
				foreach (Volume volume in node.Volumes)
				{
					VolumeDAL.DeleteVolume(volume);
				}
			}
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("DELETE FROM Pollers WHERE NetObjectType=@NetObjectType AND NetObjectID=@NetObjectID"))
			{
				textCommand.Parameters.Add("@NetObjectType", SqlDbType.VarChar, 5).Value = "N";
				textCommand.Parameters.Add("@NetObjectID", SqlDbType.Int).Value = node.ID;
				SqlHelper.ExecuteNonQuery(textCommand);
			}
			EventsDAL.InsertEvent(node.ID, node.ID, "N", 17, string.Format(Resources.COREBLCODE_IB0_1, node.Caption));
		}

		// Token: 0x060007E1 RID: 2017 RVA: 0x00037DE0 File Offset: 0x00035FE0
		public static float GetAvailability(int nodeID, DateTime startDate, DateTime endDate)
		{
			SqlCommand textCommand = SqlHelper.GetTextCommand("select avg(Availability)  from [ResponseTime] where (NodeID = @NodeID) and (DateTime > @StartDate) and (DateTime < @EndDate)");
			textCommand.Parameters.Add("@NodeID", SqlDbType.Int).Value = nodeID;
			textCommand.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate;
			textCommand.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate;
			float result = -1f;
			float.TryParse(SqlHelper.ExecuteScalar(textCommand).ToString(), out result);
			return result;
		}

		// Token: 0x060007E2 RID: 2018 RVA: 0x00037E64 File Offset: 0x00036064
		public static bool IsNodeWireless(int nodeId)
		{
			return NodeBLDAL.IsPollingEnabled(nodeId, "N.Wireless%");
		}

		// Token: 0x060007E3 RID: 2019 RVA: 0x00037E71 File Offset: 0x00036071
		public static bool IsNodeEnergyWise(int nodeId)
		{
			return NodeBLDAL.IsPollingEnabled(nodeId, "N.EnergyWise%");
		}

		// Token: 0x060007E4 RID: 2020 RVA: 0x00037E80 File Offset: 0x00036080
		private static bool IsPollingEnabled(int nodeId, string pollerTypePattern)
		{
			bool result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT PollerID from Pollers WHERE PollerType LIKE @pollerTypePattern AND NetObjectID = @nodeId AND NetObjectType ='N'"))
			{
				textCommand.Parameters.AddWithValue("@nodeId", nodeId);
				textCommand.Parameters.AddWithValue("@pollerTypePattern", pollerTypePattern);
				result = Convert.ToBoolean(SqlHelper.ExecuteScalar(textCommand));
			}
			return result;
		}

		// Token: 0x060007E5 RID: 2021 RVA: 0x0000AB3C File Offset: 0x00008D3C
		public static NodeHardwareType GetNodeHardwareType(int nodeId)
		{
			return NodeHardwareType.Physical;
		}

		// Token: 0x060007E6 RID: 2022 RVA: 0x00037EEC File Offset: 0x000360EC
		public static void BulkUpdateNodePollingInterval(int pollInterval, int engineId)
		{
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("IF (@engineID <=0) UPDATE NodesData SET PollInterval = @PollInterval ELSE UPDATE NodesData SET PollInterval = @PollInterval WHERE EngineID = @engineID"))
			{
				textCommand.Parameters.Add("@PollInterval", SqlDbType.Int, 4).Value = pollInterval;
				textCommand.Parameters.Add("@engineID", SqlDbType.Int, 4).Value = engineId;
				SqlHelper.ExecuteNonQuery(textCommand);
			}
		}

		// Token: 0x060007E7 RID: 2023 RVA: 0x00037F64 File Offset: 0x00036164
		public static List<KeyValuePair<Uri, List<int>>> EnumerateNodeEngineAssigments()
		{
			return NodeDAL.EnumerateNodeEngineAssigments();
		}

		// Token: 0x060007E8 RID: 2024 RVA: 0x00037F6B File Offset: 0x0003616B
		public static List<int> EnumerateUnmanagedNodes()
		{
			return NodeDAL.EnumerateUnmanagedNodes();
		}

		// Token: 0x060007E9 RID: 2025 RVA: 0x00037F74 File Offset: 0x00036174
		internal static Dictionary<string, int> GetValuesAndCountsForPropertyFiltered(string property, string accountId, Dictionary<string, object> filters)
		{
			List<string> list = new List<string>(filters.Count);
			List<SqlParameter> list2 = new List<SqlParameter>(filters.Count);
			foreach (KeyValuePair<string, object> keyValuePair in filters)
			{
				list.Add(string.Format("ISNULL(Nodes.[{0}],'')=@{0}", keyValuePair.Key));
				list2.Add(new SqlParameter(keyValuePair.Key, keyValuePair.Value ?? string.Empty));
			}
			string text = string.Empty;
			if (list.Count > 0)
			{
				text = " WHERE " + string.Join(" AND ", list.ToArray());
			}
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(Limitation.LimitSQL(string.Format(string.Concat(new string[]
			{
				"SELECT rtrim(ltrim({0})), Count({0}), {0}, ",
				property,
				" FROM Nodes WITH (NOLOCK)",
				text,
				" GROUP BY rtrim(ltrim({0})), {0}, ",
				property
			}), string.Format("ISNULL({0},'')", property)), accountId)))
			{
				textCommand.Parameters.AddRange(list2.ToArray());
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						if (dataReader[2] is bool)
						{
							dictionary.Add(dataReader[2].ToString(), DatabaseFunctions.GetInt32(dataReader, 1));
						}
						else if (dataReader[2] is int)
						{
							dictionary.Add(dataReader[3].ToString(), DatabaseFunctions.GetInt32(dataReader, 1));
						}
						else if (dataReader[2] is float)
						{
							if (string.IsNullOrEmpty(dataReader[3].ToString()))
							{
								dictionary.Add(dataReader[3].ToString(), DatabaseFunctions.GetInt32(dataReader, 1));
							}
							else
							{
								dictionary.Add(DatabaseFunctions.GetFloat(dataReader, 2).ToString(CultureInfo.InvariantCulture.NumberFormat), DatabaseFunctions.GetInt32(dataReader, 1));
							}
						}
						else if (dataReader[2] is DateTime)
						{
							if (dataReader[3].GetType() == typeof(DBNull))
							{
								dictionary.Add(dataReader[3].ToString(), DatabaseFunctions.GetInt32(dataReader, 1));
							}
							else
							{
								dictionary.Add(dataReader[0].ToString(), DatabaseFunctions.GetInt32(dataReader, 1));
							}
						}
						else
						{
							dictionary.Add(dataReader[0].ToString(), DatabaseFunctions.GetInt32(dataReader, 1));
						}
					}
				}
			}
			return dictionary;
		}

		// Token: 0x060007EA RID: 2026 RVA: 0x00038260 File Offset: 0x00036460
		internal static Dictionary<string, int> GetValuesAndCountsForProperty(string property, string accountId)
		{
			return NodeBLDAL.GetValuesAndCountsForProperty(property, accountId, new CultureInfo("en-us"));
		}

		// Token: 0x060007EB RID: 2027 RVA: 0x00038274 File Offset: 0x00036474
		internal static Dictionary<string, int> GetValuesAndCountsForProperty(string property, string accountId, CultureInfo culture)
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(Limitation.LimitSQL(string.Format("SELECT {0}, Count({0}), " + property + " FROM Nodes WITH (NOLOCK) GROUP BY {0}, " + property, string.Format("ISNULL({0},'')", property)), accountId)))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						if (dataReader[0].GetType() == typeof(DateTime))
						{
							dictionary.Add(dataReader.GetDateTime(0).ToString(culture), DatabaseFunctions.GetInt32(dataReader, 1));
						}
						else if (dataReader[0] is float)
						{
							if (string.IsNullOrEmpty(dataReader[2].ToString()))
							{
								dictionary.Add(dataReader[2].ToString(), DatabaseFunctions.GetInt32(dataReader, 1));
							}
							else
							{
								dictionary.Add(dataReader.GetFloat(0).ToString(culture.NumberFormat), DatabaseFunctions.GetInt32(dataReader, 1));
							}
						}
						else if (dataReader[0] is int || dataReader[0] is byte)
						{
							if (string.IsNullOrEmpty(dataReader[2].ToString()))
							{
								dictionary.Add(dataReader[2].ToString(), DatabaseFunctions.GetInt32(dataReader, 1));
							}
							else
							{
								dictionary.Add(dataReader[0].ToString(), DatabaseFunctions.GetInt32(dataReader, 1));
							}
						}
						else
						{
							dictionary.Add(dataReader[0].ToString(), DatabaseFunctions.GetInt32(dataReader, 1));
						}
					}
				}
			}
			return dictionary;
		}

		// Token: 0x060007EC RID: 2028 RVA: 0x00038438 File Offset: 0x00036638
		internal static List<string> GetNodeDistinctValuesForField(string fieldName)
		{
			List<string> list = new List<string>();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT DISTINCT " + fieldName + " AS Field FROM Nodes WITH (NOLOCK)"))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						list.Add(dataReader["Field"].ToString());
					}
				}
			}
			return list;
		}

		// Token: 0x060007ED RID: 2029 RVA: 0x000384BC File Offset: 0x000366BC
		internal static Nodes GetNodesFiltered(Dictionary<string, object> filterValues, bool includeInterfaces, bool includeVolumes)
		{
			List<string> list = new List<string>(filterValues.Count);
			List<SqlParameter> list2 = new List<SqlParameter>(filterValues.Count);
			string arg = "''";
			foreach (KeyValuePair<string, object> keyValuePair in filterValues)
			{
				if (keyValuePair.Value != null && !string.IsNullOrEmpty(keyValuePair.Value.ToString()) && CustomPropertyMgr.IsCustom("NodesCustomProperties", keyValuePair.Key))
				{
					try
					{
						string a = CustomPropertyMgr.GetTypeForProp("NodesCustomProperties", keyValuePair.Key).Name.ToLowerInvariant();
						if (a == "single" || a == "float" || a == "double")
						{
							double num;
							if (!double.TryParse(keyValuePair.Value.ToString(), out num))
							{
								string text = keyValuePair.Value.ToString();
								if (text.Contains("."))
								{
									text = text.Replace(".", ",");
								}
								else if (text.Contains(","))
								{
									text = text.Replace(",", ".");
								}
								double.TryParse(text, out num);
							}
							list.Add(string.Format("ISNULL(Nodes.[{0}],{1})=@{0}", keyValuePair.Key, arg));
							list2.Add(new SqlParameter(keyValuePair.Key, keyValuePair.Value));
							continue;
						}
					}
					catch (Exception ex)
					{
						NodeBLDAL.log.Error("Error while trying to convert float custom property:", ex);
					}
				}
				list.Add(string.Format("ISNULL(Nodes.[{0}],{1})=@{0}", keyValuePair.Key, arg));
				if (keyValuePair.Value != null && (keyValuePair.Value.ToString().ToLowerInvariant().Equals("true") || keyValuePair.Value.ToString().ToLowerInvariant().Equals("false")))
				{
					list2.Add(new SqlParameter(keyValuePair.Key, keyValuePair.Value.ToString().ToLowerInvariant().Equals("true") ? "1" : "0"));
				}
				else
				{
					list2.Add(new SqlParameter(keyValuePair.Key, keyValuePair.Value ?? string.Empty));
				}
			}
			string str = string.Empty;
			if (list.Count > 0)
			{
				str = " WHERE " + string.Join(" AND ", list.ToArray());
			}
			string commandString = "SELECT * from dbo.Nodes " + str + " ORDER BY Caption";
			Nodes nodes = Collection<int, Node>.FillCollection<Nodes, bool>(new Collection<int, Node>.CreateElementWithParams<bool>(NodeBLDAL.CreateNode), commandString, list2.ToArray(), new bool[2]);
			if (includeInterfaces && NodeBLDAL._areInterfacesAllowed)
			{
				using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT Interfaces.* FROM Interfaces \r\nINNER JOIN Nodes on Interfaces.NodeID=Nodes.NodeID" + str + " ORDER BY Interfaces.Caption"))
				{
					foreach (SqlParameter sqlParameter in list2)
					{
						textCommand.Parameters.AddWithValue(sqlParameter.ParameterName, sqlParameter.Value);
					}
					using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
					{
						while (dataReader.Read())
						{
							Interface @interface = InterfaceBLDAL.CreateInterface(dataReader);
							nodes.FindByID(@interface.NodeID).Interfaces.Add(@interface);
						}
					}
				}
			}
			if (includeVolumes)
			{
				using (SqlCommand textCommand2 = SqlHelper.GetTextCommand("SELECT Volumes.* FROM Volumes \r\nINNER JOIN Nodes on Volumes.NodeID=Nodes.NodeID" + str + " ORDER BY Volumes.Caption"))
				{
					foreach (SqlParameter sqlParameter2 in list2)
					{
						textCommand2.Parameters.AddWithValue(sqlParameter2.ParameterName, sqlParameter2.Value);
					}
					using (IDataReader dataReader2 = SqlHelper.ExecuteReader(textCommand2))
					{
						while (dataReader2.Read())
						{
							Volume volume = VolumeDAL.CreateVolume(dataReader2);
							nodes.FindByID(volume.NodeID).Volumes.Add(volume);
						}
					}
				}
			}
			return nodes;
		}

		// Token: 0x060007EE RID: 2030 RVA: 0x000389AC File Offset: 0x00036BAC
		internal static Dictionary<string, string> GetVendorIconFileNames()
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT DISTINCT Vendor, VendorIcon FROM Nodes WITH (NOLOCK)"))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						dictionary[DatabaseFunctions.GetString(dataReader, 0)] = DatabaseFunctions.GetString(dataReader, 1).Trim();
					}
				}
			}
			return dictionary;
		}

		// Token: 0x060007EF RID: 2031 RVA: 0x00038A28 File Offset: 0x00036C28
		internal static void PopulateWebCommunityStrings()
		{
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("INSERT WebCommunityStrings\r\nSELECT NEWID() AS [GUID], Nodes.Community AS CommunityString FROM Nodes\r\nWHERE NOT EXISTS (SELECT * FROM WebCommunityStrings WHERE Nodes.Community = WebCommunityStrings.CommunityString)\r\nGROUP BY Nodes.Community"))
			{
				int num = SqlHelper.ExecuteNonQuery(textCommand);
				if (num > 0)
				{
					NodeBLDAL.log.InfoFormat("Added {0} rows to WebCommunityStrings.", num);
				}
				else
				{
					NodeBLDAL.log.Debug("PopulateWebCommunityStrings - no rows added.");
				}
			}
		}

		// Token: 0x060007F0 RID: 2032 RVA: 0x00038A90 File Offset: 0x00036C90
		public static Dictionary<string, object> GetNodeCustomProperties(int nodeId, ICollection<string> properties)
		{
			Node node = NodeBLDAL.GetNode(nodeId);
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (properties == null || properties.Count == 0)
			{
				properties = node.CustomProperties.Keys;
			}
			MacroParser macroParser = new MacroParser(new Action<string, int>(BusinessLayerOrionEvent.WriteEvent))
			{
				ObjectType = "Node",
				ActiveObject = node.ID.ToString(),
				NodeID = node.ID,
				NodeName = node.Name
			};
			using (macroParser.MyDBConnection = DatabaseFunctions.CreateConnection())
			{
				foreach (string text in properties)
				{
					string key = text.Trim();
					if (node.CustomProperties.ContainsKey(key))
					{
						object obj = node.CustomProperties[key];
						if (obj != null && obj.ToString().Contains("${"))
						{
							dictionary[key] = macroParser.ParseMacros(obj.ToString(), false);
						}
						else
						{
							dictionary[key] = obj;
						}
					}
				}
			}
			return dictionary;
		}

		// Token: 0x060007F1 RID: 2033 RVA: 0x00038BCC File Offset: 0x00036DCC
		public static DataTable GetPagebleNodes(string property, string proptype, string val, string column, string direction, int number, int size, string searchText)
		{
			size = Math.Max(size, 15);
			bool flag = EntityManager.InstanceWithCache.IsThereEntity("Orion.NPM.EW.Entity");
			DataTable dataTable;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Format("\r\nSelect [NodeID], [IP_Address], [Caption], [Status], [StatusLED]\r\nfrom (SELECT N.*, ROW_NUMBER() OVER (ORDER BY {0} {1}) RowNr from [Nodes] N {4} WHERE {5}) t\r\nWHERE RowNr BETWEEN {2} AND {3} \r\nORDER BY {0} {1}", new object[]
			{
				string.IsNullOrEmpty(column) ? "Caption" : column,
				string.IsNullOrEmpty(direction) ? "ASC" : direction,
				number + 1,
				number + size,
				flag ? " LEFT JOIN NPM_NV_EW_NODES_V EW ON (EW.NodeID = N.NodeID) " : "",
				NodeBLDAL.GetWhere("N", property, proptype, val) + (string.IsNullOrEmpty(searchText) ? string.Empty : string.Format(" And (N.Caption Like '{0}')", searchText))
			})))
			{
				dataTable = SqlHelper.ExecuteDataTable(textCommand);
			}
			if (dataTable != null)
			{
				dataTable.TableName = "PagebleNodes";
			}
			return dataTable;
		}

		// Token: 0x060007F2 RID: 2034 RVA: 0x00038CC0 File Offset: 0x00036EC0
		public static int GetNodesCount(string property, string proptype, string val, string searchText)
		{
			bool flag = EntityManager.InstanceWithCache.IsThereEntity("Orion.NPM.EW.Entity");
			int result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Format("SELECT COUNT(N.NodeID) AS TotalCount FROM [Nodes] N {0} WHERE {1}", flag ? " LEFT JOIN NPM_NV_EW_NODES_V EW ON (EW.NodeID = N.NodeID) " : "", NodeBLDAL.GetWhere("N", property, proptype, val) + (string.IsNullOrEmpty(searchText) ? string.Empty : string.Format(" And (N.Caption Like '{0}')", searchText)))))
			{
				result = Convert.ToInt32(SqlHelper.ExecuteScalar(textCommand));
			}
			return result;
		}

		// Token: 0x060007F3 RID: 2035 RVA: 0x00038D54 File Offset: 0x00036F54
		public static DataTable GetGroupsByNodeProperty(string property, string propertyType)
		{
			string text = string.Empty;
			if ("SYSTEM.SINGLE,SYSTEM.INT32,SYSTEM.DATETIME".Contains(propertyType.ToUpperInvariant()))
			{
				text = string.Format("SELECT N.{0} AS Value, COUNT(ISNULL(N.{0},'')) AS Cnt FROM Nodes N WITH (NOLOCK) GROUP BY N.{0} ORDER BY N.{0} ASC", property);
			}
			else
			{
				text = string.Format("SELECT ISNULL(N.{0},'') AS Value, COUNT(ISNULL(N.{0},'')) AS Cnt FROM Nodes N WITH (NOLOCK) GROUP BY ISNULL(N.{0},'') ORDER BY ISNULL(N.{0},'') ASC", property);
			}
			if (property.Equals("EnergyWise", StringComparison.OrdinalIgnoreCase))
			{
				text = "SELECT EW.EnergyWise AS Value, COUNT(ISNULL(EW.EnergyWise,'')) AS Cnt FROM NPM_NV_EW_NODES_V EW LEFT JOIN Nodes N ON (EW.NodeID = N.NodeID) GROUP BY EW.EnergyWise";
			}
			DataTable dataTable = new DataTable();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(text))
			{
				dataTable = SqlHelper.ExecuteDataTable(textCommand);
			}
			if (dataTable != null)
			{
				dataTable.TableName = "GroupsByNodeProperty";
			}
			return dataTable;
		}

		// Token: 0x060007F4 RID: 2036 RVA: 0x00038DE8 File Offset: 0x00036FE8
		public static string GetWhere(string tableAlias, string prop, string type, string value)
		{
			string text = "System.Single,System.Double,System.Int32";
			if (string.IsNullOrEmpty(prop))
			{
				return "1=1";
			}
			if (prop.Equals("EnergyWise", StringComparison.OrdinalIgnoreCase))
			{
				tableAlias = "EW";
			}
			if (prop.Equals("MachineType", StringComparison.OrdinalIgnoreCase) && (value.Equals("null", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(value.Trim())))
			{
				return string.Format(" ({0}.{1} IS NULL OR {0}.{1}='' OR {0}.{1}='{2}') ", tableAlias, prop, Resources.COREBUSINESSLAYERDAL_CODE_YK0_8);
			}
			if (!value.Equals("null", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value.Trim()))
			{
				return string.Format(" {0}.{1}='{2}' ", tableAlias, prop, value.Replace("'", "''"));
			}
			if (!string.IsNullOrEmpty(type) && text.Contains(type))
			{
				return string.Format(" ({0}.{1} IS NULL) ", tableAlias, prop);
			}
			return string.Format(" ({0}.{1} IS NULL OR {0}.{1}='') ", tableAlias, prop);
		}

		// Token: 0x060007F5 RID: 2037 RVA: 0x00038EC0 File Offset: 0x000370C0
		public static DataTable GetNodeCPUsByPercentLoad(int nodeId, int pageNumber, int pageSize)
		{
			DataTable dataTable;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Format("declare @@cpuCount int\r\n\r\nSelect @@cpuCount = count(Distinct CPUIndex)  FROM CPUMultiLoad\r\nWHERE NodeID = @NodeId\r\n\r\nif @@cpuCount <= 1\r\n Select top 0 CPUIndex AS CPUName, CPUIndex, AvgLoad, TimeStampUTC from CPUMultiLoad\r\nelse\r\nSelect * from\r\n(\r\nSelect ROW_NUMBER() OVER (order by z.AvgLoad DESC) as rnbr, * from\r\n(\r\nSELECT N'{0}' + CAST(CPUIndex + 1 as varchar(6)) as CPUName, CPUIndex, AvgLoad, TimeStampUTC,\r\n DENSE_RANK() OVER (PARTITION BY CPUIndex ORDER BY TimeStampUTC desc) AS Rank\r\n FROM CPUMultiLoad\r\nWHERE NodeID = @NodeId) as z\r\nwhere z.Rank = 1\r\n) t1 where t1.rnbr >((@PageNumber-1)*@PageSize) and t1.rnbr <=((@PageNumber)*@PageSize)\r\nORDER BY AvgLoad DESC\r\n", string.Format(Resources.LIBCODE_IB0_1, string.Empty))))
			{
				textCommand.Parameters.AddWithValue("@NodeId", nodeId);
				textCommand.Parameters.AddWithValue("@PageNumber", pageNumber);
				textCommand.Parameters.AddWithValue("@PageSize", pageSize);
				dataTable = SqlHelper.ExecuteDataTable(textCommand);
			}
			if (dataTable != null)
			{
				dataTable.TableName = "NodeCPUsByPercentLoad";
			}
			return dataTable;
		}

		// Token: 0x060007F6 RID: 2038 RVA: 0x00038F64 File Offset: 0x00037164
		public static DataTable GetNodesCpuIndexCounts(List<string> nodeIds)
		{
			if (nodeIds == null || nodeIds.Count == 0)
			{
				return null;
			}
			DataTable dataTable;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Format("SELECT DISTINCT(z.NodeID) as ID, z.Caption, COUNT(z.CPUIndex) As Cnt from\r\n(SELECT cpu.NodeID, cpu.CPUIndex, n.Caption, cpu.TimeStampUTC,\r\n DENSE_RANK() OVER (PARTITION BY cpu.NodeID ORDER BY cpu.TimeStampUTC desc) AS [Rank]\r\n FROM CPUMultiLoad cpu\r\n INNER JOIN Nodes n ON cpu.NodeID = n.NodeID\r\nWHERE cpu.NodeID in ({0}) \r\n) as z\r\nwhere z.Rank = 1\r\nGROUP BY z.NodeID, z.Caption\r\nORDER BY z.Caption", string.Join(",", nodeIds.ToArray()))))
			{
				dataTable = SqlHelper.ExecuteDataTable(textCommand);
			}
			if (dataTable != null)
			{
				dataTable.TableName = "NodeCPUsByPercentLoad";
			}
			return dataTable;
		}

		// Token: 0x060007F7 RID: 2039 RVA: 0x00038FD0 File Offset: 0x000371D0
		public static void UpdateNodeProperty(IDictionary<string, object> properties, int nodeId)
		{
			if (properties == null)
			{
				throw new ArgumentNullException("Node properties must be set");
			}
			if (properties.Count == 0)
			{
				return;
			}
			List<string> list = new List<string>
			{
				"ObjectSubType",
				"IP_Address",
				"IP_Address_Type",
				"DynamicIP",
				"UnManaged",
				"UnManageFrom",
				"UnManageUntil",
				"Caption",
				"DNS",
				"Community",
				"RWCommunity",
				"SysName",
				"Vendor",
				"SysObjectID",
				"Description",
				"Location",
				"Contact",
				"RediscoveryInterval",
				"PollInterval",
				"VendorIcon",
				"IOSImage",
				"IOSVersion",
				"GroupStatus",
				"StatusDescription",
				"Status",
				"StatusLED",
				"ChildStatus",
				"EngineID",
				"MachineType",
				"Severity",
				"StatCollection",
				"Allow64BitCounters",
				"SNMPV2Only",
				"AgentPort",
				"SNMPVersion",
				"SNMPV3Username",
				"SNMPV3Context",
				"SNMPV3PrivMethod",
				"SNMPV3PrivKey",
				"SNMPV3PrivKeyIsPwd",
				"SNMPV3AuthMethod",
				"SNMPV3AuthKey",
				"SNMPV3AuthKeyIsPwd",
				"RWSNMPV3Username",
				"RWSNMPV3Context",
				"RWSNMPV3PrivMethod",
				"RWSNMPV3PrivKey",
				"RWSNMPV3PrivKeyIsPwd",
				"RWSNMPV3AuthMethod",
				"RWSNMPV3AuthKey",
				"RWSNMPV3AuthKeyIsPwd",
				"TotalMemory",
				"External",
				"EntityType",
				"CMTS",
				"BlockUntil",
				"IPAddressGUID"
			};
			List<string> list2 = new List<string>
			{
				"LastBoot",
				"SystemUpTime",
				"LastSystemUpTimePollUtc",
				"ResponseTime",
				"PercentLoss",
				"AvgResponseTime",
				"MinResponseTime",
				"MaxResponseTime",
				"NextPoll",
				"LastSync",
				"NextRediscovery",
				"CPULoad",
				"MemoryUsed",
				"PercentMemoryUsed",
				"BufferNoMemThisHour",
				"BufferNoMemToday",
				"BufferSmMissThisHour",
				"BufferSmMissToday",
				"BufferMdMissThisHour",
				"BufferMdMissToday",
				"BufferBgMissThisHour",
				"BufferBgMissToday",
				"BufferLgMissThisHour",
				"BufferLgMissToday",
				"BufferHgMissThisHour",
				"BufferHgMissToday",
				"CustomPollerLastStatisticsPoll",
				"CustomPollerLastStatisticsPollSuccess"
			};
			List<string> list3 = new List<string>();
			List<string> list4 = new List<string>();
			List<SqlParameter> list5 = new List<SqlParameter>(properties.Count);
			foreach (string text in properties.Keys)
			{
				if (list.Contains(text))
				{
					list3.Add(string.Format("[{0}]=@{0}", text));
				}
				if (list2.Contains(text))
				{
					list4.Add(string.Format("[{0}]=@{0}", text));
				}
				if (properties[text] == null || properties[text] == DBNull.Value || string.IsNullOrEmpty(properties[text].ToString()))
				{
					list5.Add(new SqlParameter("@" + text, DBNull.Value));
				}
				else
				{
					list5.Add(new SqlParameter("@" + text, properties[text]));
				}
			}
			string text2 = "";
			if (list3.Count > 0 && list4.Count > 0)
			{
				text2 = string.Format("UPDATE [NodesData] SET {0} WHERE NodeID=@node; UPDATE [NodesStatistics] SET {1} WHERE NodeID=@node;", string.Join(", ", list3.ToArray()), string.Join(", ", list4.ToArray()));
			}
			if (list3.Count > 0 && list4.Count == 0)
			{
				text2 = string.Format("UPDATE [NodesData] SET {0} WHERE NodeID=@node", string.Join(", ", list3.ToArray()));
			}
			if (list3.Count == 0 && list4.Count > 0)
			{
				text2 = string.Format("UPDATE [NodesStatistics] SET {0} WHERE NodeID=@node", string.Join(", ", list4.ToArray()));
			}
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(text2))
			{
				foreach (SqlParameter value in list5)
				{
					textCommand.Parameters.Add(value);
				}
				textCommand.Parameters.AddWithValue("node", nodeId);
				SqlHelper.ExecuteNonQuery(textCommand);
			}
		}

		// Token: 0x060007F8 RID: 2040 RVA: 0x000395D4 File Offset: 0x000377D4
		public static void DeleteShadowNodes(string ipAddress)
		{
			if (string.IsNullOrEmpty(ipAddress) || string.IsNullOrWhiteSpace(ipAddress))
			{
				return;
			}
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("DELETE FROM ShadowNodes WHERE IPAddress=@IPAddress"))
			{
				textCommand.Parameters.AddWithValue("IPAddress", ipAddress);
				SqlHelper.ExecuteNonQuery(textCommand);
			}
		}

		// Token: 0x060007F9 RID: 2041 RVA: 0x0000CB18 File Offset: 0x0000AD18
		Node INodeBLDAL.GetNode(int nodeId)
		{
			return NodeBLDAL.GetNode(nodeId);
		}

		// Token: 0x060007FA RID: 2042 RVA: 0x0000CB30 File Offset: 0x0000AD30
		Node INodeBLDAL.GetNodeWithOptions(int nodeId, bool includeInterfaces, bool includeVolumes)
		{
			return NodeBLDAL.GetNodeWithOptions(nodeId, includeInterfaces, includeVolumes);
		}

		// Token: 0x060007FB RID: 2043 RVA: 0x00039634 File Offset: 0x00037834
		void INodeBLDAL.UpdateNode(Node node)
		{
			NodeBLDAL.UpdateNode(node);
		}

		// Token: 0x060007FC RID: 2044 RVA: 0x0003963C File Offset: 0x0003783C
		Nodes INodeBLDAL.GetNodes(bool includeInterfaces, bool includeVolumes)
		{
			return NodeBLDAL.GetNodes(includeInterfaces, includeVolumes);
		}

		// Token: 0x060007FD RID: 2045 RVA: 0x00039645 File Offset: 0x00037845
		void INodeBLDAL.UpdateNode(IDictionary<string, object> properties, int nodeId)
		{
			NodeBLDAL.UpdateNodeProperty(properties, nodeId);
		}

		// Token: 0x060007FE RID: 2046 RVA: 0x00039650 File Offset: 0x00037850
		public static bool AddNodeNote(int nodeId, string accountId, string note, DateTime modificationDateTime)
		{
			int? num = null;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Format("INSERT INTO [NodeNotes] ({0}) OUTPUT inserted.NodeNoteID VALUES ({1})", "Note, NodeID, AccountID, TimeStamp", "@Note, @NodeID, @AccountID, @TimeStamp")))
			{
				textCommand.Parameters.AddWithValue("@Note", note);
				textCommand.Parameters.AddWithValue("@NodeID", nodeId);
				textCommand.Parameters.AddWithValue("@AccountID", accountId);
				textCommand.Parameters.AddWithValue("@TimeStamp", modificationDateTime);
				using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
				{
					object obj = SqlHelper.ExecuteScalar(textCommand, sqlConnection);
					if (obj != null && obj != DBNull.Value)
					{
						num = new int?(Convert.ToInt32(obj));
					}
				}
			}
			return num != null;
		}

		// Token: 0x0400024F RID: 591
		private static readonly Log log = new Log();

		// Token: 0x04000250 RID: 592
		private static readonly List<string> _fields = new List<string>
		{
			"DynamicIP",
			"UnManaged",
			"UnManageFrom",
			"CommunityString",
			"RWCommunity",
			"Vendor",
			"SysObjectID",
			"Location",
			"Contact",
			"RediscoveryInterval",
			"PollInterval",
			"IOSImage",
			"IOSVersion",
			"StatusDescription",
			"Status",
			"MachineType",
			"Allow64BitCounters",
			"SNMPPort",
			"SNMPVersion",
			"SNMPV3Username",
			"SNMPV3Context",
			"VMwareProductName",
			"VMwareProductVersion",
			"VMPollingJobID",
			"VMServiceURIID",
			"VMCredentialID",
			"External",
			"ChildStatus",
			"IsServer"
		};

		// Token: 0x04000251 RID: 593
		private static bool _areInterfacesAllowed = PackageManager.InstanceWithCache.IsPackageInstalled("Orion.Interfaces");

		// Token: 0x04000252 RID: 594
		private const string Columns = "NodeID, Caption, DNS, Vendor, IP_Address, EngineID, Community, VendorIcon, GroupStatus, \r\n\t\t\tSNMPVersion, AgentPort, SNMPV3Username, SNMPV3AuthKey, SNMPV3PrivKey, SNMPV3AuthMethod, SNMPV3PrivMethod, \r\n\t\t\tObjectSubType, StatCollection, PollInterval, UnManaged, UnManageFrom, UnManageUntil, NextRediscovery, NextPoll, LastBoot, SysObjectID, Description, Location, Contact, IOSImage, IOSVersion, StatusDescription, Status, MachineType,\r\n\t\t\tAllow64BitCounters, DynamicIP,RediscoveryInterval, RWCommunity, SNMPV3Context, RWSNMPV3Username, RWSNMPV3Context, RWSNMPV3PrivMethod, RWSNMPV3PrivKey, RWSNMPV3AuthMethod, RWSNMPV3AuthKey, External, ChildStatus, IsServer";

		// Token: 0x0200019A RID: 410
		private enum ColumnOrder
		{
			// Token: 0x04000518 RID: 1304
			ID,
			// Token: 0x04000519 RID: 1305
			Name,
			// Token: 0x0400051A RID: 1306
			DNS,
			// Token: 0x0400051B RID: 1307
			Vendor,
			// Token: 0x0400051C RID: 1308
			IpAddress,
			// Token: 0x0400051D RID: 1309
			EngineID,
			// Token: 0x0400051E RID: 1310
			Community,
			// Token: 0x0400051F RID: 1311
			VendorIcon,
			// Token: 0x04000520 RID: 1312
			GroupStatus,
			// Token: 0x04000521 RID: 1313
			SNMPVersion,
			// Token: 0x04000522 RID: 1314
			SNMPPort,
			// Token: 0x04000523 RID: 1315
			SNMPV3Username,
			// Token: 0x04000524 RID: 1316
			SNMPV3AuthKey,
			// Token: 0x04000525 RID: 1317
			SNMPV3PrivKey,
			// Token: 0x04000526 RID: 1318
			SNMPV3AuthType,
			// Token: 0x04000527 RID: 1319
			SNMPV3PrivType,
			// Token: 0x04000528 RID: 1320
			ObjectSubType,
			// Token: 0x04000529 RID: 1321
			StatCollection,
			// Token: 0x0400052A RID: 1322
			PollInterval,
			// Token: 0x0400052B RID: 1323
			UnManaged,
			// Token: 0x0400052C RID: 1324
			UnManageFrom,
			// Token: 0x0400052D RID: 1325
			UnManageUntil,
			// Token: 0x0400052E RID: 1326
			NextRediscovery,
			// Token: 0x0400052F RID: 1327
			NextPoll,
			// Token: 0x04000530 RID: 1328
			LastBoot,
			// Token: 0x04000531 RID: 1329
			SysObjectID,
			// Token: 0x04000532 RID: 1330
			Description,
			// Token: 0x04000533 RID: 1331
			Location,
			// Token: 0x04000534 RID: 1332
			Contact,
			// Token: 0x04000535 RID: 1333
			IOSImage,
			// Token: 0x04000536 RID: 1334
			IOSVersion,
			// Token: 0x04000537 RID: 1335
			StatusDescription,
			// Token: 0x04000538 RID: 1336
			Status,
			// Token: 0x04000539 RID: 1337
			MachineType,
			// Token: 0x0400053A RID: 1338
			Allow64BitCounters,
			// Token: 0x0400053B RID: 1339
			DynamicIP,
			// Token: 0x0400053C RID: 1340
			RediscoveryInterval,
			// Token: 0x0400053D RID: 1341
			RWCommunity,
			// Token: 0x0400053E RID: 1342
			SNMPV3Context,
			// Token: 0x0400053F RID: 1343
			RWSNMPV3Username,
			// Token: 0x04000540 RID: 1344
			RWSNMPV3Context,
			// Token: 0x04000541 RID: 1345
			RWSNMPV3PrivMethod,
			// Token: 0x04000542 RID: 1346
			RWSNMPV3PrivKey,
			// Token: 0x04000543 RID: 1347
			RWSNMPV3AuthMethod,
			// Token: 0x04000544 RID: 1348
			RWSNMPV3AuthKey,
			// Token: 0x04000545 RID: 1349
			External,
			// Token: 0x04000546 RID: 1350
			ChildStatus,
			// Token: 0x04000547 RID: 1351
			IsServer
		}
	}
}
