using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using SolarWinds.Common.Utility;
using SolarWinds.InformationService.Contract2;
using SolarWinds.InformationService.Contract2.PubSub;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Indications;
using SolarWinds.Orion.Core.Common.InformationService;
using SolarWinds.Orion.Core.Common.Swis;
using SolarWinds.Orion.Swis.Contract.InformationService;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200002C RID: 44
	public class OrionCoreNotificationSubscriber : INotificationSubscriber
	{
		// Token: 0x0600036B RID: 875 RVA: 0x00015635 File Offset: 0x00013835
		public OrionCoreNotificationSubscriber(ISqlHelper sqlHelper)
		{
			if (sqlHelper == null)
			{
				throw new ArgumentNullException("sqlHelper");
			}
			this._sqlHelper = sqlHelper;
		}

		// Token: 0x0600036C RID: 876 RVA: 0x00015654 File Offset: 0x00013854
		public void OnIndication(string subscriptionId, string indicationType, PropertyBag indicationProperties, PropertyBag sourceInstanceProperties)
		{
			if (OrionCoreNotificationSubscriber.log.IsDebugEnabled)
			{
				OrionCoreNotificationSubscriber.log.DebugFormat("Indication of type \"{0}\" arrived.", indicationType);
			}
			try
			{
				object obj;
				if (indicationType == IndicationHelper.GetIndicationType(1) && sourceInstanceProperties.TryGetValue("InstanceType", out obj) && string.Equals(obj as string, "Orion.Nodes", StringComparison.OrdinalIgnoreCase))
				{
					if (sourceInstanceProperties.ContainsKey("NodeID"))
					{
						int nodeId = Convert.ToInt32(sourceInstanceProperties["NodeID"]);
						this.InsertIntoDeletedTable(nodeId);
					}
					else
					{
						OrionCoreNotificationSubscriber.log.WarnFormat("Indication is type of {0} but does not contain NodeID", indicationType);
					}
				}
			}
			catch (Exception ex)
			{
				OrionCoreNotificationSubscriber.log.Error(string.Format("Exception occured when processing incomming indication of type \"{0}\"", indicationType), ex);
			}
		}

		// Token: 0x0600036D RID: 877 RVA: 0x00015714 File Offset: 0x00013914
		public void Start()
		{
			Scheduler.Instance.Add(new ScheduledTask("OrionCoreIndications", new TimerCallback(this.Subscribe), null, TimeSpan.FromSeconds(1.0), TimeSpan.FromMinutes(1.0)));
		}

		// Token: 0x0600036E RID: 878 RVA: 0x00015753 File Offset: 0x00013953
		public void Stop()
		{
			Scheduler.Instance.Remove("OrionCoreIndications");
		}

		// Token: 0x0600036F RID: 879 RVA: 0x00015764 File Offset: 0x00013964
		private void Subscribe(object state)
		{
			OrionCoreNotificationSubscriber.log.Debug("Subscribing indications..");
			try
			{
				OrionCoreNotificationSubscriber.DeleteOldSubscriptions();
			}
			catch (Exception ex)
			{
				OrionCoreNotificationSubscriber.log.Warn("Exception deleting old subscriptions:", ex);
			}
			try
			{
				string text = InformationServiceSubscriptionProviderShared.Instance().Subscribe("SUBSCRIBE System.InstanceDeleted", this, new SubscriptionOptions
				{
					Description = "OrionCoreIndications"
				});
				if (OrionCoreNotificationSubscriber.log.IsDebugEnabled)
				{
					OrionCoreNotificationSubscriber.log.DebugFormat("PubSub Subscription succeeded. uri:'{0}'", text);
				}
				Scheduler.Instance.Remove("OrionCoreIndications");
			}
			catch (Exception ex2)
			{
				OrionCoreNotificationSubscriber.log.Error("Subscription did not succeed, retrying .. (Is SWIS v3 running ?)", ex2);
			}
		}

		// Token: 0x06000370 RID: 880 RVA: 0x00015818 File Offset: 0x00013A18
		private void InsertIntoDeletedTable(int nodeId)
		{
			using (SqlCommand textCommand = this._sqlHelper.GetTextCommand("IF NOT EXISTS (SELECT NodeId FROM [dbo].[DeletedNodes] WHERE NodeId=@NodeId)  BEGIN   INSERT INTO [dbo].[DeletedNodes](NodeId)    VALUES(@NodeId)  END "))
			{
				textCommand.Parameters.AddWithValue("@NodeId", nodeId);
				this._sqlHelper.ExecuteNonQuery(textCommand);
			}
		}

		// Token: 0x06000371 RID: 881 RVA: 0x00015878 File Offset: 0x00013A78
		private static void DeleteOldSubscriptions()
		{
			using (IInformationServiceProxy2 informationServiceProxy = SwisConnectionProxyPool.GetSystemCreator().Create())
			{
				string text = "SELECT Uri FROM System.Subscription WHERE description = @description";
				foreach (DataRow dataRow in informationServiceProxy.Query(text, new Dictionary<string, object>
				{
					{
						"description",
						"OrionCoreIndications"
					}
				}).Rows.Cast<DataRow>())
				{
					informationServiceProxy.Delete(dataRow[0].ToString());
				}
			}
		}

		// Token: 0x040000B2 RID: 178
		public const string OrionCoreIndications = "OrionCoreIndications";

		// Token: 0x040000B3 RID: 179
		public const string NodeIndications = "NodeIndications";

		// Token: 0x040000B4 RID: 180
		private static Log log = new Log(typeof(OrionCoreNotificationSubscriber));

		// Token: 0x040000B5 RID: 181
		private ISqlHelper _sqlHelper;
	}
}
