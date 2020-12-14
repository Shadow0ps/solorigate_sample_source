using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.SqlClient;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common.Models.Thresholds;
using SolarWinds.Orion.Core.Common.Thresholds;

namespace SolarWinds.Orion.Core.BusinessLayer.Thresholds
{
	// Token: 0x0200004B RID: 75
	[Export(typeof(ThresholdDataProvider))]
	public class CoreThresholdDataProvider : ThresholdDataProvider
	{
		// Token: 0x060004B8 RID: 1208 RVA: 0x0001DAC2 File Offset: 0x0001BCC2
		public override IEnumerable<string> GetKnownThresholdNames()
		{
			yield return "Nodes.Stats.PercentMemoryUsed";
			yield return "Nodes.Stats.ResponseTime";
			yield return "Nodes.Stats.PercentLoss";
			yield return "Nodes.Stats.CpuLoad";
			yield break;
		}

		// Token: 0x060004B9 RID: 1209 RVA: 0x0001DACB File Offset: 0x0001BCCB
		public override Type GetThresholdDataProcessor()
		{
			return typeof(CoreThresholdProcessor);
		}

		// Token: 0x060004BA RID: 1210 RVA: 0x0001DAD8 File Offset: 0x0001BCD8
		public override StatisticalTableMetadata GetStatisticalTableMetadata(string thresholdName)
		{
			if (CoreThresholdDataProvider.IsResponseTime(thresholdName))
			{
				return new StatisticalTableMetadata
				{
					TableName = "ResponseTime_Statistics",
					InstanceIdColumnName = "NodeID",
					MeanColumnName = "AvgResponseTimeMean",
					StdDevColumnName = "AvgResponseTimeStDev",
					MinColumnName = "AvgResponseTimeMin",
					MaxColumnName = "AvgResponseTimeMax",
					CountColumnName = "AvgResponseTimeCount",
					MinDateTime = "MinDateTime",
					MaxDateTime = "MaxDateTime",
					Timestamp = "Timestamp"
				};
			}
			if (CoreThresholdDataProvider.IsPercentLoss(thresholdName))
			{
				return new StatisticalTableMetadata
				{
					TableName = "ResponseTime_Statistics",
					InstanceIdColumnName = "NodeID",
					MeanColumnName = "PercentLossMean",
					StdDevColumnName = "PercentLossStDev",
					MinColumnName = "PercentLossMin",
					MaxColumnName = "PercentLossMax",
					CountColumnName = "PercentLossCount",
					MinDateTime = "MinDateTime",
					MaxDateTime = "MaxDateTime",
					Timestamp = "Timestamp"
				};
			}
			if (CoreThresholdDataProvider.IsCpuLoad(thresholdName))
			{
				return new StatisticalTableMetadata
				{
					TableName = "CPULoad_Statistics",
					InstanceIdColumnName = "NodeID",
					MeanColumnName = "AvgLoadMean",
					StdDevColumnName = "AvgLoadStDev",
					MinColumnName = "AvgLoadMin",
					MaxColumnName = "AvgLoadMax",
					CountColumnName = "AvgLoadCount",
					MinDateTime = "MinDateTime",
					MaxDateTime = "MaxDateTime",
					Timestamp = "Timestamp"
				};
			}
			if (CoreThresholdDataProvider.IsPercentMemoryUsage(thresholdName))
			{
				return new StatisticalTableMetadata
				{
					TableName = "CPULoad_Statistics",
					InstanceIdColumnName = "NodeID",
					MeanColumnName = "AvgPercentMemoryUsedMean",
					StdDevColumnName = "AvgPercentMemoryUsedStDev",
					MinColumnName = "AvgPercentMemoryUsedMin",
					MaxColumnName = "AvgPercentMemoryUsedMax",
					CountColumnName = "AvgPercentMemoryUsedCount",
					MinDateTime = "MinDateTime",
					MaxDateTime = "MaxDateTime",
					Timestamp = "Timestamp"
				};
			}
			throw new InvalidOperationException(string.Format("Threshold name '{0}' is not supported.", thresholdName));
		}

		// Token: 0x060004BB RID: 1211 RVA: 0x0001DCE8 File Offset: 0x0001BEE8
		public override ThresholdMinMaxValue GetThresholdMinMaxValues(string thresholdName, int instanceId)
		{
			if (CoreThresholdDataProvider.IsResponseTime(thresholdName))
			{
				return new ThresholdMinMaxValue
				{
					Min = 0.0,
					Max = 100000.0,
					DataType = typeof(int)
				};
			}
			if (CoreThresholdDataProvider.IsPercentLoss(thresholdName))
			{
				return new ThresholdMinMaxValue
				{
					Min = 0.0,
					Max = 100.0,
					DataType = typeof(int)
				};
			}
			if (CoreThresholdDataProvider.IsCpuLoad(thresholdName))
			{
				return new ThresholdMinMaxValue
				{
					Min = 0.0,
					Max = 100.0,
					DataType = typeof(int)
				};
			}
			if (CoreThresholdDataProvider.IsPercentMemoryUsage(thresholdName))
			{
				return new ThresholdMinMaxValue
				{
					Min = 0.0,
					Max = 100.0,
					DataType = typeof(double)
				};
			}
			throw new InvalidOperationException(string.Format("Threshold name '{0}' is not supported.", thresholdName));
		}

		// Token: 0x060004BC RID: 1212 RVA: 0x0001DDF8 File Offset: 0x0001BFF8
		public override StatisticalData[] GetStatisticalData(string thresholdName, int instanceId, DateTime minDateTimeInUtc, DateTime maxDateTimeInUtc)
		{
			string text;
			if (CoreThresholdDataProvider.IsResponseTime(thresholdName))
			{
				text = "SELECT AvgResponseTime, [DateTime] FROM ResponseTime_Detail WHERE NodeID = @nodeId AND ([DateTime] between @start and @end)";
			}
			else if (CoreThresholdDataProvider.IsPercentLoss(thresholdName))
			{
				text = "SELECT PercentLoss, [DateTime] FROM ResponseTime_Detail WHERE NodeID = @nodeId AND ([DateTime] between @start and @end)";
			}
			else if (CoreThresholdDataProvider.IsCpuLoad(thresholdName))
			{
				text = "SELECT AvgLoad, [DateTime] FROM CPULoad_Detail WHERE NodeID = @nodeId AND ([DateTime] between @start and @end)";
			}
			else
			{
				if (!CoreThresholdDataProvider.IsPercentMemoryUsage(thresholdName))
				{
					throw new InvalidOperationException(string.Format("Threshold name '{0}' is not supported.", thresholdName));
				}
				text = "SELECT AvgPercentMemoryUsed, [DateTime] FROM CPULoad_Detail WHERE NodeID = @nodeId AND ([DateTime] between @start and @end)";
			}
			List<StatisticalData> list = new List<StatisticalData>();
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				using (SqlCommand textCommand = SqlHelper.GetTextCommand(text))
				{
					textCommand.Parameters.AddWithValue("nodeId", instanceId).SqlDbType = SqlDbType.Int;
					textCommand.Parameters.AddWithValue("start", minDateTimeInUtc.ToLocalTime()).SqlDbType = SqlDbType.DateTime;
					textCommand.Parameters.AddWithValue("end", maxDateTimeInUtc.ToLocalTime()).SqlDbType = SqlDbType.DateTime;
					using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand, sqlConnection))
					{
						while (dataReader.Read())
						{
							if (!dataReader.IsDBNull(0) && !dataReader.IsDBNull(1))
							{
								list.Add(new StatisticalData
								{
									Value = Convert.ToDouble(dataReader[0]),
									Date = DatabaseFunctions.GetDateTime(dataReader, 1, DateTimeKind.Local)
								});
							}
						}
					}
				}
			}
			return list.ToArray();
		}

		// Token: 0x060004BD RID: 1213 RVA: 0x0001DF70 File Offset: 0x0001C170
		public override string GetThresholdInstanceName(string thresholdName, int instanceId)
		{
			string text = "SELECT [Caption] FROM [NodesData] WHERE [NodeId] = @NodeId";
			string result;
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				using (SqlCommand textCommand = SqlHelper.GetTextCommand(text))
				{
					textCommand.Connection = sqlConnection;
					textCommand.Parameters.AddWithValue("NodeId", instanceId).SqlDbType = SqlDbType.Int;
					object obj = textCommand.ExecuteScalar();
					if (obj != null && obj != DBNull.Value)
					{
						result = obj.ToString();
					}
					else
					{
						result = string.Empty;
					}
				}
			}
			return result;
		}

		// Token: 0x060004BE RID: 1214 RVA: 0x0001E00C File Offset: 0x0001C20C
		public override string GetStatisticalDataChartName(string thresholdName)
		{
			if (CoreThresholdDataProvider.IsResponseTime(thresholdName))
			{
				return "MinMaxAvgRT";
			}
			if (CoreThresholdDataProvider.IsPercentLoss(thresholdName))
			{
				return "PacketLossLine";
			}
			if (CoreThresholdDataProvider.IsCpuLoad(thresholdName))
			{
				return "CiscoMMAvgCPULoad";
			}
			if (CoreThresholdDataProvider.IsPercentMemoryUsage(thresholdName))
			{
				return "HostAvgPercentMemoryUsed";
			}
			throw new InvalidOperationException(string.Format("Threshold name '{0}' is not supported.", thresholdName));
		}

		// Token: 0x060004BF RID: 1215 RVA: 0x0001E061 File Offset: 0x0001C261
		private static bool IsResponseTime(string thresholdName)
		{
			return string.Equals(thresholdName, "Nodes.Stats.ResponseTime", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060004C0 RID: 1216 RVA: 0x0001E06F File Offset: 0x0001C26F
		private static bool IsPercentLoss(string thresholdName)
		{
			return string.Equals(thresholdName, "Nodes.Stats.PercentLoss", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060004C1 RID: 1217 RVA: 0x0001E07D File Offset: 0x0001C27D
		private static bool IsCpuLoad(string thresholdName)
		{
			return string.Equals(thresholdName, "Nodes.Stats.CpuLoad", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060004C2 RID: 1218 RVA: 0x0001E08B File Offset: 0x0001C28B
		private static bool IsPercentMemoryUsage(string thresholdName)
		{
			return string.Equals(thresholdName, "Nodes.Stats.PercentMemoryUsed", StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x04000146 RID: 326
		private const string PercentMemoryUsedName = "Nodes.Stats.PercentMemoryUsed";

		// Token: 0x04000147 RID: 327
		private const string ResponseTimeName = "Nodes.Stats.ResponseTime";

		// Token: 0x04000148 RID: 328
		private const string PercentLossName = "Nodes.Stats.PercentLoss";

		// Token: 0x04000149 RID: 329
		private const string CpuLoadName = "Nodes.Stats.CpuLoad";

		// Token: 0x0400014A RID: 330
		private const string PercentMemoryUsedChartName = "HostAvgPercentMemoryUsed";

		// Token: 0x0400014B RID: 331
		private const string ResponseTimeChartName = "MinMaxAvgRT";

		// Token: 0x0400014C RID: 332
		private const string PercentLossChartName = "PacketLossLine";

		// Token: 0x0400014D RID: 333
		private const string CpuLoadChartName = "CiscoMMAvgCPULoad";
	}
}
