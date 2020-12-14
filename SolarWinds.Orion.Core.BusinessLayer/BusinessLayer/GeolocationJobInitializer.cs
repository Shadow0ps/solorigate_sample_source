using System;
using SolarWinds.Common.Utility;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Models.Actions;
using SolarWinds.Orion.Core.Models.Actions.Contexts;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000043 RID: 67
	public class GeolocationJobInitializer
	{
		// Token: 0x06000431 RID: 1073 RVA: 0x0001C648 File Offset: 0x0001A848
		public static void AddActionsToScheduler(CoreBusinessLayerService service)
		{
			GeolocationActionContext geolocationContext = new GeolocationActionContext();
			string[] entitiesAvailableForGeolocation = WorldMapPointsDAL.GetEntitiesAvailableForGeolocation();
			int num = 1;
			string[] array = entitiesAvailableForGeolocation;
			for (int i = 0; i < array.Length; i++)
			{
				string currentEntity2 = array[i];
				string currentEntity = currentEntity2;
				ScheduledTask scheduledTask = new ScheduledTask(string.Format("GeolocationJob-{0}", num), delegate(object o)
				{
					if (!Settings.IsAutomaticGeolocationEnabled)
					{
						return;
					}
					string text;
					if (!WebSettingsDAL.TryGet(string.Format("{0}_GeolocationField", currentEntity), ref text))
					{
						return;
					}
					if (string.IsNullOrWhiteSpace(text))
					{
						return;
					}
					GeolocationJobInitializer.log.Info("Starting action execution");
					CoreBusinessLayerService service2 = service;
					ActionDefinition actionDefinition = new ActionDefinition();
					actionDefinition.ActionTypeID = "Geolocation";
					actionDefinition.Enabled = true;
					ActionProperties actionProperties = new ActionProperties();
					actionProperties.Add("StreetAddress", text);
					actionProperties.Add("Entity", currentEntity);
					actionProperties.Add("MapQuestApiKey", WorldMapPointsDAL.GetMapQuestKey());
					actionDefinition.Properties = actionProperties;
					service2.ExecuteAction(actionDefinition, geolocationContext);
				}, null, Settings.AutomaticGeolocationCheckInterval);
				Scheduler.Instance.Add(scheduledTask);
				num++;
			}
		}

		// Token: 0x040000FE RID: 254
		private static readonly Log log = new Log();

		// Token: 0x040000FF RID: 255
		public const string JobNamingPattern = "GeolocationJob-{0}";
	}
}
