using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SolarWinds.Common.Utility;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.BusinessLayer.InformationService;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.InformationService;
using SolarWinds.Orion.Swis.Contract.InformationService;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000038 RID: 56
	internal class ScheduledTaskFactory
	{
		// Token: 0x060003EA RID: 1002 RVA: 0x0001B680 File Offset: 0x00019880
		internal static ScheduledTask CreateDatabaseMaintenanceTask(InformationServiceSubscriptionProviderBase subscribtionProvider)
		{
			string text = string.Empty;
			DateTime dateTime;
			try
			{
				text = SettingsDAL.Get("SWNetPerfMon-Settings-Archive Time");
				dateTime = DateTime.FromOADate(double.Parse(text));
			}
			catch (Exception ex)
			{
				dateTime = DateTime.MinValue.Date.AddHours(2.0).AddMinutes(15.0);
				ScheduledTaskFactory.log.ErrorFormat("DB maintenance time setting is not set or is not correct. Setting value is {0}. \nException: {1}", text, ex);
			}
			ScheduledTaskInExactTime scheduledTaskInExactTime = new ScheduledTaskInExactTime("DatabaseMaintenance", new TimerCallback(ScheduledTaskFactory.RunDatabaseMaintenace), null, dateTime);
			if (subscribtionProvider != null)
			{
				SettingsArchiveTimeSubscriber settingsArchiveTimeSubscriber = new SettingsArchiveTimeSubscriber(scheduledTaskInExactTime);
				subscribtionProvider.Subscribe("SUBSCRIBE CHANGES TO Orion.Settings WHEN SettingsID = 'SWNetPerfMon-Settings-Archive Time'", settingsArchiveTimeSubscriber, new SubscriptionOptions
				{
					Description = "SettingsArchiveTimeSubscriber"
				});
			}
			else
			{
				ScheduledTaskFactory.log.Error("SubscribtionProvider is not initialized.");
			}
			return scheduledTaskInExactTime;
		}

		// Token: 0x060003EB RID: 1003 RVA: 0x0001B758 File Offset: 0x00019958
		private static void RunDatabaseMaintenace(object state)
		{
			ScheduledTaskFactory.log.Info("Database maintenance task is starting.");
			try
			{
				Process.Start(Path.Combine(OrionConfiguration.InstallPath, "Database-Maint.exe"), "-Archive");
				ScheduledTaskFactory.log.Info("Database maintenace task started.");
				SettingsDAL.Set("SWNetPerfMon-Settings-Last Archive", DateTime.UtcNow.ToOADate());
			}
			catch (Exception ex)
			{
				ScheduledTaskFactory.log.Error("Error while executing Database-Maint.exe.", ex);
			}
		}

		// Token: 0x040000E3 RID: 227
		private static readonly Log log = new Log();
	}
}
