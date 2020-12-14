using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x0200008C RID: 140
	internal static class HistoryTableDdlDAL
	{
		// Token: 0x060006D2 RID: 1746 RVA: 0x0002B708 File Offset: 0x00029908
		public static void EnsureHistoryTables()
		{
			DateTime value = DateTime.FromOADate(SettingsDAL.GetCurrent<double>("SWNetPerfMon-Settings-Last Archive", new DateTime(1900, 1, 1).ToOADate()));
			int days = DateTime.UtcNow.Subtract(value).Days;
			DataTable dataTable;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT ObjectName, ObjectType FROM [dbo].[HistoryTableDDL] WHERE NumberOfDaysAhead < @lastMaintDays"))
			{
				textCommand.Parameters.AddWithValue("lastMaintDays", days);
				dataTable = SqlHelper.ExecuteDataTable(textCommand);
			}
			HistoryTableDdlDAL.log.InfoFormat("Days since last maintenance: {0}; Creating {1} tables", days, dataTable.Rows.Count);
			foreach (DataRow row in dataTable.Rows.Cast<DataRow>())
			{
				string text = row.Field(0);
				string text2 = row.Field(1);
				HistoryTableDdlDAL.log.DebugFormat("Creating table {0}-{1}", text, text2);
				using (SqlCommand textCommand2 = SqlHelper.GetTextCommand("Exec [dbo].[dbm_SlidePartitionedView] @objectName, @objectType, @dropOldTables"))
				{
					textCommand2.Parameters.AddWithValue("objectName", text);
					textCommand2.Parameters.AddWithValue("objectType", text2);
					textCommand2.Parameters.AddWithValue("dropOldTables", false);
					SqlHelper.ExecuteNonQuery(textCommand2);
				}
			}
		}

		// Token: 0x04000221 RID: 545
		private static readonly Log log = new Log();
	}
}
