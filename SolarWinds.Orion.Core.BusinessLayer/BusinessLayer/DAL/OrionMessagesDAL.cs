using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using SolarWinds.Common.Extensions;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.InformationService;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Common.Swis;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x020000A6 RID: 166
	internal class OrionMessagesDAL
	{
		// Token: 0x0600084F RID: 2127 RVA: 0x0003B338 File Offset: 0x00039538
		public static DataTable GetOrionMessagesTable(OrionMessagesFilter filter)
		{
			if (!filter.IncludeAlerts && !filter.IncludeEvents && !filter.IncludeSyslogs && !filter.IncludeTraps && !filter.IncludeAudits)
			{
				return null;
			}
			string arg = "SELECT MsgID, DateTime, MessageType, Icon, Message, ObjectType,\r\nObjectID, ObjectID2, NetObjectValue, IPAddress, Caption, BackColor, \r\nAcknowledged, ActiveNetObject, NetObjectPrefix, SiteId, SiteName FROM";
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendFormat("SELECT TOP {0}\r\nMsgID, DateTime, MessageType, Icon, Message, ObjectType,\r\nObjectID, ObjectID2, NetObjectValue, IPAddress, Caption, BackColor, \r\nAcknowledged, ActiveNetObject, NetObjectPrefix, SiteId, SiteName FROM (", filter.Count);
			string format = " {1} ( {0} )";
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			bool flag6 = false;
			int num = 0;
			if (!string.IsNullOrEmpty(filter.AlertType))
			{
				string text = filter.AlertType;
				if (text.StartsWith("AA-"))
				{
					text = text.Substring("AA-".Length);
				}
				int num2;
				if (int.TryParse(text, out num2))
				{
					num = num2;
				}
			}
			if (filter.NodeId != null)
			{
				flag = true;
			}
			else if (!string.IsNullOrEmpty(filter.DeviceType))
			{
				flag2 = true;
			}
			else if (!string.IsNullOrEmpty(filter.Vendor))
			{
				flag3 = true;
			}
			else if (!string.IsNullOrEmpty(filter.IpAddress))
			{
				flag4 = true;
			}
			else if (!string.IsNullOrEmpty(filter.Hostname))
			{
				flag5 = true;
			}
			if (filter.SiteID != null)
			{
				flag6 = true;
			}
			if (filter.IncludeAlerts)
			{
				stringBuilder.AppendFormat(format, OrionMessagesDAL.GetNewAlertsSwql(flag, flag2, flag3, flag4, flag5, flag6, num > 0, !string.IsNullOrEmpty(filter.SearchString), filter.ShowAcknowledged, filter.AlertCategoryLimitation, filter.Count), arg);
				format = " UNION ( {1} ( {0} ) ) ";
			}
			if (filter.IncludeAudits)
			{
				stringBuilder.AppendFormat(format, OrionMessagesDAL.GetAuditSwql(flag, flag2, flag3, flag4, flag5, flag6, filter), arg);
				format = " UNION ( {1} ( {0} ) ) ";
			}
			if (filter.IncludeEvents)
			{
				stringBuilder.AppendFormat(format, OrionMessagesDAL.GetEventsSwql(flag, flag2, flag3, flag4, flag5, flag6, !string.IsNullOrEmpty(filter.EventType), !string.IsNullOrEmpty(filter.SearchString), filter.ShowAcknowledged, filter.Count), arg);
				format = " UNION ( {1} ( {0} ) ) ";
			}
			if (filter.IncludeSyslogs)
			{
				stringBuilder.AppendFormat(format, OrionMessagesDAL.GetSyslogSwql(flag, flag2, flag3, flag4, flag5, flag6, filter.SyslogSeverity < byte.MaxValue, filter.SyslogFacility < byte.MaxValue, !string.IsNullOrEmpty(filter.SearchString), filter.ShowAcknowledged, filter.Count), arg);
				format = " UNION ( {1} ( {0} ) ) ";
			}
			if (filter.IncludeTraps)
			{
				stringBuilder.AppendFormat(format, OrionMessagesDAL.GetTrapsSwql(flag, flag2, flag3, flag4, flag5, flag6, !string.IsNullOrEmpty(filter.TrapType), !string.IsNullOrEmpty(filter.TrapCommunity), !string.IsNullOrEmpty(filter.SearchString), filter.ShowAcknowledged, filter.Count), arg);
			}
			stringBuilder.AppendLine(")a ORDER BY a.DateTime DESC");
			DataTable result;
			using (IInformationServiceProxy2 informationServiceProxy = OrionMessagesDAL.creator.Create())
			{
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				dictionary.Add("fromDate", filter.FromDate);
				dictionary.Add("toDate", filter.ToDate);
				if (flag)
				{
					dictionary.Add("nodeId", filter.NodeId);
				}
				if (flag2)
				{
					dictionary.Add("deviceType", filter.DeviceType);
				}
				if (flag3)
				{
					dictionary.Add("vendor", filter.Vendor);
				}
				if (flag4)
				{
					dictionary.Add("ip_address", string.Format("%{0}%", CommonHelper.FormatFilter(IPAddressHelper.ToStringIp(filter.IpAddress))));
				}
				if (flag5)
				{
					dictionary.Add("hostname", string.Format("%{0}%", CommonHelper.FormatFilter(filter.Hostname)));
				}
				if (flag6)
				{
					dictionary.Add("siteId", filter.SiteID);
				}
				if (!string.IsNullOrEmpty(filter.EventType))
				{
					dictionary.Add("event_type", filter.EventType);
				}
				if (filter.SyslogSeverity < 255)
				{
					dictionary.Add("syslog_severity", filter.SyslogSeverity);
				}
				if (filter.SyslogFacility < 255)
				{
					dictionary.Add("syslog_facility", filter.SyslogFacility);
				}
				if (!string.IsNullOrEmpty(filter.TrapType))
				{
					dictionary.Add("trap_type", filter.TrapType);
				}
				if (!string.IsNullOrEmpty(filter.TrapCommunity))
				{
					dictionary.Add("trap_community", filter.TrapCommunity);
				}
				if (filter.IncludeAlerts && num > 0)
				{
					dictionary.Add("newAlert_id", num);
				}
				if (filter.IncludeAlerts && !string.IsNullOrEmpty(filter.AlertCategoryLimitation))
				{
					dictionary.Add("alertCategoryLimitation", filter.AlertCategoryLimitation);
				}
				if (!string.IsNullOrWhiteSpace(filter.AuditType))
				{
					dictionary.Add("actionTypeId", int.Parse(filter.AuditType));
				}
				if (!string.IsNullOrWhiteSpace(filter.Audituser))
				{
					dictionary.Add("accountId", filter.Audituser);
				}
				if (!string.IsNullOrEmpty(filter.SearchString))
				{
					dictionary.Add("search_str", string.Format("%{0}%", filter.SearchString));
				}
				DataTable dataTable = informationServiceProxy.QueryWithAppendedErrors(stringBuilder.ToString(), dictionary, SwisFederationInfo.IsFederationEnabled);
				dataTable.TableName = "OrionMessages";
				IEnumerableExtensions.Iterate<DataRow>(dataTable.Rows.OfType<DataRow>(), delegate(DataRow r)
				{
					r["DateTime"] = ((DateTime)r["DateTime"]).ToLocalTime();
				});
				result = dataTable;
			}
			return result;
		}

		// Token: 0x06000850 RID: 2128 RVA: 0x0003B8AC File Offset: 0x00039AAC
		private static string GetEventsSwql(bool isNodeId, bool isDeviceType, bool isVendor, bool isIp_address, bool isHostname, bool isSiteId, bool isEventType, bool isSearchString, bool showAck, int maxRowCount)
		{
			StringBuilder stringBuilder = new StringBuilder("SELECT TOP {1}\r\n\tTOSTRING(e.EventID) AS MsgID,\r\n\te.EventTime AS DateTime, \r\n\t'{0}' AS MessageType,\r\n\tTOSTRING(e.EventType) AS Icon,\r\n\te.Message AS Message,\r\n\te.NetObjectType AS ObjectType, \r\n\tTOSTRING(e.NetObjectID) AS ObjectID, \r\n\t'' AS ObjectID2,\r\n    e.NetObjectValue AS NetObjectValue,\r\n\tn.IP_Address AS IPAddress,\r\n\t'' AS Caption,\r\n\tISNULL(et.BackColor, 0) AS BackColor,\r\n\te.Acknowledged AS Acknowledged,\r\n\tTOSTRING(e.NetObjectID) AS ActiveNetObject,\r\n\te.NetObjectType AS NetObjectPrefix,\r\n    e.InstanceSiteId AS SiteId,\r\n    s.Name AS SiteName\r\nFROM Orion.Events (nolock=true) AS e\r\nLEFT JOIN Orion.Nodes (nolock=true) AS n ON e.NetworkNode = n.NodeID AND e.InstanceSiteId = n.InstanceSiteId\r\nLEFT JOIN Orion.EventTypes (nolock=true) AS et ON e.EventType = et.EventType AND e.InstanceSiteId = et.InstanceSiteId\r\nLEFT JOIN Orion.Sites (nolock=true) AS s ON s.SiteID = e.InstanceSiteId\r\nWHERE e.EventTime >= @fromDate AND e.EventTime <= @toDate ");
			if (isNodeId)
			{
				stringBuilder.Append(" AND e.NetworkNode=@nodeId ");
			}
			if (!showAck)
			{
				stringBuilder.Append(" AND e.Acknowledged=0 ");
			}
			if (isDeviceType)
			{
				stringBuilder.Append(" AND n.MachineType Like @deviceType ");
			}
			if (isVendor)
			{
				stringBuilder.Append(" AND n.Vendor = @vendor ");
			}
			if (isIp_address)
			{
				stringBuilder.Append(" AND n.IP_Address Like @ip_address ");
			}
			if (isHostname)
			{
				stringBuilder.Append(" AND (n.SysName Like @hostname OR n.Caption Like @hostname) ");
			}
			if (isEventType)
			{
				stringBuilder.Append(" AND e.EventType = @event_type ");
			}
			if (isSearchString)
			{
				stringBuilder.Append(" AND e.Message Like @search_str ");
			}
			if (isSiteId)
			{
				stringBuilder.Append(" AND e.InstanceSiteId = @siteId ");
			}
			stringBuilder.Append(" ORDER BY e.EventTime DESC");
			return string.Format(stringBuilder.ToString(), OrionMessagesHelper.GetMessageTypeString(OrionMessageType.EVENT_MESSAGE), maxRowCount);
		}

		// Token: 0x06000851 RID: 2129 RVA: 0x0003B974 File Offset: 0x00039B74
		private static string GetSyslogSwql(bool isNodeId, bool isDeviceType, bool isVendor, bool isIp_address, bool isHostname, bool isSiteId, bool isSeverityCode, bool isFacilityCode, bool isSearchString, bool showAck, int maxRowCount)
		{
			StringBuilder stringBuilder = new StringBuilder("SELECT TOP {1}\r\n\tTOSTRING(s.MessageID) AS MsgID,\r\n\ts.DateTime AS DateTime,\r\n\t'{0}' AS MessageType,\r\n\tTOSTRING(s.SysLogSeverity) AS Icon,\r\n\ts.Message AS Message,\r\n\t'N' AS ObjectType, \r\n\tTOSTRING(s.NodeID) AS ObjectID, \r\n\t'' AS ObjectID2,\r\n    '' AS NetObjectValue,\r\n\ts.IPAddress AS IPAddress,\r\n\ts.Hostname AS Caption,\r\n\ts.SysLogSeverity*1 AS BackColor,\r\n\ts.Acknowledged AS Acknowledged,\r\n\tTOSTRING(s.NodeID) AS ActiveNetObject,\r\n\t'N' AS NetObjectPrefix,\r\n    s.InstanceSiteId AS SiteId,\r\n    os.Name AS SiteName\r\nFROM Orion.Syslog (nolock=true) AS s\r\nLEFT JOIN Orion.Nodes (nolock=true) AS n ON s.NodeID = n.NodeID AND s.InstanceSiteId = n.InstanceSiteId\r\nLEFT JOIN Orion.Sites (nolock=true) AS os ON s.InstanceSiteId = os.SiteID\r\nWHERE s.DateTime >= @fromDate AND s.DateTime <= @toDate");
			if (isNodeId)
			{
				stringBuilder.Append(" AND s.NodeID=@nodeId ");
			}
			if (!showAck)
			{
				stringBuilder.Append(" AND s.Acknowledged=0 ");
			}
			if (isDeviceType)
			{
				stringBuilder.Append(" AND n.MachineType Like @deviceType ");
			}
			if (isVendor)
			{
				stringBuilder.Append(" AND n.Vendor = @vendor ");
			}
			if (isIp_address)
			{
				stringBuilder.Append(" AND s.IPAddress Like @ip_address ");
			}
			if (isHostname)
			{
				stringBuilder.Append(" AND (s.Hostname Like @hostname OR n.Caption Like @hostname) ");
			}
			if (isSeverityCode)
			{
				stringBuilder.Append(" AND s.SysLogSeverity = @syslog_severity ");
			}
			if (isFacilityCode)
			{
				stringBuilder.Append(" AND s.SysLogFacility = @syslog_facility ");
			}
			if (isSearchString)
			{
				stringBuilder.Append(" AND s.Message Like @search_str ");
			}
			if (isSiteId)
			{
				stringBuilder.Append(" AND s.InstanceSiteId = @siteId ");
			}
			stringBuilder.Append(" ORDER BY s.DateTime DESC");
			return string.Format(stringBuilder.ToString(), OrionMessagesHelper.GetMessageTypeString(OrionMessageType.SYSLOG_MESSAGE), maxRowCount);
		}

		// Token: 0x06000852 RID: 2130 RVA: 0x0003BA4C File Offset: 0x00039C4C
		private static string GetTrapsSwql(bool isNodeId, bool isDeviceType, bool isVendor, bool isIp_address, bool isHostname, bool isSiteId, bool isTrapType, bool isCommunity, bool isSearchString, bool showAck, int maxRowCount)
		{
			StringBuilder stringBuilder = new StringBuilder("Select TOP {1}\r\n\tTOSTRING(t.TrapID) AS MsgID,\r\n\tt.DateTime AS DateTime,\r\n\t'{0}' AS MessageType,\r\n\t'' AS Icon,\r\n\tt.Message AS Message,\r\n\t'N' AS ObjectType, \r\n\tTOSTRING(t.NodeID) AS ObjectID, \r\n\t'' AS ObjectID2,\r\n    '' AS NetObjectValue,\r\n\tt.IPAddress AS IPAddress,\r\n\tt.Hostname AS Caption,\r\n\tt.ColorCode AS BackColor,\r\n\tt.Acknowledged AS Acknowledged,\r\n\tTOSTRING(t.NodeID) AS ActiveNetObject,\r\n\t'N' AS NetObjectPrefix,\r\n    t.InstanceSiteId AS SiteId,\r\n    s.Name AS SiteName\r\nFROM Orion.Traps (nolock=true) AS t\r\nLEFT JOIN Orion.Nodes (nolock=true) AS n ON t.NodeID = n.NodeID AND t.InstanceSiteId = n.InstanceSiteId\r\nLEFT JOIN Orion.Sites (nolock=true) AS s ON t.InstanceSiteId = s.SiteID\r\nWHERE t.DateTime >= @fromDate AND t.DateTime <= @toDate");
			if (isNodeId)
			{
				stringBuilder.Append(" AND t.NodeID=@nodeId ");
			}
			if (!showAck)
			{
				stringBuilder.Append(" AND t.Acknowledged=0 ");
			}
			if (isDeviceType)
			{
				stringBuilder.Append(" AND n.MachineType Like @deviceType ");
			}
			if (isVendor)
			{
				stringBuilder.Append(" AND n.Vendor = @vendor ");
			}
			if (isIp_address)
			{
				stringBuilder.Append(" AND t.IPAddress Like @ip_address ");
			}
			if (isTrapType)
			{
				stringBuilder.Append(" AND t.TrapType Like @trap_type ");
			}
			if (isHostname)
			{
				stringBuilder.Append(" AND (t.Hostname Like @hostname OR n.Caption Like @hostname) ");
			}
			if (isCommunity)
			{
				stringBuilder.Append(" AND t.Community Like @trap_community ");
			}
			if (isSearchString)
			{
				stringBuilder.Append(" AND t.Message Like @search_str ");
			}
			if (isSiteId)
			{
				stringBuilder.Append(" AND t.InstanceSiteId = @siteId ");
			}
			stringBuilder.Append(" ORDER BY t.DateTime DESC");
			return string.Format(stringBuilder.ToString(), OrionMessagesHelper.GetMessageTypeString(OrionMessageType.TRAP_MESSAGE), maxRowCount);
		}

		// Token: 0x06000853 RID: 2131 RVA: 0x0003BB24 File Offset: 0x00039D24
		private static string GetNewAlertsSwql(bool isNodeId, bool isDeviceType, bool isVendor, bool isIp_address, bool isHostname, bool isSiteId, bool isAlertID, bool isSearchString, bool showAck, string alertCategoryLimitation, int maxRowCount)
		{
			StringBuilder stringBuilder = new StringBuilder("\r\nSELECT TOP {0}\r\n\t'AA-' + TOSTRING(aa.AlertActiveID) AS MsgID,\r\n\taa.TriggeredDateTime AS DateTime,\r\n\t'Advanced Alert' AS MessageType,\r\n\t'' AS Icon,\r\n\taa.TriggeredMessage AS Message,\r\n\t'AAT' AS ObjectType,\r\n\tTOSTRING(aa.AlertObjectID) AS ObjectID,\r\n\t'' AS ObjectID2,\r\n    '' AS NetObjectValue,\r\n\tn.IP_Address AS IPAddress,\r\n\tao.EntityCaption AS Caption,\r\n\tCASE ac.Severity\r\n\t\tWHEN 1 THEN 15658734\r\n\t\tWHEN 2 THEN 16638651\r\n\t\tELSE 16300735\r\n\tEND AS BackColor,\r\n\tCASE \r\n\t\tWHEN aa.Acknowledged IS NULL THEN 0\r\n\t\tELSE 1\r\n\tEND AS Acknowledged,\r\n\t'' AS ActiveNetObject,\r\n\tao.EntityDetailsUrl AS NetObjectPrefix,\r\n    aa.InstanceSiteId AS SiteId,\r\n    s.Name AS SiteName\r\nFROM Orion.AlertActive (nolock=true) AS aa\r\nINNER JOIN Orion.AlertObjects (nolock=true) AS ao ON aa.AlertObjectID = ao.AlertObjectID AND aa.InstanceSiteId = ao.InstanceSiteId\r\nLEFT JOIN Orion.AlertConfigurations (nolock=true) AS ac ON ao.AlertID = ac.AlertID AND ao.InstanceSiteId = ac.InstanceSiteId\r\nLEFT JOIN Orion.Nodes (nolock=true) AS n ON ao.RelatedNodeId = n.NodeID AND ao.InstanceSiteId = n.InstanceSiteId\r\nLEFT JOIN Orion.Sites (nolock=true) AS s ON s.SiteID = aa.InstanceSiteId\r\nWHERE aa.TriggeredDateTime >= @fromDate AND aa.TriggeredDateTime <= @toDate");
			if (isNodeId)
			{
				stringBuilder.Append(" AND n.NodeID=@nodeId ");
			}
			if (isDeviceType)
			{
				stringBuilder.Append(" AND n.MachineType Like @deviceType ");
			}
			if (isVendor)
			{
				stringBuilder.Append(" AND n.Vendor = @vendor ");
			}
			if (isIp_address)
			{
				stringBuilder.Append(" AND n.IP_Address Like @ip_address ");
			}
			if (isHostname)
			{
				stringBuilder.Append(" AND (n.SysName Like @hostname OR n.Caption Like @hostname) ");
			}
			if (isAlertID)
			{
				stringBuilder.Append(" AND ac.AlertID = @newAlert_id ");
			}
			if (isSearchString)
			{
				stringBuilder.Append(" AND ac.Name LIKE @search_str ");
			}
			if (!showAck)
			{
				stringBuilder.Append(" AND aa.Acknowledged IS NULL ");
			}
			if (!string.IsNullOrEmpty(alertCategoryLimitation))
			{
				stringBuilder.Append(" AND ac.Category=@alertCategoryLimitation ");
			}
			if (isSiteId)
			{
				stringBuilder.Append(" AND aa.InstanceSiteId = @siteId ");
			}
			stringBuilder.Append(" ORDER BY aa.TriggeredDateTime DESC");
			return string.Format(stringBuilder.ToString(), maxRowCount);
		}

		// Token: 0x06000854 RID: 2132 RVA: 0x0003BBFC File Offset: 0x00039DFC
		private static string GetAuditSwql(bool isNodeId, bool isDeviceType, bool isVendor, bool isIp_address, bool isHostname, bool isSiteId, OrionMessagesFilter filter)
		{
			object obj = new StringBuilder("\r\nSELECT TOP {1}\r\n    TOSTRING(a.AuditEventID) AS MsgID,\r\n    a.TimeLoggedUtc AS DateTime,\r\n    '{0}' AS MessageType,\r\n    '' AS Icon, \r\n    a.AuditEventMessage AS Message, \r\n\ta.NetObjectType AS ObjectType, \r\n\tTOSTRING(a.NetObjectID) AS ObjectID, \r\n    '' AS ObjectID2, \r\n    '' AS NetObjectValue,\r\n    '' AS IPAddress, \r\n    '' AS Caption,\r\n    0 AS BackColor,\r\n    0 AS Acknowledged,\r\n\tTOSTRING(a.NetObjectID) AS ActiveNetObject,\r\n\ta.NetObjectType AS NetObjectPrefix,\r\n    a.InstanceSiteId AS SiteId,\r\n    s.Name AS SiteName\r\nFROM Orion.AuditingEvents (nolock=true) AS a\r\n{3}\r\nLEFT JOIN Orion.Sites (nolock=true) AS s ON a.InstanceSiteId = s.SiteID\r\nWHERE \r\n    a.TimeLoggedUtc >= @fromDate AND a.TimeLoggedUtc <= @toDate\r\n    {2} \r\n ORDER BY a.TimeLoggedUtc DESC");
			bool flag = false;
			StringBuilder stringBuilder = new StringBuilder();
			if (isNodeId)
			{
				stringBuilder.Append(" AND a.NetworkNode = @nodeId ");
			}
			if (isDeviceType)
			{
				stringBuilder.Append(" AND n.MachineType Like @deviceType ");
				flag = true;
			}
			if (isVendor)
			{
				stringBuilder.Append(" AND n.Vendor = @vendor ");
				flag = true;
			}
			if (isIp_address)
			{
				stringBuilder.Append(" AND n.IP_Address Like @ip_address ");
				flag = true;
			}
			if (isHostname)
			{
				stringBuilder.Append(" AND (n.SysName Like @hostname OR n.Caption Like @hostname) ");
				flag = true;
			}
			if (!string.IsNullOrWhiteSpace(filter.AuditType))
			{
				stringBuilder.Append(" AND a.ActionTypeID = @actionTypeId ");
			}
			if (!string.IsNullOrWhiteSpace(filter.Audituser))
			{
				stringBuilder.Append(" AND a.AccountID LIKE @accountId ");
			}
			if (!string.IsNullOrWhiteSpace(filter.SearchString))
			{
				stringBuilder.Append(" AND a.AuditEventMessage LIKE @search_str ");
			}
			if (isSiteId)
			{
				stringBuilder.Append(" AND a.InstanceSiteId = @siteId");
			}
			return string.Format(obj.ToString(), new object[]
			{
				OrionMessagesHelper.GetMessageTypeString(OrionMessageType.AUDIT_MESSAGE),
				filter.Count,
				stringBuilder,
				flag ? " LEFT JOIN Orion.Nodes (nolock=true) AS n ON a.NetworkNode = n.NodeID AND a.InstanceSiteId = n.InstanceSiteId " : string.Empty
			});
		}

		// Token: 0x04000260 RID: 608
		private static readonly IInformationServiceProxyCreator creator = SwisConnectionProxyPool.GetCreator();

		// Token: 0x04000261 RID: 609
		private const string AdvancedAlertId = "AA-";
	}
}
