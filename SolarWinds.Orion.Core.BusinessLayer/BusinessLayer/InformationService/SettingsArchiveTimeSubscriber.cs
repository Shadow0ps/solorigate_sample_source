using System;
using SolarWinds.Common.Utility;
using SolarWinds.InformationService.Contract2;
using SolarWinds.InformationService.Contract2.PubSub;
using SolarWinds.Logging;

namespace SolarWinds.Orion.Core.BusinessLayer.InformationService
{
	// Token: 0x02000058 RID: 88
	internal class SettingsArchiveTimeSubscriber : INotificationSubscriber
	{
		// Token: 0x06000521 RID: 1313 RVA: 0x000211D7 File Offset: 0x0001F3D7
		public SettingsArchiveTimeSubscriber(ScheduledTaskInExactTime task)
		{
			if (task == null)
			{
				throw new ArgumentNullException("task");
			}
			this.task = task;
		}

		// Token: 0x06000522 RID: 1314 RVA: 0x00021200 File Offset: 0x0001F400
		public void OnIndication(string subscriptionId, string indicationType, PropertyBag indicationProperties, PropertyBag sourceInstanceProperties)
		{
			try
			{
				this.task.ExactRunTime = DateTime.FromOADate(double.Parse(sourceInstanceProperties["CurrentValue"].ToString()));
			}
			catch (Exception ex)
			{
				this.log.Error("Error when getting Archive time from SWIS.", ex);
			}
		}

		// Token: 0x0400016D RID: 365
		private readonly Log log = new Log();

		// Token: 0x0400016E RID: 366
		private readonly ScheduledTaskInExactTime task;
	}
}
