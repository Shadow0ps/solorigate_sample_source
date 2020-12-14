using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using SolarWinds.Common.Utility;
using SolarWinds.InformationService.Contract2;
using SolarWinds.InformationService.Contract2.PubSub;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Common.Indications;
using SolarWinds.Orion.Core.Common.InformationService;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Common.Swis;
using SolarWinds.Orion.Swis.Contract.InformationService;

namespace SolarWinds.Orion.Core.BusinessLayer.DowntimeMonitoring
{
	// Token: 0x02000076 RID: 118
	public class DowntimeMonitoringNotificationSubscriber : INotificationSubscriber
	{
		// Token: 0x170000EA RID: 234
		// (set) Token: 0x060005FD RID: 1533 RVA: 0x00023C8E File Offset: 0x00021E8E
		internal Lazy<ILookup<string, NetObjectTypeEx>> NetObjectTypes
		{
			set
			{
				this._netObjectTypes = value;
			}
		}

		// Token: 0x170000EB RID: 235
		// (get) Token: 0x060005FE RID: 1534 RVA: 0x00023C97 File Offset: 0x00021E97
		private static bool IsEnabled
		{
			get
			{
				return SettingsDAL.GetCurrent<bool>("SWNetPerfMon-Settings-EnableDowntimeMonitoring", true);
			}
		}

		// Token: 0x060005FF RID: 1535 RVA: 0x00023CA4 File Offset: 0x00021EA4
		public DowntimeMonitoringNotificationSubscriber(INetObjectDowntimeDAL netObjectDowntimeDal) : this(netObjectDowntimeDal, SwisConnectionProxyPool.GetSystemCreator(), new SwisUriParser())
		{
		}

		// Token: 0x06000600 RID: 1536 RVA: 0x00023CB8 File Offset: 0x00021EB8
		internal DowntimeMonitoringNotificationSubscriber(INetObjectDowntimeDAL netObjectDowntimeDal, IInformationServiceProxyCreator serviceProxyCreator, ISwisUriParser swisUriParser)
		{
			if (netObjectDowntimeDal == null)
			{
				throw new ArgumentNullException("netObjectDowntimeDal");
			}
			this._netObjectDowntimeDal = netObjectDowntimeDal;
			if (serviceProxyCreator == null)
			{
				throw new ArgumentNullException("serviceProxyCreator");
			}
			this._swisServiceProxyCreator = serviceProxyCreator;
			if (swisUriParser == null)
			{
				throw new ArgumentNullException("swisUriParser");
			}
			this._swisUriParser = swisUriParser;
			this._nodeNetObjectIdColumn = null;
			this._netObjectTypes = new Lazy<ILookup<string, NetObjectTypeEx>>(new Func<ILookup<string, NetObjectTypeEx>>(this.LoadNetObjectTypesExtSwisInfo), LazyThreadSafetyMode.PublicationOnly);
		}

		// Token: 0x06000601 RID: 1537 RVA: 0x00023D38 File Offset: 0x00021F38
		public void OnIndication(string subscriptionId, string indicationType, PropertyBag indicationProperties, PropertyBag sourceInstanceProperties)
		{
			Stopwatch stopwatch = new Stopwatch();
			try
			{
				stopwatch.Start();
				if (sourceInstanceProperties == null)
				{
					throw new ArgumentNullException("sourceInstanceProperties");
				}
				if (DowntimeMonitoringNotificationSubscriber.log.IsDebugEnabled)
				{
					DowntimeMonitoringNotificationSubscriber.log.Debug(this.DetailInfo(subscriptionId, indicationType, indicationProperties, sourceInstanceProperties));
				}
				string text = sourceInstanceProperties.TryGet<string>("InstanceType") ?? sourceInstanceProperties.TryGet<string>("SourceInstanceType");
				if (text == null)
				{
					DowntimeMonitoringNotificationSubscriber.log.Error("Wrong PropertyBag data. InstanceType or SourceInstanceType are null");
				}
				else
				{
					string netObjectIdColumnForSwisEntity = this.GetNetObjectIdColumnForSwisEntity(text);
					object obj;
					if (netObjectIdColumnForSwisEntity == null)
					{
						DowntimeMonitoringNotificationSubscriber.log.DebugFormat("Not a supported instance type: {0}", text);
					}
					else if (!sourceInstanceProperties.TryGetValue(netObjectIdColumnForSwisEntity, out obj))
					{
						DowntimeMonitoringNotificationSubscriber.log.DebugFormat("Unable to get Entity ID. InstanceType : {0}, ID Field: {1}", text, netObjectIdColumnForSwisEntity);
					}
					else if (indicationType == IndicationHelper.GetIndicationType(2) || indicationType == IndicationHelper.GetIndicationType(0))
					{
						object obj2;
						sourceInstanceProperties.TryGetValue("Status", out obj2);
						if (obj2 == null)
						{
							DowntimeMonitoringNotificationSubscriber.log.DebugFormat("No Status reported for InstanceType : {0}", text);
						}
						else
						{
							if (this._nodeNetObjectIdColumn == null)
							{
								this._nodeNetObjectIdColumn = this.GetNetObjectIdColumnForSwisEntity("Orion.Nodes");
							}
							object obj3;
							sourceInstanceProperties.TryGetValue(this._nodeNetObjectIdColumn, out obj3);
							if (obj3 == null)
							{
								DowntimeMonitoringNotificationSubscriber.log.DebugFormat("SourceBag must include NodeId. InstanceType : {0}", text);
							}
							else
							{
								this._netObjectDowntimeDal.Insert(new NetObjectDowntime
								{
									EntityID = obj.ToString(),
									NodeID = this.ExtractStatusID(obj3),
									EntityType = text,
									DateTimeFrom = (DateTime)indicationProperties[IndicationConstants.IndicationTime],
									StatusID = this.ExtractStatusID(obj2)
								});
							}
						}
					}
					else if (indicationType == IndicationHelper.GetIndicationType(1))
					{
						this._netObjectDowntimeDal.DeleteDowntimeObjects(obj.ToString(), text);
					}
				}
			}
			catch (Exception ex)
			{
				DowntimeMonitoringNotificationSubscriber.log.Error(string.Format("Exception occured when processing incoming indication of type \"{0}\"", indicationType), ex);
			}
			finally
			{
				stopwatch.Stop();
				DowntimeMonitoringNotificationSubscriber.log.DebugFormat("Downtime notification has been processed in {0} miliseconds.", stopwatch.ElapsedMilliseconds);
			}
		}

		// Token: 0x06000602 RID: 1538 RVA: 0x00023F6C File Offset: 0x0002216C
		internal int ExtractStatusID(object statusObject)
		{
			return Convert.ToInt32(statusObject);
		}

		// Token: 0x06000603 RID: 1539 RVA: 0x00023F74 File Offset: 0x00022174
		public virtual void Start()
		{
			if (!DowntimeMonitoringNotificationSubscriber.IsEnabled)
			{
				DowntimeMonitoringNotificationSubscriber.log.Info("Subscription of Downtime Monitoring cancelled (disabled by Settings option)");
				this.Stop();
				return;
			}
			Scheduler.Instance.Add(new ScheduledTask("NetObjectDowntimeInitializator", new TimerCallback(this.Initialize), null, TimeSpan.FromSeconds(1.0), TimeSpan.FromMinutes(1.0)));
			Scheduler.Instance.Add(new ScheduledTask("NetObjectDowntimeIndication", new TimerCallback(this.Subscribe), null, TimeSpan.FromSeconds(1.0), TimeSpan.FromMinutes(1.0)));
		}

		// Token: 0x06000604 RID: 1540 RVA: 0x00024018 File Offset: 0x00022218
		private void Initialize(object state)
		{
			List<NetObjectDowntime> list = new List<NetObjectDowntime>();
			try
			{
				using (IInformationServiceProxy2 informationServiceProxy = this._swisServiceProxyCreator.Create())
				{
					DateTime utcNow = DateTime.UtcNow;
					foreach (object obj in informationServiceProxy.Query("SELECT Uri, Status, InstanceType, AncestorDetailsUrls\r\n                                            FROM System.ManagedEntity\r\n                                            WHERE UnManaged = false").Rows)
					{
						DataRow dataRow = (DataRow)obj;
						try
						{
							if (this.IsValid(dataRow))
							{
								list.Add(new NetObjectDowntime
								{
									DateTimeFrom = utcNow,
									EntityID = this._swisUriParser.GetEntityId(dataRow["Uri"].ToString()),
									NodeID = this.ExtractStatusID(this.GetNodeIDFromUrl((string[])dataRow["AncestorDetailsUrls"])),
									EntityType = dataRow["InstanceType"].ToString(),
									StatusID = (int)dataRow["Status"]
								});
							}
						}
						catch (Exception arg)
						{
							DowntimeMonitoringNotificationSubscriber.log.Error(string.Format("Unable to create NetObjectDowntime instance from ManagedEntity with Uri '{0}', {1}", dataRow["Uri"], arg));
						}
					}
				}
			}
			catch (Exception ex)
			{
				DowntimeMonitoringNotificationSubscriber.log.ErrorFormat("Exception while initializing NetObjectDowntime table with ManagedEntities. {0}", ex);
			}
			this._netObjectDowntimeDal.Insert(list);
			Scheduler.Instance.Remove("NetObjectDowntimeInitializator");
		}

		// Token: 0x06000605 RID: 1541 RVA: 0x000241E4 File Offset: 0x000223E4
		private bool IsValid(DataRow row)
		{
			bool result;
			try
			{
				this.GetNodeIDFromUrl((string[])row["AncestorDetailsUrls"]);
				result = !row.IsNull("Uri");
			}
			catch (Exception)
			{
				result = false;
			}
			return result;
		}

		// Token: 0x06000606 RID: 1542 RVA: 0x00024230 File Offset: 0x00022430
		public virtual void Stop()
		{
			this.Unsubscribe();
			Scheduler.Instance.Remove("NetObjectDowntimeIndication");
		}

		// Token: 0x06000607 RID: 1543 RVA: 0x00024248 File Offset: 0x00022448
		private void Unsubscribe()
		{
			if (this.subscriptionUris.Count == 0)
			{
				return;
			}
			try
			{
				foreach (string text in this.subscriptionUris)
				{
					InformationServiceSubscriptionProviderShared.Instance().Unsubscribe(text);
				}
				this.subscriptionUris.Clear();
			}
			catch (Exception ex)
			{
				DowntimeMonitoringNotificationSubscriber.log.ErrorFormat("Unsubscribe failed: '{0}'", ex);
				throw;
			}
		}

		// Token: 0x06000608 RID: 1544 RVA: 0x000242DC File Offset: 0x000224DC
		private void Subscribe(object state)
		{
			DowntimeMonitoringNotificationSubscriber.log.Debug("Subscribing Managed Entity changed indications..");
			try
			{
				try
				{
					this.DeleteOldSubscriptions();
				}
				catch (Exception ex)
				{
					DowntimeMonitoringNotificationSubscriber.log.Warn("Exception deleting old subscriptions:", ex);
				}
				if (this.subscriptionUris.Count > 0)
				{
					this.Unsubscribe();
				}
				foreach (string text in new string[]
				{
					"SUBSCRIBE System.InstanceDeleted WHEN InstanceType ISA 'System.ManagedEntity' OR SourceInstanceType ISA 'System.ManagedEntity'",
					"SUBSCRIBE System.InstanceCreated WHEN InstanceType ISA 'System.ManagedEntity' OR SourceInstanceType ISA 'System.ManagedEntity'",
					"SUBSCRIBE CHANGES TO System.ManagedEntity WHEN Status CHANGES"
				})
				{
					string text2 = InformationServiceSubscriptionProviderShared.Instance().Subscribe(text, this, new SubscriptionOptions
					{
						Description = "NetObjectDowntimeIndication"
					});
					this.subscriptionUris.Add(text2);
					DowntimeMonitoringNotificationSubscriber.log.InfoFormat("Pub/sub subscription succeeded. uri:'{0}'", text2);
				}
				Scheduler.Instance.Remove("NetObjectDowntimeIndication");
			}
			catch (Exception ex2)
			{
				DowntimeMonitoringNotificationSubscriber.log.ErrorFormat("{0} in Subscribe: {1}\r\n{2}", ex2.GetType(), ex2.Message, ex2.StackTrace);
			}
		}

		// Token: 0x06000609 RID: 1545 RVA: 0x000243EC File Offset: 0x000225EC
		private string DetailInfo(string subscriptionId, string indicationType, PropertyBag indicationProperties, PropertyBag sourceInstanceProperties)
		{
			string format = "Pub/Sub Notification: ID: {0}, Type: {1}, IndicationProperties: {2}, InstanceProperties: {3}";
			object[] array = new object[4];
			array[0] = subscriptionId;
			array[1] = indicationType;
			array[2] = string.Join(", ", from kvp in indicationProperties
			select string.Format("{0} = {1}", kvp.Key, kvp.Value));
			int num = 3;
			object obj;
			if (sourceInstanceProperties.Count <= 0)
			{
				obj = string.Empty;
			}
			else
			{
				obj = string.Join(", ", from kvp in sourceInstanceProperties
				select string.Format("{0} = {1}", kvp.Key, kvp.Value));
			}
			array[num] = obj;
			return string.Format(format, array);
		}

		// Token: 0x0600060A RID: 1546 RVA: 0x00024488 File Offset: 0x00022688
		internal string GetNodeIDFromUrl(string[] urls)
		{
			foreach (string input in urls)
			{
				Match match = DowntimeMonitoringNotificationSubscriber.NodeIdRegex.Match(input);
				if (match.Success)
				{
					return NetObjectHelper.GetObjectID(match.Value);
				}
			}
			throw new ArgumentException(string.Format("Cannot parse NodeId from AncestorUrl. Urls: {0}.", string.Join(",", urls)), "urls");
		}

		// Token: 0x0600060B RID: 1547 RVA: 0x000244E8 File Offset: 0x000226E8
		internal string GetNetObjectIdColumnForSwisEntity(string instanceType)
		{
			string result = null;
			if (this._netObjectTypes == null || this._netObjectTypes.Value == null)
			{
				return null;
			}
			NetObjectTypeEx netObjectTypeEx = this._netObjectTypes.Value[instanceType].FirstOrDefault((NetObjectTypeEx n) => n.KeyPropertyIndex == 0);
			if (netObjectTypeEx != null)
			{
				result = netObjectTypeEx.KeyProperty;
			}
			return result;
		}

		// Token: 0x0600060C RID: 1548 RVA: 0x00024550 File Offset: 0x00022750
		private ILookup<string, NetObjectTypeEx> LoadNetObjectTypesExtSwisInfo()
		{
			ILookup<string, NetObjectTypeEx> result;
			using (IInformationServiceProxy2 informationServiceProxy = this._swisServiceProxyCreator.Create())
			{
				result = (from DataRow row in informationServiceProxy.Query("SELECT EntityType, Name, Prefix, KeyProperty, NameProperty, KeyPropertyIndex, CanConvert FROM Orion.NetObjectTypesExt").Rows
				select new NetObjectTypeEx(row.Field("EntityType"), row.Field("Name"), row.Field("Prefix"), row.Field("KeyProperty"), row.Field("NameProperty"), (int)row.Field("CanConvert"), row.Field("KeyPropertyIndex"))).ToLookup((NetObjectTypeEx k) => k.EntityType);
			}
			return result;
		}

		// Token: 0x0600060D RID: 1549 RVA: 0x000245E4 File Offset: 0x000227E4
		private void DeleteOldSubscriptions()
		{
			using (IInformationServiceProxy2 informationServiceProxy = this._swisServiceProxyCreator.Create())
			{
				string text = "SELECT Uri FROM System.Subscription WHERE description = @description";
				foreach (DataRow dataRow in informationServiceProxy.Query(text, new Dictionary<string, object>
				{
					{
						"description",
						"NetObjectDowntimeIndication"
					}
				}).Rows.Cast<DataRow>())
				{
					informationServiceProxy.Delete(dataRow[0].ToString());
				}
			}
		}

		// Token: 0x040001DA RID: 474
		private const string NetObjectDowntimeIndication = "NetObjectDowntimeIndication";

		// Token: 0x040001DB RID: 475
		private const string NetObjectDowntimeInitializator = "NetObjectDowntimeInitializator";

		// Token: 0x040001DC RID: 476
		private string _nodeNetObjectIdColumn;

		// Token: 0x040001DD RID: 477
		protected static readonly Log log = new Log();

		// Token: 0x040001DE RID: 478
		private readonly INetObjectDowntimeDAL _netObjectDowntimeDal;

		// Token: 0x040001DF RID: 479
		private readonly IInformationServiceProxyCreator _swisServiceProxyCreator;

		// Token: 0x040001E0 RID: 480
		private readonly ISwisUriParser _swisUriParser;

		// Token: 0x040001E1 RID: 481
		private Lazy<ILookup<string, NetObjectTypeEx>> _netObjectTypes;

		// Token: 0x040001E2 RID: 482
		private static readonly Regex NodeIdRegex = new Regex("(N:\\d+)", RegexOptions.Compiled);

		// Token: 0x040001E3 RID: 483
		private readonly List<string> subscriptionUris = new List<string>();
	}
}
