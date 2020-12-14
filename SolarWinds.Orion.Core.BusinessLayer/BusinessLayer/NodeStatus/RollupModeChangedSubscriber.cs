using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.PubSub;
using SolarWinds.Shared;

namespace SolarWinds.Orion.Core.BusinessLayer.NodeStatus
{
	// Token: 0x02000071 RID: 113
	public class RollupModeChangedSubscriber : ISubscriber, IDisposable
	{
		// Token: 0x060005E0 RID: 1504 RVA: 0x00023334 File Offset: 0x00021534
		public RollupModeChangedSubscriber(ISubscriptionManager subscriptionManager, ISqlHelper sqlHelper)
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

		// Token: 0x060005E1 RID: 1505 RVA: 0x00023368 File Offset: 0x00021568
		public async Task OnNotificationAsync(Notification notification)
		{
			if (this.subscription != null && !(((notification != null) ? notification.SubscriptionId.UniqueName : null) != RollupModeChangedSubscriber.SubscriptionUniqueName))
			{
				if (notification.SourceInstanceProperties == null)
				{
					RollupModeChangedSubscriber.log.Error("Argument SourceInstanceProperties is null.");
				}
				else if (!notification.SourceInstanceProperties.ContainsKey("Core.StatusRollupMode"))
				{
					RollupModeChangedSubscriber.log.Error("Core.StatusRollupMode not supplied in SourceInstanceProperties.");
				}
				else
				{
					try
					{
						string text = (string)notification.SourceInstanceProperties["Core.StatusRollupMode"];
						EvaluationMethod evaluationMethod = (text != null) ? Convert.ToInt32(text) : 0;
						int nodeId = Convert.ToInt32(notification.SourceInstanceProperties["NodeId"]);
						RollupModeChangedSubscriber.log.DebugFormat("Node with id '{0}' rollup mode changed to '{1}', re-calculating node status ..", nodeId, evaluationMethod);
						await Task.Run(delegate()
						{
							this.RecalculateNodeStatus(nodeId);
						});
					}
					catch (Exception ex)
					{
						RollupModeChangedSubscriber.log.Error("Indication handling failed", ex);
					}
				}
			}
		}

		// Token: 0x060005E2 RID: 1506 RVA: 0x000233B8 File Offset: 0x000215B8
		public RollupModeChangedSubscriber Start()
		{
			RollupModeChangedSubscriber.log.Debug("Subscribing RollupMode changed indications..");
			try
			{
				if (this.subscription != null)
				{
					RollupModeChangedSubscriber.log.Debug("Already subscribed, unsubscribing first..");
					this.Unsubscribe(this.subscription.Id);
				}
				SubscriptionId subscriptionId;
				subscriptionId..ctor("Core", RollupModeChangedSubscriber.SubscriptionUniqueName, 0);
				this.subscription = this.subscriptionManager.Subscribe(subscriptionId, this, new SubscriberConfiguration
				{
					SubscriptionQuery = RollupModeChangedSubscriber.SubscriptionQuery,
					ReliableDelivery = true,
					AcknowledgeMode = 0,
					MessageTimeToLive = TimeSpan.Zero
				});
			}
			catch (Exception ex)
			{
				RollupModeChangedSubscriber.log.Error("Failed to subscribe.", ex);
				throw;
			}
			return this;
		}

		// Token: 0x060005E3 RID: 1507 RVA: 0x00023474 File Offset: 0x00021674
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x060005E4 RID: 1508 RVA: 0x00023484 File Offset: 0x00021684
		protected void Dispose(bool disposing)
		{
			if (this.subscription != null)
			{
				try
				{
					RollupModeChangedSubscriber.log.Debug("Unsubscribing RollupMode changed indications..");
					this.Unsubscribe(this.subscription.Id);
					this.subscription = null;
				}
				catch (Exception ex)
				{
					RollupModeChangedSubscriber.log.Error("Error unsubscribing subscription.", ex);
				}
			}
		}

		// Token: 0x060005E5 RID: 1509 RVA: 0x000234E8 File Offset: 0x000216E8
		private void Unsubscribe(SubscriptionId subscriptionId)
		{
			this.subscriptionManager.Unsubscribe(subscriptionId);
		}

		// Token: 0x060005E6 RID: 1510 RVA: 0x000234F8 File Offset: 0x000216F8
		private void RecalculateNodeStatus(int nodeId)
		{
			using (SqlCommand textCommand = this.sqlHelper.GetTextCommand("EXEC dbo.[swsp_ReflowNodeChildStatus] @nodeId"))
			{
				textCommand.Parameters.Add(new SqlParameter("@nodeId", nodeId));
				this.sqlHelper.ExecuteNonQuery(textCommand);
			}
		}

		// Token: 0x040001C7 RID: 455
		public static string SubscriptionUniqueName = "RollupModeChanged";

		// Token: 0x040001C8 RID: 456
		private static string SubscriptionQuery = "SUBSCRIBE CHANGES TO Orion.Nodes WHEN [Core.StatusRollupMode] CHANGES";

		// Token: 0x040001C9 RID: 457
		private static readonly Log log = new Log();

		// Token: 0x040001CA RID: 458
		private readonly ISubscriptionManager subscriptionManager;

		// Token: 0x040001CB RID: 459
		private readonly ISqlHelper sqlHelper;

		// Token: 0x040001CC RID: 460
		private ISubscription subscription;
	}
}
