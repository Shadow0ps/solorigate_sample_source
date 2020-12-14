using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Data;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Common.Models.Thresholds;
using SolarWinds.Orion.Core.Common.Settings;
using SolarWinds.Orion.Core.Common.Thresholds;
using SolarWinds.Orion.Core.Models;
using SolarWinds.Orion.Core.Strings;

namespace SolarWinds.Orion.Core.BusinessLayer.Thresholds
{
	// Token: 0x02000054 RID: 84
	internal class ThresholdProcessingEngine
	{
		// Token: 0x060004EF RID: 1263 RVA: 0x0001F2B4 File Offset: 0x0001D4B4
		internal ThresholdProcessingEngine(IEnumerable<IThresholdDataProcessor> thresholdProcessors, IEnumerable<ThresholdDataProvider> thresholdDataProviders, IThresholdIndicator thresholdIndicator, ICollectorSettings settings)
		{
			if (thresholdIndicator == null)
			{
				throw new ArgumentNullException("thresholdIndicator");
			}
			this._thresholdIndicator = thresholdIndicator;
			if (settings == null)
			{
				throw new ArgumentNullException("settings");
			}
			this._settings = settings;
			if (thresholdProcessors == null)
			{
				throw new ArgumentNullException("thresholdProcessors");
			}
			if (thresholdDataProviders == null)
			{
				throw new ArgumentNullException("thresholdDataProviders");
			}
			this._thresholdProcessors = thresholdProcessors.ToArray<IThresholdDataProcessor>();
			this._thresholdDataProviders = thresholdDataProviders.ToArray<ThresholdDataProvider>();
			if (!this._thresholdProcessors.Any<IThresholdDataProcessor>())
			{
				throw new InvalidOperationException("At least one threshold processor must be defined.");
			}
			if (!this._thresholdDataProviders.Any<ThresholdDataProvider>())
			{
				throw new InvalidOperationException("At least one threshold data provider must be defined.");
			}
			if (this._thresholdDataProviders.SelectMany((ThresholdDataProvider p) => p.GetKnownThresholdNames()).GroupBy((string n) => n, StringComparer.OrdinalIgnoreCase).Any((IGrouping<string, string> g) => g.Count<string>() > 1))
			{
				throw new InvalidOperationException("Threshold data providers do not provide unique known thresholds names.");
			}
			this._dataProcessorsDictionary = new Dictionary<string, IThresholdDataProcessor>(StringComparer.OrdinalIgnoreCase);
			this._dataProvidersDictionary = new Dictionary<string, ThresholdDataProvider>(StringComparer.OrdinalIgnoreCase);
			ThresholdProcessingEngine._log.Debug("Starting _thresholdDataProviders processing.");
			ThresholdDataProvider[] thresholdDataProviders2 = this._thresholdDataProviders;
			for (int i = 0; i < thresholdDataProviders2.Length; i++)
			{
				ThresholdDataProvider thresholdDataProvider = thresholdDataProviders2[i];
				ThresholdProcessingEngine._log.DebugFormat("Processing provider '{0}'.", thresholdDataProvider);
				Type processorType = thresholdDataProvider.GetThresholdDataProcessor();
				if (processorType == null || !typeof(IThresholdDataProcessor).IsAssignableFrom(processorType))
				{
					throw new InvalidOperationException(string.Format("Invalid threshold processor type '{0}'", processorType));
				}
				IThresholdDataProcessor thresholdDataProcessor = this._thresholdProcessors.FirstOrDefault((IThresholdDataProcessor p) => p.GetType() == processorType);
				ThresholdProcessingEngine._log.DebugFormat("Getting processor '{0}'.", thresholdDataProcessor);
				if (thresholdDataProcessor == null)
				{
					throw new InvalidOperationException(string.Format("Cannot find any threshold processor for type '{0}'", processorType));
				}
				foreach (string key in thresholdDataProvider.GetKnownThresholdNames())
				{
					this._dataProcessorsDictionary.Add(key, thresholdDataProcessor);
					this._dataProvidersDictionary.Add(key, thresholdDataProvider);
				}
			}
			ThresholdProcessingEngine._log.Debug("_thresholdDataProviders processing finished.");
		}

		// Token: 0x170000BD RID: 189
		// (get) Token: 0x060004F0 RID: 1264 RVA: 0x0001F54C File Offset: 0x0001D74C
		// (set) Token: 0x060004F1 RID: 1265 RVA: 0x0001F554 File Offset: 0x0001D754
		internal int BatchSize
		{
			get
			{
				return this._batchSize;
			}
			set
			{
				if (value <= 0 || value > 100000)
				{
					throw new ArgumentOutOfRangeException("value", "BatchSize has valid range between 1 and 100000.");
				}
				this._batchSize = value;
			}
		}

		// Token: 0x170000BE RID: 190
		// (get) Token: 0x060004F2 RID: 1266 RVA: 0x0001F579 File Offset: 0x0001D779
		// (set) Token: 0x060004F3 RID: 1267 RVA: 0x0001F581 File Offset: 0x0001D781
		internal string BaselineTimeFrame
		{
			get
			{
				return this._baselineTimeFrame;
			}
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					throw new ArgumentNullException("value");
				}
				this._baselineTimeFrame = value;
			}
		}

		// Token: 0x170000BF RID: 191
		// (get) Token: 0x060004F4 RID: 1268 RVA: 0x0001F59D File Offset: 0x0001D79D
		internal int BaselineCollectionDuration
		{
			get
			{
				return SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-Baseline Collection Duration", 7);
			}
		}

		// Token: 0x170000C0 RID: 192
		// (get) Token: 0x060004F5 RID: 1269 RVA: 0x0001F5AA File Offset: 0x0001D7AA
		internal IEnumerable<ThresholdDataProvider> ThresholdDataProviders
		{
			get
			{
				return this._thresholdDataProviders;
			}
		}

		// Token: 0x170000C1 RID: 193
		// (get) Token: 0x060004F6 RID: 1270 RVA: 0x0001F5B2 File Offset: 0x0001D7B2
		internal IEnumerable<IThresholdDataProcessor> ThresholdDataProcessors
		{
			get
			{
				return this._thresholdProcessors;
			}
		}

		// Token: 0x060004F7 RID: 1271 RVA: 0x0001F5BC File Offset: 0x0001D7BC
		public BaselineValues GetBaselineValues(string thresholdName, int instanceId)
		{
			if (string.IsNullOrEmpty(thresholdName))
			{
				throw new ArgumentNullException("thresholdName");
			}
			BaselineValues result;
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				ThresholdDataProvider thresholdDataProvider = this.GetThresholdDataProvider(thresholdName, true);
				StatisticalTableMetadata statisticalTableMetadata = thresholdDataProvider.GetStatisticalTableMetadata(thresholdName);
				string arg = thresholdDataProvider.CreateProjectionFromMetadata(statisticalTableMetadata);
				using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Format("SELECT t.Name as TimeFrameName,\r\n                             {0} \r\n                             {1}\r\n                        FROM {2} stats\r\n                        JOIN TimeFrames t ON t.TimeFrameID = stats.TimeFrameID\r\n                       WHERE stats.{0} = @instanceId\r\n                         AND t.Name = @timeFrameName", statisticalTableMetadata.InstanceIdColumnName, arg, statisticalTableMetadata.TableName)))
				{
					textCommand.Parameters.AddWithValue("instanceId", instanceId);
					textCommand.Parameters.AddWithValue("timeFrameName", this.BaselineTimeFrame);
					using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand, sqlConnection))
					{
						if (dataReader.Read())
						{
							result = thresholdDataProvider.CreateBaselineValuesFromReader(dataReader);
						}
						else
						{
							result = null;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x060004F8 RID: 1272 RVA: 0x0001F6BC File Offset: 0x0001D8BC
		public List<BaselineValues> GetBaselineValuesForAllTimeFrames(string thresholdName, int instanceId)
		{
			if (string.IsNullOrEmpty(thresholdName))
			{
				throw new ArgumentNullException("thresholdName");
			}
			List<BaselineValues> list = new List<BaselineValues>();
			List<BaselineValues> result;
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				ThresholdDataProvider thresholdDataProvider = this.GetThresholdDataProvider(thresholdName, true);
				StatisticalTableMetadata statisticalTableMetadata = thresholdDataProvider.GetStatisticalTableMetadata(thresholdName);
				string arg = thresholdDataProvider.CreateProjectionFromMetadata(statisticalTableMetadata);
				using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Format("SELECT t.Name as TimeFrameName,\r\n                             {0} \r\n                             {1}\r\n                        FROM {2} stats\r\n                        JOIN TimeFrames t ON t.TimeFrameID = stats.TimeFrameID\r\n                       WHERE stats.{0} = @instanceId", statisticalTableMetadata.InstanceIdColumnName, arg, statisticalTableMetadata.TableName)))
				{
					textCommand.Parameters.AddWithValue("instanceId", instanceId);
					using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand, sqlConnection))
					{
						while (dataReader.Read())
						{
							BaselineValues baselineValues = thresholdDataProvider.CreateBaselineValuesFromReader(dataReader);
							if (baselineValues != null)
							{
								list.Add(baselineValues);
							}
						}
					}
				}
				result = list;
			}
			return result;
		}

		// Token: 0x060004F9 RID: 1273 RVA: 0x0001F7B8 File Offset: 0x0001D9B8
		public ThresholdComputationResult ComputeThresholds(string thresholdName, int instanceId, string warningFormula, string criticalFormula, BaselineValues baselineValues, ThresholdOperatorEnum thresholdOperator)
		{
			if (string.IsNullOrEmpty(thresholdName))
			{
				throw new ArgumentNullException("thresholdName");
			}
			if (baselineValues == null)
			{
				throw new ArgumentNullException("baselineValues");
			}
			IThresholdDataProcessor thresholdDataProcessor = this.GetThresholdDataProcessor(thresholdName, true);
			bool flag = !string.IsNullOrEmpty(warningFormula);
			bool flag2 = !string.IsNullOrEmpty(criticalFormula);
			if (flag && !thresholdDataProcessor.IsFormulaValid(warningFormula, ThresholdLevel.Warning, thresholdOperator).IsValid)
			{
				throw new InvalidOperationException(string.Format("Provided formula '{0}' is not valid.", warningFormula));
			}
			if (flag2 && !thresholdDataProcessor.IsFormulaValid(criticalFormula, ThresholdLevel.Critical, thresholdOperator).IsValid)
			{
				throw new InvalidOperationException(string.Format("Provided formula '{0}' is not valid.", criticalFormula));
			}
			ThresholdComputationResult result;
			try
			{
				int disabledValue = ThresholdsHelper.GetDisabledValue(thresholdOperator);
				double warningThreshold = flag ? thresholdDataProcessor.ComputeThreshold(warningFormula, baselineValues, ThresholdLevel.Warning, thresholdOperator) : ((double)disabledValue);
				double criticalThreshold = flag2 ? thresholdDataProcessor.ComputeThreshold(criticalFormula, baselineValues, ThresholdLevel.Critical, thresholdOperator) : ((double)disabledValue);
				ThresholdMinMaxValue thresholdMinMaxValues = this.GetThresholdDataProvider(thresholdName, true).GetThresholdMinMaxValues(thresholdName, instanceId);
				result = this.ProcessThresholds(flag, warningThreshold, flag2, criticalThreshold, thresholdOperator, thresholdMinMaxValues);
			}
			catch (DivideByZeroException)
			{
				result = new ThresholdComputationResult
				{
					IsSuccess = false,
					Message = Resources.LIBCODE_PC0_02
				};
			}
			return result;
		}

		// Token: 0x060004FA RID: 1274 RVA: 0x0001F8D8 File Offset: 0x0001DAD8
		public ValidationResult IsFormulaValid(string thresholdName, string formula, ThresholdLevel level, ThresholdOperatorEnum thresholdOperator)
		{
			if (string.IsNullOrEmpty(thresholdName))
			{
				throw new ArgumentNullException("thresholdName");
			}
			return this.GetThresholdDataProcessor(thresholdName, true).IsFormulaValid(formula, level, thresholdOperator);
		}

		// Token: 0x060004FB RID: 1275 RVA: 0x0001F8FE File Offset: 0x0001DAFE
		public ThresholdMinMaxValue GetThresholdMinMaxValues(string thresholdName, int instanceId)
		{
			if (string.IsNullOrEmpty(thresholdName))
			{
				throw new ArgumentNullException("thresholdName");
			}
			return this.GetThresholdDataProvider(thresholdName, true).GetThresholdMinMaxValues(thresholdName, instanceId);
		}

		// Token: 0x060004FC RID: 1276 RVA: 0x0001F924 File Offset: 0x0001DB24
		public int SetThreshold(Threshold threshold)
		{
			int maxThresholdPollsInterval = this._settings.MaxThresholdPollsInterval;
			if (threshold == null)
			{
				throw new ArgumentNullException("threshold");
			}
			if (string.IsNullOrEmpty(threshold.ThresholdName))
			{
				throw new InvalidOperationException("Threshold name have to be set.");
			}
			if (!string.IsNullOrEmpty(threshold.WarningFormula) && !this.IsFormulaValid(threshold.ThresholdName, threshold.WarningFormula, ThresholdLevel.Warning, threshold.ThresholdOperator).IsValid)
			{
				throw new InvalidOperationException(string.Format("Warning formula '{0}' is not valid.", threshold.WarningFormula));
			}
			if (!string.IsNullOrEmpty(threshold.CriticalFormula) && !this.IsFormulaValid(threshold.ThresholdName, threshold.CriticalFormula, ThresholdLevel.Critical, threshold.ThresholdOperator).IsValid)
			{
				throw new InvalidOperationException(string.Format("Critical formula '{0}' is not valid.", threshold.CriticalFormula));
			}
			if (threshold.WarningPolls != null || threshold.WarningPollsInterval != null)
			{
				if (threshold.WarningPolls == null || threshold.WarningPollsInterval == null)
				{
					throw new ArgumentException("Both WarningPolls and WarningPollsInterval must be set");
				}
				if (threshold.WarningPolls.Value < 1 || threshold.WarningPollsInterval.Value < 1)
				{
					throw new ArgumentException("Values in Warning fields must be at least 1");
				}
				if (maxThresholdPollsInterval < threshold.WarningPollsInterval.Value)
				{
					throw new ArgumentException(string.Format("Number of total warning polls is greater than limit: {0}.", maxThresholdPollsInterval));
				}
				if (threshold.WarningPollsInterval.Value < threshold.WarningPolls.Value)
				{
					throw new ArgumentException("Number of expected warning polls is greater than number of total polls");
				}
			}
			if (threshold.CriticalPolls != null || threshold.CriticalPollsInterval != null)
			{
				if (threshold.CriticalPolls == null || threshold.CriticalPollsInterval == null)
				{
					throw new ArgumentException("Both CriticalPolls and CriticalPollsInterval must be set");
				}
				if (threshold.CriticalPolls.Value < 1 || threshold.CriticalPollsInterval.Value < 1)
				{
					throw new ArgumentException("Values in Critical fields must be at least 1");
				}
				if (maxThresholdPollsInterval < threshold.CriticalPollsInterval.Value)
				{
					throw new ArgumentException(string.Format("Number of total critical polls is greater than limit: {0}.", maxThresholdPollsInterval));
				}
				if (threshold.CriticalPollsInterval.Value < threshold.CriticalPolls.Value)
				{
					throw new ArgumentException("Number of expected critical polls is greater than number of total polls");
				}
			}
			this._thresholdIndicator.LoadPreviousThresholdData(threshold.InstanceId, threshold.ThresholdName);
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				using (SqlCommand textCommand = SqlHelper.GetTextCommand("\r\n                    DECLARE @thresholdNameId int\r\n                    DECLARE @thresholdId int\r\n\r\n                    SELECT @thresholdNameId = Id FROM dbo.ThresholdsNames WHERE Name = @thresholdName\r\n                    SELECT @thresholdId = Id FROM dbo.Thresholds WHERE InstanceId = @instanceId AND ThresholdNameId = @thresholdNameId\r\n\r\n                    IF @thresholdId IS NULL\r\n                    BEGIN\r\n\t                    INSERT INTO dbo.Thresholds (InstanceId,ThresholdType,ThresholdNameId,ThresholdOperator,Warning,Critical\r\n\t\t\t                       ,WarningFormula,CriticalFormula,BaselineFrom,BaselineTo,BaselineApplied,BaselineApplyError\r\n                                    ,WarningPolls,WarningPollsInterval,CriticalPolls,CriticalPollsInterval,WarningEnabled,CriticalEnabled)\r\n                         VALUES (@instanceId,@thresholdType,@thresholdNameId,@thresholdOperator,@warning,@critical\r\n\t                            ,@warningFormula,@criticalFormula,@baselineFrom,@baselineTo,@baselineApplied,@baselineApplyError\r\n                                 ,@warningpolls,@warningpollsinterval,@criticalpolls,@criticalpollsinterval,@warningenabled,@criticalenabled)\r\n\t                    SET @thresholdId = SCOPE_IDENTITY()\r\n                    END\r\n                    ELSE\r\n                    BEGIN\r\n\t                    UPDATE dbo.Thresholds\r\n\t                       SET ThresholdType = @thresholdType\r\n\t\t                      ,ThresholdOperator = @thresholdOperator\r\n\t\t                      ,Warning = @warning\r\n\t\t                      ,Critical = @critical\r\n\t\t                      ,WarningFormula = @warningFormula\r\n\t\t                      ,CriticalFormula = @criticalFormula\r\n\t\t                      ,BaselineFrom = @baselineFrom\r\n\t\t                      ,BaselineTo = @baselineTo\r\n\t\t                      ,BaselineApplied = @baselineApplied\r\n\t\t                      ,BaselineApplyError  = @baselineApplyError\r\n                              ,WarningPolls = @warningpolls\r\n                              ,WarningPollsInterval = @warningpollsinterval\r\n                              ,CriticalPolls = @criticalpolls\r\n                              ,CriticalPollsInterval = @criticalpollsinterval\r\n                              ,WarningEnabled = @warningenabled\r\n                              ,CriticalEnabled = @criticalenabled\r\n\t                     WHERE Id = @thresholdId\r\n                    END\r\n                    SELECT @thresholdId AS ThresholdId"))
				{
					textCommand.Parameters.AddWithValue("instanceId", threshold.InstanceId);
					textCommand.Parameters.AddWithValue("thresholdName", threshold.ThresholdName);
					textCommand.Parameters.AddWithValue("thresholdType", threshold.ThresholdType);
					textCommand.Parameters.AddWithValue("thresholdOperator", threshold.ThresholdOperator);
					textCommand.Parameters.AddWithValue("baselineApplyError", threshold.BaselineApplyError ?? DBNull.Value);
					textCommand.Parameters.AddWithValue("warningpolls", threshold.WarningPolls ?? 1);
					textCommand.Parameters.AddWithValue("warningpollsinterval", threshold.WarningPollsInterval ?? 1);
					textCommand.Parameters.AddWithValue("criticalpolls", threshold.CriticalPolls ?? 1);
					textCommand.Parameters.AddWithValue("criticalpollsinterval", threshold.CriticalPollsInterval ?? 1);
					textCommand.Parameters.AddWithValue("warningenabled", threshold.WarningEnabled);
					textCommand.Parameters.AddWithValue("criticalenabled", threshold.CriticalEnabled);
					textCommand.Parameters.AddWithValue("warning", (threshold.Warning != null) ? threshold.Warning.Value : DBNull.Value);
					textCommand.Parameters.AddWithValue("critical", (threshold.Critical != null) ? threshold.Critical.Value : DBNull.Value);
					textCommand.Parameters.AddWithValue("warningFormula", string.IsNullOrEmpty(threshold.WarningFormula) ? DBNull.Value : threshold.WarningFormula);
					textCommand.Parameters.AddWithValue("criticalFormula", string.IsNullOrEmpty(threshold.CriticalFormula) ? DBNull.Value : threshold.CriticalFormula);
					if (threshold.BaselineApplied != null)
					{
						textCommand.Parameters.AddWithValue("baselineApplied", threshold.BaselineApplied.Value);
						textCommand.Parameters.AddWithValue("baselineFrom", threshold.BaselineFrom.Value);
						textCommand.Parameters.AddWithValue("baselineTo", threshold.BaselineTo.Value);
					}
					else
					{
						textCommand.Parameters.AddWithValue("baselineFrom", DBNull.Value);
						textCommand.Parameters.AddWithValue("baselineTo", DBNull.Value);
						textCommand.Parameters.AddWithValue("baselineApplied", DBNull.Value);
					}
					threshold.Id = (int)SqlHelper.ExecuteScalar(textCommand, sqlConnection);
				}
			}
			this._thresholdIndicator.ReportThresholdIndication(threshold);
			return threshold.Id;
		}

		// Token: 0x060004FD RID: 1277 RVA: 0x0001FF2C File Offset: 0x0001E12C
		public void UpdateThresholds()
		{
			ThresholdProcessingEngine._log.Debug("UpdateThresholds method enter.");
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				Dictionary<int, string> thresholdsNamesForRecalculation = ThresholdProcessingEngine.GetThresholdsNamesForRecalculation(sqlConnection);
				Stopwatch stopwatch = new Stopwatch();
				if (thresholdsNamesForRecalculation.Count > 0)
				{
					int defaultTimeFrameId = this.GetDefaultTimeFrameId(sqlConnection, this.BaselineTimeFrame);
					DateTime utcNow = DateTime.UtcNow;
					DateTime dateTime = utcNow.AddDays((double)(-(double)this.BaselineCollectionDuration));
					int num = 0;
					stopwatch.Start();
					ThresholdProcessingEngine._log.InfoFormat("Update thresholds started with parameters: applyDate - '{0}', minBaselineDate - '{1}'.", utcNow, dateTime);
					foreach (KeyValuePair<int, string> thresholdNameKvp in thresholdsNamesForRecalculation)
					{
						ThresholdProcessingEngine._log.DebugFormat("Processing of threshold name '{0}' started.", thresholdNameKvp.Value);
						ThresholdDataProvider thresholdDataProvider = this.GetThresholdDataProvider(thresholdNameKvp.Value, false);
						if (thresholdDataProvider == null)
						{
							ThresholdProcessingEngine._log.ErrorFormat("Threshold data provider for threshold name '{0}' not found.", thresholdNameKvp.Value);
						}
						else
						{
							IThresholdDataProcessor thresholdDataProcessor = this.GetThresholdDataProcessor(thresholdNameKvp.Value, false);
							if (thresholdDataProcessor == null)
							{
								ThresholdProcessingEngine._log.ErrorFormat("Threshold data processor for threshold name '{0}' not found.", thresholdNameKvp.Value);
							}
							else
							{
								for (;;)
								{
									ThresholdProcessingEngine._log.Debug("Threshold calculations started.");
									IList<BaselineProcessingInfo> baselineProcessingInfo = this.GetBaselineProcessingInfo(sqlConnection, thresholdDataProvider, thresholdNameKvp, defaultTimeFrameId, utcNow, dateTime, this.BatchSize);
									if (baselineProcessingInfo.Count == 0)
									{
										break;
									}
									foreach (BaselineProcessingInfo processingInfo in baselineProcessingInfo)
									{
										this.ComputeThresholds(thresholdDataProcessor, thresholdDataProvider, processingInfo, utcNow);
									}
									ThresholdProcessingEngine._log.Debug("Threshold calculations finished.");
									int num2 = this.UpdateThresholds(sqlConnection, baselineProcessingInfo, utcNow);
									num += num2;
								}
								this.UpdateRecalculationNeededFlag(sqlConnection, thresholdNameKvp.Key);
								ThresholdProcessingEngine._log.DebugFormat("Processing of threshold name '{0}' finished.", thresholdNameKvp.Value);
							}
						}
					}
					stopwatch.Stop();
					ThresholdProcessingEngine._log.InfoFormat("Update thresholds processed {0} rows and finished in {1} ms.", num, stopwatch.ElapsedMilliseconds);
				}
			}
			ThresholdProcessingEngine._log.Debug("UpdateThresholds method exit.");
		}

		// Token: 0x060004FE RID: 1278 RVA: 0x0002019C File Offset: 0x0001E39C
		public StatisticalDataHistogram[] GetHistogramForStatisticalData(string thresholdName, int instanceId)
		{
			if (thresholdName == null)
			{
				throw new ArgumentNullException("thresholdName");
			}
			ThresholdDataProvider thresholdDataProvider = this.GetThresholdDataProvider(thresholdName, true);
			StatisticalTableMetadata statisticalTableMetadata = thresholdDataProvider.GetStatisticalTableMetadata(thresholdName);
			DateTime minDateTimeInUtc;
			DateTime maxDateTimeInUtc;
			this.GetHistogramDateBorders(statisticalTableMetadata, instanceId, out minDateTimeInUtc, out maxDateTimeInUtc);
			StatisticalData[] statisticalData = thresholdDataProvider.GetStatisticalData(thresholdName, instanceId, minDateTimeInUtc, maxDateTimeInUtc);
			TimeFrame[] timeFrames = TimeFramesDAL.GetCoreTimeFrames(null).ToArray();
			ThresholdMinMaxValue thresholdMinMaxValues = thresholdDataProvider.GetThresholdMinMaxValues(thresholdName, instanceId);
			return new HistogramCalculator().CreateHistogramWithScaledInterval(statisticalData, timeFrames, BusinessLayerSettings.Instance.ThresholdsHistogramChartIntervalsCount, thresholdMinMaxValues.DataType);
		}

		// Token: 0x060004FF RID: 1279 RVA: 0x00020212 File Offset: 0x0001E412
		public string GetThresholdInstanceName(string thresholdName, int instanceId)
		{
			if (string.IsNullOrEmpty(thresholdName))
			{
				throw new ArgumentNullException("thresholdName");
			}
			return this.GetThresholdDataProvider(thresholdName, true).GetThresholdInstanceName(thresholdName, instanceId);
		}

		// Token: 0x06000500 RID: 1280 RVA: 0x00020236 File Offset: 0x0001E436
		public string GetStatisticalDataChartName(string thresholdName)
		{
			if (thresholdName == null)
			{
				throw new ArgumentNullException("thresholdName");
			}
			return this.GetThresholdDataProvider(thresholdName, true).GetStatisticalDataChartName(thresholdName);
		}

		// Token: 0x06000501 RID: 1281 RVA: 0x00020254 File Offset: 0x0001E454
		private void GetHistogramDateBorders(StatisticalTableMetadata metadata, int instanceId, out DateTime startDay, out DateTime endDay)
		{
			startDay = DateTime.UtcNow;
			endDay = DateTime.UtcNow.AddDays((double)(-1 * this.BaselineCollectionDuration));
			string text = string.Format("SELECT {0},{1} \r\n                                         FROM {2} stats \r\n                                         LEFT JOIN TimeFrames t ON t.TimeFrameID = stats.TimeFrameID \r\n                                         WHERE stats.{3} = @instanceId\r\n                                         AND t.Name = @timeFrameName", new object[]
			{
				metadata.MinDateTime,
				metadata.MaxDateTime,
				metadata.TableName,
				metadata.InstanceIdColumnName
			});
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				using (SqlCommand textCommand = SqlHelper.GetTextCommand(text))
				{
					textCommand.Parameters.AddWithValue("@instanceId", instanceId);
					textCommand.Parameters.AddWithValue("@timeFrameName", this.BaselineTimeFrame);
					using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand, sqlConnection))
					{
						if (dataReader.Read())
						{
							startDay = DatabaseFunctions.GetDateTime(dataReader, metadata.MinDateTime, DateTimeKind.Utc);
							endDay = DatabaseFunctions.GetDateTime(dataReader, metadata.MaxDateTime, DateTimeKind.Utc);
						}
					}
				}
			}
		}

		// Token: 0x06000502 RID: 1282 RVA: 0x00020380 File Offset: 0x0001E580
		private void ComputeThresholds(IThresholdDataProcessor thresholdDataProcessor, ThresholdDataProvider provider, BaselineProcessingInfo processingInfo, DateTime applyDate)
		{
			Threshold threshold = processingInfo.Threshold;
			if (thresholdDataProcessor.IsBaselineValuesValid(processingInfo.BaselineValues))
			{
				try
				{
					int disabledValue = ThresholdsHelper.GetDisabledValue(threshold.ThresholdOperator);
					double warningThreshold = threshold.WarningEnabled ? thresholdDataProcessor.ComputeThreshold(threshold.WarningFormula, processingInfo.BaselineValues, ThresholdLevel.Warning, threshold.ThresholdOperator) : ((double)disabledValue);
					double criticalThreshold = threshold.CriticalEnabled ? thresholdDataProcessor.ComputeThreshold(threshold.CriticalFormula, processingInfo.BaselineValues, ThresholdLevel.Critical, threshold.ThresholdOperator) : ((double)disabledValue);
					ThresholdMinMaxValue thresholdMinMaxValues = provider.GetThresholdMinMaxValues(threshold.ThresholdName, threshold.InstanceId);
					ThresholdComputationResult thresholdComputationResult = this.ProcessThresholds(threshold.WarningEnabled, warningThreshold, threshold.CriticalEnabled, criticalThreshold, threshold.ThresholdOperator, thresholdMinMaxValues);
					if (thresholdComputationResult.IsSuccess)
					{
						threshold.Warning = new double?(thresholdComputationResult.Warning);
						threshold.Critical = new double?(thresholdComputationResult.Critical);
						threshold.BaselineApplyError = null;
					}
					else
					{
						threshold.Warning = null;
						threshold.Critical = null;
						threshold.BaselineApplyError = thresholdComputationResult.Message;
					}
					return;
				}
				catch (Exception ex)
				{
					ThresholdProcessingEngine._log.ErrorFormat("Cannot compute thresholds for baseline values [{0}] and threshold [{1}]. Exception: {2}", processingInfo.BaselineValues, threshold, ex);
					return;
				}
			}
			ThresholdProcessingEngine._log.VerboseFormat("Baseline values [{0}] are not valid for threshold [{1}]", new object[]
			{
				processingInfo.BaselineValues,
				threshold
			});
			if (processingInfo.BaselineValues.MinDateTime == null)
			{
				processingInfo.BaselineValues.MinDateTime = new DateTime?(applyDate);
			}
			if (processingInfo.BaselineValues.MaxDateTime == null)
			{
				processingInfo.BaselineValues.MaxDateTime = new DateTime?(applyDate);
			}
			threshold.BaselineApplyError = Resources.LIBCODE_PF0_6;
		}

		// Token: 0x06000503 RID: 1283 RVA: 0x00020540 File Offset: 0x0001E740
		public ThresholdComputationResult ProcessThresholds(double warningThreshold, double criticalThreshold, ThresholdOperatorEnum oper, ThresholdMinMaxValue minMaxValues)
		{
			return this.ProcessThresholds(true, warningThreshold, true, criticalThreshold, oper, minMaxValues);
		}

		// Token: 0x06000504 RID: 1284 RVA: 0x00020550 File Offset: 0x0001E750
		public ThresholdComputationResult ProcessThresholds(bool warningEnabled, double warningThreshold, bool criticalEnabled, double criticalThreshold, ThresholdOperatorEnum oper, ThresholdMinMaxValue minMaxValues)
		{
			double num;
			double num2;
			ThresholdProcessingEngine.RoundThresholdsValues(minMaxValues.Min, minMaxValues.Max, warningThreshold, criticalThreshold, out num, out num2);
			int disabledValue = ThresholdsHelper.GetDisabledValue(oper);
			ThresholdComputationResult thresholdComputationResult = new ThresholdComputationResult
			{
				Warning = (warningEnabled ? num : ((double)disabledValue)),
				Critical = (criticalEnabled ? num2 : ((double)disabledValue)),
				IsSuccess = false,
				IsValid = false,
				Message = null
			};
			bool flag = warningEnabled && criticalEnabled;
			if (flag && warningThreshold == criticalThreshold)
			{
				thresholdComputationResult.Message = Resources.LIBCODE_PF0_1;
			}
			else if ((warningEnabled && warningThreshold < minMaxValues.Min) || (criticalEnabled && criticalThreshold < minMaxValues.Min))
			{
				thresholdComputationResult.IsValid = true;
				thresholdComputationResult.Message = Resources.LIBCODE_PF0_2;
			}
			else if ((warningEnabled && warningThreshold > minMaxValues.Max) || (criticalEnabled && criticalThreshold > minMaxValues.Max))
			{
				thresholdComputationResult.IsValid = true;
				thresholdComputationResult.Message = Resources.LIBCODE_PF0_3;
			}
			else if (flag && warningThreshold > criticalThreshold && (oper.Equals(ThresholdOperatorEnum.Greater) || oper.Equals(ThresholdOperatorEnum.GreaterOrEqual)))
			{
				thresholdComputationResult.Message = Resources.LIBCODE_PF0_4;
			}
			else if (flag && warningThreshold < criticalThreshold && (oper.Equals(ThresholdOperatorEnum.Less) || oper.Equals(ThresholdOperatorEnum.LessOrEqual)))
			{
				thresholdComputationResult.Message = Resources.LIBCODE_PF0_5;
			}
			else
			{
				thresholdComputationResult.IsSuccess = true;
				thresholdComputationResult.IsValid = true;
			}
			return thresholdComputationResult;
		}

		// Token: 0x06000505 RID: 1285 RVA: 0x000206C7 File Offset: 0x0001E8C7
		private static void RoundThresholdsValues(double min, double max, double warning, double critical, out double warningRounded, out double criticalRounded)
		{
			ThresholdsHelper.RoundThresholdsValues(min, max, warning, critical, out warningRounded, out criticalRounded);
		}

		// Token: 0x06000506 RID: 1286 RVA: 0x000206D8 File Offset: 0x0001E8D8
		private static Dictionary<int, string> GetThresholdsNamesForRecalculation(SqlConnection connection)
		{
			Dictionary<int, string> dictionary = new Dictionary<int, string>(100);
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT Id, \r\n                         Name\r\n                    FROM dbo.ThresholdsNames\r\n                   WHERE RecalculationNeeded = 1"))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand, connection))
				{
					while (dataReader.Read())
					{
						dictionary.Add((int)dataReader["Id"], dataReader["Name"].ToString());
					}
				}
			}
			return dictionary;
		}

		// Token: 0x06000507 RID: 1287 RVA: 0x00020764 File Offset: 0x0001E964
		private int GetDefaultTimeFrameId(SqlConnection connection, string timeFrameName)
		{
			int value;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT TimeFrameID\r\n                    FROM dbo.TimeFrames\r\n                   WHERE Name = @timeFrameName\r\n                     AND IsDisabled = 0"))
			{
				textCommand.Parameters.AddWithValue("timeFrameName", timeFrameName);
				int? num = SqlHelper.ExecuteScalar(textCommand, connection) as int?;
				if (num == null)
				{
					throw new InvalidOperationException(string.Format("Cannot find time frame with name '{0}'", timeFrameName));
				}
				value = num.Value;
			}
			return value;
		}

		// Token: 0x06000508 RID: 1288 RVA: 0x000207E0 File Offset: 0x0001E9E0
		private IList<BaselineProcessingInfo> GetBaselineProcessingInfo(SqlConnection connection, ThresholdDataProvider provider, KeyValuePair<int, string> thresholdNameKvp, int timeFrameId, DateTime currentApplyDate, DateTime minBaselineDate, int batchSize)
		{
			StatisticalTableMetadata statisticalTableMetadata = provider.GetStatisticalTableMetadata(thresholdNameKvp.Value);
			string text = ThresholdProcessingEngine.CreateBaselineInfoQuey(provider, statisticalTableMetadata, batchSize);
			List<BaselineProcessingInfo> list = new List<BaselineProcessingInfo>(batchSize);
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			ThresholdProcessingEngine._log.DebugFormat("GetBaselineProcessingInfo started for '{0}' with batch size {1}.", thresholdNameKvp.Value, batchSize);
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(text))
			{
				textCommand.Parameters.AddWithValue("currentApplyDate", currentApplyDate);
				textCommand.Parameters.AddWithValue("thresholdNameId", thresholdNameKvp.Key);
				textCommand.Parameters.AddWithValue("timeFrameId", timeFrameId);
				if (!string.IsNullOrEmpty(statisticalTableMetadata.MinDateTime))
				{
					textCommand.Parameters.AddWithValue("minDateTime", minBaselineDate);
				}
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand, connection))
				{
					while (dataReader.Read())
					{
						Threshold threshold = new Threshold
						{
							Id = (int)dataReader["Id"],
							InstanceId = (int)dataReader["InstanceId"],
							ThresholdOperator = (ThresholdOperatorEnum)dataReader["ThresholdOperator"],
							WarningFormula = dataReader["WarningFormula"].ToString(),
							CriticalFormula = dataReader["CriticalFormula"].ToString(),
							ThresholdName = thresholdNameKvp.Value,
							WarningPolls = new int?((int)dataReader["WarningPolls"]),
							WarningPollsInterval = new int?((int)dataReader["WarningPollsInterval"]),
							CriticalPolls = new int?((int)dataReader["CriticalPolls"]),
							CriticalPollsInterval = new int?((int)dataReader["CriticalPollsInterval"]),
							WarningEnabled = (bool)dataReader["WarningEnabled"],
							CriticalEnabled = (bool)dataReader["CriticalEnabled"]
						};
						BaselineValues baselineValues = provider.CreateBaselineValuesFromReader(dataReader);
						list.Add(new BaselineProcessingInfo(threshold, baselineValues));
					}
				}
			}
			stopwatch.Stop();
			ThresholdProcessingEngine._log.DebugFormat("GetBaselineProcessingInfo finished in {0} ms. Number of selected rows {1}", stopwatch.ElapsedMilliseconds, list.Count);
			return list;
		}

		// Token: 0x06000509 RID: 1289 RVA: 0x00020A80 File Offset: 0x0001EC80
		private static string CreateBaselineInfoQuey(ThresholdDataProvider provider, StatisticalTableMetadata metadata, int batchSize)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendFormat("SELECT TOP {0}\r\n                         t.Id\r\n                        ,t.InstanceId\r\n                        ,t.ThresholdOperator\r\n                        ,t.WarningFormula\r\n                        ,t.CriticalFormula\r\n                        ,t.WarningPolls\r\n                        ,t.WarningPollsInterval\r\n                        ,t.CriticalPolls\r\n                        ,t.CriticalPollsInterval\r\n                        ,t.WarningEnabled\r\n                        ,t.CriticalEnabled", batchSize);
			stringBuilder.AppendLine();
			string value = provider.CreateProjectionFromMetadata(metadata);
			stringBuilder.AppendLine(value);
			string arg = string.Format("{0} stats ON stats.{1} = t.InstanceId", metadata.TableName, metadata.InstanceIdColumnName);
			string arg2 = (!string.IsNullOrEmpty(metadata.MinDateTime)) ? string.Format("AND stats.{0} <= @minDateTime", metadata.MinDateTime) : string.Empty;
			stringBuilder.AppendFormat("FROM dbo.Thresholds t\r\n                  JOIN {0}\r\n                  WHERE ((t.ThresholdType = 1 AND t.Warning IS NULL)\r\n                          OR ((t.ThresholdType = 2) AND (t.BaselineApplied IS NULL OR t.BaselineApplied < @currentApplyDate)))\r\n                  AND t.ThresholdNameId = @thresholdNameId\r\n                  AND stats.TimeFrameID = @timeFrameId\r\n                  {1}", arg, arg2);
			return stringBuilder.ToString();
		}

		// Token: 0x0600050A RID: 1290 RVA: 0x00020B0C File Offset: 0x0001ED0C
		private int UpdateThresholds(SqlConnection connection, ICollection<BaselineProcessingInfo> thresholdsToUpdate, DateTime applyDate)
		{
			if (!thresholdsToUpdate.Any<BaselineProcessingInfo>())
			{
				return 0;
			}
			string text = "IF OBJECT_ID('tempdb..#ThresholdsForUpdate') IS NULL\r\n                                        BEGIN\r\n\t                                        CREATE TABLE #ThresholdsForUpdate\r\n\t                                        (\r\n\t\t                                        ThresholdId INT,\r\n\t\t                                        Warning FLOAT,\r\n\t\t                                        Critical FLOAT,\r\n\t\t                                        MinDateTime DATETIME,\r\n\t\t                                        MaxDateTime DATETIME,\r\n\t\t                                        BaselineApplyError NVARCHAR(MAX),\r\n                                                WarningPolls INT,\r\n                                                WarningPollsInterval INT,\r\n                                                CriticalPolls INT,\r\n                                                CriticalPollsInterval INT,\r\n                                                WarningEnabled BIT,\r\n                                                CriticalEnabled BIT\r\n\t                                        )    \r\n                                        END\r\n                                        ELSE\r\n                                        BEGIN\r\n                                            TRUNCATE TABLE #ThresholdsForUpdate\r\n                                        END";
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			ThresholdProcessingEngine._log.DebugFormat("Bulk update thresholds operation started with {0} thresholds to update.", thresholdsToUpdate.Count);
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(text))
			{
				SqlHelper.ExecuteNonQuery(textCommand, connection);
			}
			using (IDataReader dataReader = new EnumerableDataReader<BaselineProcessingInfo>(new SinglePropertyAccessor<BaselineProcessingInfo>().AddColumn("ThresholdId", (BaselineProcessingInfo t) => t.Threshold.Id).AddColumn("Warning", (BaselineProcessingInfo t) => t.Threshold.Warning).AddColumn("Critical", (BaselineProcessingInfo t) => t.Threshold.Critical).AddColumn("MinDateTime", (BaselineProcessingInfo t) => t.BaselineValues.MinDateTime).AddColumn("MaxDateTime", (BaselineProcessingInfo t) => t.BaselineValues.MaxDateTime).AddColumn("BaselineApplyError", (BaselineProcessingInfo t) => t.Threshold.BaselineApplyError).AddColumn("WarningPolls", (BaselineProcessingInfo t) => t.Threshold.WarningPolls).AddColumn("WarningPollsInterval", (BaselineProcessingInfo t) => t.Threshold.WarningPollsInterval).AddColumn("CriticalPolls", (BaselineProcessingInfo t) => t.Threshold.CriticalPolls).AddColumn("CriticalPollsInterval", (BaselineProcessingInfo t) => t.Threshold.CriticalPollsInterval).AddColumn("WarningEnabled", (BaselineProcessingInfo t) => t.Threshold.WarningEnabled).AddColumn("CriticalEnabled", (BaselineProcessingInfo t) => t.Threshold.CriticalEnabled), thresholdsToUpdate))
			{
				SqlHelper.ExecuteBulkCopy("#ThresholdsForUpdate", dataReader, connection, null, SqlBulkCopyOptions.TableLock);
			}
			ThresholdProcessingEngine._log.DebugFormat("Bulk insert finished in {0} ms.", stopwatch.ElapsedMilliseconds);
			int result;
			using (SqlCommand textCommand2 = SqlHelper.GetTextCommand("UPDATE Thresholds\r\n                SET Thresholds.Warning = #ThresholdsForUpdate.Warning,\r\n                    Thresholds.Critical = #ThresholdsForUpdate.Critical,\r\n                    Thresholds.BaselineFrom = #ThresholdsForUpdate.MinDateTime,\r\n                    Thresholds.BaselineTo = #ThresholdsForUpdate.MaxDateTime,\r\n                    Thresholds.BaselineApplied = @applyDate,\r\n                    Thresholds.BaselineApplyError = #ThresholdsForUpdate.BaselineApplyError,\r\n                    Thresholds.WarningPolls = #ThresholdsForUpdate.WarningPolls,\r\n                    Thresholds.WarningPollsInterval = #ThresholdsForUpdate.WarningPollsInterval,\r\n                    Thresholds.CriticalPolls = #ThresholdsForUpdate.CriticalPolls,\r\n                    Thresholds.CriticalPollsInterval = #ThresholdsForUpdate.CriticalPollsInterval,\r\n                    Thresholds.WarningEnabled = #ThresholdsForUpdate.WarningEnabled,\r\n                    Thresholds.CriticalEnabled = #ThresholdsForUpdate.CriticalEnabled\r\n                FROM Thresholds\r\n                JOIN #ThresholdsForUpdate ON Thresholds.Id = #ThresholdsForUpdate.ThresholdId"))
			{
				textCommand2.Parameters.AddWithValue("applyDate", applyDate);
				int num = SqlHelper.ExecuteNonQuery(textCommand2, connection);
				stopwatch.Stop();
				ThresholdProcessingEngine._log.DebugFormat("Bulk update finished in {0} ms. Affected rows {1}.", stopwatch.ElapsedMilliseconds, num);
				result = num;
			}
			return result;
		}

		// Token: 0x0600050B RID: 1291 RVA: 0x00020E20 File Offset: 0x0001F020
		private void UpdateRecalculationNeededFlag(SqlConnection connection, int thresholdNameId)
		{
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("UPDATE dbo.ThresholdsNames \r\n                     SET RecalculationNeeded = 0\r\n                   WHERE Id = @id"))
			{
				textCommand.Parameters.AddWithValue("@id", thresholdNameId);
				int num = SqlHelper.ExecuteNonQuery(textCommand, connection);
				ThresholdProcessingEngine._log.DebugFormat("RecalculationNeeded flag updated for thresholdNameId {0}. Affected rows {1}.", thresholdNameId, num);
			}
		}

		// Token: 0x0600050C RID: 1292 RVA: 0x00020E90 File Offset: 0x0001F090
		private IThresholdDataProcessor GetThresholdDataProcessor(string thresholdName, bool throwsException)
		{
			IThresholdDataProcessor result;
			if (this._dataProcessorsDictionary.TryGetValue(thresholdName, out result))
			{
				return result;
			}
			string text = string.Format("Threshold processor for '{0}' was not found.", thresholdName);
			ThresholdProcessingEngine._log.Error(text);
			if (throwsException)
			{
				throw new InvalidOperationException(text);
			}
			return null;
		}

		// Token: 0x0600050D RID: 1293 RVA: 0x00020ED4 File Offset: 0x0001F0D4
		private ThresholdDataProvider GetThresholdDataProvider(string thresholdName, bool throwsException)
		{
			ThresholdDataProvider result;
			if (this._dataProvidersDictionary.TryGetValue(thresholdName, out result))
			{
				return result;
			}
			string text = string.Format("Threshold data provider for '{0}' was not found.", thresholdName);
			ThresholdProcessingEngine._log.Error(text);
			if (throwsException)
			{
				throw new InvalidOperationException(text);
			}
			return null;
		}

		// Token: 0x0400015A RID: 346
		private static readonly Log _log = new Log();

		// Token: 0x0400015B RID: 347
		private readonly IThresholdIndicator _thresholdIndicator;

		// Token: 0x0400015C RID: 348
		private readonly IThresholdDataProcessor[] _thresholdProcessors;

		// Token: 0x0400015D RID: 349
		private readonly ThresholdDataProvider[] _thresholdDataProviders;

		// Token: 0x0400015E RID: 350
		private readonly ICollectorSettings _settings;

		// Token: 0x0400015F RID: 351
		private readonly IDictionary<string, IThresholdDataProcessor> _dataProcessorsDictionary;

		// Token: 0x04000160 RID: 352
		private readonly IDictionary<string, ThresholdDataProvider> _dataProvidersDictionary;

		// Token: 0x04000161 RID: 353
		private int _batchSize = 10000;

		// Token: 0x04000162 RID: 354
		private string _baselineTimeFrame = "Core_All";
	}
}
