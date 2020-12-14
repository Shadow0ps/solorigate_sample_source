using System;
using System.Linq;
using SolarWinds.InformationService.Contract2;
using SolarWinds.InformationService.Contract2.PubSub;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.Common.Indications;
using SolarWinds.Orion.Core.Common.InformationService;
using SolarWinds.Orion.Core.Models.MaintenanceMode;

namespace SolarWinds.Orion.Core.BusinessLayer.MaintenanceMode
{
	// Token: 0x02000067 RID: 103
	internal class MaintenanceIndicationSubscriber : INotificationSubscriber, IDisposable
	{
		// Token: 0x0600058D RID: 1421 RVA: 0x00021C78 File Offset: 0x0001FE78
		public MaintenanceIndicationSubscriber() : this(new MaintenanceManager(InformationServiceProxyPoolCreatorFactory.GetSystemCreator(), new MaintenanceModePlanDAL()), InformationServiceSubscriptionProviderShared.Instance())
		{
		}

		// Token: 0x0600058E RID: 1422 RVA: 0x00021C94 File Offset: 0x0001FE94
		public MaintenanceIndicationSubscriber(IMaintenanceManager manager, InformationServiceSubscriptionProviderBase subscriptionProvider)
		{
			this.manager = manager;
			this.subscriptionProvider = subscriptionProvider;
		}

		// Token: 0x0600058F RID: 1423 RVA: 0x00021CAC File Offset: 0x0001FEAC
		public void Start()
		{
			try
			{
				this.subscriptionId = this.Subscribe();
			}
			catch (Exception ex)
			{
				MaintenanceIndicationSubscriber.log.ErrorFormat("Unable to start maintenance mode service. Unmanage functionality may be affected. {0}", ex);
				throw;
			}
		}

		// Token: 0x06000590 RID: 1424 RVA: 0x00021CEC File Offset: 0x0001FEEC
		public void OnIndication(string subscriptionId, string indicationType, PropertyBag indicationProperties, PropertyBag sourceInstanceProperties)
		{
			if (this.subscriptionId != subscriptionId)
			{
				return;
			}
			MaintenanceIndicationSubscriber.log.DebugFormat("Received maintenance mode indication '{0}'.", indicationType);
			MaintenanceIndicationSubscriber.log.DebugFormat("Indication Properties: {0}", indicationProperties);
			MaintenanceIndicationSubscriber.log.DebugFormat("Source Instance Properties: {0}", sourceInstanceProperties);
			try
			{
				MaintenancePlanAssignment assignment = this.CreateAssignment(sourceInstanceProperties);
				if (IndicationHelper.GetIndicationType(0).Equals(indicationType))
				{
					this.manager.Unmanage(assignment);
				}
				else if (IndicationHelper.GetIndicationType(1).Equals(indicationType))
				{
					this.manager.Remanage(assignment);
				}
				else
				{
					IndicationHelper.GetIndicationType(2).Equals(indicationType);
				}
			}
			catch (Exception ex)
			{
				MaintenanceIndicationSubscriber.log.ErrorFormat("Unable to process maintenance mode indication. {0}", ex);
				throw;
			}
		}

		// Token: 0x06000591 RID: 1425 RVA: 0x00021DAC File Offset: 0x0001FFAC
		internal MaintenancePlanAssignment CreateAssignment(PropertyBag sourceInstanceProperties)
		{
			if (sourceInstanceProperties == null)
			{
				throw new ArgumentNullException("sourceInstanceProperties");
			}
			if (!sourceInstanceProperties.Keys.Any<string>())
			{
				throw new ArgumentException("sourceInstanceProperties");
			}
			return new MaintenancePlanAssignment
			{
				ID = Convert.ToInt32(sourceInstanceProperties["ID"]),
				EntityType = Convert.ToString(sourceInstanceProperties["EntityType"]),
				EntityID = Convert.ToInt32(sourceInstanceProperties["EntityID"]),
				MaintenancePlanID = Convert.ToInt32(sourceInstanceProperties["MaintenancePlanID"])
			};
		}

		// Token: 0x06000592 RID: 1426 RVA: 0x00021E3C File Offset: 0x0002003C
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06000593 RID: 1427 RVA: 0x00021E4C File Offset: 0x0002004C
		protected void Dispose(bool disposing)
		{
			if (!string.IsNullOrEmpty(this.subscriptionId))
			{
				try
				{
					this.Unsubscribe(this.subscriptionId);
					this.subscriptionId = null;
				}
				catch (Exception ex)
				{
					MaintenanceIndicationSubscriber.log.ErrorFormat("Error unsubscribing subscription '{0}'. {1}", this.subscriptionId, ex);
				}
			}
		}

		// Token: 0x06000594 RID: 1428 RVA: 0x00021EA4 File Offset: 0x000200A4
		private string Subscribe()
		{
			return this.subscriptionProvider.Subscribe("SUBSCRIBE CHANGES TO Orion.MaintenancePlanAssignment", this);
		}

		// Token: 0x06000595 RID: 1429 RVA: 0x00021EB7 File Offset: 0x000200B7
		private void Unsubscribe(string subscriptionId)
		{
			this.subscriptionProvider.Unsubscribe(subscriptionId);
		}

		// Token: 0x06000596 RID: 1430 RVA: 0x00021EC8 File Offset: 0x000200C8
		~MaintenanceIndicationSubscriber()
		{
			this.Dispose(false);
		}

		// Token: 0x0400019C RID: 412
		private static readonly Log log = new Log();

		// Token: 0x0400019D RID: 413
		private readonly IMaintenanceManager manager;

		// Token: 0x0400019E RID: 414
		private readonly InformationServiceSubscriptionProviderBase subscriptionProvider;

		// Token: 0x0400019F RID: 415
		private string subscriptionId;

		// Token: 0x040001A0 RID: 416
		private const string SubscriptionQuery = "SUBSCRIBE CHANGES TO Orion.MaintenancePlanAssignment";
	}
}
