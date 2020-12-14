using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.PubSub;

namespace SolarWinds.Orion.Core.BusinessLayer.NodeStatus
{
	// Token: 0x0200006E RID: 110
	public class EnhancedNodeStatusCalculationSubscriber : ISubscriber, IDisposable
	{
		// Token: 0x060005C5 RID: 1477 RVA: 0x00022969 File Offset: 0x00020B69
		public EnhancedNodeStatusCalculationSubscriber(ISubscriptionManager subscriptionManager, ISqlHelper sqlHelper)
		{
			if (subscriptionManager == null)
			{
				throw new ArgumentNullException("subscriptionManager");
			}
			this.subscriptionManager = subscriptionManager;
			if (sqlHelper == null)
			{
				throw new ArgumentNullException("sqlHelper");
			}
			this.sqlHelper = sqlHelper;
		}

		// Token: 0x060005C6 RID: 1478 RVA: 0x000229A0 File Offset: 0x00020BA0
		public async Task OnNotificationAsync(Notification notification)
		{
			if (this.subscription != null && !(((notification != null) ? notification.SubscriptionId.UniqueName : null) != EnhancedNodeStatusCalculationSubscriber.SubscriptionUniqueName))
			{
				if (notification.SourceInstanceProperties == null)
				{
					EnhancedNodeStatusCalculationSubscriber.log.Error("Argument SourceInstanceProperties is null.");
				}
				else if (!notification.SourceInstanceProperties.ContainsKey("CurrentValue"))
				{
					EnhancedNodeStatusCalculationSubscriber.log.Error("CurrentValue not supplied in SourceInstanceProperties.");
				}
				else
				{
					try
					{
						bool flag = Convert.ToInt32(notification.SourceInstanceProperties["CurrentValue"]) == 1;
						EnhancedNodeStatusCalculationSubscriber.log.DebugFormat("Node status calculation changed to '{0} calculation', re-calculating node status ..", flag ? "Enhanced" : "Classic");
						await Task.Run(delegate()
						{
							this.RecalculateNodeStatus();
						});
					}
					catch (Exception ex)
					{
						EnhancedNodeStatusCalculationSubscriber.log.Error("Indication handling failed", ex);
					}
				}
			}
		}

		// Token: 0x060005C7 RID: 1479 RVA: 0x000229F0 File Offset: 0x00020BF0
		public EnhancedNodeStatusCalculationSubscriber Start()
		{
			EnhancedNodeStatusCalculationSubscriber.log.Debug("Subscribing EnhancedNodeStatusCalculation changed indications..");
			try
			{
				if (this.subscription != null)
				{
					EnhancedNodeStatusCalculationSubscriber.log.Debug("Already subscribed, unsubscribing first..");
					this.Unsubscribe(this.subscription.Id);
				}
				SubscriptionId subscriptionId;
				subscriptionId..ctor("Core", EnhancedNodeStatusCalculationSubscriber.SubscriptionUniqueName, 0);
				this.subscription = this.subscriptionManager.Subscribe(subscriptionId, this, new SubscriberConfiguration
				{
					SubscriptionQuery = EnhancedNodeStatusCalculationSubscriber.SubscriptionQuery,
					ReliableDelivery = true,
					AcknowledgeMode = 0,
					MessageTimeToLive = TimeSpan.Zero
				});
			}
			catch (Exception ex)
			{
				EnhancedNodeStatusCalculationSubscriber.log.Error("Failed to subscribe.", ex);
				throw;
			}
			return this;
		}

		// Token: 0x060005C8 RID: 1480 RVA: 0x00022AAC File Offset: 0x00020CAC
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x060005C9 RID: 1481 RVA: 0x00022ABC File Offset: 0x00020CBC
		protected void Dispose(bool disposing)
		{
			if (this.subscription != null)
			{
				try
				{
					EnhancedNodeStatusCalculationSubscriber.log.Debug("Unsubscribing EnhancedNodeStatusCalculation changed indications..");
					this.Unsubscribe(this.subscription.Id);
					this.subscription = null;
				}
				catch (Exception ex)
				{
					EnhancedNodeStatusCalculationSubscriber.log.Error("Error unsubscribing subscription.", ex);
				}
			}
		}

		// Token: 0x060005CA RID: 1482 RVA: 0x00022B20 File Offset: 0x00020D20
		private void Unsubscribe(SubscriptionId subscriptionId)
		{
			this.subscriptionManager.Unsubscribe(subscriptionId);
		}

		// Token: 0x060005CB RID: 1483 RVA: 0x00022B30 File Offset: 0x00020D30
		private void RecalculateNodeStatus()
		{
			string sqlText = "EXEC dbo.[swsp_ReflowAllNodeChildStatus]";
			using (SqlCommand textCommand = this.sqlHelper.GetTextCommand(sqlText))
			{
				this.sqlHelper.ExecuteNonQuery(textCommand);
			}
		}

		// Token: 0x040001B3 RID: 435
		public static string SubscriptionUniqueName = "EnhancedNodeStatusCalculation";

		// Token: 0x040001B4 RID: 436
		private static string SubscriptionQuery = "SUBSCRIBE CHANGES TO Orion.Settings WHEN SettingsID = 'EnhancedNodeStatusCalculation'";

		// Token: 0x040001B5 RID: 437
		private static readonly Log log = new Log();

		// Token: 0x040001B6 RID: 438
		private readonly ISubscriptionManager subscriptionManager;

		// Token: 0x040001B7 RID: 439
		private readonly ISqlHelper sqlHelper;

		// Token: 0x040001B8 RID: 440
		private ISubscription subscription;
	}
}
