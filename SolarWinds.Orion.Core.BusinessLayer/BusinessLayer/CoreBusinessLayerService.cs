using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using SolarWinds.Collector.Contract;
using SolarWinds.Common.Net;
using SolarWinds.Common.Utility;
using SolarWinds.InformationService.Contract2;
using SolarWinds.InformationService.Linq.Plugins;
using SolarWinds.InformationService.Linq.Plugins.Core.Orion;
using SolarWinds.JobEngine;
using SolarWinds.JobEngine.Security;
using SolarWinds.Licensing.Framework;
using SolarWinds.Logging;
using SolarWinds.Net.SNMP;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Actions;
using SolarWinds.Orion.Core.Actions.DAL;
using SolarWinds.Orion.Core.Actions.Runners;
using SolarWinds.Orion.Core.Alerting;
using SolarWinds.Orion.Core.Alerting.DAL;
using SolarWinds.Orion.Core.Alerting.Migration;
using SolarWinds.Orion.Core.Alerting.Migration.Plugins;
using SolarWinds.Orion.Core.Alerting.Models;
using SolarWinds.Orion.Core.Alerting.Plugins.Conditions.Dynamic;
using SolarWinds.Orion.Core.Alerting.Plugins.Conditions.Sql;
using SolarWinds.Orion.Core.BusinessLayer.Agent;
using SolarWinds.Orion.Core.BusinessLayer.BL;
using SolarWinds.Orion.Core.BusinessLayer.CentralizedSettings;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.BusinessLayer.Discovery;
using SolarWinds.Orion.Core.BusinessLayer.Discovery.DiscoveryCache;
using SolarWinds.Orion.Core.BusinessLayer.OneTimeJobs;
using SolarWinds.Orion.Core.BusinessLayer.Thresholds;
using SolarWinds.Orion.Core.BusinessLayer.TraceRoute;
using SolarWinds.Orion.Core.CertificateUpdate;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Agent;
using SolarWinds.Orion.Core.Common.Alerting;
using SolarWinds.Orion.Core.Common.BusinessLayer;
using SolarWinds.Orion.Core.Common.Catalogs;
using SolarWinds.Orion.Core.Common.CentralizedSettings;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Common.Enums;
using SolarWinds.Orion.Core.Common.ExpressionEvaluator;
using SolarWinds.Orion.Core.Common.Extensions;
using SolarWinds.Orion.Core.Common.i18n;
using SolarWinds.Orion.Core.Common.Indications;
using SolarWinds.Orion.Core.Common.InformationService;
using SolarWinds.Orion.Core.Common.JobEngine;
using SolarWinds.Orion.Core.Common.Licensing;
using SolarWinds.Orion.Core.Common.MacroParsing;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Common.Models.Alerts;
using SolarWinds.Orion.Core.Common.Models.Mib;
using SolarWinds.Orion.Core.Common.Models.Technology;
using SolarWinds.Orion.Core.Common.Models.Thresholds;
using SolarWinds.Orion.Core.Common.ModuleManager;
using SolarWinds.Orion.Core.Common.PackageManager;
using SolarWinds.Orion.Core.Common.Proxy.Audit;
using SolarWinds.Orion.Core.Common.Proxy.BusinessLayer;
using SolarWinds.Orion.Core.Common.Settings;
using SolarWinds.Orion.Core.Common.Swis;
using SolarWinds.Orion.Core.Discovery;
using SolarWinds.Orion.Core.Discovery.BL;
using SolarWinds.Orion.Core.Discovery.DAL;
using SolarWinds.Orion.Core.Discovery.DataAccess;
using SolarWinds.Orion.Core.Jobs2;
using SolarWinds.Orion.Core.Models;
using SolarWinds.Orion.Core.Models.Actions;
using SolarWinds.Orion.Core.Models.Actions.Contexts;
using SolarWinds.Orion.Core.Models.Alerting;
using SolarWinds.Orion.Core.Models.Credentials;
using SolarWinds.Orion.Core.Models.DiscoveredObjects;
using SolarWinds.Orion.Core.Models.Discovery;
using SolarWinds.Orion.Core.Models.Events;
using SolarWinds.Orion.Core.Models.Interfaces;
using SolarWinds.Orion.Core.Models.MacroParsing;
using SolarWinds.Orion.Core.Models.OldDiscoveryModels;
using SolarWinds.Orion.Core.Models.Technology;
using SolarWinds.Orion.Core.Models.WebIntegration;
using SolarWinds.Orion.Core.Pollers.Node.ResponseTime;
using SolarWinds.Orion.Core.Pollers.Node.WMI;
using SolarWinds.Orion.Core.SharedCredentials;
using SolarWinds.Orion.Core.SharedCredentials.Credentials;
using SolarWinds.Orion.Core.Strings;
using SolarWinds.Orion.Discovery.Contract.DiscoveryPlugin;
using SolarWinds.Orion.Discovery.Contract.Models;
using SolarWinds.Orion.Discovery.Framework;
using SolarWinds.Orion.Discovery.Framework.Pluggability;
using SolarWinds.Orion.MacroProcessor;
using SolarWinds.Orion.Pollers.Framework.SNMP;
using SolarWinds.Orion.ServiceDirectory.Wcf;
using SolarWinds.Orion.Web.Integration;
using SolarWinds.Orion.Web.Integration.Common.Models;
using SolarWinds.Orion.Web.Integration.Maintenance;
using SolarWinds.Orion.Web.Integration.SupportCases;
using SolarWinds.Serialization.Json;
using SolarWinds.ServiceDirectory.Client.Contract;
using SolarWinds.Settings;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200000D RID: 13
	[ServiceBehavior(Name = "CoreServiceEngine", InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = false, AutomaticSessionShutdown = true, ConcurrencyMode = ConcurrencyMode.Multiple)]
	[ErrorBehavior(typeof(CoreErrorHandler))]
	[ServiceDirectory("Core.BusinessLayer")]
	public class CoreBusinessLayerService : ICoreBusinessLayer, IDisposable, IOneTimeAgentDiscoveryJobFactory, IServiceDirectoryIntegration
	{
		// Token: 0x17000014 RID: 20
		// (get) Token: 0x0600005E RID: 94 RVA: 0x00005A78 File Offset: 0x00003C78
		private CertificateMaintenance Maintenance
		{
			get
			{
				if (this.certificateMaintenance == null)
				{
					this.certificateMaintenance = (RegistrySettings.IsFullOrion() ? CertificateMaintenance.GetForFullMaintenance(BusinessLayerSettings.Instance.SafeCertificateMaintenanceTrialPeriod, BusinessLayerSettings.Instance.CertificateMaintenanceAgentPollFrequency) : CertificateMaintenance.GetForDbSyncOnly());
				}
				return this.certificateMaintenance;
			}
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00005AB5 File Offset: 0x00003CB5
		[Obsolete("Replacing MD5 certificates is not supported anymore - PRO-1041")]
		public void StartCertificateMaintenance()
		{
			this.Maintenance.StartCertificateMaintenance();
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00005AC2 File Offset: 0x00003CC2
		[Obsolete("Replacing MD5 certificates is not supported anymore - PRO-1041")]
		public void ApproveBreakingCertificateMaintenance()
		{
			this.Maintenance.ApproveBreakingCertificateMaintenance();
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00005ACF File Offset: 0x00003CCF
		[Obsolete("Replacing MD5 certificates is not supported anymore - PRO-1041")]
		public void RetryCertificateMaintenance()
		{
			this.RemoveCertificateMaintenanceNotification();
			this.Maintenance.RetryCertificateMaintenance();
		}

		// Token: 0x06000062 RID: 98 RVA: 0x00005AE2 File Offset: 0x00003CE2
		[Obsolete("Replacing MD5 certificates is not supported anymore - PRO-1041")]
		public CertificateUpdateBlockingInfo GetCertificateUpdateBlockingInfo()
		{
			return this.Maintenance.GetCertificateUpdateBlockingInfo();
		}

		// Token: 0x06000063 RID: 99 RVA: 0x00005AF0 File Offset: 0x00003CF0
		[Obsolete("Replacing MD5 certificates is not supported anymore - PRO-1041")]
		public CertificateMaintenanceResult GetCertificateMaintenanceStatus()
		{
			CertificateMaintenanceResult certificateMaintenanceStatus = this.Maintenance.GetCertificateMaintenanceStatus();
			if (certificateMaintenanceStatus == null || certificateMaintenanceStatus == 1)
			{
				this.RemoveCertificateMaintenanceNotification();
			}
			return certificateMaintenanceStatus;
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00005B18 File Offset: 0x00003D18
		[Obsolete("Replacing MD5 certificates is not supported anymore - PRO-1041")]
		public void RequestUserApprovalForCertificateMaintenance()
		{
			NotificationItem notificationItemById = this.GetNotificationItemById(this.ItemTypeCertificateUpdateRequired);
			if (notificationItemById == null)
			{
				CoreBusinessLayerService.log.Info("Creating notification to confirm certificate maintenance.");
				NotificationItem item = new NotificationItem(this.ItemTypeCertificateUpdateRequired, Resources2.LIBCODE_OM0_6, Resources2.LIBCODE_OM0_7, DateTime.UtcNow, false, this.ItemTypeCertificateUpdateRequired, "/Orion/CertificateMaintenanceConfirmation.aspx", null, null);
				this.InsertNotificationItem(item);
				return;
			}
			if (CoreBusinessLayerService.ShouldNotificationBeRestored(notificationItemById))
			{
				CoreBusinessLayerService.log.Info("Notification for certificate maintenance is acknowledged for long time. Unacknowledging it to remind user.");
				notificationItemById.SetNotAcknowledged();
				this.UpdateNotificationItem(notificationItemById);
				return;
			}
			CoreBusinessLayerService.log.Info("Notification for certificate maintenance already exists.");
		}

		// Token: 0x06000065 RID: 101 RVA: 0x00005BB1 File Offset: 0x00003DB1
		private void RemoveCertificateMaintenanceNotification()
		{
			this.DeleteNotificationItemById(this.ItemTypeCertificateUpdateRequired);
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00005BC0 File Offset: 0x00003DC0
		private static bool ShouldNotificationBeRestored(NotificationItem notificationItem)
		{
			TimeSpan certificateMaintenanceNotificationReappearPeriod = BusinessLayerSettings.Instance.CertificateMaintenanceNotificationReappearPeriod;
			return notificationItem.IsAcknowledged && DateTime.UtcNow - notificationItem.AcknowledgedAt > certificateMaintenanceNotificationReappearPeriod;
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00005C32 File Offset: 0x00003E32
		private static IActionRunner CreateActionRunner()
		{
			return new ActionRunner(new MEFActionPluginsProvider(false), CoreBusinessLayerService.CreateProxy());
		}

		// Token: 0x06000068 RID: 104 RVA: 0x00005C44 File Offset: 0x00003E44
		public ActionResult ExecuteAction(ActionDefinition actionDefinition, ActionContextBase context)
		{
			return this.actionRunner.Value.Execute(actionDefinition, context);
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00005C58 File Offset: 0x00003E58
		public string InvokeActionMethod(string actionTypeID, string methodName, string args)
		{
			return this._actionMethodInvoker.InvokeActionMethod(actionTypeID, methodName, args);
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00005C68 File Offset: 0x00003E68
		private static IInformationServiceProxyCreator CreateProxy()
		{
			return new SwisConnectionProxyCreator(() => new SwisConnectionProxyFactory(true).CreateConnection());
		}

		// Token: 0x0600006B RID: 107 RVA: 0x00005C8E File Offset: 0x00003E8E
		public string SimulateAction(ActionDefinition actionDefinition, ActionContextBase context)
		{
			return this.actionRunner.Value.Simulate(actionDefinition, context);
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00005CA4 File Offset: 0x00003EA4
		public int DeployAgent(AgentDeploymentSettings settings)
		{
			int result;
			try
			{
				CoreBusinessLayerService.log.InfoFormat("DeployAgent on {0}-{1} called", settings.IpAddress, settings.Hostname);
				result = new AgentDeployer().StartDeployingAgent(settings);
			}
			catch (Exception ex)
			{
				throw MessageUtilities.NewFaultException<CoreFaultContract>(ex);
			}
			return result;
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00005CF4 File Offset: 0x00003EF4
		public void DeployAgentPlugins(int agentId, IEnumerable<string> requiredPlugins)
		{
			this.DeployAgentPlugins(agentId, requiredPlugins, null);
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00005CFF File Offset: 0x00003EFF
		protected void DeployAgentPlugins(int agentId, IEnumerable<string> requiredPlugins, Action<AgentDeploymentStatus> onFinishedCallback)
		{
			CoreBusinessLayerService.log.InfoFormat("DeployAgentPlugins called, agentId:{0}, requiredPlugins:{1}", agentId, string.Join(",", requiredPlugins));
			new AgentDeployer().StartDeployingPlugins(agentId, requiredPlugins, onFinishedCallback);
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00005D2E File Offset: 0x00003F2E
		public string[] GetRequiredAgentDiscoveryPlugins()
		{
			return DiscoveryHelper.GetAgentDiscoveryPluginIds();
		}

		// Token: 0x06000070 RID: 112 RVA: 0x00005D35 File Offset: 0x00003F35
		public void DeployAgentDiscoveryPlugins(int agentId)
		{
			this.DeployAgentDiscoveryPluginsAsync(agentId);
		}

		// Token: 0x06000071 RID: 113 RVA: 0x00005D40 File Offset: 0x00003F40
		public Task<AgentDeploymentStatus> DeployAgentDiscoveryPluginsAsync(int agentId)
		{
			TaskCompletionSource<AgentDeploymentStatus> taskSource = new TaskCompletionSource<AgentDeploymentStatus>();
			string[] requiredAgentDiscoveryPlugins = this.GetRequiredAgentDiscoveryPlugins();
			this.DeployAgentPlugins(agentId, requiredAgentDiscoveryPlugins, delegate(AgentDeploymentStatus status)
			{
				taskSource.TrySetResult(status);
			});
			return taskSource.Task;
		}

		// Token: 0x06000072 RID: 114 RVA: 0x00005D84 File Offset: 0x00003F84
		public AgentInfo GetAgentInfo(int agentId)
		{
			return new AgentManager(this._agentInfoDal).GetAgentInfo(agentId);
		}

		// Token: 0x06000073 RID: 115 RVA: 0x00005D97 File Offset: 0x00003F97
		public AgentInfo GetAgentInfoByNodeId(int nodeId)
		{
			return new AgentManager(this._agentInfoDal).GetAgentInfoByNodeId(nodeId);
		}

		// Token: 0x06000074 RID: 116 RVA: 0x00005DAA File Offset: 0x00003FAA
		public AgentInfo DetectAgent(string ipAddress, string hostname)
		{
			return new AgentManager(this._agentInfoDal).DetectAgent(ipAddress, hostname);
		}

		// Token: 0x06000075 RID: 117 RVA: 0x00005DBE File Offset: 0x00003FBE
		public AgentDeploymentInfo GetAgentDeploymentInfo(int agentId)
		{
			return AgentDeploymentWatcher.GetInstance(this._agentInfoDal).GetAgentDeploymentInfo(agentId);
		}

		// Token: 0x06000076 RID: 118 RVA: 0x00005DD1 File Offset: 0x00003FD1
		public void UpdateAgentNodeId(int agentId, int nodeId)
		{
			new AgentManager(this._agentInfoDal).UpdateAgentNodeId(agentId, nodeId);
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00005DE5 File Offset: 0x00003FE5
		public void ResetAgentNodeId(int nodeId)
		{
			new AgentManager(this._agentInfoDal).ResetAgentNodeId(nodeId);
		}

		// Token: 0x06000078 RID: 120 RVA: 0x00005DF8 File Offset: 0x00003FF8
		private void UpdateNotification()
		{
			CoreBusinessLayerService.log.Debug("Agent deployed, update notification item");
		}

		// Token: 0x06000079 RID: 121 RVA: 0x00005E09 File Offset: 0x00004009
		public List<KeyValuePair<string, string>> GetAlertList()
		{
			return AlertDAL.GetAlertList();
		}

		// Token: 0x0600007A RID: 122 RVA: 0x00005E10 File Offset: 0x00004010
		[Obsolete("Old alerting will be removed. Use GetAlertList() method instead.")]
		public List<KeyValuePair<string, string>> GetAlertNames(bool includeBasic)
		{
			return AlertDAL.GetAlertList(includeBasic);
		}

		// Token: 0x0600007B RID: 123 RVA: 0x00005E18 File Offset: 0x00004018
		public List<NetObjectType> GetAlertNetObjectTypes()
		{
			return ModuleAlertsMap.NetObjectTypes.Items;
		}

		// Token: 0x0600007C RID: 124 RVA: 0x00005E24 File Offset: 0x00004024
		[Obsolete("Method does not return V2 alerts.")]
		public DataTable GetSortableAlertTable(string netObject, string deviceType, string alertID, string orderByClause, int maxRecords, bool showAcknowledged, List<int> limitationIDs, bool includeBasic)
		{
			return AlertDAL.GetSortableAlertTable(netObject, deviceType, alertID, orderByClause, maxRecords, showAcknowledged, limitationIDs, includeBasic);
		}

		// Token: 0x0600007D RID: 125 RVA: 0x00005E38 File Offset: 0x00004038
		public List<ActiveAlertDetailed> GetAlertTableByDate(DateTime date, int? lastAlertHistoryId, List<int> limitationIDs, bool showAcknowledged)
		{
			return new ActiveAlertDAL().GetAlertTableByDate(date.ToLocalTime(), lastAlertHistoryId, limitationIDs);
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00005E4D File Offset: 0x0000404D
		public int GetLastAlertHistoryId()
		{
			return new AlertHistoryDAL().GetLastHystoryId();
		}

		// Token: 0x0600007F RID: 127 RVA: 0x00005E59 File Offset: 0x00004059
		[Obsolete("Method does not return V2 alerts.")]
		public DataTable GetPageableAlerts(List<int> limitationIDs, string period, int fromRow, int toRow, string type, string alertId, bool showAcknAlerts)
		{
			return AlertDAL.GetPageableAlerts(limitationIDs, period, fromRow, toRow, type, alertId, showAcknAlerts);
		}

		// Token: 0x06000080 RID: 128 RVA: 0x00005E6B File Offset: 0x0000406B
		[Obsolete("Method does not return V2 alerts.")]
		public DataTable GetAlertTable(string netObject, string deviceType, string alertID, int maxRecords, bool showAcknowledged, List<int> limitationIDs)
		{
			return AlertDAL.GetAlertTable(netObject, deviceType, alertID, maxRecords, showAcknowledged, limitationIDs);
		}

		// Token: 0x06000081 RID: 129 RVA: 0x00005E7B File Offset: 0x0000407B
		[Obsolete("Method does not return V2 alerts.")]
		public DataTable GetAlerts(string netObject, string deviceType, string alertID, int maxRecords, bool showAcknowledged, List<int> limitationIDs, bool includeBasic)
		{
			return AlertDAL.GetAlertTable(netObject, deviceType, alertID, maxRecords, showAcknowledged, limitationIDs, includeBasic);
		}

		// Token: 0x06000082 RID: 130 RVA: 0x00005E8D File Offset: 0x0000408D
		[Obsolete("Old alerting will be removed")]
		public void AcknowledgeAlertsAction(List<string> alertKeys, string accountID)
		{
			AlertDAL.AcknowledgeAlertsAction(alertKeys, accountID);
			this.FireUpdateNotification(alertKeys, AlertUpdatedIndicationType.Acknowledged, accountID);
		}

		// Token: 0x06000083 RID: 131 RVA: 0x00005E9F File Offset: 0x0000409F
		[Obsolete("Old alerting will be removed")]
		public void AcknowledgeAlertsFromAlertManager(List<string> alertKeys, string accountID)
		{
			AlertDAL.AcknowledgeAlertsAction(alertKeys, accountID, AlertAcknowledgeType.AlertManager, null);
			this.FireUpdateNotification(alertKeys, AlertUpdatedIndicationType.Acknowledged, accountID);
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00005EB3 File Offset: 0x000040B3
		[Obsolete("Old alerting will be removed")]
		public void UnacknowledgeAlertsFromAlertManager(List<string> alertKeys, string accountID)
		{
			AlertDAL.UnacknowledgeAlertsAction(alertKeys, accountID, AlertAcknowledgeType.AlertManager);
			this.FireUpdateNotification(alertKeys, AlertUpdatedIndicationType.Acknowledged, accountID);
		}

		// Token: 0x06000085 RID: 133 RVA: 0x00005EC6 File Offset: 0x000040C6
		[Obsolete("Old alerting will be removed")]
		public void AcknowledgeAlerts(List<string> alertKeys, string accountID, bool viaEmail)
		{
			AlertDAL.AcknowledgeAlertsAction(alertKeys, accountID, viaEmail);
			this.FireUpdateNotification(alertKeys, AlertUpdatedIndicationType.Acknowledged, accountID);
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00005ED9 File Offset: 0x000040D9
		[Obsolete("Old alerting will be removed")]
		public void AcknowledgeAlertsWithNotes(List<string> alertKeys, string accountID, string notes)
		{
			AlertDAL.AcknowledgeAlertsAction(alertKeys, accountID, false, notes);
			this.FireUpdateNotification(alertKeys, AlertUpdatedIndicationType.Acknowledged | AlertUpdatedIndicationType.NoteChanged, accountID);
		}

		// Token: 0x06000087 RID: 135 RVA: 0x00005EED File Offset: 0x000040ED
		public int GetAlertObjectId(string alertkey)
		{
			return AlertDAL.GetAlertObjectId(alertkey);
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00005EF8 File Offset: 0x000040F8
		public int AcknowledgeAlertsWithMethod(List<string> alertKeys, string accountId, string notes, string method)
		{
			new List<string>();
			List<int> list = new List<int>();
			foreach (string text in alertKeys)
			{
				string empty = string.Empty;
				string text2 = string.Empty;
				string empty2 = string.Empty;
				if (AlertsHelper.TryParseAlertKey(text.Replace("swis://", "swis//"), out empty, out text2, out empty2))
				{
					text2 = text2.Replace("swis//", "swis://");
					if (!text2.StartsWith("swis://"))
					{
						CoreBusinessLayerService.log.WarnFormat("Unable to acknowledge alert {0} for net object {1}. Old alerts aren't supported.", text, text2);
					}
					else
					{
						int alertObjectId = this.GetAlertObjectId(empty, text2, empty2);
						if (alertObjectId > 0)
						{
							list.Add(alertObjectId);
						}
					}
				}
			}
			int num = 0;
			if (list.Any<int>())
			{
				num += this.AcknowledgeAlertsWithMethodV2(list, accountId, notes, DateTime.UtcNow, method);
			}
			return num;
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00005FF4 File Offset: 0x000041F4
		public int AcknowledgeAlertsV2(IEnumerable<int> alertObjectIds, string accountId, string notes, DateTime acknowledgeDateTime)
		{
			return this.AcknowledgeAlertsWithMethodV2(alertObjectIds, accountId, notes, acknowledgeDateTime, null);
		}

		// Token: 0x0600008A RID: 138 RVA: 0x00006004 File Offset: 0x00004204
		public int AcknowledgeAlertsWithMethodV2(IEnumerable<int> alertObjectIds, string accountId, string notes, DateTime acknowledgeDateTime, string method)
		{
			ActiveAlertDAL activeAlertDAL = new ActiveAlertDAL();
			IEnumerable<int> alertObjectIds2 = activeAlertDAL.LimitAlertAckStateUpdateCandidates(alertObjectIds, true);
			List<IIndication> list = new List<IIndication>();
			list.AddRange(activeAlertDAL.GetAlertUpdatedIndicationPropertiesByAlertObjectIds(alertObjectIds2, accountId, notes, acknowledgeDateTime, true, method));
			DataTable alertResetOrUpdateIndicationPropertiesTableByAlertObjectIds = activeAlertDAL.GetAlertResetOrUpdateIndicationPropertiesTableByAlertObjectIds(alertObjectIds);
			foreach (int num in alertObjectIds)
			{
				DataRow[] array = alertResetOrUpdateIndicationPropertiesTableByAlertObjectIds.Select("AlertObjectID=" + num);
				PropertyBag propertyBag = new PropertyBag();
				if (array.Length != 0)
				{
					propertyBag.Add("Acknowledged", (array[0]["Acknowledged"] != DBNull.Value) ? Convert.ToString(array[0]["Acknowledged"]) : "False");
					propertyBag.Add("AcknowledgedBy", (array[0]["AcknowledgedBy"] != DBNull.Value) ? Convert.ToString(array[0]["AcknowledgedBy"]) : string.Empty);
					propertyBag.Add("AcknowledgedDateTime", (array[0]["AcknowledgedDateTime"] != DBNull.Value) ? Convert.ToString(array[0]["AcknowledgedDateTime"]) : string.Empty);
					propertyBag.Add("AlertNote", (array[0]["AlertNote"] != DBNull.Value) ? Convert.ToString(array[0]["AlertNote"]) : string.Empty);
				}
				List<IIndication> list2 = list;
				IndicationType indicationType = 2;
				PropertyBag propertyBag2 = new PropertyBag();
				propertyBag2.Add("AlertObjectID", num);
				propertyBag2.Add("Acknowledged", "True");
				propertyBag2.Add("AcknowledgedBy", accountId);
				propertyBag2.Add("AcknowledgedDateTime", acknowledgeDateTime);
				propertyBag2.Add("AlertNote", notes);
				propertyBag2.Add("PreviousProperties", propertyBag);
				propertyBag2.Add("InstanceType", "Orion.AlertActive");
				list2.Add(new CommonIndication(indicationType, propertyBag2));
			}
			int num2 = activeAlertDAL.AcknowledgeActiveAlerts(alertObjectIds2, accountId, notes, acknowledgeDateTime);
			if (num2 > 0)
			{
				IndicationPublisher.CreateV3().ReportIndications(list);
			}
			return num2;
		}

		// Token: 0x0600008B RID: 139 RVA: 0x00006238 File Offset: 0x00004438
		public AlertAcknowledgeResult AcknowledgeAlert(string alertId, string netObjectId, string objectType, string accountId, string notes, string method)
		{
			AlertAcknowledgeResult result = AlertAcknowledgeResult.Acknowledged;
			if (!netObjectId.StartsWith("swis://"))
			{
				CoreBusinessLayerService.log.WarnFormat("Unable to acknowledge alert {0} for net object {1}. Old alerts aren't supported.", alertId, netObjectId);
			}
			else
			{
				int alertObjectId = this.GetAlertObjectId(alertId, netObjectId, objectType);
				if (alertObjectId > 0)
				{
					result = ((this.AcknowledgeAlertsV2(new List<int>
					{
						alertObjectId
					}, accountId, notes, DateTime.UtcNow) == 1) ? AlertAcknowledgeResult.Acknowledged : AlertAcknowledgeResult.AlertNotTriggered);
				}
			}
			return result;
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00006299 File Offset: 0x00004499
		[Obsolete("Old alerting will be removed", true)]
		public void ClearTriggeredAlerts(List<string> alertKeys)
		{
			this.FireResetNotification(alertKeys, IndicationConstants.SystemAccountId);
			AlertDAL.ClearTriggeredAlert(alertKeys);
		}

		// Token: 0x0600008D RID: 141 RVA: 0x000062B0 File Offset: 0x000044B0
		public void ClearTriggeredAlertsV2(IEnumerable<int> alertObjectIds, string accountId)
		{
			ActiveAlertDAL activeAlertDAL = new ActiveAlertDAL();
			IEnumerable<AlertClearedIndicationProperties> alertClearedIndicationPropertiesByAlertObjectIds = activeAlertDAL.GetAlertClearedIndicationPropertiesByAlertObjectIds(alertObjectIds);
			List<IIndication> list = new List<IIndication>();
			foreach (AlertClearedIndicationProperties properties in alertClearedIndicationPropertiesByAlertObjectIds)
			{
				AlertClearedIndication item = new AlertClearedIndication((!string.IsNullOrEmpty(accountId)) ? accountId : IndicationConstants.SystemAccountId, properties);
				list.Add(item);
			}
			foreach (int alertObjectId in alertObjectIds)
			{
				AlertDeletedIndicationProperties alertDeletedIndicationProperties = new AlertDeletedIndicationProperties
				{
					AlertObjectId = alertObjectId
				};
				CommonIndication item2 = new CommonIndication(1, alertDeletedIndicationProperties.CreatePropertyBag());
				list.Add(item2);
			}
			activeAlertDAL.ClearTriggeredActiveAlerts(alertObjectIds, accountId);
			IndicationPublisher.CreateV3().ReportIndications(list);
		}

		// Token: 0x0600008E RID: 142 RVA: 0x00006394 File Offset: 0x00004594
		[Obsolete("Old alerting will be removed")]
		public int EnableAdvancedAlert(Guid alertDefID, bool enable)
		{
			return AlertDAL.EnableAdvancedAlert(alertDefID, enable);
		}

		// Token: 0x0600008F RID: 143 RVA: 0x0000639D File Offset: 0x0000459D
		[Obsolete("Old alerting will be removed")]
		public int EnableAdvancedAlerts(List<string> alertDefIDs, bool enable, bool enableAll)
		{
			return AlertDAL.EnableAdvancedAlerts(alertDefIDs, enable, enableAll);
		}

		// Token: 0x06000090 RID: 144 RVA: 0x000063A7 File Offset: 0x000045A7
		[Obsolete("Old alerting will be removed")]
		public int RemoveAdvancedAlert(Guid alertDefID)
		{
			return AlertDAL.RemoveAdvancedAlert(alertDefID);
		}

		// Token: 0x06000091 RID: 145 RVA: 0x000063AF File Offset: 0x000045AF
		public int RemoveAdvancedAlerts(List<string> alertDefIDs, bool deleteAll)
		{
			return AlertDAL.RemoveAdvancedAlerts(alertDefIDs, deleteAll);
		}

		// Token: 0x06000092 RID: 146 RVA: 0x000063B8 File Offset: 0x000045B8
		[Obsolete("Old alerting will be removed")]
		public int UpdateAlertDef(Guid alertDefID, string alertName, string alertDescr, bool enabled, int evInterval, string dow, DateTime startTime, DateTime endTime, bool ignoreTimeout)
		{
			return AlertDAL.UpdateAlertDef(alertDefID, alertName, alertDescr, enabled, evInterval, dow, startTime, endTime, ignoreTimeout);
		}

		// Token: 0x06000093 RID: 147 RVA: 0x000063D9 File Offset: 0x000045D9
		[Obsolete("Old alerting will be removed")]
		public DataTable GetAdvancedAlerts()
		{
			return AlertDAL.GetAdvancedAlerts();
		}

		// Token: 0x06000094 RID: 148 RVA: 0x000063E0 File Offset: 0x000045E0
		[Obsolete("Old alerting will be removed")]
		public DataTable GetPagebleAdvancedAlerts(string sortColumn, string sortDirection, int startRowNumber, int pageSize)
		{
			return AlertDAL.GetPagebleAdvancedAlerts(sortColumn, sortDirection, startRowNumber, pageSize);
		}

		// Token: 0x06000095 RID: 149 RVA: 0x000063EC File Offset: 0x000045EC
		public ActiveAlertPage GetPageableActiveAlerts(PageableActiveAlertRequest pageableRequest, ActiveAlertsRequest activeAlertsRequest = null)
		{
			return AlertDAL.GetPageableActiveAlerts(pageableRequest, activeAlertsRequest);
		}

		// Token: 0x06000096 RID: 150 RVA: 0x000063F5 File Offset: 0x000045F5
		public ActiveAlertObjectPage GetPageableActiveAlertObjects(PageableActiveAlertObjectRequest request)
		{
			return new ActiveAlertDAL().GetPageableActiveAlertObjects(request);
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00006402 File Offset: 0x00004602
		public ActiveAlert GetActiveAlert(ActiveAlertUniqueidentifier activeAlertUniqIdentifier, IEnumerable<int> limitationIDs)
		{
			return new ActiveAlertDAL().GetActiveAlert(activeAlertUniqIdentifier.AlertObjectID, limitationIDs, true);
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00006416 File Offset: 0x00004616
		public AlertHistoryPage GetActiveAlertHistory(int alertObjectId, PageableActiveAlertRequest request)
		{
			return new AlertHistoryDAL().GetActiveAlertHistory(alertObjectId, request);
		}

		// Token: 0x06000099 RID: 153 RVA: 0x00006424 File Offset: 0x00004624
		[Obsolete("Old alerting will be removed")]
		public int AdvAlertsCount()
		{
			return AlertDAL.AdvAlertsCount();
		}

		// Token: 0x0600009A RID: 154 RVA: 0x0000642B File Offset: 0x0000462B
		[Obsolete("Old alerting will be removed")]
		public DataTable GetAdvancedAlert(Guid alertDefID)
		{
			return AlertDAL.GetAdvancedAlert(alertDefID);
		}

		// Token: 0x0600009B RID: 155 RVA: 0x00006434 File Offset: 0x00004634
		private int GetAlertObjectId(string alertDefId, string activeObject, string objectType)
		{
			int result = 0;
			string empty = string.Empty;
			this.GetAlertObjectIdAndAlertNote(alertDefId, activeObject, objectType, out result, out empty);
			return result;
		}

		// Token: 0x0600009C RID: 156 RVA: 0x00006458 File Offset: 0x00004658
		private void GetAlertObjectIdAndAlertNote(string alertDefId, string activeObject, string objectType, out int objectId, out string note)
		{
			objectId = 0;
			note = string.Empty;
			string text = "SELECT AO.AlertObjectID, AO.AlertNote FROM Orion.AlertObjects AO \r\n                                    INNER JOIN Orion.AlertConfigurations AC ON AO.AlertID=AC.AlertID\r\n                                    WHERE EntityUri=@entityUri AND EntityType=@objectType AND AC.AlertRefID=@alertDefId";
			using (IInformationServiceProxy2 informationServiceProxy = SwisConnectionProxyPool.GetCreator().Create())
			{
				DataTable dataTable = informationServiceProxy.Query(text, new Dictionary<string, object>
				{
					{
						"entityUri",
						activeObject
					},
					{
						"objectType",
						objectType
					},
					{
						"alertDefId",
						alertDefId
					}
				});
				if (dataTable.Rows.Count > 0)
				{
					objectId = ((dataTable.Rows[0]["AlertObjectID"] != DBNull.Value) ? Convert.ToInt32(dataTable.Rows[0]["AlertObjectID"]) : 0);
					note = ((dataTable.Rows[0]["AlertNote"] != DBNull.Value) ? Convert.ToString(dataTable.Rows[0]["AlertNote"]) : string.Empty);
				}
			}
		}

		// Token: 0x0600009D RID: 157 RVA: 0x00006560 File Offset: 0x00004760
		public int AppendNoteToAlert(string alertDefId, string activeObject, string objectType, string note)
		{
			string accountId = AuditMessageInspector.UserContext ?? IndicationConstants.SystemAccountId;
			int result = 0;
			if (!activeObject.StartsWith("swis://"))
			{
				CoreBusinessLayerService.log.WarnFormat("Unable to append Note to alert {0}. Old alerts aren't supported.", activeObject);
			}
			else
			{
				int num = 0;
				string empty = string.Empty;
				this.GetAlertObjectIdAndAlertNote(alertDefId, activeObject, objectType, out num, out empty);
				if (num > 0)
				{
					result = (this.SetAlertNote(num, accountId, note, DateTime.UtcNow, empty) ? 1 : 0);
				}
			}
			return result;
		}

		// Token: 0x0600009E RID: 158 RVA: 0x000065D0 File Offset: 0x000047D0
		[Obsolete("Old alerting will be removed")]
		public int UpdateAdvancedAlertNote(string alerfDefID, string activeObject, string objectType, string notes)
		{
			int result = AlertDAL.UpdateAdvancedAlertNote(alerfDefID, activeObject, objectType, notes);
			this.FireUpdateNotification(new string[]
			{
				AlertsHelper.GetAlertKey(alerfDefID, activeObject, objectType)
			}, AlertUpdatedIndicationType.NoteChanged, IndicationConstants.SystemAccountId);
			return result;
		}

		// Token: 0x0600009F RID: 159 RVA: 0x00006606 File Offset: 0x00004806
		[Obsolete("Old alerting will be removed")]
		public AlertNotificationSettings GetAlertNotificationSettings(string alertDefID, string netObjectType, string alertName)
		{
			return AlertDAL.GetAlertNotificationSettings(alertDefID, netObjectType, alertName);
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x00006610 File Offset: 0x00004810
		[Obsolete("Old alerting will be removed")]
		public void SetAlertNotificationSettings(string alertDefID, AlertNotificationSettings settings)
		{
			AlertDAL.SetAlertNotificationSettings(alertDefID, settings);
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x00006619 File Offset: 0x00004819
		[Obsolete("Old alerting will be removed")]
		public AlertNotificationSettings GetBasicAlertNotificationSettings(int alertID, string netObjectType, int propertyID, string alertName)
		{
			return AlertDAL.GetBasicAlertNotificationSettings(alertID, netObjectType, propertyID, alertName);
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00006625 File Offset: 0x00004825
		public void SetBasicAlertNotificationSettings(int alertID, AlertNotificationSettings settings)
		{
			AlertDAL.SetBasicAlertNotificationSettings(alertID, settings);
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x00006630 File Offset: 0x00004830
		[Obsolete("Old alerting will be removed", true)]
		private void FireUpdateNotification(IEnumerable<string> alertKeys, AlertUpdatedIndicationType type, string accountId)
		{
			this.FireNotification<AlertUpdatedIndicationProperties, AlertUpdatedIndication>(alertKeys, accountId, "Alert Update", delegate(AlertNotificationDetails notificationDetails, AlertUpdatedIndicationProperties indicationProperties)
			{
				indicationProperties.Type = type;
				indicationProperties.Acknowledged = notificationDetails.Acknowledged;
				indicationProperties.AcknowledgedBy = notificationDetails.AcknowledgedBy;
				indicationProperties.AcknowledgedMethod = notificationDetails.AcknowledgedMethod;
				indicationProperties.Notes = notificationDetails.Notes;
			});
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x00006663 File Offset: 0x00004863
		[Obsolete("Old alerting will be removed", true)]
		private void FireResetNotification(IEnumerable<string> alertKeys, string accountId)
		{
			this.FireNotification<AlertResetIndicationProperties, AlertResetIndication>(alertKeys, accountId, "Alert Reset", delegate(AlertNotificationDetails notificationDetails, AlertResetIndicationProperties indicationProperties)
			{
				indicationProperties.ResetTime = DateTime.UtcNow;
			});
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x00006694 File Offset: 0x00004894
		[Obsolete("Old alerting will be removed")]
		private void FireNotification<TProperties, TIndication>(IEnumerable<string> alertKeys, string accountId, string name, Action<AlertNotificationDetails, TProperties> customIndicationPropertiesHandler) where TProperties : AlertIndicationProperties, new() where TIndication : AlertIndication
		{
			CoreBusinessLayerService.log.DebugFormat("Firing {0} notifications", name);
			MacroParser macroParser = new MacroParser(new Action<string, int>(BusinessLayerOrionEvent.WriteEvent));
			using (macroParser.MyDBConnection = DatabaseFunctions.CreateConnection(false))
			{
				Func<string, string> <>9__0;
				foreach (string text in alertKeys)
				{
					try
					{
						string alertDefID;
						string activeObject;
						string objectType;
						if (!AlertsHelper.TryParseAlertKey(text, out alertDefID, out activeObject, out objectType))
						{
							CoreBusinessLayerService.log.WarnFormat("Error firing notification for {0} because of invalid alert key {1}", name, text);
						}
						else
						{
							AlertNotificationDetails alertDetailsForNotification = AlertDAL.GetAlertDetailsForNotification(alertDefID, activeObject, objectType);
							if (alertDetailsForNotification != null && alertDetailsForNotification.NotificationSettings.Enabled)
							{
								macroParser.ObjectType = alertDetailsForNotification.ObjectType;
								macroParser.ActiveObject = alertDetailsForNotification.ActiveObject;
								macroParser.ObjectName = alertDetailsForNotification.ObjectName;
								macroParser.AlertID = new Guid(alertDetailsForNotification.AlertDefinitionId);
								macroParser.AlertName = alertDetailsForNotification.AlertName;
								macroParser.AlertMessage = alertDetailsForNotification.AlertMessage;
								macroParser.AlertTriggerTime = alertDetailsForNotification.TriggerTimeStamp.ToLocalTime();
								macroParser.AlertTriggerCount = alertDetailsForNotification.TriggerCount;
								macroParser.Acknowledged = alertDetailsForNotification.Acknowledged;
								macroParser.AcknowledgedBy = alertDetailsForNotification.AcknowledgedBy;
								macroParser.AcknowledgedTime = alertDetailsForNotification.AcknowledgedTime.ToLocalTime();
								AlertNotificationSettingsProvider alertNotificationSettingsProvider = new AlertNotificationSettingsProvider();
								Func<string, string> macroParser2;
								if ((macroParser2 = <>9__0) == null)
								{
									macroParser2 = (<>9__0 = ((string s) => macroParser.ParseMacros(s, false)));
								}
								TProperties alertIndicationProperties = alertNotificationSettingsProvider.GetAlertIndicationProperties<TProperties>(macroParser2, alertDetailsForNotification.ActiveObject, alertDetailsForNotification.ObjectType, alertDetailsForNotification.ObjectName, new Guid(alertDetailsForNotification.AlertDefinitionId), alertDetailsForNotification.AlertName, alertDetailsForNotification.TriggerTimeStamp, alertDetailsForNotification.NotificationSettings);
								customIndicationPropertiesHandler(alertDetailsForNotification, alertIndicationProperties);
								IndicationPublisher.CreateV3().ReportIndication((AlertIndication)Activator.CreateInstance(typeof(TIndication), new object[]
								{
									accountId,
									alertIndicationProperties
								}));
							}
						}
					}
					catch (Exception ex)
					{
						CoreBusinessLayerService.log.Error(string.Format("Error firing {0} notification", name), ex);
					}
				}
			}
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x00006948 File Offset: 0x00004B48
		public IEnumerable<AlertScopeItem> GetObjectsInAlertScope(int[] alertIds)
		{
			List<AlertScopeItem> list = new List<AlertScopeItem>();
			ISwisConnectionProxyCreator creator = SwisConnectionProxyPool.GetCreator();
			IAlertDefinitionsDAL alertDefinitionsDAL = AlertDefinitionsDAL.Create(ConditionTypeProvider.Create(creator), creator);
			foreach (int num in alertIds)
			{
				if (!alertDefinitionsDAL.Exist(num))
				{
					if (CoreBusinessLayerService.log.IsDebugEnabled)
					{
						CoreBusinessLayerService.log.DebugFormat("There is no AlertDefinition with AlertId={0}", num);
					}
				}
				else
				{
					AlertDefinition alertDefinition = alertDefinitionsDAL.Get(num);
					IConditionEntityScope conditionEntityScope = alertDefinition.Trigger.Conditions[0].Type as IConditionEntityScope;
					if (conditionEntityScope != null)
					{
						IEnumerable<EntityInstance> enumerable = conditionEntityScope.GetScope(alertDefinition.Trigger.Conditions[0].Condition, alertDefinition.Trigger.Conditions[0].ObjectType).ToList<EntityInstance>();
						if (enumerable.Any<EntityInstance>())
						{
							string entityType = null;
							Entity entityByObjectType = alertDefinition.Trigger.Conditions[0].Type.EntityProvider.GetEntityByObjectType(alertDefinition.Trigger.Conditions[0].ObjectType);
							if (entityByObjectType != null)
							{
								entityType = entityByObjectType.FullName;
							}
							foreach (EntityInstance entityInstance in enumerable)
							{
								list.Add(new AlertScopeItem
								{
									InstanceName = entityInstance.DisplayName,
									ObjectId = entityInstance.Uri,
									AlertId = num,
									EntityType = entityType
								});
							}
						}
					}
				}
			}
			return list;
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x00006AF4 File Offset: 0x00004CF4
		public IEnumerable<AlertScopeItem> GetAllAlertsInObjectScopeWithParams(string entityType, string[] objectIds, bool loadAction, bool loadSchedules)
		{
			List<AlertScopeItem> list = new List<AlertScopeItem>();
			ISwisConnectionProxyCreator creator = SwisConnectionProxyPool.GetCreator();
			IEnumerable<AlertScopeItem> alertsWhichCanAffectObject = this.GetAlertsWhichCanAffectObject(entityType, creator, loadAction, loadSchedules);
			List<AlertScopeItem> list2 = new List<AlertScopeItem>();
			int num = 0;
			foreach (AlertScopeItem alertScopeItem in alertsWhichCanAffectObject)
			{
				bool flag = false;
				if (alertScopeItem.ScopeQuery == null || alertScopeItem.ScopeQuery.Params == null || alertScopeItem.ScopeQuery.Params.Count == 0)
				{
					list2.Add(alertScopeItem);
					flag = true;
				}
				else if (alertScopeItem.ScopeQuery.Params.Count + num < 2000)
				{
					list2.Add(alertScopeItem);
					num += alertScopeItem.ScopeQuery.Params.Count;
					flag = true;
				}
				if (!flag)
				{
					this.GetAlertsForTheBulk(list2, entityType, objectIds, creator, list);
					list2.Clear();
					list2.Add(alertScopeItem);
					num = alertScopeItem.ScopeQuery.Params.Count;
				}
			}
			this.GetAlertsForTheBulk(list2, entityType, objectIds, creator, list);
			return list;
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x00006C10 File Offset: 0x00004E10
		public IEnumerable<AlertScopeItem> GetAllAlertsInObjectScope(string entityType, string[] objectIds)
		{
			return this.GetAllAlertsInObjectScopeWithParams(entityType, objectIds, true, true);
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x00006C1C File Offset: 0x00004E1C
		private void GetAlertsForTheBulk(List<AlertScopeItem> alertScopeItems, string entityType, string[] objectIds, ISwisConnectionProxyCreator swisCreator, List<AlertScopeItem> resultItems)
		{
			if (alertScopeItems == null || alertScopeItems.Count == 0)
			{
				return;
			}
			try
			{
				List<AlertScopeItem> alertsForQueries = this.GetAlertsForQueries(alertScopeItems, entityType, objectIds, swisCreator);
				if (alertsForQueries != null && alertsForQueries.Count > 0)
				{
					foreach (AlertScopeItem item in alertsForQueries)
					{
						resultItems.Add(item);
					}
				}
			}
			catch (Exception)
			{
				CoreBusinessLayerService.log.ErrorFormat("Error occurred during validating alert scope queries for {0} type {1} objects and {2} AlertScope elements", entityType, objectIds.Length, alertScopeItems.Count);
			}
		}

		// Token: 0x060000AA RID: 170 RVA: 0x00006CC4 File Offset: 0x00004EC4
		private List<AlertScopeItem> GetAlertsForQueries(IEnumerable<AlertScopeItem> alertScopeItems, string entityType, string[] objectIds, ISwisConnectionProxyCreator swisCreator)
		{
			List<AlertScopeItem> list = new List<AlertScopeItem>();
			Tuple<string, IDictionary<string, object>> tuple = this.PrepareQueryForAlerts(entityType, alertScopeItems, objectIds);
			if (!string.IsNullOrEmpty(tuple.Item1))
			{
				using (IInformationServiceProxy2 informationServiceProxy = swisCreator.Create())
				{
					DataTable dataTable = informationServiceProxy.Query(tuple.Item1, tuple.Item2);
					if (dataTable != null && dataTable.Rows.Count > 0)
					{
						Dictionary<int, string> dictionary = alertScopeItems.ToDictionary((AlertScopeItem p) => p.AlertId, (AlertScopeItem q) => q.InstanceName);
						foreach (object obj in dataTable.Rows)
						{
							DataRow dataRow = (DataRow)obj;
							int num = Convert.ToInt32(dataRow["AlertId"]);
							string instanceName = dictionary[num];
							int alertId = num;
							string objectId = dataRow["ObjectId"].ToString();
							list.Add(new AlertScopeItem
							{
								AlertId = alertId,
								EntityType = entityType,
								InstanceName = instanceName,
								ObjectId = objectId
							});
						}
					}
				}
			}
			return list;
		}

		// Token: 0x060000AB RID: 171 RVA: 0x00006E44 File Offset: 0x00005044
		private Tuple<string, IDictionary<string, object>> PrepareQueryForAlerts(string entityType, IEnumerable<AlertScopeItem> alertScopeItems, string[] objectIds)
		{
			string text = string.Join(",", from p in objectIds
			select "'" + p + "'");
			if (string.IsNullOrEmpty("Uri"))
			{
				throw new InvalidInputException(string.Format("Orion.Alert can't recognize {0} entity, check Orion.NetObjectTypesExt if exists", entityType));
			}
			int num = 1;
			IDictionary<string, object> dictionary = new Dictionary<string, object>();
			StringBuilder stringBuilder = new StringBuilder();
			foreach (AlertScopeItem alertScopeItem in alertScopeItems)
			{
				string text2 = string.Format("SELECT {0} AS AlertId, '{1}' AS ObjectId FROM {2} AS E0 WHERE E0.{3} IN ({4})", new object[]
				{
					alertScopeItem.AlertId,
					objectIds[0],
					entityType,
					"Uri",
					text
				});
				if (string.IsNullOrEmpty(alertScopeItem.ScopeQuery.Query))
				{
					CoreBusinessLayerService.log.Warn("Object scope can be evaluated because ScopeQuery (query for evaluation) is not provided (null)");
				}
				else
				{
					int num2 = alertScopeItem.ScopeQuery.Query.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
					if (num2 < 0)
					{
						bool flag = stringBuilder.Length == 0;
						if (!flag)
						{
							stringBuilder.AppendLine("UNION (");
						}
						stringBuilder.AppendLine(text2);
						if (!flag)
						{
							stringBuilder.AppendLine(")");
						}
					}
					else
					{
						string text3 = alertScopeItem.ScopeQuery.Query.Substring(num2 + 5).Trim();
						text3 = Regex.Replace(text3, "\\s+", " ") + " ";
						foreach (string text4 in alertScopeItem.ScopeQuery.Params.Keys)
						{
							string oldValue = string.Format("@{0} ", text4);
							string newValue = string.Format("@{0}no{1} ", text4, num);
							text3 = text3.Replace(oldValue, newValue);
							dictionary.Add(text4 + "no" + num, alertScopeItem.ScopeQuery.Params[text4]);
						}
						bool flag2 = stringBuilder.Length == 0;
						if (!flag2)
						{
							stringBuilder.AppendLine("UNION (");
						}
						stringBuilder.AppendLine(string.Format("{0} AND {1}", text2, text3));
						num++;
						if (!flag2)
						{
							stringBuilder.AppendLine(")");
						}
					}
				}
			}
			string text5 = stringBuilder.ToString();
			if (CoreBusinessLayerService.log.IsDebugEnabled)
			{
				CoreBusinessLayerService.log.DebugFormat("Provided query to evaluation of alert scope: {0}", text5);
			}
			return Tuple.Create<string, IDictionary<string, object>>(text5, dictionary);
		}

		// Token: 0x060000AC RID: 172 RVA: 0x00007100 File Offset: 0x00005300
		private IEnumerable<AlertScopeItem> GetAlertsWhichCanAffectObject(string entityType, IInformationServiceProxyCreator swisCreator, bool loadActions, bool loadSchedules)
		{
			List<AlertScopeItem> results = new List<AlertScopeItem>();
			ConditionTypeProvider conditionTypeProvider = ConditionTypeProvider.Create(swisCreator);
			IAlertDefinitionsDAL alertDefinitionsDAL = AlertDefinitionsDAL.Create(conditionTypeProvider, swisCreator);
			ConditionTypeDynamic conditionTypeDynamic = (ConditionTypeDynamic)conditionTypeProvider.Get("Core.Dynamic");
			string objectType = conditionTypeDynamic.EntityProvider.GetObjectTypeByEntityFullName(entityType);
			IEnumerable<AlertDefinition> enumerable = alertDefinitionsDAL.GetValidItemsOfObjectTypeWithParams(objectType, loadActions, loadSchedules);
			if (enumerable != null)
			{
				enumerable = from p in enumerable
				where p.Enabled
				select p;
				object obj = new object();
				Parallel.ForEach<AlertDefinition>(enumerable, delegate(AlertDefinition definition)
				{
					if (definition.Trigger.Conditions.Count > 0)
					{
						IConditionEntityScope conditionEntityScope = definition.Trigger.Conditions[0].Type as IConditionEntityScope;
						if (conditionEntityScope != null)
						{
							QueryResult scopeQuery = conditionEntityScope.GetScopeQuery(definition.Trigger.Conditions[0].Condition, objectType);
							AlertScopeItem item = new AlertScopeItem
							{
								AlertId = Convert.ToInt32(definition.AlertID),
								InstanceName = definition.Name,
								ScopeQuery = scopeQuery,
								EntityType = entityType
							};
							object obj = obj;
							lock (obj)
							{
								results.Add(item);
							}
						}
					}
				});
			}
			return results;
		}

		// Token: 0x060000AD RID: 173 RVA: 0x000071C8 File Offset: 0x000053C8
		public AlertScopeItemDetails GetAlertScopeDetails(int alertId)
		{
			DataTable dataTable;
			DataTable dataTable2;
			using (IInformationServiceProxy2 informationServiceProxy = SwisConnectionProxyPool.GetCreator().Create())
			{
				dataTable = informationServiceProxy.Query("SELECT Field \r\n\t                                                  FROM Orion.CustomProperty \r\n\t                                                  WHERE TargetEntity = 'Orion.AlertConfigurationsCustomProperties'\r\n\t                                                  AND Table = 'AlertConfigurationsCustomProperties'");
				StringBuilder stringBuilder = new StringBuilder();
				if (dataTable.Rows.Count > 0)
				{
					foreach (object obj in dataTable.Rows)
					{
						DataRow dataRow = (DataRow)obj;
						stringBuilder.Append(", [CP]." + dataRow[0].ToString());
					}
				}
				string text = "SELECT AC.AlertID, AC.Name, AC.Description, AC.Severity {columnNames} \r\n\t                                     FROM Orion.AlertConfigurations AS [AC] \r\n\t                                     INNER JOIN Orion.AlertConfigurationsCustomProperties AS [CP] \r\n\t                                     ON AC.AlertID = CP.AlertID WHERE AC.AlertID = @alertID".Replace("{columnNames}", stringBuilder.ToString());
				dataTable2 = informationServiceProxy.Query(text, new Dictionary<string, object>
				{
					{
						"alertID",
						alertId
					}
				});
			}
			AlertScopeItemDetails alertScopeItemDetails = null;
			if (dataTable2 != null && dataTable2.Rows.Count == 1)
			{
				alertScopeItemDetails = new AlertScopeItemDetails();
				alertScopeItemDetails.AlertId = alertId;
				alertScopeItemDetails.AlertName = dataTable2.Rows[0]["Name"].ToString();
				alertScopeItemDetails.Description = dataTable2.Rows[0]["Description"].ToString();
				alertScopeItemDetails.Severity = int.Parse(dataTable2.Rows[0]["Severity"].ToString());
				alertScopeItemDetails.CustomProperties = new Dictionary<string, string>();
				foreach (object obj2 in dataTable.Rows)
				{
					string text2 = ((DataRow)obj2)[0].ToString();
					alertScopeItemDetails.CustomProperties.Add(text2, dataTable2.Rows[0][text2].ToString());
				}
			}
			return alertScopeItemDetails;
		}

		// Token: 0x060000AE RID: 174 RVA: 0x000073D0 File Offset: 0x000055D0
		public bool UnacknowledgeAlerts(int[] alertObjectIds, string accountId)
		{
			ActiveAlertDAL activeAlertDAL = new ActiveAlertDAL();
			IEnumerable<int> source = activeAlertDAL.LimitAlertAckStateUpdateCandidates(alertObjectIds, false);
			List<IIndication> list = new List<IIndication>();
			list.AddRange(activeAlertDAL.GetAlertUpdatedIndicationPropertiesByAlertObjectIds(alertObjectIds, accountId, string.Empty, DateTime.UtcNow, false));
			DataTable alertResetOrUpdateIndicationPropertiesTableByAlertObjectIds = activeAlertDAL.GetAlertResetOrUpdateIndicationPropertiesTableByAlertObjectIds(alertObjectIds);
			foreach (int num in alertObjectIds)
			{
				DataRow[] array = alertResetOrUpdateIndicationPropertiesTableByAlertObjectIds.Select("AlertObjectID=" + num);
				PropertyBag propertyBag = new PropertyBag();
				if (array.Length != 0)
				{
					propertyBag.Add("Acknowledged", (array[0]["Acknowledged"] != DBNull.Value) ? Convert.ToString(array[0]["Acknowledged"]) : "False");
					propertyBag.Add("AcknowledgedBy", (array[0]["AcknowledgedBy"] != DBNull.Value) ? Convert.ToString(array[0]["AcknowledgedBy"]) : string.Empty);
					propertyBag.Add("AcknowledgedDateTime", (array[0]["AcknowledgedDateTime"] != DBNull.Value) ? Convert.ToString(array[0]["AcknowledgedDateTime"]) : string.Empty);
				}
				List<IIndication> list2 = list;
				IndicationType indicationType = 2;
				PropertyBag propertyBag2 = new PropertyBag();
				propertyBag2.Add("AlertObjectID", num);
				propertyBag2.Add("Acknowledged", "False");
				propertyBag2.Add("AcknowledgedBy", string.Empty);
				propertyBag2.Add("AcknowledgedDateTime", string.Empty);
				propertyBag2.Add("PreviousProperties", propertyBag);
				propertyBag2.Add("InstanceType", "Orion.AlertActive");
				list2.Add(new CommonIndication(indicationType, propertyBag2));
			}
			bool flag = ActiveAlertDAL.UnacknowledgeAlerts(source.ToArray<int>(), accountId);
			if (flag)
			{
				IndicationPublisher.CreateV3().ReportIndications(list);
			}
			return flag;
		}

		// Token: 0x060000AF RID: 175 RVA: 0x00007599 File Offset: 0x00005799
		public string GetAlertNote(int alertObjectId)
		{
			return new ActiveAlertDAL().GetAlertNote(alertObjectId);
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x000075A8 File Offset: 0x000057A8
		public bool SetAlertNote(int alertObjectId, string accountId, string note, DateTime modificationDateTime, string previousNote)
		{
			ActiveAlertDAL activeAlertDAL = new ActiveAlertDAL();
			bool flag = activeAlertDAL.SetAlertNote(alertObjectId, accountId, note, modificationDateTime);
			if (flag)
			{
				List<IIndication> list = new List<IIndication>();
				List<IIndication> list2 = list;
				IndicationType indicationType = 2;
				PropertyBag propertyBag = new PropertyBag();
				propertyBag.Add("AlertObjectID", alertObjectId);
				propertyBag.Add("AlertNote", note);
				string key = "PreviousProperties";
				PropertyBag propertyBag2 = new PropertyBag();
				propertyBag2.Add("AlertNote", previousNote);
				propertyBag.Add(key, propertyBag2);
				propertyBag.Add("InstanceType", "Orion.AlertObjects");
				list2.Add(new CommonIndication(indicationType, propertyBag));
				IEnumerable<AlertUpdatedIndication> alertUpdatedIndicationPropertiesByAlertObjectIds = activeAlertDAL.GetAlertUpdatedIndicationPropertiesByAlertObjectIds(new List<int>
				{
					alertObjectId
				}, accountId, note, DateTime.UtcNow, false);
				if (alertUpdatedIndicationPropertiesByAlertObjectIds.Any<AlertUpdatedIndication>())
				{
					PropertyBag sourceInstanceProperties = alertUpdatedIndicationPropertiesByAlertObjectIds.ElementAt(0).GetSourceInstanceProperties();
					if (sourceInstanceProperties.ContainsKey("Acknowledged"))
					{
						sourceInstanceProperties.Remove("Acknowledged");
					}
					if (sourceInstanceProperties.ContainsKey("AcknowledgedBy"))
					{
						sourceInstanceProperties.Remove("AcknowledgedBy");
					}
					if (sourceInstanceProperties.ContainsKey("AcknowledgedMethod"))
					{
						sourceInstanceProperties.Remove("AcknowledgedMethod");
					}
					if (!sourceInstanceProperties.ContainsKey("Notes"))
					{
						sourceInstanceProperties.Add("Notes", note);
					}
					IIndication item = new CommonIndication(10, sourceInstanceProperties);
					list.Add(item);
				}
				IndicationPublisher.CreateV3().ReportIndications(list);
			}
			return flag;
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x000076E1 File Offset: 0x000058E1
		public bool SetAlertNote(int alertObjectId, string accountId, string note, DateTime modificationDateTime)
		{
			return this.SetAlertNote(alertObjectId, accountId, note, modificationDateTime, string.Empty);
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x000076F4 File Offset: 0x000058F4
		public bool AppendAlertNote(int alertObjectId, string accountId, string note, DateTime modificationDateTime)
		{
			string alertNote = this.GetAlertNote(alertObjectId);
			note = (string.IsNullOrWhiteSpace(alertNote) ? note : (alertNote + Environment.NewLine + note));
			return this.SetAlertNote(alertObjectId, accountId, note, modificationDateTime, alertNote);
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x0000772E File Offset: 0x0000592E
		public AlertImportResult ImportAlert(string fileContent, string userName, bool generateNewGuid, bool importIfExists, bool importSmtpServer)
		{
			return this.ImportAlertConfiguration(fileContent, userName, generateNewGuid, importIfExists, importSmtpServer, false, string.Empty);
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x00007744 File Offset: 0x00005944
		public AlertImportResult ImportAlertConfiguration(string fileContent, string user, bool generateNewGuid, bool importIfExists, bool importSmtpServer, bool stripSensitiveData, string protectionPassword)
		{
			AlertImportResult result = null;
			try
			{
				ISwisConnectionProxyCreator creator = SwisConnectionProxyPool.GetCreator();
				AlertDefinition alertDefinition = new AlertImporter(AlertMigrationPluginProvider.Create(AppDomainCatalogSingleton.Instance), creator, new CoreNetObjectTypeSource(this), generateNewGuid, importIfExists, false, false).ImportAlert(XElement.Parse(fileContent), null, false, true, this, null, stripSensitiveData, protectionPassword);
				result = new AlertImportResult
				{
					AlertId = new int?(alertDefinition.AlertID.Value),
					Name = alertDefinition.Name,
					MigrationMessage = "Alert imported successfully"
				};
			}
			catch (CryptographicException ex)
			{
				result = new AlertImportResult
				{
					MigrationMessage = string.Format("Alert import failed with error: {0}", ex.Message),
					IncorrectPasswordForDecryptSensitiveData = true
				};
			}
			catch (Exception ex2)
			{
				result = new AlertImportResult
				{
					MigrationMessage = string.Format("Alert import failed with error: {0}", ex2.Message)
				};
			}
			return result;
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x00007828 File Offset: 0x00005A28
		[Obsolete("Old alerting will be removed")]
		public bool RevertMigratedAlert(Guid alertRefId, bool enableInOldAlerting)
		{
			return AlertDAL.RevertMigratedAlert(alertRefId, enableInOldAlerting);
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x00007831 File Offset: 0x00005A31
		public string ExportAlert(int alertId)
		{
			return new AlertExporter().ExportAlert(alertId);
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x0000783E File Offset: 0x00005A3E
		public string ExportAlertConfiguration(int alertId, bool stripSensitiveData, string protectionPassword)
		{
			return new AlertExporter().ExportAlert(alertId, stripSensitiveData, protectionPassword);
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x00007850 File Offset: 0x00005A50
		public AlertMigrationResult MigrateAdvancedAlertFromDB(string definitionIdGuid)
		{
			AlertMigrationResult result;
			using (AlertsMigrator alertsMigrator = new AlertsMigrator())
			{
				result = alertsMigrator.MigrateAdvancedAlertFromDB(definitionIdGuid);
			}
			return result;
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x00007888 File Offset: 0x00005A88
		public AlertMigrationResult[] MigrateAllAdvancedAlertsFromDB()
		{
			AlertMigrationResult[] result;
			using (AlertsMigrator alertsMigrator = new AlertsMigrator())
			{
				result = alertsMigrator.MigrateAllAdvancedAlertsFromDB(false);
			}
			return result;
		}

		// Token: 0x060000BA RID: 186 RVA: 0x000078C0 File Offset: 0x00005AC0
		public AlertMigrationResult[] MigrateAdvancedAlertFromXML(string xmlOldAlertDefinition)
		{
			AlertMigrationResult[] result;
			using (AlertsMigrator alertsMigrator = new AlertsMigrator())
			{
				result = alertsMigrator.MigrateAdvancedAlertFromXML(xmlOldAlertDefinition);
			}
			return result;
		}

		// Token: 0x060000BB RID: 187 RVA: 0x000078F8 File Offset: 0x00005AF8
		public AlertMigrationResult MigrateBasicAlertFromDB(int alertId)
		{
			AlertMigrationResult result;
			using (AlertsMigrator alertsMigrator = new AlertsMigrator())
			{
				result = alertsMigrator.MigrateBasicAlertFromDB(alertId);
			}
			return result;
		}

		// Token: 0x060000BC RID: 188 RVA: 0x00007930 File Offset: 0x00005B30
		public AlertMigrationResult[] MigrateAllBasicAlertsFromDB()
		{
			AlertMigrationResult[] result;
			using (AlertsMigrator alertsMigrator = new AlertsMigrator())
			{
				result = alertsMigrator.MigrateAllBasicAlertsFromDB(false);
			}
			return result;
		}

		// Token: 0x060000BD RID: 189 RVA: 0x00007968 File Offset: 0x00005B68
		public AlertMigrationResult[] GetAlertMigrationResults(string migrationId)
		{
			return new AlertMigrationLogDAL().GetAlertMigrationResults(migrationId).ToArray<AlertMigrationResult>();
		}

		// Token: 0x060000BE RID: 190 RVA: 0x0000797A File Offset: 0x00005B7A
		public CannedAlertImportResult[] GetCannedAlertImportResults(string importId)
		{
			return new ImportedCannedAlertDAL().GetCannedAlertImportResults(importId).ToArray<CannedAlertImportResult>();
		}

		// Token: 0x060000BF RID: 191 RVA: 0x0000798C File Offset: 0x00005B8C
		public List<AuditActionTypeInfo> GetAuditingActionTypes()
		{
			AuditingDAL auditingDAL = new AuditingDAL();
			auditingDAL.LoadKeys();
			return auditingDAL.GetAuditingActionTypes();
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x0000799F File Offset: 0x00005B9F
		public DataTable GetAuditingTable(int maxRecords, int netObjectId, string netObjectType, int nodeId, string actionTypeIds, DateTime startTime, DateTime endTime)
		{
			return this.GetAuditingTableWithHtmlMessages(AuditingDAL.GetAuditingTable(maxRecords, netObjectId, netObjectType, nodeId, actionTypeIds, startTime, endTime));
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x000079B8 File Offset: 0x00005BB8
		private DataTable GetAuditingTableWithHtmlMessages(DataTable originTable)
		{
			DataTable dataTable = new DataTable
			{
				TableName = "AuditingTableWithHtmlMessages"
			};
			dataTable.Columns.Add("DateTime", typeof(DateTime));
			dataTable.Columns.Add("AccountID", typeof(string));
			dataTable.Columns.Add("Message", typeof(string));
			if (originTable == null || originTable.Rows.Count == 0)
			{
				return dataTable;
			}
			foreach (IGrouping<int, DataRow> source in from DataRow it in originTable.Rows
			group it by it.Field("AuditEventID"))
			{
				int actionTypeId = source.First<DataRow>().Field("ActionTypeID");
				string text = source.First<DataRow>().Field("AccountID");
				DateTime dateTime = source.First<DataRow>().Field("DateTime");
				Dictionary<string, string> args = source.Select((DataRow it) => new
				{
					Key = it.Field("ArgsKey"),
					Value = it.Field("ArgsValue")
				}).Where(it => it.Key != null).ToDictionary(it => it.Key, it => it.Value);
				string storedMessage = source.First<DataRow>().Field("Message");
				dataTable.Rows.Add(new object[]
				{
					dateTime,
					text,
					this.RetrieveHtmlMessage(actionTypeId, text, args, storedMessage)
				});
			}
			return dataTable;
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x00007BB0 File Offset: 0x00005DB0
		private string RetrieveHtmlMessage(int actionTypeId, string account, Dictionary<string, string> args, string storedMessage)
		{
			AuditActionType actionTypeFromActionId = this.auditingDal.GetActionTypeFromActionId(actionTypeId);
			IAuditing2 auditingInstancesOfActionType = this._auditPluginManager.GetAuditingInstancesOfActionType(actionTypeFromActionId);
			if (auditingInstancesOfActionType != null)
			{
				return auditingInstancesOfActionType.GetHTMLMessage(new AuditDataContainer(actionTypeFromActionId, args, account));
			}
			return storedMessage;
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x00007BEB File Offset: 0x00005DEB
		public DataTable GetAuditingTypesTable()
		{
			return AuditingDAL.GetAuditingTypesTable();
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x00007BF4 File Offset: 0x00005DF4
		private static BlogItem DalToWfc(BlogItemDAL dal)
		{
			if (dal == null)
			{
				return null;
			}
			return new BlogItem(dal.Id, dal.Title, dal.Description, dal.CreatedAt, dal.Ignored, dal.Url, dal.AcknowledgedAt, dal.AcknowledgedBy, dal.PostGuid, dal.PostId, dal.Owner, dal.PublicationDate, dal.CommentsUrl, dal.CommentsCount);
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x00007C60 File Offset: 0x00005E60
		public BlogItem GetBlogNotificationItem(Guid blogId)
		{
			CoreBusinessLayerService.log.Debug("Sending request for BlogItemDAL.GetBlogById.");
			BlogItem result;
			try
			{
				result = CoreBusinessLayerService.DalToWfc(BlogItemDAL.GetItemById(blogId));
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error obtaining blog notification item: " + ex.ToString());
				throw new Exception(string.Format(Resources.LIBCODE_JM0_25, blogId));
			}
			return result;
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x00007CCC File Offset: 0x00005ECC
		public List<BlogItem> GetBlogNotificationItems(int maxResultsCount, bool includeIgnored)
		{
			CoreBusinessLayerService.log.Debug("Sending request for BlogItemDAL.GetItems.");
			List<BlogItem> result;
			try
			{
				List<BlogItem> list = new List<BlogItem>();
				foreach (BlogItemDAL dal in BlogItemDAL.GetItems(new BlogFilter(true, includeIgnored, maxResultsCount)))
				{
					list.Add(CoreBusinessLayerService.DalToWfc(dal));
				}
				result = list;
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error when obtaining blog notification items: " + ex.ToString());
				throw new Exception(Resources.LIBCODE_JM0_26);
			}
			return result;
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x00007D74 File Offset: 0x00005F74
		public void ForceBlogUpdatesCheck()
		{
			CoreBusinessLayerService.log.Debug("Sending request for CoreHelper.CheckOrionProductTeamBlog.");
			try
			{
				CoreHelper.CheckOrionProductTeamBlog();
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error forcing blog notification items update: " + ex.ToString());
				throw new Exception(Resources.LIBCODE_JM0_27);
			}
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x00007DD0 File Offset: 0x00005FD0
		public BlogItem GetBlogNotificationItemForPost(Guid postGuid, long postId)
		{
			CoreBusinessLayerService.log.Debug("Sending request for BlogItemDAL.GetBlogItemForPos.");
			BlogItem result;
			try
			{
				result = CoreBusinessLayerService.DalToWfc(BlogItemDAL.GetBlogItemForPost(postGuid, postId));
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error obtaining blog notification item for post: " + ex.ToString());
				throw new Exception(string.Format(Resources.LIBCODE_JM0_28, postGuid, postId));
			}
			return result;
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x00007E44 File Offset: 0x00006044
		[Obsolete("removed", true)]
		public List<ServiceURI> GetAllVMwareServiceURIs()
		{
			throw new NotSupportedException("GetAllVMwareServiceURIs");
		}

		// Token: 0x060000CA RID: 202 RVA: 0x00007E50 File Offset: 0x00006050
		[Obsolete("removed", true)]
		public VMCredential GetVMwareCredential(long vmwareCredentialsID)
		{
			throw new NotSupportedException("GetVMwareCredential");
		}

		// Token: 0x060000CB RID: 203 RVA: 0x00007E5C File Offset: 0x0000605C
		[Obsolete("removed", true)]
		public void InsertUpdateVMHostNode(VMHostNode node)
		{
			throw new NotSupportedException("InsertUpdateVMHostNode");
		}

		// Token: 0x060000CC RID: 204 RVA: 0x00007E68 File Offset: 0x00006068
		[Obsolete("removed", true)]
		public VMHostNode GetVMHostNode(int nodeId)
		{
			throw new NotSupportedException("GetVMHostNode");
		}

		// Token: 0x060000CD RID: 205 RVA: 0x00007E74 File Offset: 0x00006074
		[Obsolete("Core-Split cleanup. If you need this member please contact Core team", true)]
		public Dictionary<int, bool> RunNowGeolocation()
		{
			CoreBusinessLayerService.log.DebugFormat("Running job(s)", Array.Empty<object>());
			Dictionary<int, bool> dictionary = new Dictionary<int, bool>();
			string[] entitiesAvailableForGeolocation = WorldMapPointsDAL.GetEntitiesAvailableForGeolocation();
			int num = 1;
			foreach (string text in entitiesAvailableForGeolocation)
			{
				string value;
				if (!WebSettingsDAL.TryGet(string.Format("{0}_GeolocationField", text), ref value))
				{
					return dictionary;
				}
				if (string.IsNullOrWhiteSpace(value))
				{
					return dictionary;
				}
				ActionDefinition actionDefinition = new ActionDefinition();
				actionDefinition.ActionTypeID = "Geolocation";
				actionDefinition.Enabled = true;
				ActionProperties actionProperties = new ActionProperties();
				actionProperties.Add("StreetAddress", "Location");
				actionProperties.Add("Entity", text);
				actionProperties.Add("MapQuestApiKey", WorldMapPointsDAL.GetMapQuestKey());
				actionDefinition.Properties = actionProperties;
				bool value2 = this.ExecuteAction(actionDefinition, new GeolocationActionContext()).Status == 1;
				if (!dictionary.Keys.Contains(num))
				{
					dictionary.Add(num, value2);
				}
				else
				{
					dictionary[num] = value2;
				}
			}
			return dictionary;
		}

		// Token: 0x060000CE RID: 206 RVA: 0x00007F65 File Offset: 0x00006165
		public List<MacroPickerDefinition> GetMacroPickerDefinition(MacroContext context)
		{
			return new MacroParser().GetMacroPickerDefinition(context).ToList<MacroPickerDefinition>();
		}

		// Token: 0x060000CF RID: 207 RVA: 0x00007F77 File Offset: 0x00006177
		public string FormatMacroValue(string formatter, string value, string dataType)
		{
			return new MacroParser().FormatValue(formatter, value, dataType);
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x00007F86 File Offset: 0x00006186
		public NetObjectTypes GetNetObjectTypes()
		{
			return NetObjectTypeMgr.GetNetObjectTypes();
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x00007F8D File Offset: 0x0000618D
		public Dictionary<string, string> GetNetObjectData()
		{
			return NetObjectTypeMgr.GetNetObjectData();
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x00007F94 File Offset: 0x00006194
		public TestJobResult TestSnmpCredentialsOnAgent(int nodeId, uint snmpAgentPort, SnmpCredentialsV2 credentials)
		{
			AgentInfo agentInfoByNode = this._agentInfoDal.GetAgentInfoByNode(nodeId);
			if (agentInfoByNode == null || agentInfoByNode.ConnectionStatus != 1)
			{
				CoreBusinessLayerService.log.WarnFormat("SNMP credential test could not start because agent for node {0} is not connected", nodeId);
				return new TestJobResult
				{
					Success = false,
					Message = Resources.TestErrorAgentNotConnected
				};
			}
			TimeSpan testJobTimeout = BusinessLayerSettings.Instance.TestJobTimeout;
			SnmpSettings snmpSettings = new SnmpSettings
			{
				AgentPort = (int)snmpAgentPort,
				TargetIP = IPAddress.Loopback
			};
			JobDescription jobDescription = new JobDescription
			{
				TypeName = "SolarWinds.Orion.Core.TestSnmpCredentialsJob",
				JobDetailConfiguration = SerializationHelper.ToJson(snmpSettings),
				JobNamespace = "orion",
				ResultTTL = testJobTimeout,
				Timeout = testJobTimeout,
				TargetNode = new HostAddress(IPAddress.Loopback.ToString(), 4),
				EndpointAddress = agentInfoByNode.AgentGuid.ToString(),
				SupportedRoles = 7
			};
			string message;
			TestJobResult testJobResult = this.ExecuteJobAndGetResult<TestJobResult>(jobDescription, credentials, JobResultDataFormatType.Json, "SNMP", out message);
			if (testJobResult.Success)
			{
				CoreBusinessLayerService.log.InfoFormat("SNMP credential test finished. Success: {0}, Message: {1}", testJobResult.Success, testJobResult.Message);
				return testJobResult;
			}
			return new TestJobResult
			{
				Success = false,
				Message = message
			};
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x000080CC File Offset: 0x000062CC
		internal T ExecuteJobAndGetResult<T>(JobDescription jobDescription, CredentialBase jobCredential, JobResultDataFormatType resultDataFormat, string jobType, out string errorMessage) where T : TestJobResult, new()
		{
			this.GetCurrentServiceInstance().RouteJobToEngine(jobDescription);
			T result;
			using (OneTimeJobRawResult oneTimeJobRawResult = this._oneTimeJobManager.ExecuteJob(jobDescription, jobCredential))
			{
				errorMessage = oneTimeJobRawResult.Error;
				if (!oneTimeJobRawResult.Success)
				{
					CoreBusinessLayerService.log.WarnFormat(jobType + " credential test failed: " + oneTimeJobRawResult.Error, Array.Empty<object>());
					string localizedErrorMessageFromException = this.GetLocalizedErrorMessageFromException(oneTimeJobRawResult.ExceptionFromJob);
					T t = Activator.CreateInstance<T>();
					t.Success = false;
					t.Message = (string.IsNullOrEmpty(localizedErrorMessageFromException) ? errorMessage : localizedErrorMessageFromException);
					result = t;
				}
				else
				{
					try
					{
						if (resultDataFormat == JobResultDataFormatType.Xml)
						{
							using (XmlTextReader xmlTextReader = new XmlTextReader(oneTimeJobRawResult.JobResultStream))
							{
								xmlTextReader.Namespaces = false;
								return (T)((object)new XmlSerializer(typeof(T)).Deserialize(xmlTextReader));
							}
						}
						result = SerializationHelper.Deserialize<T>(oneTimeJobRawResult.JobResultStream);
					}
					catch (Exception arg)
					{
						CoreBusinessLayerService.log.Error(string.Format("Failed to deserialize {0} credential test job result: {1}", jobType, arg));
						T t2 = Activator.CreateInstance<T>();
						t2.Success = false;
						t2.Message = this.GetLocalizedErrorMessageFromException(oneTimeJobRawResult.ExceptionFromJob);
						result = t2;
					}
				}
			}
			return result;
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x00008258 File Offset: 0x00006458
		private string GetLocalizedErrorMessageFromException(Exception exception)
		{
			if (exception != null && exception is FaultException<JobEngineConnectionFault>)
			{
				return Resources.LIBCODE_PS0_20;
			}
			return string.Empty;
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x00008270 File Offset: 0x00006470
		public void UpdateOrionFeatures()
		{
			this.ServiceContainer.GetService<OrionFeatureResolver>().Resolve();
		}

		// Token: 0x060000D6 RID: 214 RVA: 0x00008282 File Offset: 0x00006482
		public void UpdateOrionFeaturesForProvider(string provider)
		{
			this.ServiceContainer.GetService<OrionFeatureResolver>().Resolve(provider);
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x00008295 File Offset: 0x00006495
		public void DeleteOrionServerByEngineId(int engineId)
		{
			new OrionServerDAL().DeleteOrionServerByEngineId(engineId);
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x000082A3 File Offset: 0x000064A3
		public IEnumerable<Technology> GetTechnologyList()
		{
			return (from t in TechnologyManager.Instance.TechnologyFactory.Items()
			select new Technology
			{
				DisplayName = t.DisplayName,
				TargetEntity = t.TargetEntity,
				TechnologyID = t.TechnologyID
			}).ToList<Technology>();
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x000082DD File Offset: 0x000064DD
		public IEnumerable<TechnologyPolling> GetTechnologyPollingList()
		{
			return (from t in TechnologyManager.Instance.TechnologyPollingFactory.Items()
			select new TechnologyPolling
			{
				DisplayName = t.DisplayName,
				TechnologyID = t.TechnologyID,
				Priority = t.Priority,
				TechnologyPollingID = t.TechnologyPollingID
			}).ToList<TechnologyPolling>();
		}

		// Token: 0x060000DA RID: 218 RVA: 0x00008317 File Offset: 0x00006517
		public int[] EnableDisableTechnologyPollingAssignmentOnNetObjects(string technologyPollingID, bool enable, int[] netObjectIDs)
		{
			return TechnologyManager.Instance.TechnologyPollingFactory.EnableDisableAssignments(technologyPollingID, enable, netObjectIDs);
		}

		// Token: 0x060000DB RID: 219 RVA: 0x0000832B File Offset: 0x0000652B
		public int[] EnableDisableTechnologyPollingAssignment(string technologyPollingID, bool enable)
		{
			return TechnologyManager.Instance.TechnologyPollingFactory.EnableDisableAssignments(technologyPollingID, enable, null);
		}

		// Token: 0x060000DC RID: 220 RVA: 0x0000833F File Offset: 0x0000653F
		public IEnumerable<TechnologyPollingAssignment> GetTechnologyPollingAssignmentsOnNetObjects(string technologyPollingID, int[] netObjectIDs)
		{
			return TechnologyManager.Instance.TechnologyPollingFactory.GetAssignments(technologyPollingID, netObjectIDs).ToList<TechnologyPollingAssignment>();
		}

		// Token: 0x060000DD RID: 221 RVA: 0x00008357 File Offset: 0x00006557
		public IEnumerable<TechnologyPollingAssignment> GetTechnologyPollingAssignments(string technologyPollingID)
		{
			return TechnologyManager.Instance.TechnologyPollingFactory.GetAssignments(technologyPollingID).ToList<TechnologyPollingAssignment>();
		}

		// Token: 0x060000DE RID: 222 RVA: 0x0000836E File Offset: 0x0000656E
		public ICollection<TechnologyPollingAssignment> GetTechnologyPollingAssignmentsFiltered(string[] technologyPollingIDsFilter, int[] netObjectIDsFilter, string[] targetEntitiesFilter, bool[] enabledFilter)
		{
			return TechnologyManager.Instance.TechnologyPollingFactory.GetAssignmentsFiltered(technologyPollingIDsFilter, netObjectIDsFilter, targetEntitiesFilter, enabledFilter).ToList<TechnologyPollingAssignment>();
		}

		// Token: 0x060000DF RID: 223 RVA: 0x00008389 File Offset: 0x00006589
		public List<TimeFrame> GetAllTimeFrames(string timeFrameName = null)
		{
			return TimeFramesDAL.GetAllTimeFrames(timeFrameName);
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x00008391 File Offset: 0x00006591
		public List<TimeFrame> GetCoreTimeFrames(string timeFrameName = null)
		{
			return TimeFramesDAL.GetCoreTimeFrames(timeFrameName);
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x00008399 File Offset: 0x00006599
		public IList<PredefinedCustomProperty> GetPredefinedCustomProperties(string targetEntity, bool includeAdvanced)
		{
			return CustomPropertyDAL.GetPredefinedPropertiesForTable(targetEntity, includeAdvanced);
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x000083A2 File Offset: 0x000065A2
		public bool IsSystemProperty(string tableName, string propName)
		{
			return CustomPropertyDAL.IsSystemProperty(tableName, propName);
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x000083AB File Offset: 0x000065AB
		public bool IsReservedWord(string propName)
		{
			return CustomPropertyDAL.IsReservedWord(propName);
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x000083B3 File Offset: 0x000065B3
		public bool IsColumnExists(string table, string name)
		{
			return CustomPropertyDAL.IsColumnExists(table, name);
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x000083BC File Offset: 0x000065BC
		public List<string> GetSystemPropertyNamesFromDb(string table)
		{
			return CustomPropertyDAL.GetSystemPropertyNamesFromDb(table);
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x000083C4 File Offset: 0x000065C4
		[Obsolete("Use IDependencyProxy class", true)]
		public int DeleteDependencies(List<int> listIds)
		{
			CoreBusinessLayerService.log.Error("Unexpected call to deprecated method DeleteDependencies.");
			throw new InvalidOperationException("Use IDependencyProxy class");
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x000083DF File Offset: 0x000065DF
		[Obsolete("Use IDependencyProxy class", true)]
		public Dependency GetDependency(int id)
		{
			CoreBusinessLayerService.log.Error("Unexpected call to deprecated method GetDependency");
			throw new InvalidOperationException("Use IDependencyProxy class");
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x000083FA File Offset: 0x000065FA
		[Obsolete("Use IDependencyProxy class", true)]
		public void UpdateDependency(Dependency dependency)
		{
			CoreBusinessLayerService.log.Error("Unexpected call to deprecated method UpdateDependency");
			throw new InvalidOperationException("Use IDependencyProxy class");
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x00008415 File Offset: 0x00006615
		[Obsolete("Use DeleteOrionDiscoveryProfile", true)]
		public void DeleteDiscoveryProfile(int profileID)
		{
		}

		// Token: 0x060000EA RID: 234 RVA: 0x00008417 File Offset: 0x00006617
		public DiscoveryConfiguration GetDiscoveryConfigurationByProfile(int profileID)
		{
			return DiscoveryDatabase.GetDiscoveryConfiguration(profileID);
		}

		// Token: 0x060000EB RID: 235 RVA: 0x00008420 File Offset: 0x00006620
		public bool TryConnectionWithJobScheduler(out string errorMessage)
		{
			bool result;
			try
			{
				using (IJobSchedulerHelper instance = JobScheduler.GetInstance())
				{
					instance.PolicyExists("Nothing");
					errorMessage = string.Empty;
					result = true;
				}
			}
			catch (Exception ex)
			{
				errorMessage = string.Format("{0}: {1}", ex.GetType().Name, ex.Message);
				result = false;
			}
			return result;
		}

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x060000EC RID: 236 RVA: 0x00008494 File Offset: 0x00006694
		// (set) Token: 0x060000ED RID: 237 RVA: 0x000084B9 File Offset: 0x000066B9
		public IJobFactory JobFactory
		{
			get
			{
				IJobFactory result;
				if ((result = this._jobFactory) == null)
				{
					result = (this._jobFactory = new OrionDiscoveryJobFactory());
				}
				return result;
			}
			set
			{
				this._jobFactory = value;
			}
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x060000EE RID: 238 RVA: 0x000084C4 File Offset: 0x000066C4
		// (set) Token: 0x060000EF RID: 239 RVA: 0x000084E9 File Offset: 0x000066E9
		public IPersistentDiscoveryCache PersistentDiscoveryCache
		{
			get
			{
				IPersistentDiscoveryCache result;
				if ((result = this._persistentDiscoveryCache) == null)
				{
					result = (this._persistentDiscoveryCache = new PersistentDiscoveryCache());
				}
				return result;
			}
			set
			{
				this._persistentDiscoveryCache = value;
			}
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x000084F4 File Offset: 0x000066F4
		public Guid CreateOneTimeAgentDiscoveryJob(int nodeId, int engineId, int? profileId, List<Credential> credentials)
		{
			OneTimeDiscoveryJobConfiguration jobConfiguration = new OneTimeDiscoveryJobConfiguration
			{
				NodeId = new int?(nodeId),
				IpAddress = IPAddress.Loopback,
				EngineId = engineId,
				Credentials = credentials,
				ProfileId = profileId
			};
			return this.CreateOneTimeDiscoveryJobWithCache(jobConfiguration, DiscoveryCacheConfiguration.EnableCaching);
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x0000853C File Offset: 0x0000673C
		public Guid CreateOneTimeDiscoveryJob(int? nodeId, uint? snmpPort, SNMPVersion? preferredSnmpVersion, IPAddress ip, int engineId, List<Credential> credentials)
		{
			OneTimeDiscoveryJobConfiguration jobConfiguration = new OneTimeDiscoveryJobConfiguration
			{
				NodeId = nodeId,
				SnmpPort = snmpPort,
				PreferredSnmpVersion = preferredSnmpVersion,
				IpAddress = ip,
				EngineId = engineId,
				Credentials = credentials
			};
			return this.CreateOneTimeDiscoveryJobWithCache(jobConfiguration, DiscoveryCacheConfiguration.EnableCaching);
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x00008584 File Offset: 0x00006784
		public Guid CreateOneTimeDiscoveryJobWithCache(int? nodeId, uint? snmpPort, SNMPVersion? preferredSnmpVersion, IPAddress ip, int engineId, List<Credential> credentials, DiscoveryCacheConfiguration cacheConfiguration)
		{
			OneTimeDiscoveryJobConfiguration jobConfiguration = new OneTimeDiscoveryJobConfiguration
			{
				NodeId = nodeId,
				SnmpPort = snmpPort,
				PreferredSnmpVersion = preferredSnmpVersion,
				IpAddress = ip,
				EngineId = engineId,
				Credentials = credentials
			};
			return this.CreateOneTimeDiscoveryJobWithCache(jobConfiguration, cacheConfiguration);
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x000085D0 File Offset: 0x000067D0
		public Guid CreateOneTimeDiscoveryJobWithCache(OneTimeDiscoveryJobConfiguration jobConfiguration, DiscoveryCacheConfiguration cacheConfiguration)
		{
			CoreBusinessLayerService.log.DebugFormat("Creating one shot discovery job. Caching policy is {0}", cacheConfiguration);
			if (jobConfiguration.NodeId != null && cacheConfiguration == DiscoveryCacheConfiguration.EnableCaching)
			{
				CoreBusinessLayerService.log.DebugFormat("Scanning cache for discovery for Node {0}", jobConfiguration.NodeId);
				DiscoveryResultItem resultForNode = this.PersistentDiscoveryCache.GetResultForNode(jobConfiguration.NodeId.Value);
				if (resultForNode != null)
				{
					DiscoveryResultCache.Instance.AddOrReplaceResult(resultForNode);
					return resultForNode.JobId;
				}
			}
			else
			{
				CoreBusinessLayerService.log.DebugFormat("Bypassing discovery cache. ", Array.Empty<object>());
			}
			DiscoveryConfiguration discoveryConfiguration = new DiscoveryConfiguration
			{
				ProfileId = jobConfiguration.ProfileId,
				EngineId = jobConfiguration.EngineId,
				HopCount = 0,
				SearchTimeout = DiscoverySettings.DefaultSearchTimeout,
				SnmpTimeout = TimeSpan.FromMilliseconds((double)this._settingsDal.GetCurrentInt("SWNetPerfMon-Settings-SNMP Timeout", 2500)),
				SnmpRetries = this._settingsDal.GetCurrentInt("SWNetPerfMon-Settings-SNMP Retries", 2),
				DefaultProbes = jobConfiguration.DefaultProbes,
				TagFilter = jobConfiguration.TagFilter,
				DisableICMP = jobConfiguration.DisableIcmp
			};
			DiscoveryPollingEngineType? discoveryPollingEngineType = OrionDiscoveryJobFactory.GetDiscoveryPollingEngineType(jobConfiguration.EngineId, this._engineDal);
			Node node = null;
			if (jobConfiguration.NodeId != null)
			{
				node = this._nodeBlDal.GetNode(jobConfiguration.NodeId.Value);
				if (node == null)
				{
					CoreBusinessLayerService.log.ErrorFormat("Unable to get node {0}", jobConfiguration.NodeId.Value);
				}
			}
			if (jobConfiguration.SnmpPort != null)
			{
				discoveryConfiguration.SnmpPort = jobConfiguration.SnmpPort.Value;
			}
			else if (node != null)
			{
				discoveryConfiguration.SnmpPort = node.SNMPPort;
			}
			else
			{
				discoveryConfiguration.SnmpPort = 161U;
				CoreBusinessLayerService.log.InfoFormat("Unable to determine SNMP port node {0} IP {1}, using default 161", jobConfiguration.NodeId ?? -1, jobConfiguration.IpAddress);
			}
			if (jobConfiguration.PreferredSnmpVersion != null)
			{
				discoveryConfiguration.PreferredSnmpVersion = jobConfiguration.PreferredSnmpVersion.Value;
			}
			else if (node != null)
			{
				discoveryConfiguration.PreferredSnmpVersion = node.SNMPVersion;
			}
			else
			{
				discoveryConfiguration.PreferredSnmpVersion = 2;
				CoreBusinessLayerService.log.InfoFormat("Unable to determine preffered SNMP version node {0} IP {1}, using default v2c", jobConfiguration.NodeId ?? -1, jobConfiguration.IpAddress);
			}
			List<Credential> list = jobConfiguration.Credentials ?? new List<Credential>();
			AgentInfo agentInfo = this.TryGetAgentInfoAndUpdateConfiguration(node, list, discoveryConfiguration);
			List<string> list2 = new List<string>();
			bool flag = RegistrySettings.IsFreePoller();
			List<DiscoveryPluginInfo> discoveryPluginInfos = DiscoveryPluginFactory.GetDiscoveryPluginInfos();
			IList<IDiscoveryPlugin> orderedDiscoveryPlugins = DiscoveryHelper.GetOrderedDiscoveryPlugins();
			IDictionary<IDiscoveryPlugin, DiscoveryPluginInfo> pluginInfoPairs = DiscoveryPluginHelper.CreatePairsPluginAndInfo(orderedDiscoveryPlugins, discoveryPluginInfos);
			foreach (IDiscoveryPlugin discoveryPlugin in orderedDiscoveryPlugins)
			{
				if (flag && !(discoveryPlugin is ISupportFreeEngine))
				{
					CoreBusinessLayerService.log.DebugFormat("Discovery plugin {0} is not supported on FPE machine", discoveryPlugin.GetType().FullName);
				}
				else
				{
					IOneTimeJobSupport oneTimeJobSupport = discoveryPlugin as IOneTimeJobSupport;
					if (oneTimeJobSupport == null)
					{
						CoreBusinessLayerService.log.DebugFormat("N/A one time job for {0}", discoveryPlugin);
					}
					else
					{
						if (jobConfiguration.TagFilter != null && jobConfiguration.TagFilter.Any<string>())
						{
							IDiscoveryPluginTags discoveryPluginTags = discoveryPlugin as IDiscoveryPluginTags;
							if (discoveryPluginTags == null)
							{
								CoreBusinessLayerService.log.DebugFormat("Discovery job for tags requested, however plugin {0} doesn't support tags, skipping.", discoveryPlugin);
								continue;
							}
							if (!jobConfiguration.TagFilter.Intersect(discoveryPluginTags.Tags ?? Enumerable.Empty<string>(), StringComparer.InvariantCultureIgnoreCase).Any<string>())
							{
								CoreBusinessLayerService.log.DebugFormat("Discovery job for tags [{0}], however plugin {1} doesn't support any of the tags requested, skipping.", string.Join(",", jobConfiguration.TagFilter), discoveryPlugin);
								continue;
							}
						}
						if (agentInfo == null || this.DoesPluginSupportAgent(discoveryPlugin, agentInfo, list2))
						{
							if (discoveryPollingEngineType != null && !OrionDiscoveryJobFactory.IsDiscoveryPluginSupportedForDiscoveryPollingEngineType(discoveryPlugin, discoveryPollingEngineType.Value, pluginInfoPairs))
							{
								if (CoreBusinessLayerService.log.IsDebugEnabled)
								{
									CoreBusinessLayerService.log.DebugFormat(string.Format("Plugin {0} is not supported for polling engine {1} of type {2}", discoveryPlugin.GetType().FullName, discoveryConfiguration.EngineID, discoveryPollingEngineType.Value), Array.Empty<object>());
								}
							}
							else
							{
								DiscoveryPluginConfigurationBase oneTimeJobConfiguration = oneTimeJobSupport.GetOneTimeJobConfiguration(jobConfiguration.NodeId, jobConfiguration.IpAddress, list);
								discoveryConfiguration.AddDiscoveryPluginConfiguration(oneTimeJobConfiguration);
								CoreBusinessLayerService.log.DebugFormat("added one time job for {0}", discoveryPlugin);
							}
						}
					}
				}
			}
			discoveryConfiguration.AgentPlugins = list2.ToArray();
			ScheduledJob scheduledJob = this.JobFactory.CreateDiscoveryJob(discoveryConfiguration);
			if (scheduledJob == null)
			{
				CoreBusinessLayerService.log.WarnFormat("Cannot create Discovery Job for NodeID {0}", jobConfiguration.NodeId);
				return Guid.Empty;
			}
			Guid guid = this.JobFactory.SubmitScheduledJobToLocalEngine(Guid.Empty, scheduledJob, true);
			CoreBusinessLayerService.log.DebugFormat("Adding one time job with ID {0} into result cache", guid);
			DiscoveryResultCache.Instance.AddOrReplaceResult(new DiscoveryResultItem(guid, jobConfiguration.NodeId, cacheConfiguration));
			return guid;
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x00008AD8 File Offset: 0x00006CD8
		private AgentInfo TryGetAgentInfoAndUpdateConfiguration(Node node, List<Credential> credentials, DiscoveryConfiguration configuration)
		{
			AgentInfo agentInfo = this.TryGetAgentInfoFromNodeOrCredentials(node, credentials);
			if (agentInfo != null)
			{
				this.EnsureDiscoveryPluginsOnAgent(node, credentials, ref agentInfo);
				configuration.AgentAddress = agentInfo.AgentGuid.ToString();
				configuration.IsAgentJob = true;
				configuration.UseJsonFormat = agentInfo.UseJsonFormat;
			}
			return agentInfo;
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x00008B2C File Offset: 0x00006D2C
		private void EnsureDiscoveryPluginsOnAgent(Node node, List<Credential> credentials, ref AgentInfo agentInfo)
		{
			try
			{
				using (IEnumerator<IAgentPluginJobSupport> enumerator = DiscoveryHelper.GetOrderedDiscoveryPlugins().OfType<IOneTimeJobSupport>().OfType<IAgentPluginJobSupport>().GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						IAgentPluginJobSupport plugin = enumerator.Current;
						AgentPluginInfo agentPluginInfo = agentInfo.Plugins.SingleOrDefault((AgentPluginInfo ap) => ap.PluginId == plugin.PluginId);
						if (agentPluginInfo == null || !AgentInfo.PluginDeploymentFinishedStatuses.Contains(agentPluginInfo.Status))
						{
							CoreBusinessLayerService.log.DebugFormat("Found plugin '{0}' that is required for agent discovery but is missing on agent {1} ({2}) NodeId {3}", new object[]
							{
								plugin.PluginId,
								agentInfo.HostName,
								agentInfo.IPAddress,
								agentInfo.NodeID
							});
							Task<AgentDeploymentStatus> task = this.DeployAgentDiscoveryPluginsAsync(agentInfo.AgentId);
							TimeSpan agentDiscoveryPluginsDeploymentTimeLimit = BusinessLayerSettings.Instance.AgentDiscoveryPluginsDeploymentTimeLimit;
							if (!task.Wait(agentDiscoveryPluginsDeploymentTimeLimit))
							{
								CoreBusinessLayerService.log.WarnFormat("Plugin deployment on agent {0} ({1}) NodeId {2} hasn't finished in {3}.", new object[]
								{
									agentInfo.HostName,
									agentInfo.IPAddress,
									agentInfo.NodeID,
									agentDiscoveryPluginsDeploymentTimeLimit
								});
							}
							else if (task.Result == AgentDeploymentStatus.Finished)
							{
								CoreBusinessLayerService.log.DebugFormat("Plugin deployment on agent {0} ({1}) NodeId {2} finished successfuly.", agentInfo.HostName, agentInfo.IPAddress, agentInfo.NodeID);
							}
							else
							{
								CoreBusinessLayerService.log.WarnFormat("Plugin deployment on agent {0} ({1}) NodeId {2} finished with status {3}.", new object[]
								{
									agentInfo.HostName,
									agentInfo.IPAddress,
									agentInfo.NodeID,
									task.Result
								});
							}
							agentInfo = this.TryGetAgentInfoFromNodeOrCredentials(node, credentials);
							break;
						}
					}
				}
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.ErrorFormat("Error during EnsureDiscoveryPluginsOnAgent for agent {0} ({1}) NodeId {2}. {3}", new object[]
				{
					agentInfo.HostName,
					agentInfo.IPAddress,
					agentInfo.NodeID,
					ex
				});
			}
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x00008D58 File Offset: 0x00006F58
		private AgentInfo TryGetAgentInfoFromNodeOrCredentials(Node node, List<Credential> credentials)
		{
			if (credentials == null)
			{
				throw new ArgumentNullException("credentials");
			}
			AgentInfo agentInfo = null;
			AgentManagementCredential agentManagementCredential = credentials.OfType<AgentManagementCredential>().SingleOrDefault<AgentManagementCredential>();
			if (agentManagementCredential != null)
			{
				agentInfo = this._agentInfoDal.GetAgentInfo(agentManagementCredential.AgentId);
				if (agentInfo == null)
				{
					throw new InvalidOperationException(string.Format("No AgentManagement record found for AgentID {0}", agentManagementCredential.AgentId));
				}
			}
			if (agentInfo == null && node != null && node.NodeSubType == NodeSubType.Agent)
			{
				agentInfo = this._agentInfoDal.GetAgentInfoByNode(node.ID);
				if (agentInfo == null)
				{
					throw new InvalidOperationException(string.Format("No AgentManagement record found for NodeID {0}", node.ID));
				}
				AgentManagementCredential agentManagementCredential2 = new AgentManagementCredential();
				agentManagementCredential2.AgentId = agentInfo.AgentId;
				agentManagementCredential2.AgentGuid = agentInfo.AgentGuid;
				agentManagementCredential2.Plugins = (from p in agentInfo.Plugins
				select p.PluginId).ToArray<string>();
				agentManagementCredential = agentManagementCredential2;
				credentials.Add(agentManagementCredential);
			}
			return agentInfo;
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x00008E58 File Offset: 0x00007058
		private bool DoesPluginSupportAgent(IDiscoveryPlugin plugin, AgentInfo agentInfo, List<string> agentPlugins)
		{
			IAgentPluginJobSupport agentSupport = plugin as IAgentPluginJobSupport;
			if (agentSupport == null)
			{
				CoreBusinessLayerService.log.DebugFormat("Agent discovery plugin jobs not supported for {0} on agent {1} ({2}) NodeId {3}", new object[]
				{
					plugin,
					agentInfo.HostName,
					agentInfo.IPAddress,
					agentInfo.NodeID
				});
				return false;
			}
			if (agentPlugins.Contains(agentSupport.PluginId))
			{
				return true;
			}
			AgentPluginInfo agentPluginInfo = agentInfo.Plugins.SingleOrDefault((AgentPluginInfo ap) => ap.PluginId == agentSupport.PluginId);
			if (agentPluginInfo == null || agentPluginInfo.Status != 1)
			{
				CoreBusinessLayerService.log.WarnFormat("Agent plugin {0} on agent {1} ({2}) NodeId {3} not deployed for discovery. Plugin status: {4}. ", new object[]
				{
					agentSupport.PluginId,
					agentInfo.HostName,
					agentInfo.IPAddress,
					agentInfo.NodeID,
					(agentPluginInfo != null) ? agentPluginInfo.Status : 0
				});
				return false;
			}
			agentPlugins.Add(agentPluginInfo.PluginId);
			return true;
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x00008F5C File Offset: 0x0000715C
		public CreateDiscoveryJobResult CreateOrionDiscoveryJob(int profileID, bool executeImmediately)
		{
			CoreBusinessLayerService.log.DebugFormat("Creating discovery job for profile {0} where executeImmediately is {1}.", profileID, executeImmediately);
			DiscoveryConfiguration discoveryConfiguration = this.ServiceContainer.GetService<IDiscoveryDAL>().GetDiscoveryConfiguration(profileID);
			if (discoveryConfiguration == null)
			{
				throw new ArgumentNullException("configuration");
			}
			DiscoveryProfileEntry profileByID = DiscoveryProfileEntry.GetProfileByID(discoveryConfiguration.ProfileID.Value);
			if (profileByID == null)
			{
				throw new ArgumentNullException("profile");
			}
			if (discoveryConfiguration.Status.Status == 1 && discoveryConfiguration.JobID != Guid.Empty)
			{
				return CreateDiscoveryJobResult.UnableToChangeRunningJob;
			}
			if (profileByID.JobID != Guid.Empty)
			{
				CoreBusinessLayerService.log.DebugFormat("Deleting old job for profile {0}.", profileID);
				if (!this.JobFactory.DeleteJob(profileByID.JobID))
				{
					throw new CoreBusinessLayerService.DicoveryDeletingJobError(Resources.DiscoveryBL_DicoveryDeletingJobError, new object[]
					{
						discoveryConfiguration.JobID
					});
				}
				profileByID.JobID = Guid.Empty;
			}
			ScheduledJob scheduledJob = this.JobFactory.CreateDiscoveryJob(discoveryConfiguration);
			if (scheduledJob == null)
			{
				return CreateDiscoveryJobResult.JobCreated;
			}
			if (!executeImmediately)
			{
				if (discoveryConfiguration.CronSchedule != null)
				{
					profileByID.Status = new DiscoveryComplexStatus(5, string.Empty);
				}
				else if (discoveryConfiguration.ScheduleRunAtTime != DateTime.MinValue || discoveryConfiguration.ScheduleRunFrequency != TimeSpan.Zero)
				{
					int num = Convert.ToInt32(DateTime.Now.ToUniversalTime().TimeOfDay.TotalMinutes);
					int minutes = 0;
					if (!profileByID.ScheduleRunAtTime.Equals(DateTime.MinValue))
					{
						int num2 = Convert.ToInt32(profileByID.ScheduleRunAtTime.TimeOfDay.TotalMinutes);
						if (num < num2)
						{
							minutes = num2 - num;
						}
						else
						{
							minutes = 1440 - (num - num2);
						}
					}
					if (!discoveryConfiguration.ScheduleRunFrequency.Equals(TimeSpan.Zero))
					{
						minutes = profileByID.ScheduleRunFrequency;
					}
					scheduledJob.InitialWait = new TimeSpan(0, minutes, 0);
					profileByID.Status = new DiscoveryComplexStatus(5, string.Empty);
				}
				else
				{
					profileByID.Status = new DiscoveryComplexStatus(4, string.Empty);
				}
			}
			else
			{
				profileByID.Status = new DiscoveryComplexStatus(1, string.Empty);
			}
			if (profileByID.Status.Status != 4)
			{
				CoreBusinessLayerService.log.DebugFormat("Submiting job for profile {0}.", profileID);
				Guid jobID;
				try
				{
					jobID = this.JobFactory.SubmitScheduledJobToLocalEngine(Guid.Empty, scheduledJob, executeImmediately);
				}
				catch (FaultException ex)
				{
					CoreBusinessLayerService.log.Error(string.Format("Failed to create job for scheduled discovery profile {0}, rescheduler will keep trying", profileID), ex);
					this.parent.RunRescheduleEngineDiscoveryJobsTask(profileByID.EngineID);
					throw;
				}
				profileByID.JobID = jobID;
			}
			else
			{
				CoreBusinessLayerService.log.DebugFormat("No job for profile {0} will be created.", profileID);
				profileByID.JobID = Guid.Empty;
			}
			CoreBusinessLayerService.log.DebugFormat("Updating profile {0}.", profileID);
			profileByID.Update();
			CoreBusinessLayerService.log.DebugFormat("Job for profile {0} created.", profileID);
			return CreateDiscoveryJobResult.JobCreated;
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x00009260 File Offset: 0x00007460
		public OrionDiscoveryJobProgressInfo GetOrionDiscoveryJobProgress(int profileID)
		{
			OrionDiscoveryJobProgressInfo orionDiscoveryJobProgressInfo = OrionDiscoveryJobSchedulerEventsService.GetProgressInfo(profileID);
			if (orionDiscoveryJobProgressInfo == null)
			{
				orionDiscoveryJobProgressInfo = new OrionDiscoveryJobProgressInfo();
				DiscoveryProfileEntry profileByID = DiscoveryProfileEntry.GetProfileByID(profileID);
				orionDiscoveryJobProgressInfo.Status = profileByID.Status;
				orionDiscoveryJobProgressInfo.Starting = true;
				orionDiscoveryJobProgressInfo.IsAutoImport = profileByID.IsAutoImport;
			}
			if (orionDiscoveryJobProgressInfo.Status.Status == 3)
			{
				CoreBusinessLayerService.log.WarnFormat("GetOrionDiscoveryJobProgress(): Error status on profile Id {0}", profileID);
				throw new Exception("Error state received from discovery: " + orionDiscoveryJobProgressInfo.ToString());
			}
			return orionDiscoveryJobProgressInfo;
		}

		// Token: 0x060000FA RID: 250 RVA: 0x00008415 File Offset: 0x00006615
		[Obsolete("Method from old discovery", true)]
		public void CancelDiscovery(int profileID)
		{
		}

		// Token: 0x060000FB RID: 251 RVA: 0x000092E0 File Offset: 0x000074E0
		public List<DiscoveryResult> GetDiscoveryResultsList(DiscoveryNodeStatus status, DiscoveryResultsFilterType filterType, object filterValue, bool selectOnlyTopX, out bool thereIsMoreNodes, bool loadInterfacesAndVolumes)
		{
			CoreBusinessLayerService.log.DebugFormat("Sending request for results to DAL for status: {0}, filter type: {1}, filter: {2}.", status, filterType, (filterValue == null) ? "null" : filterValue);
			List<DiscoveryResult> discoveryResultsList = DiscoveryDatabase.GetDiscoveryResultsList(status, filterType, filterValue, selectOnlyTopX, ref thereIsMoreNodes, loadInterfacesAndVolumes);
			CoreBusinessLayerService.log.DebugFormat("Results recieved from DAL for status: {0}, filter type: {1}, filter: {2}.", status, filterType, (filterValue == null) ? "null" : filterValue);
			if (filterType == DiscoveryResultsFilterType.DiscoveredBy)
			{
				int profileId = Convert.ToInt32(filterValue);
				if (!discoveryResultsList.Any((DiscoveryResult item) => item.ProfileID == profileId))
				{
					DiscoveryResult item2 = new DiscoveryResult(profileId);
					discoveryResultsList.Add(item2);
				}
			}
			CoreBusinessLayerService.log.DebugFormat("Converting old discovery result to new one.", Array.Empty<object>());
			List<DiscoveryResult> result = this.ConvertScheduledDiscoveryResults(discoveryResultsList);
			CoreBusinessLayerService.log.DebugFormat("Converting old discovery result to new one finished.", Array.Empty<object>());
			CoreBusinessLayerService.log.DebugFormat("Sending list of results back for status: {0}, filter type: {1}, filter: {2}.", status, filterType, (filterValue == null) ? "null" : filterValue);
			return result;
		}

		// Token: 0x060000FC RID: 252 RVA: 0x000093E0 File Offset: 0x000075E0
		public DiscoveryNode GetVolumesAndInterfacesForDiscoveryNode(DiscoveryNode discoveryNode)
		{
			CoreBusinessLayerService.log.DebugFormat("Sending request for load interfaces and volumes to BL for nodeID: {0}", discoveryNode.NodeID);
			IEnumerable<IScheduledDiscoveryImport> enumerable = DiscoveryHelper.GetOrderedDiscoveryPlugins().OfType<IScheduledDiscoveryImport>();
			DiscoveryDatabase.LoadInterfacesAndVolumesForNode(discoveryNode, enumerable);
			CoreBusinessLayerService.log.DebugFormat("Request received for load interfaces and volumes to BL for nodeID: {0}", discoveryNode.NodeID);
			return discoveryNode;
		}

		// Token: 0x060000FD RID: 253 RVA: 0x00009434 File Offset: 0x00007634
		public int GetCountOfDiscoveryResults(DiscoveryNodeStatus status)
		{
			return DiscoveryNodeEntry.GetCountOfNodes(status);
		}

		// Token: 0x060000FE RID: 254 RVA: 0x0000943C File Offset: 0x0000763C
		public List<DateTime> GetDiscoveryResultListOfDates(DiscoveryNodeStatus status)
		{
			return DiscoveryNodeEntry.GetListOfDatesByStatus(status);
		}

		// Token: 0x060000FF RID: 255 RVA: 0x00009444 File Offset: 0x00007644
		public List<int> GetDiscoveryResultListOfProfiles(DiscoveryNodeStatus status)
		{
			return DiscoveryNodeEntry.GetListOfProfilesByStatus(status);
		}

		// Token: 0x06000100 RID: 256 RVA: 0x0000944C File Offset: 0x0000764C
		public List<string> GetDiscoveryResultListOfMachineTypes(DiscoveryNodeStatus status)
		{
			return DiscoveryNodeEntry.GetListOfMachineTypesByStatus(status);
		}

		// Token: 0x06000101 RID: 257 RVA: 0x00009454 File Offset: 0x00007654
		public void DeleteDiscoveryResultsByProfile(int profileID)
		{
			DiscoveryDatabase.DeleteResultsByProfile(profileID);
		}

		// Token: 0x06000102 RID: 258 RVA: 0x0000945C File Offset: 0x0000765C
		public DiscoveredObjectTreeWcfWrapper GetOneTimeJobResult(Guid jobId)
		{
			DiscoveryResultItem discoveryResultItem = null;
			if (DiscoveryResultCache.Instance.TryGetResultItem(jobId, ref discoveryResultItem) && discoveryResultItem != null && discoveryResultItem.ResultTree != null)
			{
				CoreBusinessLayerService.log.DebugFormat("Recieved one time job {0} result from cache", jobId);
				if (discoveryResultItem.CacheConfiguration != DiscoveryCacheConfiguration.DoNotUseCache && discoveryResultItem.nodeId != null)
				{
					CoreBusinessLayerService.log.DebugFormat("Storing the result into cache", Array.Empty<object>());
					this.PersistentDiscoveryCache.StoreResultForNode(discoveryResultItem.nodeId.Value, discoveryResultItem);
				}
				if (discoveryResultItem.nodeId != null && discoveryResultItem.isCached)
				{
					foreach (IOneTimeJobSupport oneTimeJobSupport in DiscoveryHelper.GetOrderedDiscoveryPlugins().OfType<IOneTimeJobSupport>())
					{
						try
						{
							oneTimeJobSupport.GetDiscoveredResourcesManagedStatus(discoveryResultItem.ResultTree, discoveryResultItem.nodeId.Value);
						}
						catch (Exception ex)
						{
							CoreBusinessLayerService.log.WarnFormat("Error occurred while updating selections in Resource tree with plugin {0}. Ex: {1}", oneTimeJobSupport.GetType(), ex);
						}
					}
				}
				DiscoveryResultCache.Instance.RemoveResult(jobId);
				return new DiscoveredObjectTreeWcfWrapper(discoveryResultItem.ResultTree, discoveryResultItem.timeOfCreation, discoveryResultItem.isCached);
			}
			return null;
		}

		// Token: 0x06000103 RID: 259 RVA: 0x000095A8 File Offset: 0x000077A8
		public OrionDiscoveryJobProgressInfo GetOneTimeJobProgress(Guid jobId)
		{
			DiscoveryResultItem discoveryResultItem = null;
			if (DiscoveryResultCache.Instance.TryGetResultItem(jobId, ref discoveryResultItem) && discoveryResultItem != null && discoveryResultItem.Progress != null)
			{
				CoreBusinessLayerService.log.DebugFormat("Recieved one time job {0} progress from cache", jobId);
				return discoveryResultItem.Progress;
			}
			return null;
		}

		// Token: 0x06000104 RID: 260 RVA: 0x000095F0 File Offset: 0x000077F0
		public Dictionary<string, int> ImportOneTimeJobResult(DiscoveredObjectTreeWcfWrapper treeOfSelection, int nodeId)
		{
			if (treeOfSelection == null)
			{
				throw new ArgumentNullException("treeOfSelection");
			}
			if (treeOfSelection.Tree == null)
			{
				throw new NullReferenceException("treeOfSelection::Tree");
			}
			CoreBusinessLayerService.log.DebugFormat("Importing List of Discovered Resources for node with id '{0}'", nodeId);
			DateTime now = DateTime.Now;
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			Action action = TechnologyPollingIndicator.AuditTechnologiesChanges(treeOfSelection.Tree.GetAllTreeObjectsOfType<IDiscoveredObjectWithTechnology>(), nodeId);
			foreach (IOneTimeJobSupport oneTimeJobSupport in DiscoveryHelper.GetOrderedDiscoveryPlugins().OfType<IOneTimeJobSupport>())
			{
				try
				{
					if (CoreBusinessLayerService.log.IsDebugEnabled)
					{
						CoreBusinessLayerService.log.DebugFormat("Updating List of Discovered Resources in plugin '{0}' for node with id '{1}'", oneTimeJobSupport.GetType(), nodeId);
					}
					Dictionary<string, int> dictionary2 = oneTimeJobSupport.UpdateDiscoveredResourcesManagedStatus(treeOfSelection.Tree, nodeId);
					if (dictionary2 != null && dictionary2.Count > 0)
					{
						foreach (KeyValuePair<string, int> keyValuePair in dictionary2)
						{
							dictionary.Add(keyValuePair.Key, keyValuePair.Value);
						}
					}
				}
				catch (Exception ex)
				{
					CoreBusinessLayerService.log.Error(string.Format("Unhandled exception occured when importing one time job result with plugin {0}", oneTimeJobSupport.GetType()), ex);
				}
			}
			action();
			CoreBusinessLayerService.log.DebugFormat("Completed updating of Discovered Resources for node with id '{0}'. Total execution time: {1} ms", nodeId, (DateTime.Now - now).Milliseconds);
			return dictionary;
		}

		// Token: 0x06000105 RID: 261 RVA: 0x0000978C File Offset: 0x0000798C
		public List<DiscoveryItemGroupDefinition> GetDiscoveryScheduledImportGroupDefinitions()
		{
			return (from n in DiscoveryHelper.GetOrderedDiscoveryPlugins().OfType<IScheduledDiscoveryImport>().SelectMany((IScheduledDiscoveryImport n) => n.GetScheduledDiscoveryObjectGroups())
			select new DiscoveryItemGroupDefinition
			{
				Group = n
			}).ToList<DiscoveryItemGroupDefinition>();
		}

		// Token: 0x06000106 RID: 262 RVA: 0x000097F0 File Offset: 0x000079F0
		public List<DiscoveryIgnoredNode> GetDiscoveryIgnoredNodes()
		{
			List<DiscoveryIgnoredNode> list = new List<DiscoveryIgnoredNode>();
			bool flag = ModuleManager.InstanceWithCache.IsThereModule("NPM");
			IDictionary<int, ICollection<DiscoveryIgnoredInterfaceEntry>> ignoredInterfacesDict = DiscoveryIgnoredInterfaceEntry.GetIgnoredInterfacesDict();
			IDictionary<int, ICollection<DiscoveryIgnoredVolumeEntry>> ignoredVolumesDict = DiscoveryIgnoredVolumeEntry.GetIgnoredVolumesDict();
			IEnumerable<IScheduledDiscoveryIgnore> enumerable = DiscoveryHelper.GetOrderedDiscoveryPlugins().OfType<IScheduledDiscoveryIgnore>().ToList<IScheduledDiscoveryIgnore>();
			foreach (DiscoveryIgnoredNodeEntry discoveryIgnoredNodeEntry in DiscoveryIgnoredNodeEntry.GetIgnoredNodesList())
			{
				DiscoveryIgnoredNode discoveryIgnoredNode = new DiscoveryIgnoredNode(discoveryIgnoredNodeEntry.ID, discoveryIgnoredNodeEntry.EngineID, discoveryIgnoredNodeEntry.IPAddress, discoveryIgnoredNodeEntry.Caption, discoveryIgnoredNodeEntry.IsIgnored, discoveryIgnoredNodeEntry.DateAdded);
				if (ignoredInterfacesDict.ContainsKey(discoveryIgnoredNodeEntry.ID))
				{
					foreach (DiscoveryIgnoredInterfaceEntry discoveryIgnoredInterfaceEntry in ignoredInterfacesDict[discoveryIgnoredNodeEntry.ID])
					{
						discoveryIgnoredNode.IgnoredInterfaces.Add(new DiscoveryIgnoredInterface(discoveryIgnoredInterfaceEntry.ID, discoveryIgnoredInterfaceEntry.IgnoredNodeID, discoveryIgnoredInterfaceEntry.PhysicalAddress, discoveryIgnoredInterfaceEntry.Description, discoveryIgnoredInterfaceEntry.Caption, discoveryIgnoredInterfaceEntry.Type, discoveryIgnoredInterfaceEntry.IfxName, discoveryIgnoredInterfaceEntry.DateAdded));
					}
				}
				if (ignoredVolumesDict.ContainsKey(discoveryIgnoredNodeEntry.ID))
				{
					foreach (DiscoveryIgnoredVolumeEntry discoveryIgnoredVolumeEntry in ignoredVolumesDict[discoveryIgnoredNodeEntry.ID])
					{
						discoveryIgnoredNode.IgnoredVolumes.Add(new DiscoveryIgnoredVolume(discoveryIgnoredVolumeEntry.ID, discoveryIgnoredVolumeEntry.IgnoredNodeID, discoveryIgnoredVolumeEntry.Description, discoveryIgnoredVolumeEntry.Type, discoveryIgnoredVolumeEntry.DateAdded));
					}
				}
				foreach (IScheduledDiscoveryIgnore scheduledDiscoveryIgnore in enumerable)
				{
					DiscoveryPluginResultBase discoveryPluginResultBase = scheduledDiscoveryIgnore.LoadIgnoredResults(discoveryIgnoredNodeEntry.ID);
					discoveryIgnoredNode.NodeResult.PluginResults.Add(discoveryPluginResultBase);
				}
				bool flag2;
				if (!flag && !discoveryIgnoredNodeEntry.IsIgnored && discoveryIgnoredNode.IgnoredVolumes.Count == 0)
				{
					flag2 = !discoveryIgnoredNode.NodeResult.PluginResults.Any((DiscoveryPluginResultBase n) => n.GetDiscoveredObjects().Any<IDiscoveredObject>());
				}
				else
				{
					flag2 = false;
				}
				if (!flag2)
				{
					list.Add(discoveryIgnoredNode);
				}
			}
			return list;
		}

		// Token: 0x06000107 RID: 263 RVA: 0x00009AA8 File Offset: 0x00007CA8
		public string AddDiscoveryIgnoredNode(DiscoveryNode discoveryNode)
		{
			int num = this.AddDiscoveryIgnoredNodeOnly(discoveryNode);
			if (num == -1)
			{
				CoreBusinessLayerService.log.ErrorFormat("Discovery Node(NodeID:{0},ProfileID:{1}) could not be ignored", discoveryNode.NodeID, discoveryNode.ProfileID);
				return string.Format(Resources.WEBCODE_ET_01, discoveryNode.Name);
			}
			if (!discoveryNode.IsSelected)
			{
				foreach (DiscoveryInterface discoveryInterface in from n in discoveryNode.Interfaces
				where n.IsSelected
				select n)
				{
					if (!this.AddDiscoveryIgnoredInterface(discoveryNode, discoveryInterface))
					{
						CoreBusinessLayerService.log.WarnFormat("Discovery Interface(InterfaceID:{0}) could not be ignored, because it is already ignored", discoveryInterface.InterfaceID);
					}
				}
				foreach (DiscoveryVolume discoveryVolume in from n in discoveryNode.Volumes
				where n.IsSelected
				select n)
				{
					if (!this.AddDiscoveryIgnoredVolume(discoveryNode, discoveryVolume))
					{
						CoreBusinessLayerService.log.WarnFormat("Discovery Volume(VolumeID:{0}) could not be ignored, because it is already ignored", discoveryVolume.VolumeID);
					}
				}
				DiscoveredNode discoveredNode = new DiscoveredNode();
				discoveredNode.IgnoredNodeId = new int?(num);
				discoveredNode.NodeID = discoveryNode.NodeID;
				discoveredNode.OrionNodeId = new int?(discoveryNode.ManagedNodeId);
				CoreDiscoveryPluginResult coreDiscoveryPluginResult = new CoreDiscoveryPluginResult();
				coreDiscoveryPluginResult.DiscoveredNodes = new List<DiscoveredNode>
				{
					discoveredNode
				};
				discoveryNode.NodeResult.PluginResults.Add(coreDiscoveryPluginResult);
				foreach (IScheduledDiscoveryIgnore scheduledDiscoveryIgnore in DiscoveryHelper.GetOrderedDiscoveryPlugins().OfType<IScheduledDiscoveryIgnore>())
				{
					scheduledDiscoveryIgnore.StoreResultsToIgnoreList(discoveryNode.NodeResult);
				}
			}
			return string.Empty;
		}

		// Token: 0x06000108 RID: 264 RVA: 0x00009CB0 File Offset: 0x00007EB0
		private int AddDiscoveryIgnoredNodeOnly(DiscoveryNode discoveryNode)
		{
			CoreBusinessLayerService.log.Debug("Sending request for insert ignored node to DAL.");
			int result;
			try
			{
				result = DiscoveryIgnoredNodeEntry.Insert(discoveryNode.EngineID, discoveryNode.IPAddress, discoveryNode.Name, discoveryNode.IsSelected, discoveryNode.ProfileID);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error when inserting ignored node: " + ex.ToString());
				throw new CoreBusinessLayerService.DiscoveryInsertingIgnoredNodeError(Resources.DiscoveryBL_DiscoveryInsertingIgnoredNodeError, new object[]
				{
					discoveryNode.IPAddress
				}, ex);
			}
			return result;
		}

		// Token: 0x06000109 RID: 265 RVA: 0x00009D3C File Offset: 0x00007F3C
		public bool AddDiscoveryIgnoredNodesNewDiscovery(IEnumerable<DiscoveredNode> discoveredNodes)
		{
			IEnumerable<IScheduledDiscoveryIgnore> enumerable = DiscoveryHelper.GetOrderedDiscoveryPlugins().OfType<IScheduledDiscoveryIgnore>().ToList<IScheduledDiscoveryIgnore>();
			IDictionary<int, int> dictionary = new Dictionary<int, int>();
			bool flag = ModuleManager.InstanceWithCache.IsThereModule("NPM");
			foreach (DiscoveredNode discoveredNode in discoveredNodes)
			{
				CoreBusinessLayerService.log.Debug("Sending request for insert ignored node to DAL.");
				try
				{
					int num;
					if (dictionary.ContainsKey(discoveredNode.ProfileID))
					{
						num = dictionary[discoveredNode.ProfileID];
					}
					else
					{
						num = DiscoveryProfileEntry.GetProfileByID(discoveredNode.ProfileID).EngineID;
						dictionary[discoveredNode.ProfileID] = num;
					}
					string text = IPAddressHelper.ToStringIp(discoveredNode.IP);
					string displayName = discoveredNode.DisplayName;
					int num2 = DiscoveryIgnoredNodeEntry.Insert(num, text, displayName, true, discoveredNode.ProfileID);
					foreach (DiscoveryIgnoredDAL.VolumeInfo volumeInfo in DiscoveryIgnoredDAL.GetDiscoveredVolumesForNode(discoveredNode))
					{
						DiscoveryIgnoredVolumeEntry.Insert(num, text, displayName, volumeInfo.VolumeDescription, volumeInfo.VolumeType, discoveredNode.NodeID, discoveredNode.ProfileID, num2);
					}
					if (flag)
					{
						foreach (DiscoveryIgnoredDAL.InterfaceInfo interfaceInfo in DiscoveryIgnoredDAL.GetDiscoveredInterfacesForNode(discoveredNode))
						{
							DiscoveryIgnoredInterfaceEntry.Insert(num, text, displayName, interfaceInfo.PhysicalAddress, interfaceInfo.InterfaceName, interfaceInfo.InterfaceName, interfaceInfo.InterfaceType, interfaceInfo.IfName, discoveredNode.NodeID, discoveredNode.ProfileID, num2);
						}
					}
					DiscoveryResultBase discoveryResultBase = new DiscoveryResultBase();
					CoreDiscoveryPluginResult coreDiscoveryPluginResult = new CoreDiscoveryPluginResult();
					coreDiscoveryPluginResult.DiscoveredNodes = new List<DiscoveredNode>
					{
						discoveredNode
					};
					discoveredNode.IgnoredNodeId = new int?(num2);
					discoveryResultBase.PluginResults.Add(coreDiscoveryPluginResult);
					foreach (IScheduledDiscoveryIgnore scheduledDiscoveryIgnore in enumerable)
					{
						DiscoveryPluginResultBase discoveryPluginResultBase = scheduledDiscoveryIgnore.LoadResults(discoveredNode.ProfileID, discoveredNode.NodeID);
						discoveryResultBase.PluginResults.Add(discoveryPluginResultBase);
						scheduledDiscoveryIgnore.StoreResultsToIgnoreList(discoveryResultBase);
					}
				}
				catch (Exception ex)
				{
					CoreBusinessLayerService.log.Error("Error when inserting ignored node: " + ex.ToString());
					throw new CoreBusinessLayerService.DiscoveryInsertingIgnoredNodeError(Resources.DiscoveryBL_DiscoveryInsertingIgnoredNodeError, new object[]
					{
						IPAddressHelper.ToStringIp(discoveredNode.IP)
					}, ex);
				}
			}
			return true;
		}

		// Token: 0x0600010A RID: 266 RVA: 0x0000A034 File Offset: 0x00008234
		public bool AddDiscoveryIgnoredNodeNewDiscovery(DiscoveredNode discoveredNode)
		{
			CoreBusinessLayerService.log.Debug("Sending request for insert ignored node to DAL.");
			bool result;
			try
			{
				int engineID = DiscoveryProfileEntry.GetProfileByID(discoveredNode.ProfileID).EngineID;
				string text = IPAddressHelper.ToStringIp(discoveredNode.IP);
				string displayName = discoveredNode.DisplayName;
				int num = DiscoveryIgnoredNodeEntry.Insert(engineID, text, displayName, true, discoveredNode.ProfileID);
				bool flag = num != -1;
				foreach (DiscoveryIgnoredDAL.VolumeInfo volumeInfo in DiscoveryIgnoredDAL.GetDiscoveredVolumesForNode(discoveredNode))
				{
					DiscoveryIgnoredVolumeEntry.Insert(engineID, text, displayName, volumeInfo.VolumeDescription, volumeInfo.VolumeType, discoveredNode.NodeID, discoveredNode.ProfileID, num);
				}
				if (ModuleManager.InstanceWithCache.IsThereModule("NPM"))
				{
					foreach (DiscoveryIgnoredDAL.InterfaceInfo interfaceInfo in DiscoveryIgnoredDAL.GetDiscoveredInterfacesForNode(discoveredNode))
					{
						DiscoveryIgnoredInterfaceEntry.Insert(engineID, text, displayName, interfaceInfo.PhysicalAddress, interfaceInfo.InterfaceName, interfaceInfo.InterfaceName, interfaceInfo.InterfaceType, interfaceInfo.IfName, discoveredNode.NodeID, discoveredNode.ProfileID, num);
					}
				}
				DiscoveryResultBase discoveryResultBase = new DiscoveryResultBase();
				CoreDiscoveryPluginResult coreDiscoveryPluginResult = new CoreDiscoveryPluginResult();
				coreDiscoveryPluginResult.DiscoveredNodes = new List<DiscoveredNode>
				{
					discoveredNode
				};
				discoveredNode.IgnoredNodeId = new int?(num);
				discoveryResultBase.PluginResults.Add(coreDiscoveryPluginResult);
				foreach (IScheduledDiscoveryIgnore scheduledDiscoveryIgnore in DiscoveryHelper.GetOrderedDiscoveryPlugins().OfType<IScheduledDiscoveryIgnore>())
				{
					DiscoveryPluginResultBase discoveryPluginResultBase = scheduledDiscoveryIgnore.LoadResults(discoveredNode.ProfileID, discoveredNode.NodeID);
					discoveryResultBase.PluginResults.Add(discoveryPluginResultBase);
					scheduledDiscoveryIgnore.StoreResultsToIgnoreList(discoveryResultBase);
				}
				result = flag;
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error when inserting ignored node: " + ex.ToString());
				throw new CoreBusinessLayerService.DiscoveryInsertingIgnoredNodeError(Resources.DiscoveryBL_DiscoveryInsertingIgnoredNodeError, new object[]
				{
					IPAddressHelper.ToStringIp(discoveredNode.IP)
				}, ex);
			}
			return result;
		}

		// Token: 0x0600010B RID: 267 RVA: 0x0000A298 File Offset: 0x00008498
		public void DeleteDiscoveryIgnoredNodes(IEnumerable<DiscoveryIgnoredNode> nodes)
		{
			if (nodes == null)
			{
				throw new ArgumentNullException("nodes");
			}
			IEnumerable<IScheduledDiscoveryIgnore> enumerable = DiscoveryHelper.GetOrderedDiscoveryPlugins().OfType<IScheduledDiscoveryIgnore>().ToList<IScheduledDiscoveryIgnore>();
			List<int> list = new List<int>();
			List<int> list2 = new List<int>();
			foreach (DiscoveryIgnoredNode discoveryIgnoredNode in nodes)
			{
				if (discoveryIgnoredNode != null)
				{
					foreach (DiscoveryIgnoredVolume discoveryIgnoredVolume in from n in discoveryIgnoredNode.IgnoredVolumes
					where n.IsSelected
					select n)
					{
						this.DeleteDiscoveryIgnoredVolume(discoveryIgnoredVolume);
					}
					foreach (DiscoveryIgnoredInterface discoveryIgnoredInterface in from n in discoveryIgnoredNode.IgnoredInterfaces
					where n.IsSelected
					select n)
					{
						this.DeleteDiscoveryIgnoredInterface(discoveryIgnoredInterface);
					}
					bool flag = true;
					foreach (IScheduledDiscoveryIgnore scheduledDiscoveryIgnore in enumerable)
					{
						scheduledDiscoveryIgnore.RemoveResultsFromIgnoreList(discoveryIgnoredNode.NodeResult);
						if (flag && scheduledDiscoveryIgnore.LoadIgnoredResults(discoveryIgnoredNode.ID).GetDiscoveredObjects().Any<IDiscoveredObject>())
						{
							flag = false;
						}
					}
					if (flag)
					{
						list2.Add(discoveryIgnoredNode.ID);
					}
					else
					{
						list.Add(discoveryIgnoredNode.ID);
					}
				}
			}
			if (list2.Count > 0)
			{
				DiscoveryIgnoredNodeEntry.DeleteByListID(list2);
			}
			if (list.Count > 0)
			{
				DiscoveryIgnoredNodeEntry.DisableIsIgnoredList(list);
			}
		}

		// Token: 0x0600010C RID: 268 RVA: 0x0000A4BC File Offset: 0x000086BC
		public void DeleteDiscoveryIgnoredNode(DiscoveryIgnoredNode node)
		{
			if (node == null)
			{
				throw new ArgumentNullException("node");
			}
			foreach (DiscoveryIgnoredVolume discoveryIgnoredVolume in from n in node.IgnoredVolumes
			where n.IsSelected
			select n)
			{
				this.DeleteDiscoveryIgnoredVolume(discoveryIgnoredVolume);
			}
			foreach (DiscoveryIgnoredInterface discoveryIgnoredInterface in from n in node.IgnoredInterfaces
			where n.IsSelected
			select n)
			{
				this.DeleteDiscoveryIgnoredInterface(discoveryIgnoredInterface);
			}
			bool flag = true;
			foreach (IScheduledDiscoveryIgnore scheduledDiscoveryIgnore in DiscoveryHelper.GetOrderedDiscoveryPlugins().OfType<IScheduledDiscoveryIgnore>())
			{
				scheduledDiscoveryIgnore.RemoveResultsFromIgnoreList(node.NodeResult);
				if (flag && scheduledDiscoveryIgnore.LoadIgnoredResults(node.ID).GetDiscoveredObjects().Any<IDiscoveredObject>())
				{
					flag = false;
				}
			}
			if (flag)
			{
				DiscoveryIgnoredNodeEntry.DeleteByID(node.ID);
				return;
			}
			if (node.IsSelected && node.IsIgnored)
			{
				DiscoveryIgnoredNodeEntry.DisableIsIgnored(node.ID);
			}
		}

		// Token: 0x0600010D RID: 269 RVA: 0x0000A638 File Offset: 0x00008838
		[Obsolete("Core-Split cleanup. If you need this member please contact Core team", true)]
		public void DeleteDiscoveryIgnoredNode(int ignoredNodeID)
		{
			CoreBusinessLayerService.log.Debug("Sending request for delete to DAL.");
			try
			{
				DiscoveryIgnoredNodeEntry.DeleteByID(ignoredNodeID);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error when deleting ignored node: " + ex.ToString());
				throw new CoreBusinessLayerService.DiscoveryDeletingIgnoredNodeError(Resources.DiscoveryBL_DiscoveryDeletingIgnoredNodeError, new object[]
				{
					ignoredNodeID
				}, ex);
			}
		}

		// Token: 0x0600010E RID: 270 RVA: 0x0000A6A4 File Offset: 0x000088A4
		public void RemoveDiscoveryNodeFromIgnored(DiscoveryNode discoveryNode)
		{
			CoreBusinessLayerService.log.Debug("Sending request for delete to DAL.");
			try
			{
				DiscoveryIgnoredNodeEntry.DeleteByKeyColums(discoveryNode.EngineID, discoveryNode.IPAddress, discoveryNode.Name);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error when deleting ignored node: " + ex.ToString());
				throw new CoreBusinessLayerService.DiscoveryDeletingIgnoredNodeError(Resources.DiscoveryBL_DiscoveryDeletingIgnoredNodeError_Engine, new object[]
				{
					discoveryNode.EngineID,
					discoveryNode.IPAddress
				}, ex);
			}
		}

		// Token: 0x0600010F RID: 271 RVA: 0x0000A730 File Offset: 0x00008930
		public bool AddDiscoveryIgnoredInterface(DiscoveryNode discoveryNode, DiscoveryInterface discoveryInterface)
		{
			CoreBusinessLayerService.log.Debug("Sending request for insert ignored interface to DAL.");
			bool result;
			try
			{
				result = DiscoveryIgnoredInterfaceEntry.Insert(discoveryNode.EngineID, discoveryNode.IPAddress, discoveryNode.Name, discoveryInterface.PhysicalAddress, discoveryInterface.InterfaceDescription, discoveryInterface.InterfaceCaption, discoveryInterface.InterfaceType, discoveryInterface.IfxName, discoveryNode.NodeID, discoveryNode.ProfileID, 0);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error when inserting ignored interface: " + ex.ToString());
				throw new CoreBusinessLayerService.DiscoveryInsertingIgnoredInterfaceError(Resources.DiscoveryBL_DiscoveryInsertingIgnoredInterfaceError, new object[]
				{
					discoveryNode.IPAddress
				}, ex);
			}
			return result;
		}

		// Token: 0x06000110 RID: 272 RVA: 0x0000A7D8 File Offset: 0x000089D8
		public bool DeleteDiscoveryIgnoredInterface(DiscoveryIgnoredInterface discoveryIgnoredInterface)
		{
			CoreBusinessLayerService.log.Debug("Sending request for delete ignored interface to DAL.");
			bool result;
			try
			{
				result = DiscoveryIgnoredInterfaceEntry.DeleteByID(discoveryIgnoredInterface.ID);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error when deleting ignored interface: " + ex.ToString());
				throw new CoreBusinessLayerService.DiscoveryDeletingIgnoredInterfaceError(Resources.DiscoveryBL_DiscoveryDeletingIgnoredInterfaceError, new object[]
				{
					discoveryIgnoredInterface.Description
				}, ex);
			}
			return result;
		}

		// Token: 0x06000111 RID: 273 RVA: 0x0000A84C File Offset: 0x00008A4C
		public bool AddDiscoveryIgnoredVolume(DiscoveryNode discoveryNode, DiscoveryVolume discoveryVolume)
		{
			CoreBusinessLayerService.log.Debug("Sending request for insert ignored volume to DAL.");
			bool result;
			try
			{
				result = DiscoveryIgnoredVolumeEntry.Insert(discoveryNode.EngineID, discoveryNode.IPAddress, discoveryNode.Name, discoveryVolume.VolumeDescription, discoveryVolume.VolumeType, discoveryNode.NodeID, discoveryNode.ProfileID, 0);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error when inserting ignored volume: " + ex.ToString());
				throw new CoreBusinessLayerService.DiscoveryInsertingIgnoredVolumeError(Resources.DiscoveryBL_DiscoveryInsertingIgnoredVolumeError, new object[]
				{
					discoveryNode.IPAddress
				}, ex);
			}
			return result;
		}

		// Token: 0x06000112 RID: 274 RVA: 0x0000A8E4 File Offset: 0x00008AE4
		public bool DeleteDiscoveryIgnoredVolume(DiscoveryIgnoredVolume discoveryIgnoredVolume)
		{
			CoreBusinessLayerService.log.Debug("Sending request for delete ignored volume to DAL.");
			bool result;
			try
			{
				result = DiscoveryIgnoredVolumeEntry.DeleteByID(discoveryIgnoredVolume.ID);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error when deleting ignored volume: " + ex.ToString());
				throw new CoreBusinessLayerService.DiscoveryDeletingIgnoredVolumeError(Resources.DiscoveryBL_DiscoveryDeletingIgnoredVolumeError, new object[]
				{
					discoveryIgnoredVolume.Description
				}, ex);
			}
			return result;
		}

		// Token: 0x06000113 RID: 275 RVA: 0x0000A958 File Offset: 0x00008B58
		public string ValidateBulkList(IEnumerable<string> bulkList)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (string hostNameOrAddress in HostListNormalizer.NormalizeHostNames(bulkList))
			{
				this.ValidateHostAddress(hostNameOrAddress, stringBuilder);
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06000114 RID: 276 RVA: 0x0000A9BC File Offset: 0x00008BBC
		public List<Subnet> FindRouterSubnets(string router, List<SnmpEntry> credentials, int engineId, out string errorMessage)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (!this.ValidateHostAddress(router, stringBuilder))
			{
				errorMessage = stringBuilder.ToString();
				return null;
			}
			errorMessage = null;
			string text = Dns.GetHostAddresses(router).FirstOrDefault((IPAddress ipaddress) => ipaddress.AddressFamily == AddressFamily.InterNetwork).ToString();
			if (text == null)
			{
				CoreBusinessLayerService.log.Error(string.Format("IP address for host {0} is missing", router));
				throw new CoreBusinessLayerService.DiscoveryHostAddressMissingError(Resources.DiscoveryBL_DiscoveryHostAddressMissingError, new object[]
				{
					router
				});
			}
			Dictionary<string, string> dictionary;
			SNMPHelper.SNMPQueryForIp(text, "1.3.6.1.2.1.4.21.1.11", credentials, "getsubtree", out dictionary);
			List<Subnet> list = new List<Subnet>();
			foreach (KeyValuePair<string, string> keyValuePair in dictionary)
			{
				uint num;
				if (!(keyValuePair.Value == "255.255.255.255") && !(keyValuePair.Value == "0.0.0.0") && !(keyValuePair.Key == "127.0.0.0") && HostHelper.IsIpAddress(keyValuePair.Key) && HostHelper.IsIpAddress(keyValuePair.Value) && Subnet.GetSubnetClass(keyValuePair.Key, ref num) != null)
				{
					Subnet item = new Subnet(keyValuePair.Key, keyValuePair.Value);
					list.Add(item);
				}
			}
			if (list.Count == 0)
			{
				errorMessage = Resources.CODE_VB0_1;
				return null;
			}
			return list;
		}

		// Token: 0x06000115 RID: 277 RVA: 0x0000AB3C File Offset: 0x00008D3C
		[Obsolete("There is no longer a sigle license count value.  Use FeatureManager to get the license limits for each element type.", true)]
		public int GetLicenseCount()
		{
			return 0;
		}

		// Token: 0x06000116 RID: 278 RVA: 0x0000AB3F File Offset: 0x00008D3F
		public void ValidateProfilesTimeout()
		{
			new List<DiscoveryProfileEntry>(DiscoveryProfileEntry.GetAllProfiles()).ForEach(delegate(DiscoveryProfileEntry profile)
			{
				if (DateTime.MinValue != profile.LastRun && (DateTime.Now - profile.LastRun.ToLocalTime()).TotalMinutes > (double)profile.JobTimeout && profile.Status.Status == 1)
				{
					CoreBusinessLayerService.log.Warn(string.Format("Discovery profile {0} end during timeout {1}", profile.ProfileID, profile.JobTimeout));
					profile.Status = new DiscoveryComplexStatus(3, "LIBCODE_TM0_25");
					profile.Update();
				}
			});
		}

		// Token: 0x06000117 RID: 279 RVA: 0x0000AB6F File Offset: 0x00008D6F
		public Intervals GetSettingsPollingIntervals()
		{
			return DiscoveryDAL.GetSettingsPollingIntervals();
		}

		// Token: 0x06000118 RID: 280 RVA: 0x0000AB76 File Offset: 0x00008D76
		public List<SnmpEntry> GetAllCredentials()
		{
			return DiscoveryDAL.GetAllCredentials();
		}

		// Token: 0x06000119 RID: 281 RVA: 0x0000AB7D File Offset: 0x00008D7D
		internal bool UpdateDiscoveryJobs(int engineId)
		{
			return this.UpdateSelectedDiscoveryJobs(null, engineId);
		}

		// Token: 0x0600011A RID: 282 RVA: 0x0000AB88 File Offset: 0x00008D88
		public void UpdateSelectedDiscoveryJobs(List<int> profileIdsFilter)
		{
			int currentOperationEngineId = this.GetCurrentOperationEngineId();
			this.UpdateSelectedDiscoveryJobs(profileIdsFilter, currentOperationEngineId);
		}

		// Token: 0x0600011B RID: 283 RVA: 0x0000ABA8 File Offset: 0x00008DA8
		private bool UpdateSelectedDiscoveryJobs(List<int> profileIdsFilter, int engineId)
		{
			bool result;
			try
			{
				CoreBusinessLayerService.log.Debug("Updating scheduled discovery jobs.");
				string text;
				if (profileIdsFilter != null && profileIdsFilter.Count == 0)
				{
					result = true;
				}
				else if (this.TryConnectionWithJobSchedulerV2(out text))
				{
					IEnumerable<DiscoveryProfileEntry> allProfiles = DiscoveryProfileEntry.GetAllProfiles();
					CoreBusinessLayerService.log.Debug("Filtering old profiles");
					List<DiscoveryProfileEntry> list = (from p in allProfiles
					where p.SIPPort == 0
					select p).ToList<DiscoveryProfileEntry>();
					if (profileIdsFilter != null)
					{
						list = (from p in list
						where profileIdsFilter.Contains(p.ProfileID)
						select p).ToList<DiscoveryProfileEntry>();
					}
					foreach (DiscoveryProfileEntry discoveryProfileEntry in list)
					{
						DiscoveryConfiguration discoveryConfiguration = this.ServiceContainer.GetService<IDiscoveryDAL>().GetDiscoveryConfiguration(discoveryProfileEntry.ProfileID);
						if (discoveryConfiguration.EngineId == engineId && discoveryProfileEntry.IsScheduled)
						{
							this.UpdateDiscoveryJob(discoveryProfileEntry, discoveryConfiguration);
						}
					}
					result = true;
				}
				else
				{
					CoreBusinessLayerService.log.WarnFormat("Can't update scheduled jobs, JobScheduler is not running. - {0}", text);
					result = false;
				}
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Unhandled exception occured when rescheduling discovery jobs", ex);
				result = false;
			}
			return result;
		}

		// Token: 0x0600011C RID: 284 RVA: 0x0000AD44 File Offset: 0x00008F44
		private void UpdateDiscoveryJob(DiscoveryProfileEntry profile, DiscoveryConfiguration configuration)
		{
			CoreBusinessLayerService.log.DebugFormat("Updating discovery job. ProfileId={0}.", profile.ProfileID);
			if (configuration == null)
			{
				CoreBusinessLayerService.log.ErrorFormat("Discovery Configuration wasn't found. ProfileId={0}", profile.ProfileID);
				return;
			}
			ScheduledJob scheduledJob = this.JobFactory.CreateDiscoveryJob(configuration);
			if (scheduledJob == null)
			{
				return;
			}
			scheduledJob.InitialWait = CoreBusinessLayerService.CalculateJobInitialWait(profile);
			CoreBusinessLayerService.log.DebugFormat("Submiting job for profile {0}. CronExpression={1}, Frequency={2}, InitialWait={3}", new object[]
			{
				profile.ProfileID,
				scheduledJob.CronExpression,
				scheduledJob.Frequency,
				scheduledJob.InitialWait
			});
			Guid guid = this.JobFactory.SubmitScheduledJobToLocalEngine(profile.JobID, scheduledJob, false);
			if (guid != profile.JobID)
			{
				CoreBusinessLayerService.log.DebugFormat("Updating profile, ProfileId={0}.", profile.ProfileID);
				profile.JobID = guid;
				profile.Status = new DiscoveryComplexStatus(5, string.Empty);
				profile.Update();
			}
			CoreBusinessLayerService.log.DebugFormat("Discovery job was updated successfully, ProfileId={0}.", profile.ProfileID);
		}

		// Token: 0x0600011D RID: 285 RVA: 0x0000AE64 File Offset: 0x00009064
		private static TimeSpan CalculateJobInitialWait(DiscoveryProfileEntry profile)
		{
			int num = Convert.ToInt32(DateTime.Now.ToUniversalTime().TimeOfDay.TotalMinutes);
			int minutes = 0;
			if (!profile.ScheduleRunAtTime.Equals(DateTime.MinValue))
			{
				int num2 = Convert.ToInt32(profile.ScheduleRunAtTime.TimeOfDay.TotalMinutes);
				if (num < num2)
				{
					minutes = num2 - num;
				}
				else
				{
					minutes = 1440 - (num - num2);
				}
			}
			if (profile.ScheduleRunFrequency != 0)
			{
				minutes = profile.ScheduleRunFrequency;
			}
			return new TimeSpan(0, minutes, 0);
		}

		// Token: 0x0600011E RID: 286 RVA: 0x0000AEF8 File Offset: 0x000090F8
		private bool ValidateHostAddress(string hostNameOrAddress, StringBuilder errors)
		{
			try
			{
				Dns.GetHostAddresses(hostNameOrAddress);
				return true;
			}
			catch (ArgumentOutOfRangeException)
			{
				errors.AppendFormat(Resources.LIBCODE_TM0_12 + "<br />", hostNameOrAddress);
			}
			catch (ArgumentException)
			{
				errors.AppendFormat(Resources.LIBCODE_TM0_13 + "<br />", hostNameOrAddress);
			}
			catch (Exception)
			{
				errors.AppendFormat(Resources.LIBCODE_TM0_14 + "<br/>", hostNameOrAddress);
			}
			return false;
		}

		// Token: 0x0600011F RID: 287 RVA: 0x0000AF8C File Offset: 0x0000918C
		internal void ForceDiscoveryPluginsToLoadTypes()
		{
			CoreBusinessLayerService.log.Debug("Start loading plugins known types");
			IList<IDiscoveryPlugin> orderedDiscoveryPlugins = DiscoveryHelper.GetOrderedDiscoveryPlugins();
			CoreBusinessLayerService.log.DebugFormat("Number of found plugins:", orderedDiscoveryPlugins.Count);
			Type[] array = orderedDiscoveryPlugins.SelectMany((IDiscoveryPlugin plugin) => plugin.GetKnownTypes()).ToArray<Type>();
			CoreBusinessLayerService.log.DebugFormat("Number of found known types:", array.Length);
			CoreBusinessLayerService.log.Debug("Finish loading plugins known types");
		}

		// Token: 0x06000120 RID: 288 RVA: 0x00008415 File Offset: 0x00006615
		[Obsolete("Core-Split cleanup. If you need this member please contact Core team", true)]
		public void StartDiscoveryImport(int profileId)
		{
		}

		// Token: 0x06000121 RID: 289 RVA: 0x0000B019 File Offset: 0x00009219
		[Obsolete("Core-Split cleanup. If you need this member please contact Core team", true)]
		public void StoreDiscoveryConfiguration(DiscoveryConfiguration configuration)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000122 RID: 290 RVA: 0x0000B019 File Offset: 0x00009219
		[Obsolete("Core-Split cleanup. If you need this member please contact Core team", true)]
		public DiscoveryConfiguration LoadDiscoveryConfiguration(int profileID)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000123 RID: 291 RVA: 0x0000B020 File Offset: 0x00009220
		public DiscoveryResultBase GetDiscoveryResult(int profileId)
		{
			return DiscoveryResultManager.GetDiscoveryResult(profileId, DiscoveryHelper.GetOrderedDiscoveryPlugins());
		}

		// Token: 0x06000124 RID: 292 RVA: 0x0000B02D File Offset: 0x0000922D
		public DiscoveryImportProgressInfo GetOrionDiscoveryImportProgress(Guid importID)
		{
			return DiscoveryImportManager.GetImportProgress(importID);
		}

		// Token: 0x06000125 RID: 293 RVA: 0x0000B038 File Offset: 0x00009238
		public StartImportStatus ImportOrionDiscoveryResults(Guid importId, DiscoveryResultBase result)
		{
			SortedDictionary<int, List<IDiscoveryPlugin>> orderedPlugins = DiscoveryPluginHelper.GetOrderedPlugins(DiscoveryHelper.GetOrderedDiscoveryPlugins(), DiscoveryHelper.GetDiscoveryPluginInfos());
			return DiscoveryImportManager.StartImport(importId, result, orderedPlugins, false, delegate(DiscoveryResultBase _result, Guid importJobID, StartImportStatus StartImportStatus)
			{
				try
				{
					DiscoveryLogs discoveryLogs = new DiscoveryLogs();
					DiscoveryImportManager.FillDiscoveryLogEntity(discoveryLogs, _result, StartImportStatus);
					discoveryLogs.AutoImport = false;
					using (CoreSwisContext coreSwisContext = SwisContextFactory.CreateSystemContext())
					{
						discoveryLogs.Create(coreSwisContext);
					}
					CoreBusinessLayerService.log.InfoFormat("DiscoveryLog created for ProfileID:{0}", discoveryLogs.ProfileID);
				}
				catch (Exception ex)
				{
					CoreBusinessLayerService.log.Error("Unable to create discovery import log", ex);
				}
			});
		}

		// Token: 0x06000126 RID: 294 RVA: 0x0000B080 File Offset: 0x00009280
		public int CreateOrionDiscoveryProfileFromConfigurationStrings(DiscoveryConfiguration configurationWithoutPluginInformation, List<string> discoveryPluginConfigurationBaseItems)
		{
			foreach (DiscoveryPluginConfigurationBase discoveryPluginConfigurationBase in this.discoveryLogic.DeserializePluginConfigurationItems(discoveryPluginConfigurationBaseItems))
			{
				configurationWithoutPluginInformation.PluginConfigurations.Add(discoveryPluginConfigurationBase);
			}
			return this.CreateOrionDiscoveryProfile(configurationWithoutPluginInformation, true);
		}

		// Token: 0x06000127 RID: 295 RVA: 0x0000B0E0 File Offset: 0x000092E0
		protected int CreateOrionDiscoveryProfile(DiscoveryConfiguration configuration, bool refreshCredentialsFromDb)
		{
			CoreBusinessLayerService.log.Debug("Creating new discovery profile.");
			List<int> list;
			int num = this.ServiceContainer.GetService<IDiscoveryDAL>().CreateNewConfiguration(configuration, refreshCredentialsFromDb, ref list);
			if (list.Count > 0)
			{
				this.RescheduleJobsWithUsingCredentials(list, num);
			}
			CoreBusinessLayerService.log.DebugFormat("Discovery profile {0} was successfully created.", num);
			return num;
		}

		// Token: 0x06000128 RID: 296 RVA: 0x0000B138 File Offset: 0x00009338
		public int CreateOrionDiscoveryProfile(DiscoveryConfiguration configuration)
		{
			return this.CreateOrionDiscoveryProfile(configuration, false);
		}

		// Token: 0x06000129 RID: 297 RVA: 0x0000B144 File Offset: 0x00009344
		public void UpdateOrionDiscoveryProfile(DiscoveryConfiguration configuration)
		{
			if (configuration.ProfileID != null)
			{
				CoreBusinessLayerService.log.DebugFormat("Updating configuration for profile {0}", configuration.ProfileID);
			}
			else
			{
				CoreBusinessLayerService.log.Warn("Trying to update configuration for profile with no ID");
			}
			List<int> list = this.ServiceContainer.GetService<IDiscoveryDAL>().StoreDiscoveryConfiguration(configuration);
			if (list.Count > 0)
			{
				this.RescheduleJobsWithUsingCredentials(list, configuration.ProfileId.Value);
			}
			CoreBusinessLayerService.log.DebugFormat("Configuration for profile {0} updated.", configuration.ProfileID);
		}

		// Token: 0x0600012A RID: 298 RVA: 0x0000B1D8 File Offset: 0x000093D8
		private void RescheduleJobsWithUsingCredentials(List<int> usedCredentials, int excludedProfileId)
		{
			List<int> list = (from id in this.GetProfileIDsUsingCredentials(usedCredentials)
			where id != excludedProfileId
			select id).ToList<int>();
			CoreBusinessLayerService.log.InfoFormat("Rescheduling discovery profiles [{0}] because credential it is refefencing were changed.", string.Join(", ", (from id in list
			select id.ToString()).ToArray<string>()));
			new DiscoveryJobRescheduler().RescheduleJobsForProfiles(list);
		}

		// Token: 0x0600012B RID: 299 RVA: 0x0000B260 File Offset: 0x00009460
		public List<int> GetProfileIDsUsingCredentials(List<int> credentialIdList)
		{
			if (credentialIdList == null)
			{
				throw new ArgumentNullException("credentialIdList");
			}
			List<int> result = new List<int>();
			if (credentialIdList.Count == 0)
			{
				return result;
			}
			IDiscoveryDAL service = this.ServiceContainer.GetService<IDiscoveryDAL>();
			List<int> allProfileIDs = service.GetAllProfileIDs();
			List<DiscoveryConfiguration> list = new List<DiscoveryConfiguration>();
			foreach (int num in allProfileIDs)
			{
				list.Add(service.GetDiscoveryConfiguration(num));
			}
			using (List<DiscoveryConfiguration>.Enumerator enumerator2 = list.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					DiscoveryConfiguration configuration = enumerator2.Current;
					Action<int> <>9__0;
					foreach (ICredentialStorage credentialStorage in configuration.PluginConfigurations.OfType<ICredentialStorage>())
					{
						List<int> list2 = credentialStorage.GetCredentialList().ToList<int>();
						Action<int> action;
						if ((action = <>9__0) == null)
						{
							action = (<>9__0 = delegate(int c)
							{
								if (credentialIdList.Contains(c) && !result.Contains(configuration.ProfileId.Value))
								{
									result.Add(configuration.ProfileId.Value);
								}
							});
						}
						list2.ForEach(action);
					}
				}
			}
			return result;
		}

		// Token: 0x0600012C RID: 300 RVA: 0x0000B3DC File Offset: 0x000095DC
		public void DeleteOrionDiscoveryProfile(int profileID)
		{
			this.discoveryLogic.DeleteOrionDiscoveryProfile(profileID);
		}

		// Token: 0x0600012D RID: 301 RVA: 0x0000B3EA File Offset: 0x000095EA
		public void DeleteHiddenOrionDiscoveryProfilesByName(string profileName)
		{
			this.discoveryLogic.DeleteHiddenOrionDiscoveryProfilesByName(profileName);
		}

		// Token: 0x0600012E RID: 302 RVA: 0x0000B3F8 File Offset: 0x000095F8
		public DiscoveryConfiguration GetOrionDiscoveryConfigurationByProfile(int profileID)
		{
			return this.ServiceContainer.GetService<IDiscoveryDAL>().GetDiscoveryConfiguration(profileID);
		}

		// Token: 0x0600012F RID: 303 RVA: 0x0000B40C File Offset: 0x0000960C
		public void CancelOrionDiscovery(int profileID)
		{
			DiscoveryProfileEntry profileByID = DiscoveryProfileEntry.GetProfileByID(profileID);
			if (profileByID.Status.Status == 1 && profileByID.JobID != Guid.Empty)
			{
				using (IJobSchedulerHelper localInstance = JobScheduler.GetLocalInstance())
				{
					CoreBusinessLayerService.log.DebugFormat("Checking if job {0} exists in is really running", profileByID.JobID);
					OrionDiscoveryJobProgressInfo progressInfo = OrionDiscoveryJobSchedulerEventsService.GetProgressInfo(profileID);
					if (progressInfo == null)
					{
						string text = "An error has occurred during Network Discovery cancellation: there are no Network Discovery jobs to cancel.\r\nThis error may be due to either a lost database connection or a business layer fault condition.";
						profileByID.Status = new DiscoveryComplexStatus(3, "WEBDATA_TP0_ERROR_DURING_DISCOVERY");
						profileByID.Update();
						CoreBusinessLayerService.log.ErrorFormat("Job {0}: {1}", profileByID.JobID, text);
						throw new CoreBusinessLayerService.DiscoveryJobCancellationError(Resources.DiscoveryBL_DiscoveryJobCancellationError, new object[]
						{
							profileByID.JobID
						});
					}
					CoreBusinessLayerService.log.DebugFormat("Cancelling job {0}", profileByID.JobID);
					try
					{
						OrionDiscoveryJobSchedulerEventsService.CancelDiscoveryJob(profileByID.ProfileID);
						localInstance.CancelJob(profileByID.JobID);
						profileByID.Status = new DiscoveryComplexStatus(7, "WEBDATA_TP0_DISCOVERY_CANCELLED_BY_USER");
						profileByID.Update();
					}
					catch (Exception ex)
					{
						string text2 = "An error has occurred during Network Discovery cancellation: " + ex.Message;
						profileByID.Status = new DiscoveryComplexStatus(3, "WEBDATA_TP0_ERROR_DURING_DISCOVER_NO_JOB");
						profileByID.Update();
						if (progressInfo != null)
						{
							progressInfo.Status = new DiscoveryComplexStatus(3, "WEBDATA_TP0_ERROR_DURING_DISCOVER_NO_JOB");
						}
						CoreBusinessLayerService.log.ErrorFormat("Job {0}: {1}", profileByID.JobID, text2);
						throw;
					}
				}
			}
		}

		// Token: 0x06000130 RID: 304 RVA: 0x0000B5B0 File Offset: 0x000097B0
		public bool TryConnectionWithJobSchedulerV2(out string errorMessage)
		{
			bool result;
			try
			{
				using (IJobSchedulerHelper localInstance = JobScheduler.GetLocalInstance())
				{
					localInstance.PolicyExists("Nothing");
					errorMessage = string.Empty;
					result = true;
				}
			}
			catch (Exception ex)
			{
				errorMessage = string.Format("{0}: {1}", ex.GetType().Name, ex.Message);
				result = false;
			}
			return result;
		}

		// Token: 0x06000131 RID: 305 RVA: 0x0000B624 File Offset: 0x00009824
		public List<SnmpCredentialsV2> GetSharedSnmpV2Credentials(string owner)
		{
			List<SnmpCredentialsV2> result = new List<SnmpCredentialsV2>();
			try
			{
				result = new CredentialManager().GetCredentials<SnmpCredentialsV2>(owner).ToList<SnmpCredentialsV2>();
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Unhandled exception occured when loading shared credentials.", ex);
			}
			return result;
		}

		// Token: 0x06000132 RID: 306 RVA: 0x0000B670 File Offset: 0x00009870
		public List<SnmpCredentialsV3> GetSharedSnmpV3Credentials(string owner)
		{
			List<SnmpCredentialsV3> result = new List<SnmpCredentialsV3>();
			try
			{
				result = new CredentialManager().GetCredentials<SnmpCredentialsV3>(owner).ToList<SnmpCredentialsV3>();
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Unhandled exception occured when loading shared credentials.", ex);
			}
			return result;
		}

		// Token: 0x06000133 RID: 307 RVA: 0x0000B6BC File Offset: 0x000098BC
		public List<UsernamePasswordCredential> GetSharedWmiCredentials(string owner)
		{
			List<UsernamePasswordCredential> result = new List<UsernamePasswordCredential>();
			try
			{
				result = new CredentialManager().GetCredentials<UsernamePasswordCredential>(owner).ToList<UsernamePasswordCredential>();
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Unhandled exception occured when loading shared credentials.", ex);
			}
			return result;
		}

		// Token: 0x06000134 RID: 308 RVA: 0x0000B708 File Offset: 0x00009908
		public Dictionary<string, int> GetElementsManagedCount()
		{
			return LicenseSaturationLogic.GetElementsManagedCount();
		}

		// Token: 0x06000135 RID: 309 RVA: 0x0000B710 File Offset: 0x00009910
		public Dictionary<string, int> GetAlreadyManagedElementCount(List<DiscoveryResultBase> discoveryResults, IList<IDiscoveryPlugin> plugins)
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			foreach (IDiscoveryPlugin discoveryPlugin in plugins)
			{
				if (discoveryPlugin is IGetManagedElements)
				{
					Dictionary<string, int> pluginAlreadyManaged = ((IGetManagedElements)discoveryPlugin).GetAlreadyManagedElements(discoveryResults);
					dictionary = pluginAlreadyManaged.Concat(from kvp in dictionary
					where !pluginAlreadyManaged.ContainsKey(kvp.Key)
					select kvp).ToDictionary((KeyValuePair<string, int> v) => v.Key.ToLower(), (KeyValuePair<string, int> v) => v.Value);
				}
			}
			return dictionary;
		}

		// Token: 0x06000136 RID: 310 RVA: 0x0000B7E0 File Offset: 0x000099E0
		public Dictionary<string, int> GetAlreadyManagedElementCount(List<DiscoveryResultBase> discoveryResults)
		{
			IList<IDiscoveryPlugin> orderedDiscoveryPlugins = DiscoveryHelper.GetOrderedDiscoveryPlugins();
			return this.GetAlreadyManagedElementCount(discoveryResults, orderedDiscoveryPlugins);
		}

		// Token: 0x06000137 RID: 311 RVA: 0x0000B7FB File Offset: 0x000099FB
		public DiscoveryResultBase FilterIgnoredItems(DiscoveryResultBase discoveryResult)
		{
			return this.discoveryLogic.FilterIgnoredItems(discoveryResult);
		}

		// Token: 0x06000138 RID: 312 RVA: 0x0000B80C File Offset: 0x00009A0C
		public List<DiscoveryResult> ConvertScheduledDiscoveryResults(List<DiscoveryResult> scheduledResults)
		{
			List<DiscoveryResult> list = new ScheduledDiscoveryResultConvertor().ConvertScheduledDiscoveryResults(scheduledResults);
			foreach (DiscoveryResult discoveryResult in list)
			{
				DiscoveryFilterResultByTechnology.FilterByPriority(discoveryResult, TechnologyManager.Instance);
				List<DiscoveryPluginResultBase> list2 = discoveryResult.PluginResults.ToList<DiscoveryPluginResultBase>();
				discoveryResult.PluginResults.Clear();
				foreach (DiscoveryPluginResultBase discoveryPluginResultBase in list2)
				{
					discoveryResult.PluginResults.Add(discoveryPluginResultBase.GetFilteredPluginResult());
				}
			}
			return list;
		}

		// Token: 0x06000139 RID: 313 RVA: 0x0000B8D0 File Offset: 0x00009AD0
		public void RequestScheduledDiscoveryNetObjectStatusUpdateAsync()
		{
			DiscoveryNetObjectStatusManager.Instance.RequestUpdateAsync(null, TimeSpan.Zero);
		}

		// Token: 0x0600013A RID: 314 RVA: 0x0000B8E4 File Offset: 0x00009AE4
		public void ImportDiscoveryResultForProfile(int profileID, bool deleteProfileAfterImport)
		{
			this.discoveryLogic.ImportDiscoveryResultForProfile(profileID, deleteProfileAfterImport, null, false, null);
		}

		// Token: 0x0600013B RID: 315 RVA: 0x0000B909 File Offset: 0x00009B09
		public Guid ImportDiscoveryResultsForConfiguration(DiscoveryImportConfiguration importCfg)
		{
			Guid importID = Guid.NewGuid();
			ThreadPool.QueueUserWorkItem(delegate(object callback)
			{
				try
				{
					this.discoveryLogic.ImportDiscoveryResultsForConfiguration(importCfg, importID);
				}
				catch (Exception ex)
				{
					CoreBusinessLayerService.log.Error("Error in ImportDiscoveryResultsForConfiguration", ex);
				}
			});
			return importID;
		}

		// Token: 0x0600013C RID: 316 RVA: 0x0000B940 File Offset: 0x00009B40
		public ValidationResult ValidateActiveDirectoryAccess(ActiveDirectoryAccess access)
		{
			if (access == null)
			{
				throw new ArgumentNullException("access");
			}
			if (access.Credential == null)
			{
				UsernamePasswordCredential credential = new CredentialManager().GetCredential<UsernamePasswordCredential>(access.CredentialID);
				access.Credential = credential;
			}
			return new ActiveDirectoryDiscovery(access.HostName, access.Credential).ValidateConnection();
		}

		// Token: 0x0600013D RID: 317 RVA: 0x0000B991 File Offset: 0x00009B91
		public List<OrganizationalUnit> GetActiveDirectoryOrganizationUnits(ActiveDirectoryAccess access)
		{
			return this.GetActiveDirectoryDiscovery(access).GetAllOrganizationalUnits().ToList<OrganizationalUnit>();
		}

		// Token: 0x0600013E RID: 318 RVA: 0x0000B9A4 File Offset: 0x00009BA4
		public List<OrganizationalUnitCountOfComputers> GetCountOfComputers(ActiveDirectoryAccess access)
		{
			return this.GetActiveDirectoryDiscovery(access).GetCountOfStationsInAD();
		}

		// Token: 0x0600013F RID: 319 RVA: 0x0000B9B4 File Offset: 0x00009BB4
		private ActiveDirectoryDiscovery GetActiveDirectoryDiscovery(ActiveDirectoryAccess access)
		{
			if (access == null)
			{
				throw new ArgumentNullException("access");
			}
			if (access.Credential == null)
			{
				UsernamePasswordCredential credential = new CredentialManager().GetCredential<UsernamePasswordCredential>(access.CredentialID);
				access.Credential = credential;
			}
			return new ActiveDirectoryDiscovery(access.HostName, access.Credential);
		}

		// Token: 0x06000140 RID: 320 RVA: 0x0000BA00 File Offset: 0x00009C00
		public IPAddress GetHostAddress(string hostName, AddressFamily preferredAddressFamily)
		{
			JobDescription jobDescription = GetHostAddressJob.CreateJobDescription(hostName, BusinessLayerSettings.Instance.TestJobTimeout);
			string text;
			GetHostAddressJobResult getHostAddressJobResult = this.ExecuteJobAndGetResult<GetHostAddressJobResult>(jobDescription, null, JobResultDataFormatType.Xml, "GetHostAddressJob", out text);
			if (!getHostAddressJobResult.Success)
			{
				throw new ResolveHostAddressException("Can not resolve IP address for host " + hostName + ".");
			}
			IPAddress hostAddress = CommonHelper.GetHostAddress(from item in getHostAddressJobResult.IpAddresses
			select IPAddress.Parse(item), preferredAddressFamily, null);
			CoreBusinessLayerService.log.InfoFormat(string.Format("IPAddress for host {0} is {1}", hostName, hostAddress), Array.Empty<object>());
			return hostAddress;
		}

		// Token: 0x06000141 RID: 321 RVA: 0x0000BA9C File Offset: 0x00009C9C
		public void CleanOneTimeJobResults()
		{
			foreach (IOneTimeJobCleanup oneTimeJobCleanup in DiscoveryHelper.GetOrderedDiscoveryPlugins().OfType<IOneTimeJobCleanup>())
			{
				try
				{
					CoreBusinessLayerService.log.DebugFormat("Cleaning one time job results within plugin '{0}'", oneTimeJobCleanup.GetType());
					oneTimeJobCleanup.CleanOneTimeJobResults();
				}
				catch (Exception ex)
				{
					CoreBusinessLayerService.log.Error(string.Format("Exception occured when cleaning one time job results within plugin {0}", oneTimeJobCleanup.GetType()), ex);
				}
			}
		}

		// Token: 0x06000142 RID: 322 RVA: 0x0000BB30 File Offset: 0x00009D30
		public Dictionary<string, Dictionary<int, string>> GetEngines()
		{
			return EngineDAL.GetEngines();
		}

		// Token: 0x06000143 RID: 323 RVA: 0x0000BB37 File Offset: 0x00009D37
		public void DeleteEngine(int engineID)
		{
			EngineDAL.DeleteEngine(engineID);
		}

		// Token: 0x06000144 RID: 324 RVA: 0x0000BB3F File Offset: 0x00009D3F
		[Obsolete("Core-Split cleanup. If you need this member please contact Core team", true)]
		public string GetCurrentEngineIPAddress()
		{
			return EngineDAL.GetEngineIpAddressByServerName(Environment.MachineName);
		}

		// Token: 0x06000145 RID: 325 RVA: 0x0000BB4B File Offset: 0x00009D4B
		public DataTable GetEventTypesTable()
		{
			return EventsWebDAL.GetEventTypesTable();
		}

		// Token: 0x06000146 RID: 326 RVA: 0x0000BB52 File Offset: 0x00009D52
		public DataTable GetEventsTable(GetEventsParameter param)
		{
			return EventsWebDAL.GetEventsTable(param);
		}

		// Token: 0x06000147 RID: 327 RVA: 0x0000BB5A File Offset: 0x00009D5A
		public DataTable GetEvents(GetEventsParameter param)
		{
			return EventsWebDAL.GetEvents(param);
		}

		// Token: 0x06000148 RID: 328 RVA: 0x0000BB62 File Offset: 0x00009D62
		public void AcknowledgeEvents(List<int> events)
		{
			EventsWebDAL.AcknowledgeEvents(events);
		}

		// Token: 0x06000149 RID: 329 RVA: 0x0000BB6A File Offset: 0x00009D6A
		public DataTable GetEventSummaryTable(int netObjectID, string netObjectType, DateTime fromDate, DateTime toDate, List<int> limitationIDs)
		{
			return EventsWebDAL.GetEventSummaryTable(netObjectID, netObjectType, fromDate, toDate, limitationIDs);
		}

		// Token: 0x0600014A RID: 330 RVA: 0x0000BB78 File Offset: 0x00009D78
		public bool Blow(bool generateException, string exceptionType, string message)
		{
			if (!generateException)
			{
				return true;
			}
			Exception ex;
			if (CoreBusinessLayerService.TryGetException(exceptionType, message, out ex))
			{
				throw ex;
			}
			return false;
		}

		// Token: 0x0600014B RID: 331 RVA: 0x0000BB98 File Offset: 0x00009D98
		private static bool TryGetException(string exceptionType, string message, out Exception exception)
		{
			bool result;
			try
			{
				if (string.Equals(exceptionType, "SolarWinds.Orion.Core.Common.CoreFaultContract", StringComparison.OrdinalIgnoreCase))
				{
					exception = MessageUtilities.NewFaultException<CoreFaultContract>(new ApplicationException(message));
				}
				else
				{
					Type type = Type.GetType(exceptionType);
					exception = (Exception)Activator.CreateInstance(type, new object[]
					{
						message
					});
				}
				result = true;
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Failed to fake exception. Returning false.", ex);
				exception = null;
				result = false;
			}
			return result;
		}

		// Token: 0x0600014C RID: 332 RVA: 0x0000BC10 File Offset: 0x00009E10
		public List<JobEngineInfo> EnumerateJobEngines()
		{
			return JobEngineDAL.EnumerateJobEngine();
		}

		// Token: 0x0600014D RID: 333 RVA: 0x0000BC17 File Offset: 0x00009E17
		public JobEngineInfo GetEngine(int engineId)
		{
			return JobEngineDAL.GetEngine(engineId);
		}

		// Token: 0x0600014E RID: 334 RVA: 0x0000BC1F File Offset: 0x00009E1F
		[Obsolete("Use GetEngine method. Obsolete since Core 2018.2 Pacman.")]
		public JobEngineInfo GetEngineWithPollingSettings(int engineId)
		{
			return JobEngineDAL.GetEngineWithPollingSettings(engineId);
		}

		// Token: 0x0600014F RID: 335 RVA: 0x0000BC27 File Offset: 0x00009E27
		public int GetEngineIdForNetObject(string netObject)
		{
			return JobEngineDAL.GetEngineIdForNetObject(netObject);
		}

		// Token: 0x06000150 RID: 336 RVA: 0x0000BC30 File Offset: 0x00009E30
		internal IIndication RemoveNetObjectInternal(string netobject)
		{
			if (string.IsNullOrEmpty(netobject))
			{
				return null;
			}
			string[] array = netobject.Split(new char[]
			{
				':'
			});
			if (array.Length != 2)
			{
				return null;
			}
			try
			{
				int num;
				if (int.TryParse(array[1], out num))
				{
					if (array[0].Equals("N", StringComparison.OrdinalIgnoreCase))
					{
						foreach (Volume volume in VolumeDAL.GetNodeVolumes(num))
						{
							this.DeleteVolume(volume);
						}
						this.DeleteNode(num);
					}
					else if (array[0].Equals("V", StringComparison.OrdinalIgnoreCase))
					{
						Volume volume2 = VolumeDAL.GetVolume(num);
						if (volume2 != null)
						{
							this.DeleteVolume(volume2);
						}
					}
					else if (array[0].Equals("I", StringComparison.OrdinalIgnoreCase) && this.AreInterfacesSupported)
					{
						Interface @interface = new Interface
						{
							InterfaceID = num
						};
						return new InterfaceIndication(1, @interface);
					}
				}
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error(ex);
				throw;
			}
			return null;
		}

		// Token: 0x06000151 RID: 337 RVA: 0x0000BD44 File Offset: 0x00009F44
		public void RemoveNetObjects(List<string> netObjectIds)
		{
			List<IIndication> indications = new List<IIndication>(netObjectIds.Count);
			netObjectIds.ForEach(delegate(string nodeId)
			{
				IIndication indication = this.RemoveNetObjectInternal(nodeId);
				if (indication != null)
				{
					indications.Add(indication);
				}
			});
			if (indications.Count > 0)
			{
				IndicationPublisher.CreateV3().ReportIndications(indications);
			}
		}

		// Token: 0x06000152 RID: 338 RVA: 0x0000BDA0 File Offset: 0x00009FA0
		public void RemoveNetObject(string netObjectId)
		{
			IIndication indication = this.RemoveNetObjectInternal(netObjectId);
			if (indication != null)
			{
				IndicationPublisher.CreateV3().ReportIndication(indication);
			}
		}

		// Token: 0x06000153 RID: 339 RVA: 0x0000BDC3 File Offset: 0x00009FC3
		private static void BizLayerErrorHandler(Exception ex)
		{
			CoreBusinessLayerService.log.Error("Exception occurred when communication with VIM Business Layer");
		}

		// Token: 0x06000154 RID: 340 RVA: 0x0000BDD4 File Offset: 0x00009FD4
		public void PollNow(string netObjectId)
		{
			using (IPollingControllerServiceHelper instance = PollingController.GetInstance())
			{
				instance.PollNow(netObjectId);
			}
		}

		// Token: 0x06000155 RID: 341 RVA: 0x0000BE0C File Offset: 0x0000A00C
		public void JobNowByJobKey(string netObjectId, string jobKey)
		{
			JobExecutionCondition jobExecutionCondition = new JobExecutionCondition
			{
				EntityIdentifier = netObjectId,
				JobKey = jobKey
			};
			using (IPollingControllerServiceHelper instance = PollingController.GetInstance())
			{
				instance.JobNow(jobExecutionCondition);
			}
		}

		// Token: 0x06000156 RID: 342 RVA: 0x0000BE58 File Offset: 0x0000A058
		public string PollNodeNow(string netObjectId)
		{
			string empty = string.Empty;
			using (IPollingControllerServiceHelper instance = PollingController.GetInstance())
			{
				instance.PollNow(netObjectId);
			}
			return empty;
		}

		// Token: 0x06000157 RID: 343 RVA: 0x0000BE98 File Offset: 0x0000A098
		public void CancelNow(string netObjectId)
		{
			using (IPollingControllerServiceHelper instance = PollingController.GetInstance())
			{
				instance.CancelJob(new JobExecutionCondition
				{
					JobType = 7,
					EntityIdentifier = netObjectId
				});
			}
		}

		// Token: 0x06000158 RID: 344 RVA: 0x0000BEE0 File Offset: 0x0000A0E0
		public void Rediscover(string netObjectId)
		{
			using (IPollingControllerServiceHelper instance = PollingController.GetInstance())
			{
				instance.RediscoverNow(netObjectId);
			}
		}

		// Token: 0x06000159 RID: 345 RVA: 0x0000BF18 File Offset: 0x0000A118
		public void RefreshSettingsFromDatabase()
		{
			new SettingsToRegistry().Synchronize();
		}

		// Token: 0x0600015A RID: 346 RVA: 0x0000BF24 File Offset: 0x0000A124
		public void ApplyPollingIntervals(int nodePollInterval, int interfacePollInterval, int volumePollInterval, int rediscoveryInterval)
		{
			CoreBusinessLayerService.log.ErrorFormat("NotImplemented ApplyPollingIntervals", Array.Empty<object>());
		}

		// Token: 0x0600015B RID: 347 RVA: 0x0000BF3A File Offset: 0x0000A13A
		public void ApplyStatPollingIntervals(int nodePollInterval, int interfacePollInterval, int volumePollInterval)
		{
			CoreBusinessLayerService.log.ErrorFormat("NotImplemented ApplyStatPollingIntervals", Array.Empty<object>());
		}

		// Token: 0x0600015C RID: 348 RVA: 0x0000BF50 File Offset: 0x0000A150
		public int UpdateNodesPollingEngine(int engineId, int[] nodeIds)
		{
			return JobEngineDAL.UpdateNodesPollingEngine(engineId, nodeIds);
		}

		// Token: 0x0600015D RID: 349 RVA: 0x0000BF5C File Offset: 0x0000A15C
		public string GetLicenseSWID()
		{
			IProductLicense[] activeLicenses = LicenseManager.GetInstance().GetActiveLicenses(true);
			IProductLicense productLicense = activeLicenses.FirstOrDefault((IProductLicense l) => l.LicenseType != 1 && l.LicenseType > 0);
			if (productLicense == null)
			{
				productLicense = activeLicenses.FirstOrDefault<IProductLicense>();
			}
			return productLicense.CustomerId;
		}

		// Token: 0x0600015E RID: 350 RVA: 0x0000BFAC File Offset: 0x0000A1AC
		public bool ActivateOfflineLicense(string fileNamePath)
		{
			if (string.IsNullOrEmpty(fileNamePath))
			{
				throw new ArgumentNullException("fileNamePath");
			}
			if (!File.Exists(fileNamePath))
			{
				CoreBusinessLayerService.log.DebugFormat("File {0} doesn't exists.", fileNamePath);
				return false;
			}
			string text = File.ReadAllText(fileNamePath);
			bool result;
			using (IInformationServiceProxy2 informationServiceProxy = SwisConnectionProxyPool.GetSystemCreator().Create())
			{
				PropertyBag propertyBag = informationServiceProxy.Invoke<PropertyBag>("Orion.Licensing.Licenses", "ActivateOffline", new object[]
				{
					text
				});
				if (propertyBag.ContainsKey("Success"))
				{
					result = Convert.ToBoolean(propertyBag["Success"]);
				}
				else
				{
					result = false;
				}
			}
			return result;
		}

		// Token: 0x0600015F RID: 351 RVA: 0x0000C054 File Offset: 0x0000A254
		public SolarWinds.Orion.Core.Common.Models.Mib.Oid GetOid(string oidValue)
		{
			return this.mibDAL.GetOid(oidValue);
		}

		// Token: 0x06000160 RID: 352 RVA: 0x0000C062 File Offset: 0x0000A262
		public bool IsMibDatabaseAvailable()
		{
			return this.mibDAL.IsMibDatabaseAvailable();
		}

		// Token: 0x06000161 RID: 353 RVA: 0x0000C06F File Offset: 0x0000A26F
		public Oids GetChildOids(string parentOid)
		{
			return this.mibDAL.GetChildOids(parentOid);
		}

		// Token: 0x06000162 RID: 354 RVA: 0x0000C07D File Offset: 0x0000A27D
		public MemoryStream GetIcon(string oid)
		{
			return this.mibDAL.GetIcon(oid);
		}

		// Token: 0x06000163 RID: 355 RVA: 0x0000C08B File Offset: 0x0000A28B
		public Dictionary<string, MemoryStream> GetIcons()
		{
			return this.mibDAL.GetIcons();
		}

		// Token: 0x06000164 RID: 356 RVA: 0x0000C098 File Offset: 0x0000A298
		public Oids GetSearchingOidsByDescription(string searchCriteria, string searchMIBsCriteria)
		{
			return this.mibDAL.GetSearchingOidsByDescription(searchCriteria, searchMIBsCriteria);
		}

		// Token: 0x06000165 RID: 357 RVA: 0x0000C0A7 File Offset: 0x0000A2A7
		public Oids GetSearchingOidsByName(string searchCriteria)
		{
			return this.mibDAL.GetSearchingOidsByName(searchCriteria);
		}

		// Token: 0x06000166 RID: 358 RVA: 0x0000C0B5 File Offset: 0x0000A2B5
		public void CancelRunningCommand()
		{
			this.mibDAL.CancelRunningCommand();
		}

		// Token: 0x06000167 RID: 359 RVA: 0x0000C0C2 File Offset: 0x0000A2C2
		public bool IsModuleInstalled(string moduleTag)
		{
			return ModulesCollector.IsModuleInstalled(moduleTag);
		}

		// Token: 0x06000168 RID: 360 RVA: 0x0000C0CA File Offset: 0x0000A2CA
		public bool IsModuleInstalledbyTabName(string moduleTabName)
		{
			return ModulesCollector.IsModuleInstalledbyTabName(moduleTabName);
		}

		// Token: 0x06000169 RID: 361 RVA: 0x0000C0D2 File Offset: 0x0000A2D2
		public List<ModuleInfo> GetInstalledModules()
		{
			return ModulesCollector.GetInstalledModules();
		}

		// Token: 0x0600016A RID: 362 RVA: 0x0000C0D9 File Offset: 0x0000A2D9
		public List<ModuleLicenseInfo> GetModuleLicenseInformation()
		{
			return ModuleLicenseInfoProvider.GetModuleLicenseInformation();
		}

		// Token: 0x0600016B RID: 363 RVA: 0x0000C0E0 File Offset: 0x0000A2E0
		public Version GetModuleVersion(string moduleTag)
		{
			return ModulesCollector.GetModuleVersion(moduleTag);
		}

		// Token: 0x0600016C RID: 364 RVA: 0x0000C0E8 File Offset: 0x0000A2E8
		public List<ModuleLicenseSaturationInfo> GetModuleSaturationInformation()
		{
			return LicenseSaturationLogic.GetModulesSaturationInfo(new int?(Settings.LicenseSaturationPercentage));
		}

		// Token: 0x0600016D RID: 365 RVA: 0x0000C0F9 File Offset: 0x0000A2F9
		public List<Node> GetNetworkDevices(CorePageType pageType, List<int> limitationIDs)
		{
			return NetworkDeviceDAL.Instance.GetNetworkDevices(pageType, limitationIDs);
		}

		// Token: 0x0600016E RID: 366 RVA: 0x0000C107 File Offset: 0x0000A307
		public Dictionary<int, string> GetNetworkDeviceNamesForPage(CorePageType pageType, List<int> limitationIDs)
		{
			return NetworkDeviceDAL.Instance.GetNetworkDeviceNamesForPage(pageType, limitationIDs);
		}

		// Token: 0x0600016F RID: 367 RVA: 0x0000C115 File Offset: 0x0000A315
		public Dictionary<int, string> GetDeviceNamesForPage(CorePageType pageType, List<int> limitationIDs, bool includeBasic)
		{
			return NetworkDeviceDAL.Instance.GetNetworkDeviceNamesForPage(pageType, limitationIDs, includeBasic);
		}

		// Token: 0x06000170 RID: 368 RVA: 0x0000C124 File Offset: 0x0000A324
		public Dictionary<string, string> GetNetworkDeviceTypes(List<int> limitationIDs)
		{
			return NetworkDeviceDAL.Instance.GetNetworkDeviceTypes(limitationIDs);
		}

		// Token: 0x06000171 RID: 369 RVA: 0x0000C131 File Offset: 0x0000A331
		public List<string> GetAllVendors(List<int> limitationIDs)
		{
			return NetworkDeviceDAL.Instance.GetAllVendors(limitationIDs);
		}

		// Token: 0x06000172 RID: 370 RVA: 0x0000C140 File Offset: 0x0000A340
		private static MaintenanceRenewalItem DalToWfc(MaintenanceRenewalItemDAL dal)
		{
			if (dal == null)
			{
				return null;
			}
			return new MaintenanceRenewalItem(dal.Id, dal.Title, dal.Description, dal.CreatedAt, dal.Ignored, dal.Url, dal.AcknowledgedAt, dal.AcknowledgedBy, dal.ProductTag, dal.DateReleased, dal.NewVersion);
		}

		// Token: 0x06000173 RID: 371 RVA: 0x0000C19C File Offset: 0x0000A39C
		public MaintenanceRenewalItem GetMaintenanceRenewalNotificationItem(Guid renewalId)
		{
			CoreBusinessLayerService.log.Debug("Sending request for MaintenanceRenewalItemDAL.GetItemById.");
			MaintenanceRenewalItem result;
			try
			{
				result = CoreBusinessLayerService.DalToWfc(MaintenanceRenewalItemDAL.GetItemById(renewalId));
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error obtaining maintenance renewal notification item: " + ex.ToString());
				throw new Exception(string.Format(Resources.LIBCODE_JM0_22, renewalId));
			}
			return result;
		}

		// Token: 0x06000174 RID: 372 RVA: 0x0000C208 File Offset: 0x0000A408
		public List<MaintenanceRenewalItem> GetMaintenanceRenewalNotificationItems(bool includeIgnored)
		{
			CoreBusinessLayerService.log.Debug("Sending request for MaintenanceRenewalItemDAL.GetItems.");
			List<MaintenanceRenewalItem> result;
			try
			{
				List<MaintenanceRenewalItem> list = new List<MaintenanceRenewalItem>();
				foreach (MaintenanceRenewalItemDAL dal in MaintenanceRenewalItemDAL.GetItems(new MaintenanceRenewalFilter(true, includeIgnored, null)))
				{
					list.Add(CoreBusinessLayerService.DalToWfc(dal));
				}
				result = list;
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error when obtaining maintenance renewals notification items: " + ex.ToString());
				throw new Exception(Resources.LIBCODE_JM0_23);
			}
			return result;
		}

		// Token: 0x06000175 RID: 373 RVA: 0x0000C2B0 File Offset: 0x0000A4B0
		public MaintenanceRenewalItem GetLatestMaintenanceRenewalItem()
		{
			CoreBusinessLayerService.log.Debug("Sending request for MaintenanceRenewalItemDAL.GetLatestItem.");
			MaintenanceRenewalItem result;
			try
			{
				result = CoreBusinessLayerService.DalToWfc(MaintenanceRenewalItemDAL.GetLatestItem(new MaintenanceRenewalFilter(true, false, null)));
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error when obtaining maintenance renewals notification item: " + ex.ToString());
				throw new Exception(Resources.LIBCODE_JM0_23);
			}
			return result;
		}

		// Token: 0x06000176 RID: 374 RVA: 0x0000C318 File Offset: 0x0000A518
		public MaintenanceRenewalsCheckStatus GetMaintenanceRenewalsCheckStatus()
		{
			CoreBusinessLayerService.log.Debug("Sending request for MaintenanceRenewalsCheckStatusDAL.GetCheckStatus.");
			MaintenanceRenewalsCheckStatus result;
			try
			{
				MaintenanceRenewalsCheckStatusDAL checkStatus = MaintenanceRenewalsCheckStatusDAL.GetCheckStatus();
				if (checkStatus == null)
				{
					result = null;
				}
				else
				{
					result = new MaintenanceRenewalsCheckStatus(checkStatus.LastUpdateCheck, checkStatus.NextUpdateCheck);
				}
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error obtaining maintenance renewals status: " + ex.ToString());
				throw new Exception(Resources.LIBCODE_JM0_20);
			}
			return result;
		}

		// Token: 0x06000177 RID: 375 RVA: 0x0000C38C File Offset: 0x0000A58C
		public void ForceMaintenanceRenewalsCheck()
		{
			CoreBusinessLayerService.log.Debug("Sending request for CoreHelper.CheckMaintenanceRenewals.");
			try
			{
				CoreHelper.CheckMaintenanceRenewals(false);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error while checking maintenance renewals: " + ex.ToString());
				throw new Exception(Resources.LIBCODE_JM0_21);
			}
		}

		// Token: 0x06000178 RID: 376 RVA: 0x0000C3E8 File Offset: 0x0000A5E8
		public Nodes GetAllNodes()
		{
			return NodeBLDAL.GetNodes();
		}

		// Token: 0x06000179 RID: 377 RVA: 0x0000C3EF File Offset: 0x0000A5EF
		public List<int> GetSortedNodeIDs()
		{
			return NodeBLDAL.GetSortedNodeIDs();
		}

		// Token: 0x0600017A RID: 378 RVA: 0x0000C3F6 File Offset: 0x0000A5F6
		public List<string> GetNodeFields()
		{
			return NodeBLDAL.GetFields();
		}

		// Token: 0x0600017B RID: 379 RVA: 0x0000C3FD File Offset: 0x0000A5FD
		public List<string> GetCustomPropertyFields(bool includeMemo)
		{
			return new List<string>(CustomPropertyMgr.GetPropNamesForTable("NodesCustomProperties", includeMemo));
		}

		// Token: 0x0600017C RID: 380 RVA: 0x0000C40F File Offset: 0x0000A60F
		public Dictionary<string, string> GetVendors()
		{
			return NodeBLDAL.GetVendors();
		}

		// Token: 0x0600017D RID: 381 RVA: 0x0000C418 File Offset: 0x0000A618
		public void DeleteNode(int nodeId)
		{
			Node node = this.GetNode(nodeId);
			if (node != null)
			{
				Dictionary<string, object> nodeInfo = CoreBusinessLayerService.GetNodeInfo(node);
				NodeBLDAL.DeleteNode(node);
				NodeSettingsDAL.DeleteNodeSettings(nodeId);
				NodeNotesDAL.DeleteNodeNotes(nodeId);
				NodeIndication nodeIndication = new NodeIndication(1, new Node
				{
					Id = nodeId,
					Caption = nodeInfo["DisplayName"].ToString(),
					IpAddress = node.IpAddress,
					Status = node.Status
				});
				foreach (KeyValuePair<string, object> keyValuePair in nodeInfo)
				{
					nodeIndication.NodeProperties[keyValuePair.Key] = keyValuePair.Value;
				}
				IndicationPublisher.CreateV3().ReportIndication(nodeIndication);
			}
		}

		// Token: 0x0600017E RID: 382 RVA: 0x0000C4EC File Offset: 0x0000A6EC
		private static Dictionary<string, object> GetNodeInfo(Node node)
		{
			return new SwisEntityHelper(CoreBusinessLayerService.CreateProxy()).GetProperties("Orion.Nodes", node.ID, new string[]
			{
				"DisplayName",
				"Uri"
			});
		}

		// Token: 0x0600017F RID: 383 RVA: 0x0000C520 File Offset: 0x0000A720
		[Obsolete("This is a temporary solution. Don't use this method in modules")]
		public Node InsertNodeWithFaultContract(Node node, bool allowDuplicates, bool reportIndication)
		{
			Node result;
			try
			{
				result = this.InsertNode(node, allowDuplicates, reportIndication);
			}
			catch (LicenseException ex)
			{
				throw MessageUtilities.NewFaultException<CoreFaultContract>(ex);
			}
			return result;
		}

		// Token: 0x06000180 RID: 384 RVA: 0x0000C550 File Offset: 0x0000A750
		[Obsolete("This is a temporary solution. Don't use this method in modules")]
		public Node InsertNodeWithFaultContract(Node node, bool allowDuplicates)
		{
			return this.InsertNodeWithFaultContract(node, allowDuplicates, true);
		}

		// Token: 0x06000181 RID: 385 RVA: 0x0000C55B File Offset: 0x0000A75B
		[Obsolete("This is a temporary solution. Don't use this method in modules")]
		public Node InsertNodeWithFaultContract(Node node)
		{
			return this.InsertNode(node, false);
		}

		// Token: 0x06000182 RID: 386 RVA: 0x0000C568 File Offset: 0x0000A768
		public Node InsertNode(Node node, bool allowDuplicates, bool reportIndication)
		{
			int maxElementCount = new FeatureManager().GetMaxElementCount(WellKnownElementTypes.Nodes);
			if (NodeBLDAL.GetNodeCount() >= maxElementCount)
			{
				throw LicenseException.FromElementsExceeded(maxElementCount);
			}
			node = NodeBLDAL.InsertNode(node, allowDuplicates);
			NodeBLDAL.PopulateWebCommunityStrings();
			if (reportIndication)
			{
				IndicationPublisher.CreateV3().ReportIndication(new NodeIndication(0, node));
			}
			return node;
		}

		// Token: 0x06000183 RID: 387 RVA: 0x0000C5B7 File Offset: 0x0000A7B7
		public Node InsertNode(Node node, bool allowDuplicates)
		{
			return this.InsertNode(node, allowDuplicates, true);
		}

		// Token: 0x06000184 RID: 388 RVA: 0x0000C55B File Offset: 0x0000A75B
		public Node InsertNode(Node node)
		{
			return this.InsertNode(node, false);
		}

		// Token: 0x06000185 RID: 389 RVA: 0x0000C5C2 File Offset: 0x0000A7C2
		public Node InsertNodeAndGenericPollers(Node node)
		{
			return this.nodeHelper.InsertNodeAndGenericPollers(node);
		}

		// Token: 0x06000186 RID: 390 RVA: 0x0000C5D0 File Offset: 0x0000A7D0
		public void AddPollersForNode(int nodeId, string[] newPollers)
		{
			if (nodeId <= 0)
			{
				throw new ArgumentException("Argument nodeId cannot be less then or equal zero");
			}
			Node node = NodeDAL.GetNode(nodeId);
			if (node == null)
			{
				throw new ArgumentException("Node doesn't exist");
			}
			if (newPollers == null)
			{
				return;
			}
			if (newPollers.Length == 0)
			{
				return;
			}
			this.nodeHelper.AddPollersForNode(node, newPollers);
		}

		// Token: 0x06000187 RID: 391 RVA: 0x0000C618 File Offset: 0x0000A818
		public void AddBasicPollersForNode(int nodeId, NodeSubType nodeSubtType)
		{
			if (nodeId <= 0)
			{
				throw new ArgumentException("nodeId");
			}
			Node node = NodeDAL.GetNode(nodeId);
			if (node == null)
			{
				throw new ArgumentException("Node doesn't exist");
			}
			this.nodeHelper.AddBasicPollersForNode(node);
		}

		// Token: 0x06000188 RID: 392 RVA: 0x0000C658 File Offset: 0x0000A858
		public void RemoveBasicPollersForNode(int nodeId, NodeSubType nodeSubType)
		{
			CoreBusinessLayerService.log.DebugFormat("Removing basic pollers for NodeID = {0}, SubType = {1} ....", nodeId, nodeSubType);
			PollersDAL pollersDAL = new PollersDAL();
			List<string> list = new List<string>();
			list.AddRange(NodeResponseTimeIcmpPoller.SubPollerTypes);
			if (nodeSubType != NodeSubType.ICMP)
			{
				if (nodeSubType == NodeSubType.SNMP)
				{
					list.Add("N.Details.SNMP.Generic");
					list.Add("N.Uptime.SNMP.Generic");
				}
				else if (nodeSubType == NodeSubType.WMI)
				{
					list.Add(NodeDetailsPollerGeneric.PollerType);
					list.Add(NodeUptimePollerGeneric.PollerType);
				}
			}
			pollersDAL.Delete("N", nodeId, list.ToArray());
			CoreBusinessLayerService.log.DebugFormat("Basic pollers count = {0}, removed for NodeID = {1}, SubType = {2}", list.Count, nodeId, nodeSubType);
		}

		// Token: 0x06000189 RID: 393 RVA: 0x0000C708 File Offset: 0x0000A908
		public void UpdateNode(Node node)
		{
			DateTime utcNow = DateTime.UtcNow;
			if (utcNow > node.UnManageFrom && utcNow < node.UnManageUntil)
			{
				node.UnManaged = true;
				node.Status = "9";
				node.PolledStatus = 9;
				node.GroupStatus = "Unmanaged.gif";
				node.StatusDescription = "Node status is Unmanaged.";
			}
			else if (node.Status.Trim() == "9")
			{
				node.UnManaged = false;
				node.Status = "0";
				node.PolledStatus = 0;
				node.GroupStatus = "Unknown.gif";
				node.StatusDescription = "Node status is Unknown.";
			}
			if (node.UnPluggable)
			{
				node.Status = "11";
				node.PolledStatus = 11;
				node.GroupStatus = "External.gif";
				node.StatusDescription = "Node status is External.";
			}
			else if (node.Status.Trim() == "11")
			{
				node.Status = "0";
				node.PolledStatus = 0;
				node.GroupStatus = "Unknown.gif";
				node.StatusDescription = "Node status is Unknown.";
			}
			Node node2 = NodeBLDAL.GetNode(node.Id);
			NodeBLDAL.UpdateNode(node);
			NodeBLDAL.PopulateWebCommunityStrings();
			NodeIndication nodeIndication = new NodeIndication(2, node, node2);
			if (nodeIndication.ChangedProperties.ContainsKey("ObjectSubType"))
			{
				bool flag = CoreBusinessLayerService.WmiCompatibleNodeSubTypes.Contains(node.NodeSubType) && CoreBusinessLayerService.WmiCompatibleNodeSubTypes.Contains(node2.NodeSubType);
				if (!flag)
				{
					foreach (Volume volume in VolumeDAL.GetNodeVolumes(node.Id))
					{
						VolumeDAL.DeleteVolume(volume);
					}
				}
				PollersDAL pollersDAL = new PollersDAL();
				pollersDAL.Delete(new PollerAssignment("N", node.ID, "%SNMP%"));
				if (flag)
				{
					pollersDAL.Delete(new PollerAssignment("N", node.ID, "N.Uptime.%"));
					pollersDAL.Delete(new PollerAssignment("N", node.ID, "N.Details.%"));
				}
				else
				{
					pollersDAL.Delete(new PollerAssignment("N", node.ID, "%WMI%"));
				}
				pollersDAL.Delete(new PollerAssignment("N", node.ID, "%Agent%"));
				if (node.NodeSubType == NodeSubType.Agent)
				{
					pollersDAL.Delete(new PollerAssignment("N", node.ID, "%ICMP%"));
				}
				if (!(node.EntityType ?? "").Contains(".VIM."))
				{
					NodeBLDAL.UpdateNodeProperty(new Dictionary<string, object>
					{
						{
							"CPULoad",
							-2
						},
						{
							"TotalMemory",
							-2
						},
						{
							"MemoryUsed",
							-2
						},
						{
							"PercentMemoryUsed",
							-2
						},
						{
							"BufferNoMemThisHour",
							-2
						},
						{
							"BufferNoMemToday",
							-2
						},
						{
							"BufferSmMissThisHour",
							-2
						},
						{
							"BufferSmMissToday",
							-2
						},
						{
							"BufferMdMissThisHour",
							-2
						},
						{
							"BufferMdMissToday",
							-2
						},
						{
							"BufferBgMissThisHour",
							-2
						},
						{
							"BufferBgMissToday",
							-2
						},
						{
							"BufferLgMissThisHour",
							-2
						},
						{
							"BufferLgMissToday",
							-2
						},
						{
							"BufferHgMissThisHour",
							-2
						},
						{
							"BufferHgMissToday",
							-2
						}
					}, node.ID);
				}
				NodeBLDAL.UpdateNodeProperty(new Dictionary<string, object>
				{
					{
						"LastBoot",
						null
					},
					{
						"SystemUpTime",
						null
					},
					{
						"LastSystemUpTimePollUtc",
						null
					}
				}, node.ID);
			}
			if (nodeIndication.AnyChange)
			{
				IndicationPublisher.CreateV3().ReportIndication(nodeIndication);
			}
		}

		// Token: 0x0600018A RID: 394 RVA: 0x0000CB18 File Offset: 0x0000AD18
		public Node GetNode(int nodeId)
		{
			return NodeBLDAL.GetNode(nodeId);
		}

		// Token: 0x0600018B RID: 395 RVA: 0x0000CB20 File Offset: 0x0000AD20
		public bool IsNodeWireless(int nodeId)
		{
			return NodeBLDAL.IsNodeWireless(nodeId);
		}

		// Token: 0x0600018C RID: 396 RVA: 0x0000CB28 File Offset: 0x0000AD28
		public bool IsNodeEnergyWise(int nodeId)
		{
			return NodeBLDAL.IsNodeEnergyWise(nodeId);
		}

		// Token: 0x0600018D RID: 397 RVA: 0x0000CB30 File Offset: 0x0000AD30
		public Node GetNodeWithOptions(int nodeId, bool getInterfaces, bool getVolumes)
		{
			return NodeBLDAL.GetNodeWithOptions(nodeId, getInterfaces, getVolumes);
		}

		// Token: 0x0600018E RID: 398 RVA: 0x0000CB3A File Offset: 0x0000AD3A
		public Resources ListResources(Node node)
		{
			return ResourceLister.ListResources(node);
		}

		// Token: 0x0600018F RID: 399 RVA: 0x0000CB42 File Offset: 0x0000AD42
		public Guid BeginListResources(Node node)
		{
			return ResourceLister.BeginListResources(node);
		}

		// Token: 0x06000190 RID: 400 RVA: 0x0000CB4A File Offset: 0x0000AD4A
		public Guid BeginCoreListResources(Node node, bool includeInterfaces)
		{
			return ResourceLister.BeginListResources(node, includeInterfaces);
		}

		// Token: 0x06000191 RID: 401 RVA: 0x0000CB53 File Offset: 0x0000AD53
		public ListResourcesStatus GetListResourcesStatus(Guid listResourcesOperationId)
		{
			return ResourceLister.GetListResourcesStatus(listResourcesOperationId);
		}

		// Token: 0x06000192 RID: 402 RVA: 0x0000CB5B File Offset: 0x0000AD5B
		public float GetAvailability(int nodeID, DateTime startDate, DateTime endDate)
		{
			return NodeBLDAL.GetAvailability(nodeID, startDate, endDate);
		}

		// Token: 0x06000193 RID: 403 RVA: 0x0000CB65 File Offset: 0x0000AD65
		public Dictionary<string, int> GetValuesAndCountsForNodePropertyFiltered(string property, string accountId, Dictionary<string, object> filters)
		{
			return NodeBLDAL.GetValuesAndCountsForPropertyFiltered(property, accountId, filters);
		}

		// Token: 0x06000194 RID: 404 RVA: 0x0000CB6F File Offset: 0x0000AD6F
		public Dictionary<string, int> GetValuesAndCountsForNodeProperty(string property, string accountId)
		{
			return NodeBLDAL.GetValuesAndCountsForProperty(property, accountId);
		}

		// Token: 0x06000195 RID: 405 RVA: 0x0000CB78 File Offset: 0x0000AD78
		public Dictionary<string, int> GetCultureSpecificValuesAndCountsForNodeProperty(string property, string accountId, CultureInfo culture)
		{
			return NodeBLDAL.GetValuesAndCountsForProperty(property, accountId, culture);
		}

		// Token: 0x06000196 RID: 406 RVA: 0x0000CB82 File Offset: 0x0000AD82
		public Nodes GetNodesFiltered(Dictionary<string, object> filterValues, bool includeInterfaces, bool includeVolumes)
		{
			return NodeBLDAL.GetNodesFiltered(filterValues, includeInterfaces, includeVolumes);
		}

		// Token: 0x06000197 RID: 407 RVA: 0x0000CB8C File Offset: 0x0000AD8C
		public Nodes GetNodesByIds(int[] nodeIds)
		{
			return NodeBLDAL.GetNodesByIds(nodeIds);
		}

		// Token: 0x06000198 RID: 408 RVA: 0x0000CB94 File Offset: 0x0000AD94
		public Dictionary<string, string> GetVendorIconFileNames()
		{
			return NodeBLDAL.GetVendorIconFileNames();
		}

		// Token: 0x06000199 RID: 409 RVA: 0x0000CB9B File Offset: 0x0000AD9B
		public List<string> GetNodeDistinctValuesForField(string fieldName)
		{
			return NodeBLDAL.GetNodeDistinctValuesForField(fieldName);
		}

		// Token: 0x0600019A RID: 410 RVA: 0x0000CBA3 File Offset: 0x0000ADA3
		public NodeHardwareType GetNodeHardwareType(int nodeId)
		{
			return NodeBLDAL.GetNodeHardwareType(nodeId);
		}

		// Token: 0x0600019B RID: 411 RVA: 0x0000CBAB File Offset: 0x0000ADAB
		public void BulkUpdateNodePollingInterval(int pollInterval, int engineId)
		{
			NodeBLDAL.BulkUpdateNodePollingInterval(pollInterval, engineId);
		}

		// Token: 0x0600019C RID: 412 RVA: 0x0000CBB4 File Offset: 0x0000ADB4
		public Dictionary<string, object> GetNodeCustomProperties(int nodeId, ICollection<string> properties)
		{
			return NodeBLDAL.GetNodeCustomProperties(nodeId, properties);
		}

		// Token: 0x0600019D RID: 413 RVA: 0x0000CBBD File Offset: 0x0000ADBD
		public DataTable GetPagebleNodes(string property, string type, string val, string column, string direction, int number, int size, string searchText)
		{
			return NodeBLDAL.GetPagebleNodes(property, type, val, column, direction, number, size, searchText);
		}

		// Token: 0x0600019E RID: 414 RVA: 0x0000CBD1 File Offset: 0x0000ADD1
		public int GetNodesCount(string property, string type, string val, string searchText)
		{
			return NodeBLDAL.GetNodesCount(property, type, val, searchText);
		}

		// Token: 0x0600019F RID: 415 RVA: 0x0000CBDD File Offset: 0x0000ADDD
		public DataTable GetGroupsByNodeProperty(string property, string propertyType)
		{
			return NodeBLDAL.GetGroupsByNodeProperty(property, propertyType);
		}

		// Token: 0x060001A0 RID: 416 RVA: 0x0000CBE6 File Offset: 0x0000ADE6
		public void ReflowAllNodeChildStatus()
		{
			NodeChildStatusParticipationDAL.ReflowAllNodeChildStatus();
		}

		// Token: 0x060001A1 RID: 417 RVA: 0x0000CBF0 File Offset: 0x0000ADF0
		public string ResolveNodeNameByIP(string ipAddress)
		{
			JobDescription jobDescription = ResolveHostNameByIpJob.CreateJobDescription(ipAddress, BusinessLayerSettings.Instance.TestJobTimeout);
			string text;
			return this.ExecuteJobAndGetResult<ResolveHostNameByIpJobResult>(jobDescription, null, JobResultDataFormatType.Xml, "ResolveHostNameByIpJob", out text).HostName;
		}

		// Token: 0x060001A2 RID: 418 RVA: 0x0000CC23 File Offset: 0x0000AE23
		public DataTable GetNodeCPUsByPercentLoad(int nodeId, int pageNumber, int pageSize)
		{
			return NodeBLDAL.GetNodeCPUsByPercentLoad(nodeId, pageNumber, pageSize);
		}

		// Token: 0x060001A3 RID: 419 RVA: 0x0000CC2D File Offset: 0x0000AE2D
		public DataTable GetNodesCpuIndexCounts(List<string> nodeIds)
		{
			return NodeBLDAL.GetNodesCpuIndexCounts(nodeIds);
		}

		// Token: 0x060001A4 RID: 420 RVA: 0x0000CC35 File Offset: 0x0000AE35
		public bool AddNodeNote(int nodeId, string accountId, string note, DateTime modificationDateTime)
		{
			return NodeBLDAL.AddNodeNote(nodeId, accountId, note, modificationDateTime);
		}

		// Token: 0x060001A5 RID: 421 RVA: 0x0000CC41 File Offset: 0x0000AE41
		public NodeNotesPage GetNodeNotes(PageableNodeNoteRequest request)
		{
			return new NodeNotesDAL().GetNodeNotes(request);
		}

		// Token: 0x060001A6 RID: 422 RVA: 0x0000CC4E File Offset: 0x0000AE4E
		public void UpdateSpecificSettingForAllNodes(string settingName, string settingValue, string whereClause)
		{
			NodeSettingsDAL.UpdateSpecificSettingForAllNodes(settingName, settingValue, whereClause);
		}

		// Token: 0x060001A7 RID: 423 RVA: 0x0000CC58 File Offset: 0x0000AE58
		public int GetAvailableNotificationItemsCountByType(Guid typeId)
		{
			CoreBusinessLayerService.log.Debug("Sending request for NotificationItemDAL.GetNotificationsCountByType.");
			int notificationsCountByType;
			try
			{
				notificationsCountByType = SolarWinds.Orion.Core.BusinessLayer.DAL.NotificationItemDAL.GetNotificationsCountByType(typeId, new NotificationItemFilter(false, false));
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Can't get notification items count by type: " + ex.ToString());
				throw new Exception(string.Format(Resources.LIBCODE_JM0_8, typeId));
			}
			return notificationsCountByType;
		}

		// Token: 0x060001A8 RID: 424 RVA: 0x0000CCC8 File Offset: 0x0000AEC8
		public Dictionary<Guid, int> GetAvailableNotificationItemsCounts()
		{
			CoreBusinessLayerService.log.Debug("Sending request for NotificationItemDAL.GetNotificationsCounts.");
			Dictionary<Guid, int> notificationsCounts;
			try
			{
				notificationsCounts = SolarWinds.Orion.Core.BusinessLayer.DAL.NotificationItemDAL.GetNotificationsCounts();
			}
			catch (Exception arg)
			{
				CoreBusinessLayerService.log.Error("Can't get notification items count for all types: " + arg);
				throw new Exception(Resources.LIBCODE_JM0_9);
			}
			return notificationsCounts;
		}

		// Token: 0x060001A9 RID: 425 RVA: 0x0000CD20 File Offset: 0x0000AF20
		public void IgnoreNotificationItem(Guid notificationId)
		{
			CoreBusinessLayerService.log.Debug("Sending request for NotificationItemDAL.IgnoreItem.");
			try
			{
				SolarWinds.Orion.Core.BusinessLayer.DAL.NotificationItemDAL.IgnoreItem(notificationId);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Can't ignore notification item: " + ex.ToString());
				throw new Exception(string.Format(Resources.LIBCODE_JM0_10, notificationId));
			}
		}

		// Token: 0x060001AA RID: 426 RVA: 0x0000CD88 File Offset: 0x0000AF88
		public void IgnoreNotificationItems(List<Guid> notificationIds)
		{
			CoreBusinessLayerService.log.Debug("Sending request for NotificationItemDAL.IgnoreItems.");
			try
			{
				SolarWinds.Orion.Core.BusinessLayer.DAL.NotificationItemDAL.IgnoreItems(notificationIds);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Can't ignore multiple notification items: " + ex.ToString());
				throw new Exception(Resources.LIBCODE_JM0_11);
			}
		}

		// Token: 0x060001AB RID: 427 RVA: 0x0000CDE4 File Offset: 0x0000AFE4
		public void AcknowledgeNotificationItem(Guid notificationId, string byAccountId, DateTime createdBefore)
		{
			CoreBusinessLayerService.log.Debug("Sending request for .NotificationItemDAL.AcknowledgeItem.");
			try
			{
				SolarWinds.Orion.Core.BusinessLayer.DAL.NotificationItemDAL.AcknowledgeItem(notificationId, byAccountId, createdBefore);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Can't acknowledge notification item: " + ex.ToString());
				throw new Exception(string.Format(Resources.LIBCODE_JM0_12, notificationId, byAccountId));
			}
		}

		// Token: 0x060001AC RID: 428 RVA: 0x0000CE50 File Offset: 0x0000B050
		public void AcknowledgeNotificationItemsByType(Guid typeId, string byAccountId, DateTime createdBefore)
		{
			CoreBusinessLayerService.log.Debug("Sending request for .NotificationItemDAL.AcknowledgeItemsByType.");
			try
			{
				SolarWinds.Orion.Core.BusinessLayer.DAL.NotificationItemDAL.AcknowledgeItemsByType(typeId, byAccountId, createdBefore);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Can't acknowledge notification items by type: " + ex.ToString());
				throw new Exception(string.Format(Resources.LIBCODE_JM0_13, typeId, byAccountId));
			}
		}

		// Token: 0x060001AD RID: 429 RVA: 0x0000CEBC File Offset: 0x0000B0BC
		public void AcknowledgeAllNotificationItems(string byAccountId, DateTime createdBefore)
		{
			CoreBusinessLayerService.log.Debug("Sending request for NotificationItemDAL.AcknowledgeAllItems.");
			try
			{
				SolarWinds.Orion.Core.BusinessLayer.DAL.NotificationItemDAL.AcknowledgeAllItems(byAccountId, createdBefore);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Can't acknowledge all notification items: " + ex.ToString());
				throw new Exception(string.Format(Resources.LIBCODE_JM0_14, byAccountId));
			}
		}

		// Token: 0x060001AE RID: 430 RVA: 0x0000CF20 File Offset: 0x0000B120
		public List<HeaderNotificationItem> GetLatestNotificationItemsWithCount()
		{
			CoreBusinessLayerService.log.Debug("Sending request for NotificationItemDAL.GetLatestItemByType.");
			List<HeaderNotificationItem> ret2;
			try
			{
				List<HeaderNotificationItem> ret = new List<HeaderNotificationItem>();
				SolarWinds.Orion.Core.BusinessLayer.DAL.NotificationItemDAL.GetLatestItemsWithCount(new NotificationItemFilter(false, false), delegate(SolarWinds.Orion.Core.BusinessLayer.DAL.NotificationItemDAL item, int count)
				{
					ret.Add(new HeaderNotificationItem(item.Id, item.Title, item.Description, item.CreatedAt, item.Ignored, item.TypeId, item.Url, item.AcknowledgedAt, item.AcknowledgedBy, count));
				});
				ret2 = ret;
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Can't obtain latest notification items: " + ex.ToString());
				throw new Exception(Resources.LIBCODE_LK0_1);
			}
			return ret2;
		}

		// Token: 0x060001AF RID: 431 RVA: 0x0000CFA8 File Offset: 0x0000B1A8
		public List<NotificationItem> GetNotificationItemsByType(Guid typeId, bool includeIgnored)
		{
			CoreBusinessLayerService.log.Debug("Sending request for NotificationItemDAL.GetItemsByTypeId.");
			List<NotificationItem> result;
			try
			{
				IEnumerable<SolarWinds.Orion.Core.BusinessLayer.DAL.NotificationItemDAL> itemsByTypeId = SolarWinds.Orion.Core.BusinessLayer.DAL.NotificationItemDAL.GetItemsByTypeId(typeId, new NotificationItemFilter(true, includeIgnored));
				List<NotificationItem> list = new List<NotificationItem>();
				foreach (SolarWinds.Orion.Core.BusinessLayer.DAL.NotificationItemDAL notificationItemDAL in itemsByTypeId)
				{
					NotificationItem item = new NotificationItem(notificationItemDAL.Id, notificationItemDAL.Title, notificationItemDAL.Description, notificationItemDAL.CreatedAt, notificationItemDAL.Ignored, notificationItemDAL.TypeId, notificationItemDAL.Url, notificationItemDAL.AcknowledgedAt, notificationItemDAL.AcknowledgedBy);
					list.Add(item);
				}
				result = list;
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Can't obtain latest notification item: " + ex.ToString());
				throw new Exception(string.Format(Resources.LIBCODE_JM0_15, typeId));
			}
			return result;
		}

		// Token: 0x060001B0 RID: 432 RVA: 0x0000D094 File Offset: 0x0000B294
		public void InsertNotificationItem(NotificationItem item)
		{
			CoreBusinessLayerService.log.Debug("Sending request for NotificationItemDAL.InsertNotificationItem.");
			try
			{
				SolarWinds.Orion.Core.BusinessLayer.DAL.NotificationItemDAL.Insert(item.Id, item.TypeId, item.Title, item.Description, item.Ignored, item.Url, item.AcknowledgedAt, item.AcknowledgedBy);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Unable to insert notification item: " + ex.ToString());
				throw new Exception(string.Format(Resources.LIBCODE_JM0_17, item.Id, item.TypeId));
			}
		}

		// Token: 0x060001B1 RID: 433 RVA: 0x0000D13C File Offset: 0x0000B33C
		public void UpdateNotificationItem(NotificationItem item)
		{
			CoreBusinessLayerService.log.Debug("Sending request for NotificationItemDAL.UpdateNotificationItem.");
			try
			{
				SolarWinds.Orion.Core.BusinessLayer.DAL.NotificationItemDAL.Update(item.Id, item.TypeId, item.Title, item.Description, item.Ignored, item.Url, item.CreatedAt, item.AcknowledgedAt, item.AcknowledgedBy);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Unable to update notification item: " + ex.ToString());
				throw new Exception(string.Format(Resources.LIBCODE_JM0_16, item.Id, item.TypeId));
			}
		}

		// Token: 0x060001B2 RID: 434 RVA: 0x0000D1E8 File Offset: 0x0000B3E8
		public bool DeleteNotificationItemById(Guid itemId)
		{
			CoreBusinessLayerService.log.Debug("Sending request for NotificationItemDAL.Delete.");
			bool result;
			try
			{
				result = SolarWinds.Orion.Core.BusinessLayer.DAL.NotificationItemDAL.Delete(itemId);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Unable to delete notification item: " + ex.ToString());
				throw new Exception(string.Format(Resources.LIBCODE_JM0_18, itemId));
			}
			return result;
		}

		// Token: 0x060001B3 RID: 435 RVA: 0x0000D250 File Offset: 0x0000B450
		public NotificationItem GetNotificationItemById(Guid itemId)
		{
			CoreBusinessLayerService.log.Debug("Sending request for NotificationItemDAL.GetItemById.");
			NotificationItem result;
			try
			{
				SolarWinds.Orion.Core.BusinessLayer.DAL.NotificationItemDAL itemById = SolarWinds.Orion.Core.BusinessLayer.DAL.NotificationItemDAL.GetItemById<SolarWinds.Orion.Core.BusinessLayer.DAL.NotificationItemDAL>(itemId);
				if (itemById != null)
				{
					result = new NotificationItem(itemById.Id, itemById.Title, itemById.Description, itemById.CreatedAt, itemById.Ignored, itemById.TypeId, itemById.Url, itemById.AcknowledgedAt, itemById.AcknowledgedBy);
				}
				else
				{
					result = null;
				}
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Can't obtain notification item: " + ex.ToString());
				throw new Exception(string.Format(Resources.LIBCODE_JM0_19, itemId));
			}
			return result;
		}

		// Token: 0x060001B4 RID: 436 RVA: 0x0000D2FC File Offset: 0x0000B4FC
		public DataTable GetOrionMessagesTable(OrionMessagesFilter filter)
		{
			return OrionMessagesDAL.GetOrionMessagesTable(filter);
		}

		// Token: 0x060001B5 RID: 437 RVA: 0x0000D304 File Offset: 0x0000B504
		public PollerAssignment GetPoller(int pollerID)
		{
			return PollerDAL.GetPoller(pollerID);
		}

		// Token: 0x060001B6 RID: 438 RVA: 0x0000D30C File Offset: 0x0000B50C
		public void DeletePoller(int pollerID)
		{
			PollerDAL.DeletePoller(pollerID);
		}

		// Token: 0x060001B7 RID: 439 RVA: 0x0000D314 File Offset: 0x0000B514
		public int InsertPoller(PollerAssignment poller)
		{
			return PollerDAL.InsertPoller(poller);
		}

		// Token: 0x060001B8 RID: 440 RVA: 0x0000D31C File Offset: 0x0000B51C
		public PollerAssignments GetPollersForNode(int nodeId)
		{
			return PollerDAL.GetPollersForNode(nodeId);
		}

		// Token: 0x060001B9 RID: 441 RVA: 0x0000D324 File Offset: 0x0000B524
		public PollerAssignments GetAllPollersForNode(int nodeId)
		{
			return PollerDAL.GetAllPollersForNode(nodeId, this.AreInterfacesSupported);
		}

		// Token: 0x060001BA RID: 442 RVA: 0x0000D334 File Offset: 0x0000B534
		public int AddReportJob(ReportJobConfiguration configuration)
		{
			new List<int>();
			ISwisConnectionProxyCreator creator = SwisConnectionProxyPool.GetCreator();
			List<int> freqIds = new FrequenciesDAL(creator).SaveFrequencies(new List<ISchedule>(configuration.Schedules));
			int num = ReportJobDAL.AddReportJob(configuration);
			ReportSchedulesDAL.ScheduleReportJobWithFrequencies(freqIds, num);
			new ActionsDAL(creator).SaveActionsForAssignments(num, 1.ToString(), configuration.Actions);
			configuration.ReportJobID = num;
			this.AddBLScheduler(configuration);
			return configuration.ReportJobID;
		}

		// Token: 0x060001BB RID: 443 RVA: 0x0000D3A4 File Offset: 0x0000B5A4
		public void ChangeReportJobStatus(int jobId, bool enable)
		{
			ReportJobDAL.ChangeReportJobStatus(jobId, enable);
			ReportJobConfiguration reportJob = ReportJobDAL.GetReportJob(jobId);
			this.RemoveBLScheduler(reportJob);
			this.AddBLScheduler(reportJob);
		}

		// Token: 0x060001BC RID: 444 RVA: 0x0000D3D0 File Offset: 0x0000B5D0
		public void AssignJobsToReport(int reportId, List<int> schedulesIds)
		{
			CoreBusinessLayerService.log.DebugFormat("Assigning jobs for report", Array.Empty<object>());
			List<int> jobsIdsWithReport = ReportJobDAL.GetJobsIdsWithReport(reportId);
			jobsIdsWithReport.AddRange(schedulesIds);
			ReportJobDAL.AssignJobsToReport(reportId, schedulesIds);
			if (jobsIdsWithReport.Count != 0)
			{
				foreach (ReportJobConfiguration configuration in ReportJobDAL.GetJobsByIds(jobsIdsWithReport))
				{
					this.RemoveBLScheduler(configuration);
					this.AddBLScheduler(configuration);
				}
			}
		}

		// Token: 0x060001BD RID: 445 RVA: 0x0000D45C File Offset: 0x0000B65C
		public void AssignJobsToReports(List<int> reportIds, List<int> schedulesIds)
		{
			CoreBusinessLayerService.log.DebugFormat("Assigning jobs for report", Array.Empty<object>());
			List<int> list = new List<int>();
			if (reportIds.Count > 0)
			{
				list = ReportJobDAL.GetJobsIdsWithReports(reportIds);
			}
			list.AddRange(schedulesIds);
			ReportJobDAL.AssignJobsToReports(reportIds, schedulesIds);
			foreach (ReportJobConfiguration configuration in ReportJobDAL.GetJobsByIds(list))
			{
				this.RemoveBLScheduler(configuration);
				this.AddBLScheduler(configuration);
			}
		}

		// Token: 0x060001BE RID: 446 RVA: 0x0000D4F0 File Offset: 0x0000B6F0
		public void UpdateReportJob(ReportJobConfiguration configuration, int[] allowedReportIds)
		{
			CoreBusinessLayerService.log.DebugFormat("Updating report job", Array.Empty<object>());
			ReportJobConfiguration reportJob = ReportJobDAL.GetReportJob(configuration.ReportJobID);
			this.RemoveBLScheduler(reportJob);
			bool flag = true;
			if (reportJob.Schedules != null && configuration.Schedules != null && reportJob.Schedules.Count == configuration.Schedules.Count)
			{
				foreach (ReportSchedule reportSchedule in reportJob.Schedules)
				{
					reportSchedule.StartTime = reportSchedule.StartTime.ToLocalTime();
					reportSchedule.EndTime = ((reportSchedule.EndTime == null) ? reportSchedule.EndTime : new DateTime?(reportSchedule.EndTime.Value.ToLocalTime()));
				}
				flag = !reportJob.Schedules.SequenceEqual(configuration.Schedules);
			}
			List<int> list = new List<int>();
			ISwisConnectionProxyCreator creator = SwisConnectionProxyPool.GetCreator();
			if (flag)
			{
				FrequenciesDAL frequenciesDAL = new FrequenciesDAL(creator);
				list = frequenciesDAL.SaveFrequencies(new List<ISchedule>(configuration.Schedules));
				List<int> list2 = (from x in this.GetReportJob(configuration.ReportJobID).Schedules
				select x.FrequencyId).Except(list).ToList<int>();
				if (list2.Count > 0)
				{
					frequenciesDAL.DeleteFrequencies(list2);
				}
			}
			ReportJobDAL.UpdateReportJob(configuration, allowedReportIds, flag);
			ReportSchedulesDAL.ScheduleReportJobWithFrequencies(list, configuration.ReportJobID);
			List<ActionDefinition> list3 = new List<ActionDefinition>();
			using (List<ActionDefinition>.Enumerator enumerator2 = reportJob.Actions.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					ActionDefinition actionDefinition = enumerator2.Current;
					if (!configuration.Actions.Any(delegate(ActionDefinition a)
					{
						int? id = a.ID;
						int? id2 = actionDefinition.ID;
						return id.GetValueOrDefault() == id2.GetValueOrDefault() & id != null == (id2 != null);
					}) && !actionDefinition.IsShared)
					{
						list3.Add(actionDefinition);
					}
				}
			}
			ActionsDAL actionsDAL = new ActionsDAL(creator);
			actionsDAL.SaveActionsForAssignments(configuration.ReportJobID, 1.ToString(), configuration.Actions);
			foreach (ActionDefinition actionDefinition2 in list3)
			{
				actionsDAL.DeleteAction(Convert.ToInt32(actionDefinition2.ID));
			}
			this.AddBLScheduler(configuration);
		}

		// Token: 0x060001BF RID: 447 RVA: 0x0000D7A4 File Offset: 0x0000B9A4
		public void DeleteReportJobs(List<int> reportJobIds)
		{
			CoreBusinessLayerService.log.DebugFormat("Deleting report job", Array.Empty<object>());
			List<ReportJobConfiguration> jobsByIds = ReportJobDAL.GetJobsByIds(reportJobIds);
			ISwisConnectionProxyCreator creator = SwisConnectionProxyPool.GetCreator();
			ActionsDAL actionsDAL = new ActionsDAL(creator);
			foreach (ReportJobConfiguration reportJobConfiguration in jobsByIds)
			{
				this.RemoveBLScheduler(reportJobConfiguration);
				actionsDAL.SaveActionsForAssignments(reportJobConfiguration.ReportJobID, 1.ToString(), new List<ActionDefinition>());
			}
			List<int> list = new List<int>();
			foreach (ReportJobConfiguration reportJobConfiguration2 in jobsByIds)
			{
				if (reportJobConfiguration2.Schedules != null)
				{
					list.AddRange(from x in reportJobConfiguration2.Schedules
					select x.FrequencyId);
				}
			}
			new FrequenciesDAL(creator).DeleteFrequencies(list);
			ReportJobDAL.DeleteReportJobs(reportJobIds);
		}

		// Token: 0x060001C0 RID: 448 RVA: 0x0000D8CC File Offset: 0x0000BACC
		private void RemoveBLScheduler(ReportJobConfiguration configuration)
		{
			if (configuration.Schedules != null)
			{
				for (int i = 0; i < configuration.Schedules.Count; i++)
				{
					Scheduler.Instance.Remove(string.Format("ReportJob-{0}_{1}", configuration.ReportJobID.ToString(), i));
				}
			}
		}

		// Token: 0x060001C1 RID: 449 RVA: 0x0000D91F File Offset: 0x0000BB1F
		private void AddBLScheduler(ReportJobConfiguration configuration)
		{
			ReportJobInitializer.AddActionsToScheduler(configuration, this);
		}

		// Token: 0x060001C2 RID: 450 RVA: 0x0000D928 File Offset: 0x0000BB28
		public List<ReportJobConfiguration> GetSchedulesWithReport(int reportId)
		{
			return ReportJobDAL.GetJobsWithReport(reportId);
		}

		// Token: 0x060001C3 RID: 451 RVA: 0x0000D930 File Offset: 0x0000BB30
		public int DublicateReportJob(int reportJobId, string jobName, int[] allowedReportIds)
		{
			CoreBusinessLayerService.log.DebugFormat("Dublicate report job", Array.Empty<object>());
			int num = ReportJobDAL.DublicateReportJob(reportJobId, jobName, allowedReportIds);
			ReportJobConfiguration reportJob = this.GetReportJob(reportJobId);
			reportJob.ReportJobID = num;
			ISwisConnectionProxyCreator creator = SwisConnectionProxyPool.GetCreator();
			ActionsDAL actionsDAL = new ActionsDAL(creator);
			foreach (ReportSchedule reportSchedule in reportJob.Schedules)
			{
				reportSchedule.FrequencyId = 0;
			}
			ReportSchedulesDAL.ScheduleReportJobWithFrequencies(new FrequenciesDAL(creator).SaveFrequencies(new List<ISchedule>(reportJob.Schedules)), num);
			foreach (ActionDefinition actionDefinition in reportJob.Actions)
			{
				actionDefinition.Title = actionsDAL.GetUniqueNameForAction(actionDefinition.Title);
				actionDefinition.IsShared = false;
			}
			actionsDAL.SaveActionsForAssignments(num, 1.ToString(), reportJob.Actions);
			this.AddBLScheduler(reportJob);
			return num;
		}

		// Token: 0x060001C4 RID: 452 RVA: 0x0000DA58 File Offset: 0x0000BC58
		public ReportJobConfiguration GetReportJob(int jobId)
		{
			CoreBusinessLayerService.log.DebugFormat("Extracting report job by ID", Array.Empty<object>());
			return ReportJobDAL.GetReportJob(jobId);
		}

		// Token: 0x060001C5 RID: 453 RVA: 0x0000DA74 File Offset: 0x0000BC74
		public void UnAssignReportsFromJob(int jobId, List<int> reportIds)
		{
			ReportJobDAL.UnAssignReportsFromJob(jobId, reportIds);
			ReportJobConfiguration reportJob = ReportJobDAL.GetReportJob(jobId);
			this.RemoveBLScheduler(reportJob);
			this.AddBLScheduler(reportJob);
		}

		// Token: 0x060001C6 RID: 454 RVA: 0x0000DAA0 File Offset: 0x0000BCA0
		public Dictionary<int, bool> RunNow(List<int> schedulesIds)
		{
			CoreBusinessLayerService.log.DebugFormat("Running job(s)", Array.Empty<object>());
			Dictionary<int, bool> dictionary = new Dictionary<int, bool>();
			List<ReportJobConfiguration> jobsByIds = ReportJobDAL.GetJobsByIds(schedulesIds);
			for (int i = 0; i < jobsByIds.Count; i++)
			{
				foreach (ActionDefinition actionDefinition in jobsByIds[i].Actions)
				{
					ReportingActionContext reportingActionContext = new ReportingActionContext
					{
						AccountID = jobsByIds[i].AccountID,
						UrlsGroupedByLeftPart = ReportJobInitializer.GroupUrls(jobsByIds[i]),
						WebsiteID = jobsByIds[i].WebsiteID
					};
					reportingActionContext.MacroContext.Add(new ReportingContext
					{
						AccountID = jobsByIds[i].AccountID,
						ScheduleName = jobsByIds[i].Name,
						ScheduleDescription = jobsByIds[i].Description,
						LastRun = jobsByIds[i].LastRun,
						WebsiteID = jobsByIds[i].WebsiteID
					});
					reportingActionContext.MacroContext.Add(new GenericContext());
					if (!dictionary.Keys.Contains(jobsByIds[i].ReportJobID))
					{
						dictionary.Add(jobsByIds[i].ReportJobID, this.ExecuteAction(actionDefinition, reportingActionContext).Status == 1);
					}
					else
					{
						ActionResult actionResult = this.ExecuteAction(actionDefinition, reportingActionContext);
						dictionary[jobsByIds[i].ReportJobID] = (actionResult.Status == 1 && dictionary[jobsByIds[i].ReportJobID]);
					}
				}
				jobsByIds[i].LastRun = new DateTime?(DateTime.Now.ToUniversalTime());
				ReportJobDAL.UpdateLastRun(jobsByIds[i].ReportJobID, jobsByIds[i].LastRun);
			}
			return dictionary;
		}

		// Token: 0x060001C7 RID: 455 RVA: 0x0000DCB0 File Offset: 0x0000BEB0
		[Obsolete("Core-Split cleanup. If you need this member please contact Core team", true)]
		public int[] GetListOfAllowedReports()
		{
			int[] result;
			try
			{
				using (IInformationServiceProxy2 informationServiceProxy = SwisConnectionProxyPool.GetCreator().Create())
				{
					DataTable dataTable = informationServiceProxy.Query("SELECT ReportID FROM Orion.Report");
					List<int> list = new List<int>();
					if (dataTable != null)
					{
						list.AddRange(from DataRow row in dataTable.Rows
						select (int)row["ReportID"]);
					}
					result = list.ToArray();
				}
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("GetLimitedReportIds failed.", ex);
				throw;
			}
			return result;
		}

		// Token: 0x060001C8 RID: 456 RVA: 0x0000DD58 File Offset: 0x0000BF58
		public List<SmtpServer> GetAvailableSmtpServers()
		{
			return SmtpServerDAL.GetAvailableSmtpServers();
		}

		// Token: 0x060001C9 RID: 457 RVA: 0x0000DD5F File Offset: 0x0000BF5F
		public bool DefaultSmtpServerExists()
		{
			return SmtpServerDAL.DefaultSmtpServerExists();
		}

		// Token: 0x060001CA RID: 458 RVA: 0x0000DD66 File Offset: 0x0000BF66
		public int InsertSmtpServer(SmtpServer server)
		{
			return SmtpServerDAL.InsertSmtpServer(server);
		}

		// Token: 0x060001CB RID: 459 RVA: 0x0000DD6E File Offset: 0x0000BF6E
		public SmtpServer GetSmtpServer(int id)
		{
			return SmtpServerDAL.GetSmtpServer(id);
		}

		// Token: 0x060001CC RID: 460 RVA: 0x0000DD76 File Offset: 0x0000BF76
		public SmtpServer GetSmtpServerByAddress(string address)
		{
			return SmtpServerDAL.GetSmtpServerByAddress(address);
		}

		// Token: 0x060001CD RID: 461 RVA: 0x0000DD7E File Offset: 0x0000BF7E
		public bool UpdateSmtpServer(SmtpServer server)
		{
			return SmtpServerDAL.UpdateSmtpServer(server);
		}

		// Token: 0x060001CE RID: 462 RVA: 0x0000DD86 File Offset: 0x0000BF86
		public bool DeleteSmtpServer(int id)
		{
			return SmtpServerDAL.DeleteSmtpServer(id);
		}

		// Token: 0x060001CF RID: 463 RVA: 0x0000DD8E File Offset: 0x0000BF8E
		public void SetSmtpServerAsDefault(int id)
		{
			SmtpServerDAL.SetSmtpServerAsDefault(id);
		}

		// Token: 0x060001D0 RID: 464 RVA: 0x0000DD96 File Offset: 0x0000BF96
		public bool SNMPQuery(int nodeId, string oid, string snmpGetType, out Dictionary<string, string> response)
		{
			return SNMPHelper.SNMPQuery(nodeId, snmpGetType, oid, out response);
		}

		// Token: 0x060001D1 RID: 465 RVA: 0x0000DDA4 File Offset: 0x0000BFA4
		private static bool AreRelatedOids(string queryOid, string returnedOid)
		{
			if (queryOid.Equals(returnedOid))
			{
				return true;
			}
			string[] array = queryOid.Split(new char[]
			{
				'.'
			});
			string[] array2 = returnedOid.Split(new char[]
			{
				'.'
			});
			if (array.Length > array2.Length)
			{
				return false;
			}
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].CompareTo(array2[i]) != 0)
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060001D2 RID: 466 RVA: 0x0000DE08 File Offset: 0x0000C008
		private static string GetOidValueFromXmlNodes(XmlNode[] xmlNodes)
		{
			string result = string.Empty;
			object obj;
			if (xmlNodes == null)
			{
				obj = null;
			}
			else
			{
				obj = xmlNodes.FirstOrDefault((XmlNode item) => item is XmlText);
			}
			XmlText xmlText = (XmlText)obj;
			if (xmlText != null)
			{
				result = xmlText.Value;
			}
			return result;
		}

		// Token: 0x060001D3 RID: 467 RVA: 0x0000DE58 File Offset: 0x0000C058
		private static bool NodeSnmpQueryProcessGetOrGetNextResult(string oid, Dictionary<string, string> response, SnmpJobResults jobResult)
		{
			string oidValueFromXmlNodes = CoreBusinessLayerService.GetOidValueFromXmlNodes(jobResult.Results[0].OIDs[0].Value as XmlNode[]);
			if (string.IsNullOrEmpty(oidValueFromXmlNodes))
			{
				response["swerror"] = Resources.LIBCODE_PS0_18;
				return false;
			}
			if (CoreBusinessLayerService.AreRelatedOids(oid, jobResult.Results[0].OIDs[0].OID))
			{
				response["0"] = oidValueFromXmlNodes;
				return true;
			}
			response["swerror"] = Resources.LIBCODE_PS0_19;
			return false;
		}

		// Token: 0x060001D4 RID: 468 RVA: 0x0000DEEC File Offset: 0x0000C0EC
		private static bool NodeSnmpQueryProcessSubtreeResult(string oid, Dictionary<string, string> response, SnmpJobResults jobResult)
		{
			if (jobResult.Results[0].OIDs.Count == 0)
			{
				return false;
			}
			string a = oid;
			bool result = true;
			foreach (SnmpOID snmpOID in jobResult.Results[0].OIDs)
			{
				if (snmpOID.OID.StartsWith(oid + ".") && a != snmpOID.OID)
				{
					a = snmpOID.OID;
					string key = "0";
					if (snmpOID.OID.Length > oid.Length + 1)
					{
						key = snmpOID.OID.Substring(oid.Length + 1);
					}
					string oidValueFromXmlNodes = CoreBusinessLayerService.GetOidValueFromXmlNodes(snmpOID.Value as XmlNode[]);
					if (string.IsNullOrEmpty(oidValueFromXmlNodes))
					{
						response[key] = Resources.LIBCODE_PS0_19;
						result = false;
					}
					else
					{
						response[key] = oidValueFromXmlNodes;
					}
				}
			}
			return result;
		}

		// Token: 0x060001D5 RID: 469 RVA: 0x0000E008 File Offset: 0x0000C208
		public bool NodeSNMPQuery(Node node, string oid, string snmpGetType, out Dictionary<string, string> response)
		{
			SnmpRequestType snmpRequestType;
			if (!Enum.TryParse<SnmpRequestType>(snmpGetType, true, out snmpRequestType))
			{
				snmpRequestType = 0;
			}
			List<SnmpRequest> list = new List<SnmpRequest>();
			list.Add(new SnmpRequest
			{
				OID = oid,
				IsTransform = false,
				RequestType = snmpRequestType
			});
			int num = SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-SNMP Timeout", 2500) / 1000;
			JobDescription jobDescription = SnmpJob.CreateJobDescription(node.IpAddress, node.SNMPPort, num, node.SNMPVersion, list, BusinessLayerSettings.Instance.TestJobTimeout);
			SnmpCredentialsV2 jobCredential = new SnmpCredentialsV2(node.ReadOnlyCredentials);
			response = new Dictionary<string, string>();
			string text;
			SnmpJobResults snmpJobResults = this.ExecuteJobAndGetResult<SnmpJobResults>(jobDescription, jobCredential, JobResultDataFormatType.Xml, "SNMP", out text);
			if (!snmpJobResults.Success || snmpJobResults.Results.Count == 0)
			{
				return false;
			}
			if (snmpJobResults.Results[0].ResultType != null)
			{
				response["swerror"] = snmpJobResults.Results[0].ErrorMessage;
				return false;
			}
			if (snmpRequestType == null || snmpRequestType == 1)
			{
				return CoreBusinessLayerService.NodeSnmpQueryProcessGetOrGetNextResult(oid, response, snmpJobResults);
			}
			return CoreBusinessLayerService.NodeSnmpQueryProcessSubtreeResult(oid, response, snmpJobResults);
		}

		// Token: 0x060001D6 RID: 470 RVA: 0x0000E116 File Offset: 0x0000C316
		[Obsolete("Core-Split cleanup. If you need this member please contact Core team", true)]
		public void SNMPQueryForIp(string ip, string oid, List<SnmpEntry> credentials, string snmpGetType, out Dictionary<string, string> response)
		{
			SNMPHelper.SNMPQueryForIp(ip, oid, credentials, snmpGetType, out response);
		}

		// Token: 0x060001D7 RID: 471 RVA: 0x0000E124 File Offset: 0x0000C324
		public Dictionary<string, Dictionary<string, string>> GetColumns(string tableOID, int nodeId)
		{
			return SNMPHelper.GetColumns(tableOID, nodeId);
		}

		// Token: 0x060001D8 RID: 472 RVA: 0x0000E130 File Offset: 0x0000C330
		public bool ValidateSNMP(SNMPVersion snmpVersion, string ip, uint snmpPort, string community, string authKey, bool authKeyIsPwd, SNMPv3AuthType authType, SNMPv3PrivacyType privacyType, string privacyPassword, bool privKeyIsPwd, string context, string username)
		{
			string text;
			return this.ValidateSNMPWithErrorMessage(snmpVersion, ip, snmpPort, community, authKey, authKeyIsPwd, authType, privacyType, privacyPassword, privKeyIsPwd, context, username, out text);
		}

		// Token: 0x060001D9 RID: 473 RVA: 0x0000E15C File Offset: 0x0000C35C
		public bool ValidateSNMPWithErrorMessage(SNMPVersion snmpVersion, string ip, uint snmpPort, string community, string authKey, bool authKeyIsPwd, SNMPv3AuthType authType, SNMPv3PrivacyType privacyType, string privacyPassword, bool privKeyIsPwd, string context, string username, out string localizedErrorMessage)
		{
			List<SnmpRequest> list = new List<SnmpRequest>();
			list.Add(new SnmpRequest
			{
				OID = "1.3.6.1.2.1.1.2.0",
				IsTransform = false,
				OIDLabel = "sysObjectID",
				RequestType = 0
			});
			localizedErrorMessage = string.Empty;
			int num = SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-SNMP Timeout", 2500) / 1000;
			JobDescription jobDescription = SnmpJob.CreateJobDescription(ip, snmpPort, num, snmpVersion, list, BusinessLayerSettings.Instance.TestJobTimeout);
			SnmpCredentialsV2 jobCredential = new SnmpCredentialsV2
			{
				CommunityString = community,
				CredentialName = "",
				SNMPV3AuthKeyIsPwd = authKeyIsPwd,
				SNMPv3AuthType = authType,
				SNMPv3AuthPassword = authKey,
				SNMPv3PrivacyType = privacyType,
				SNMPv3PrivacyPassword = privacyPassword,
				SNMPV3PrivKeyIsPwd = privKeyIsPwd,
				SnmpV3Context = context,
				SNMPv3UserName = username
			};
			string text;
			SnmpJobResults snmpJobResults = this.ExecuteJobAndGetResult<SnmpJobResults>(jobDescription, jobCredential, JobResultDataFormatType.Xml, "SNMP", out text);
			if (!snmpJobResults.Success)
			{
				localizedErrorMessage = snmpJobResults.Message;
				return false;
			}
			bool flag = snmpJobResults.Results.Count > 0 && snmpJobResults.Results[0].ResultType == 0;
			CoreBusinessLayerService.log.InfoFormat("SNMP credential test finished. Success: {0}", flag);
			return flag;
		}

		// Token: 0x060001DA RID: 474 RVA: 0x0000E294 File Offset: 0x0000C494
		public bool ValidateReadWriteSNMP(SNMPVersion snmpVersion, string ip, uint snmpPort, string community, string authKey, bool authKeyIsPwd, SNMPv3AuthType authType, SNMPv3PrivacyType privacyType, string privacyPassword, bool privKeyIsPwd, string context, string username)
		{
			int num = SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-SNMP Timeout", 2500) / 1000;
			JobDescription jobDescription = SnmpReadWriteCredentialValidateJob.CreateJobDescription(ip, snmpPort, num, snmpVersion, BusinessLayerSettings.Instance.TestJobTimeout);
			SnmpCredentialsV2 jobCredential = new SnmpCredentialsV2
			{
				CommunityString = community,
				CredentialName = "",
				SNMPV3AuthKeyIsPwd = authKeyIsPwd,
				SNMPv3AuthType = authType,
				SNMPv3AuthPassword = authKey,
				SNMPv3PrivacyType = privacyType,
				SNMPv3PrivacyPassword = privacyPassword,
				SNMPV3PrivKeyIsPwd = privKeyIsPwd,
				SnmpV3Context = context,
				SNMPv3UserName = username
			};
			string text;
			ValidateJobResult validateJobResult = this.ExecuteJobAndGetResult<ValidateJobResult>(jobDescription, jobCredential, JobResultDataFormatType.Xml, "SNMP", out text);
			CoreBusinessLayerService.log.InfoFormat(string.Format("SNMP read/write credential test finished. Success: {0}.", validateJobResult.IsValid), Array.Empty<object>());
			return validateJobResult.IsValid;
		}

		// Token: 0x060001DB RID: 475 RVA: 0x0000E360 File Offset: 0x0000C560
		private void SnmpEncodingSettingsChanged(object sender, SettingsChangedEventArgs e)
		{
			try
			{
				if (SnmpSettings.Instance.Encoding == SNMPEncoding.Auto.GetWebName())
				{
					string autoEncoding = SNMPHelper.GetAutoEncoding();
					CoreBusinessLayerService.log.InfoFormat("Set encoding {0} for primary locale {1}", autoEncoding, LocaleConfiguration.PrimaryLocale);
					SNMPEncodingSettings.Instance.ChangeEncoding(Encoding.GetEncoding(autoEncoding));
				}
				else
				{
					SNMPEncodingSettings.Instance.ChangeEncoding(Encoding.GetEncoding(SnmpSettings.Instance.Encoding));
				}
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Unable to save text encoding setting for snmp. Encoding will be system default: " + Encoding.Default.WebName, ex);
			}
		}

		// Token: 0x060001DC RID: 476 RVA: 0x0000E400 File Offset: 0x0000C600
		[Obsolete("Use GetSnmpV3CredentialsSet method")]
		public List<string> GetCredentialsSet()
		{
			return this.GetSnmpV3CredentialsSet().Values.ToList<string>();
		}

		// Token: 0x060001DD RID: 477 RVA: 0x0000E414 File Offset: 0x0000C614
		public IDictionary<int, string> GetSnmpV3CredentialsSet()
		{
			IDictionary<int, string> credentialNames;
			try
			{
				credentialNames = new CredentialManager().GetCredentialNames<SnmpCredentialsV3>("Orion");
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error getting Snmp v3 credentials", ex);
				throw;
			}
			return credentialNames;
		}

		// Token: 0x060001DE RID: 478 RVA: 0x0000E458 File Offset: 0x0000C658
		[Obsolete("Use InsertSnmpV3Credentials method")]
		public void InsertCredentials(SnmpCredentials crendentials)
		{
			Credential credential = CredentialHelper.ParseCredentials(crendentials);
			this.InsertSnmpV3Credentials((SnmpCredentialsV3)credential);
		}

		// Token: 0x060001DF RID: 479 RVA: 0x0000E47C File Offset: 0x0000C67C
		public int? InsertSnmpV3Credentials(SnmpCredentialsV3 credentials)
		{
			int? id;
			try
			{
				new CredentialManager().AddCredential<SnmpCredentialsV3>("Orion", credentials);
				id = credentials.ID;
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error inserting Snmp v3 credentials", ex);
				throw;
			}
			return id;
		}

		// Token: 0x060001E0 RID: 480 RVA: 0x0000E4C8 File Offset: 0x0000C6C8
		[Obsolete("Use DeleteSnmpV3Credentials method")]
		public void DeleteCredentials(string CredentialName)
		{
			this.DeleteSnmpV3Credentials(CredentialName);
		}

		// Token: 0x060001E1 RID: 481 RVA: 0x0000E4D4 File Offset: 0x0000C6D4
		public void DeleteSnmpV3Credentials(string CredentialName)
		{
			try
			{
				CredentialManager credentialManager = new CredentialManager();
				IEnumerable<SnmpCredentialsV3> credentials = credentialManager.GetCredentials<SnmpCredentialsV3>("Orion", CredentialName);
				credentialManager.DeleteCredential<SnmpCredentialsV3>("Orion", credentials.FirstOrDefault<SnmpCredentialsV3>());
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error(string.Format("Error deleting Snmp v3 credentials by name {0}", CredentialName), ex);
				throw;
			}
		}

		// Token: 0x060001E2 RID: 482 RVA: 0x0000E530 File Offset: 0x0000C730
		public void DeleteSnmpV3CredentialsByID(int CredentialID)
		{
			try
			{
				new CredentialManager().DeleteCredential("Orion", CredentialID);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error(string.Format("Error deleting Snmp v3 credentials by id {0}", CredentialID), ex);
				throw;
			}
		}

		// Token: 0x060001E3 RID: 483 RVA: 0x0000E580 File Offset: 0x0000C780
		[Obsolete("Use GetSnmpV3Credentials method")]
		public SnmpCredentials GetCredentials(string CredentialName)
		{
			SnmpCredentials result;
			try
			{
				result = SnmpCredentials.CreateSnmpCredentials(CredentialHelper.GetSnmpEntry(new CredentialManager().GetCredentials<SnmpCredentialsV3>("Orion", CredentialName).FirstOrDefault<SnmpCredentialsV3>()));
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error(string.Format("Error getting Snmp v3 credentials by name {0}", CredentialName), ex);
				throw;
			}
			return result;
		}

		// Token: 0x060001E4 RID: 484 RVA: 0x0000E5DC File Offset: 0x0000C7DC
		public SnmpCredentialsV3 GetSnmpV3Credentials(string CredentialName)
		{
			SnmpCredentialsV3 result;
			try
			{
				result = new CredentialManager().GetCredentials<SnmpCredentialsV3>("Orion", CredentialName).FirstOrDefault<SnmpCredentialsV3>();
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error(string.Format("Error getting Snmp v3 credentials by name {0}", CredentialName), ex);
				throw;
			}
			return result;
		}

		// Token: 0x060001E5 RID: 485 RVA: 0x0000E62C File Offset: 0x0000C82C
		public SnmpCredentialsV3 GetSnmpV3CredentialsByID(int CredentialID)
		{
			SnmpCredentialsV3 result;
			try
			{
				result = new CredentialManager().GetCredentials<SnmpCredentialsV3>(new List<int>
				{
					CredentialID
				}).FirstOrDefault<SnmpCredentialsV3>();
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error(string.Format("Error getting Snmp v3 credentials by id {0}", CredentialID), ex);
				throw;
			}
			return result;
		}

		// Token: 0x060001E6 RID: 486 RVA: 0x0000E688 File Offset: 0x0000C888
		[Obsolete("Use UpdateSnmpV3Credentials method")]
		public void UpdateCredentials(SnmpCredentials credentials)
		{
			try
			{
				Credential credential = CredentialHelper.ParseCredentials(credentials);
				new CredentialManager().UpdateCredential<SnmpCredentialsV3>("Orion", (SnmpCredentialsV3)credential);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error updating Snmp v3 credentials", ex);
				throw;
			}
		}

		// Token: 0x060001E7 RID: 487 RVA: 0x0000E6D8 File Offset: 0x0000C8D8
		public void UpdateSnmpV3Credentials(SnmpCredentialsV3 credentials)
		{
			try
			{
				new CredentialManager().UpdateCredential<SnmpCredentialsV3>("Orion", credentials);
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error updating Snmp v3 credentials", ex);
				throw;
			}
		}

		// Token: 0x060001E8 RID: 488 RVA: 0x0000E71C File Offset: 0x0000C91C
		public StringDictionary GetSeverities()
		{
			return SysLogDAL.GetSeverities();
		}

		// Token: 0x060001E9 RID: 489 RVA: 0x0000E723 File Offset: 0x0000C923
		public StringDictionary GetFacilities()
		{
			return SysLogDAL.GetFacilities();
		}

		// Token: 0x060001EA RID: 490 RVA: 0x0000E72A File Offset: 0x0000C92A
		public BaselineValues GetBaselineValues(string thresholdName, int instanceId)
		{
			return ThresholdProcessingManager.Instance.Engine.GetBaselineValues(thresholdName, instanceId);
		}

		// Token: 0x060001EB RID: 491 RVA: 0x0000E73D File Offset: 0x0000C93D
		public List<BaselineValues> GetBaselineValuesForAllTimeFrames(string thresholdName, int instanceId)
		{
			return ThresholdProcessingManager.Instance.Engine.GetBaselineValuesForAllTimeFrames(thresholdName, instanceId);
		}

		// Token: 0x060001EC RID: 492 RVA: 0x0000E750 File Offset: 0x0000C950
		public ThresholdComputationResult ComputeThresholds(string thresholdName, int instanceId, string warningFormula, string criticalFormula, BaselineValues baselineValues, ThresholdOperatorEnum thresholdOperator)
		{
			return ThresholdProcessingManager.Instance.Engine.ComputeThresholds(thresholdName, instanceId, warningFormula, criticalFormula, baselineValues, thresholdOperator);
		}

		// Token: 0x060001ED RID: 493 RVA: 0x0000E76A File Offset: 0x0000C96A
		public ThresholdComputationResult ProcessThresholds(double warningThreshold, double criticalThreshold, ThresholdOperatorEnum oper, ThresholdMinMaxValue minMaxValues)
		{
			return ThresholdProcessingManager.Instance.Engine.ProcessThresholds(warningThreshold, criticalThreshold, oper, minMaxValues);
		}

		// Token: 0x060001EE RID: 494 RVA: 0x0000E780 File Offset: 0x0000C980
		public ThresholdComputationResult ProcessThresholds(bool warningEnabled, double warningThreshold, bool criticalEnabled, double criticalThreshold, ThresholdOperatorEnum oper, ThresholdMinMaxValue minMaxValues)
		{
			return ThresholdProcessingManager.Instance.Engine.ProcessThresholds(warningEnabled, warningThreshold, criticalEnabled, criticalThreshold, oper, minMaxValues);
		}

		// Token: 0x060001EF RID: 495 RVA: 0x0000E79A File Offset: 0x0000C99A
		public ValidationResult IsFormulaValid(string thresholdName, string formula, ThresholdLevel level, ThresholdOperatorEnum thresholdOperator)
		{
			return ThresholdProcessingManager.Instance.Engine.IsFormulaValid(thresholdName, formula, level, thresholdOperator);
		}

		// Token: 0x060001F0 RID: 496 RVA: 0x0000E7B0 File Offset: 0x0000C9B0
		public ThresholdMinMaxValue GetThresholdMinMaxValues(string thresholdName, int instanceId)
		{
			return ThresholdProcessingManager.Instance.Engine.GetThresholdMinMaxValues(thresholdName, instanceId);
		}

		// Token: 0x060001F1 RID: 497 RVA: 0x0000E7C3 File Offset: 0x0000C9C3
		public int SetThreshold(Threshold threshold)
		{
			return ThresholdProcessingManager.Instance.Engine.SetThreshold(threshold);
		}

		// Token: 0x060001F2 RID: 498 RVA: 0x0000E7D5 File Offset: 0x0000C9D5
		public StatisticalDataHistogram[] GetHistogramForStatisticalData(string thresholdName, int instanceId)
		{
			return ThresholdProcessingManager.Instance.Engine.GetHistogramForStatisticalData(thresholdName, instanceId);
		}

		// Token: 0x060001F3 RID: 499 RVA: 0x0000E7E8 File Offset: 0x0000C9E8
		public string GetStatisticalDataChartName(string thresholdName)
		{
			return ThresholdProcessingManager.Instance.Engine.GetStatisticalDataChartName(thresholdName);
		}

		// Token: 0x060001F4 RID: 500 RVA: 0x0000E7FA File Offset: 0x0000C9FA
		public string GetThresholdInstanceName(string thresholdName, int instanceId)
		{
			return ThresholdProcessingManager.Instance.Engine.GetThresholdInstanceName(thresholdName, instanceId);
		}

		// Token: 0x060001F5 RID: 501 RVA: 0x0000E80D File Offset: 0x0000CA0D
		public TracerouteResult TraceRoute(string destinationHostNameOrIpAddress)
		{
			return CoreBusinessLayerService.CreateTraceRouteProvider().TraceRoute(destinationHostNameOrIpAddress);
		}

		// Token: 0x060001F6 RID: 502 RVA: 0x0000E81A File Offset: 0x0000CA1A
		private static ITraceRouteProvider CreateTraceRouteProvider()
		{
			return new TraceRouteProviderSync();
		}

		// Token: 0x060001F7 RID: 503 RVA: 0x0000E821 File Offset: 0x0000CA21
		public Views GetSummaryDetailsViews()
		{
			return ViewsDAL.GetSummaryDetailsViews();
		}

		// Token: 0x060001F8 RID: 504 RVA: 0x0000E828 File Offset: 0x0000CA28
		public void DeleteVolume(Volume volume)
		{
			Dictionary<string, object> volumeBaseInfo = CoreBusinessLayerService.GetVolumeBaseInfo(volume);
			VolumeDAL.DeleteVolume(volume);
			VolumeIndication volumeIndication = new VolumeIndication(1, volume);
			foreach (KeyValuePair<string, object> keyValuePair in volumeBaseInfo)
			{
				volumeIndication.AddSourceInstanceProperty(keyValuePair.Key, keyValuePair.Value);
			}
			volumeIndication.AddIndicationProperty("SourceInstanceUri", volumeBaseInfo["Uri"]);
			IndicationPublisher.CreateV3().ReportIndication(volumeIndication);
		}

		// Token: 0x060001F9 RID: 505 RVA: 0x0000E8BC File Offset: 0x0000CABC
		public int InsertVolume(Volume volume)
		{
			int maxElementCount = new FeatureManager().GetMaxElementCount(WellKnownElementTypes.Volumes);
			if (VolumeDAL.GetVolumeCount() >= maxElementCount)
			{
				throw LicenseException.FromElementsExceeded(maxElementCount);
			}
			int result = VolumeDAL.InsertVolume(volume);
			CoreBusinessLayerService.FireVolumeIndication(0, volume, null);
			return result;
		}

		// Token: 0x060001FA RID: 506 RVA: 0x0000E8F8 File Offset: 0x0000CAF8
		public void UpdateVolume(Volume volume)
		{
			PropertyBag changedProperties = VolumeDAL.UpdateVolume(volume);
			CoreBusinessLayerService.FireVolumeIndication(2, volume, changedProperties);
		}

		// Token: 0x060001FB RID: 507 RVA: 0x0000E914 File Offset: 0x0000CB14
		public Volume GetVolume(int volumeID)
		{
			return VolumeDAL.GetVolume(volumeID);
		}

		// Token: 0x060001FC RID: 508 RVA: 0x0000E91C File Offset: 0x0000CB1C
		public void BulkUpdateVolumePollingInterval(int pollingInterval, int engineId)
		{
			VolumeDAL.BulkUpdateVolumePollingInterval(pollingInterval, engineId);
		}

		// Token: 0x060001FD RID: 509 RVA: 0x0000E925 File Offset: 0x0000CB25
		public Dictionary<string, object> GetVolumeCustomProperties(int volumeId, ICollection<string> properties)
		{
			return VolumeDAL.GetVolumeCustomProperties(volumeId, properties);
		}

		// Token: 0x060001FE RID: 510 RVA: 0x0000E92E File Offset: 0x0000CB2E
		public Volumes GetVolumesByIds(int[] volumeIds)
		{
			return VolumeDAL.GetVolumesByIds(volumeIds);
		}

		// Token: 0x060001FF RID: 511 RVA: 0x0000E938 File Offset: 0x0000CB38
		private static void FireVolumeIndication(IndicationType indicationType, Volume volume, PropertyBag changedProperties = null)
		{
			try
			{
				Dictionary<string, object> volumeBaseInfo = CoreBusinessLayerService.GetVolumeBaseInfo(volume);
				VolumeIndication volumeIndication = new VolumeIndication(indicationType, volume);
				if (indicationType <= 2)
				{
					volumeIndication.AddIndicationProperty("SourceInstanceUri", volumeBaseInfo["Uri"]);
				}
				foreach (KeyValuePair<string, object> keyValuePair in volumeBaseInfo)
				{
					volumeIndication.AddSourceInstanceProperty(keyValuePair.Key, keyValuePair.Value);
				}
				if (changedProperties != null)
				{
					foreach (KeyValuePair<string, object> keyValuePair2 in changedProperties)
					{
						volumeIndication.AddSourceInstanceProperty(keyValuePair2.Key, keyValuePair2.Value);
					}
				}
				IndicationPublisher.CreateV3().ReportIndication(volumeIndication);
			}
			catch (Exception ex)
			{
				string text = string.Format("Error delivering indication {0} for Volume '{1}' with id {2}.", indicationType, volume.ID, volume.Caption);
				CoreBusinessLayerService.log.Error(text, ex);
			}
		}

		// Token: 0x06000200 RID: 512 RVA: 0x0000EA58 File Offset: 0x0000CC58
		private static Dictionary<string, object> GetVolumeBaseInfo(Volume volume)
		{
			return new SwisEntityHelper(CoreBusinessLayerService.CreateProxy()).GetProperties("Orion.Volumes", volume.VolumeId, new string[]
			{
				"DisplayName",
				"Uri"
			});
		}

		// Token: 0x06000201 RID: 513 RVA: 0x0000EA8A File Offset: 0x0000CC8A
		public ExternalWebsites GetExternalWebsites()
		{
			return ExternalWebsitesDAL.GetAll();
		}

		// Token: 0x06000202 RID: 514 RVA: 0x0000EA91 File Offset: 0x0000CC91
		public ExternalWebsite GetExternalWebsite(int id)
		{
			return ExternalWebsitesDAL.Get(id);
		}

		// Token: 0x06000203 RID: 515 RVA: 0x0000EA99 File Offset: 0x0000CC99
		public int CreateExternalWebsite(ExternalWebsite site)
		{
			return ExternalWebsitesDAL.Insert(site);
		}

		// Token: 0x06000204 RID: 516 RVA: 0x0000EAA1 File Offset: 0x0000CCA1
		public void UpdateExternalWebsite(ExternalWebsite site)
		{
			ExternalWebsitesDAL.Update(site);
		}

		// Token: 0x06000205 RID: 517 RVA: 0x0000EAA9 File Offset: 0x0000CCA9
		public void DeleteExternalWebsite(int id)
		{
			ExternalWebsitesDAL.Delete(id);
		}

		// Token: 0x06000206 RID: 518 RVA: 0x0000EAB4 File Offset: 0x0000CCB4
		public void AddNewWebMenuItemToMenubar(WebMenuItem item, string menubarName)
		{
			int itemId = WebMenubarDAL.InsertItem(item);
			WebMenubarDAL.AppendItemToMenu(menubarName, itemId);
		}

		// Token: 0x06000207 RID: 519 RVA: 0x0000EACF File Offset: 0x0000CCCF
		public void DeleteWebMenuItemByLink(string link)
		{
			WebMenubarDAL.DeleteItemByLink(link);
		}

		// Token: 0x06000208 RID: 520 RVA: 0x0000EAD7 File Offset: 0x0000CCD7
		public void RenameWebMenuItemByLink(string newName, string newDescription, string newMenuBar, string link)
		{
			WebMenubarDAL.RenameItemByLink(newName, newDescription, newMenuBar, link);
		}

		// Token: 0x06000209 RID: 521 RVA: 0x0000EAE3 File Offset: 0x0000CCE3
		public bool MenuItemExists(string link)
		{
			return WebMenubarDAL.MenuItemExists(link);
		}

		// Token: 0x0600020A RID: 522 RVA: 0x0000EAEB File Offset: 0x0000CCEB
		public RemoteAccessToken GetUserWebIntegrationToken(string username)
		{
			return new RemoteAuthManager().GetUserToken(username);
		}

		// Token: 0x0600020B RID: 523 RVA: 0x0000EAF8 File Offset: 0x0000CCF8
		public bool IsUserWebIntegrationAvailable(string username)
		{
			return new RemoteAuthManager().IsUserAvailable(username);
		}

		// Token: 0x0600020C RID: 524 RVA: 0x0000EB05 File Offset: 0x0000CD05
		public void DisableUserWebIntegration(string username)
		{
			new RemoteAuthManager().DisableUser(username);
		}

		// Token: 0x0600020D RID: 525 RVA: 0x0000EB14 File Offset: 0x0000CD14
		public RemoteAccessToken ConfigureUserWebIntegration(string username, string clientId, string clientPassword)
		{
			RemoteAuthManager remoteAuthManager = new RemoteAuthManager();
			RemoteAccessToken result;
			try
			{
				result = remoteAuthManager.ConfigureUser(username, clientId, clientPassword);
			}
			catch (Exception ex)
			{
				throw MessageUtilities.NewFaultException<CoreFaultContract>(ex);
			}
			return result;
		}

		// Token: 0x0600020E RID: 526 RVA: 0x0000EB4C File Offset: 0x0000CD4C
		public IEnumerable<MaintenanceStatus> GetMaintenanceInfoFromCustomerPortal(string username)
		{
			IEnumerable<MaintenanceStatus> result;
			try
			{
				result = this.MaintenanceInfoCache[username];
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error(ex);
				throw;
			}
			return result;
		}

		// Token: 0x0600020F RID: 527 RVA: 0x0000EB88 File Offset: 0x0000CD88
		public LicenseAndManagementInfo GetLicenseAndMaintenanceSummary(string username)
		{
			return this.LAMInfoCache[username];
		}

		// Token: 0x06000210 RID: 528 RVA: 0x0000EB98 File Offset: 0x0000CD98
		public IEnumerable<SupportCase> GetSupportCases(string username)
		{
			IEnumerable<SupportCase> result;
			try
			{
				result = this.SupportCasesCache[username];
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error(ex);
				throw;
			}
			return result;
		}

		// Token: 0x06000211 RID: 529 RVA: 0x0000EBD4 File Offset: 0x0000CDD4
		private IEnumerable<MaintenanceStatus> GetMaintenanceInfoFromCustomerPortalInternal(string username)
		{
			return (from i in new RemoteMaintenanceClient(username).GetMaintenanceInfo()
			select i.ToMaintenanceStatus()).ToList<MaintenanceStatus>();
		}

		// Token: 0x06000212 RID: 530 RVA: 0x0000EC0C File Offset: 0x0000CE0C
		private LicenseAndManagementInfo GetLicenseAndMaintenanceSummaryInternal(string username)
		{
			int maintenanceActiveCount;
			int maintenanceExpiringCount;
			int maintenanceExpiredCount;
			if (this.IsUserWebIntegrationAvailable(username))
			{
				IEnumerable<MaintenanceStatus> enumerable = this.GetMaintenanceInfoFromCustomerPortal(username);
				enumerable = ((enumerable == null) ? new List<MaintenanceStatus>() : enumerable.ToList<MaintenanceStatus>());
				maintenanceActiveCount = enumerable.Count((MaintenanceStatus m) => (int)(m.ExpirationDate - DateTime.UtcNow.Date).TotalDays >= 90);
				maintenanceExpiringCount = enumerable.Count((MaintenanceStatus m) => (int)(m.ExpirationDate - DateTime.UtcNow.Date).TotalDays < 90 && (int)(m.ExpirationDate - DateTime.UtcNow.Date).TotalDays > 0);
				maintenanceExpiredCount = enumerable.Count((MaintenanceStatus m) => (int)(m.ExpirationDate - DateTime.UtcNow.Date).TotalDays <= 0);
			}
			else
			{
				List<ModuleLicenseInfo> source = (from m in this.GetModuleLicenseInformation()
				where !string.Equals("DPI", m.ModuleName, StringComparison.OrdinalIgnoreCase) && !m.IsEval
				select m).ToList<ModuleLicenseInfo>();
				maintenanceActiveCount = source.Count((ModuleLicenseInfo m) => (int)(m.MaintenanceExpiration.Date - DateTime.UtcNow.Date).TotalDays >= 90);
				maintenanceExpiringCount = source.Count((ModuleLicenseInfo m) => (int)(m.MaintenanceExpiration.Date - DateTime.UtcNow.Date).TotalDays < 90 && (int)(m.MaintenanceExpiration.Date - DateTime.UtcNow.Date).TotalDays > 0);
				maintenanceExpiredCount = source.Count((ModuleLicenseInfo m) => (int)(m.MaintenanceExpiration.Date - DateTime.UtcNow.Date).TotalDays <= 0);
			}
			int evaluationExpiringCount = this.GetModuleLicenseInformation().Count((ModuleLicenseInfo m) => !string.Equals("DPI", m.ModuleName, StringComparison.OrdinalIgnoreCase) && m.IsEval && !m.IsRC);
			int count = this.GetModuleSaturationInformation().Count;
			int count2 = this.GetMaintenanceRenewalNotificationItems(false).Count;
			return new LicenseAndManagementInfo
			{
				UpdatesAvailableCount = count2,
				LicenseLimitReachedCount = count,
				EvaluationExpiringCount = evaluationExpiringCount,
				MaintenanceExpiringCount = maintenanceExpiringCount,
				MaintenanceActiveCount = maintenanceActiveCount,
				MaintenanceExpiredCount = maintenanceExpiredCount
			};
		}

		// Token: 0x06000213 RID: 531 RVA: 0x0000EDD3 File Offset: 0x0000CFD3
		private IEnumerable<SupportCase> GetSupportCasesInternal(string username)
		{
			return (from c in new RemoteSupportCasesClient(username).GetSupportCases()
			select c.ToSupportCase()).ToList<SupportCase>();
		}

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000214 RID: 532 RVA: 0x0000EE0C File Offset: 0x0000D00C
		private ExpirableCache<string, IEnumerable<MaintenanceStatus>> MaintenanceInfoCache
		{
			get
			{
				ExpirableCache<string, IEnumerable<MaintenanceStatus>> result;
				if ((result = this._maintenanceInfoCache) == null)
				{
					result = (this._maintenanceInfoCache = new ExpirableCache<string, IEnumerable<MaintenanceStatus>>(TimeSpan.FromMinutes(5.0), new Func<string, IEnumerable<MaintenanceStatus>>(this.GetMaintenanceInfoFromCustomerPortalInternal)));
				}
				return result;
			}
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000215 RID: 533 RVA: 0x0000EE4C File Offset: 0x0000D04C
		private ExpirableCache<string, LicenseAndManagementInfo> LAMInfoCache
		{
			get
			{
				ExpirableCache<string, LicenseAndManagementInfo> result;
				if ((result = this._LAMInfoCache) == null)
				{
					result = (this._LAMInfoCache = new ExpirableCache<string, LicenseAndManagementInfo>(TimeSpan.FromMinutes(1.0), new Func<string, LicenseAndManagementInfo>(this.GetLicenseAndMaintenanceSummaryInternal)));
				}
				return result;
			}
		}

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000216 RID: 534 RVA: 0x0000EE8C File Offset: 0x0000D08C
		private ExpirableCache<string, IEnumerable<SupportCase>> SupportCasesCache
		{
			get
			{
				ExpirableCache<string, IEnumerable<SupportCase>> result;
				if ((result = this._supportCasesCache) == null)
				{
					result = (this._supportCasesCache = new ExpirableCache<string, IEnumerable<SupportCase>>(TimeSpan.FromMinutes(5.0), new Func<string, IEnumerable<SupportCase>>(this.GetSupportCasesInternal)));
				}
				return result;
			}
		}

		// Token: 0x06000217 RID: 535 RVA: 0x0000EECB File Offset: 0x0000D0CB
		public WebResources GetSpecificResources(int viewID, string queryFilterString)
		{
			return WebResourcesDAL.GetSpecificResources(viewID, queryFilterString);
		}

		// Token: 0x06000218 RID: 536 RVA: 0x0000EED4 File Offset: 0x0000D0D4
		public void DeleteResource(int resourceId)
		{
			WebResourcesDAL.DeleteResource(resourceId);
		}

		// Token: 0x06000219 RID: 537 RVA: 0x0000EEDC File Offset: 0x0000D0DC
		public void DeleteResourceProperties(int resourceId)
		{
			WebResourcesDAL.DeleteResourceProperties(resourceId);
		}

		// Token: 0x0600021A RID: 538 RVA: 0x0000EEE4 File Offset: 0x0000D0E4
		public int InsertNewResource(WebResource resource, int viewID)
		{
			return WebResourcesDAL.InsertNewResource(resource, viewID);
		}

		// Token: 0x0600021B RID: 539 RVA: 0x0000EEED File Offset: 0x0000D0ED
		public void InsertNewResourceProperty(int resourceID, string propertyName, string propertyValue)
		{
			WebResourcesDAL.InsertNewResourceProperty(resourceID, propertyName, propertyValue);
		}

		// Token: 0x0600021C RID: 540 RVA: 0x0000EEF7 File Offset: 0x0000D0F7
		public string GetSpecificResourceProperty(int resourceID, string queryFilterString)
		{
			return WebResourcesDAL.GetSpecificResourceProperty(resourceID, queryFilterString);
		}

		// Token: 0x0600021D RID: 541 RVA: 0x0000EF00 File Offset: 0x0000D100
		public void UpdateResourceProperty(int resourceID, string propertyName, string propertyValue)
		{
			WebResourcesDAL.UpdateResourceProperty(resourceID, propertyName, propertyValue);
		}

		// Token: 0x0600021E RID: 542 RVA: 0x0000EF0A File Offset: 0x0000D10A
		public void UpdateWebsiteInfo(string serverName, string ipAddress, int port)
		{
			WebsitesDAL.UpdateWebsiteInfo(serverName, ipAddress, port);
		}

		// Token: 0x0600021F RID: 543 RVA: 0x0000EF14 File Offset: 0x0000D114
		public string GetSiteAddress()
		{
			return WebsitesDAL.GetSiteAddress();
		}

		// Token: 0x06000220 RID: 544 RVA: 0x0000EF1B File Offset: 0x0000D11B
		public bool IsHttpsUsed()
		{
			return WebsitesDAL.IsHttpsUsed();
		}

		// Token: 0x06000221 RID: 545 RVA: 0x0000EF24 File Offset: 0x0000D124
		private static NotificationItemType DalToWfc(NotificationItemTypeDAL dal)
		{
			if (dal == null)
			{
				return null;
			}
			return new NotificationItemType(dal.Id, dal.TypeName, dal.Module, dal.Caption, dal.DetailsUrl, dal.DetailsCaption, dal.Icon, dal.Description, dal.DisplayAs, dal.RequiredRoles.ToArray(), dal.CustomDismissButtonText, dal.HideDismissButton);
		}

		// Token: 0x06000222 RID: 546 RVA: 0x0000EF88 File Offset: 0x0000D188
		public NotificationItemType GetNotificationItemTypeById(Guid typeId)
		{
			CoreBusinessLayerService.log.Debug("Sending request for NotificationItemTypeDAL.GetTypeById.");
			NotificationItemType result;
			try
			{
				result = CoreBusinessLayerService.DalToWfc(NotificationItemTypeDAL.GetTypeById(typeId));
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error obtaining notification item type: " + ex.ToString());
				throw new Exception(string.Format("Error obtaining notification item type: ID={0}.", typeId));
			}
			return result;
		}

		// Token: 0x06000223 RID: 547 RVA: 0x0000EFF4 File Offset: 0x0000D1F4
		public List<NotificationItemType> GetNotificationItemTypes()
		{
			CoreBusinessLayerService.log.Debug("Sending request for NotificationItemTypeDAL.GetTypes.");
			List<NotificationItemType> result;
			try
			{
				List<NotificationItemType> list = new List<NotificationItemType>();
				foreach (NotificationItemTypeDAL dal in NotificationItemTypeDAL.GetTypes())
				{
					list.Add(CoreBusinessLayerService.DalToWfc(dal));
				}
				result = list;
			}
			catch (Exception ex)
			{
				CoreBusinessLayerService.log.Error("Error obtaining notification item types collection: " + ex.ToString());
				throw new Exception("Error obtaining notification item types collection.");
			}
			return result;
		}

		// Token: 0x06000224 RID: 548 RVA: 0x0000F094 File Offset: 0x0000D294
		public Dictionary<string, string> GetServicesDisplayNames(List<string> servicesNames)
		{
			return ServiceManager.Instance.GetServicesDisplayNames(servicesNames);
		}

		// Token: 0x06000225 RID: 549 RVA: 0x0000F0A1 File Offset: 0x0000D2A1
		public Dictionary<string, WindowsServiceRestartState> GetServicesStates(List<string> servicesNames)
		{
			return ServiceManager.Instance.GetServicesStates(servicesNames);
		}

		// Token: 0x06000226 RID: 550 RVA: 0x0000F0AE File Offset: 0x0000D2AE
		public void RestartServices(List<string> servicesNames)
		{
			ServiceManager.Instance.RestartServices(servicesNames);
		}

		// Token: 0x06000227 RID: 551 RVA: 0x0000F0BC File Offset: 0x0000D2BC
		public bool ValidateWMI(string ip, string userName, string password)
		{
			string text;
			return this.ValidateWMIWithErrorMessage(ip, userName, password, out text);
		}

		// Token: 0x06000228 RID: 552 RVA: 0x0000F0D4 File Offset: 0x0000D2D4
		public bool ValidateWMIWithErrorMessage(string ip, string userName, string password, out string localizedErrorMessage)
		{
			JobDescription jobDescription = WmiJob<WmiValidateCredentialJobResults>.CreateJobDescription<WmiValidateCredentialJob>(ip, SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-WMI Retries", 0), Convert.ToBoolean(SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-WMI Auto Correct Reverse DNS", 0)), SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-WMI Default Root Namespace Override Index", 0), SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-WMI Retry Interval", 0), BusinessLayerSettings.Instance.TestJobTimeout);
			WmiCredentials jobCredential = new WmiCredentials
			{
				Password = password,
				UserName = userName
			};
			localizedErrorMessage = string.Empty;
			string text;
			WmiValidateCredentialJobResults wmiValidateCredentialJobResults = this.ExecuteJobAndGetResult<WmiValidateCredentialJobResults>(jobDescription, jobCredential, JobResultDataFormatType.Xml, "WMI", out text);
			if (!wmiValidateCredentialJobResults.Success)
			{
				localizedErrorMessage = wmiValidateCredentialJobResults.Message;
				return false;
			}
			CoreBusinessLayerService.log.InfoFormat("WMI credential test finished. Success: {0}", wmiValidateCredentialJobResults.CredentialsValid);
			return wmiValidateCredentialJobResults.CredentialsValid;
		}

		// Token: 0x06000229 RID: 553 RVA: 0x0000F183 File Offset: 0x0000D383
		public int? InsertWmiCredential(UsernamePasswordCredential credential, string owner)
		{
			new CredentialManager().AddCredential<UsernamePasswordCredential>(owner, credential);
			return credential.ID;
		}

		// Token: 0x0600022A RID: 554 RVA: 0x0000F197 File Offset: 0x0000D397
		public UsernamePasswordCredential GetWmiCredential(int credentialID)
		{
			return new CredentialManager().GetCredential<UsernamePasswordCredential>(credentialID);
		}

		// Token: 0x0600022B RID: 555 RVA: 0x0000F1A4 File Offset: 0x0000D3A4
		public string GetWmiSysName(string ip, string userName, string password)
		{
			JobDescription jobDescription = WmiJob<GetSysNameJobResult>.CreateJobDescription<WmiGetSysNameJob>(ip, SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-WMI Retries", 0), Convert.ToBoolean(SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-WMI Auto Correct Reverse DNS", 0)), SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-WMI Default Root Namespace Override Index", 0), SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-WMI Retry Interval", 0), BusinessLayerSettings.Instance.TestJobTimeout);
			WmiCredentials jobCredential = new WmiCredentials
			{
				Password = password,
				UserName = userName
			};
			string text;
			GetSysNameJobResult getSysNameJobResult = this.ExecuteJobAndGetResult<GetSysNameJobResult>(jobDescription, jobCredential, JobResultDataFormatType.Xml, "WMI", out text);
			CoreBusinessLayerService.log.InfoFormat("Wmi GetSysName job finished. SysName: {0}", getSysNameJobResult.SysName);
			return getSysNameJobResult.SysName;
		}

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x0600022C RID: 556 RVA: 0x0000F233 File Offset: 0x0000D433
		private IServiceProvider ServiceContainer
		{
			get
			{
				return this.parent.ServiceContainer;
			}
		}

		// Token: 0x0600022D RID: 557 RVA: 0x0000F240 File Offset: 0x0000D440
		public CoreBusinessLayerService(CoreBusinessLayerPlugin pluginParent, IOneTimeJobManager oneTimeJobManager, int engineId) : this(pluginParent, PackageManager.InstanceWithCache, new NodeBLDAL(), new AgentInfoDAL(), new SettingsDAL(), oneTimeJobManager, new EngineDAL(), new EngineIdentityProvider(), engineId)
		{
		}

		// Token: 0x0600022E RID: 558 RVA: 0x0000F274 File Offset: 0x0000D474
		internal CoreBusinessLayerService(CoreBusinessLayerPlugin pluginParent, IPackageManager packageManager, INodeBLDAL nodeBlDal, IAgentInfoDAL agentInfoDal, ISettingsDAL settingsDal, IOneTimeJobManager oneTimeJobManager, IEngineDAL engineDal, IEngineIdentityProvider engineIdentityProvider, int engineId)
		{
			if (nodeBlDal == null)
			{
				throw new ArgumentNullException("nodeBlDal");
			}
			if (agentInfoDal == null)
			{
				throw new ArgumentNullException("agentInfoDal");
			}
			if (settingsDal == null)
			{
				throw new ArgumentNullException("settingsDal");
			}
			if (oneTimeJobManager == null)
			{
				throw new ArgumentNullException("oneTimeJobManager");
			}
			if (engineDal == null)
			{
				throw new ArgumentNullException("engineDal");
			}
			if (engineIdentityProvider == null)
			{
				throw new ArgumentNullException("engineIdentityProvider");
			}
			this.parent = pluginParent;
			this._nodeBlDal = nodeBlDal;
			this._agentInfoDal = agentInfoDal;
			this._settingsDal = settingsDal;
			this._auditPluginManager.Initialize();
			this._areInterfacesSupported = packageManager.IsPackageInstalled("Orion.Interfaces");
			this._oneTimeJobManager = oneTimeJobManager;
			this._engineDal = engineDal;
			this._engineIdentityProvider = engineIdentityProvider;
			this._serviceLogicalInstanceId = CoreBusinessLayerConfiguration.GetLogicalInstanceId(engineId);
			SnmpSettings.Instance.Changed += this.SnmpEncodingSettingsChanged;
		}

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x0600022F RID: 559 RVA: 0x0000F404 File Offset: 0x0000D604
		public EventWaitHandle ShutdownWaitHandle
		{
			get
			{
				return this.shutdownEvent;
			}
		}

		// Token: 0x06000230 RID: 560 RVA: 0x0000F40C File Offset: 0x0000D60C
		public void Shutdown()
		{
			IChannel channel = JobScheduler.GetInstance() as IChannel;
			if (channel != null)
			{
				MessageUtilities.ShutdownCommunicationObject(channel);
			}
		}

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000231 RID: 561 RVA: 0x0000F42D File Offset: 0x0000D62D
		internal bool AreInterfacesSupported
		{
			get
			{
				return this._areInterfacesSupported;
			}
		}

		// Token: 0x06000232 RID: 562 RVA: 0x00008415 File Offset: 0x00006615
		public void CheckBLConnection()
		{
		}

		// Token: 0x06000233 RID: 563 RVA: 0x00008415 File Offset: 0x00006615
		public void Dispose()
		{
		}

		// Token: 0x06000234 RID: 564 RVA: 0x0000F435 File Offset: 0x0000D635
		public IEnumerable<ServiceEndpointDescriptor> GetServiceEndpointDescriptors(ServiceDescription serviceDescription)
		{
			ConnectionDescriptorToServiceEndpointMapper endpointMapper = new ConnectionDescriptorToServiceEndpointMapper();
			yield return new ServiceEndpointDescriptor
			{
				ServiceEndpointProperties = new Dictionary<string, object>
				{
					{
						"isLocal",
						true
					},
					{
						"isStreamed",
						false
					}
				},
				ConnectionDescriptor = ConnectionDescriptorToServiceEndpointMapperExtensions.Map(endpointMapper, LegacyServicesSettings.Instance.NetPipeWcfEndpointEnabled ? "CoreBlNamedPipe" : "CoreBlNetTcp", serviceDescription)
			};
			yield return new ServiceEndpointDescriptor
			{
				ServiceEndpointProperties = new Dictionary<string, object>
				{
					{
						"isLocal",
						false
					},
					{
						"isStreamed",
						false
					}
				},
				ConnectionDescriptor = ConnectionDescriptorToServiceEndpointMapperExtensions.Map(endpointMapper, "CoreBlNetTcp", serviceDescription)
			};
			yield return new ServiceEndpointDescriptor
			{
				ServiceEndpointProperties = new Dictionary<string, object>
				{
					{
						"isLocal",
						false
					},
					{
						"isStreamed",
						true
					}
				},
				ConnectionDescriptor = ConnectionDescriptorToServiceEndpointMapperExtensions.Map(endpointMapper, "CoreBlNetTcpStreamed", serviceDescription)
			};
			yield break;
		}

		// Token: 0x06000235 RID: 565 RVA: 0x0000F448 File Offset: 0x0000D648
		private CoreBusinessLayerServiceInstance GetCurrentServiceInstance()
		{
			int currentOperationEngineId = this.GetCurrentOperationEngineId();
			return this.parent.GetServiceInstance(currentOperationEngineId);
		}

		// Token: 0x06000236 RID: 566 RVA: 0x0000F468 File Offset: 0x0000D668
		private int GetCurrentOperationEngineId()
		{
			IEngineIdentity engineIdentity;
			if (!this._engineIdentityProvider.TryGetCurrent(out engineIdentity))
			{
				throw new InvalidOperationException("Failed to retrieve current EngineId from the operation context.");
			}
			return engineIdentity.EngineId;
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000237 RID: 567 RVA: 0x0000F495 File Offset: 0x0000D695
		public string ServiceLogicalInstanceId
		{
			get
			{
				CoreBusinessLayerService.log.Info("Registering to service directory, ServiceId: Core.BusinessLayer, ServiceLogicalInstanceId: " + this._serviceLogicalInstanceId);
				return this._serviceLogicalInstanceId;
			}
		}

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x06000238 RID: 568 RVA: 0x0000F4B7 File Offset: 0x0000D6B7
		public Version ServiceInstanceVersion
		{
			get
			{
				return CoreBusinessLayerService.CoreBusinessLayerServiceVersion;
			}
		}

		// Token: 0x0400003E RID: 62
		private readonly Guid ItemTypeCertificateUpdateRequired = new Guid("{4E9EB71A-3A11-468E-A672-1E3E440E4F89}");

		// Token: 0x0400003F RID: 63
		private CertificateMaintenance certificateMaintenance = CertificateMaintenance.GetForFullMaintenance(BusinessLayerSettings.Instance.SafeCertificateMaintenanceTrialPeriod, BusinessLayerSettings.Instance.CertificateMaintenanceAgentPollFrequency);

		// Token: 0x04000040 RID: 64
		private Lazy<IActionRunner> actionRunner = new Lazy<IActionRunner>(new Func<IActionRunner>(CoreBusinessLayerService.CreateActionRunner));

		// Token: 0x04000041 RID: 65
		private readonly ActionMethodInvoker _actionMethodInvoker = new ActionMethodInvoker();

		// Token: 0x04000042 RID: 66
		private AuditingDAL auditingDal = new AuditingDAL();

		// Token: 0x04000043 RID: 67
		private IJobFactory _jobFactory;

		// Token: 0x04000044 RID: 68
		private IPersistentDiscoveryCache _persistentDiscoveryCache;

		// Token: 0x04000045 RID: 69
		private DiscoveryLogic discoveryLogic = new DiscoveryLogic();

		// Token: 0x04000046 RID: 70
		private static readonly Dictionary<string, Action<PollingEngineStatus, object>> statusParsers = new Dictionary<string, Action<PollingEngineStatus, object>>
		{
			{
				"NetPerfMon Engine:Network Node Elements",
				delegate(PollingEngineStatus s, object o)
				{
					s.NetworkNodeElements = Convert.ToInt32(o);
				}
			},
			{
				"NetPerfMon Engine:Interface Elements",
				delegate(PollingEngineStatus s, object o)
				{
					s.InterfaceElements = Convert.ToInt32(o);
				}
			},
			{
				"NetPerfMon Engine:Volume Elements",
				delegate(PollingEngineStatus s, object o)
				{
					s.VolumeElements = Convert.ToInt32(o);
				}
			},
			{
				"NetPerfMon Engine:Date Time",
				delegate(PollingEngineStatus s, object o)
				{
					s.DateTime = Convert.ToDateTime(o);
				}
			},
			{
				"NetPerfMon Engine:Paused",
				delegate(PollingEngineStatus s, object o)
				{
					s.Paused = Convert.ToBoolean(o);
				}
			},
			{
				"Max Outstanding Polls",
				delegate(PollingEngineStatus s, object o)
				{
					s.MaxOutstandingPolls = Convert.ToInt32(o);
				}
			},
			{
				"Status Pollers:ICMP Status Polling Index",
				delegate(PollingEngineStatus s, object o)
				{
					s.ICMPStatusPollingIndex = o.ToString();
				}
			},
			{
				"Status Pollers:SNMP Status Polling Index",
				delegate(PollingEngineStatus s, object o)
				{
					s.SNMPStatusPollingIndex = o.ToString();
				}
			},
			{
				"Status Pollers:ICMP Polls per second",
				delegate(PollingEngineStatus s, object o)
				{
					s.ICMPStatusPollsPerSecond = Convert.ToDouble(o);
				}
			},
			{
				"Status Pollers:SNMP Polls per second",
				delegate(PollingEngineStatus s, object o)
				{
					s.SNMPStatusPollsPerSecond = Convert.ToDouble(o);
				}
			},
			{
				"Packet Queues:DNS Outstanding",
				delegate(PollingEngineStatus s, object o)
				{
					s.DNSOutstanding = Convert.ToInt32(o);
				}
			},
			{
				"Packet Queues:ICMP Outstanding",
				delegate(PollingEngineStatus s, object o)
				{
					s.ICMPOutstanding = Convert.ToInt32(o);
				}
			},
			{
				"Packet Queues:SNMP Outstanding",
				delegate(PollingEngineStatus s, object o)
				{
					s.SNMPOutstanding = Convert.ToInt32(o);
				}
			},
			{
				"Statistics Pollers:ICMP Statistic Polling Index",
				delegate(PollingEngineStatus s, object o)
				{
					s.ICMPStatisticPollingIndex = o.ToString();
				}
			},
			{
				"Statistics Pollers:SNMP Statistic Polling Index",
				delegate(PollingEngineStatus s, object o)
				{
					s.SNMPStatisticPollingIndex = o.ToString();
				}
			},
			{
				"Statistics Pollers:ICMP Polls per second",
				delegate(PollingEngineStatus s, object o)
				{
					s.ICMPStatisticPollsPerSecond = Convert.ToDouble(o);
				}
			},
			{
				"Statistics Pollers:SNMP Polls per second",
				delegate(PollingEngineStatus s, object o)
				{
					s.SNMPStatisticPollsPerSecond = Convert.ToDouble(o);
				}
			},
			{
				"Status Pollers:Max Status Polls Per Second",
				delegate(PollingEngineStatus s, object o)
				{
					s.MaxStatusPollsPerSecond = Convert.ToInt32(o);
				}
			},
			{
				"Statistics Pollers:Max Statistic Polls Per Second",
				delegate(PollingEngineStatus s, object o)
				{
					s.MaxStatisticPollsPerSecond = Convert.ToInt32(o);
				}
			}
		};

		// Token: 0x04000047 RID: 71
		private readonly object _syncRoot = new object();

		// Token: 0x04000048 RID: 72
		private readonly Dictionary<string, string> _elementCountQueries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		// Token: 0x04000049 RID: 73
		private readonly MibDAL mibDAL = new MibDAL();

		// Token: 0x0400004A RID: 74
		private static readonly List<NodeSubType> WmiCompatibleNodeSubTypes = new List<NodeSubType>
		{
			NodeSubType.Agent,
			NodeSubType.WMI
		};

		// Token: 0x0400004B RID: 75
		private NodeAssignmentHelper nodeHelper = new NodeAssignmentHelper();

		// Token: 0x0400004C RID: 76
		private const string SourceInstanceUriProperty = "SourceInstanceUri";

		// Token: 0x0400004D RID: 77
		private const string UriProperty = "Uri";

		// Token: 0x0400004E RID: 78
		private ExpirableCache<string, IEnumerable<MaintenanceStatus>> _maintenanceInfoCache;

		// Token: 0x0400004F RID: 79
		private ExpirableCache<string, IEnumerable<SupportCase>> _supportCasesCache;

		// Token: 0x04000050 RID: 80
		private ExpirableCache<string, LicenseAndManagementInfo> _LAMInfoCache;

		// Token: 0x04000051 RID: 81
		public static readonly Version CoreBusinessLayerServiceVersion = typeof(CoreBusinessLayerService).Assembly.GetName().Version;

		// Token: 0x04000052 RID: 82
		private static readonly Log log = new Log();

		// Token: 0x04000053 RID: 83
		private readonly ManualResetEvent shutdownEvent = new ManualResetEvent(false);

		// Token: 0x04000054 RID: 84
		private readonly CoreBusinessLayerPlugin parent;

		// Token: 0x04000055 RID: 85
		private readonly bool _areInterfacesSupported;

		// Token: 0x04000056 RID: 86
		private readonly AuditingPluginManager _auditPluginManager = new AuditingPluginManager();

		// Token: 0x04000057 RID: 87
		private readonly IAgentInfoDAL _agentInfoDal;

		// Token: 0x04000058 RID: 88
		private readonly INodeBLDAL _nodeBlDal;

		// Token: 0x04000059 RID: 89
		private readonly ISettingsDAL _settingsDal;

		// Token: 0x0400005A RID: 90
		private readonly IOneTimeJobManager _oneTimeJobManager;

		// Token: 0x0400005B RID: 91
		private readonly IEngineDAL _engineDal;

		// Token: 0x0400005C RID: 92
		private readonly IEngineIdentityProvider _engineIdentityProvider;

		// Token: 0x0400005D RID: 93
		private readonly string _serviceLogicalInstanceId;

		// Token: 0x020000DD RID: 221
		public class DiscoveryBusinessLayerError : Exception
		{
			// Token: 0x060009D5 RID: 2517 RVA: 0x00046C13 File Offset: 0x00044E13
			internal DiscoveryBusinessLayerError(string format, object[] args, Exception inner) : base(string.Format(CultureInfo.CurrentUICulture, format ?? string.Empty, args ?? new object[0]), inner)
			{
			}

			// Token: 0x060009D6 RID: 2518 RVA: 0x00046C3B File Offset: 0x00044E3B
			internal DiscoveryBusinessLayerError(string format, params object[] args) : base(string.Format(CultureInfo.CurrentUICulture, format ?? string.Empty, args ?? new object[0]))
			{
			}

			// Token: 0x060009D7 RID: 2519 RVA: 0x00046C62 File Offset: 0x00044E62
			internal DiscoveryBusinessLayerError(string message, Exception inner) : base(message, inner)
			{
			}

			// Token: 0x060009D8 RID: 2520 RVA: 0x00046C6C File Offset: 0x00044E6C
			internal DiscoveryBusinessLayerError(string message) : base(message)
			{
			}
		}

		// Token: 0x020000DE RID: 222
		public class DicoveryDeletingJobError : CoreBusinessLayerService.DiscoveryBusinessLayerError
		{
			// Token: 0x060009D9 RID: 2521 RVA: 0x00046C75 File Offset: 0x00044E75
			internal DicoveryDeletingJobError(string format, params object[] args) : base(format, args)
			{
			}
		}

		// Token: 0x020000DF RID: 223
		[Obsolete("Core-Split cleanup. If you need this member please contact Core team", true)]
		public class DicoveryStateError : CoreBusinessLayerService.DiscoveryBusinessLayerError
		{
			// Token: 0x060009DA RID: 2522 RVA: 0x00046C75 File Offset: 0x00044E75
			internal DicoveryStateError(string format, params object[] args) : base(format, args)
			{
			}
		}

		// Token: 0x020000E0 RID: 224
		public class DiscoveryInsertingIgnoredNodeError : CoreBusinessLayerService.DiscoveryBusinessLayerError
		{
			// Token: 0x060009DB RID: 2523 RVA: 0x00046C7F File Offset: 0x00044E7F
			internal DiscoveryInsertingIgnoredNodeError(string format, object[] args, Exception inner) : base(format, args, inner)
			{
			}
		}

		// Token: 0x020000E1 RID: 225
		public class DiscoveryDeletingIgnoredNodeError : CoreBusinessLayerService.DiscoveryBusinessLayerError
		{
			// Token: 0x060009DC RID: 2524 RVA: 0x00046C7F File Offset: 0x00044E7F
			internal DiscoveryDeletingIgnoredNodeError(string format, object[] args, Exception inner) : base(format, args, inner)
			{
			}
		}

		// Token: 0x020000E2 RID: 226
		public class DiscoveryInsertingIgnoredInterfaceError : CoreBusinessLayerService.DiscoveryBusinessLayerError
		{
			// Token: 0x060009DD RID: 2525 RVA: 0x00046C7F File Offset: 0x00044E7F
			internal DiscoveryInsertingIgnoredInterfaceError(string format, object[] args, Exception inner) : base(format, args, inner)
			{
			}
		}

		// Token: 0x020000E3 RID: 227
		public class DiscoveryDeletingIgnoredInterfaceError : CoreBusinessLayerService.DiscoveryBusinessLayerError
		{
			// Token: 0x060009DE RID: 2526 RVA: 0x00046C7F File Offset: 0x00044E7F
			internal DiscoveryDeletingIgnoredInterfaceError(string format, object[] args, Exception inner) : base(format, args, inner)
			{
			}
		}

		// Token: 0x020000E4 RID: 228
		public class DiscoveryInsertingIgnoredVolumeError : CoreBusinessLayerService.DiscoveryBusinessLayerError
		{
			// Token: 0x060009DF RID: 2527 RVA: 0x00046C7F File Offset: 0x00044E7F
			internal DiscoveryInsertingIgnoredVolumeError(string format, object[] args, Exception inner) : base(format, args, inner)
			{
			}
		}

		// Token: 0x020000E5 RID: 229
		public class DiscoveryDeletingIgnoredVolumeError : CoreBusinessLayerService.DiscoveryBusinessLayerError
		{
			// Token: 0x060009E0 RID: 2528 RVA: 0x00046C7F File Offset: 0x00044E7F
			internal DiscoveryDeletingIgnoredVolumeError(string format, object[] args, Exception inner) : base(format, args, inner)
			{
			}
		}

		// Token: 0x020000E6 RID: 230
		public class DiscoveryHostAddressMissingError : CoreBusinessLayerService.DiscoveryBusinessLayerError
		{
			// Token: 0x060009E1 RID: 2529 RVA: 0x00046C75 File Offset: 0x00044E75
			internal DiscoveryHostAddressMissingError(string format, params object[] args) : base(format, args)
			{
			}
		}

		// Token: 0x020000E7 RID: 231
		public class DiscoveryJobCancellationError : CoreBusinessLayerService.DiscoveryBusinessLayerError
		{
			// Token: 0x060009E2 RID: 2530 RVA: 0x00046C75 File Offset: 0x00044E75
			internal DiscoveryJobCancellationError(string format, params object[] args) : base(format, args)
			{
			}
		}
	}
}
