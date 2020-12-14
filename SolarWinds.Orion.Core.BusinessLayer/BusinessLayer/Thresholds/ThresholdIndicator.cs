using System;
using System.Collections.Generic;
using System.Data;
using SolarWinds.InformationService.Contract2;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Indications;
using SolarWinds.Orion.Core.Common.InformationService;
using SolarWinds.Orion.Core.Common.Models.Thresholds;

namespace SolarWinds.Orion.Core.BusinessLayer.Thresholds
{
	// Token: 0x02000053 RID: 83
	internal class ThresholdIndicator : IThresholdIndicator
	{
		// Token: 0x060004E9 RID: 1257 RVA: 0x0001EB94 File Offset: 0x0001CD94
		public ThresholdIndicator() : this(new InformationServiceProxyFactory(), IndicationPublisher.CreateV3())
		{
		}

		// Token: 0x060004EA RID: 1258 RVA: 0x0001EBA6 File Offset: 0x0001CDA6
		public ThresholdIndicator(IInformationServiceProxyFactory swisFactory, IndicationPublisher indicationReporter)
		{
			if (swisFactory == null)
			{
				throw new ArgumentNullException("swisFactory");
			}
			if (indicationReporter == null)
			{
				throw new ArgumentNullException("indicationReporter");
			}
			this._swisFactory = swisFactory;
			this._indicationPublisher = indicationReporter;
		}

		// Token: 0x060004EB RID: 1259 RVA: 0x0001EBD8 File Offset: 0x0001CDD8
		private ThresholdIndicator.InstanceInformation GetInstanceInformation(string entityType, int instanceId)
		{
			if (string.IsNullOrEmpty(entityType) || instanceId == 0)
			{
				return null;
			}
			ThresholdIndicator.InstanceInformation instanceInformation = new ThresholdIndicator.InstanceInformation();
			using (IInformationServiceProxy2 informationServiceProxy = this._swisFactory.CreateConnection())
			{
				DataTable dataTable = informationServiceProxy.Query("SELECT TOP 1 Prefix, KeyProperty, NameProperty FROM Orion.NetObjectTypes WHERE EntityType = @entityType", new Dictionary<string, object>
				{
					{
						"entityType",
						entityType
					}
				});
				if (dataTable != null && dataTable.Rows.Count == 1)
				{
					string arg = dataTable.Rows[0]["Prefix"] as string;
					object obj = dataTable.Rows[0]["KeyProperty"];
					object obj2 = dataTable.Rows[0]["NameProperty"];
					instanceInformation.NetObject = string.Format("{0}:{1}", arg, instanceId);
					if (obj != DBNull.Value && obj != DBNull.Value)
					{
						DataTable dataTable2 = informationServiceProxy.Query(string.Format("SELECT {0} FROM {1} WHERE {2} = @InstanceId", obj2, entityType, obj), new Dictionary<string, object>
						{
							{
								"InstanceId",
								instanceId
							}
						});
						if (dataTable2 != null && dataTable2.Rows.Count > 0)
						{
							instanceInformation.InstanceName = dataTable2.Rows[0][obj2.ToString()].ToString();
						}
						else
						{
							instanceInformation.InstanceName = instanceInformation.NetObject;
						}
					}
					else
					{
						instanceInformation.InstanceName = instanceInformation.NetObject;
					}
				}
			}
			return instanceInformation;
		}

		// Token: 0x060004EC RID: 1260 RVA: 0x0001ED58 File Offset: 0x0001CF58
		public void LoadPreviousThresholdData(int instanceId, string thresholdName)
		{
			using (IInformationServiceProxy2 informationServiceProxy = this._swisFactory.CreateConnection())
			{
				this._previousThresholdValues = informationServiceProxy.Query("SELECT OT.ThresholdOperator,\r\n                    OT.Level1Value,\r\n                    OT.Level1Formula,\r\n                    OT.Level2Value,\r\n                    OT.Level2Formula,\r\n                    OT.WarningPolls,\r\n                    OT.WarningPollsInterval,\r\n                    OT.CriticalPolls,\r\n                    OT.CriticalPollsInterval,\r\n                    OT.WarningEnabled,\r\n                    OT.CriticalEnabled\r\n                    FROM Orion.Thresholds OT\r\n                    WHERE OT.InstanceId = @InstanceId AND OT.Name = @Name", new Dictionary<string, object>
				{
					{
						"InstanceId",
						instanceId
					},
					{
						"Name",
						thresholdName
					}
				});
			}
		}

		// Token: 0x060004ED RID: 1261 RVA: 0x0001EDC0 File Offset: 0x0001CFC0
		private string GetThresholdEntityType(string thresholdName)
		{
			using (IInformationServiceProxy2 informationServiceProxy = this._swisFactory.CreateConnection())
			{
				DataTable dataTable = informationServiceProxy.Query("SELECT EntityType FROM Orion.Thresholds WHERE Name = @Name", new Dictionary<string, object>
				{
					{
						"Name",
						thresholdName
					}
				});
				if (dataTable != null && dataTable.Rows.Count > 0)
				{
					return dataTable.Rows[0]["EntityType"].ToString();
				}
			}
			return null;
		}

		// Token: 0x060004EE RID: 1262 RVA: 0x0001EE44 File Offset: 0x0001D044
		public void ReportThresholdIndication(Threshold threshold)
		{
			if (threshold == null)
			{
				throw new ArgumentNullException("threshold");
			}
			string thresholdEntityType = this.GetThresholdEntityType(threshold.ThresholdName);
			ThresholdIndicator.InstanceInformation instanceInformation = this.GetInstanceInformation(thresholdEntityType, threshold.InstanceId);
			PropertyBag propertyBag = new PropertyBag();
			propertyBag.Add("InstanceType", "Orion.Thresholds");
			propertyBag.Add("Name", threshold.ThresholdName);
			propertyBag.Add("InstanceName", (instanceInformation != null) ? instanceInformation.InstanceName : threshold.InstanceId.ToString());
			propertyBag.Add("InstanceId", threshold.InstanceId);
			propertyBag.Add("ThresholdType", (int)threshold.ThresholdType);
			propertyBag.Add("ThresholdOperator", (int)threshold.ThresholdOperator);
			propertyBag.Add("Level1Value", threshold.Warning);
			propertyBag.Add("Level2Value", threshold.Critical);
			propertyBag.Add("Level1Formula", threshold.WarningFormula);
			propertyBag.Add("Level2Formula", threshold.CriticalFormula);
			propertyBag.Add("WarningPolls", threshold.WarningPolls);
			propertyBag.Add("WarningPollsInterval", threshold.WarningPollsInterval);
			propertyBag.Add("CriticalPolls", threshold.CriticalPolls);
			propertyBag.Add("CriticalPollsInterval", threshold.CriticalPollsInterval);
			propertyBag.Add("WarningEnabled", threshold.WarningEnabled);
			propertyBag.Add("CriticalEnabled", threshold.CriticalEnabled);
			PropertyBag propertyBag2 = propertyBag;
			if (instanceInformation != null && !string.IsNullOrEmpty(instanceInformation.NetObject))
			{
				propertyBag2.Add("NetObject", instanceInformation.NetObject);
			}
			if (this._previousThresholdValues != null && this._previousThresholdValues.Rows.Count > 0)
			{
				PropertyBag propertyBag3 = new PropertyBag();
				object obj = this._previousThresholdValues.Rows[0]["ThresholdOperator"];
				object obj2 = this._previousThresholdValues.Rows[0]["Level1Value"];
				object obj3 = this._previousThresholdValues.Rows[0]["Level2Value"];
				object obj4 = this._previousThresholdValues.Rows[0]["Level1Formula"];
				object obj5 = this._previousThresholdValues.Rows[0]["Level2Formula"];
				object obj6 = this._previousThresholdValues.Rows[0]["WarningPolls"];
				object obj7 = this._previousThresholdValues.Rows[0]["WarningPollsInterval"];
				object obj8 = this._previousThresholdValues.Rows[0]["CriticalPolls"];
				object obj9 = this._previousThresholdValues.Rows[0]["CriticalPollsInterval"];
				object obj10 = this._previousThresholdValues.Rows[0]["WarningEnabled"];
				object obj11 = this._previousThresholdValues.Rows[0]["CriticalEnabled"];
				propertyBag3.Add("ThresholdOperator", (obj != DBNull.Value) ? obj : null);
				propertyBag3.Add("Level1Value", (obj2 != DBNull.Value) ? obj2 : null);
				propertyBag3.Add("Level2Value", (obj3 != DBNull.Value) ? obj3 : null);
				propertyBag3.Add("Level1Formula", (obj4 != DBNull.Value) ? obj4 : null);
				propertyBag3.Add("Level2Formula", (obj5 != DBNull.Value) ? obj5 : null);
				propertyBag3.Add("WarningPolls", (obj6 != DBNull.Value) ? obj6 : null);
				propertyBag3.Add("WarningPollsInterval", (obj7 != DBNull.Value) ? obj7 : null);
				propertyBag3.Add("CriticalPolls", (obj8 != DBNull.Value) ? obj8 : null);
				propertyBag3.Add("CriticalPollsInterval", (obj9 != DBNull.Value) ? obj9 : null);
				propertyBag3.Add("WarningEnabled", (obj10 != DBNull.Value) ? obj10 : null);
				propertyBag3.Add("CriticalEnabled", (obj11 != DBNull.Value) ? obj11 : null);
				if (propertyBag3.Count > 0)
				{
					propertyBag2.Add("PreviousProperties", propertyBag3);
				}
				this._previousThresholdValues.Clear();
			}
			this._indicationPublisher.ReportIndication(new ThresholdIndication(2, propertyBag2));
		}

		// Token: 0x04000157 RID: 343
		private readonly IInformationServiceProxyFactory _swisFactory;

		// Token: 0x04000158 RID: 344
		private readonly IndicationPublisher _indicationPublisher;

		// Token: 0x04000159 RID: 345
		private DataTable _previousThresholdValues;

		// Token: 0x02000155 RID: 341
		public class InstanceInformation
		{
			// Token: 0x17000146 RID: 326
			// (get) Token: 0x06000B73 RID: 2931 RVA: 0x00049089 File Offset: 0x00047289
			// (set) Token: 0x06000B74 RID: 2932 RVA: 0x00049091 File Offset: 0x00047291
			public string NetObject { get; set; }

			// Token: 0x17000147 RID: 327
			// (get) Token: 0x06000B75 RID: 2933 RVA: 0x0004909A File Offset: 0x0004729A
			// (set) Token: 0x06000B76 RID: 2934 RVA: 0x000490A2 File Offset: 0x000472A2
			public string InstanceName { get; set; }
		}
	}
}
