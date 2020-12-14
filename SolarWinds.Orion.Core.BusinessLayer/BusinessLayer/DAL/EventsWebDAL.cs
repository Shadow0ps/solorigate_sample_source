using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Models.Events;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000099 RID: 153
	public class EventsWebDAL
	{
		// Token: 0x0600078A RID: 1930 RVA: 0x00033FA0 File Offset: 0x000321A0
		public static DataTable GetEventTypesTable()
		{
			DataTable dataTable = null;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(Limitation.LimitSQL("SELECT DISTINCT Name, EventType FROM EventTypes ORDER BY Name", Array.Empty<Limitation>())))
			{
				dataTable = SqlHelper.ExecuteDataTable(textCommand);
			}
			dataTable.TableName = "EventTypesTable";
			return dataTable;
		}

		// Token: 0x0600078B RID: 1931 RVA: 0x00033FF4 File Offset: 0x000321F4
		public static DataTable GetEvents(GetEventsParameter param)
		{
			if (param == null)
			{
				throw new ArgumentNullException("param");
			}
			DataTable dataTable = null;
			StringBuilder stringBuilder = new StringBuilder();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Empty))
			{
				stringBuilder.Append("SELECT TOP (@maxRecords) NetObjectType, NetObjectID, NetObjectID2, NetObjectValue, EventID, Acknowledged, EventTime, \r\n                             Events.EventType as EventType, Message, ISNULL(EventTypes.BackColor, 0) as BackColor, NodesData.NodeID as LinkNodeID\r\n                             FROM Events WITH(NOLOCK)\r\n                             LEFT JOIN NodesData WITH(NOLOCK) ON Events.NetworkNode = NodesData.NodeID\r\n                             LEFT JOIN NodesCustomProperties WITH(NOLOCK) ON Events.NetworkNode = NodesCustomProperties.NodeID\r\n                             LEFT JOIN EventTypes ON Events.EventType = EventTypes.EventType");
				textCommand.Parameters.Add(new SqlParameter("@maxRecords", param.MaxRecords));
				List<string> list = new List<string>();
				if (!param.IncludeAcknowledged)
				{
					list.Add("Acknowledged='false'");
				}
				if (param.NodeId >= 0)
				{
					list.Add(" Events.NetworkNode=@nodeID");
					textCommand.Parameters.Add(new SqlParameter("@nodeID", param.NodeId));
				}
				if (param.NetObjectId >= 0)
				{
					list.Add(" Events.NetObjectID=@netObjectID");
					textCommand.Parameters.Add(new SqlParameter("@netObjectID", param.NetObjectId));
				}
				if (!string.IsNullOrEmpty(param.NetObjectType))
				{
					list.Add(" Events.NetObjectType Like @netObjectType");
					textCommand.Parameters.Add(new SqlParameter("@netObjectType", SqlDbType.VarChar, 10)
					{
						Value = param.NetObjectType
					});
				}
				if (!string.IsNullOrEmpty(param.DeviceType))
				{
					list.Add(" (NodesData.MachineType Like @deviceType)");
					textCommand.Parameters.Add(new SqlParameter("@deviceType", param.DeviceType));
				}
				if (param.EventType > 0)
				{
					list.Add(" Events.EventType=@eventType");
					textCommand.Parameters.Add(new SqlParameter("@eventType", param.EventType.ToString()));
				}
				if (param.FromDate != null && param.ToDate != null)
				{
					list.Add(" EventTime >= @fromDate AND EventTime <= @toDate");
					textCommand.Parameters.Add(new SqlParameter("@fromDate", param.FromDate));
					textCommand.Parameters.Add(new SqlParameter("@toDate", param.ToDate));
				}
				if (list.Count > 0)
				{
					stringBuilder.Append(" WHERE");
					if (list.Count == 1)
					{
						stringBuilder.AppendFormat(" {0}", list[0]);
					}
					else
					{
						stringBuilder.AppendFormat(" {0}", list[0]);
						for (int i = 1; i < list.Count; i++)
						{
							stringBuilder.AppendFormat(" AND {0}", list[i]);
						}
					}
				}
				stringBuilder.Append(" ORDER BY EventID DESC");
				textCommand.CommandText = Limitation.LimitSQL(stringBuilder.ToString(), param.LimitationIDs);
				dataTable = SqlHelper.ExecuteDataTable(textCommand);
			}
			dataTable.TableName = "EventsTable";
			return dataTable;
		}

		// Token: 0x0600078C RID: 1932 RVA: 0x000342BC File Offset: 0x000324BC
		public static DataTable GetEventsTable(GetEventsParameter param)
		{
			if (param.NetObjectType.Equals("N", StringComparison.OrdinalIgnoreCase))
			{
				param.NodeId = param.NetObjectId;
				param.NetObjectId = -1;
				param.NetObjectType = string.Empty;
				return EventsWebDAL.GetEvents(param);
			}
			param.NodeId = -1;
			return EventsWebDAL.GetEvents(param);
		}

		// Token: 0x0600078D RID: 1933 RVA: 0x00034310 File Offset: 0x00032510
		public static void AcknowledgeEvents(List<int> events)
		{
			if (events == null)
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("UPDATE Events SET Acknowledged='true' WHERE EventID IN (");
			for (int i = 0; i < events.Count - 1; i++)
			{
				stringBuilder.AppendFormat("{0}, ", events[i]);
			}
			stringBuilder.AppendFormat("{0} )", events[events.Count - 1]);
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(stringBuilder.ToString()))
			{
				SqlHelper.ExecuteNonQuery(textCommand);
			}
		}

		// Token: 0x0600078E RID: 1934 RVA: 0x000343AC File Offset: 0x000325AC
		public static DataTable GetEventSummaryTable(int netObjectID, string netObjectType, DateTime fromDate, DateTime toDate, List<int> limitationIDs)
		{
			DataTable dataTable = null;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Empty))
			{
				StringBuilder stringBuilder = new StringBuilder("SELECT Events.EventType as EventType, EventTypes.Name as Name, EventTypes.BackColor as BackColor, Count(Events.EventType) as Total  \r\n                                                        FROM EventTypes \r\n                                                        INNER JOIN Events WITH(NOLOCK) ON EventTypes.EventType = Events.EventType\r\n                                                        LEFT JOIN NodesData WITH(NOLOCK) ON Events.NetworkNode = NodesData.NodeID \r\n                                                        LEFT JOIN NodesCustomProperties WITH(NOLOCK) ON Events.NetworkNode = NodesCustomProperties.NodeID \r\n                                                        WHERE Events.Acknowledged='false' \r\n                                                        AND Events.EventTime >= @fromDate AND Events.EventTime <= @toDate ");
				textCommand.Parameters.Add(new SqlParameter("@fromDate", fromDate));
				textCommand.Parameters.Add(new SqlParameter("@toDate", toDate));
				if (netObjectID >= 0 && !string.IsNullOrEmpty(netObjectType) && netObjectType.Equals("N", StringComparison.OrdinalIgnoreCase))
				{
					stringBuilder.Append(" AND ((Events.NetObjectID=@netObjectID AND Events.NetObjectType LIKE @netObjectType) OR Events.NetworkNode=@netObjectID)");
					textCommand.Parameters.Add(new SqlParameter("@netObjectID", netObjectID));
					textCommand.Parameters.Add(new SqlParameter("@netObjectType", netObjectType));
				}
				else
				{
					if (netObjectID >= 0)
					{
						stringBuilder.Append(" AND Events.NetObjectID=@netObjectID");
						textCommand.Parameters.Add(new SqlParameter("@netObjectID", netObjectID));
					}
					if (!string.IsNullOrEmpty(netObjectType))
					{
						stringBuilder.Append(" AND Events.NetObjectType LIKE @netObjectType");
						textCommand.Parameters.Add(new SqlParameter("@netObjectType", SqlDbType.VarChar, 10)
						{
							Value = netObjectType
						});
					}
				}
				stringBuilder.Append(" GROUP BY Events.EventType, EventTypes.Name, EventTypes.BackColor ORDER BY Events.EventType");
				textCommand.CommandText = Limitation.LimitSQL(stringBuilder.ToString(), limitationIDs);
				dataTable = SqlHelper.ExecuteDataTable(textCommand);
			}
			dataTable.TableName = "EventSummaryTable";
			return dataTable;
		}
	}
}
