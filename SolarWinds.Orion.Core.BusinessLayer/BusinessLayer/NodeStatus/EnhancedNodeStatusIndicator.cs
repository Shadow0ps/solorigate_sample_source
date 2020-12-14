using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Serialization;
using SolarWinds.InformationService.Contract2;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Indications;

namespace SolarWinds.Orion.Core.BusinessLayer.NodeStatus
{
	// Token: 0x0200006F RID: 111
	public class EnhancedNodeStatusIndicator
	{
		// Token: 0x060005CE RID: 1486 RVA: 0x00022BA4 File Offset: 0x00020DA4
		public EnhancedNodeStatusIndicator(ISqlHelper sqlHelper, IIndicationReporterPublisher ip)
		{
			this.sqlHelper = sqlHelper;
			this.ip = ip;
		}

		// Token: 0x060005CF RID: 1487 RVA: 0x00022BBA File Offset: 0x00020DBA
		public void Execute()
		{
			this.ProcessIndications(this.ReadFromDB());
		}

		// Token: 0x060005D0 RID: 1488 RVA: 0x00022BC8 File Offset: 0x00020DC8
		private List<EnhancedNodeStatusIndicator.IndicationInfo> ReadFromDB()
		{
			List<EnhancedNodeStatusIndicator.IndicationInfo> list = new List<EnhancedNodeStatusIndicator.IndicationInfo>();
			using (SqlCommand textCommand = this.sqlHelper.GetTextCommand(EnhancedNodeStatusIndicator.SelectNodeStatusQuery))
			{
				using (IDataReader dataReader = this.sqlHelper.ExecuteReader(textCommand))
				{
					try
					{
						while (dataReader.Read())
						{
							int @int = dataReader.GetInt32(0);
							string @string = dataReader.GetString(1);
							list.Add(new EnhancedNodeStatusIndicator.IndicationInfo
							{
								Id = @int,
								Data = @string
							});
							EnhancedNodeStatusIndicator.log.Debug(string.Format("Reading new indication info from DB ({0},{1})", @int, @string));
						}
					}
					catch (Exception ex)
					{
						EnhancedNodeStatusIndicator.log.Warn("Reading indication data failed", ex);
					}
				}
			}
			return list;
		}

		// Token: 0x060005D1 RID: 1489 RVA: 0x00022CA0 File Offset: 0x00020EA0
		private void DeleteIndicationFromDB(EnhancedNodeStatusIndicator.IndicationInfo record)
		{
			try
			{
				SqlCommand textCommand = this.sqlHelper.GetTextCommand(EnhancedNodeStatusIndicator.DeleteNodeStatusQuery);
				textCommand.Parameters.Add(new SqlParameter("id", record.Id));
				this.sqlHelper.ExecuteNonQuery(textCommand);
			}
			catch (Exception ex)
			{
				EnhancedNodeStatusIndicator.log.Error("Deleting from indication table failed", ex);
			}
		}

		// Token: 0x060005D2 RID: 1490 RVA: 0x00022D14 File Offset: 0x00020F14
		internal void ProcessIndications(IEnumerable<EnhancedNodeStatusIndicator.IndicationInfo> indications)
		{
			foreach (EnhancedNodeStatusIndicator.IndicationInfo indicationInfo in indications)
			{
				try
				{
					EnhancedNodeStatusIndicator.NodeStatusIndication nodeStatusIndication = (EnhancedNodeStatusIndicator.NodeStatusIndication)OrionSerializationHelper.FromJSON(indicationInfo.Data, typeof(EnhancedNodeStatusIndicator.NodeStatusIndication));
					PropertyBag propertyBag = new PropertyBag();
					propertyBag["PreviousStatus"] = nodeStatusIndication.PreviousStatus;
					PropertyBag propertyBag2 = new PropertyBag();
					propertyBag2["NodeID"] = nodeStatusIndication.NodeID;
					propertyBag2["Status"] = nodeStatusIndication.Status;
					propertyBag2["InstanceType"] = "Orion.Nodes";
					propertyBag2["PreviousProperties"] = propertyBag;
					this.ip.ReportIndication(IndicationHelper.GetIndicationType(2), IndicationHelper.GetIndicationProperties(), propertyBag2);
					this.DeleteIndicationFromDB(indicationInfo);
					EnhancedNodeStatusIndicator.log.Debug("Enhanced node status indication processed " + string.Format("(N:{0} [{1}]->[{2}])", nodeStatusIndication.NodeID, nodeStatusIndication.PreviousStatus, nodeStatusIndication.Status));
				}
				catch (Exception ex)
				{
					EnhancedNodeStatusIndicator.log.Error("Indication processing failed", ex);
				}
			}
		}

		// Token: 0x040001B9 RID: 441
		private static string SelectNodeStatusQuery = "SELECT TOP 1000 [ID], [Data]FROM [DatabaseIndicationQueue] WHERE [Owner] = 'Core.Status' ORDER BY ID";

		// Token: 0x040001BA RID: 442
		private static string DeleteNodeStatusQuery = "DELETE FROM [DatabaseIndicationQueue] WHERE ID=@id";

		// Token: 0x040001BB RID: 443
		private static readonly Log log = new Log();

		// Token: 0x040001BC RID: 444
		private readonly ISqlHelper sqlHelper;

		// Token: 0x040001BD RID: 445
		private readonly IIndicationReporterPublisher ip;

		// Token: 0x0200015D RID: 349
		internal class IndicationInfo
		{
			// Token: 0x1700014B RID: 331
			// (get) Token: 0x06000B9B RID: 2971 RVA: 0x0004943E File Offset: 0x0004763E
			// (set) Token: 0x06000B9C RID: 2972 RVA: 0x00049446 File Offset: 0x00047646
			public int Id { get; set; }

			// Token: 0x1700014C RID: 332
			// (get) Token: 0x06000B9D RID: 2973 RVA: 0x0004944F File Offset: 0x0004764F
			// (set) Token: 0x06000B9E RID: 2974 RVA: 0x00049457 File Offset: 0x00047657
			public string Data { get; set; }
		}

		// Token: 0x0200015E RID: 350
		[DataContract]
		internal class NodeStatusIndication
		{
			// Token: 0x1700014D RID: 333
			// (get) Token: 0x06000BA0 RID: 2976 RVA: 0x00049460 File Offset: 0x00047660
			// (set) Token: 0x06000BA1 RID: 2977 RVA: 0x00049468 File Offset: 0x00047668
			[DataMember]
			public int NodeID { get; set; }

			// Token: 0x1700014E RID: 334
			// (get) Token: 0x06000BA2 RID: 2978 RVA: 0x00049471 File Offset: 0x00047671
			// (set) Token: 0x06000BA3 RID: 2979 RVA: 0x00049479 File Offset: 0x00047679
			[DataMember]
			public int Status { get; set; }

			// Token: 0x1700014F RID: 335
			// (get) Token: 0x06000BA4 RID: 2980 RVA: 0x00049482 File Offset: 0x00047682
			// (set) Token: 0x06000BA5 RID: 2981 RVA: 0x0004948A File Offset: 0x0004768A
			[DataMember]
			public int PreviousStatus { get; set; }
		}
	}
}
