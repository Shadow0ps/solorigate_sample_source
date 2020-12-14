using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.PubSub;

namespace SolarWinds.Orion.Core.BusinessLayer.NodeStatus
{
	// Token: 0x02000070 RID: 112
	public class NodeChildStatusParticipationSubscriber : ISubscriber, IDisposable
	{
		// Token: 0x060005D4 RID: 1492 RVA: 0x00022EA0 File Offset: 0x000210A0
		public NodeChildStatusParticipationSubscriber(ISubscriptionManager subscriptionManager, ISqlHelper sqlHelper) : this(subscriptionManager, sqlHelper, 5000)
		{
		}

		// Token: 0x060005D5 RID: 1493 RVA: 0x00022EB0 File Offset: 0x000210B0
		public NodeChildStatusParticipationSubscriber(ISubscriptionManager subscriptionManager, ISqlHelper sqlHelper, int delay)
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
			if (delay < 0)
			{
				throw new ArgumentOutOfRangeException("delay", "cannot be negative");
			}
			this.delay = delay;
		}

		// Token: 0x060005D6 RID: 1494 RVA: 0x00022F0C File Offset: 0x0002110C
		public Task OnNotificationAsync(Notification notification)
		{
			Task<bool> result = Task.FromResult<bool>(false);
			if (this.subscription == null || ((notification != null) ? notification.SubscriptionId.UniqueName : null) != NodeChildStatusParticipationSubscriber.SubscriptionUniqueName)
			{
				return result;
			}
			if (notification.SourceInstanceProperties == null)
			{
				NodeChildStatusParticipationSubscriber.log.Error("Argument SourceInstanceProperties is null.");
				return result;
			}
			if (!notification.SourceInstanceProperties.ContainsKey("EntityType") || !notification.SourceInstanceProperties.ContainsKey("Enabled"))
			{
				NodeChildStatusParticipationSubscriber.log.Error("The EntityType or Enabled not supplied in SourceInstanceProperties.");
				return result;
			}
			try
			{
				string text = Convert.ToString(notification.SourceInstanceProperties["EntityType"]);
				string text2 = Convert.ToBoolean(notification.SourceInstanceProperties["Enabled"]) ? "enabled" : "disabled";
				NodeChildStatusParticipationSubscriber.log.DebugFormat(string.Concat(new string[]
				{
					"Node child status participation for '",
					text,
					"' is ",
					text2,
					", re-calculating node status .."
				}), Array.Empty<object>());
				Timer timer = this.reflowScheduler;
				if (timer != null)
				{
					timer.Change(-1, -1);
				}
				this.reflowScheduler = this.SetupReflowScheduler();
				return Task.FromResult<bool>(this.reflowScheduler.Change(this.delay, -1));
			}
			catch (Exception ex)
			{
				NodeChildStatusParticipationSubscriber.log.Error("Indication handling failed", ex);
			}
			return result;
		}

		// Token: 0x060005D7 RID: 1495 RVA: 0x00023074 File Offset: 0x00021274
		public NodeChildStatusParticipationSubscriber Start()
		{
			NodeChildStatusParticipationSubscriber.log.Debug("Subscribing NodeChildStatusParticipation changed indications..");
			try
			{
				if (this.subscription != null)
				{
					NodeChildStatusParticipationSubscriber.log.Debug("Already subscribed, unsubscribing first..");
					this.Unsubscribe(this.subscription.Id);
				}
				SubscriptionId subscriptionId;
				subscriptionId..ctor("Core", NodeChildStatusParticipationSubscriber.SubscriptionUniqueName, 0);
				this.subscription = this.subscriptionManager.Subscribe(subscriptionId, this, new SubscriberConfiguration
				{
					SubscriptionQuery = NodeChildStatusParticipationSubscriber.SubscriptionQuery,
					ReliableDelivery = true,
					AcknowledgeMode = 0,
					MessageTimeToLive = TimeSpan.Zero
				});
			}
			catch (Exception ex)
			{
				NodeChildStatusParticipationSubscriber.log.Error("Failed to subscribe.", ex);
				throw;
			}
			return this;
		}

		// Token: 0x060005D8 RID: 1496 RVA: 0x00023130 File Offset: 0x00021330
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x060005D9 RID: 1497 RVA: 0x00023140 File Offset: 0x00021340
		protected void Dispose(bool disposing)
		{
			if (this.subscription != null)
			{
				try
				{
					NodeChildStatusParticipationSubscriber.log.Debug("Unsubscribing NodeChildStatusParticipation changed indications..");
					this.Unsubscribe(this.subscription.Id);
					this.subscription = null;
				}
				catch (Exception ex)
				{
					NodeChildStatusParticipationSubscriber.log.Error("Error unsubscribing subscription.", ex);
				}
			}
			if (this.reflowScheduler != null && disposing)
			{
				this.reflowScheduler.Dispose();
			}
		}

		// Token: 0x060005DA RID: 1498 RVA: 0x000231BC File Offset: 0x000213BC
		private void Unsubscribe(SubscriptionId subscriptionId)
		{
			this.subscriptionManager.Unsubscribe(subscriptionId);
		}

		// Token: 0x060005DB RID: 1499 RVA: 0x000231CA File Offset: 0x000213CA
		private Timer SetupReflowScheduler()
		{
			return new Timer(new TimerCallback(this.OnReflowAllNodeChildStatus), null, -1, -1);
		}

		// Token: 0x1400000A RID: 10
		// (add) Token: 0x060005DC RID: 1500 RVA: 0x000231E0 File Offset: 0x000213E0
		// (remove) Token: 0x060005DD RID: 1501 RVA: 0x00023218 File Offset: 0x00021418
		public event EventHandler ReflowAllNodeChildStatus;

		// Token: 0x060005DE RID: 1502 RVA: 0x00023250 File Offset: 0x00021450
		private void OnReflowAllNodeChildStatus(object state)
		{
			try
			{
				using (NodeChildStatusParticipationSubscriber.log.Block("NodeChildStatusParticipation.OnReflowAllNodeChildStatus"))
				{
					NodeChildStatusParticipationSubscriber.log.Debug("swsp_ReflowAllNodeChildStatus");
					string sqlText = "EXEC dbo.[swsp_ReflowAllNodeChildStatus]";
					using (SqlCommand textCommand = this.sqlHelper.GetTextCommand(sqlText))
					{
						this.sqlHelper.ExecuteNonQuery(textCommand);
					}
					NodeChildStatusParticipationSubscriber.log.Debug("Invoke Notification Event");
					EventHandler reflowAllNodeChildStatus = this.ReflowAllNodeChildStatus;
					if (reflowAllNodeChildStatus != null)
					{
						reflowAllNodeChildStatus(this, EventArgs.Empty);
					}
				}
			}
			catch (Exception ex)
			{
				NodeChildStatusParticipationSubscriber.log.Error(ex);
			}
		}

		// Token: 0x040001BE RID: 446
		public static string SubscriptionUniqueName = "NodeChildStatusParticipationChanged";

		// Token: 0x040001BF RID: 447
		private static string SubscriptionQuery = "SUBSCRIBE CHANGES TO Orion.NodeChildStatusParticipation WHEN [Enabled] CHANGES";

		// Token: 0x040001C0 RID: 448
		private readonly int delay;

		// Token: 0x040001C1 RID: 449
		private static readonly Log log = new Log();

		// Token: 0x040001C2 RID: 450
		private readonly ISubscriptionManager subscriptionManager;

		// Token: 0x040001C3 RID: 451
		private readonly ISqlHelper sqlHelper;

		// Token: 0x040001C4 RID: 452
		private ISubscription subscription;

		// Token: 0x040001C5 RID: 453
		private Timer reflowScheduler;
	}
}
