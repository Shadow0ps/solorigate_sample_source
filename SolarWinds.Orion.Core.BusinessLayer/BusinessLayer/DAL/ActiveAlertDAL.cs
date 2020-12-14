using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using SolarWinds.InformationService.Contract2;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Alerting.DAL;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Common.Federation;
using SolarWinds.Orion.Core.Common.Indications;
using SolarWinds.Orion.Core.Common.InformationService;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Common.Models.Alerts;
using SolarWinds.Orion.Core.Common.Swis;
using SolarWinds.Orion.Core.Models.Alerting;
using SolarWinds.Orion.Core.Strings;
using SolarWinds.Shared;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000086 RID: 134
	public class ActiveAlertDAL
	{
		// Token: 0x06000685 RID: 1669 RVA: 0x00026DF0 File Offset: 0x00024FF0
		public ActiveAlertDAL() : this(SwisConnectionProxyPool.GetCreator())
		{
		}

		// Token: 0x06000686 RID: 1670 RVA: 0x00026DFD File Offset: 0x00024FFD
		public ActiveAlertDAL(IInformationServiceProxyCreator swisProxyCreator) : this(swisProxyCreator, new AlertHistoryDAL(swisProxyCreator))
		{
		}

		// Token: 0x06000687 RID: 1671 RVA: 0x00026E0C File Offset: 0x0002500C
		public ActiveAlertDAL(IInformationServiceProxyCreator swisProxyCreator, IAlertHistoryDAL alertHistoryDAL)
		{
			this._swisProxyCreator = swisProxyCreator;
			this._alertHistoryDAL = alertHistoryDAL;
			this._alertPropertiesProvider = new Lazy<AlertObjectPropertiesProvider>(() => new AlertObjectPropertiesProvider(swisProxyCreator));
			StatusInfo.Init(new DefaultStatusInfoProvider(), ActiveAlertDAL.log);
		}

		// Token: 0x06000688 RID: 1672 RVA: 0x00026E68 File Offset: 0x00025068
		public int AcknowledgeActiveAlerts(IEnumerable<int> alertObjectIds, string accountId, string notes, DateTime acknowledgeDateTime)
		{
			if (!alertObjectIds.Any<int>())
			{
				return 0;
			}
			int result = 0;
			bool flag = !string.IsNullOrEmpty(notes);
			string format = "UPDATE AlertObjects SET AlertNote = CASE WHEN (AlertNote IS NULL) THEN @alertNote ELSE AlertNote + CHAR(13) + CHAR(10) + @alertNote END WHERE AlertObjectId IN ({0})";
			string text = "UPDATE AlertActive SET Acknowledged=1, AcknowledgedBy=@acknowledgedBy, AcknowledgedDateTime=@acknowledgedDateTime";
			text += " WHERE AlertObjectID IN ({0})";
			string text2 = string.Empty;
			int num = 0;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(text))
			{
				foreach (int num2 in alertObjectIds)
				{
					string text3 = string.Format("@alertObjectID{0}", num++);
					if (!string.IsNullOrEmpty(text2))
					{
						text2 += ",";
					}
					if (num < 1000)
					{
						textCommand.Parameters.AddWithValue(text3, num2);
						text2 += text3;
					}
					else
					{
						text2 += num2;
					}
				}
				textCommand.Parameters.AddWithValue("@acknowledgedBy", accountId);
				textCommand.Parameters.AddWithValue("@acknowledgedDateTime", acknowledgeDateTime.ToUniversalTime());
				using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
				{
					using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction(IsolationLevel.ReadCommitted))
					{
						textCommand.CommandText = string.Format(text, text2);
						SqlHelper.ExecuteNonQuery(textCommand, sqlConnection, sqlTransaction);
						if (flag)
						{
							textCommand.Parameters.AddWithValue("@alertNote", notes);
							textCommand.CommandText = string.Format(format, text2);
							SqlHelper.ExecuteNonQuery(textCommand, sqlConnection, sqlTransaction);
						}
						textCommand.CommandText = string.Format("SELECT AlertObjectID, AlertActiveID FROM AlertActive WHERE AlertObjectID IN ({0})", text2);
						DataTable dataTable = SqlHelper.ExecuteDataTable(textCommand, sqlConnection, null);
						result = dataTable.Rows.Count;
						foreach (object obj in dataTable.Rows)
						{
							DataRow dataRow = (DataRow)obj;
							AlertHistory alertHistory = new AlertHistory();
							alertHistory.EventType = EventType.Acknowledged;
							alertHistory.AccountID = accountId;
							alertHistory.Message = notes;
							alertHistory.TimeStamp = acknowledgeDateTime.ToUniversalTime();
							int alertObjectID = (dataRow["AlertObjectID"] != DBNull.Value) ? Convert.ToInt32(dataRow["AlertObjectID"]) : 0;
							long alertActiveID = (dataRow["AlertActiveID"] != DBNull.Value) ? Convert.ToInt64(dataRow["AlertActiveID"]) : 0L;
							this._alertHistoryDAL.InsertHistoryItem(alertHistory, alertActiveID, alertObjectID, sqlConnection, sqlTransaction);
						}
						sqlTransaction.Commit();
					}
				}
			}
			return result;
		}

		// Token: 0x06000689 RID: 1673 RVA: 0x00027188 File Offset: 0x00025388
		public static bool UnacknowledgeAlerts(int[] alertObjectIds, string accountId)
		{
			bool result = true;
			for (int i = 0; i < alertObjectIds.Length; i++)
			{
				if (!ActiveAlertDAL.UnacknowledgeAlert(alertObjectIds[i], accountId))
				{
					result = false;
				}
			}
			return result;
		}

		// Token: 0x0600068A RID: 1674 RVA: 0x000271B8 File Offset: 0x000253B8
		private static bool UnacknowledgeAlert(int alertObjectId, string accountId)
		{
			string text = "UPDATE AlertActive SET Acknowledged= null, \r\n                                     AcknowledgedBy=null, \r\n                                     AcknowledgedDateTime = null\r\n                                     WHERE [AlertObjectID] = @alertObjectId";
			AlertHistoryDAL alertHistoryDAL = new AlertHistoryDAL();
			int num = -1;
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				using (SqlCommand textCommand = SqlHelper.GetTextCommand(text))
				{
					using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction(IsolationLevel.ReadCommitted))
					{
						textCommand.Parameters.AddWithValue("@alertObjectId", alertObjectId);
						num = SqlHelper.ExecuteNonQuery(textCommand, sqlConnection, sqlTransaction);
						textCommand.CommandText = "SELECT AlertObjectID, AlertActiveID FROM AlertActive WHERE [AlertObjectID] = @alertObjectId";
						foreach (object obj in SqlHelper.ExecuteDataTable(textCommand, sqlConnection, null).Rows)
						{
							DataRow dataRow = (DataRow)obj;
							int alertObjectID = (dataRow["AlertObjectID"] != DBNull.Value) ? Convert.ToInt32(dataRow["AlertObjectID"]) : 0;
							long alertActiveID = (dataRow["AlertActiveID"] != DBNull.Value) ? Convert.ToInt64(dataRow["AlertActiveID"]) : 0L;
							alertHistoryDAL.InsertHistoryItem(new AlertHistory
							{
								EventType = EventType.Unacknowledge,
								AccountID = accountId,
								TimeStamp = DateTime.UtcNow
							}, alertActiveID, alertObjectID, sqlConnection, sqlTransaction);
						}
						sqlTransaction.Commit();
					}
				}
			}
			return num == 1;
		}

		// Token: 0x0600068B RID: 1675 RVA: 0x00027384 File Offset: 0x00025584
		private string GetActiveAlertQuery(IEnumerable<CustomProperty> customProperties, bool includeNotTriggered = false)
		{
			string text = string.Empty;
			if (customProperties != null)
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (CustomProperty customProperty in customProperties)
				{
					stringBuilder.AppendFormat(", AlertConfigurations.CustomProperties.[{0}]", customProperty.PropertyName);
				}
				text = stringBuilder.ToString();
			}
			string text2 = " SELECT DISTINCT OrionSite.SiteID, OrionSite.Name AS SiteName,\r\n                                 Data.AlertActiveID, Data.AlertObjectID, Data.Name,\r\n                                Data.AlertMessage, Data.Severity, Data.ObjectType,\r\n                                Data.EntityUri, Data.EntityType, Data.EntityCaption, Data.EntityDetailsUrl,\r\n                                Data.RelatedNodeUri, Data.RelatedNodeDetailsUrl, Data.RelatedNodeCaption, Data.AlertID, \r\n                                Data.TriggeredDateTime, Data.LastTriggeredDateTime, Data.Message, Data.AccountID, \r\n                                Data.LastExecutedEscalationLevel, Data.AcknowledgedDateTime, Data.Acknowledged, Data.AcknowledgedBy, Data.NumberOfNotes, \r\n                                Data.TriggeredCount, Data.AcknowledgedNote, Data.Canned, Data.Category {1},\r\n                                '' AS IncidentNumber, '' AS IncidentUrl, '' AS AssignedTo\r\n                                FROM (\r\n\r\n                                SELECT AlertActive.InstanceSiteID,AlertActive.AlertActiveID, AlertObjects.AlertObjectID, AlertConfigurations.Name,\r\n                                AlertConfigurations.AlertMessage, AlertConfigurations.Severity, AlertConfigurations.ObjectType,\r\n                                AlertObjects.EntityUri, AlertObjects.EntityType, AlertObjects.EntityCaption, AlertObjects.EntityDetailsUrl,\r\n                                AlertObjects.RelatedNodeUri, AlertObjects.RelatedNodeDetailsUrl, AlertObjects.RelatedNodeCaption, AlertObjects.AlertID, \r\n                                AlertActive.TriggeredDateTime, AlertObjects.LastTriggeredDateTime, AlertActive.TriggeredMessage AS Message, AlertActive.AcknowledgedBy AS AccountID, \r\n                                AlertActive.LastExecutedEscalationLevel, AlertActive.AcknowledgedDateTime, AlertActive.Acknowledged, AlertActive.AcknowledgedBy, AlertActive.NumberOfNotes, \r\n                                AlertObjects.TriggeredCount, AlertObjects.AlertNote as AcknowledgedNote, AlertConfigurations.Canned, AlertConfigurations.Category {0}\r\n                                FROM Orion.AlertObjects AlertObjects";
			if (includeNotTriggered)
			{
				text2 += " LEFT JOIN Orion.AlertActive (nolock=true) AlertActive ON AlertObjects.AlertObjectID=AlertActive.AlertObjectID AND AlertObjects.InstanceSiteID=AlertActive.InstanceSiteID";
			}
			else
			{
				text2 += " INNER JOIN Orion.AlertActive (nolock=true) AlertActive ON AlertObjects.AlertObjectID=AlertActive.AlertObjectID AND AlertObjects.InstanceSiteID=AlertActive.InstanceSiteID";
			}
			text2 += " INNER JOIN Orion.AlertConfigurations (nolock=true) AlertConfigurations ON AlertConfigurations.AlertID=AlertObjects.AlertID AND AlertConfigurations.InstanceSiteID=AlertObjects.InstanceSiteID";
			text2 += ") AS Data";
			text2 += " LEFT JOIN Orion.Sites AS OrionSite ON OrionSite.SiteID=Data.InstanceSiteID";
			return string.Format(text2, text, text.Replace("AlertConfigurations.CustomProperties", "Data"));
		}

		// Token: 0x0600068C RID: 1676 RVA: 0x00027450 File Offset: 0x00025650
		private string GetActiveAlertTableByDateQuery()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("SELECT AlertHistory.AlertHistoryID, AlertHistory.TimeStamp, AlertObjects.AlertID, AlertObjects.EntityCaption, AlertActive.AlertObjectID, AlertActive.AlertActiveID, AlertActive.Acknowledged, AlertActive.AcknowledgedBy, AlertActive.AcknowledgedDateTime, AlertConfigurations.ObjectType,");
			stringBuilder.Append(" AlertConfigurations.Name, AlertConfigurations.AlertMessage, AlertConfigurations.AlertRefID, AlertConfigurations.Description, AlertObjects.EntityType, AlertObjects.EntityDetailsUrl, AlertActive.TriggeredDateTime, AlertObjects.EntityUri, AlertActiveObjects.EntityUri as ActiveObjectEntityUri, AlertObjects.RelatedNodeUri,");
			stringBuilder.Append(" Actions.ActionTypeID, AlertConfigurations.LastEdit, AlertConfigurations.Severity, ActionsProperties.PropertyName, ActionsProperties.PropertyValue, AlertActive.AcknowledgedNote, AlertConfigurations.Canned, AlertConfigurations.Category ");
			stringBuilder.Append(" FROM Orion.AlertObjects AlertObjects");
			stringBuilder.Append(" LEFT JOIN Orion.AlertActive (nolock=true) AlertActive ON AlertObjects.AlertObjectID=AlertActive.AlertObjectID");
			stringBuilder.Append(" INNER JOIN Orion.AlertHistory (nolock=true) AlertHistory ON AlertObjects.AlertObjectID=AlertHistory.AlertObjectID");
			stringBuilder.Append(" INNER JOIN Orion.Actions (nolock=true) Actions ON AlertHistory.ActionID = Actions.ActionID");
			stringBuilder.Append(" INNER JOIN Orion.ActionsProperties (nolock=true) ActionsProperties ON Actions.ActionID = ActionsProperties.ActionID");
			stringBuilder.Append(" INNER JOIN Orion.AlertConfigurations (nolock=true) AlertConfigurations ON AlertConfigurations.AlertID=AlertObjects.AlertID");
			stringBuilder.Append(" LEFT JOIN Orion.AlertActiveObjects (nolock=true) AlertActiveObjects ON AlertActiveObjects.AlertActiveID=AlertActive.AlertActiveID");
			stringBuilder.Append(" WHERE Actions.ActionTypeID IN ('PlaySound', 'TextToSpeech') AND ActionsProperties.PropertyName IN ('Message', 'Text') AND (AlertActive.Acknowledged IS NULL OR AlertActive.Acknowledged = false)");
			return stringBuilder.ToString();
		}

		// Token: 0x0600068D RID: 1677 RVA: 0x000274EC File Offset: 0x000256EC
		private ActiveAlert GetActiveAlertFromDataRow(DataRow rActiveAlert, IEnumerable<CustomProperty> customProperties)
		{
			ActiveAlert activeAlert = new ActiveAlert();
			activeAlert.CustomProperties = new Dictionary<string, object>();
			activeAlert.TriggeringObjectEntityUri = ((rActiveAlert["EntityUri"] != DBNull.Value) ? Convert.ToString(rActiveAlert["EntityUri"]) : string.Empty);
			activeAlert.SiteID = ((rActiveAlert["SiteID"] != DBNull.Value) ? Convert.ToInt32(rActiveAlert["SiteID"]) : -1);
			activeAlert.SiteName = ((rActiveAlert["SiteName"] != DBNull.Value) ? Convert.ToString(rActiveAlert["SiteName"]) : string.Empty);
			string linkPrefix = FederationUrlHelper.GetLinkPrefix(activeAlert.SiteID);
			activeAlert.TriggerDateTime = ((rActiveAlert["TriggeredDateTime"] != DBNull.Value) ? DateTime.SpecifyKind(Convert.ToDateTime(rActiveAlert["TriggeredDateTime"]), DateTimeKind.Utc).ToLocalTime() : DateTime.MinValue);
			activeAlert.ActiveTime = DateTime.Now - activeAlert.TriggerDateTime;
			activeAlert.LastTriggeredDateTime = ((rActiveAlert["LastTriggeredDateTime"] != DBNull.Value) ? Convert.ToDateTime(rActiveAlert["LastTriggeredDateTime"]).ToLocalTime() : DateTime.MinValue);
			activeAlert.ActiveTimeDisplay = this.ActiveTimeToDisplayFormat(activeAlert.ActiveTime);
			activeAlert.TriggeringObjectCaption = ((rActiveAlert["EntityCaption"] != DBNull.Value) ? Convert.ToString(rActiveAlert["EntityCaption"]) : string.Empty);
			activeAlert.TriggeringObjectDetailsUrl = linkPrefix + ((rActiveAlert["EntityDetailsUrl"] != DBNull.Value) ? Convert.ToString(rActiveAlert["EntityDetailsUrl"]) : string.Empty);
			activeAlert.TriggeringObjectEntityName = ((rActiveAlert["EntityType"] != DBNull.Value) ? Convert.ToString(rActiveAlert["EntityType"]) : string.Empty);
			activeAlert.RelatedNodeCaption = ((rActiveAlert["RelatedNodeCaption"] != DBNull.Value) ? Convert.ToString(rActiveAlert["RelatedNodeCaption"]) : string.Empty);
			activeAlert.RelatedNodeDetailsUrl = linkPrefix + ((rActiveAlert["RelatedNodeDetailsUrl"] != DBNull.Value) ? Convert.ToString(rActiveAlert["RelatedNodeDetailsUrl"]) : string.Empty);
			activeAlert.RelatedNodeEntityUri = ((rActiveAlert["RelatedNodeUri"] != DBNull.Value) ? Convert.ToString(rActiveAlert["RelatedNodeUri"]) : string.Empty);
			bool flag = rActiveAlert["Acknowledged"] != DBNull.Value && Convert.ToBoolean(rActiveAlert["Acknowledged"]);
			activeAlert.Canned = (rActiveAlert["Canned"] != DBNull.Value && Convert.ToBoolean(rActiveAlert["Canned"]));
			activeAlert.Category = ((rActiveAlert["Category"] != DBNull.Value) ? Convert.ToString(rActiveAlert["Category"]) : string.Empty);
			if (flag)
			{
				activeAlert.AcknowledgedBy = ((rActiveAlert["AcknowledgedBy"] != DBNull.Value) ? Convert.ToString(rActiveAlert["AcknowledgedBy"]) : string.Empty);
				activeAlert.AcknowledgedByFullName = activeAlert.AcknowledgedBy;
				activeAlert.AcknowledgedDateTime = ((rActiveAlert["AcknowledgedDateTime"] != DBNull.Value) ? DateTime.SpecifyKind(Convert.ToDateTime(rActiveAlert["AcknowledgedDateTime"]), DateTimeKind.Utc).ToLocalTime() : DateTime.MinValue);
				activeAlert.Notes = ((rActiveAlert["AcknowledgedNote"] != DBNull.Value) ? Convert.ToString(rActiveAlert["AcknowledgedNote"]) : string.Empty);
			}
			activeAlert.NumberOfNotes = ((rActiveAlert["NumberOfNotes"] != DBNull.Value) ? Convert.ToInt32(rActiveAlert["NumberOfNotes"]) : 0);
			activeAlert.Id = ((rActiveAlert["AlertObjectID"] != DBNull.Value) ? Convert.ToInt32(rActiveAlert["AlertObjectID"]) : 0);
			activeAlert.AlertDefId = ((rActiveAlert["AlertID"] != DBNull.Value) ? Convert.ToString(rActiveAlert["AlertID"]) : string.Empty);
			activeAlert.LegacyAlert = false;
			activeAlert.Message = ((rActiveAlert["Message"] != DBNull.Value) ? Convert.ToString(rActiveAlert["Message"]) : string.Empty);
			activeAlert.Name = ((rActiveAlert["Name"] != DBNull.Value) ? Convert.ToString(rActiveAlert["Name"]) : string.Empty);
			activeAlert.ObjectType = ((rActiveAlert["ObjectType"] != DBNull.Value) ? Convert.ToString(rActiveAlert["ObjectType"]) : string.Empty);
			activeAlert.Severity = (ActiveAlertSeverity)((rActiveAlert["Severity"] != DBNull.Value) ? Convert.ToInt32(rActiveAlert["Severity"]) : 1);
			activeAlert.TriggeringObjectEntityName = ((rActiveAlert["EntityType"] != DBNull.Value) ? Convert.ToString(rActiveAlert["EntityType"]) : string.Empty);
			activeAlert.TriggeringObjectCaption = ((rActiveAlert["EntityCaption"] != DBNull.Value) ? Convert.ToString(rActiveAlert["EntityCaption"]) : string.Empty);
			activeAlert.Status = ((rActiveAlert["AlertActiveID"] != DBNull.Value) ? ActiveAlertStatus.Triggered : ActiveAlertStatus.NotTriggered);
			if (activeAlert.Status == ActiveAlertStatus.NotTriggered)
			{
				activeAlert.ActiveTimeDisplay = Resources.LIBCODE_PS0_11;
			}
			activeAlert.RelatedNodeEntityUri = ((rActiveAlert["RelatedNodeUri"] != DBNull.Value) ? Convert.ToString(rActiveAlert["RelatedNodeUri"]) : string.Empty);
			activeAlert.TriggerCount = ((rActiveAlert["TriggeredCount"] != DBNull.Value) ? Convert.ToInt32(rActiveAlert["TriggeredCount"]) : 0);
			activeAlert.EscalationLevel = ((rActiveAlert["LastExecutedEscalationLevel"] != DBNull.Value) ? Convert.ToInt32(rActiveAlert["LastExecutedEscalationLevel"]) : 0);
			activeAlert.IncidentNumber = ((rActiveAlert["IncidentNumber"] != DBNull.Value) ? Convert.ToString(rActiveAlert["IncidentNumber"]) : string.Empty);
			activeAlert.IncidentUrl = ((rActiveAlert["IncidentUrl"] != DBNull.Value) ? Convert.ToString(rActiveAlert["IncidentUrl"]) : string.Empty);
			activeAlert.AssignedTo = ((rActiveAlert["AssignedTo"] != DBNull.Value) ? Convert.ToString(rActiveAlert["AssignedTo"]) : string.Empty);
			this.FillCustomPropertiesFromRow(rActiveAlert, customProperties, activeAlert);
			return activeAlert;
		}

		// Token: 0x0600068E RID: 1678 RVA: 0x00027B7C File Offset: 0x00025D7C
		private void FillCustomPropertiesFromRow(DataRow rActiveAlert, IEnumerable<CustomProperty> customProperties, ActiveAlert activeAlert)
		{
			foreach (CustomProperty customProperty in customProperties)
			{
				object value = null;
				if (rActiveAlert[customProperty.PropertyName] != DBNull.Value)
				{
					if (customProperty.PropertyType == typeof(string))
					{
						value = Convert.ToString(rActiveAlert[customProperty.PropertyName]);
					}
					else if (customProperty.PropertyType == typeof(DateTime))
					{
						value = DateTime.SpecifyKind(Convert.ToDateTime(rActiveAlert[customProperty.PropertyName]), DateTimeKind.Local);
					}
					else if (customProperty.PropertyType == typeof(int))
					{
						value = Convert.ToInt32(rActiveAlert[customProperty.PropertyName]);
					}
					else if (customProperty.PropertyType == typeof(float))
					{
						value = Convert.ToSingle(rActiveAlert[customProperty.PropertyName]);
					}
					else if (customProperty.PropertyType == typeof(bool))
					{
						value = Convert.ToBoolean(rActiveAlert[customProperty.PropertyName]);
					}
				}
				activeAlert.CustomProperties.Add(customProperty.PropertyName, value);
			}
		}

		// Token: 0x0600068F RID: 1679 RVA: 0x00027CF0 File Offset: 0x00025EF0
		private int GetAlertObjectIdByAlertActiveId(long alertActiveId)
		{
			int result = 0;
			string query = "SELECT TOP 1 AlertObjectID FROM Orion.AlertHistory (nolock=true) WHERE AlertActiveID=@alertActiveID";
			using (IInformationServiceProxy2 informationServiceProxy = this._swisProxyCreator.Create())
			{
				DataTable dataTable = informationServiceProxy.QueryWithAppendedErrors(query, new Dictionary<string, object>
				{
					{
						"alertActiveID",
						alertActiveId
					}
				});
				if (dataTable.Rows.Count > 0)
				{
					result = ((dataTable.Rows[0]["AlertObjectID"] != DBNull.Value) ? Convert.ToInt32(dataTable.Rows[0]["AlertObjectID"]) : 0);
				}
			}
			return result;
		}

		// Token: 0x06000690 RID: 1680 RVA: 0x00027D98 File Offset: 0x00025F98
		private ActiveAlertStatus GetTriggeredStatusForActiveAlert(int alertObjectId)
		{
			if (!SqlHelper.ExecuteExistsParams("SELECT AlertActiveID FROM dbo.AlertActive WITH(NOLOCK) WHERE AlertObjectID=@alertObjectId", new SqlParameter[]
			{
				new SqlParameter("@alertObjectId", alertObjectId)
			}))
			{
				return ActiveAlertStatus.NotTriggered;
			}
			return ActiveAlertStatus.Triggered;
		}

		// Token: 0x06000691 RID: 1681 RVA: 0x00027DC2 File Offset: 0x00025FC2
		internal DataTable GetAlertResetOrUpdateIndicationPropertiesTableByAlertObjectIds(IEnumerable<int> alertObjectIds)
		{
			return this._alertPropertiesProvider.Value.GetAlertIndicationProperties(alertObjectIds);
		}

		// Token: 0x06000692 RID: 1682 RVA: 0x00027DD5 File Offset: 0x00025FD5
		private string GetQueryWithViewLimitations(string query, int viewLimitationId)
		{
			if (viewLimitationId != 0)
			{
				return string.Format("{0} WITH LIMITATION {1}", query, viewLimitationId);
			}
			return query;
		}

		// Token: 0x06000693 RID: 1683 RVA: 0x00027DF0 File Offset: 0x00025FF0
		private int GetOnlyViewLimitation(IEnumerable<int> limitationIds, bool federationEnabled = false)
		{
			if (limitationIds == null || !limitationIds.Any<int>())
			{
				return 0;
			}
			if (limitationIds.Count<int>() == 1)
			{
				return limitationIds.ElementAt(0);
			}
			string query = "SELECT AccountID, LimitationID1, LimitationID2, LimitationID3 FROM Orion.Accounts (nolock=true) \r\n                                 WHERE (LimitationID1 IS NOT NULL OR LimitationID1<>0 OR LimitationID2 IS NOT NULL \r\n                                        OR LimitationID2<>0 OR LimitationID3 IS NOT NULL OR LimitationID3<>0)\r\n                                 AND AccountID=@accountID";
			string accountID = AccountContext.GetAccountID();
			using (IInformationServiceProxy2 informationServiceProxy = this._swisProxyCreator.Create())
			{
				DataTable dataTable = informationServiceProxy.QueryWithAppendedErrors(query, new Dictionary<string, object>
				{
					{
						"accountID",
						accountID
					}
				}, federationEnabled);
				int limitationID1 = 0;
				int limitationID2 = 0;
				int limitationID3 = 0;
				if (dataTable.Rows.Count > 0 || limitationIds.Any<int>())
				{
					if (dataTable.Rows.Count > 0)
					{
						limitationID1 = ((dataTable.Rows[0]["LimitationID1"] != DBNull.Value) ? Convert.ToInt32(dataTable.Rows[0]["LimitationID1"]) : 0);
						limitationID2 = ((dataTable.Rows[0]["LimitationID2"] != DBNull.Value) ? Convert.ToInt32(dataTable.Rows[0]["LimitationID2"]) : 0);
						limitationID3 = ((dataTable.Rows[0]["LimitationID3"] != DBNull.Value) ? Convert.ToInt32(dataTable.Rows[0]["LimitationID3"]) : 0);
					}
					return limitationIds.FirstOrDefault((int item) => item != limitationID1 && item != limitationID2 && item != limitationID3);
				}
			}
			return limitationIds.ElementAt(0);
		}

		// Token: 0x06000694 RID: 1684 RVA: 0x00027FA8 File Offset: 0x000261A8
		private IEnumerable<string> GetUrisForGlobalAlert(int id, bool federationEnabled)
		{
			IEnumerable<string> result;
			using (IInformationServiceProxy2 informationServiceProxy = this._swisProxyCreator.Create())
			{
				result = from item in informationServiceProxy.QueryWithAppendedErrors("SELECT a.AlertActiveObjects.EntityUri\r\n                                              FROM Orion.AlertActive (nolock=true) AS a\r\n                                              WHERE a.AlertObjectID=@objectId", new Dictionary<string, object>
				{
					{
						"objectId",
						id
					}
				}, federationEnabled).AsEnumerable()
				where item["EntityUri"] != DBNull.Value
				select Convert.ToString(item["EntityUri"]);
			}
			return result;
		}

		// Token: 0x06000695 RID: 1685 RVA: 0x00028050 File Offset: 0x00026250
		public ActiveAlert GetActiveAlert(int alertObjectId, IEnumerable<int> limitationIDs, bool includeNotTriggered = true)
		{
			ActiveAlert activeAlert = null;
			IEnumerable<CustomProperty> customPropertiesForEntity = CustomPropertyMgr.GetCustomPropertiesForEntity("Orion.AlertConfigurationsCustomProperties");
			string text = this.GetActiveAlertQuery(customPropertiesForEntity, includeNotTriggered);
			text += " WHERE Data.AlertObjectID=@alertObjectId";
			using (IInformationServiceProxy2 informationServiceProxy = this._swisProxyCreator.Create())
			{
				DataTable dataTable = informationServiceProxy.QueryWithAppendedErrors(text, new Dictionary<string, object>
				{
					{
						"alertObjectId",
						alertObjectId
					}
				});
				if (dataTable.Rows.Count > 0)
				{
					activeAlert = this.GetActiveAlertFromDataRow(dataTable.Rows[0], customPropertiesForEntity);
					AlertIncidentCache.Build(informationServiceProxy, new int?(activeAlert.Id), false).FillIncidentInfo(activeAlert);
					if (!string.IsNullOrEmpty(activeAlert.RelatedNodeEntityUri))
					{
						text = "SELECT NodeID, Status FROM Orion.Nodes (nolock=true) WHERE Uri=@uri";
						DataTable dataTable2 = informationServiceProxy.QueryWithAppendedErrors(text, new Dictionary<string, object>
						{
							{
								"uri",
								activeAlert.RelatedNodeEntityUri
							}
						});
						if (dataTable2.Rows.Count > 0)
						{
							activeAlert.RelatedNodeID = ((dataTable2.Rows[0]["NodeID"] != DBNull.Value) ? Convert.ToInt32(dataTable2.Rows[0]["NodeID"]) : 0);
							activeAlert.RelatedNodeStatus = ((dataTable2.Rows[0]["Status"] != DBNull.Value) ? Convert.ToInt32(dataTable2.Rows[0]["Status"]) : 0);
							activeAlert.RelatedNodeDetailsUrl = string.Format("/Orion/View.aspx?NetObject=N:{0}", activeAlert.RelatedNodeID);
						}
					}
					if (activeAlert.TriggeringObjectEntityName == "Orion.Nodes")
					{
						activeAlert.ActiveNetObject = Convert.ToString(activeAlert.RelatedNodeID);
					}
					if (!string.IsNullOrEmpty(activeAlert.TriggeringObjectEntityUri))
					{
						text = "SELECT TME.Status, TME.Uri FROM System.ManagedEntity (nolock=true) TME WHERE TME.Uri=@uri";
						DataTable dataTable3 = informationServiceProxy.QueryWithAppendedErrors(text, new Dictionary<string, object>
						{
							{
								"uri",
								activeAlert.TriggeringObjectEntityUri
							}
						});
						if (dataTable3.Rows.Count > 0)
						{
							activeAlert.TriggeringObjectStatus = ((dataTable3.Rows[0]["Status"] != DBNull.Value) ? Convert.ToInt32(dataTable3.Rows[0]["Status"]) : 0);
						}
					}
					else
					{
						activeAlert.TriggeringObjectStatus = this.GetRollupStatusForGlobalAlert(activeAlert.TriggeringObjectEntityName, alertObjectId, false);
					}
					activeAlert.Status = this.GetTriggeredStatusForActiveAlert(alertObjectId);
				}
			}
			return activeAlert;
		}

		// Token: 0x06000696 RID: 1686 RVA: 0x000282C4 File Offset: 0x000264C4
		private int GetRollupStatusForGlobalAlert(string entity, int alertObjectId, bool federationEnabled = false)
		{
			if (!this.EntityHasStatusProperty(entity, federationEnabled))
			{
				return 0;
			}
			IEnumerable<string> urisForGlobalAlert = this.GetUrisForGlobalAlert(alertObjectId, federationEnabled);
			if (!urisForGlobalAlert.Any<string>())
			{
				return 0;
			}
			List<int> statuses = new List<int>();
			StringBuilder sbCondition = new StringBuilder();
			Action<Dictionary<string, object>> action = delegate(Dictionary<string, object> swqlParameters)
			{
				string query = string.Format("SELECT Status FROM {0} (nolock=true) WHERE {1}", entity, sbCondition);
				using (IInformationServiceProxy2 informationServiceProxy = this._swisProxyCreator.Create())
				{
					foreach (object obj in informationServiceProxy.QueryWithAppendedErrors(query, swqlParameters, federationEnabled).Rows)
					{
						DataRow dataRow = (DataRow)obj;
						int item = 0;
						if (dataRow["Status"] == DBNull.Value || !int.TryParse(Convert.ToString(dataRow["Status"]), out item))
						{
							item = 0;
						}
						statuses.Add(item);
					}
				}
			};
			int num = 0;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			foreach (string value in urisForGlobalAlert)
			{
				if (num > 0)
				{
					sbCondition.Append(" OR ");
				}
				sbCondition.AppendFormat("Uri=@uri{0}", num);
				dictionary.Add(string.Format("uri{0}", num), value);
				num++;
				if (num % 1000 == 0)
				{
					action(dictionary);
					dictionary = new Dictionary<string, object>();
					sbCondition.Clear();
					num = 0;
				}
			}
			if (num > 0)
			{
				action(dictionary);
			}
			return StatusInfo.RollupStatus(statuses, 2);
		}

		// Token: 0x06000697 RID: 1687 RVA: 0x0002840C File Offset: 0x0002660C
		private bool EntityHasStatusProperty(string entity, bool federationEnabled = false)
		{
			bool result;
			using (IInformationServiceProxy2 informationServiceProxy = this._swisProxyCreator.Create())
			{
				result = (informationServiceProxy.QueryWithAppendedErrors("SELECT Name FROM Metadata.Property WHERE EntityName=@entityName AND Name='Status'", new Dictionary<string, object>
				{
					{
						"entityName",
						entity
					}
				}, federationEnabled).Rows.Count > 0);
			}
			return result;
		}

		// Token: 0x06000698 RID: 1688 RVA: 0x00028470 File Offset: 0x00026670
		public IEnumerable<ActiveAlert> GetActiveAlerts(IEnumerable<CustomProperty> customProperties, IEnumerable<int> limitationIDs, out List<ErrorMessage> errors, bool federationEnabled, bool includeNotTriggered = false)
		{
			List<ActiveAlert> list = new List<ActiveAlert>();
			string text = this.GetActiveAlertQuery(customProperties, includeNotTriggered);
			if (OrionConfiguration.IsDemoServer)
			{
				text += " WHERE (Data.TriggeredDateTime <= GETUTCDATE())";
			}
			string queryWithViewLimitations = this.GetQueryWithViewLimitations(text, this.GetOnlyViewLimitation(limitationIDs, federationEnabled));
			Dictionary<string, ActiveAlert> dictionary = new Dictionary<string, ActiveAlert>();
			using (IInformationServiceProxy2 informationServiceProxy = this._swisProxyCreator.Create())
			{
				DataTable dataTable = informationServiceProxy.QueryWithAppendedErrors(queryWithViewLimitations, federationEnabled);
				errors = FederatedResultTableParser.GetErrorsFromDataTable(dataTable);
				AlertIncidentCache alertIncidentCache = AlertIncidentCache.Build(informationServiceProxy, null, false);
				foreach (object obj in dataTable.Rows)
				{
					DataRow dataRow = (DataRow)obj;
					int num = (dataRow["AlertID"] != DBNull.Value) ? Convert.ToInt32(dataRow["AlertID"]) : 0;
					string arg = (dataRow["EntityUri"] != DBNull.Value) ? Convert.ToString(dataRow["EntityUri"]) : string.Empty;
					string key = string.Format("{0}|{1}", arg, num);
					if (!dictionary.ContainsKey(key))
					{
						ActiveAlert activeAlertFromDataRow = this.GetActiveAlertFromDataRow(dataRow, customProperties);
						alertIncidentCache.FillIncidentInfo(activeAlertFromDataRow);
						if (string.IsNullOrEmpty(activeAlertFromDataRow.TriggeringObjectEntityUri))
						{
							activeAlertFromDataRow.TriggeringObjectStatus = this.GetRollupStatusForGlobalAlert(activeAlertFromDataRow.TriggeringObjectEntityName, activeAlertFromDataRow.Id, false);
						}
						list.Add(activeAlertFromDataRow);
					}
				}
			}
			return list;
		}

		// Token: 0x06000699 RID: 1689 RVA: 0x00028630 File Offset: 0x00026830
		public List<ActiveAlertDetailed> GetAlertTableByDate(DateTime dateTime, int? lastAlertHistoryId, List<int> limitationIDs)
		{
			List<ActiveAlert> list = new List<ActiveAlert>();
			StringBuilder stringBuilder = new StringBuilder(this.GetActiveAlertTableByDateQuery());
			object value;
			if (lastAlertHistoryId != null)
			{
				value = lastAlertHistoryId.Value;
				stringBuilder.Append("AND (AlertHistory.AlertHistoryID > @param)");
			}
			else
			{
				value = dateTime;
				stringBuilder.Append("AND (AlertHistory.TimeStamp > @param)");
			}
			string queryWithViewLimitations = this.GetQueryWithViewLimitations(stringBuilder.ToString(), this.GetOnlyViewLimitation(limitationIDs, false));
			using (IInformationServiceProxy2 informationServiceProxy = this._swisProxyCreator.Create())
			{
				foreach (object obj in informationServiceProxy.QueryWithAppendedErrors(queryWithViewLimitations, new Dictionary<string, object>
				{
					{
						"param",
						value
					}
				}).Rows)
				{
					DataRow dataRow = (DataRow)obj;
					ActiveAlertDetailed activeAlertDetailed = new ActiveAlertDetailed
					{
						Id = ((dataRow["AlertActiveID"] != DBNull.Value) ? Convert.ToInt32(dataRow["AlertActiveID"]) : 0),
						AlertHistoryId = ((dataRow["AlertHistoryID"] != DBNull.Value) ? Convert.ToInt32(dataRow["AlertHistoryID"]) : 0),
						AlertDefId = ((dataRow["AlertID"] != DBNull.Value) ? Convert.ToString(dataRow["AlertID"]) : string.Empty),
						AlertRefID = ((dataRow["AlertRefID"] != DBNull.Value) ? new Guid(Convert.ToString(dataRow["AlertRefID"])) : Guid.Empty),
						ActiveNetObject = ((dataRow["AlertObjectID"] != DBNull.Value) ? Convert.ToString(dataRow["AlertObjectID"]) : string.Empty),
						ObjectType = ((dataRow["ObjectType"] != DBNull.Value) ? Convert.ToString(dataRow["ObjectType"]) : string.Empty),
						Name = ((dataRow["Name"] != DBNull.Value) ? Convert.ToString(dataRow["Name"]) : string.Empty),
						TriggeringObjectDetailsUrl = ((dataRow["EntityDetailsUrl"] != DBNull.Value) ? Convert.ToString(dataRow["EntityDetailsUrl"]) : string.Empty),
						TriggerDateTime = ((dataRow["TriggeredDateTime"] != DBNull.Value) ? DateTime.SpecifyKind(Convert.ToDateTime(dataRow["TriggeredDateTime"]), DateTimeKind.Utc) : ((dataRow["TimeStamp"] != DBNull.Value) ? Convert.ToDateTime(dataRow["TimeStamp"]) : DateTime.MinValue)).ToLocalTime(),
						TriggeringObjectEntityUri = ((dataRow["EntityUri"] != DBNull.Value) ? Convert.ToString(dataRow["EntityUri"]) : ((dataRow["ActiveObjectEntityUri"] != DBNull.Value) ? Convert.ToString(dataRow["ActiveObjectEntityUri"]) : string.Empty)),
						RelatedNodeEntityUri = ((dataRow["RelatedNodeUri"] != DBNull.Value) ? Convert.ToString(dataRow["RelatedNodeUri"]) : string.Empty),
						TriggeringObjectEntityName = ((dataRow["EntityType"] != DBNull.Value) ? Convert.ToString(dataRow["EntityType"]) : string.Empty),
						Message = ((dataRow["PropertyValue"] != DBNull.Value) ? Convert.ToString(dataRow["PropertyValue"]) : string.Empty),
						TriggeringObjectCaption = ((dataRow["EntityCaption"] != DBNull.Value) ? Convert.ToString(dataRow["EntityCaption"]) : string.Empty),
						ActionType = ((dataRow["ActionTypeID"] != DBNull.Value) ? Convert.ToString(dataRow["ActionTypeID"]) : string.Empty),
						AlertDefDescription = ((dataRow["Description"] != DBNull.Value) ? Convert.ToString(dataRow["Description"]) : string.Empty),
						AlertDefLastEdit = ((dataRow["LastEdit"] != DBNull.Value) ? DateTime.SpecifyKind(Convert.ToDateTime(dataRow["LastEdit"]), DateTimeKind.Utc) : DateTime.MinValue),
						AlertDefSeverity = ((dataRow["Severity"] != DBNull.Value) ? Convert.ToInt32(dataRow["Severity"]) : 2),
						Severity = (ActiveAlertSeverity)((dataRow["Severity"] != DBNull.Value) ? Convert.ToInt32(dataRow["Severity"]) : 2),
						AlertDefMessage = ((dataRow["AlertMessage"] != DBNull.Value) ? Convert.ToString(dataRow["AlertMessage"]) : string.Empty)
					};
					if (dataRow["Acknowledged"] != DBNull.Value && Convert.ToBoolean(dataRow["Acknowledged"]))
					{
						activeAlertDetailed.AcknowledgedBy = ((dataRow["AcknowledgedBy"] != DBNull.Value) ? Convert.ToString(dataRow["AcknowledgedBy"]) : string.Empty);
						activeAlertDetailed.AcknowledgedByFullName = activeAlertDetailed.AcknowledgedBy;
						activeAlertDetailed.AcknowledgedDateTime = ((dataRow["AcknowledgedDateTime"] != DBNull.Value) ? DateTime.SpecifyKind(Convert.ToDateTime(dataRow["AcknowledgedDateTime"]), DateTimeKind.Utc).ToLocalTime() : DateTime.MinValue);
						activeAlertDetailed.Notes = ((dataRow["AcknowledgedNote"] != DBNull.Value) ? Convert.ToString(dataRow["AcknowledgedNote"]) : string.Empty);
					}
					list.Add(activeAlertDetailed);
				}
			}
			return list.Cast<ActiveAlertDetailed>().ToList<ActiveAlertDetailed>();
		}

		// Token: 0x0600069A RID: 1690 RVA: 0x00028C68 File Offset: 0x00026E68
		public int ClearTriggeredActiveAlerts(IEnumerable<int> alertObjectIds, string accountId)
		{
			if (!alertObjectIds.Any<int>())
			{
				return 0;
			}
			string text = string.Empty;
			int result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Empty))
			{
				using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
				{
					using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction(IsolationLevel.ReadCommitted))
					{
						textCommand.Transaction = sqlTransaction;
						for (int i = 0; i < alertObjectIds.Count<int>(); i++)
						{
							if (i < 1000)
							{
								string text2 = string.Format("@alertObjectID{0}", i);
								textCommand.Parameters.AddWithValue(text2, alertObjectIds.ElementAt(i));
								if (text != string.Empty)
								{
									text += ",";
								}
								text += text2;
							}
							else
							{
								if (text != string.Empty)
								{
									text += ",";
								}
								text += alertObjectIds.ElementAt(i);
							}
						}
						textCommand.CommandText = string.Format("SELECT AlertObjectID, AlertActiveID FROM AlertActive WHERE [AlertObjectID] IN ({0})", text);
						foreach (object obj in SqlHelper.ExecuteDataTable(textCommand, sqlConnection, null).Rows)
						{
							DataRow dataRow = (DataRow)obj;
							int alertObjectID = (dataRow["AlertObjectID"] != DBNull.Value) ? Convert.ToInt32(dataRow["AlertObjectID"]) : 0;
							long alertActiveID = (dataRow["AlertActiveID"] != DBNull.Value) ? Convert.ToInt64(dataRow["AlertActiveID"]) : 0L;
							AlertHistory alertHistory = new AlertHistory();
							alertHistory.EventType = EventType.Cleared;
							alertHistory.AccountID = accountId;
							alertHistory.TimeStamp = DateTime.UtcNow;
							this._alertHistoryDAL.InsertHistoryItem(alertHistory, alertActiveID, alertObjectID, sqlConnection, sqlTransaction);
						}
						foreach (int alertObjectId in alertObjectIds)
						{
							this.UpdateAlertCaptionAfterReset(alertObjectId, sqlTransaction);
						}
						textCommand.CommandText = string.Format("DELETE FROM AlertActive WHERE AlertObjectID IN ({0})", text);
						int num = SqlHelper.ExecuteNonQuery(textCommand, sqlConnection, sqlTransaction);
						sqlTransaction.Commit();
						result = num;
					}
				}
			}
			return result;
		}

		// Token: 0x0600069B RID: 1691 RVA: 0x00028F24 File Offset: 0x00027124
		public IEnumerable<AlertClearedIndicationProperties> GetAlertClearedIndicationPropertiesByAlertObjectIds(IEnumerable<int> alertObjectIds)
		{
			if (!alertObjectIds.Any<int>())
			{
				return Enumerable.Empty<AlertClearedIndicationProperties>();
			}
			List<AlertClearedIndicationProperties> list = new List<AlertClearedIndicationProperties>();
			foreach (object obj in this.GetAlertResetOrUpdateIndicationPropertiesTableByAlertObjectIds(alertObjectIds).Rows)
			{
				DataRow dataRow = (DataRow)obj;
				AlertClearedIndicationProperties alertClearedIndicationProperties = new AlertClearedIndicationProperties();
				alertClearedIndicationProperties.ClearedTime = DateTime.UtcNow;
				AlertSeverity alertSeverity = (dataRow["Severity"] != DBNull.Value) ? Convert.ToInt32(dataRow["Severity"]) : 2;
				alertClearedIndicationProperties.AlertId = ((dataRow["AlertID"] != DBNull.Value) ? Convert.ToInt32(dataRow["AlertID"]) : 0);
				alertClearedIndicationProperties.AlertName = ((dataRow["Name"] != DBNull.Value) ? Convert.ToString(dataRow["Name"]) : string.Empty);
				alertClearedIndicationProperties.AlertObjectId = ((dataRow["AlertObjectID"] != DBNull.Value) ? Convert.ToInt32(dataRow["AlertObjectID"]) : 0);
				alertClearedIndicationProperties.AlertDefinitionId = ((dataRow["AlertRefID"] != DBNull.Value) ? new Guid(Convert.ToString(dataRow["AlertRefID"])) : Guid.Empty);
				alertClearedIndicationProperties.DetailsUrl = ((dataRow["EntityDetailsUrl"] != DBNull.Value) ? Convert.ToString(dataRow["EntityDetailsUrl"]) : string.Empty);
				alertClearedIndicationProperties.Message = ((dataRow["TriggeredMessage"] != DBNull.Value) ? Convert.ToString(dataRow["TriggeredMessage"]) : string.Empty);
				alertClearedIndicationProperties.ObjectId = ((dataRow["EntityUri"] != DBNull.Value) ? Convert.ToString(dataRow["EntityUri"]) : string.Empty);
				alertClearedIndicationProperties.ObjectName = ((dataRow["EntityType"] != DBNull.Value) ? Convert.ToString(dataRow["EntityType"]) : string.Empty);
				alertClearedIndicationProperties.NetObject = ((dataRow["EntityNetObjectId"] != DBNull.Value) ? Convert.ToString(dataRow["EntityNetObjectId"]) : string.Empty);
				alertClearedIndicationProperties.EntityCaption = ((dataRow["EntityCaption"] != DBNull.Value) ? Convert.ToString(dataRow["EntityCaption"]) : string.Empty);
				alertClearedIndicationProperties.ObjectType = alertClearedIndicationProperties.ObjectName;
				alertClearedIndicationProperties.TriggerTimeStamp = ((dataRow["TriggeredDateTime"] != DBNull.Value) ? DateTime.SpecifyKind(Convert.ToDateTime(dataRow["TriggeredDateTime"]), DateTimeKind.Utc) : DateTime.MinValue);
				AlertClearedIndicationProperties alertClearedIndicationProperties2 = alertClearedIndicationProperties;
				IEnumerable<string> objectUris;
				if (!string.IsNullOrWhiteSpace(alertClearedIndicationProperties.ObjectId))
				{
					IEnumerable<string> enumerable = new List<string>();
					objectUris = enumerable;
				}
				else
				{
					objectUris = this.GetGlobalAlertRelatedUris(alertClearedIndicationProperties.AlertId);
				}
				alertClearedIndicationProperties2.ObjectUris = objectUris;
				alertClearedIndicationProperties.Severity = alertSeverity.ToString();
				list.Add(alertClearedIndicationProperties);
			}
			return list;
		}

		// Token: 0x0600069C RID: 1692 RVA: 0x00029238 File Offset: 0x00027438
		private IEnumerable<string> GetGlobalAlertRelatedUris(int AlertID)
		{
			DataTable dataTable;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT ObjectId FROM [AlertConditionState] WHERE [AlertID]=@alertID AND [Type] = 0 AND [Resolved] = 1"))
			{
				textCommand.Parameters.AddWithValue("alertID", AlertID);
				dataTable = SqlHelper.ExecuteDataTable(textCommand);
			}
			List<string> list = new List<string>();
			foreach (object obj in dataTable.Rows)
			{
				DataRow dataRow = (DataRow)obj;
				list.Add(dataRow[0].ToString());
			}
			return list;
		}

		// Token: 0x0600069D RID: 1693 RVA: 0x000292EC File Offset: 0x000274EC
		public IEnumerable<AlertUpdatedIndication> GetAlertUpdatedIndicationPropertiesByAlertObjectIds(IEnumerable<int> alertObjectIds, string accountId, string notes, DateTime acknowledgedDateTime, bool acknowledge)
		{
			if (!alertObjectIds.Any<int>())
			{
				return Enumerable.Empty<AlertUpdatedIndication>();
			}
			return this._alertPropertiesProvider.Value.GetAlertUpdatedIndicationProperties(alertObjectIds, accountId, notes, acknowledgedDateTime, acknowledge);
		}

		// Token: 0x0600069E RID: 1694 RVA: 0x00029313 File Offset: 0x00027513
		public IEnumerable<AlertUpdatedIndication> GetAlertUpdatedIndicationPropertiesByAlertObjectIds(IEnumerable<int> alertObjectIds, string accountId, string notes, DateTime acknowledgedDateTime, bool acknowledge, string method)
		{
			if (!alertObjectIds.Any<int>())
			{
				return Enumerable.Empty<AlertUpdatedIndication>();
			}
			return this._alertPropertiesProvider.Value.GetAlertUpdatedIndicationProperties(alertObjectIds, accountId, notes, acknowledgedDateTime, acknowledge, method);
		}

		// Token: 0x0600069F RID: 1695 RVA: 0x0002933C File Offset: 0x0002753C
		public IEnumerable<int> LimitAlertAckStateUpdateCandidates(IEnumerable<int> alertObjectIds, bool requestedAcknowledgedState)
		{
			if (!alertObjectIds.Any<int>())
			{
				return Enumerable.Empty<int>();
			}
			return (from DataRow row in this.GetAlertResetOrUpdateIndicationPropertiesTableByAlertObjectIds(alertObjectIds).Rows
			where row["AlertObjectId"] != DBNull.Value
			let ackState = row["Acknowledged"] != DBNull.Value && Convert.ToBoolean(row["Acknowledged"])
			where ackState != requestedAcknowledgedState
			select Convert.ToInt32(row["AlertObjectId"])).ToList<int>();
		}

		// Token: 0x060006A0 RID: 1696 RVA: 0x000293F8 File Offset: 0x000275F8
		internal string ActiveTimeToDisplayFormat(TimeSpan activeTime)
		{
			string text = string.Empty;
			if (activeTime.Days > 0)
			{
				text = string.Concat(new object[]
				{
					text,
					activeTime.Days,
					Resources.WEBCODE_PS0_30,
					" "
				});
			}
			if (activeTime.Hours > 0)
			{
				text = string.Concat(new object[]
				{
					text,
					activeTime.Hours,
					Resources.WEBCODE_PS0_31,
					" "
				});
			}
			if (activeTime.Minutes > 0)
			{
				text = string.Concat(new object[]
				{
					text,
					activeTime.Minutes,
					Resources.WEBCODE_PS0_32,
					" "
				});
			}
			return text;
		}

		// Token: 0x060006A1 RID: 1697 RVA: 0x000294B8 File Offset: 0x000276B8
		public ActiveAlertObjectPage GetPageableActiveAlertObjects(PageableActiveAlertObjectRequest request)
		{
			ActiveAlertObjectPage activeAlertObjectPage = new ActiveAlertObjectPage();
			List<ActiveAlertObject> list = new List<ActiveAlertObject>();
			using (IInformationServiceProxy2 informationServiceProxy = this._swisProxyCreator.Create())
			{
				string arg = (!string.IsNullOrEmpty(request.OrderByClause)) ? ("ORDER BY " + request.OrderByClause) : "";
				string query = string.Format("SELECT a.AlertActiveObjects.EntityUri, a.AlertActiveObjects.EntityCaption, a.AlertActiveObjects.EntityDetailsUrl,\r\n                                             a.AcknowledgedBy, a.TriggeredDateTime, MillisecondDiff(a.TriggeredDateTime, getUtcDate()) AS ActiveTime\r\n                                              FROM Orion.AlertActive (nolock=true) AS a\r\n                                              WHERE a.AlertObjectID=@objectId\r\n                                              {0}", arg);
				string queryWithViewLimitations = this.GetQueryWithViewLimitations(query, this.GetOnlyViewLimitation(request.LimitationIDs, false));
				foreach (object obj in informationServiceProxy.QueryWithAppendedErrors(queryWithViewLimitations, new Dictionary<string, object>
				{
					{
						"objectId",
						request.AlertObjectId
					}
				}).Rows)
				{
					DataRow dataRow = (DataRow)obj;
					if (dataRow["EntityUri"] != DBNull.Value)
					{
						string uri = Convert.ToString(dataRow["EntityUri"]);
						string caption = (dataRow["EntityCaption"] != DBNull.Value) ? Convert.ToString(dataRow["EntityCaption"]) : "";
						string detailsUrl = (dataRow["EntityDetailsUrl"] != DBNull.Value) ? Convert.ToString(dataRow["EntityDetailsUrl"]) : "";
						ActiveAlertObject item = new ActiveAlertObject
						{
							Caption = caption,
							Uri = uri,
							DetailsUrl = detailsUrl,
							Entity = request.TriggeringEntityName
						};
						list.Add(item);
					}
				}
			}
			this.FillAlertObjectStatus(request.TriggeringEntityName, list);
			activeAlertObjectPage.TotalRow = list.Count;
			list = list.Skip(request.StartRow).Take(request.PageSize).ToList<ActiveAlertObject>();
			activeAlertObjectPage.ActiveAlertObjects = list;
			return activeAlertObjectPage;
		}

		// Token: 0x060006A2 RID: 1698 RVA: 0x000296C0 File Offset: 0x000278C0
		private void FillAlertObjectStatus(string entity, List<ActiveAlertObject> objects)
		{
			if (!objects.Any<ActiveAlertObject>() || !this.EntityHasStatusProperty(entity, false))
			{
				return;
			}
			Dictionary<string, ActiveAlertObject> lookupUri = objects.ToDictionary((ActiveAlertObject item) => item.Uri, (ActiveAlertObject item) => item);
			StringBuilder sbCondition = new StringBuilder();
			Action<Dictionary<string, object>> action = delegate(Dictionary<string, object> swqlParameters)
			{
				string query = string.Format("SELECT Status, Uri FROM {0} (nolock=true) WHERE {1}", entity, sbCondition);
				using (IInformationServiceProxy2 informationServiceProxy = this._swisProxyCreator.Create())
				{
					foreach (object obj in informationServiceProxy.QueryWithAppendedErrors(query, swqlParameters).Rows)
					{
						DataRow dataRow = (DataRow)obj;
						int status = (dataRow["Status"] != DBNull.Value) ? Convert.ToInt32(dataRow["Status"]) : 0;
						string key = (dataRow["Uri"] != DBNull.Value) ? Convert.ToString(dataRow["Uri"]) : "";
						ActiveAlertObject activeAlertObject2;
						if (lookupUri.TryGetValue(key, out activeAlertObject2))
						{
							activeAlertObject2.Status = status;
						}
					}
				}
			};
			int num = 0;
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			foreach (ActiveAlertObject activeAlertObject in objects)
			{
				if (num > 0)
				{
					sbCondition.Append(" OR ");
				}
				sbCondition.AppendFormat("Uri=@uri{0}", num);
				dictionary.Add(string.Format("uri{0}", num), activeAlertObject.Uri);
				num++;
				if (num % 1000 == 0)
				{
					action(dictionary);
					dictionary = new Dictionary<string, object>();
					sbCondition.Clear();
					num = 0;
				}
			}
			if (num > 0)
			{
				action(dictionary);
			}
		}

		// Token: 0x060006A3 RID: 1699 RVA: 0x00029820 File Offset: 0x00027A20
		private bool UpdateAlertCaptionAfterReset(int alertObjectId, SqlTransaction transaction)
		{
			string value;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT TOP 1 [EntityType] FROM [AlertObjects]\r\n                WHERE [AlertObjectID] = @alertObjectId\r\n                AND [EntityUri] IS NULL"))
			{
				textCommand.Parameters.AddWithValue("alertObjectId", alertObjectId);
				object obj = SqlHelper.ExecuteScalar(textCommand, transaction.Connection, transaction);
				if (obj == DBNull.Value)
				{
					return false;
				}
				value = (obj as string);
				if (string.IsNullOrWhiteSpace(value))
				{
					return false;
				}
			}
			string query = "SELECT DisplayNamePlural FROM Metadata.Entity WHERE FullName = @entityName";
			string arg2;
			using (IInformationServiceProxy2 informationServiceProxy = this._swisProxyCreator.Create())
			{
				DataTable dataTable = informationServiceProxy.QueryWithAppendedErrors(query, new Dictionary<string, object>
				{
					{
						"entityName",
						value
					}
				});
				if (dataTable.Rows.Count <= 0)
				{
					return false;
				}
				string arg = dataTable.Rows[0][0] as string;
				arg2 = string.Format("{0} {1}", 0, arg);
			}
			bool result;
			using (SqlCommand textCommand2 = SqlHelper.GetTextCommand(string.Format("UPDATE [AlertObjects] SET EntityCaption = '{0}' \r\n                                                                           WHERE EntityUri IS NULL AND AlertObjectID = @alertObjectId", arg2)))
			{
				textCommand2.Parameters.AddWithValue("alertObjectId", alertObjectId);
				result = (SqlHelper.ExecuteNonQuery(textCommand2, transaction.Connection, transaction) == 1);
			}
			return result;
		}

		// Token: 0x060006A4 RID: 1700 RVA: 0x00029988 File Offset: 0x00027B88
		public string GetAlertNote(int alertObjectId)
		{
			string result;
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT AlertNote FROM AlertObjects WHERE AlertObjectID = @alertObjectId");
				textCommand.Parameters.AddWithValue("@alertObjectId", alertObjectId);
				DataTable dataTable = SqlHelper.ExecuteDataTable(textCommand, sqlConnection, null);
				if (dataTable.Rows.Count > 0)
				{
					result = (dataTable.Rows[0][0] as string);
				}
				else
				{
					result = string.Empty;
				}
			}
			return result;
		}

		// Token: 0x060006A5 RID: 1701 RVA: 0x00029A10 File Offset: 0x00027C10
		public bool SetAlertNote(int alertObjectId, string accountId, string note, DateTime modificationDateTime)
		{
			if (alertObjectId < 0 || string.IsNullOrEmpty(accountId))
			{
				return false;
			}
			bool result;
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction(IsolationLevel.ReadCommitted))
				{
					using (SqlCommand textCommand = SqlHelper.GetTextCommand("UPDATE AlertObjects SET AlertNote = @AlertNote WHERE AlertObjectId=@alertObjectId"))
					{
						textCommand.Parameters.AddWithValue("@alertObjectId", alertObjectId);
						textCommand.Parameters.AddWithValue("@AlertNote", note);
						SqlHelper.ExecuteNonQuery(textCommand, sqlConnection, sqlTransaction);
						textCommand.CommandText = "SELECT AlertActiveId FROM AlertActive WHERE AlertObjectId=@alertObjectId";
						object obj = SqlHelper.ExecuteScalar(textCommand, sqlConnection);
						AlertHistory alertHistory = new AlertHistory();
						int num = 0;
						if (obj != null && obj != DBNull.Value)
						{
							num = Convert.ToInt32(obj);
						}
						alertHistory.EventType = EventType.Note;
						alertHistory.AccountID = accountId;
						alertHistory.Message = note;
						alertHistory.TimeStamp = modificationDateTime.ToUniversalTime();
						this._alertHistoryDAL.InsertHistoryItem(alertHistory, (long)num, alertObjectId, sqlConnection, sqlTransaction);
					}
					sqlTransaction.Commit();
					result = true;
				}
			}
			return result;
		}

		// Token: 0x0400020E RID: 526
		private static readonly Log log = new Log();

		// Token: 0x0400020F RID: 527
		private readonly Lazy<AlertObjectPropertiesProvider> _alertPropertiesProvider;

		// Token: 0x04000210 RID: 528
		private readonly IInformationServiceProxyCreator _swisProxyCreator;

		// Token: 0x04000211 RID: 529
		private readonly IAlertHistoryDAL _alertHistoryDAL;
	}
}
