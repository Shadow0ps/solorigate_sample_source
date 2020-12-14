using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Models.Alerts;
using SolarWinds.Orion.Core.Strings;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x0200007F RID: 127
	internal class AlertIncidentCache
	{
		// Token: 0x06000660 RID: 1632 RVA: 0x000263D8 File Offset: 0x000245D8
		private AlertIncidentCache()
		{
		}

		// Token: 0x06000661 RID: 1633 RVA: 0x000263EC File Offset: 0x000245EC
		public static AlertIncidentCache Build(IInformationServiceProxy2 swisProxy, int? alertObjectId = null, bool detectIntegration = false)
		{
			if (swisProxy == null)
			{
				throw new ArgumentNullException("swisProxy");
			}
			AlertIncidentCache alertIncidentCache = new AlertIncidentCache();
			try
			{
				if (AlertIncidentCache.isIncidentIntegrationAvailable == null || detectIntegration)
				{
					DataTable dataTable = swisProxy.Query("\r\nSELECT COUNT(EntityName) AS Cnt\r\nFROM Metadata.EntityMetadata\r\nWHERE EntityName = 'Orion.ESI.AlertIncident'");
					if (dataTable == null || dataTable.Rows.Count == 0 || Convert.ToUInt32(dataTable.Rows[0][0]) == 0U)
					{
						AlertIncidentCache.log.Debug("Incident integration not found");
						AlertIncidentCache.isIncidentIntegrationAvailable = new bool?(false);
					}
					else
					{
						AlertIncidentCache.log.Debug("Incident integration found");
						AlertIncidentCache.isIncidentIntegrationAvailable = new bool?(true);
					}
				}
				if (!AlertIncidentCache.isIncidentIntegrationAvailable.Value)
				{
					return alertIncidentCache;
				}
				DataTable dataTable2;
				if (alertObjectId != null)
				{
					string text = string.Format("\r\nSELECT AlertObjectID, IncidentNumber, IncidentUrl, AssignedTo\r\nFROM Orion.ESI.AlertIncident\r\nWHERE AlertTriggerState > 0 {0}", "AND AlertObjectID = @aoId");
					dataTable2 = swisProxy.Query(text, new Dictionary<string, object>
					{
						{
							"aoId",
							alertObjectId.Value
						}
					});
				}
				else
				{
					string text2 = string.Format("\r\nSELECT AlertObjectID, IncidentNumber, IncidentUrl, AssignedTo\r\nFROM Orion.ESI.AlertIncident\r\nWHERE AlertTriggerState > 0 {0}", string.Empty);
					dataTable2 = swisProxy.Query(text2);
				}
				foreach (object obj in dataTable2.Rows)
				{
					DataRow dataRow = (DataRow)obj;
					int key = (int)dataRow[0];
					AlertIncidentCache.IncidentInfo item = new AlertIncidentCache.IncidentInfo
					{
						Number = (AlertIncidentCache.Get<string>(dataRow, 1) ?? string.Empty),
						Url = (AlertIncidentCache.Get<string>(dataRow, 2) ?? string.Empty),
						AssignedTo = (AlertIncidentCache.Get<string>(dataRow, 3) ?? string.Empty)
					};
					List<AlertIncidentCache.IncidentInfo> list;
					if (!alertIncidentCache.incidentInfoByAlertObjectId.TryGetValue(key, out list))
					{
						list = (alertIncidentCache.incidentInfoByAlertObjectId[key] = new List<AlertIncidentCache.IncidentInfo>());
					}
					list.Add(item);
				}
			}
			catch (Exception ex)
			{
				AlertIncidentCache.log.Error(ex);
			}
			return alertIncidentCache;
		}

		// Token: 0x06000662 RID: 1634 RVA: 0x00026610 File Offset: 0x00024810
		public void FillIncidentInfo(ActiveAlert activeAlert)
		{
			if (activeAlert == null)
			{
				throw new ArgumentNullException("activeAlert");
			}
			List<AlertIncidentCache.IncidentInfo> list;
			if (!this.incidentInfoByAlertObjectId.TryGetValue(activeAlert.Id, out list) || list.Count == 0)
			{
				return;
			}
			if (list.Count == 1)
			{
				activeAlert.IncidentNumber = list[0].Number;
				activeAlert.IncidentUrl = list[0].Url;
				activeAlert.AssignedTo = list[0].AssignedTo;
				return;
			}
			activeAlert.IncidentNumber = string.Format(CultureInfo.InvariantCulture, Resources2.ActiveAlertsGrid_IncidentsClomun_ValueFormat, list.Count);
			activeAlert.IncidentUrl = string.Format(CultureInfo.InvariantCulture, "/Orion/View.aspx?NetObject=AAT:{0}", activeAlert.Id);
			List<string> list2 = (from i in list
			select i.AssignedTo into u
			where !string.IsNullOrEmpty(u)
			select u).Distinct<string>().ToList<string>();
			if (list2.Count == 1)
			{
				activeAlert.AssignedTo = list2.First<string>();
				return;
			}
			activeAlert.AssignedTo = Resources2.ActiveAlertsGrid_IncidentAssignee_MultiUser;
		}

		// Token: 0x06000663 RID: 1635 RVA: 0x00026740 File Offset: 0x00024940
		private static T Get<T>(DataRow row, int colIndex)
		{
			if (row[colIndex] != DBNull.Value)
			{
				return (T)((object)row[colIndex]);
			}
			return default(T);
		}

		// Token: 0x04000202 RID: 514
		private static readonly Log log = new Log();

		// Token: 0x04000203 RID: 515
		private static bool? isIncidentIntegrationAvailable = null;

		// Token: 0x04000204 RID: 516
		internal const string AlertUrlFormat = "/Orion/View.aspx?NetObject=AAT:{0}";

		// Token: 0x04000205 RID: 517
		internal Dictionary<int, List<AlertIncidentCache.IncidentInfo>> incidentInfoByAlertObjectId = new Dictionary<int, List<AlertIncidentCache.IncidentInfo>>();

		// Token: 0x0200016D RID: 365
		internal class IncidentInfo
		{
			// Token: 0x17000158 RID: 344
			// (get) Token: 0x06000BE6 RID: 3046 RVA: 0x00049A49 File Offset: 0x00047C49
			// (set) Token: 0x06000BE7 RID: 3047 RVA: 0x00049A51 File Offset: 0x00047C51
			public string Number { get; set; }

			// Token: 0x17000159 RID: 345
			// (get) Token: 0x06000BE8 RID: 3048 RVA: 0x00049A5A File Offset: 0x00047C5A
			// (set) Token: 0x06000BE9 RID: 3049 RVA: 0x00049A62 File Offset: 0x00047C62
			public string Url { get; set; }

			// Token: 0x1700015A RID: 346
			// (get) Token: 0x06000BEA RID: 3050 RVA: 0x00049A6B File Offset: 0x00047C6B
			// (set) Token: 0x06000BEB RID: 3051 RVA: 0x00049A73 File Offset: 0x00047C73
			public string AssignedTo { get; set; }
		}
	}
}
