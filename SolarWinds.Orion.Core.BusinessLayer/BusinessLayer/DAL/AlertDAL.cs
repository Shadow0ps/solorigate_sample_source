using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using SolarWinds.InformationService.Contract2;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Alerting.DAL;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Alerting;
using SolarWinds.Orion.Core.Common.InformationService;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Common.Models.Alerts;
using SolarWinds.Orion.Core.Common.PackageManager;
using SolarWinds.Orion.Core.Common.Swis;
using SolarWinds.Orion.Core.Models.Alerting;
using SolarWinds.Orion.Core.Strings;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000095 RID: 149
	internal class AlertDAL
	{
		// Token: 0x06000713 RID: 1811
		[DllImport("ole32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int CLSIDFromString(string sz, out Guid clsid);

		// Token: 0x06000714 RID: 1812 RVA: 0x0002D0D4 File Offset: 0x0002B2D4
		public static bool TryStrToGuid(string s, out Guid value)
		{
			if (s == null || s == "")
			{
				value = Guid.Empty;
				return false;
			}
			s = string.Format("{{{0}}}", s);
			if (AlertDAL.CLSIDFromString(s, out value) >= 0)
			{
				return true;
			}
			value = Guid.Empty;
			return false;
		}

		// Token: 0x06000715 RID: 1813 RVA: 0x0002D124 File Offset: 0x0002B324
		public static List<KeyValuePair<string, string>> GetAlertList()
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT AlertID, Name FROM AlertConfigurations WITH(NOLOCK)"))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						dictionary.Add("AA-" + DatabaseFunctions.GetInt32(dataReader, "AlertID"), DatabaseFunctions.GetString(dataReader, "Name"));
					}
				}
			}
			return AlertDAL.SortList(dictionary);
		}

		// Token: 0x06000716 RID: 1814 RVA: 0x0002D1B8 File Offset: 0x0002B3B8
		[Obsolete("Old alerting will be removed. Use GetAlertList() method instead.")]
		public static List<KeyValuePair<string, string>> GetAlertList(bool includeBasic)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			if (includeBasic)
			{
				using (SqlCommand textCommand = SqlHelper.GetTextCommand("\r\nSelect al.AlertID, a.AlertName\r\nFrom ActiveAlerts al\r\nINNER JOIN Alerts a WITH(NOLOCK) ON al.AlertID = a.AlertID\r\nGroup By al.AlertID, a.AlertName\r\nOrder By AlertName \r\n"))
				{
					using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
					{
						while (dataReader.Read())
						{
							dictionary.Add(DatabaseFunctions.GetInt32(dataReader, "AlertID").ToString(), DatabaseFunctions.GetString(dataReader, "AlertName"));
						}
					}
				}
			}
			using (SqlCommand textCommand2 = SqlHelper.GetTextCommand("SELECT AlertDefID, AlertName FROM AlertDefinitions WITH(NOLOCK)\r\n                 Where AlertDefID NOT IN (SELECT AlertRefID As AlertDefID FROM AlertConfigurations)\r\n             ORDER BY AlertName"))
			{
				using (IDataReader dataReader2 = SqlHelper.ExecuteReader(textCommand2))
				{
					while (dataReader2.Read())
					{
						dictionary.Add(DatabaseFunctions.GetGuid(dataReader2, "AlertDefID").ToString(), DatabaseFunctions.GetString(dataReader2, "AlertName"));
					}
				}
			}
			using (SqlCommand textCommand3 = SqlHelper.GetTextCommand("SELECT AlertID, Name FROM AlertConfigurations WITH(NOLOCK)"))
			{
				using (IDataReader dataReader3 = SqlHelper.ExecuteReader(textCommand3))
				{
					while (dataReader3.Read())
					{
						dictionary.Add("AA-" + DatabaseFunctions.GetInt32(dataReader3, "AlertID"), DatabaseFunctions.GetString(dataReader3, "Name"));
					}
				}
			}
			return AlertDAL.SortList(dictionary);
		}

		// Token: 0x06000717 RID: 1815 RVA: 0x0002D344 File Offset: 0x0002B544
		private static List<KeyValuePair<string, string>> SortList(Dictionary<string, string> data)
		{
			List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>(data);
			list.Sort((KeyValuePair<string, string> first, KeyValuePair<string, string> second) => first.Value.CompareTo(second.Value));
			return list;
		}

		// Token: 0x06000718 RID: 1816 RVA: 0x0002D371 File Offset: 0x0002B571
		[Obsolete("Method does not return V2 alerts.")]
		public static DataTable GetAlertTable(string netObject, string deviceType, string alertID, int maxRecords, bool showAcknowledged, List<int> limitationIDs)
		{
			return AlertDAL.GetAlertTable(netObject, deviceType, alertID, maxRecords, showAcknowledged, limitationIDs, true);
		}

		// Token: 0x06000719 RID: 1817 RVA: 0x0002D381 File Offset: 0x0002B581
		[Obsolete("Method does not return V2 alerts.")]
		public static DataTable GetAlertTable(string netObject, string deviceType, string alertID, int maxRecords, bool showAcknowledged, List<int> limitationIDs, bool includeBasic)
		{
			return AlertDAL.GetSortableAlertTable(netObject, deviceType, alertID, " ObjectName, AlertName ", maxRecords, showAcknowledged, limitationIDs, includeBasic);
		}

		// Token: 0x0600071A RID: 1818 RVA: 0x0002D398 File Offset: 0x0002B598
		[Obsolete("Method does not return V2 alerts.")]
		public static DataTable GetSortableAlertTable(string netObject, string deviceType, string alertID, string orderByClause, int maxRecords, bool showAcknowledged, List<int> limitationIDs, bool includeBasic)
		{
			string text = string.Empty;
			List<string> list = new List<string>();
			int num = 0;
			bool flag = false;
			string text2 = string.Empty;
			string arg = string.Empty;
			Regex regex = new Regex("^(\\{){0,1}[0-9a-fA-F]{8}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{12}(\\}){0,1}$", RegexOptions.Compiled);
			string messageTypeString = OrionMessagesHelper.GetMessageTypeString(OrionMessageType.ADVANCED_ALERT);
			string messageTypeString2 = OrionMessagesHelper.GetMessageTypeString(OrionMessageType.BASIC_ALERT);
			if (alertID.Equals(messageTypeString, StringComparison.OrdinalIgnoreCase) || alertID.Equals(messageTypeString2, StringComparison.OrdinalIgnoreCase))
			{
				text2 = alertID;
				alertID = string.Empty;
			}
			if (!string.IsNullOrEmpty(netObject))
			{
				int num2 = netObject.IndexOf(':', 0);
				if (num2 != 0)
				{
					string text3 = netObject.Substring(num2 + 1);
					if (!int.TryParse(text3, out num))
					{
						flag = true;
						arg = text3;
					}
					else
					{
						num = Convert.ToInt32(text3);
					}
					text = netObject.Substring(0, num2);
					foreach (NetObjectType netObjectType in ModuleAlertsMap.NetObjectTypes.Items)
					{
						if (netObjectType.Prefix.ToUpper() == text.ToUpper())
						{
							list.Add(netObjectType.Name);
						}
					}
				}
			}
			StringBuilder stringBuilder = new StringBuilder(" AND (AlertStatus.State=2 OR AlertStatus.State=3) ");
			StringBuilder stringBuilder2 = new StringBuilder();
			DataTable dataTable;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(""))
			{
				if (!showAcknowledged)
				{
					stringBuilder.Append(" AND AlertStatus.Acknowledged=0 ");
				}
				if (!string.IsNullOrEmpty(netObject) && (num != 0 || flag) && !string.IsNullOrEmpty(text) && list.Count > 0)
				{
					if (text.Equals("N", StringComparison.OrdinalIgnoreCase) && num != 0)
					{
						stringBuilder.AppendFormat(" AND Nodes.NodeID={0} ", num);
					}
					else
					{
						StringBuilder stringBuilder3 = new StringBuilder();
						string arg2 = string.Empty;
						foreach (string arg3 in list)
						{
							stringBuilder3.AppendFormat(" {1} AlertStatus.ObjectType='{0}' ", arg3, arg2);
							arg2 = "OR";
						}
						stringBuilder2.AppendFormat(" AND (({0}) AND AlertStatus.ActiveObject=", stringBuilder3);
						if (flag)
						{
							stringBuilder2.AppendFormat("'{0}') ", arg);
						}
						else
						{
							stringBuilder2.AppendFormat("{0}) ", num);
						}
					}
				}
				else if (!string.IsNullOrEmpty(deviceType))
				{
					stringBuilder.Append(" AND (Nodes.MachineType Like @machineType) ");
					textCommand.Parameters.AddWithValue("@machineType", deviceType);
				}
				if (regex.IsMatch(alertID))
				{
					stringBuilder.Append(" AND (AlertStatus.AlertDefID=@alertDefID) ");
					textCommand.Parameters.AddWithValue("@alertDefID", alertID);
				}
				else if (!string.IsNullOrEmpty(alertID))
				{
					stringBuilder.AppendFormat(" AND (AlertStatus.AlertDefID='{0}') ", Guid.Empty);
				}
				string text4 = "IF OBJECT_ID('tempdb..#Nodes') IS NOT NULL\tDROP TABLE #Nodes\r\nSELECT Nodes.* INTO #Nodes FROM Nodes WHERE 1=1;";
				string text5 = Limitation.LimitSQL(text4, limitationIDs);
				bool flag2 = AlertDAL._enableLimitationReplacement && text5.Length / text4.Length > AlertDAL._limitationSqlExaggeration;
				text5 = (flag2 ? text5 : string.Empty);
				string text6 = flag2 ? "IF OBJECT_ID('tempdb..#Nodes') IS NOT NULL\tDROP TABLE #Nodes" : string.Empty;
				if (text2.Equals(messageTypeString, StringComparison.OrdinalIgnoreCase) || !includeBasic || flag)
				{
					textCommand.CommandText = string.Format("{3}SELECT TOP {0} a.*, WebCommunityStrings.[GUID] AS [GUID] FROM ( {1} )a LEFT OUTER JOIN WebCommunityStrings WITH(NOLOCK) ON WebCommunityStrings.CommunityString = a.Community Order By {2} \r\n{4}", new object[]
					{
						maxRecords,
						AlertDAL.GetAdvAlertSwql(stringBuilder.ToString(), stringBuilder2.ToString(), netObject, messageTypeString, limitationIDs, true, true),
						orderByClause,
						text5,
						text6
					});
				}
				else if (text2.Equals(messageTypeString2, StringComparison.OrdinalIgnoreCase))
				{
					textCommand.CommandText = string.Format("{3}SELECT TOP {0} a.*, WebCommunityStrings.[GUID] AS [GUID] FROM ( {1} )a LEFT OUTER JOIN WebCommunityStrings WITH(NOLOCK) ON WebCommunityStrings.CommunityString = a.Community Order By {2} \r\n{4}", new object[]
					{
						maxRecords,
						AlertDAL.GetBasicAlertSwql(netObject, deviceType, alertID, limitationIDs, true, true),
						orderByClause,
						text5,
						text6
					});
				}
				else
				{
					string advAlertSwql = AlertDAL.GetAdvAlertSwql(stringBuilder.ToString(), stringBuilder2.ToString(), netObject, messageTypeString, limitationIDs, true, true);
					string basicAlertSwql = AlertDAL.GetBasicAlertSwql(netObject, deviceType, alertID, limitationIDs, true, true);
					string text7 = string.Empty;
					if (string.IsNullOrEmpty(advAlertSwql))
					{
						text7 = string.Format("({0})", basicAlertSwql);
					}
					else
					{
						text7 = string.Format("( {0}  Union ( {1} ))", advAlertSwql, basicAlertSwql);
					}
					textCommand.CommandText = string.Format("{3}SELECT TOP {0} a.*, WebCommunityStrings.[GUID] AS [GUID] FROM ({1})a LEFT OUTER JOIN WebCommunityStrings WITH(NOLOCK) ON WebCommunityStrings.CommunityString = a.Community Order By {2} \r\n{4}", new object[]
					{
						maxRecords,
						text7,
						orderByClause,
						text5,
						text6
					});
				}
				dataTable = SqlHelper.ExecuteDataTable(textCommand);
			}
			dataTable.TableName = "History";
			return dataTable;
		}

		// Token: 0x0600071B RID: 1819 RVA: 0x0002D834 File Offset: 0x0002BA34
		private static DataTable GetAvailableObjectTypes(bool federationEnabled = false)
		{
			string query = "SELECT EntityType, Name, Prefix, KeyProperty, KeyPropertyIndex FROM Orion.NetObjectTypes (nolock=true)";
			return AlertDAL.SwisProxy.QueryWithAppendedErrors(query, federationEnabled);
		}

		// Token: 0x0600071C RID: 1820 RVA: 0x0002D854 File Offset: 0x0002BA54
		private static Dictionary<string, int> GetStatusesForSwisEntities(string entityName, string entityIdName, IEnumerable<string> entityIds, bool federationEnabled = false)
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
			if (entityIds.Count<string>() > 0)
			{
				string text = "SELECT Status, {1} AS Id FROM {0} (nolock=true) WHERE {1} IN ({2})";
				string arg = string.Join(",", entityIds);
				text = string.Format(text, entityName, entityIdName, arg);
				foreach (object obj in AlertDAL.SwisProxy.QueryWithAppendedErrors(text, federationEnabled).Rows)
				{
					DataRow dataRow = (DataRow)obj;
					string text2 = (dataRow["Id"] != DBNull.Value) ? Convert.ToString(dataRow["id"]) : string.Empty;
					int value = (dataRow["Status"] != DBNull.Value) ? Convert.ToInt32(dataRow["Status"]) : 0;
					if (!string.IsNullOrEmpty(text2) && !dictionary.ContainsKey(text2))
					{
						dictionary.Add(text2, value);
					}
				}
			}
			return dictionary;
		}

		// Token: 0x0600071D RID: 1821 RVA: 0x0002D964 File Offset: 0x0002BB64
		private static string GetAlertNote(string alertDefID, string activeObject, string objectType)
		{
			string result = string.Empty;
			if (new Regex("^(\\{){0,1}[0-9a-fA-F]{8}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{12}(\\}){0,1}$", RegexOptions.Compiled).IsMatch(alertDefID))
			{
				string query = "SELECT Notes FROM Orion.AlertStatus (nolock=true) WHERE AlertDefID=@alertDefID AND ActiveObject=@activeObject AND ObjectType=@objectType";
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				dictionary.Add("alertDefID", alertDefID);
				dictionary.Add("activeObject", activeObject);
				dictionary.Add("objectType", objectType);
				DataTable dataTable = AlertDAL.SwisProxy.QueryWithAppendedErrors(query, dictionary);
				if (dataTable.Rows.Count > 0)
				{
					result = ((dataTable.Rows[0]["Notes"] != DBNull.Value) ? Convert.ToString(dataTable.Rows[0]["Notes"]) : string.Empty);
				}
			}
			else
			{
				string text = "SELECT AlertID, ObjectID AS ActiveObject, ObjectType, AlertNotes FROM Orion.ActiveAlerts (nolock=true)";
				text += " WHERE AlertID=@alertID AND ObjectID=@objectID AND ObjectType=@objectType ";
				Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
				dictionary2.Add("alertID", alertDefID);
				dictionary2.Add("objectID", activeObject);
				dictionary2.Add("objectType", objectType);
				DataTable dataTable2 = AlertDAL.SwisProxy.QueryWithAppendedErrors(text, dictionary2);
				if (dataTable2.Rows.Count > 0)
				{
					result = ((dataTable2.Rows[0]["AlertNotes"] != DBNull.Value) ? Convert.ToString(dataTable2.Rows[0]["AlertNotes"]) : string.Empty);
				}
			}
			return result;
		}

		// Token: 0x0600071E RID: 1822 RVA: 0x0002DAC8 File Offset: 0x0002BCC8
		public static Dictionary<string, string> GetNotesForActiveAlerts(IEnumerable<ActiveAlert> activeAlerts)
		{
			Dictionary<string, string> res = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			string strCondition = string.Empty;
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = true;
			string sqlQuery = string.Empty;
			Action<SqlParameter[]> action = delegate(SqlParameter[] parameters)
			{
				if (!string.IsNullOrEmpty(strCondition))
				{
					sqlQuery = string.Format("SELECT AlertDefID, ActiveObject, ObjectType, Notes FROM AlertStatus WITH(NOLOCK) WHERE {0}", strCondition);
					using (SqlCommand textCommand = SqlHelper.GetTextCommand(sqlQuery))
					{
						if (parameters != null)
						{
							textCommand.Parameters.AddRange(parameters);
						}
						foreach (object obj in SqlHelper.ExecuteDataTable(textCommand).Rows)
						{
							DataRow dataRow = (DataRow)obj;
							string arg = (dataRow["AlertDefID"] != DBNull.Value) ? Convert.ToString(dataRow["AlertDefID"]) : string.Empty;
							string arg2 = (dataRow["ActiveObject"] != DBNull.Value) ? Convert.ToString(dataRow["ActiveObject"]) : string.Empty;
							string arg3 = (dataRow["ObjectType"] != DBNull.Value) ? Convert.ToString(dataRow["ObjectType"]) : string.Empty;
							string value = (dataRow["Notes"] != DBNull.Value) ? Convert.ToString(dataRow["Notes"]) : string.Empty;
							string key = string.Format("{0}|{1}|{2}", arg, arg2, arg3);
							if (!res.ContainsKey(key))
							{
								res.Add(key, value);
							}
						}
					}
				}
			};
			Action<SqlParameter[]> action2 = delegate(SqlParameter[] parameters)
			{
				if (!string.IsNullOrEmpty(strCondition))
				{
					sqlQuery = string.Format("SELECT AlertID, ObjectID AS ActiveObject, ObjectType, AlertNotes FROM ActiveAlerts WITH(NOLOCK) WHERE {0}", strCondition);
					using (SqlCommand textCommand = SqlHelper.GetTextCommand(sqlQuery))
					{
						if (parameters != null)
						{
							textCommand.Parameters.AddRange(parameters);
						}
						foreach (object obj in SqlHelper.ExecuteDataTable(textCommand).Rows)
						{
							DataRow dataRow = (DataRow)obj;
							string arg = (dataRow["AlertID"] != DBNull.Value) ? Convert.ToString(dataRow["AlertID"]) : string.Empty;
							string arg2 = (dataRow["ActiveObject"] != DBNull.Value) ? Convert.ToString(dataRow["ActiveObject"]) : string.Empty;
							string arg3 = (dataRow["ObjectType"] != DBNull.Value) ? Convert.ToString(dataRow["ObjectType"]) : string.Empty;
							string value = (dataRow["AlertNotes"] != DBNull.Value) ? Convert.ToString(dataRow["AlertNotes"]) : string.Empty;
							string key = string.Format("{0}|{1}|{2}", arg, arg2, arg3);
							if (!res.ContainsKey(key))
							{
								res.Add(key, value);
							}
						}
					}
				}
			};
			IEnumerable<ActiveAlert> enumerable = from item in activeAlerts
			where item.AlertType == ActiveAlertType.Advanced
			select item;
			int num = 0;
			List<SqlParameter> list = new List<SqlParameter>();
			foreach (ActiveAlert activeAlert in enumerable)
			{
				if (!flag)
				{
					stringBuilder.Append(" OR ");
				}
				stringBuilder.AppendFormat("(AlertDefID=@alertDefID{0} AND ActiveObject=@activeObject{0} AND ObjectType=@objectType{0})", num);
				list.Add(new SqlParameter(string.Format("@alertDefID{0}", num), activeAlert.AlertDefId));
				list.Add(new SqlParameter(string.Format("@activeObject{0}", num), activeAlert.ActiveNetObject));
				list.Add(new SqlParameter(string.Format("@objectType{0}", num), activeAlert.ObjectType));
				flag = false;
				num++;
				if (num % 520 == 0)
				{
					strCondition = stringBuilder.ToString();
					stringBuilder.Clear();
					action(list.ToArray());
					list.Clear();
					num = 0;
					strCondition = string.Empty;
					flag = true;
				}
			}
			strCondition = stringBuilder.ToString();
			if (!string.IsNullOrEmpty(strCondition))
			{
				action(list.ToArray());
			}
			stringBuilder.Clear();
			num = 0;
			strCondition = string.Empty;
			flag = true;
			list.Clear();
			foreach (ActiveAlert activeAlert2 in from item in activeAlerts
			where item.AlertType == ActiveAlertType.Basic
			select item)
			{
				if (!flag)
				{
					stringBuilder.Append(" OR ");
				}
				stringBuilder.AppendFormat("(AlertID=@alertID{0} AND ObjectID=@objectID{0} AND ObjectType=@objectType{0})", num);
				list.Add(new SqlParameter(string.Format("@alertID{0}", num), activeAlert2.AlertDefId));
				list.Add(new SqlParameter(string.Format("@objectID{0}", num), activeAlert2.ActiveNetObject));
				list.Add(new SqlParameter(string.Format("@objectType{0}", num), activeAlert2.ObjectType));
				flag = false;
				num++;
				if (num % 520 == 0)
				{
					strCondition = stringBuilder.ToString();
					stringBuilder.Clear();
					action2(list.ToArray());
					list.Clear();
					num = 0;
					strCondition = string.Empty;
				}
			}
			strCondition = stringBuilder.ToString();
			action2(list.ToArray());
			return res;
		}

		// Token: 0x0600071F RID: 1823 RVA: 0x0002DE04 File Offset: 0x0002C004
		private static Dictionary<string, int> GetTriggerCountForActiveAlerts(IEnumerable<ActiveAlert> activeAlerts)
		{
			Dictionary<string, int> res = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
			string strCondition = string.Empty;
			bool flag = true;
			StringBuilder stringBuilder = new StringBuilder();
			ActiveAlert[] array = (from item in activeAlerts
			where item.AlertType == ActiveAlertType.Advanced
			select item).ToArray<ActiveAlert>();
			int num = 0;
			Action<SqlParameter[]> action = delegate(SqlParameter[] parameters)
			{
				if (!string.IsNullOrEmpty(strCondition))
				{
					using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Format("SELECT AlertDefID, ActiveObject, ObjectType, TriggerCount FROM AlertStatus WITH(NOLOCK) WHERE {0}", strCondition)))
					{
						if (parameters != null)
						{
							textCommand.Parameters.AddRange(parameters);
						}
						foreach (object obj in SqlHelper.ExecuteDataTable(textCommand).Rows)
						{
							DataRow dataRow = (DataRow)obj;
							string arg = (dataRow["AlertDefID"] != DBNull.Value) ? Convert.ToString(dataRow["AlertDefID"]) : string.Empty;
							string arg2 = (dataRow["ActiveObject"] != DBNull.Value) ? Convert.ToString(dataRow["ActiveObject"]) : string.Empty;
							string arg3 = (dataRow["ObjectType"] != DBNull.Value) ? Convert.ToString(dataRow["ObjectType"]) : string.Empty;
							int value = (dataRow["TriggerCount"] != DBNull.Value) ? Convert.ToInt32(dataRow["TriggerCount"]) : 0;
							string key2 = string.Format("{0}|{1}|{2}", arg, arg2, arg3);
							if (!res.ContainsKey(key2))
							{
								res.Add(key2, value);
							}
						}
					}
				}
			};
			List<SqlParameter> list = new List<SqlParameter>();
			foreach (ActiveAlert activeAlert in array)
			{
				if (!flag)
				{
					stringBuilder.Append(" OR ");
				}
				stringBuilder.AppendFormat("(AlertDefID=@alertDefID{0} AND ActiveObject=@activeObject{0} AND ObjectType=@objectType{0})", num);
				list.Add(new SqlParameter(string.Format("@alertDefID{0}", num), activeAlert.AlertDefId));
				list.Add(new SqlParameter(string.Format("@activeObject{0}", num), activeAlert.ActiveNetObject));
				list.Add(new SqlParameter(string.Format("@objectType{0}", num), activeAlert.ObjectType));
				num++;
				flag = false;
				if (num % 520 == 0)
				{
					strCondition = stringBuilder.ToString();
					stringBuilder.Clear();
					action(list.ToArray());
					list.Clear();
					num = 0;
					strCondition = string.Empty;
					flag = true;
				}
			}
			action(list.ToArray());
			foreach (ActiveAlert activeAlert2 in from item in activeAlerts
			where item.AlertType == ActiveAlertType.Basic
			select item)
			{
				string key = string.Format("{0}|{1}|{2}", activeAlert2.AlertDefId, activeAlert2.ActiveNetObject, activeAlert2.ObjectType);
				if (!res.ContainsKey(key))
				{
					res.Add(key, 1);
				}
			}
			return res;
		}

		// Token: 0x06000720 RID: 1824 RVA: 0x0002E020 File Offset: 0x0002C220
		private static Dictionary<string, string> GetFullUserNames(IEnumerable<string> accountIDs, bool federationEnabled = false)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			if (accountIDs.Any<string>())
			{
				List<string> list = accountIDs.Distinct<string>().ToList<string>();
				string text = string.Empty;
				foreach (string arg in list)
				{
					if (!string.IsNullOrEmpty(text))
					{
						text += " OR ";
					}
					text += string.Format("AccountID='{0}'", arg);
				}
				string query = "SELECT AccountID, DisplayName FROM Orion.Accounts (nolock=true) WHERE " + text;
				foreach (object obj in AlertDAL.SwisProxy.QueryWithAppendedErrors(query, federationEnabled).Rows)
				{
					DataRow dataRow = (DataRow)obj;
					string text2 = (dataRow["AccountID"] != DBNull.Value) ? Convert.ToString(dataRow["AccountID"]) : string.Empty;
					string value = (dataRow["DisplayName"] != DBNull.Value) ? Convert.ToString(dataRow["DisplayName"]) : string.Empty;
					if (!string.IsNullOrEmpty(text2) && !dictionary.ContainsKey(text2))
					{
						dictionary.Add(text2, value);
					}
				}
			}
			return dictionary;
		}

		// Token: 0x06000721 RID: 1825 RVA: 0x0002E194 File Offset: 0x0002C394
		public static ActiveAlertPage GetPageableActiveAlerts(PageableActiveAlertRequest pageableRequest, ActiveAlertsRequest request = null)
		{
			IEnumerable<CustomProperty> customPropertiesForEntity = CustomPropertyMgr.GetCustomPropertiesForEntity("Orion.AlertConfigurationsCustomProperties");
			List<ErrorMessage> errorsMessages;
			List<ActiveAlert> list = AlertDAL.GetActiveAlerts(pageableRequest, customPropertiesForEntity, out errorsMessages);
			DataTable alertTable = AlertDAL.ConvertActiveAlertsToTable(list, customPropertiesForEntity);
			alertTable = AlertDAL.GetFilteredTable(pageableRequest, alertTable, request);
			IEnumerable<DataRow> enumerable = AlertDAL.GetFilteredAlertRows(pageableRequest, alertTable, customPropertiesForEntity);
			enumerable = AlertDAL.GetSortedAlerts(pageableRequest, enumerable);
			list = AlertDAL.ConvertRowsToActiveAlerts(enumerable, customPropertiesForEntity).ToList<ActiveAlert>();
			ActiveAlertPage activeAlertPage = new ActiveAlertPage();
			activeAlertPage.TotalRow = list.Count;
			list = list.Skip(pageableRequest.StartRow).Take(pageableRequest.PageSize).ToList<ActiveAlert>();
			activeAlertPage.ActiveAlerts = list;
			activeAlertPage.ErrorsMessages = errorsMessages;
			return activeAlertPage;
		}

		// Token: 0x06000722 RID: 1826 RVA: 0x0002E225 File Offset: 0x0002C425
		private static List<ActiveAlert> GetActiveAlerts(PageableActiveAlertRequest request, IEnumerable<CustomProperty> customProperties, out List<ErrorMessage> errors)
		{
			return new ActiveAlertDAL().GetActiveAlerts(customProperties, request.LimitationIDs, out errors, request.FederationEnabled, request.IncludeNotTriggered).ToList<ActiveAlert>();
		}

		// Token: 0x06000723 RID: 1827 RVA: 0x0002E24C File Offset: 0x0002C44C
		private static DataTable ConvertActiveAlertsToTable(IEnumerable<ActiveAlert> alerts, IEnumerable<CustomProperty> customProperties)
		{
			DataTable dataTable = new DataTable();
			dataTable.Locale = CultureInfo.InvariantCulture;
			dataTable.Columns.Add("ActiveAlertID", typeof(string));
			dataTable.Columns.Add("AlertDefID", typeof(string));
			dataTable.Columns.Add("ActiveAlertType", typeof(ActiveAlertType));
			dataTable.Columns.Add("AlertName", typeof(string));
			dataTable.Columns.Add("AlertMessage", typeof(string));
			dataTable.Columns.Add("TriggeringObject", typeof(string));
			dataTable.Columns.Add("TriggeringObjectEntityName", typeof(string));
			dataTable.Columns.Add("TriggeringObjectStatus", typeof(string));
			dataTable.Columns.Add("TriggeringObjectDetailsUrl", typeof(string));
			dataTable.Columns.Add("TriggeringObjectEntityUri", typeof(string));
			dataTable.Columns.Add("RelatedNode", typeof(string));
			dataTable.Columns.Add("RelatedNodeID", typeof(int));
			dataTable.Columns.Add("RelatedNodeEntityUri", typeof(string));
			dataTable.Columns.Add("RelatedNodeDetailsUrl", typeof(string));
			dataTable.Columns.Add("RelatedNodeStatus", typeof(int));
			dataTable.Columns.Add("ActiveTime", typeof(TimeSpan));
			dataTable.Columns.Add("ActiveTimeSort", typeof(string));
			dataTable.Columns.Add("ActiveTimeDisplay", typeof(string));
			dataTable.Columns.Add("TriggerTime", typeof(DateTime));
			dataTable.Columns.Add("TriggerTimeDisplay", typeof(string));
			dataTable.Columns.Add("LastTriggeredDateTime", typeof(DateTime));
			dataTable.Columns.Add("TriggerCount", typeof(int));
			dataTable.Columns.Add("AcknowledgedBy", typeof(string));
			dataTable.Columns.Add("AcknowledgedByFullName", typeof(string));
			dataTable.Columns.Add("AcknowledgeTime", typeof(DateTime));
			dataTable.Columns.Add("AcknowledgeTimeDisplay", typeof(string));
			dataTable.Columns.Add("Notes", typeof(string));
			dataTable.Columns.Add("NumberOfNotes", typeof(int));
			dataTable.Columns.Add("Severity", typeof(ActiveAlertSeverity));
			dataTable.Columns.Add("SeverityOrder", typeof(int));
			dataTable.Columns.Add("SeverityText", typeof(string));
			dataTable.Columns.Add("ActiveNetObject", typeof(string));
			dataTable.Columns.Add("ObjectType", typeof(string));
			dataTable.Columns.Add("LegacyAlert", typeof(bool));
			dataTable.Columns.Add("ObjectTriggeredThisAlertDisplayName", typeof(string));
			dataTable.Columns.Add("Canned", typeof(bool));
			dataTable.Columns.Add("Category", typeof(string));
			dataTable.Columns.Add("IncidentNumber", typeof(string));
			dataTable.Columns.Add("IncidentUrl", typeof(string));
			dataTable.Columns.Add("Assignee", typeof(string));
			dataTable.Columns.Add("SiteID", typeof(int));
			dataTable.Columns.Add("SiteName", typeof(string));
			foreach (CustomProperty customProperty in customProperties)
			{
				dataTable.Columns.Add(string.Format("CP_{0}", customProperty.PropertyName), customProperty.PropertyType);
				if (customProperty.PropertyType == typeof(bool) || customProperty.PropertyType == typeof(float) || customProperty.PropertyType == typeof(int) || customProperty.PropertyType == typeof(DateTime))
				{
					dataTable.Columns.Add(string.Format("CP_{0}_Display", customProperty.PropertyName), typeof(string));
				}
			}
			foreach (ActiveAlert activeAlert in alerts)
			{
				DataRow dataRow = dataTable.NewRow();
				dataRow["ActiveAlertID"] = activeAlert.Id;
				dataRow["AlertDefID"] = activeAlert.AlertDefId;
				dataRow["ActiveAlertType"] = activeAlert.AlertType;
				dataRow["AlertName"] = activeAlert.Name;
				dataRow["AlertMessage"] = activeAlert.Message;
				dataRow["TriggeringObject"] = activeAlert.TriggeringObjectCaption;
				dataRow["TriggeringObjectEntityName"] = activeAlert.TriggeringObjectEntityName;
				dataRow["TriggeringObjectStatus"] = activeAlert.TriggeringObjectStatus;
				dataRow["TriggeringObjectDetailsUrl"] = activeAlert.TriggeringObjectDetailsUrl;
				dataRow["TriggeringObjectEntityUri"] = activeAlert.TriggeringObjectEntityUri;
				dataRow["RelatedNode"] = activeAlert.RelatedNodeCaption;
				dataRow["RelatedNodeID"] = activeAlert.RelatedNodeID;
				dataRow["RelatedNodeEntityUri"] = activeAlert.RelatedNodeEntityUri;
				dataRow["RelatedNodeDetailsUrl"] = activeAlert.RelatedNodeDetailsUrl;
				dataRow["RelatedNodeStatus"] = activeAlert.RelatedNodeStatus;
				dataRow["ActiveTime"] = activeAlert.ActiveTime;
				dataRow["ActiveTimeSort"] = activeAlert.ActiveTime.ToString("d\\.hh\\:mm\\:ss", CultureInfo.InvariantCulture);
				dataRow["ActiveTimeDisplay"] = activeAlert.ActiveTimeDisplay;
				dataRow["TriggerTime"] = DateTime.SpecifyKind(activeAlert.TriggerDateTime, DateTimeKind.Utc);
				dataRow["TriggerTimeDisplay"] = DateTimeHelper.ConvertToDisplayDate(activeAlert.TriggerDateTime);
				dataRow["LastTriggeredDateTime"] = activeAlert.LastTriggeredDateTime;
				dataRow["TriggerCount"] = activeAlert.TriggerCount;
				dataRow["AcknowledgedBy"] = activeAlert.AcknowledgedBy;
				dataRow["AcknowledgedByFullName"] = activeAlert.AcknowledgedByFullName;
				dataRow["AcknowledgeTime"] = DateTime.SpecifyKind(activeAlert.AcknowledgedDateTime, DateTimeKind.Utc);
				dataRow["AcknowledgeTimeDisplay"] = ((activeAlert.AcknowledgedDateTime != DateTime.MinValue && activeAlert.AcknowledgedDateTime.Year != 1899) ? DateTimeHelper.ConvertToDisplayDate(activeAlert.AcknowledgedDateTime) : Resources.WEBJS_PS0_59);
				dataRow["Notes"] = activeAlert.Notes;
				dataRow["NumberOfNotes"] = activeAlert.NumberOfNotes;
				dataRow["Severity"] = activeAlert.Severity;
				dataRow["SeverityText"] = activeAlert.Severityi18nMessage;
				dataRow["SeverityOrder"] = activeAlert.SeverityOrder;
				dataRow["ActiveNetObject"] = activeAlert.ActiveNetObject;
				dataRow["ObjectType"] = activeAlert.ObjectType;
				dataRow["LegacyAlert"] = activeAlert.LegacyAlert;
				dataRow["ObjectTriggeredThisAlertDisplayName"] = activeAlert.ObjectTriggeredThisAlertDisplayName;
				dataRow["Canned"] = activeAlert.Canned;
				dataRow["Category"] = activeAlert.Category;
				dataRow["IncidentNumber"] = activeAlert.IncidentNumber;
				dataRow["IncidentUrl"] = activeAlert.IncidentUrl;
				dataRow["Assignee"] = activeAlert.AssignedTo;
				dataRow["SiteID"] = activeAlert.SiteID;
				dataRow["SiteName"] = activeAlert.SiteName;
				if (activeAlert.CustomProperties != null)
				{
					foreach (CustomProperty customProperty2 in customProperties)
					{
						if (activeAlert.CustomProperties.ContainsKey(customProperty2.PropertyName))
						{
							dataRow[string.Format("CP_{0}", customProperty2.PropertyName)] = ((activeAlert.CustomProperties[customProperty2.PropertyName] != null) ? activeAlert.CustomProperties[customProperty2.PropertyName] : DBNull.Value);
							if (customProperty2.PropertyType == typeof(bool) || customProperty2.PropertyType == typeof(float) || customProperty2.PropertyType == typeof(int))
							{
								dataRow[string.Format("CP_{0}_Display", customProperty2.PropertyName)] = ((activeAlert.CustomProperties[customProperty2.PropertyName] != null) ? Convert.ToString(activeAlert.CustomProperties[customProperty2.PropertyName]) : string.Empty);
							}
							else if (customProperty2.PropertyType == typeof(DateTime))
							{
								if (activeAlert.CustomProperties[customProperty2.PropertyName] != null)
								{
									dataRow[string.Format("CP_{0}_Display", customProperty2.PropertyName)] = DateTimeHelper.ConvertToDisplayDate((DateTime)activeAlert.CustomProperties[customProperty2.PropertyName]);
								}
								else
								{
									dataRow[string.Format("CP_{0}_Display", customProperty2.PropertyName)] = DBNull.Value;
								}
							}
						}
					}
				}
				dataTable.Rows.Add(dataRow);
			}
			return dataTable;
		}

		// Token: 0x06000724 RID: 1828 RVA: 0x0002EDAC File Offset: 0x0002CFAC
		private static IEnumerable<ActiveAlert> ConvertRowsToActiveAlerts(IEnumerable<DataRow> rows, IEnumerable<CustomProperty> customProperties)
		{
			List<ActiveAlert> list = new List<ActiveAlert>();
			foreach (DataRow dataRow in rows)
			{
				ActiveAlert activeAlert = new ActiveAlert();
				activeAlert.Id = Convert.ToInt32(dataRow["ActiveAlertID"]);
				activeAlert.AlertDefId = Convert.ToString(dataRow["AlertDefID"]);
				activeAlert.AlertType = (ActiveAlertType)dataRow["ActiveAlertType"];
				activeAlert.Name = Convert.ToString(dataRow["AlertName"]);
				activeAlert.Message = Convert.ToString(dataRow["AlertMessage"]);
				activeAlert.TriggeringObjectCaption = Convert.ToString(dataRow["TriggeringObject"]);
				activeAlert.TriggeringObjectEntityName = Convert.ToString(dataRow["TriggeringObjectEntityName"]);
				activeAlert.TriggeringObjectStatus = Convert.ToInt32(dataRow["TriggeringObjectStatus"]);
				activeAlert.TriggeringObjectDetailsUrl = Convert.ToString(dataRow["TriggeringObjectDetailsUrl"]);
				activeAlert.TriggeringObjectEntityUri = Convert.ToString(dataRow["TriggeringObjectEntityUri"]);
				activeAlert.RelatedNodeCaption = Convert.ToString(dataRow["RelatedNode"]);
				activeAlert.RelatedNodeID = Convert.ToInt32(dataRow["RelatedNodeID"]);
				activeAlert.RelatedNodeEntityUri = Convert.ToString(dataRow["RelatedNodeEntityUri"]);
				activeAlert.RelatedNodeDetailsUrl = Convert.ToString(dataRow["RelatedNodeDetailsUrl"]);
				activeAlert.RelatedNodeStatus = Convert.ToInt32(dataRow["RelatedNodeStatus"]);
				activeAlert.ActiveTime = (TimeSpan)dataRow["ActiveTime"];
				activeAlert.ActiveTimeDisplay = Convert.ToString(dataRow["ActiveTimeDisplay"]);
				activeAlert.TriggerDateTime = Convert.ToDateTime(dataRow["TriggerTime"]);
				activeAlert.LastTriggeredDateTime = Convert.ToDateTime(dataRow["LastTriggeredDateTime"]);
				activeAlert.TriggerCount = Convert.ToInt32(dataRow["TriggerCount"]);
				activeAlert.AcknowledgedBy = Convert.ToString(dataRow["AcknowledgedBy"]);
				activeAlert.AcknowledgedByFullName = Convert.ToString(dataRow["AcknowledgedByFullName"]);
				activeAlert.AcknowledgedDateTime = Convert.ToDateTime(dataRow["AcknowledgeTime"]);
				activeAlert.Notes = Convert.ToString(dataRow["Notes"]);
				activeAlert.NumberOfNotes = Convert.ToInt32(dataRow["NumberOfNotes"]);
				activeAlert.Severity = (ActiveAlertSeverity)dataRow["Severity"];
				activeAlert.ActiveNetObject = Convert.ToString(dataRow["ActiveNetObject"]);
				activeAlert.ObjectType = Convert.ToString(dataRow["ObjectType"]);
				activeAlert.LegacyAlert = Convert.ToBoolean(dataRow["LegacyAlert"]);
				activeAlert.Canned = Convert.ToBoolean(dataRow["Canned"]);
				activeAlert.Category = Convert.ToString(dataRow["Category"]);
				activeAlert.IncidentNumber = Convert.ToString(dataRow["IncidentNumber"]);
				activeAlert.IncidentUrl = Convert.ToString(dataRow["IncidentUrl"]);
				activeAlert.AssignedTo = Convert.ToString(dataRow["Assignee"]);
				activeAlert.SiteID = Convert.ToInt32(dataRow["SiteID"]);
				activeAlert.SiteName = Convert.ToString(dataRow["SiteName"]);
				if (!activeAlert.LegacyAlert)
				{
					activeAlert.CustomProperties = new Dictionary<string, object>();
					foreach (CustomProperty customProperty in customProperties)
					{
						string columnName = string.Format("CP_{0}", customProperty.PropertyName);
						if (dataRow[columnName] != DBNull.Value)
						{
							activeAlert.CustomProperties.Add(customProperty.PropertyName, dataRow[columnName]);
						}
						else
						{
							activeAlert.CustomProperties.Add(customProperty.PropertyName, null);
						}
					}
				}
				list.Add(activeAlert);
			}
			return list;
		}

		// Token: 0x06000725 RID: 1829 RVA: 0x0002F1D4 File Offset: 0x0002D3D4
		internal static DataTable GetFilteredTable(PageableActiveAlertRequest pRequest, DataTable alertTable, ActiveAlertsRequest request = null)
		{
			string activeAlertsFilterCondition = AlertDAL.GetActiveAlertsFilterCondition(pRequest);
			bool flag = activeAlertsFilterCondition.Split(new string[]
			{
				"OR",
				"AND"
			}, StringSplitOptions.None).Count<string>() > 100;
			if ((!string.IsNullOrEmpty(activeAlertsFilterCondition) && request == null) || !flag)
			{
				alertTable.CaseSensitive = true;
				DataRow[] array = alertTable.Select(activeAlertsFilterCondition);
				if (array != null && array.Length != 0)
				{
					alertTable = array.CopyToDataTable<DataRow>();
				}
				else
				{
					alertTable.Rows.Clear();
				}
			}
			else if (request != null)
			{
				alertTable.CaseSensitive = true;
				DataRow[] array = alertTable.AsEnumerable().Where(AlertDAL.GenerateLambdaFilter(request).Compile()).ToArray<DataRow>();
				if (array != null && array.Length != 0)
				{
					alertTable = array.CopyToDataTable<DataRow>();
				}
				else
				{
					alertTable.Rows.Clear();
				}
			}
			return alertTable;
		}

		// Token: 0x06000726 RID: 1830 RVA: 0x0002F290 File Offset: 0x0002D490
		private static Expression<Func<DataRow, bool>> GenerateLambdaFilter(ActiveAlertsRequest request)
		{
			Expression<Func<DataRow, bool>> expression = null;
			ParameterExpression parameterExpression;
			if (request.TriggeringObjectEntityUris != null && request.TriggeringObjectEntityUris.Any<string>())
			{
				foreach (string template in request.TriggeringObjectEntityUris)
				{
					AlertDAL.<>c__DisplayClass27_0 CS$<>8__locals1 = new AlertDAL.<>c__DisplayClass27_0();
					CS$<>8__locals1.template = template;
					Expression<Func<DataRow, bool>> testExpression = Expression.Lambda<Func<DataRow, bool>>(Expression.Call(Expression.Call(Expression.Call(parameterExpression, methodof(DataRow.get_Item(string)), new Expression[]
					{
						Expression.Constant("TriggeringObjectEntityUri", typeof(string))
					}), methodof(object.ToString()), Array.Empty<Expression>()), methodof(string.Equals(string)), new Expression[]
					{
						Expression.Field(Expression.Constant(CS$<>8__locals1, typeof(AlertDAL.<>c__DisplayClass27_0)), fieldof(AlertDAL.<>c__DisplayClass27_0.template))
					}), new ParameterExpression[]
					{
						parameterExpression
					});
					expression = AlertDAL.GetFilterPredicate(expression, testExpression);
				}
				foreach (string arg in request.TriggeringObjectEntityUris)
				{
					AlertDAL.<>c__DisplayClass27_1 CS$<>8__locals2 = new AlertDAL.<>c__DisplayClass27_1();
					CS$<>8__locals2.template = string.Format("{0}/", arg);
					Expression<Func<DataRow, bool>> testExpression2 = Expression.Lambda<Func<DataRow, bool>>(Expression.Call(Expression.Call(Expression.Call(parameterExpression, methodof(DataRow.get_Item(string)), new Expression[]
					{
						Expression.Constant("TriggeringObjectEntityUri", typeof(string))
					}), methodof(object.ToString()), Array.Empty<Expression>()), methodof(string.Contains(string)), new Expression[]
					{
						Expression.Field(Expression.Constant(CS$<>8__locals2, typeof(AlertDAL.<>c__DisplayClass27_1)), fieldof(AlertDAL.<>c__DisplayClass27_1.template))
					}), new ParameterExpression[]
					{
						parameterExpression
					});
					expression = AlertDAL.GetFilterPredicate(expression, testExpression2);
				}
				if (!request.TriggeringObjectEntityNames.Any<string>() || string.Compare(request.TriggeringObjectEntityNames[0], "Orion.Groups", StringComparison.OrdinalIgnoreCase) != 0)
				{
					goto IL_8B7;
				}
				using (IEnumerator<int> enumerator = request.AlertActiveIdsGlobal.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						int num = enumerator.Current;
						AlertDAL.<>c__DisplayClass27_2 CS$<>8__locals3 = new AlertDAL.<>c__DisplayClass27_2();
						CS$<>8__locals3.template = num.ToString();
						Expression<Func<DataRow, bool>> testExpression3 = Expression.Lambda<Func<DataRow, bool>>(Expression.Call(Expression.Call(Expression.Call(parameterExpression, methodof(DataRow.get_Item(string)), new Expression[]
						{
							Expression.Constant("ActiveAlertID", typeof(string))
						}), methodof(object.ToString()), Array.Empty<Expression>()), methodof(string.Equals(string)), new Expression[]
						{
							Expression.Field(Expression.Constant(CS$<>8__locals3, typeof(AlertDAL.<>c__DisplayClass27_2)), fieldof(AlertDAL.<>c__DisplayClass27_2.template))
						}), new ParameterExpression[]
						{
							parameterExpression
						});
						expression = AlertDAL.GetFilterPredicate(expression, testExpression3);
					}
					goto IL_8B7;
				}
			}
			if (request.TriggeringObjectEntityNames != null && request.TriggeringObjectEntityNames.Any<string>())
			{
				foreach (string template2 in request.TriggeringObjectEntityNames)
				{
					AlertDAL.<>c__DisplayClass27_3 CS$<>8__locals4 = new AlertDAL.<>c__DisplayClass27_3();
					CS$<>8__locals4.template = template2;
					Expression<Func<DataRow, bool>> testExpression4 = Expression.Lambda<Func<DataRow, bool>>(Expression.Call(Expression.Call(Expression.Call(parameterExpression, methodof(DataRow.get_Item(string)), new Expression[]
					{
						Expression.Constant("TriggeringObjectEntityName", typeof(string))
					}), methodof(object.ToString()), Array.Empty<Expression>()), methodof(string.Equals(string)), new Expression[]
					{
						Expression.Field(Expression.Constant(CS$<>8__locals4, typeof(AlertDAL.<>c__DisplayClass27_3)), fieldof(AlertDAL.<>c__DisplayClass27_3.template))
					}), new ParameterExpression[]
					{
						parameterExpression
					});
					expression = AlertDAL.GetFilterPredicate(expression, testExpression4);
				}
			}
			else if (request.RelatedNodeId > 0 || !string.IsNullOrEmpty(request.RelatedNodeEntityUri))
			{
				if (request.RelatedNodeId > 0)
				{
					AlertDAL.<>c__DisplayClass27_4 CS$<>8__locals5 = new AlertDAL.<>c__DisplayClass27_4();
					CS$<>8__locals5.template = request.RelatedNodeId.ToString();
					Expression<Func<DataRow, bool>> testExpression5 = Expression.Lambda<Func<DataRow, bool>>(Expression.Call(Expression.Call(Expression.Call(parameterExpression, methodof(DataRow.get_Item(string)), new Expression[]
					{
						Expression.Constant("RelatedNodeID", typeof(string))
					}), methodof(object.ToString()), Array.Empty<Expression>()), methodof(string.Equals(string)), new Expression[]
					{
						Expression.Field(Expression.Constant(CS$<>8__locals5, typeof(AlertDAL.<>c__DisplayClass27_4)), fieldof(AlertDAL.<>c__DisplayClass27_4.template))
					}), new ParameterExpression[]
					{
						parameterExpression
					});
					expression = AlertDAL.GetFilterPredicate(expression, testExpression5);
				}
				IEnumerable<int> alertActiveIdsGlobal = request.AlertActiveIdsGlobal;
				if (!string.IsNullOrEmpty(request.RelatedNodeEntityUri))
				{
					AlertDAL.<>c__DisplayClass27_5 CS$<>8__locals6 = new AlertDAL.<>c__DisplayClass27_5();
					foreach (int num2 in alertActiveIdsGlobal)
					{
						AlertDAL.<>c__DisplayClass27_6 CS$<>8__locals7 = new AlertDAL.<>c__DisplayClass27_6();
						CS$<>8__locals7.template = num2.ToString();
						Expression<Func<DataRow, bool>> testExpression6 = Expression.Lambda<Func<DataRow, bool>>(Expression.Call(Expression.Call(Expression.Call(parameterExpression, methodof(DataRow.get_Item(string)), new Expression[]
						{
							Expression.Constant("ActiveAlertID", typeof(string))
						}), methodof(object.ToString()), Array.Empty<Expression>()), methodof(string.Equals(string)), new Expression[]
						{
							Expression.Field(Expression.Constant(CS$<>8__locals7, typeof(AlertDAL.<>c__DisplayClass27_6)), fieldof(AlertDAL.<>c__DisplayClass27_6.template))
						}), new ParameterExpression[]
						{
							parameterExpression
						});
						expression = AlertDAL.GetFilterPredicate(expression, testExpression6);
					}
					CS$<>8__locals6.relatedNodeEntityUri = request.RelatedNodeEntityUri;
					Expression<Func<DataRow, bool>> testExpression7 = Expression.Lambda<Func<DataRow, bool>>(Expression.Call(Expression.Call(Expression.Call(parameterExpression, methodof(DataRow.get_Item(string)), new Expression[]
					{
						Expression.Constant("TriggeringObjectEntityUri", typeof(string))
					}), methodof(object.ToString()), Array.Empty<Expression>()), methodof(string.Equals(string)), new Expression[]
					{
						Expression.Field(Expression.Constant(CS$<>8__locals6, typeof(AlertDAL.<>c__DisplayClass27_5)), fieldof(AlertDAL.<>c__DisplayClass27_5.relatedNodeEntityUri))
					}), new ParameterExpression[]
					{
						parameterExpression
					});
					expression = AlertDAL.GetFilterPredicate(expression, testExpression7);
					Expression<Func<DataRow, bool>> testExpression8 = Expression.Lambda<Func<DataRow, bool>>(Expression.Call(Expression.Call(Expression.Call(parameterExpression, methodof(DataRow.get_Item(string)), new Expression[]
					{
						Expression.Constant("RelatedNodeEntityUri", typeof(string))
					}), methodof(object.ToString()), Array.Empty<Expression>()), methodof(string.Equals(string)), new Expression[]
					{
						Expression.Field(Expression.Constant(CS$<>8__locals6, typeof(AlertDAL.<>c__DisplayClass27_5)), fieldof(AlertDAL.<>c__DisplayClass27_5.relatedNodeEntityUri))
					}), new ParameterExpression[]
					{
						parameterExpression
					});
					expression = AlertDAL.GetFilterPredicate(expression, testExpression8);
				}
				foreach (string template3 in request.TriggeringObjectEntityNames)
				{
					AlertDAL.<>c__DisplayClass27_7 CS$<>8__locals8 = new AlertDAL.<>c__DisplayClass27_7();
					CS$<>8__locals8.template = template3;
					Expression<Func<DataRow, bool>> testExpression9 = Expression.Lambda<Func<DataRow, bool>>(Expression.Call(Expression.Call(Expression.Call(parameterExpression, methodof(DataRow.get_Item(string)), new Expression[]
					{
						Expression.Constant("TriggeringObjectEntityName", typeof(string))
					}), methodof(object.ToString()), Array.Empty<Expression>()), methodof(string.Equals(string)), new Expression[]
					{
						Expression.Field(Expression.Constant(CS$<>8__locals8, typeof(AlertDAL.<>c__DisplayClass27_7)), fieldof(AlertDAL.<>c__DisplayClass27_7.template))
					}), new ParameterExpression[]
					{
						parameterExpression
					});
					expression = AlertDAL.GetFilterPredicate(expression, testExpression9);
				}
			}
			IL_8B7:
			if (!request.ShowAcknowledgedAlerts)
			{
				InvocationExpression right = Expression.Invoke(Expression.Lambda<Func<DataRow, bool>>(Expression.OrElse(Expression.Call(Expression.Call(Expression.Call(parameterExpression, methodof(DataRow.get_Item(string)), new Expression[]
				{
					Expression.Constant("AcknowledgedBy", typeof(string))
				}), methodof(object.ToString()), Array.Empty<Expression>()), methodof(string.Equals(string)), new Expression[]
				{
					Expression.Constant("", typeof(string))
				}), Expression.Equal(Expression.Call(parameterExpression, methodof(DataRow.get_Item(string)), new Expression[]
				{
					Expression.Constant("AcknowledgedBy", typeof(string))
				}), Expression.Field(null, fieldof(DBNull.Value)))), new ParameterExpression[]
				{
					parameterExpression
				}), expression.Parameters.Cast<Expression>());
				expression = Expression.Lambda<Func<DataRow, bool>>(Expression.And(expression.Body, right), expression.Parameters);
			}
			return expression;
		}

		// Token: 0x06000727 RID: 1831 RVA: 0x0002FC98 File Offset: 0x0002DE98
		private static Expression<Func<DataRow, bool>> GetFilterPredicate(Expression<Func<DataRow, bool>> filterPredicate, Expression<Func<DataRow, bool>> testExpression)
		{
			if (filterPredicate == null)
			{
				filterPredicate = testExpression;
			}
			InvocationExpression right = Expression.Invoke(testExpression, filterPredicate.Parameters.Cast<Expression>());
			filterPredicate = Expression.Lambda<Func<DataRow, bool>>(Expression.Or(filterPredicate.Body, right), filterPredicate.Parameters);
			return filterPredicate;
		}

		// Token: 0x06000728 RID: 1832 RVA: 0x0002FCD8 File Offset: 0x0002DED8
		private static string GetActiveAlertsFilterCondition(PageableActiveAlertRequest request)
		{
			string result = string.Empty;
			if (!string.IsNullOrEmpty(request.FilterStatement))
			{
				result = request.FilterStatement;
			}
			else
			{
				result = AlertDAL.GetFilterCondition(request.FilterByPropertyName, request.FilterByPropertyValue, request.FilterByPropertyType);
			}
			return result;
		}

		// Token: 0x06000729 RID: 1833 RVA: 0x0002FD1C File Offset: 0x0002DF1C
		private static string GetFilterCondition(string FilterByPropertyName, string FilterByPropertyValue, string FilterByPropertyType)
		{
			string text = string.Empty;
			if (string.IsNullOrEmpty(FilterByPropertyName) || FilterByPropertyValue.Equals("[All]", StringComparison.OrdinalIgnoreCase))
			{
				return text;
			}
			text = "(" + FilterByPropertyName;
			if (string.IsNullOrEmpty(FilterByPropertyValue) && FilterByPropertyType == "System.String")
			{
				text = text + " IS NULL OR " + FilterByPropertyName + " = ''";
			}
			else if (string.IsNullOrEmpty(FilterByPropertyValue))
			{
				text += " IS NULL";
			}
			else if (FilterByPropertyType == "System.String")
			{
				text = text + "='" + FilterByPropertyValue.Replace("'", "''") + "'";
			}
			else if (FilterByPropertyType == "System.DateTime")
			{
				DateTime minValue = DateTime.MinValue;
				if (DateTime.TryParse(FilterByPropertyValue, Thread.CurrentThread.CurrentUICulture, DateTimeStyles.None, out minValue))
				{
					text += string.Format("='{0}'", minValue.ToString("yyyy-MM-ddTHH:mm:ss"));
				}
				else if (DateTime.TryParseExact(FilterByPropertyValue, "MM/dd/yyyy h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out minValue))
				{
					text += string.Format("='{0}'", minValue.ToString("yyyy-MM-ddTHH:mm:ss"));
				}
				else
				{
					text += string.Format("='{0}'", DateTime.MinValue.ToString("yyyy-MM-ddTHH:mm:ss"));
				}
			}
			else if (FilterByPropertyType == "System.Single")
			{
				text = text + "=" + FilterByPropertyValue.Replace(",", ".");
			}
			else
			{
				text += string.Format("={0}", FilterByPropertyValue);
			}
			return text + ")";
		}

		// Token: 0x0600072A RID: 1834 RVA: 0x0002FEBC File Offset: 0x0002E0BC
		private static string GetActiveAlertsSearchCondition(string filterValue, IEnumerable<CustomProperty> customProperties)
		{
			string text = AlertDAL.EscapeLikeValue(filterValue);
			string text2 = string.Empty;
			text2 = text2 + "AlertName LIKE '" + text + "'";
			text2 = text2 + " OR AlertMessage LIKE '" + text + "'";
			text2 = text2 + " OR ObjectTriggeredThisAlertDisplayName LIKE '" + text + "'";
			text2 = text2 + " OR ActiveTimeDisplay LIKE '" + text + "'";
			text2 = text2 + " OR TriggerTimeDisplay LIKE '" + text + "'";
			text2 = text2 + " OR AcknowledgedBy LIKE '" + text + "'";
			text2 = text2 + " OR SeverityText LIKE '" + text + "'";
			text2 = text2 + " OR AcknowledgeTimeDisplay LIKE '" + text + "'";
			foreach (CustomProperty customProperty in customProperties)
			{
				if (customProperty.PropertyType != typeof(bool) && customProperty.PropertyType != typeof(float) && customProperty.PropertyType != typeof(int) && customProperty.PropertyType != typeof(DateTime))
				{
					text2 += string.Format(" OR CP_{0} LIKE '{1}'", customProperty.PropertyName, text);
				}
				else
				{
					text2 += string.Format(" OR CP_{0}_Display LIKE '{1}'", customProperty.PropertyName, text);
				}
			}
			return text2;
		}

		// Token: 0x0600072B RID: 1835 RVA: 0x00030030 File Offset: 0x0002E230
		private static IEnumerable<DataRow> GetFilteredAlertRows(PageableActiveAlertRequest request, DataTable alertTable, IEnumerable<CustomProperty> customProperties)
		{
			string strCondition = (!string.IsNullOrWhiteSpace(request.SearchValue)) ? AlertDAL.GetActiveAlertsSearchCondition(request.SearchValue, customProperties) : string.Empty;
			string filterCondition = AlertDAL.GetFilterCondition(request.SecondaryFilters, strCondition, request.SecondaryFilterOperator);
			return alertTable.Select(filterCondition);
		}

		// Token: 0x0600072C RID: 1836 RVA: 0x00030078 File Offset: 0x0002E278
		private static IEnumerable<DataRow> GetSortedAlerts(PageableActiveAlertRequest request, IEnumerable<DataRow> alertRows)
		{
			DataRow[] result = new DataRow[0];
			if (!alertRows.Any<DataRow>())
			{
				return result;
			}
			string text = string.Empty;
			SortOrder sortOrder = SortOrder.Ascending;
			if (request.OrderByClause.EndsWith("ASC", StringComparison.OrdinalIgnoreCase))
			{
				text = request.OrderByClause.Substring(0, request.OrderByClause.Length - 3).Trim();
				text = text.TrimStart(new char[]
				{
					'['
				}).TrimEnd(new char[]
				{
					']'
				});
			}
			else if (request.OrderByClause.EndsWith("DESC", StringComparison.OrdinalIgnoreCase))
			{
				text = request.OrderByClause.Substring(0, request.OrderByClause.Length - 4).Trim();
				text = text.TrimStart(new char[]
				{
					'['
				}).TrimEnd(new char[]
				{
					']'
				});
				sortOrder = SortOrder.Descending;
			}
			else if (!string.IsNullOrEmpty(request.OrderByClause))
			{
				text = request.OrderByClause;
				text = text.TrimStart(new char[]
				{
					'['
				}).TrimEnd(new char[]
				{
					']'
				});
			}
			if (!string.IsNullOrEmpty(request.OrderByClause))
			{
				result = AlertDAL.GetSortedAlerts(alertRows, text, sortOrder).ToArray<DataRow>();
			}
			else
			{
				result = alertRows.ToArray<DataRow>();
			}
			return result;
		}

		// Token: 0x0600072D RID: 1837 RVA: 0x000301AC File Offset: 0x0002E3AC
		private static IEnumerable<DataRow> GetSortedAlerts(IEnumerable<DataRow> rows, string sortColumnName, SortOrder sortOrder)
		{
			if (!rows.Any<DataRow>())
			{
				return new DataRow[0];
			}
			if (!rows.ElementAt(0).Table.Columns.Contains(sortColumnName))
			{
				AlertDAL.Log.WarnFormat("Unable to sort by column '{0}', because column doesn't belong to the table. Alert grid will not be sorted. If it is custom property column please make sure, that wasn't deleted.", sortColumnName);
				return rows;
			}
			Type type = rows.First<DataRow>()[sortColumnName].GetType();
			if (type == typeof(DateTime))
			{
				Func<DataRow, DateTime> keySelector = delegate(DataRow row)
				{
					if (row[sortColumnName] == DBNull.Value)
					{
						return default(DateTime);
					}
					DateTime? dateTime = row[sortColumnName] as DateTime?;
					if (dateTime == null)
					{
						return default(DateTime);
					}
					return dateTime.Value;
				};
				if (sortOrder != SortOrder.Ascending)
				{
					return rows.OrderByDescending(keySelector).ToArray<DataRow>();
				}
				return rows.OrderBy(keySelector).ToArray<DataRow>();
			}
			else if (type == typeof(TimeSpan))
			{
				Func<DataRow, TimeSpan> keySelector2 = delegate(DataRow row)
				{
					if (row[sortColumnName] == DBNull.Value)
					{
						return TimeSpan.FromSeconds(0.0);
					}
					TimeSpan? timeSpan = row[sortColumnName] as TimeSpan?;
					if (timeSpan == null)
					{
						return TimeSpan.FromSeconds(0.0);
					}
					return timeSpan.Value;
				};
				if (sortOrder != SortOrder.Ascending)
				{
					return rows.OrderByDescending(keySelector2).ToArray<DataRow>();
				}
				return rows.OrderBy(keySelector2).ToArray<DataRow>();
			}
			else
			{
				if (sortOrder != SortOrder.Ascending)
				{
					return rows.OrderByDescending(delegate(DataRow row)
					{
						if (row[sortColumnName] == DBNull.Value)
						{
							return string.Empty;
						}
						return Convert.ToString(row[sortColumnName]);
					}, new NaturalStringComparer()).ToArray<DataRow>();
				}
				return rows.OrderBy(delegate(DataRow row)
				{
					if (row[sortColumnName] == DBNull.Value)
					{
						return string.Empty;
					}
					return Convert.ToString(row[sortColumnName]);
				}, new NaturalStringComparer()).ToArray<DataRow>();
			}
		}

		// Token: 0x0600072E RID: 1838 RVA: 0x000302D8 File Offset: 0x0002E4D8
		private static ActiveAlert SortableAlertDataRowToActiveAlertObject(DataRow rAlert, Func<string, ActiveAlertType, string> getSwisEntityName, DateTime currentDateTime, List<string> nodeStatusIds, List<string> interfaceStatusIds, List<string> containerStatusIds, List<string> acknowledgedBy)
		{
			ActiveAlert activeAlert = new ActiveAlert();
			activeAlert.AlertDefId = ((rAlert["AlertID"] != DBNull.Value) ? Convert.ToString(rAlert["AlertID"]) : string.Empty);
			Guid guid;
			if (AlertDAL.TryStrToGuid(activeAlert.AlertDefId, out guid))
			{
				activeAlert.AlertType = ActiveAlertType.Advanced;
			}
			else
			{
				activeAlert.AlertType = ActiveAlertType.Basic;
			}
			activeAlert.Name = ((rAlert["AlertName"] != DBNull.Value) ? Convert.ToString(rAlert["AlertName"]) : string.Empty);
			activeAlert.TriggerDateTime = ((rAlert["AlertTime"] != DBNull.Value) ? Convert.ToDateTime(rAlert["AlertTime"]) : DateTime.MinValue);
			activeAlert.Message = ((rAlert["EventMessage"] != DBNull.Value) ? Convert.ToString(rAlert["EventMessage"]) : string.Empty);
			activeAlert.TriggeringObjectCaption = ((rAlert["ObjectName"] != DBNull.Value) ? Convert.ToString(rAlert["ObjectName"]) : string.Empty);
			activeAlert.ActiveNetObject = ((rAlert["ActiveNetObject"] != DBNull.Value) ? Convert.ToString(rAlert["ActiveNetObject"]) : "0");
			string text = (rAlert["ObjectType"] != DBNull.Value) ? Convert.ToString(rAlert["ObjectType"]) : string.Empty;
			string text2 = getSwisEntityName(text, activeAlert.AlertType);
			activeAlert.TriggeringObjectEntityName = text2;
			activeAlert.RelatedNodeCaption = ((rAlert["Sysname"] != DBNull.Value) ? Convert.ToString(rAlert["Sysname"]) : string.Empty);
			activeAlert.RelatedNodeID = ((rAlert["NodeID"] != DBNull.Value) ? Convert.ToInt32(rAlert["NodeID"]) : 0);
			if (activeAlert.RelatedNodeID > 0 && !nodeStatusIds.Contains(activeAlert.RelatedNodeID.ToString()))
			{
				nodeStatusIds.Add(activeAlert.RelatedNodeID.ToString());
			}
			if (string.Compare(text2, "Orion.Nodes", StringComparison.OrdinalIgnoreCase) == 0)
			{
				if (!nodeStatusIds.Contains(activeAlert.ActiveNetObject))
				{
					nodeStatusIds.Add(activeAlert.ActiveNetObject);
				}
				int relatedNodeID = 0;
				if (int.TryParse(activeAlert.ActiveNetObject, out relatedNodeID))
				{
					activeAlert.RelatedNodeCaption = activeAlert.TriggeringObjectCaption;
					activeAlert.RelatedNodeID = relatedNodeID;
				}
			}
			else if (string.Compare(text2, "Orion.NPM.Interfaces", StringComparison.OrdinalIgnoreCase) == 0)
			{
				interfaceStatusIds.Add(activeAlert.ActiveNetObject);
			}
			else if (string.Compare(text2, "Orion.Groups", StringComparison.OrdinalIgnoreCase) == 0)
			{
				containerStatusIds.Add(activeAlert.ActiveNetObject);
			}
			string text3 = (rAlert["NetObjectPrefix"] != DBNull.Value) ? Convert.ToString(rAlert["NetObjectPrefix"]) : string.Empty;
			if (!string.IsNullOrEmpty(text3))
			{
				activeAlert.TriggeringObjectDetailsUrl = string.Format("/Orion/View.aspx?NetObject={0}:{1}", text3, activeAlert.ActiveNetObject);
			}
			else
			{
				activeAlert.TriggeringObjectDetailsUrl = string.Empty;
			}
			activeAlert.ActiveTime = currentDateTime - activeAlert.TriggerDateTime;
			activeAlert.ActiveTimeDisplay = new ActiveAlertDAL().ActiveTimeToDisplayFormat(activeAlert.ActiveTime);
			activeAlert.RelatedNodeDetailsUrl = string.Format("/Orion/View.aspx?NetObject=N:{0}", activeAlert.RelatedNodeID);
			activeAlert.AcknowledgedBy = ((rAlert["AcknowledgedBy"] != DBNull.Value) ? Convert.ToString(rAlert["AcknowledgedBy"]) : string.Empty);
			acknowledgedBy.Add(activeAlert.AcknowledgedBy);
			activeAlert.AcknowledgedDateTime = ((rAlert["AcknowledgedTime"] != DBNull.Value) ? DateTime.SpecifyKind(Convert.ToDateTime(rAlert["AcknowledgedTime"]), DateTimeKind.Local) : DateTime.MinValue);
			string text4 = string.Format("{0} - ", activeAlert.RelatedNodeCaption);
			if (activeAlert.TriggeringObjectCaption.StartsWith(text4))
			{
				activeAlert.TriggeringObjectCaption = activeAlert.TriggeringObjectCaption.Substring(text4.Length, activeAlert.TriggeringObjectCaption.Length - text4.Length);
			}
			activeAlert.ObjectType = text;
			activeAlert.Severity = ActiveAlertSeverity.Warning;
			activeAlert.LegacyAlert = true;
			return activeAlert;
		}

		// Token: 0x0600072F RID: 1839 RVA: 0x000306EC File Offset: 0x0002E8EC
		internal static string EscapeLikeValue(string value)
		{
			if (value.StartsWith("%") && value.EndsWith("%") && value.Length >= 2)
			{
				value = value.Substring(1, value.Length - 2);
				return string.Format("%{0}%", AlertDAL.EscapeLikeValueInternal(value));
			}
			return AlertDAL.EscapeLikeValueInternal(value);
		}

		// Token: 0x06000730 RID: 1840 RVA: 0x00030744 File Offset: 0x0002E944
		private static string EscapeLikeValueInternal(string value)
		{
			StringBuilder stringBuilder = new StringBuilder(value.Length);
			int i = 0;
			while (i < value.Length)
			{
				char c = value[i];
				if (c <= '\'')
				{
					if (c == '%')
					{
						goto IL_38;
					}
					if (c != '\'')
					{
						goto IL_64;
					}
					stringBuilder.Append("''");
				}
				else
				{
					if (c == '*' || c == '[' || c == ']')
					{
						goto IL_38;
					}
					goto IL_64;
				}
				IL_6C:
				i++;
				continue;
				IL_38:
				stringBuilder.Append("[").Append(c).Append("]");
				goto IL_6C;
				IL_64:
				stringBuilder.Append(c);
				goto IL_6C;
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06000731 RID: 1841 RVA: 0x000307D0 File Offset: 0x0002E9D0
		[Obsolete("Method does not return V2 alerts.")]
		public static ActiveAlert GetActiveAlert(string activeAlertDefId, string activeNetObject, string objectType, IEnumerable<int> limitationIDs)
		{
			DataRow[] array = AlertDAL.GetSortableAlertTable(string.Empty, string.Empty, activeAlertDefId, "AlertTime DESC", int.MaxValue, true, limitationIDs.ToList<int>(), true).Select(string.Format("ActiveNetObject='{0}' AND ObjectType='{1}'", activeNetObject, objectType));
			DataTable tblNetObjectTypes = AlertDAL.GetAvailableObjectTypes(false);
			Func<string, ActiveAlertType, string> func = delegate(string objectType2, ActiveAlertType alertType)
			{
				DataRow[] array2;
				if (alertType == ActiveAlertType.Advanced)
				{
					array2 = tblNetObjectTypes.Select("Name='" + objectType2 + "'");
				}
				else
				{
					array2 = tblNetObjectTypes.Select("Prefix='" + objectType2 + "'");
				}
				if (array2.Length == 0)
				{
					return string.Empty;
				}
				if (array2[0]["EntityType"] == DBNull.Value)
				{
					return string.Empty;
				}
				return Convert.ToString(array2[0]["EntityType"]);
			};
			if (array.Length != 0)
			{
				List<string> list = new List<string>();
				List<string> list2 = new List<string>();
				List<string> list3 = new List<string>();
				List<string> list4 = new List<string>();
				ActiveAlert activeAlert = AlertDAL.SortableAlertDataRowToActiveAlertObject(array[0], func, DateTime.Now, list, list2, list3, list4);
				Dictionary<string, int> statusesForSwisEntities = AlertDAL.GetStatusesForSwisEntities("Orion.Nodes", "NodeID", list, false);
				Dictionary<string, int> statusesForSwisEntities2 = AlertDAL.GetStatusesForSwisEntities("Orion.NPM.Interfaces", "InterfaceID", list2, false);
				Dictionary<string, int> statusesForSwisEntities3 = AlertDAL.GetStatusesForSwisEntities("Orion.Groups", "ContainerID", list3, false);
				Dictionary<string, string> fullUserNames = AlertDAL.GetFullUserNames(list4, false);
				string strA = func(activeAlert.ObjectType, activeAlert.AlertType);
				if (string.Compare(strA, "Orion.Nodes", StringComparison.OrdinalIgnoreCase) == 0 && statusesForSwisEntities.ContainsKey(activeAlert.ActiveNetObject))
				{
					activeAlert.TriggeringObjectStatus = statusesForSwisEntities[activeAlert.ActiveNetObject];
				}
				else if (string.Compare(strA, "Orion.NPM.Interfaces", StringComparison.OrdinalIgnoreCase) == 0 && statusesForSwisEntities2.ContainsKey(activeAlert.ActiveNetObject))
				{
					activeAlert.TriggeringObjectStatus = statusesForSwisEntities2[activeAlert.ActiveNetObject];
				}
				else if (string.Compare(strA, "Orion.Groups", StringComparison.OrdinalIgnoreCase) == 0 && statusesForSwisEntities3.ContainsKey(activeAlert.ActiveNetObject))
				{
					activeAlert.TriggeringObjectStatus = statusesForSwisEntities3[activeAlert.ActiveNetObject];
				}
				if (statusesForSwisEntities.ContainsKey(activeAlert.RelatedNodeID.ToString()))
				{
					activeAlert.RelatedNodeStatus = statusesForSwisEntities[activeAlert.RelatedNodeID.ToString()];
				}
				if (fullUserNames.ContainsKey(activeAlert.AcknowledgedBy))
				{
					activeAlert.AcknowledgedByFullName = fullUserNames[activeAlert.AcknowledgedBy];
				}
				activeAlert.Notes = AlertDAL.GetAlertNote(activeAlert.AlertDefId, activeAlert.ActiveNetObject, activeAlert.ObjectType);
				activeAlert.Status = ActiveAlertStatus.Triggered;
				activeAlert.EscalationLevel = 1;
				activeAlert.LegacyAlert = true;
				return activeAlert;
			}
			string query = "SELECT Name FROM Orion.AlertDefinitions WHERE AlertDefID=@alertDefId AND ObjectType=@objectType";
			DataTable dataTable = AlertDAL.SwisProxy.QueryWithAppendedErrors(query, new Dictionary<string, object>
			{
				{
					"alertDefId",
					activeAlertDefId
				},
				{
					"objectType",
					objectType
				}
			});
			if (dataTable.Rows.Count > 0)
			{
				ActiveAlert activeAlert2 = new ActiveAlert();
				activeAlert2.LegacyAlert = false;
				activeAlert2.Status = ActiveAlertStatus.NotTriggered;
				activeAlert2.Name = ((dataTable.Rows[0]["Name"] != DBNull.Value) ? Convert.ToString(dataTable.Rows[0]["Name"]) : string.Empty);
				activeAlert2.Severity = ActiveAlertSeverity.Warning;
				activeAlert2.ObjectType = objectType;
				activeAlert2.TriggeringObjectEntityName = func(objectType, activeAlert2.AlertType);
				activeAlert2.ActiveNetObject = activeNetObject;
				string strA2 = func(activeAlert2.ObjectType, activeAlert2.AlertType);
				if (string.Compare(strA2, "Orion.Nodes", StringComparison.OrdinalIgnoreCase) == 0)
				{
					Dictionary<string, int> statusesForSwisEntities4 = AlertDAL.GetStatusesForSwisEntities("Orion.Nodes", "NodeID", new List<string>
					{
						activeNetObject
					}, false);
					if (statusesForSwisEntities4.ContainsKey(activeAlert2.ActiveNetObject))
					{
						activeAlert2.TriggeringObjectStatus = statusesForSwisEntities4[activeAlert2.ActiveNetObject];
					}
				}
				else if (string.Compare(strA2, "Orion.NPM.Interfaces", StringComparison.OrdinalIgnoreCase) == 0)
				{
					Dictionary<string, int> statusesForSwisEntities5 = AlertDAL.GetStatusesForSwisEntities("Orion.NPM.Interfaces", "InterfaceID", new List<string>
					{
						activeNetObject
					}, false);
					if (statusesForSwisEntities5.ContainsKey(activeAlert2.ActiveNetObject))
					{
						activeAlert2.TriggeringObjectStatus = statusesForSwisEntities5[activeAlert2.ActiveNetObject];
					}
				}
				else if (string.Compare(strA2, "Orion.Groups", StringComparison.OrdinalIgnoreCase) == 0)
				{
					Dictionary<string, int> statusesForSwisEntities6 = AlertDAL.GetStatusesForSwisEntities("Orion.Groups", "ContainerID", new List<string>
					{
						activeNetObject
					}, false);
					if (statusesForSwisEntities6.ContainsKey(activeAlert2.ActiveNetObject))
					{
						activeAlert2.TriggeringObjectStatus = statusesForSwisEntities6[activeAlert2.ActiveNetObject];
					}
				}
				return activeAlert2;
			}
			return null;
		}

		// Token: 0x06000732 RID: 1842 RVA: 0x00030BE8 File Offset: 0x0002EDE8
		private static string GetFilterCondition(IEnumerable<ActiveAlertFilter> filters, string strCondition, FilterOperator filterOperator)
		{
			if (filters.Any<ActiveAlertFilter>())
			{
				string text = "AND";
				if (filterOperator == FilterOperator.Or)
				{
					text = "OR";
				}
				if (!string.IsNullOrEmpty(strCondition))
				{
					strCondition = string.Format("({0}) {1} ", strCondition, text);
				}
				if (filterOperator == FilterOperator.And)
				{
					strCondition += " ( 1=1 ";
				}
				else
				{
					strCondition += " ( 1<>1";
				}
				foreach (ActiveAlertFilter activeAlertFilter in filters)
				{
					if (string.Compare(activeAlertFilter.FieldDataType, "datetime", true) == 0)
					{
						strCondition = AlertDAL.GetDateTimeFilterCondition(strCondition, activeAlertFilter);
					}
					else if (string.Compare(activeAlertFilter.FieldDataType, "string", true) == 0)
					{
						if (!string.IsNullOrEmpty(activeAlertFilter.Value))
						{
							activeAlertFilter.Value = AlertDAL.EscapeLikeValue(activeAlertFilter.Value);
							if (activeAlertFilter.Comparison == ComparisonType.Equal)
							{
								strCondition += string.Format(" {2} {0} = '{1}'", activeAlertFilter.FieldName, activeAlertFilter.Value, text);
							}
							else if (activeAlertFilter.Comparison == ComparisonType.Contains)
							{
								strCondition += string.Format(" {2} {0} LIKE '%{1}%'", activeAlertFilter.FieldName, activeAlertFilter.Value, text);
							}
							else
							{
								strCondition += string.Format(" {2} {0} <> '{1}'", activeAlertFilter.FieldName, activeAlertFilter.Value, text);
							}
						}
						else if (activeAlertFilter.Comparison != ComparisonType.NotEqual)
						{
							strCondition += string.Format(" {1} ({0} = '' OR {0} IS NULL)", activeAlertFilter.FieldName, text);
						}
						else
						{
							strCondition += string.Format(" {1} ({0} <> '' OR {0} IS NOT NULL)", activeAlertFilter.FieldName, text);
						}
					}
					else if (string.Compare(activeAlertFilter.FieldDataType, "list$system.string", true) == 0)
					{
						if (!string.IsNullOrEmpty(activeAlertFilter.Value))
						{
							if (activeAlertFilter.Comparison != ComparisonType.NotEqual)
							{
								StringBuilder stringBuilder = new StringBuilder();
								foreach (string str in activeAlertFilter.Value.Split(new string[]
								{
									"$#"
								}, StringSplitOptions.None))
								{
									stringBuilder.Append("'" + str + "',");
								}
								if (stringBuilder.Length > 0)
								{
									stringBuilder = stringBuilder.Remove(stringBuilder.Length - 1, 1);
								}
								strCondition += string.Format(" {2} {0} in ({1})", activeAlertFilter.FieldName, stringBuilder, text);
							}
							else
							{
								strCondition += string.Format(" {2} {0} <> '{1}'", activeAlertFilter.FieldName, activeAlertFilter.Value, text);
							}
						}
						else if (activeAlertFilter.Comparison != ComparisonType.NotEqual)
						{
							strCondition += string.Format(" {1} ({0} = '' OR {0} IS NULL)", activeAlertFilter.FieldName, text);
						}
						else
						{
							strCondition += string.Format(" {1} ({0} <> '' OR {0} IS NOT NULL)", activeAlertFilter.FieldName, text);
						}
					}
					else if (activeAlertFilter.FieldDataType.StartsWith("bool"))
					{
						if (activeAlertFilter.Comparison == ComparisonType.Equal)
						{
							strCondition += string.Format(" {2} {0} = {1}", activeAlertFilter.FieldName, Convert.ToInt32(activeAlertFilter.Value), text);
						}
						else
						{
							strCondition += string.Format(" {2} {0} <> {1}", activeAlertFilter.FieldName, Convert.ToInt32(activeAlertFilter.Value), text);
						}
					}
					else if (string.Compare(activeAlertFilter.FieldDataType, "numeric", true) == 0)
					{
						if (activeAlertFilter.Comparison == ComparisonType.Less)
						{
							strCondition += string.Format(" {2} {0} < {1}", activeAlertFilter.FieldName, Convert.ToDouble(activeAlertFilter.Value), text);
						}
						else if (activeAlertFilter.Comparison == ComparisonType.Greater)
						{
							strCondition += string.Format(" {2} {0} > {1}", activeAlertFilter.FieldName, Convert.ToDouble(activeAlertFilter.Value), text);
						}
						else if (activeAlertFilter.Comparison == ComparisonType.Equal)
						{
							if (string.IsNullOrEmpty(activeAlertFilter.Value))
							{
								strCondition += string.Format(" {1} {0} IS Null", activeAlertFilter.FieldName, text);
							}
							else
							{
								strCondition += string.Format(" {2} {0} = {1}", activeAlertFilter.FieldName, Convert.ToDouble(activeAlertFilter.Value), text);
							}
						}
					}
				}
				strCondition += ")";
			}
			return strCondition;
		}

		// Token: 0x06000733 RID: 1843 RVA: 0x0003102C File Offset: 0x0002F22C
		private static string GetDateTimeFilterCondition(string strCondition, ActiveAlertFilter filter)
		{
			string text = DateTime.MinValue.ToString("yyyy-MM-ddTHH:mm:ss");
			string text2 = new DateTime(1899, 12, 30).ToString("yyyy-MM-ddTHH:mm:ss");
			DateTime dateTime;
			if (string.IsNullOrEmpty(filter.Value))
			{
				strCondition += string.Format(" AND ({0} IS NULL OR {0} = '{1}' OR {0} = '{2}') ", filter.FieldName, text, text2);
			}
			else if (DateTime.TryParseExact(filter.Value, "MM/dd/yyyy h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
			{
				string text3 = dateTime.ToString("yyyy-MM-ddTHH:mm:ss");
				if (filter.Comparison == ComparisonType.Equal)
				{
					strCondition += string.Format(" AND {0} = '{1}'", filter.FieldName, text3);
				}
				else if (filter.Comparison == ComparisonType.Less)
				{
					strCondition += string.Format(" AND {0} < '{1}' AND {0} > '{2}' AND {0} > '{3}'", new object[]
					{
						filter.FieldName,
						text3,
						text,
						text2
					});
				}
				else if (filter.Comparison == ComparisonType.Greater)
				{
					strCondition += string.Format(" AND {0} > '{1}'", filter.FieldName, text3);
				}
				else if (filter.Comparison == ComparisonType.NotEqual)
				{
					strCondition += string.Format(" AND {0} <> '{1}'", filter.FieldName, text3);
				}
			}
			return strCondition;
		}

		// Token: 0x06000734 RID: 1844 RVA: 0x00031168 File Offset: 0x0002F368
		[Obsolete("Method does not return V2 alerts.")]
		public static string GetAdvAlertSwql(List<int> limitationIDs)
		{
			return AlertDAL.GetAdvAlertSwql(string.Empty, string.Empty, limitationIDs, false, false);
		}

		// Token: 0x06000735 RID: 1845 RVA: 0x0003117C File Offset: 0x0002F37C
		[Obsolete("Method does not return V2 alerts.")]
		public static string GetAdvAlertSwql(string whereClause, string advAlertsLabel, List<int> limitationIDs, bool includeDefaultFields, bool includeToolsetFields)
		{
			return AlertDAL.GetAdvAlertSwql(whereClause, string.Empty, string.Empty, advAlertsLabel, limitationIDs, includeDefaultFields, includeToolsetFields);
		}

		// Token: 0x06000736 RID: 1846 RVA: 0x00031194 File Offset: 0x0002F394
		[Obsolete("Method does not return V2 alerts.")]
		public static string GetAdvAlertSwql(string whereClause, string netObjectWhereClause, string netObject, string advAlertsLabel, List<int> limitationIDs, bool includeDefaultFields, bool includeToolsetFields)
		{
			StringBuilder stringBuilder = new StringBuilder();
			string value = string.Empty;
			List<string> activeNetObjectTypes = AlertDAL.GetActiveNetObjectTypes(whereClause);
			List<NetObjectType> list;
			if (activeNetObjectTypes != null && activeNetObjectTypes.Count != 0)
			{
				list = ModuleAlertsMap.NetObjectTypes.Items;
			}
			else
			{
				(list = new List<NetObjectType>()).Add(ModuleAlertsMap.NetObjectTypes.Items[0]);
			}
			string format = "{0}";
			using (List<NetObjectType>.Enumerator enumerator = list.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					NetObjectType noType = enumerator.Current;
					if (activeNetObjectTypes.Count <= 0 || activeNetObjectTypes.Any((string netObj) => string.Equals(netObj, noType.Name, StringComparison.OrdinalIgnoreCase)))
					{
						string triggeredAlertsQuery = ModuleAlertsMap.GetTriggeredAlertsQuery(noType.Name, includeDefaultFields, includeToolsetFields);
						AlertDAL.Log.DebugFormat("Query Template for {0}  : {1} ", noType.Name, triggeredAlertsQuery);
						if (!string.IsNullOrEmpty(triggeredAlertsQuery))
						{
							string parentNetObjectCondition = ModuleAlertsMap.GetParentNetObjectCondition(noType.Name, netObject);
							string arg = string.Format("{0} {1}", whereClause, string.IsNullOrEmpty(parentNetObjectCondition) ? netObjectWhereClause : string.Format(" AND {0}", parentNetObjectCondition));
							stringBuilder.AppendLine(value);
							string arg2 = string.Format(triggeredAlertsQuery, arg, advAlertsLabel);
							stringBuilder.AppendFormat(format, arg2);
							format = "({0})";
							value = " UNION ";
						}
					}
				}
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06000737 RID: 1847 RVA: 0x00031300 File Offset: 0x0002F500
		[Obsolete("Old alerting will be removed")]
		private static List<string> GetActiveNetObjectTypes(string whereClause)
		{
			List<string> list = new List<string>();
			string arg = string.Empty;
			Match match = AlertDAL._ackRegex.Match(whereClause);
			if (match.Success)
			{
				arg = string.Format("AND {0}", match.Value);
			}
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Format("Select DISTINCT AlertStatus.ObjectType from AlertDefinitions WITH(NOLOCK)\r\nINNER JOIN AlertStatus WITH(NOLOCK) ON AlertStatus.AlertDefID = AlertDefinitions.AlertDefID Where (AlertStatus.State=2 OR AlertStatus.State=3) {0}", arg)))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						list.Add(DatabaseFunctions.GetString(dataReader, "ObjectType"));
					}
				}
			}
			return list;
		}

		// Token: 0x06000738 RID: 1848 RVA: 0x000313AC File Offset: 0x0002F5AC
		[Obsolete("Method does not return V2 alerts.")]
		public static DataTable GetPageableAlerts(List<int> limitationIDs, string period, int fromRow, int toRow, string type, string alertId, bool showAcknAlerts)
		{
			DataTable dataTable = null;
			List<SqlParameter> list = new List<SqlParameter>();
			string[] array = period.Split(new char[]
			{
				'~'
			});
			DateTime dateTime = DateTime.Parse(array[0]).ToLocalTime();
			DateTime dateTime2 = DateTime.Parse(array[1]).ToLocalTime();
			list.Add(new SqlParameter("@StartDate", SqlDbType.DateTime)
			{
				Value = dateTime
			});
			list.Add(new SqlParameter("@EndDate", SqlDbType.DateTime)
			{
				Value = dateTime2
			});
			string text = "IF OBJECT_ID('tempdb..#Nodes') IS NOT NULL\tDROP TABLE #Nodes\r\nSELECT * INTO #Nodes FROM Nodes WHERE 1=1;";
			string text2 = Limitation.LimitSQL(text, limitationIDs);
			bool flag = AlertDAL._enableLimitationReplacement && text2.Length / text.Length > AlertDAL._limitationSqlExaggeration;
			text2 = (flag ? text2 : string.Empty);
			text2 = (flag ? text2 : string.Empty);
			string text3 = flag ? "IF OBJECT_ID('tempdb..#Nodes') IS NOT NULL\tDROP TABLE #Nodes" : string.Empty;
			string a = type.ToUpper();
			string text6;
			if (!(a == "ADVANCED"))
			{
				if (!(a == "BASIC"))
				{
					string whereClause = AlertDAL.GetWhereClause(alertId, showAcknAlerts, list);
					string text4 = string.Format("Select *, 0 as BAlertID, 'Advanced' AS AlertType From ({0}) AAT", AlertDAL.GetAdvAlertSwql(whereClause, string.Empty, string.Empty, OrionMessagesHelper.GetMessageTypeString(OrionMessageType.ADVANCED_ALERT), limitationIDs, true, true));
					string text5 = string.Format("Select *, BAT.AlertID as BAlertID, 'Basic' AS AlertType From ({0}) BAT", AlertDAL.GetBasicAlertSwql(string.Empty, string.Empty, alertId, limitationIDs, true, true));
					text6 = string.Format("{4}Select * from (\r\nSELECT a.AlertTime, a.AlertName, \r\na.AlertType, Case a.AlertType When 'Advanced' Then a.ObjectName Else a.ObjectType + '::' + a.ObjectName End As ObjectName, a.Acknowledged, a.AlertID, \r\na.BAlertID, a.ObjectType, a.ObjectID as ActiveObject, ROW_NUMBER() OVER (ORDER BY a.ObjectName, a.AlertName) AS Row \r\nFROM ( {0} Union ( {1} ))a Where a.AlertTime >= @StartDate And a.AlertTime <= @EndDate\r\n) t \r\nWHERE Row BETWEEN {2} AND {3} Order By t.ObjectName, t.AlertName\r\n{5}", new object[]
					{
						text4,
						text5,
						fromRow,
						toRow,
						text2,
						text3
					});
				}
				else
				{
					string text5 = AlertDAL.GetBasicAlertSwql(string.Empty, string.Empty, alertId, limitationIDs, true, true);
					text6 = string.Format("Select * from (\r\nSELECT a.AlertTime, a.AlertName, 'Basic' AS AlertType, a.ObjectType + '::' + a.ObjectName As ObjectName, \r\na.Acknowledged, a.AlertID, a.AlertID as BAlertID, a.ObjectType, a.ObjectID as ActiveObject, ROW_NUMBER() OVER (ORDER BY a.ObjectName, a.AlertName) AS Row \r\nFROM ( {0} ) a Where a.AlertTime >= @StartDate And a.AlertTime <= @EndDate\r\n) t \r\nWHERE Row BETWEEN {1} AND {2} Order By t.ObjectName, t.AlertName", text5, fromRow, toRow);
				}
			}
			else
			{
				string whereClause = AlertDAL.GetWhereClause(alertId, showAcknAlerts, list);
				string text4 = AlertDAL.GetAdvAlertSwql(whereClause, string.Empty, string.Empty, OrionMessagesHelper.GetMessageTypeString(OrionMessageType.ADVANCED_ALERT), limitationIDs, true, true);
				text6 = string.Format("{3}Select * from (\r\nSELECT a.AlertTime, a.AlertName, 'Advanced' AS AlertType, a.ObjectName, a.Acknowledged, a.AlertID, 0 as BAlertID, a.ObjectType, \r\na.ObjectID as ActiveObject, ROW_NUMBER() OVER (ORDER BY a.ObjectName, a.AlertName) AS Row \r\nFROM ( {0} )a Where a.AlertTime >= @StartDate And a.AlertTime <= @EndDate\r\n) t \r\nWHERE Row BETWEEN {1} AND {2} Order By t.ObjectName, t.AlertName\r\n{4}", new object[]
				{
					text4,
					fromRow,
					toRow,
					text2,
					text3
				});
			}
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(text6))
			{
				textCommand.Parameters.AddRange(list.ToArray());
				dataTable = SqlHelper.ExecuteDataTable(textCommand);
			}
			dataTable.TableName = "Alerts";
			return dataTable;
		}

		// Token: 0x06000739 RID: 1849 RVA: 0x00031620 File Offset: 0x0002F820
		private static string GetWhereClause(string alertId, bool showAcknAlerts, List<SqlParameter> sqlParams)
		{
			StringBuilder stringBuilder = new StringBuilder(" AND (AlertStatus.State=@Triggered) ");
			sqlParams.Add(new SqlParameter("@Triggered", SqlDbType.TinyInt)
			{
				Value = 2
			});
			if (!showAcknAlerts)
			{
				stringBuilder.Append(" AND AlertStatus.Acknowledged=@Acknowledged ");
				sqlParams.Add(new SqlParameter("@Acknowledged", SqlDbType.TinyInt)
				{
					Value = 0
				});
			}
			Guid guid;
			if (Guid.TryParse(alertId, out guid))
			{
				stringBuilder.Append(" AND (AlertStatus.AlertDefID=@AlertDefID) ");
				sqlParams.Add(new SqlParameter("@AlertDefID", SqlDbType.UniqueIdentifier)
				{
					Value = guid
				});
			}
			return stringBuilder.ToString();
		}

		// Token: 0x0600073A RID: 1850 RVA: 0x000316BD File Offset: 0x0002F8BD
		[Obsolete("Don't use this method anymore. Old alerts will be removed.")]
		public static string GetBasicAlertSwql(List<int> limitationIDs)
		{
			return AlertDAL.GetBasicAlertSwql(string.Empty, string.Empty, string.Empty, limitationIDs, false, false);
		}

		// Token: 0x0600073B RID: 1851 RVA: 0x000316D8 File Offset: 0x0002F8D8
		[Obsolete("Don't use this method anymore. Old alerts will be removed.")]
		public static string GetBasicAlertSwql(string netObject, string deviceType, string alertId, List<int> limitationIDs, bool includeDefaultFields, bool includeToolsetFields)
		{
			string text = string.Empty;
			int num = 0;
			if (!string.IsNullOrEmpty(netObject))
			{
				string[] array = netObject.Split(new char[]
				{
					':'
				});
				if (array.Length == 2)
				{
					text = array[0];
					num = Convert.ToInt32(array[1]);
				}
			}
			StringBuilder stringBuilder = new StringBuilder();
			if (num != 0)
			{
				if (text == "N")
				{
					stringBuilder.AppendFormat(" AND (ActiveAlerts.NodeID={0}) ", num);
				}
				else
				{
					stringBuilder.AppendFormat(" AND (ObjectType='{0}' AND ObjectID={1}) ", text, num);
				}
			}
			else if (!string.IsNullOrEmpty(deviceType))
			{
				stringBuilder.AppendFormat(" AND (MachineType Like '{0}') ", deviceType);
			}
			int num2;
			if (int.TryParse(alertId, out num2))
			{
				stringBuilder.AppendFormat(" AND (ActiveAlerts.AlertID={0}) ", num2);
			}
			else if (!string.IsNullOrEmpty(alertId))
			{
				stringBuilder.Append(" AND (ActiveAlerts.AlertID=0) ");
			}
			return AlertDAL.GetBasicAlertSwql(stringBuilder.ToString(), string.Empty, limitationIDs, includeDefaultFields, includeToolsetFields);
		}

		// Token: 0x0600073C RID: 1852 RVA: 0x000317BC File Offset: 0x0002F9BC
		[Obsolete("Don't use this method anymore. Old alerts will be removed.")]
		public static string GetBasicAlertSwql(string whereClause, string basicAlertsLabel, List<int> limitationIDs, bool includeDefaultFields, bool includeToolsetFields)
		{
			bool flag = AlertDAL.IsInterfacesAllowed();
			StringBuilder stringBuilder = new StringBuilder("SELECT ");
			if (includeDefaultFields)
			{
				stringBuilder.AppendFormat("\r\nCAST(ActiveAlerts.AlertID AS NVARCHAR(38)) AS AlertID,\r\nAlerts.AlertName AS AlertName,\r\nActiveAlerts.AlertTime AS AlertTime, \r\nCAST(ActiveAlerts.ObjectID AS NVARCHAR(38)) AS ObjectID, \r\nCASE WHEN ActiveAlerts.ObjectType = 'N' THEN ActiveAlerts.ObjectName ELSE ActiveAlerts.NodeName + '-' + ActiveAlerts.ObjectName END AS ObjectName,\r\nActiveAlerts.ObjectType AS ObjectType,\r\n'0' AS Acknowledged,\r\n'' AS AcknowledgedBy, \r\n'18991230' AS AcknowledgedTime, \r\nCAST(ActiveAlerts.EventMessage AS NVARCHAR(1024)) AS EventMessage,\r\n{0} AS MonitoredProperty, \r\n", (!string.IsNullOrEmpty(basicAlertsLabel)) ? string.Format("'{0}'", basicAlertsLabel) : "ActiveAlerts.MonitoredProperty");
			}
			if (includeToolsetFields)
			{
				stringBuilder.Append("\r\nNodes.IP_Address AS IP_Address, \r\nNodes.DNS AS DNS, \r\nNodes.[SysName] AS [Sysname], \r\nNodes.[Community] AS [Community], \r\n");
				if (flag)
				{
					stringBuilder.Append("\r\nInterfaces.InterfaceName AS InterfaceName, \r\nInterfaces.InterfaceIndex AS InterfaceIndex,\r\n");
				}
				else
				{
					stringBuilder.Append("\r\nNULL AS InterfaceName, \r\nNULL AS InterfaceIndex,\r\n");
				}
			}
			stringBuilder.Append("\r\nActiveAlerts.CurrentValue AS CurrentValue, \r\nCAST(ActiveAlerts.ObjectID AS NVARCHAR(38)) AS ActiveNetObject, \r\nActiveAlerts.ObjectType AS NetObjectPrefix, \r\nNodes.NodeID AS NodeID\r\nFROM ActiveAlerts\r\nINNER JOIN Nodes WITH(NOLOCK) ON ActiveAlerts.NodeID = Nodes.NodeID\r\nINNER JOIN Alerts WITH(NOLOCK) ON ActiveAlerts.AlertID = Alerts.AlertID ");
			if (includeToolsetFields && flag)
			{
				stringBuilder.Append(" LEFT OUTER JOIN Interfaces WITH(NOLOCK) ON ActiveAlerts.ObjectID = Interfaces.InterfaceID AND ActiveAlerts.ObjectType = 'I' ");
			}
			stringBuilder.AppendLine(" WHERE 1=1 ");
			stringBuilder.Append(whereClause);
			return Limitation.LimitSQL(stringBuilder.ToString(), limitationIDs);
		}

		// Token: 0x0600073D RID: 1853 RVA: 0x00031874 File Offset: 0x0002FA74
		[Obsolete("Don't use this method anymore. Old alerts will be removed.")]
		public static List<Node> GetAlertNetObjects(List<int> limitationIDs)
		{
			List<Node> list = new List<Node>();
			string text = string.Format("Select * FROM Nodes WITH(NOLOCK)  \r\n                                    WHERE Nodes.NodeID IN (\r\n\t\t\t\t\t\t\t\t\tSelect DISTINCT NodeID FROM({0} UNION {1}) as t1\r\n\t\t\t\t\t\t\t\t\t) Order By Caption", AlertDAL.GetAdvAlertSwql(limitationIDs), AlertDAL.GetBasicAlertSwql(limitationIDs));
			bool[] getObjects = new bool[2];
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(text))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						list.Add(NodeDAL.CreateNode(dataReader, getObjects));
					}
				}
			}
			return list;
		}

		// Token: 0x0600073E RID: 1854 RVA: 0x00031904 File Offset: 0x0002FB04
		[Obsolete("Don't use this method anymore. Old alerts will be removed.")]
		public static Dictionary<int, string> GetNodeData(List<int> limitationIDs)
		{
			return AlertDAL.GetNodeData(limitationIDs, true);
		}

		// Token: 0x0600073F RID: 1855 RVA: 0x00031910 File Offset: 0x0002FB10
		[Obsolete("Don't use this method anymore. Old alerts will be removed.")]
		public static Dictionary<int, string> GetNodeData(List<int> limitationIDs, bool includeBasic)
		{
			Dictionary<int, string> dictionary = new Dictionary<int, string>();
			string text = Limitation.LimitSQL("Select Top 1 NodeID from Nodes", limitationIDs);
			bool flag = AlertDAL._enableLimitationReplacement && text.Length / "Select Top 1 NodeID from Nodes".Length > AlertDAL._limitationSqlExaggeration;
			string text2 = flag ? "IF OBJECT_ID('tempdb..#Nodes') IS NOT NULL\tDROP TABLE #Nodes" : string.Empty;
			string text3 = flag ? "#Nodes" : "Nodes";
			string text4 = flag ? "IF OBJECT_ID('tempdb..#Nodes') IS NOT NULL\tDROP TABLE #Nodes\r\nSELECT * INTO #Nodes FROM Nodes WHERE 1=1;" : string.Empty;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Format("{4}\r\nSelect {3}.NodeID, {3}.Caption\r\nFROM {3} \r\nWhere {3}.NodeID IN\r\n(Select DISTINCT NodeID FROM({0}{1}) as t1) \r\nOrder By Caption \r\n{2}", new object[]
			{
				AlertDAL.GetAdvAlertSwql(limitationIDs),
				includeBasic ? (" UNION " + AlertDAL.GetBasicAlertSwql(limitationIDs)) : string.Empty,
				text2,
				text3,
				text4
			})))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						dictionary.Add(DatabaseFunctions.GetInt32(dataReader, "NodeID"), DatabaseFunctions.GetString(dataReader, "Caption"));
					}
				}
			}
			return dictionary;
		}

		// Token: 0x06000740 RID: 1856 RVA: 0x00031A34 File Offset: 0x0002FC34
		[Obsolete("Old alerting will be removed")]
		public static void AcknowledgeAlertsAction(List<string> alertKeys, string accountID, bool fromEmail, string acknowledgeNotes)
		{
			AlertDAL.AcknowledgeAlertsAction(alertKeys, accountID, fromEmail ? AlertAcknowledgeType.Email : AlertAcknowledgeType.Website, acknowledgeNotes);
		}

		// Token: 0x06000741 RID: 1857 RVA: 0x00031A45 File Offset: 0x0002FC45
		[Obsolete("Old alerting will be removed")]
		public static void AcknowledgeAlertsAction(List<string> alertKeys, string accountId, AlertAcknowledgeType acknowledgeType, string acknowledgeNotes)
		{
			AlertDAL.AcknowledgeAlertsAction(alertKeys, accountId, acknowledgeNotes, AlertsHelper.GetAcknowledgeMethodDisplayString(acknowledgeType));
		}

		// Token: 0x06000742 RID: 1858 RVA: 0x00031A58 File Offset: 0x0002FC58
		[Obsolete("Old alerting will be removed")]
		public static int AcknowledgeAlertsAction(List<string> alertKeys, string accountID, string acknowledgeNotes, string method)
		{
			int num = 0;
			using (List<string>.Enumerator enumerator = alertKeys.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					string alertId;
					string netObjectId;
					string objectType;
					if (AlertsHelper.TryParseAlertKey(enumerator.Current, out alertId, out netObjectId, out objectType) && AlertDAL.AcknowledgeAlert(alertId, netObjectId, objectType, accountID, acknowledgeNotes, method) == AlertAcknowledgeResult.Acknowledged)
					{
						num++;
					}
				}
			}
			return num;
		}

		// Token: 0x06000743 RID: 1859 RVA: 0x00031AC0 File Offset: 0x0002FCC0
		[Obsolete("Old alerting will be removed")]
		public static AlertAcknowledgeResult AcknowledgeAlert(string alertId, string netObjectId, string objectType, string accountId, string acknowledgeNotes, string method)
		{
			string text = string.Empty;
			if (!string.IsNullOrEmpty(acknowledgeNotes))
			{
				text = ", Notes = CAST(ISNULL(Notes, '') AS NVARCHAR(MAX)) + @Notes";
			}
			AlertAcknowledgeResult result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Format("\r\nBEGIN TRAN\r\n\r\nDECLARE @acknowleged smallint;\r\nSET @acknowleged = -1;\r\n\r\nSELECT @acknowleged = Acknowledged  FROM [AlertStatus] \r\nWHERE AlertDefID =  @AlertDefID AND ActiveObject = @ActiveObject AND ObjectType LIKE @ObjectType\r\n\r\nIF(@acknowleged = 0)\r\nBEGIN\r\n\tUPDATE AlertStatus SET \r\n                                    Acknowledged = 1, \r\n                                    AcknowledgedBy = @AcknowledgedBy,\r\n                                    AcknowledgedTime = GETDATE(),\r\n          LastUpdate = GETDATE() {0}\r\n          WHERE AlertDefID = @AlertDefID AND ActiveObject = @ActiveObject AND ObjectType LIKE @ObjectType AND Acknowledged = 0\r\nEND\r\n\r\nSELECT @acknowleged\r\n\r\nCOMMIT", text)))
			{
				string acknowledgeUsername = AlertsHelper.GetAcknowledgeUsername(accountId, method);
				textCommand.Parameters.AddWithValue("@AcknowledgedBy", acknowledgeUsername);
				textCommand.Parameters.AddWithValue("@AlertDefID", alertId);
				textCommand.Parameters.AddWithValue("@ActiveObject", netObjectId);
				textCommand.Parameters.AddWithValue("@ObjectType", objectType);
				text = string.Format(Resources.COREBUSINESSLAYERDAL_CODE_YK0_7, Environment.NewLine, acknowledgeNotes);
				textCommand.Parameters.AddWithValue("@Notes", text);
				result = (AlertAcknowledgeResult)Convert.ToInt32(SqlHelper.ExecuteScalar(textCommand));
			}
			return result;
		}

		// Token: 0x06000744 RID: 1860 RVA: 0x00031B94 File Offset: 0x0002FD94
		[Obsolete("Old alerting will be removed")]
		public static void UnacknowledgeAlertsAction(List<string> alertKeys, string accountID, AlertAcknowledgeType acknowledgeType)
		{
			using (List<string>.Enumerator enumerator = alertKeys.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					string value;
					string value2;
					string value3;
					if (AlertsHelper.TryParseAlertKey(enumerator.Current, out value, out value2, out value3))
					{
						using (SqlCommand textCommand = SqlHelper.GetTextCommand("UPDATE AlertStatus SET \r\n                                    Acknowledged = 0, \r\n                                    AcknowledgedBy = @AcknowledgedBy,\r\n                                    AcknowledgedTime = GETDATE(),\r\n                                    LastUpdate = GETDATE()\r\n                                   WHERE AlertDefID = @AlertDefID\r\n                                     AND ActiveObject = @ActiveObject\r\n                                     AND ObjectType LIKE @ObjectType\r\n                                     AND Acknowledged = 1"))
						{
							textCommand.Parameters.AddWithValue("@AcknowledgedBy", AlertsHelper.GetAcknowledgeUsername(accountID, acknowledgeType));
							textCommand.Parameters.AddWithValue("@AlertDefID", value);
							textCommand.Parameters.AddWithValue("@ActiveObject", value2);
							textCommand.Parameters.AddWithValue("@ObjectType", value3);
							SqlHelper.ExecuteNonQuery(textCommand);
						}
					}
				}
			}
		}

		// Token: 0x06000745 RID: 1861 RVA: 0x00031C70 File Offset: 0x0002FE70
		[Obsolete("Old alerting will be removed")]
		public static void AcknowledgeAlertsAction(List<string> alertKeys, string accountID)
		{
			AlertDAL.AcknowledgeAlertsAction(alertKeys, accountID, false);
		}

		// Token: 0x06000746 RID: 1862 RVA: 0x00031C7A File Offset: 0x0002FE7A
		[Obsolete("Old alerting will be removed")]
		public static void AcknowledgeAlertsAction(List<string> alertKeys, string accountID, bool fromEmail)
		{
			AlertDAL.AcknowledgeAlertsAction(alertKeys, accountID, fromEmail, null);
		}

		// Token: 0x06000747 RID: 1863 RVA: 0x00031C88 File Offset: 0x0002FE88
		[Obsolete("Old alerting will be removed")]
		public static void ClearTriggeredAlert(List<string> alertKeys)
		{
			Regex regex = new Regex("^(\\{){0,1}[0-9a-fA-F]{8}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{12}(\\}){0,1}$", RegexOptions.Compiled);
			using (List<string>.Enumerator enumerator = alertKeys.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					string text;
					string value;
					string value2;
					if (AlertsHelper.TryParseAlertKey(enumerator.Current, out text, out value, out value2))
					{
						string empty = string.Empty;
						if (regex.IsMatch(text))
						{
							using (SqlCommand textCommand = SqlHelper.GetTextCommand("DELETE FROM AlertStatus WHERE AlertDefID = @AlertDefID \r\n                                    AND ActiveObject = @ActiveObject AND ObjectType LIKE @ObjectType"))
							{
								textCommand.Parameters.AddWithValue("@AlertDefID", text);
								textCommand.Parameters.AddWithValue("@ActiveObject", value);
								textCommand.Parameters.AddWithValue("@ObjectType", value2);
								SqlHelper.ExecuteNonQuery(textCommand);
								continue;
							}
						}
						using (SqlCommand textCommand2 = SqlHelper.GetTextCommand("DELETE FROM ActiveAlerts WHERE AlertID=@alertID AND ObjectID=@activeObject AND ObjectType LIKE @objectType"))
						{
							textCommand2.Parameters.AddWithValue("@alertID", text);
							textCommand2.Parameters.AddWithValue("@activeObject", value);
							textCommand2.Parameters.AddWithValue("@objectType", value2);
							SqlHelper.ExecuteNonQuery(textCommand2);
						}
					}
				}
			}
		}

		// Token: 0x06000748 RID: 1864 RVA: 0x00031DD0 File Offset: 0x0002FFD0
		[Obsolete("Old alerting will be removed")]
		public static int EnableAdvancedAlert(Guid alertDefID, bool enabled)
		{
			int result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("\r\nSET NOCOUNT OFF;\r\nUPDATE [AlertDefinitions]\r\n SET [Enabled]=@enabled\r\n WHERE AlertDefID = @AlertDefID"))
			{
				textCommand.Parameters.Add("@AlertDefID", SqlDbType.UniqueIdentifier).Value = alertDefID;
				textCommand.Parameters.Add("@enabled", SqlDbType.Bit).Value = enabled;
				result = SqlHelper.ExecuteNonQuery(textCommand);
			}
			return result;
		}

		// Token: 0x06000749 RID: 1865 RVA: 0x00031E48 File Offset: 0x00030048
		[Obsolete("Old alerting will be removed")]
		public static int EnableAdvancedAlerts(List<string> alertDefIDs, bool enabled, bool enableAll)
		{
			if (alertDefIDs.Count == 0)
			{
				return 0;
			}
			string arg = string.Empty;
			string arg2 = string.Empty;
			if (!enableAll)
			{
				foreach (string arg3 in alertDefIDs)
				{
					arg = string.Format("{0}{1}'{2}'", arg, arg2, arg3);
					arg2 = ", ";
				}
			}
			int result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Format("\r\nSET NOCOUNT OFF;\r\nUPDATE [AlertDefinitions]\r\n SET [Enabled]=@enabled\r\n{0}", enableAll ? string.Empty : string.Format("WHERE AlertDefID in ({0})", arg))))
			{
				textCommand.Parameters.Add("@enabled", SqlDbType.Bit).Value = enabled;
				result = SqlHelper.ExecuteNonQuery(textCommand);
			}
			return result;
		}

		// Token: 0x0600074A RID: 1866 RVA: 0x00031F24 File Offset: 0x00030124
		[Obsolete("Old alerting will be removed")]
		public static int RemoveAdvancedAlert(Guid alertDefID)
		{
			int result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("\r\nSET NOCOUNT OFF;\r\nDelete FROM [AlertDefinitions]\r\n WHERE AlertDefID = @AlertDefID"))
			{
				textCommand.Parameters.Add("@AlertDefID", SqlDbType.UniqueIdentifier).Value = alertDefID;
				result = SqlHelper.ExecuteNonQuery(textCommand);
			}
			return result;
		}

		// Token: 0x0600074B RID: 1867 RVA: 0x00031F80 File Offset: 0x00030180
		public static int RemoveAdvancedAlerts(List<string> alertDefIDs, bool deleteAll)
		{
			if (alertDefIDs.Count == 0)
			{
				return 0;
			}
			string arg = string.Empty;
			string arg2 = string.Empty;
			if (!deleteAll)
			{
				foreach (string arg3 in alertDefIDs)
				{
					arg = string.Format("{0}{1}'{2}'", arg, arg2, arg3);
					arg2 = ", ";
				}
			}
			int result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Format("\r\nSET NOCOUNT OFF;\r\nDelete FROM [AlertDefinitions]\r\n{0}", deleteAll ? string.Empty : string.Format("WHERE AlertDefID in ({0})", arg))))
			{
				result = SqlHelper.ExecuteNonQuery(textCommand);
			}
			return result;
		}

		// Token: 0x0600074C RID: 1868 RVA: 0x00032040 File Offset: 0x00030240
		[Obsolete("Old alerting will be removed")]
		public static int AdvAlertsCount()
		{
			int result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT COUNT(AlertDefID) AS TotalCount FROM [AlertDefinitions]"))
			{
				result = Convert.ToInt32(SqlHelper.ExecuteScalar(textCommand));
			}
			return result;
		}

		// Token: 0x0600074D RID: 1869 RVA: 0x00032084 File Offset: 0x00030284
		[Obsolete("Old alerting will be removed")]
		public static DataTable GetAdvancedAlerts()
		{
			DataTable dataTable;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("Select * from [AlertDefinitions]"))
			{
				dataTable = SqlHelper.ExecuteDataTable(textCommand);
			}
			if (dataTable != null)
			{
				dataTable.TableName = "AlertsDefinition";
			}
			return dataTable;
		}

		// Token: 0x0600074E RID: 1870 RVA: 0x000320D0 File Offset: 0x000302D0
		[Obsolete("Old alerting will be removed")]
		public static List<AlertAction> GetAdvancedAlertActions(Guid? alertDefID = null)
		{
			return OldAlertsDAL.GetAdvancedAlertActions(alertDefID);
		}

		// Token: 0x0600074F RID: 1871 RVA: 0x000320D8 File Offset: 0x000302D8
		[Obsolete("Old alerting will be removed")]
		public static AlertDefinitionOld GetAdvancedAlertDefinition(Guid alertDefID)
		{
			return OldAlertsDAL.GetAdvancedAlertDefinition(alertDefID, true);
		}

		// Token: 0x06000750 RID: 1872 RVA: 0x000320E1 File Offset: 0x000302E1
		[Obsolete("Old alerting will be removed")]
		public static List<AlertDefinitionOld> GetAdvancedAlertDefinitions()
		{
			return OldAlertsDAL.GetAdvancedAlertDefinitions(true);
		}

		// Token: 0x06000751 RID: 1873 RVA: 0x000320EC File Offset: 0x000302EC
		[Obsolete("Old alerting will be removed")]
		public static DataTable GetAdvancedAlert(Guid alertDefID)
		{
			DataTable dataTable;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT Ald.AlertDefID, Ald.AlertName, Ald.AlertDescription, Ald.StartTime, Ald.EndTime, Ald.DOW, Ald.Enabled, Ald.TriggerSustained, Ald.ExecuteInterval, Ald.IgnoreTimeout,\r\n\t\t\t\t\t\t\tAld.TriggerQuery, Ald.ResetQuery, Ald.SuppressionQuery, \r\n\t\t\t\t\t\t\tAcd.TriggerAction, Acd.SortOrder, ActionType, Title, Target, Parameter1, Parameter2, Parameter3, Parameter4\r\n\t\t\t\tFROM [AlertDefinitions] Ald\r\n\t\t\t\tLeft Join [ActionDefinitions] Acd ON Ald.AlertDefID = Acd.AlertDefID\r\nWHERE Ald.AlertDefID=@AlertDefID\r\nOrder by Ald.AlertDefID, Acd.TriggerAction, Acd.SortOrder\r\n"))
			{
				textCommand.Parameters.Add("@AlertDefID", SqlDbType.UniqueIdentifier).Value = alertDefID;
				dataTable = SqlHelper.ExecuteDataTable(textCommand);
			}
			if (dataTable != null)
			{
				dataTable.TableName = "AlertsDefinition";
			}
			return dataTable;
		}

		// Token: 0x06000752 RID: 1874 RVA: 0x00032154 File Offset: 0x00030354
		[Obsolete("Old alerting will be removed")]
		public static int UpdateAlertDef(Guid alertDefID, bool enabled)
		{
			return OldAlertsDAL.UpdateAlertDef(alertDefID, enabled);
		}

		// Token: 0x06000753 RID: 1875 RVA: 0x00032160 File Offset: 0x00030360
		[Obsolete("Old alerting will be removed")]
		public static int UpdateAlertDef(Guid alertDefID, string alertName, string alertDescr, bool enabled, int evInterval, string dow, DateTime startTime, DateTime endTime, bool ignoreTimeout)
		{
			int result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SET NOCOUNT OFF;\r\nUpdate [AlertDefinitions]\r\n SET [AlertName] = @alertName\r\n      ,[AlertDescription] = @alertDescr\r\n      ,[Enabled] = @enabled\r\n      ,[DOW] = @dow\r\n      ,[ExecuteInterval] = @evInterval\r\n\t\t,[StartTime] = @startTime\r\n\t\t,[EndTime] = @endTime\r\n\t,[IgnoreTimeout] = @ignoreTimeout\r\n WHERE AlertDefID = @AlertDefID"))
			{
				textCommand.Parameters.Add("@AlertDefID", SqlDbType.UniqueIdentifier).Value = alertDefID;
				textCommand.Parameters.Add("@alertName", SqlDbType.NVarChar).Value = alertName;
				textCommand.Parameters.Add("@alertDescr", SqlDbType.NVarChar).Value = alertDescr;
				textCommand.Parameters.Add("@enabled", SqlDbType.Bit).Value = enabled;
				textCommand.Parameters.Add("@dow", SqlDbType.NVarChar, 16).Value = dow;
				textCommand.Parameters.Add("@evInterval", SqlDbType.BigInt).Value = evInterval;
				textCommand.Parameters.Add("@startTime", SqlDbType.DateTime).Value = startTime;
				textCommand.Parameters.Add("@endTime", SqlDbType.DateTime).Value = endTime;
				textCommand.Parameters.Add("@ignoreTimeout", SqlDbType.Bit).Value = ignoreTimeout;
				result = SqlHelper.ExecuteNonQuery(textCommand);
			}
			return result;
		}

		// Token: 0x06000754 RID: 1876 RVA: 0x000322A0 File Offset: 0x000304A0
		[Obsolete("Old alerting will be removed")]
		public static DataTable GetPagebleAdvancedAlerts(string column, string direction, int number, int size)
		{
			size = Math.Max(size, 25);
			DataTable dataTable;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Format("\r\nSelect *\r\nfrom (SELECT *, ROW_NUMBER() OVER (ORDER BY {0} {1}) RowNr from [AlertDefinitions]) t\r\nWHERE RowNr BETWEEN {2} AND {3}\r\nORDER BY {0} {1}", new object[]
			{
				string.IsNullOrEmpty(column) ? "AlertName" : column,
				string.IsNullOrEmpty(direction) ? "ASC" : direction,
				number + 1,
				number + size
			})))
			{
				dataTable = SqlHelper.ExecuteDataTable(textCommand);
			}
			if (dataTable != null)
			{
				dataTable.TableName = "AlertsDefinition";
			}
			return dataTable;
		}

		// Token: 0x06000755 RID: 1877 RVA: 0x0003233C File Offset: 0x0003053C
		public static int UpdateAdvancedAlertNote(string alerfDefID, string activeObject, string objectType, string notes)
		{
			int result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("UPDATE AlertStatus SET Notes=@Notes Where AlertDefID=@AlertDefID AND ActiveObject=@ActiveObject AND ObjectType=@ObjectType"))
			{
				textCommand.Parameters.Add("@Notes", SqlDbType.NVarChar).Value = notes;
				textCommand.Parameters.AddWithValue("@AlertDefID", alerfDefID);
				textCommand.Parameters.Add("@ActiveObject", SqlDbType.VarChar).Value = activeObject;
				textCommand.Parameters.Add("@ObjectType", SqlDbType.NVarChar).Value = objectType;
				result = SqlHelper.ExecuteNonQuery(textCommand);
			}
			return result;
		}

		// Token: 0x06000756 RID: 1878 RVA: 0x000323D4 File Offset: 0x000305D4
		public static int AppendNoteToAlert(string alertId, string activeObject, string objectType, string note)
		{
			int result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("UPDATE AlertStatus SET Notes =(\r\nCASE\r\nWHEN (Notes IS NULL)\r\nTHEN\r\n @Notes\r\nELSE\r\n CAST(Notes AS NVARCHAR(MAX)) + CHAR(13) + CHAR(10) + '---' + CHAR(13) + CHAR(10) + @Notes\r\nEND\r\n) Where AlertDefID=@AlertDefID AND ActiveObject=@ActiveObject AND ObjectType=@ObjectType"))
			{
				textCommand.Parameters.AddWithValue("@AlertDefID", alertId);
				textCommand.Parameters.Add("@ActiveObject", SqlDbType.VarChar).Value = activeObject;
				textCommand.Parameters.Add("@ObjectType", SqlDbType.NVarChar).Value = objectType;
				textCommand.Parameters.AddWithValue("@Notes", note);
				result = SqlHelper.ExecuteNonQuery(textCommand);
			}
			return result;
		}

		// Token: 0x06000757 RID: 1879 RVA: 0x00032464 File Offset: 0x00030664
		[Obsolete("Old alerting will be removed")]
		public static AlertNotificationSettings GetAlertNotificationSettings(string alertDefID, string netObjectType, string alertName)
		{
			AlertNotificationSettings result;
			try
			{
				IAlertNotificationSettingsProvider alertNotificationSettingsProvider = new AlertNotificationSettingsProvider();
				AlertNotificationSettings alertNotificationSettings = null;
				using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT AlertName\r\n                            ,ObjectType\r\n                            ,NotifyEnabled\r\n                            ,NotificationSettings\r\n                    FROM [AlertDefinitions]\r\n                    WHERE AlertDefID = @AlertDefinitionId"))
				{
					textCommand.Parameters.AddWithValue("@AlertDefinitionId", string.IsNullOrWhiteSpace(alertDefID) ? Guid.Empty : new Guid(alertDefID));
					DataTable dataTable = SqlHelper.ExecuteDataTable(textCommand);
					if (dataTable.Rows.Count != 0)
					{
						string alertName2 = (string)dataTable.Rows[0]["AlertName"];
						string text = (string)dataTable.Rows[0]["ObjectType"];
						bool enabled = (bool)dataTable.Rows[0]["NotifyEnabled"];
						string settingsXml = (dataTable.Rows[0]["NotificationSettings"] is DBNull) ? null : ((string)dataTable.Rows[0]["NotificationSettings"]);
						if (text.Equals(netObjectType, StringComparison.OrdinalIgnoreCase))
						{
							alertNotificationSettings = alertNotificationSettingsProvider.GetAlertNotificationSettings(text, alertName2, settingsXml);
							alertNotificationSettings.Enabled = enabled;
						}
					}
					if (alertNotificationSettings == null)
					{
						alertNotificationSettings = alertNotificationSettingsProvider.GetDefaultAlertNotificationSettings(netObjectType, alertName);
						alertNotificationSettings.Enabled = true;
					}
				}
				result = alertNotificationSettings;
			}
			catch (Exception ex)
			{
				AlertDAL.Log.Error(string.Format("Error getting alert notification settings for alert {0}", alertDefID), ex);
				throw;
			}
			return result;
		}

		// Token: 0x06000758 RID: 1880 RVA: 0x000325F0 File Offset: 0x000307F0
		[Obsolete("Old alerting will be removed")]
		public static void SetAlertNotificationSettings(string alertDefID, AlertNotificationSettings settings)
		{
			try
			{
				if (alertDefID == null)
				{
					throw new ArgumentNullException("alertDefID");
				}
				string value = ((IAlertNotificationSettingsConverter)new AlertNotificationSettingsConverter()).ToXml(settings);
				using (SqlCommand textCommand = SqlHelper.GetTextCommand("UPDATE [AlertDefinitions] SET\r\n                            NotifyEnabled = @NotifyEnabled,\r\n                            NotificationSettings = @NotificationSettings\r\n                      WHERE AlertDefID = @AlertDefinitionId"))
				{
					textCommand.Parameters.AddWithValue("@AlertDefinitionId", alertDefID);
					textCommand.Parameters.AddWithValue("@NotifyEnabled", settings.Enabled);
					textCommand.Parameters.AddWithValue("@NotificationSettings", value);
					SqlHelper.ExecuteNonQuery(textCommand);
				}
			}
			catch (Exception ex)
			{
				AlertDAL.Log.Error(string.Format("Error setting alert notification settings for alert {0}", alertDefID), ex);
				throw;
			}
		}

		// Token: 0x06000759 RID: 1881 RVA: 0x000326AC File Offset: 0x000308AC
		[Obsolete("Old alerting will be removed")]
		public static AlertNotificationDetails GetAlertDetailsForNotification(string alertDefID, string activeObject, string objectType)
		{
			AlertNotificationDetails result;
			try
			{
				AlertNotificationDetails alertNotificationDetails = new AlertNotificationDetails();
				using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT s.ActiveObject\r\n                          ,s.ObjectType\r\n                          ,s.ObjectName\r\n                          ,s.TriggerTimeStamp\r\n                          ,s.ResetTimeStamp\r\n                          ,s.Acknowledged\r\n                          ,s.AcknowledgedBy\r\n                          ,s.AcknowledgedTime\r\n                          ,s.AlertNotes\r\n                          ,s.Notes\r\n                          ,s.AlertMessage\r\n                          ,s.TriggerCount\r\n                         ,ad.AlertName\r\n                         ,ad.NotifyEnabled\r\n                         ,ad.NotificationSettings\r\n                    FROM AlertStatus s JOIN AlertDefinitions ad\r\n                      ON s.AlertDefID = ad.AlertDefID\r\n                    WHERE ad.NotifyEnabled = 1\r\n                      AND s.AlertDefID = @AlertDefinitionId\r\n                      AND s.ActiveObject=@ActiveObject \r\n                      AND s.ObjectType LIKE @ObjectType"))
				{
					textCommand.Parameters.AddWithValue("@AlertDefinitionId", alertDefID);
					textCommand.Parameters.AddWithValue("@ActiveObject", activeObject);
					textCommand.Parameters.AddWithValue("@ObjectType", objectType);
					DataTable dataTable = SqlHelper.ExecuteDataTable(textCommand);
					if (dataTable.Rows.Count == 0)
					{
						return alertNotificationDetails;
					}
					string acknowledgedBy;
					string acknowledgedMethod;
					AlertsHelper.GetOriginalUsernameFromAcknowledgeUsername((string)dataTable.Rows[0]["AcknowledgedBy"], out acknowledgedBy, out acknowledgedMethod);
					alertNotificationDetails.Acknowledged = ((byte)dataTable.Rows[0]["Acknowledged"] > 0);
					alertNotificationDetails.AcknowledgedBy = acknowledgedBy;
					alertNotificationDetails.AcknowledgedMethod = acknowledgedMethod;
					alertNotificationDetails.AcknowledgedTime = ((DateTime)dataTable.Rows[0]["AcknowledgedTime"]).ToUniversalTime();
					alertNotificationDetails.AlertDefinitionId = alertDefID;
					alertNotificationDetails.ActiveObject = (string)dataTable.Rows[0]["ActiveObject"];
					alertNotificationDetails.ObjectType = (string)dataTable.Rows[0]["ObjectType"];
					alertNotificationDetails.AlertName = (string)dataTable.Rows[0]["AlertName"];
					alertNotificationDetails.ObjectName = (string)dataTable.Rows[0]["ObjectName"];
					alertNotificationDetails.AlertNotes = ((dataTable.Rows[0]["AlertNotes"] is DBNull) ? string.Empty : ((string)dataTable.Rows[0]["AlertNotes"]));
					alertNotificationDetails.Notes = ((dataTable.Rows[0]["Notes"] is DBNull) ? string.Empty : ((string)dataTable.Rows[0]["Notes"]));
					alertNotificationDetails.TriggerCount = (int)dataTable.Rows[0]["TriggerCount"];
					alertNotificationDetails.AlertMessage = ((dataTable.Rows[0]["AlertMessage"] is DBNull) ? string.Empty : ((string)dataTable.Rows[0]["AlertMessage"]));
					alertNotificationDetails.TriggerTimeStamp = ((DateTime)dataTable.Rows[0]["TriggerTimeStamp"]).ToUniversalTime();
					alertNotificationDetails.ResetTimeStamp = ((DateTime)dataTable.Rows[0]["ResetTimeStamp"]).ToUniversalTime();
					IAlertNotificationSettingsProvider alertNotificationSettingsProvider = new AlertNotificationSettingsProvider();
					alertNotificationDetails.NotificationSettings = alertNotificationSettingsProvider.GetAlertNotificationSettings(alertNotificationDetails.ObjectType, alertNotificationDetails.AlertName, (dataTable.Rows[0]["NotificationSettings"] is DBNull) ? null : ((string)dataTable.Rows[0]["NotificationSettings"]));
					alertNotificationDetails.NotificationSettings.Enabled = (bool)dataTable.Rows[0]["NotifyEnabled"];
				}
				result = alertNotificationDetails;
			}
			catch (Exception ex)
			{
				AlertDAL.Log.Error(string.Format("Error getting alert details for notification for alert {0}", alertDefID), ex);
				throw;
			}
			return result;
		}

		// Token: 0x0600075A RID: 1882 RVA: 0x00032A50 File Offset: 0x00030C50
		[Obsolete("Old alerting will be removed")]
		public static AlertNotificationSettings GetBasicAlertNotificationSettings(int alertID, string netObjectType, int propertyID, string alertName)
		{
			AlertNotificationSettings result;
			try
			{
				IAlertNotificationSettingsProvider alertNotificationSettingsProvider = new AlertNotificationSettingsProvider();
				AlertNotificationSettings alertNotificationSettings = null;
				if (netObjectType.Equals("NetworkNode", StringComparison.OrdinalIgnoreCase))
				{
					netObjectType = "Node";
				}
				using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT AlertName\r\n                            ,PropertyID\r\n                            ,NotifyEnabled\r\n                            ,NotificationSettings\r\n                    FROM [Alerts]\r\n                    WHERE AlertID = @AlertDefinitionId"))
				{
					textCommand.Parameters.AddWithValue("@AlertDefinitionId", alertID);
					DataTable dataTable = SqlHelper.ExecuteDataTable(textCommand);
					if (dataTable.Rows.Count != 0)
					{
						string text = (string)dataTable.Rows[0]["AlertName"];
						int num = (int)dataTable.Rows[0]["PropertyID"];
						bool enabled = (bool)dataTable.Rows[0]["NotifyEnabled"];
						string settingsXml = (dataTable.Rows[0]["NotificationSettings"] is DBNull) ? null : ((string)dataTable.Rows[0]["NotificationSettings"]);
						if (num == propertyID)
						{
							alertNotificationSettings = alertNotificationSettingsProvider.GetAlertNotificationSettings(netObjectType, text.Trim(), settingsXml);
							alertNotificationSettings.Enabled = enabled;
						}
					}
					if (alertNotificationSettings == null)
					{
						alertNotificationSettings = alertNotificationSettingsProvider.GetDefaultAlertNotificationSettings(netObjectType, alertName);
						alertNotificationSettings.Enabled = true;
					}
				}
				result = alertNotificationSettings;
			}
			catch (Exception ex)
			{
				AlertDAL.Log.Error(string.Format("Error getting basic alert notification settings for alert {0}", alertID), ex);
				throw;
			}
			return result;
		}

		// Token: 0x0600075B RID: 1883 RVA: 0x00032BDC File Offset: 0x00030DDC
		public static void SetBasicAlertNotificationSettings(int alertID, AlertNotificationSettings settings)
		{
			try
			{
				string value = ((IAlertNotificationSettingsConverter)new AlertNotificationSettingsConverter()).ToXml(settings);
				using (SqlCommand textCommand = SqlHelper.GetTextCommand("UPDATE [Alerts] SET\r\n                            NotifyEnabled = @NotifyEnabled,\r\n                            NotificationSettings = @NotificationSettings\r\n                      WHERE AlertID = @AlertDefinitionId"))
				{
					textCommand.Parameters.AddWithValue("@AlertDefinitionId", alertID);
					textCommand.Parameters.AddWithValue("@NotifyEnabled", settings.Enabled);
					textCommand.Parameters.AddWithValue("@NotificationSettings", value);
					SqlHelper.ExecuteNonQuery(textCommand);
				}
			}
			catch (Exception ex)
			{
				AlertDAL.Log.Error(string.Format("Error setting basic alert notification settings for alert {0}", alertID), ex);
				throw;
			}
		}

		// Token: 0x0600075C RID: 1884 RVA: 0x00032C94 File Offset: 0x00030E94
		[Obsolete("Old alerting will be removed")]
		public static bool RevertMigratedAlert(Guid alertRefId, bool enableInOldAlerting)
		{
			string text = "Update AlertDefinitions Set Reverted=@Reverted, Enabled=@Enabled WHERE AlertDefID=@AlertDefID";
			string text2 = "Update Alerts Set Reverted=@Reverted, Enabled=@Enabled WHERE AlertDefID=@AlertDefID";
			int num;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(text))
			{
				textCommand.Parameters.AddRange(new SqlParameter[]
				{
					new SqlParameter("Reverted", true),
					new SqlParameter("Enabled", enableInOldAlerting),
					new SqlParameter("AlertDefID", alertRefId)
				});
				num = SqlHelper.ExecuteNonQuery(textCommand);
			}
			if (num < 1)
			{
				using (SqlCommand textCommand2 = SqlHelper.GetTextCommand(text2))
				{
					textCommand2.Parameters.AddRange(new SqlParameter[]
					{
						new SqlParameter("Reverted", true),
						new SqlParameter("Enabled", enableInOldAlerting),
						new SqlParameter("AlertDefID", alertRefId)
					});
					num = SqlHelper.ExecuteNonQuery(textCommand2);
				}
			}
			return num > 0;
		}

		// Token: 0x0600075D RID: 1885 RVA: 0x00032D98 File Offset: 0x00030F98
		public static int GetAlertObjectId(string alertKey)
		{
			if (string.IsNullOrWhiteSpace(alertKey))
			{
				throw new ArgumentException("Parameter is null or empty", "alertKey");
			}
			string[] array = alertKey.Split(new char[]
			{
				':'
			});
			if (array.Length != 3)
			{
				string text = string.Format("Error getting alert key parts. Original key: '{0}'", alertKey);
				AlertDAL.Log.Error(text);
				throw new ArgumentException(text);
			}
			Guid guid;
			if (!Guid.TryParse(array[0], out guid))
			{
				string text2 = string.Format("Error getting AlertDefId as GUID. Original key: '{0}'", array[0]);
				AlertDAL.Log.Error(text2);
				throw new ArgumentException(text2);
			}
			int result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT TOP 1 AlertObjectID FROM AlertStatusView WHERE AlertDefID=@alertDefID AND ActiveObject=@activeObject"))
			{
				textCommand.Parameters.Add(new SqlParameter("alertDefID", SqlDbType.UniqueIdentifier)
				{
					Value = guid
				});
				textCommand.Parameters.Add(new SqlParameter("activeObject", SqlDbType.NVarChar)
				{
					Value = array[1]
				});
				object obj = SqlHelper.ExecuteScalar(textCommand);
				int num;
				if (obj != DBNull.Value && obj != null && int.TryParse(obj.ToString(), out num))
				{
					result = num;
				}
				else
				{
					AlertDAL.Log.InfoFormat("AlertObjectID for alertKey: '{0}' was not found.", alertKey);
					result = 0;
				}
			}
			return result;
		}

		// Token: 0x04000234 RID: 564
		private const int CLRWhereClausesLimit = 100;

		// Token: 0x04000235 RID: 565
		private static readonly Regex _ackRegex = new Regex("AlertStatus.Acknowledged=[0-1]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Token: 0x04000236 RID: 566
		internal static Func<bool> IsInterfacesAllowed = () => PackageManager.InstanceWithCache.IsPackageInstalled("Orion.Interfaces");

		// Token: 0x04000237 RID: 567
		private static readonly Log Log = new Log();

		// Token: 0x04000238 RID: 568
		private static bool _enableLimitationReplacement = BusinessLayerSettings.Instance.EnableLimitationReplacement;

		// Token: 0x04000239 RID: 569
		private static int _limitationSqlExaggeration = BusinessLayerSettings.Instance.LimitationSqlExaggeration;

		// Token: 0x0400023A RID: 570
		internal static IInformationServiceProxyCreator SwisCreator = new SwisConnectionProxyCreator(() => new SwisConnectionProxyFactory().CreateConnection());

		// Token: 0x0400023B RID: 571
		internal static IInformationServiceProxy2 SwisProxy = AlertDAL.SwisCreator.Create();
	}
}
