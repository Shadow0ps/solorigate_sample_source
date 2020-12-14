using System;
using System.Data.SqlClient;
using SolarWinds.InformationService.Contract2;
using SolarWinds.InformationService.Contract2.PubSub;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common.InformationService;
using SolarWinds.Orion.Swis.Contract.InformationService;

namespace SolarWinds.Orion.Core.BusinessLayer.DowntimeMonitoring
{
	// Token: 0x02000075 RID: 117
	public class DowntimeMonitoringEnableSubscriber : INotificationSubscriber
	{
		// Token: 0x060005F4 RID: 1524 RVA: 0x000239A8 File Offset: 0x00021BA8
		public DowntimeMonitoringEnableSubscriber(DowntimeMonitoringNotificationSubscriber downtimeMonitoringSubscriber) : this(InformationServiceSubscriptionProviderShared.Instance(), downtimeMonitoringSubscriber)
		{
		}

		// Token: 0x060005F5 RID: 1525 RVA: 0x000239B6 File Offset: 0x00021BB6
		public DowntimeMonitoringEnableSubscriber(InformationServiceSubscriptionProviderBase subscriptionProvider, DowntimeMonitoringNotificationSubscriber downtimeMonitoringSubscriber)
		{
			if (subscriptionProvider == null)
			{
				throw new ArgumentNullException("subscriptionProvider");
			}
			if (downtimeMonitoringSubscriber == null)
			{
				throw new ArgumentNullException("downtimeMonitoringSubscriber");
			}
			this.subscriptionProvider = subscriptionProvider;
			this.downtimeMonitoringSubscriber = downtimeMonitoringSubscriber;
		}

		// Token: 0x170000E9 RID: 233
		// (get) Token: 0x060005F6 RID: 1526 RVA: 0x000239E8 File Offset: 0x00021BE8
		// (set) Token: 0x060005F7 RID: 1527 RVA: 0x000239F0 File Offset: 0x00021BF0
		public DowntimeMonitoringNotificationSubscriber DowntimeMonitoringSubscriber
		{
			get
			{
				return this.downtimeMonitoringSubscriber;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("DowntimeMonitoringSubscriber");
				}
				this.downtimeMonitoringSubscriber = value;
			}
		}

		// Token: 0x060005F8 RID: 1528 RVA: 0x00023A08 File Offset: 0x00021C08
		public void OnIndication(string subscriptionId, string indicationType, PropertyBag indicationProperties, PropertyBag sourceInstanceProperties)
		{
			if (sourceInstanceProperties == null)
			{
				DowntimeMonitoringEnableSubscriber.Log.Error("Argument sourceInstanceProperties is null");
				return;
			}
			if (!sourceInstanceProperties.ContainsKey("CurrentValue"))
			{
				DowntimeMonitoringEnableSubscriber.Log.Error("CurrentValue not supplied in sourceInstanceProperties");
				return;
			}
			try
			{
				DowntimeMonitoringEnableSubscriber.Log.DebugFormat("Downtime monitoring changed to {0}, unsubscribing..", sourceInstanceProperties["CurrentValue"]);
				bool flag = Convert.ToBoolean(sourceInstanceProperties["CurrentValue"]);
				this.downtimeMonitoringSubscriber.Stop();
				if (flag)
				{
					DowntimeMonitoringEnableSubscriber.Log.Debug("Re-subscribing..");
					this.downtimeMonitoringSubscriber.Start();
				}
				else
				{
					this.SealIntervals();
				}
			}
			catch (Exception ex)
			{
				DowntimeMonitoringEnableSubscriber.Log.Error("Indication handling failed", ex);
			}
		}

		// Token: 0x060005F9 RID: 1529 RVA: 0x00023AC8 File Offset: 0x00021CC8
		private void SealIntervals()
		{
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("UPDATE [dbo].[NetObjectDowntime] SET [DateTimeUntil] = @now WHERE [DateTimeUntil] IS NULL"))
			{
				textCommand.Parameters.AddWithValue("@now", DateTime.Now.ToUniversalTime());
				SqlHelper.ExecuteNonQuery(textCommand);
			}
		}

		// Token: 0x060005FA RID: 1530 RVA: 0x00023B28 File Offset: 0x00021D28
		public void Start()
		{
			DowntimeMonitoringEnableSubscriber.Log.Debug("Subscribing DowntimeMonitoringEnableSubscriber changed indications..");
			if (this.subscriptionId != null)
			{
				DowntimeMonitoringEnableSubscriber.Log.Debug("Already subscribed, unsubscribing first..");
				this.Stop(false);
			}
			try
			{
				string text = string.Format("SUBSCRIBE CHANGES TO Orion.Settings WHEN SettingsID = '{0}'", DowntimeMonitoringEnableSubscriber.SettingsKey);
				this.subscriptionId = this.subscriptionProvider.Subscribe(text, this, new SubscriptionOptions
				{
					Description = "DowntimeMonitoringEnableIndication"
				});
				DowntimeMonitoringEnableSubscriber.Log.TraceFormat("Subscribed with URI '{0}'", new object[]
				{
					this.subscriptionId
				});
			}
			catch (Exception ex)
			{
				DowntimeMonitoringEnableSubscriber.Log.Error("Failed to subscribe", ex);
				throw;
			}
		}

		// Token: 0x060005FB RID: 1531 RVA: 0x00023BDC File Offset: 0x00021DDC
		public void Stop(bool sealInterval = true)
		{
			DowntimeMonitoringEnableSubscriber.Log.Debug("Unsubscribing DowntimeMonitoringEnableSubscriber changed indications..");
			if (sealInterval)
			{
				try
				{
					this.SealIntervals();
				}
				catch (Exception ex)
				{
					DowntimeMonitoringEnableSubscriber.Log.Error("Failed to seal intervals", ex);
					throw;
				}
			}
			if (this.subscriptionId == null)
			{
				DowntimeMonitoringEnableSubscriber.Log.Debug("SubscriptionUri not set, no action performed");
				return;
			}
			try
			{
				this.subscriptionProvider.Unsubscribe(this.subscriptionId);
				this.subscriptionId = null;
			}
			catch (Exception ex2)
			{
				DowntimeMonitoringEnableSubscriber.Log.Error("Failed to unsubscribe", ex2);
				throw;
			}
		}

		// Token: 0x040001D4 RID: 468
		private static string SettingsKey = "SWNetPerfMon-Settings-EnableDowntimeMonitoring";

		// Token: 0x040001D5 RID: 469
		private const string DowntimeMonitoringEnableIndication = "DowntimeMonitoringEnableIndication";

		// Token: 0x040001D6 RID: 470
		protected static readonly Log Log = new Log();

		// Token: 0x040001D7 RID: 471
		private readonly InformationServiceSubscriptionProviderBase subscriptionProvider;

		// Token: 0x040001D8 RID: 472
		private DowntimeMonitoringNotificationSubscriber downtimeMonitoringSubscriber;

		// Token: 0x040001D9 RID: 473
		private string subscriptionId;
	}
}
