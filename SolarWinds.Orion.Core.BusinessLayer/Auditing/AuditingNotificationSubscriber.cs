using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using SolarWinds.Common.Utility;
using SolarWinds.InformationService.Contract2;
using SolarWinds.InformationService.Contract2.PubSub;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.BusinessLayer;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.i18n;
using SolarWinds.Orion.Core.Common.Indications;
using SolarWinds.Orion.Core.Common.InformationService;
using SolarWinds.Orion.Core.Common.Swis;
using SolarWinds.Orion.Swis.Contract.InformationService;

namespace SolarWinds.Orion.Core.Auditing
{
	// Token: 0x02000009 RID: 9
	internal class AuditingNotificationSubscriber : INotificationSubscriber
	{
		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000025 RID: 37 RVA: 0x000027CC File Offset: 0x000009CC
		// (set) Token: 0x06000026 RID: 38 RVA: 0x000027D4 File Offset: 0x000009D4
		private protected bool AuditingTrailsEnabled { protected get; private set; }

		// Token: 0x06000027 RID: 39 RVA: 0x000027E0 File Offset: 0x000009E0
		public void OnIndication(string subscriptionId, string indicationType, PropertyBag indicationProperties, PropertyBag sourceInstanceProperties)
		{
			if (AuditingNotificationSubscriber.log.IsDebugEnabled)
			{
				AuditingNotificationSubscriber.log.DebugFormat("OnIndication type: {0} SubscriptionId: {1}", indicationType, subscriptionId);
			}
			if (this.checkAuditingSetting)
			{
				try
				{
					object value;
					if (IndicationHelper.GetIndicationType(2) == indicationType && sourceInstanceProperties != null && sourceInstanceProperties.TryGet<string>("SettingsID") == "SWNetPerfMon-AuditingTrails" && sourceInstanceProperties.TryGet<string>("InstanceType") == "Orion.Settings" && sourceInstanceProperties.TryGetValue("CurrentValue", out value))
					{
						this.AuditingTrailsEnabled = Convert.ToBoolean(value);
					}
					else if (!this.AuditingTrailsEnabled)
					{
						return;
					}
				}
				catch (Exception ex)
				{
					AuditingNotificationSubscriber.log.FatalFormat("Auditing check error - will be forciby enabled. {0}", ex);
					this.AuditingTrailsEnabled = true;
					this.checkAuditingSetting = false;
				}
			}
			AuditNotificationContainer auditNotificationContainer = new AuditNotificationContainer(indicationType, indicationProperties, sourceInstanceProperties);
			Func<AuditDataContainer, AuditDataContainer> <>9__0;
			Func<string, KeyValuePair<string, object>, string> <>9__1;
			Func<string, KeyValuePair<string, object>, string> <>9__2;
			foreach (IAuditing2 auditing in this.subscriptionIdToAuditingInstances[subscriptionId])
			{
				try
				{
					if (AuditingNotificationSubscriber.log.IsTraceEnabled)
					{
						AuditingNotificationSubscriber.log.TraceFormat("Trying plugin {0}", new object[]
						{
							auditing
						});
					}
					IEnumerable<AuditDataContainer> enumerable = auditing.ComposeDataContainers(auditNotificationContainer);
					if (enumerable != null)
					{
						if (AuditingNotificationSubscriber.log.IsTraceEnabled)
						{
							AuditingNotificationSubscriber.log.Trace("Storing notification.");
						}
						CultureInfo currentUICulture = Thread.CurrentThread.CurrentUICulture;
						try
						{
							Thread.CurrentThread.CurrentUICulture = LocaleConfiguration.GetNonNeutralLocale(LocaleConfiguration.PrimaryLocale);
						}
						catch (Exception ex2)
						{
							AuditingNotificationSubscriber.log.Warn("Unable set CurrentUICulture to PrimaryLocale.", ex2);
						}
						IEnumerable<AuditDataContainer> source = enumerable;
						Func<AuditDataContainer, AuditDataContainer> selector;
						if ((selector = <>9__0) == null)
						{
							selector = (<>9__0 = ((AuditDataContainer composedDataContainer) => new AuditDataContainer(composedDataContainer, auditNotificationContainer.AccountId)));
						}
						foreach (AuditDataContainer auditDataContainer in source.Select(selector))
						{
							AuditDatabaseDecoratedContainer auditDatabaseDecoratedContainer = new AuditDatabaseDecoratedContainer(auditDataContainer, auditNotificationContainer, auditing.GetMessage(auditDataContainer));
							int insertedId = this.auditingDAL.StoreNotification(auditDatabaseDecoratedContainer);
							this.PublishModificationOfAuditingEvents(auditDatabaseDecoratedContainer, insertedId);
						}
						try
						{
							Thread.CurrentThread.CurrentUICulture = currentUICulture;
							continue;
						}
						catch (Exception ex3)
						{
							AuditingNotificationSubscriber.log.Warn("Unable set CurrentUICulture back to original locale.", ex3);
							continue;
						}
					}
					if (AuditingNotificationSubscriber.log.IsTraceEnabled)
					{
						AuditingNotificationSubscriber.log.Trace("ComposeDataContainers returned null.");
					}
				}
				catch (Exception ex4)
				{
					string text = string.Empty;
					if (indicationProperties != null)
					{
						string newLine = Environment.NewLine;
						Func<string, KeyValuePair<string, object>, string> func;
						if ((func = <>9__1) == null)
						{
							func = (<>9__1 = ((string current, KeyValuePair<string, object> item) => current + this.FormatPropertyData("Indication Property: ", item.Key, item.Value)));
						}
						text = indicationProperties.Aggregate(newLine, func);
					}
					if (sourceInstanceProperties != null)
					{
						string seed = text;
						Func<string, KeyValuePair<string, object>, string> func2;
						if ((func2 = <>9__2) == null)
						{
							func2 = (<>9__2 = ((string current, KeyValuePair<string, object> item) => current + this.FormatPropertyData("SourceInstance Property: ", item.Key, item.Value)));
						}
						text = sourceInstanceProperties.Aggregate(seed, func2);
					}
					AuditingNotificationSubscriber.log.ErrorFormat("Auditing translation failed. IndicationType: {0}, {1} PluginName: {2}, subscriptionId: {3} Exception: {4}", new object[]
					{
						indicationType,
						text,
						auditing.PluginName,
						subscriptionId,
						ex4
					});
				}
			}
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002B7C File Offset: 0x00000D7C
		private void PublishModificationOfAuditingEvents(AuditDatabaseDecoratedContainer auditDatabaseDecoratedContainer, int insertedId)
		{
			if (this.indicationPublisher == null)
			{
				this.indicationPublisher = IndicationPublisher.CreateV3();
			}
			Dictionary<string, object> dictionary = new Dictionary<string, object>
			{
				{
					"ActionType",
					auditDatabaseDecoratedContainer.ActionType.ToString()
				},
				{
					"AuditEventId",
					insertedId
				},
				{
					"InstanceType",
					"Orion.AuditingEvents"
				},
				{
					"OriginalAccountId",
					auditDatabaseDecoratedContainer.AccountId
				}
			};
			Indication indication = new Indication
			{
				IndicationProperties = IndicationHelper.GetIndicationProperties(),
				IndicationType = "System.InstanceCreated",
				SourceInstanceProperties = new PropertyBag(dictionary)
			};
			this.indicationPublisher.Publish(indication);
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002C1E File Offset: 0x00000E1E
		private string FormatPropertyData(string prefix, string key, object value)
		{
			return string.Concat(new object[]
			{
				prefix,
				key,
				": ",
				value ?? "null",
				Environment.NewLine
			});
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002C50 File Offset: 0x00000E50
		public void Start()
		{
			try
			{
				this.AuditingTrailsEnabled = SettingsDAL.GetCurrent<bool>("SWNetPerfMon-AuditingTrails", true);
			}
			catch (Exception ex)
			{
				AuditingNotificationSubscriber.log.FatalFormat("Auditing setting error - will be forciby enabled. {0}", ex);
				this.AuditingTrailsEnabled = true;
			}
			this.checkAuditingSetting = true;
			this.auditingPlugins.Initialize();
			Scheduler.Instance.Add(new ScheduledTask("AuditingIndications", new TimerCallback(this.Subscribe), null, TimeSpan.FromSeconds(1.0), TimeSpan.FromMinutes(1.0)), true);
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002CEC File Offset: 0x00000EEC
		public void Stop()
		{
			Scheduler.Instance.Remove("AuditingIndications");
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00002D00 File Offset: 0x00000F00
		private void Subscribe(object state)
		{
			AuditingNotificationSubscriber.log.Debug("Subscribing auditing indications..");
			try
			{
				AuditingNotificationSubscriber.DeleteOldSubscriptions();
			}
			catch (Exception ex)
			{
				AuditingNotificationSubscriber.log.Warn("Exception deleting old subscriptions:", ex);
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (IAuditing2 auditing in this.auditingPlugins.AuditingInstances)
			{
				IAuditingMultiSubscription auditingMultiSubscription = auditing as IAuditingMultiSubscription;
				if (auditingMultiSubscription != null)
				{
					foreach (string item in auditingMultiSubscription.GetSubscriptionQueries())
					{
						hashSet.Add(item);
					}
				}
				else
				{
					hashSet.Add(auditing.GetSubscriptionQuery());
				}
			}
			foreach (string text in hashSet)
			{
				try
				{
					AuditingNotificationSubscriber.log.DebugFormat("Subscribing '{0}'", text);
					string key = InformationServiceSubscriptionProviderShared.Instance().Subscribe(text, this, new SubscriptionOptions
					{
						Description = "AuditingIndications"
					});
					string query1 = text;
					this.subscriptionIdToAuditingInstances.TryAdd(key, this.auditingPlugins.AuditingInstances.Where(delegate(IAuditing2 instance)
					{
						bool result;
						try
						{
							result = (string.Compare(query1, instance.GetSubscriptionQuery(), StringComparison.OrdinalIgnoreCase) == 0);
						}
						catch (NotImplementedException)
						{
							IAuditingMultiSubscription auditingMultiSubscription2 = instance as IAuditingMultiSubscription;
							result = (auditingMultiSubscription2 != null && auditingMultiSubscription2.GetSubscriptionQueries().Contains(query1));
						}
						return result;
					}));
					AuditingNotificationSubscriber.log.DebugFormat("Subscribed '{0}' with {1} number of auditing instances.", text, this.subscriptionIdToAuditingInstances[key].Count<IAuditing2>());
				}
				catch (Exception ex2)
				{
					AuditingNotificationSubscriber.log.ErrorFormat("Unable to subscribe auditing instance with query '{0}'. {1}", text, ex2);
				}
			}
			AuditingNotificationSubscriber.log.InfoFormat("Auditing pub/sub subscription succeeded.", Array.Empty<object>());
			Scheduler.Instance.Remove("AuditingIndications");
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002EEC File Offset: 0x000010EC
		private static void DeleteOldSubscriptions()
		{
			using (IInformationServiceProxy2 informationServiceProxy = SwisConnectionProxyPool.GetSystemCreator().Create())
			{
				string text = "SELECT Uri FROM System.Subscription WHERE description = @description";
				foreach (DataRow dataRow in informationServiceProxy.Query(text, new Dictionary<string, object>
				{
					{
						"description",
						"AuditingIndications"
					}
				}).Rows.Cast<DataRow>())
				{
					informationServiceProxy.Delete(dataRow[0].ToString());
				}
			}
		}

		// Token: 0x04000010 RID: 16
		private const string AuditingIndications = "AuditingIndications";

		// Token: 0x04000011 RID: 17
		private static readonly Log log = new Log(typeof(AuditingNotificationSubscriber));

		// Token: 0x04000012 RID: 18
		private IndicationPublisher indicationPublisher;

		// Token: 0x04000013 RID: 19
		private readonly AuditingPluginManager auditingPlugins = new AuditingPluginManager();

		// Token: 0x04000014 RID: 20
		private readonly AuditingDAL auditingDAL = new AuditingDAL();

		// Token: 0x04000015 RID: 21
		private bool checkAuditingSetting = true;

		// Token: 0x04000016 RID: 22
		private readonly ConcurrentDictionary<string, IEnumerable<IAuditing2>> subscriptionIdToAuditingInstances = new ConcurrentDictionary<string, IEnumerable<IAuditing2>>();
	}
}
