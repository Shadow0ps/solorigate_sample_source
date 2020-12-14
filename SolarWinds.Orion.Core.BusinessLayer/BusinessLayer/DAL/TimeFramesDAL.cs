using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000093 RID: 147
	public class TimeFramesDAL
	{
		// Token: 0x06000708 RID: 1800 RVA: 0x0002CE1C File Offset: 0x0002B01C
		public static List<TimeFrame> GetCoreTimeFrames(string timeFrameName = null)
		{
			string sql = "\r\nSELECT tf.TimeFrameID, tf.Name, tf.StartTime, tf.EndTime, tf.IsDisabled, tfd.DayOfWeek, tfd.WholeDay\r\nFROM TimeFrames AS tf\r\nINNER JOIN TimeFrameDays AS tfd ON tf.TimeFrameID = tfd.TimeFrameID\r\nWHERE Name LIKE 'Core_%'\r\n";
			return TimeFramesDAL.GetTimeFrames(timeFrameName, sql);
		}

		// Token: 0x06000709 RID: 1801 RVA: 0x0002CE38 File Offset: 0x0002B038
		public static List<TimeFrame> GetAllTimeFrames(string timeFrameName = null)
		{
			string sql = "\r\nSELECT tf.TimeFrameID, tf.Name, tf.StartTime, tf.EndTime, tf.IsDisabled, tfd.DayOfWeek, tfd.WholeDay\r\nFROM TimeFrames AS tf\r\nINNER JOIN TimeFrameDays AS tfd ON tf.TimeFrameID = tfd.TimeFrameID\r\n";
			return TimeFramesDAL.GetTimeFrames(timeFrameName, sql);
		}

		// Token: 0x0600070A RID: 1802 RVA: 0x0002CE54 File Offset: 0x0002B054
		private static List<TimeFrame> GetTimeFrames(string timeFrameName, string sql)
		{
			List<TimeFrame> list = new List<TimeFrame>();
			if (timeFrameName != null)
			{
				sql = string.Format(CultureInfo.InvariantCulture, "{0} AND Name = '{1}'", sql, timeFrameName);
			}
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(sql))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						int id = DatabaseFunctions.GetInt32(dataReader, "TimeFrameID");
						TimeFrame timeFrame = list.Find((TimeFrame t) => t.Id.Equals(id));
						if (timeFrame == null)
						{
							timeFrame = new TimeFrame();
							timeFrame.Id = id;
							timeFrame.Name = DatabaseFunctions.GetString(dataReader, "Name");
							if (dataReader["StartTime"] != DBNull.Value)
							{
								timeFrame.StartTime = new DateTime?(DatabaseFunctions.GetDateTime(dataReader, "StartTime", DateTimeKind.Utc));
							}
							if (dataReader["EndTime"] != DBNull.Value)
							{
								timeFrame.EndTime = new DateTime?(DatabaseFunctions.GetDateTime(dataReader, "EndTime", DateTimeKind.Utc));
							}
							timeFrame.WeekDays = new Dictionary<int, bool>();
							list.Add(timeFrame);
						}
						timeFrame.WeekDays.Add(DatabaseFunctions.GetInt32(dataReader, "DayOfWeek"), DatabaseFunctions.GetBoolean(dataReader, "WholeDay"));
					}
				}
			}
			return list;
		}
	}
}
