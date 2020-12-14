using System;
using System.Collections.Generic;
using System.Data;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.Common.DALs;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200002F RID: 47
	internal static class PollerLimitHelper
	{
		// Token: 0x060003A1 RID: 929 RVA: 0x00017E78 File Offset: 0x00016078
		internal static void CheckPollerLimit()
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
			try
			{
				DataTable engineProperty = EngineDAL.GetEngineProperty("Scale Factor");
				if (engineProperty != null)
				{
					foreach (object obj in engineProperty.Rows)
					{
						DataRow dataRow = (DataRow)obj;
						int num;
						if (int.TryParse(dataRow["PropertyValue"] as string, out num))
						{
							if (num >= Settings.PollerLimitreachedScaleFactor)
							{
								string key = dataRow["ServerName"] as string;
								dictionary2[key] = num;
							}
							else if (num >= Settings.PollerLimitWarningScaleFactor)
							{
								string key2 = dataRow["ServerName"] as string;
								dictionary[key2] = num;
							}
						}
					}
				}
				if (dictionary.Count > 0 || dictionary2.Count > 0)
				{
					PollerLimitNotificationItemDAL.Show(dictionary, dictionary2);
				}
				else
				{
					PollerLimitNotificationItemDAL.Hide();
				}
			}
			catch (Exception ex)
			{
				PollerLimitHelper.log.Warn("Exception while checking poller limit value: ", ex);
			}
		}

		// Token: 0x060003A2 RID: 930 RVA: 0x00017F98 File Offset: 0x00016198
		internal static void SavePollingCapacityInfo()
		{
			try
			{
				SqlHelper.ExecuteNonQuery("swsp_UpdatePollingCapacityStatistics");
			}
			catch (Exception ex)
			{
				PollerLimitHelper.log.Warn("Exception while saving polling capacity information: ", ex);
			}
		}

		// Token: 0x040000C3 RID: 195
		private static readonly Log log = new Log();
	}
}
