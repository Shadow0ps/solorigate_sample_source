using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using SolarWinds.InformationService.Contract2;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.MacroProcessor;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x020000AC RID: 172
	public class VolumeDAL
	{
		// Token: 0x06000873 RID: 2163 RVA: 0x0003C824 File Offset: 0x0003AA24
		private static SqlCommand LoadCommandParams(Volume volume, SqlCommand command, bool includeID)
		{
			if (VolumeDAL.log.IsDebugEnabled)
			{
				VolumeDAL.log.DebugFormat("Loading command parameters for volume {0}", volume);
			}
			command.Parameters.Add("@NodeID", SqlDbType.Int, 100).Value = volume.NodeID;
			command.Parameters.Add("@VolumeIndex", SqlDbType.Int, 4).Value = volume.VolumeIndex;
			command.Parameters.Add("@Caption", SqlDbType.NVarChar, 75).Value = volume.Caption;
			command.Parameters.Add("@PollInterval", SqlDbType.Int, 4).Value = volume.PollInterval;
			command.Parameters.Add("@StatCollection", SqlDbType.Int, 4).Value = volume.StatCollection;
			command.Parameters.Add("@RediscoveryInterval", SqlDbType.Int, 4).Value = volume.RediscoveryInterval;
			command.Parameters.Add("@VolumeDescription", SqlDbType.NVarChar, 512).Value = volume.VolumeDescription;
			command.Parameters.Add("@VolumeTypeID", SqlDbType.Int, 4).Value = volume.VolumeTypeID;
			command.Parameters.Add("@VolumeType", SqlDbType.NVarChar, 40).Value = volume.VolumeType;
			command.Parameters.Add("@VolumeTypeIcon", SqlDbType.VarChar, 20).Value = volume.VolumeTypeIcon;
			command.Parameters.Add("@VolumePercentUsed", SqlDbType.Real).Value = volume.VolumePercentUsed;
			command.Parameters.Add("@VolumeSpaceUsed", SqlDbType.Float).Value = volume.VolumeSpaceUsed;
			command.Parameters.Add("@VolumeSpaceAvailable", SqlDbType.Float, 4).Value = volume.VolumeSpaceAvailable;
			command.Parameters.Add("@VolumeSize", SqlDbType.Float, 4).Value = volume.VolumeSize;
			command.Parameters.Add("@Status", SqlDbType.Int, 4).Value = volume.Status;
			command.Parameters.Add("@StatusLED", SqlDbType.VarChar, 20).Value = volume.StatusLED;
			command.Parameters.Add("@VolumeResponding", SqlDbType.Char, 1).Value = (volume.VolumeResponding ? 'Y' : 'N');
			command.Parameters.Add("@VolumeAllocationFailuresThisHour", SqlDbType.Int, 4).Value = volume.VolumeAllocationFailuresThisHour;
			command.Parameters.Add("@VolumeAllocationFailuresToday", SqlDbType.Int, 4).Value = volume.VolumeAllocationFailuresToday;
			command.Parameters.Add("@NextPoll", SqlDbType.DateTime).Value = volume.NextPoll;
			command.Parameters.Add("@NextRediscovery", SqlDbType.DateTime).Value = volume.NextRediscovery;
			command.Parameters.Add("@FullName", SqlDbType.NVarChar, 255).Value = volume.FullName;
			DbParameter dbParameter = command.Parameters.Add("@DiskQueueLength", SqlDbType.Float, 4);
			double? num = volume.DiskQueueLength;
			dbParameter.Value = ((num != null) ? num.GetValueOrDefault() : DBNull.Value);
			DbParameter dbParameter2 = command.Parameters.Add("@DiskTransfer", SqlDbType.Float, 4);
			num = volume.DiskTransfer;
			dbParameter2.Value = ((num != null) ? num.GetValueOrDefault() : DBNull.Value);
			DbParameter dbParameter3 = command.Parameters.Add("@DiskReads", SqlDbType.Float, 4);
			num = volume.DiskReads;
			dbParameter3.Value = ((num != null) ? num.GetValueOrDefault() : DBNull.Value);
			DbParameter dbParameter4 = command.Parameters.Add("@DiskWrites", SqlDbType.Float, 4);
			num = volume.DiskWrites;
			dbParameter4.Value = ((num != null) ? num.GetValueOrDefault() : DBNull.Value);
			DbParameter dbParameter5 = command.Parameters.Add("@TotalDiskIOPS", SqlDbType.Float, 4);
			num = volume.TotalDiskIOPS;
			dbParameter5.Value = ((num != null) ? num.GetValueOrDefault() : DBNull.Value);
			command.Parameters.Add("@DeviceId", SqlDbType.NVarChar, 512).Value = ((!string.IsNullOrWhiteSpace(volume.DeviceId)) ? volume.DeviceId : DBNull.Value);
			command.Parameters.Add("@DiskSerialNumber", SqlDbType.NVarChar, 255).Value = ((!string.IsNullOrWhiteSpace(volume.DiskSerialNumber)) ? volume.DiskSerialNumber : DBNull.Value);
			command.Parameters.Add("@InterfaceType", SqlDbType.NVarChar, 20).Value = ((!string.IsNullOrWhiteSpace(volume.InterfaceType)) ? volume.InterfaceType : DBNull.Value);
			DbParameter dbParameter6 = command.Parameters.Add("@SCSITargetId", SqlDbType.Int, 4);
			int? num2 = volume.SCSITargetId;
			dbParameter6.Value = ((num2 != null) ? num2.GetValueOrDefault() : DBNull.Value);
			DbParameter dbParameter7 = command.Parameters.Add("@SCSIPortId", SqlDbType.Int, 4);
			num2 = volume.SCSIPortId;
			dbParameter7.Value = ((num2 != null) ? num2.GetValueOrDefault() : DBNull.Value);
			DbParameter dbParameter8 = command.Parameters.Add("@SCSILunId", SqlDbType.Int, 4);
			num2 = volume.SCSILunId;
			dbParameter8.Value = ((num2 != null) ? num2.GetValueOrDefault() : DBNull.Value);
			command.Parameters.Add("@SCSIControllerId", SqlDbType.NVarChar, 255).Value = ((!string.IsNullOrWhiteSpace(volume.SCSIControllerId)) ? volume.SCSIControllerId : DBNull.Value);
			DbParameter dbParameter9 = command.Parameters.Add("@SCSIPortOffset", SqlDbType.Int, 4);
			num2 = volume.SCSIPortOffset;
			dbParameter9.Value = ((num2 != null) ? num2.GetValueOrDefault() : DBNull.Value);
			if (volume.LastSync == DateTime.MinValue)
			{
				command.Parameters.Add("@LastSync", SqlDbType.DateTime).Value = DBNull.Value;
			}
			else
			{
				command.Parameters.Add("@LastSync", SqlDbType.DateTime).Value = volume.LastSync;
			}
			if (includeID)
			{
				command.Parameters.AddWithValue("VolumeID", volume.ID);
			}
			return command;
		}

		// Token: 0x06000874 RID: 2164 RVA: 0x0003CE84 File Offset: 0x0003B084
		public static void DeleteVolume(Volume volume)
		{
			SqlCommand sqlCommand = SqlHelper.GetTextCommand("swsp_DeleteVolume");
			sqlCommand.CommandType = CommandType.StoredProcedure;
			sqlCommand.Parameters.Add("@id", SqlDbType.Int).Value = volume.ID;
			SqlHelper.ExecuteNonQuery(sqlCommand);
			SqlCommand textCommand;
			sqlCommand = (textCommand = SqlHelper.GetTextCommand("Delete FROM Pollers WHERE NetObject = @NetObject"));
			try
			{
				sqlCommand.Parameters.Add("@NetObject", SqlDbType.VarChar, 50).Value = "V:" + volume.ID;
				SqlHelper.ExecuteNonQuery(sqlCommand);
			}
			finally
			{
				if (textCommand != null)
				{
					((IDisposable)textCommand).Dispose();
				}
			}
		}

		// Token: 0x06000875 RID: 2165 RVA: 0x0003CF2C File Offset: 0x0003B12C
		public static int GetVolumeCount()
		{
			int result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT COUNT(*) FROM Volumes"))
			{
				result = (int)SqlHelper.ExecuteScalar(textCommand);
			}
			return result;
		}

		// Token: 0x06000876 RID: 2166 RVA: 0x0003CF70 File Offset: 0x0003B170
		public static int InsertVolume(Volume volume)
		{
			SqlCommand textCommand = SqlHelper.GetTextCommand("\r\nIF NOT EXISTS (SELECT * FROM Volumes WHERE NodeID = @NodeID AND VolumeDescription = @VolumeDescription)\r\nBEGIN \r\n    INSERT INTO Volumes\r\n        ([NodeID]\r\n        ,[LastSync]\r\n        ,[VolumeIndex]\r\n        ,[Caption]\r\n        ,[PollInterval]\r\n        ,[StatCollection]\r\n        ,[RediscoveryInterval]\r\n        ,[VolumeDescription]\r\n        ,[VolumeTypeID]\r\n        ,[VolumeType]\r\n        ,[VolumeTypeIcon]\r\n        ,[VolumePercentUsed]\r\n        ,[VolumeSpaceUsed]\r\n        ,[VolumeSpaceAvailable]\r\n        ,[VolumeSize]\r\n        ,[Status]\r\n        ,[StatusLED]\r\n        ,[VolumeResponding]\r\n        ,[VolumeAllocationFailuresThisHour]\r\n        ,[VolumeAllocationFailuresToday]\r\n        ,[NextPoll]\r\n        ,[NextRediscovery]\r\n        ,[FullName]\r\n        ,[DiskQueueLength]\r\n        ,[DiskTransfer]\r\n        ,[DiskReads]\r\n        ,[DiskWrites]\r\n        ,[TotalDiskIOPS]\r\n        ,[DeviceId]\r\n        ,[DiskSerialNumber]\r\n\t\t,[InterfaceType]\r\n        ,[SCSITargetId]\r\n        ,[SCSIPortId]\r\n        ,[SCSILunId]\r\n        ,[SCSIControllerId]\r\n        ,[SCSIPortOffset])\r\n    VALUES \r\n        (@NodeID \r\n        ,@LastSync\r\n        ,@VolumeIndex\r\n        ,@Caption\r\n        ,@PollInterval\r\n        ,@StatCollection\r\n        ,@RediscoveryInterval\r\n        ,@VolumeDescription\r\n        ,@VolumeTypeID\r\n        ,@VolumeType\r\n        ,@VolumeTypeIcon\r\n        ,@VolumePercentUsed\r\n        ,@VolumeSpaceUsed\r\n        ,@VolumeSpaceAvailable\r\n        ,@VolumeSize\r\n        ,@Status\r\n        ,@StatusLED\r\n        ,@VolumeResponding\r\n        ,@VolumeAllocationFailuresThisHour\r\n        ,@VolumeAllocationFailuresToday\r\n        ,@NextPoll\r\n        ,@NextRediscovery\r\n        ,@FullName\r\n        ,@DiskQueueLength\r\n        ,@DiskTransfer\r\n        ,@DiskReads\r\n        ,@DiskWrites\r\n        ,@TotalDiskIOPS\r\n        ,@DeviceId\r\n        ,@DiskSerialNumber\r\n\t\t,@InterfaceType\r\n        ,@SCSITargetId\r\n        ,@SCSIPortId\r\n        ,@SCSILunId\r\n        ,@SCSIControllerId\r\n        ,@SCSIPortOffset);\r\n\r\n    SELECT scope_identity();\r\nEND\r\nELSE\r\nBEGIN\r\n    SELECT -1;\r\nEND\r\n");
			volume = new DALHelper<Volume>().Initialize(volume);
			VolumeDAL.LoadCommandParams(volume, textCommand, true);
			VolumeDAL.log.TraceFormat("Inserting volume. Locking thread. NodeID: {0}, Name: {1}", new object[]
			{
				volume.NodeID,
				volume.VolumeDescription
			});
			object obj = VolumeDAL.insertVolumeLock;
			lock (obj)
			{
				VolumeDAL.log.TraceFormat("Inserting volume. Thread locked. NodeID: {0}, Name: {1}", new object[]
				{
					volume.NodeID,
					volume.VolumeDescription
				});
				volume.VolumeId = Convert.ToInt32(SqlHelper.ExecuteScalar(textCommand));
				if (volume.VolumeId > 0)
				{
					VolumeDAL.log.DebugFormat("Volume [{0}] inserted with ID {1} on node {2}", volume.VolumeDescription, volume.VolumeId, volume.NodeID);
				}
				else
				{
					VolumeDAL.log.DebugFormat("Volume [{0}] managed already on node {1}", volume.VolumeDescription, volume.NodeID);
				}
			}
			return volume.VolumeId;
		}

		// Token: 0x06000877 RID: 2167 RVA: 0x0003D090 File Offset: 0x0003B290
		public static PropertyBag UpdateVolume(Volume volume)
		{
			PropertyBag propertyBag = new PropertyBag();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("\r\n                DECLARE @tempTable TABLE (Caption nvarchar(75), FullName nvarchar(255), Status int, PollInterval int, StatCollection int, RediscoveryInterval int, VolumeIndex int, VolumeType nvarchar(40), VolumeDescription nvarchar(512), VolumeSize float, VolumeResponding char(1));\r\n\t\t\t\tUPDATE [Volumes] SET \r\n\t\t\t\t[LastSync] = @LastSync     \r\n\t\t\t\t,[VolumeIndex] = @VolumeIndex\r\n\t\t\t\t,[Caption] = @Caption\r\n\t\t\t\t,[PollInterval] = @PollInterval\r\n\t\t\t\t,[StatCollection] = @StatCollection\r\n\t\t\t\t,[RediscoveryInterval] = @RediscoveryInterval\r\n\t\t\t\t,[VolumeDescription] = @VolumeDescription\r\n                ,[VolumeTypeID] = @VolumeTypeID\r\n\t\t\t\t,[VolumeType] = @VolumeType\r\n\t\t\t\t,[VolumeTypeIcon] = @VolumeTypeIcon\r\n\t\t\t\t,[VolumePercentUsed] = @VolumePercentUsed\r\n\t\t\t\t,[VolumeSpaceUsed] = @VolumeSpaceUsed\r\n\t\t\t\t,[VolumeSpaceAvailable] = @VolumeSpaceAvailable\r\n\t\t\t\t,[VolumeSize] = @VolumeSize\r\n\t\t\t\t,[Status] = @Status\r\n\t\t\t\t,[StatusLED] = @StatusLED\r\n\t\t\t\t,[VolumeResponding] = @VolumeResponding\r\n\t\t\t\t,[VolumeAllocationFailuresThisHour] = @VolumeAllocationFailuresThisHour\r\n\t\t\t\t,[VolumeAllocationFailuresToday] = @VolumeAllocationFailuresToday\r\n\t\t\t\t,[NextPoll] = @NextPoll\r\n\t\t\t\t,[NextRediscovery] = @NextRediscovery\r\n\t\t\t\t,[FullName] = @FullName\r\n                ,[DiskQueueLength] = @DiskQueueLength\r\n                ,[DiskTransfer] = @DiskTransfer\r\n                ,[DiskReads] = @DiskReads\r\n                ,[DiskWrites] = @DiskWrites\r\n                ,[TotalDiskIOPS] = @TotalDiskIOPS\r\n                ,[DeviceId] = @DeviceId\r\n                ,[DiskSerialNumber] = @DiskSerialNumber\r\n\t\t\t\t,[InterfaceType] = @InterfaceType\r\n                ,[SCSITargetId] = @SCSITargetId\r\n                ,[SCSIPortId] = @SCSIPortId\r\n                ,[SCSILunId] = @SCSILunId\r\n                ,[SCSIControllerId] = @SCSIControllerId\r\n                ,[SCSIPortOffset] = @SCSIPortOffset\r\n                OUTPUT DELETED.Caption, \r\n                       DELETED.FullName, \r\n                       DELETED.Status, \r\n                       DELETED.PollInterval, \r\n                       DELETED.StatCollection,\r\n                       DELETED.RediscoveryInterval,\r\n                       DELETED.VolumeIndex,\r\n                       DELETED.VolumeType,\r\n                       DELETED.VolumeDescription,\r\n                       DELETED.VolumeSize,\r\n                       DELETED.VolumeResponding INTO @tempTable\r\n\t\t\t\tWHERE VolumeID = @VolumeID;\r\n                SELECT * FROM @tempTable;"))
			{
				VolumeDAL.LoadCommandParams(volume, textCommand, true);
				using (DataTable dataTable = SqlHelper.ExecuteDataTable(textCommand))
				{
					if (dataTable.Rows.Count == 1)
					{
						VolumeDAL.UpdateCustomProperties(volume);
						foreach (object obj in dataTable.Rows)
						{
							DataRow dataRow = (DataRow)obj;
							PropertyBag propertyBag2 = new PropertyBag();
							foreach (object obj2 in dataTable.Columns)
							{
								DataColumn dataColumn = (DataColumn)obj2;
								object obj3 = (dataRow[dataColumn] == DBNull.Value) ? null : dataRow[dataColumn];
								object obj4 = null;
								bool? flag = null;
								if (dataColumn.ColumnName.Equals("Caption", StringComparison.OrdinalIgnoreCase))
								{
									obj4 = volume.Caption;
								}
								else if (dataColumn.ColumnName.Equals("FullName", StringComparison.OrdinalIgnoreCase))
								{
									obj4 = volume.FullName;
								}
								else if (dataColumn.ColumnName.Equals("Status", StringComparison.OrdinalIgnoreCase))
								{
									obj4 = volume.Status;
								}
								else if (dataColumn.ColumnName.Equals("PollInterval", StringComparison.OrdinalIgnoreCase))
								{
									obj4 = volume.PollInterval;
								}
								else if (dataColumn.ColumnName.Equals("StatCollection", StringComparison.OrdinalIgnoreCase))
								{
									obj4 = volume.StatCollection;
								}
								else if (dataColumn.ColumnName.Equals("RediscoveryInterval", StringComparison.OrdinalIgnoreCase))
								{
									obj4 = volume.RediscoveryInterval;
								}
								else if (dataColumn.ColumnName.Equals("VolumeDescription", StringComparison.OrdinalIgnoreCase))
								{
									obj4 = volume.VolumeDescription;
								}
								else if (dataColumn.ColumnName.Equals("VolumeSize", StringComparison.OrdinalIgnoreCase))
								{
									obj4 = volume.VolumeSize;
								}
								else if (dataColumn.ColumnName.Equals("VolumeIndex", StringComparison.OrdinalIgnoreCase))
								{
									obj4 = volume.VolumeIndex;
								}
								else if (dataColumn.ColumnName.Equals("VolumeType", StringComparison.OrdinalIgnoreCase))
								{
									obj4 = volume.VolumeType;
								}
								else if (dataColumn.ColumnName.Equals("VolumeResponding", StringComparison.OrdinalIgnoreCase))
								{
									obj4 = volume.VolumeResponding;
									bool flag2 = obj3 != null && string.Equals(obj3.ToString(), "Y", StringComparison.OrdinalIgnoreCase);
									flag = new bool?((bool)obj4 == flag2);
								}
								if ((obj3 == null && obj4 != null) || (obj3 != null && obj4 == null) || (flag == null && obj4 != null && obj3 != null && !string.Equals(obj4.ToString(), obj3.ToString(), StringComparison.Ordinal)) || (flag != null && !flag.Value))
								{
									propertyBag.Add(dataColumn.ColumnName, obj4);
									propertyBag2.Add(dataColumn.ColumnName, obj3);
								}
							}
							if (propertyBag2.Count != 0)
							{
								propertyBag.Add("PreviousProperties", propertyBag2);
							}
						}
					}
				}
			}
			return propertyBag;
		}

		// Token: 0x06000878 RID: 2168 RVA: 0x0003D430 File Offset: 0x0003B630
		private static void UpdateCustomProperties(Volume _volume)
		{
			IDictionary<string, object> customProperties = _volume.CustomProperties;
			if (customProperties.Count == 0)
			{
				return;
			}
			List<string> list = new List<string>(customProperties.Count);
			List<SqlParameter> list2 = new List<SqlParameter>(customProperties.Count);
			int num = 0;
			foreach (string text in customProperties.Keys)
			{
				string text2 = string.Format("p{0}", num);
				num++;
				list.Add(string.Format("[{0}]=@{1}", text, text2));
				if (customProperties[text] == null || customProperties[text] == DBNull.Value || string.IsNullOrEmpty(customProperties[text].ToString()))
				{
					list2.Add(new SqlParameter(text2, DBNull.Value));
				}
				else
				{
					list2.Add(new SqlParameter(text2, customProperties[text]));
				}
			}
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Format("UPDATE Volumes SET {0} WHERE VolumeID=@VolumeID", string.Join(", ", list.ToArray()))))
			{
				foreach (SqlParameter value in list2)
				{
					textCommand.Parameters.Add(value);
				}
				textCommand.Parameters.AddWithValue("VolumeID", _volume.ID);
				SqlHelper.ExecuteNonQuery(textCommand);
			}
		}

		// Token: 0x06000879 RID: 2169 RVA: 0x0003D5D4 File Offset: 0x0003B7D4
		public static Volumes GetNodeVolumes(int nodeID)
		{
			Volumes volumes = new Volumes();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT * FROM Volumes WHERE NodeID=@NodeId"))
			{
				textCommand.Parameters.Add("@NodeId", SqlDbType.Int).Value = nodeID;
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						volumes.Add(DatabaseFunctions.GetInt32(dataReader, "VolumeID"), VolumeDAL.CreateVolume(dataReader));
					}
				}
			}
			return volumes;
		}

		// Token: 0x0600087A RID: 2170 RVA: 0x0003D66C File Offset: 0x0003B86C
		public static Volumes GetNodesVolumes(IEnumerable<int> nodeIDs)
		{
			StringBuilder stringBuilder = new StringBuilder("SELECT * FROM Volumes WHERE NodeID IN (");
			foreach (int value in nodeIDs)
			{
				stringBuilder.Append(value);
				stringBuilder.Append(',');
			}
			StringBuilder stringBuilder2 = stringBuilder;
			int length = stringBuilder2.Length;
			stringBuilder2.Length = length - 1;
			stringBuilder.Append(')');
			return Collection<int, Volume>.FillCollection<Volumes>(new Collection<int, Volume>.CreateElement(VolumeDAL.CreateVolume), stringBuilder.ToString(), Array.Empty<SqlParameter>());
		}

		// Token: 0x0600087B RID: 2171 RVA: 0x0003D700 File Offset: 0x0003B900
		public static Volume GetVolume(int volumeID)
		{
			string commandString = "SELECT * FROM Volumes WHERE VolumeID=@VolumeID";
			SqlParameter[] sqlParamList = new SqlParameter[]
			{
				new SqlParameter("@VolumeID", volumeID)
			};
			return Collection<int, Volume>.GetCollectionItem<Volumes>(new Collection<int, Volume>.CreateElement(VolumeDAL.CreateVolume), commandString, sqlParamList);
		}

		// Token: 0x0600087C RID: 2172 RVA: 0x0003D740 File Offset: 0x0003B940
		public static Volumes GetVolumes()
		{
			string commandString = "SELECT * FROM Volumes";
			return Collection<int, Volume>.FillCollection<Volumes>(new Collection<int, Volume>.CreateElement(VolumeDAL.CreateVolume), commandString, null);
		}

		// Token: 0x0600087D RID: 2173 RVA: 0x0003D768 File Offset: 0x0003B968
		public static Volumes GetVolumesByIds(int[] volumeIds)
		{
			StringBuilder stringBuilder = new StringBuilder();
			string arg = string.Empty;
			foreach (int num in volumeIds)
			{
				stringBuilder.AppendFormat("{0}{1}", arg, num);
				arg = ",";
			}
			return Collection<int, Volume>.FillCollection<Volumes>(new Collection<int, Volume>.CreateElement(VolumeDAL.CreateVolume), string.Format("SELECT * FROM Volumes WHERE Volumes.VolumeID in ({0})", stringBuilder), null);
		}

		// Token: 0x0600087E RID: 2174 RVA: 0x0003D7D0 File Offset: 0x0003B9D0
		public static Volume CreateVolume(IDataReader reader)
		{
			Volume volume = new Volume();
			int i = 0;
			while (i < reader.FieldCount)
			{
				string name = reader.GetName(i);
				uint num = <PrivateImplementationDetails>.ComputeStringHash(name);
				if (num <= 2427376960U)
				{
					if (num <= 1195632823U)
					{
						if (num <= 256162167U)
						{
							if (num <= 45007414U)
							{
								if (num != 6222351U)
								{
									if (num != 45007414U)
									{
										goto IL_841;
									}
									if (!(name == "LastSync"))
									{
										goto IL_841;
									}
									volume.LastSync = DatabaseFunctions.GetDateTime(reader, i);
								}
								else
								{
									if (!(name == "Status"))
									{
										goto IL_841;
									}
									volume.Status = DatabaseFunctions.GetInt32(reader, i);
								}
							}
							else if (num != 248528016U)
							{
								if (num != 256162167U)
								{
									goto IL_841;
								}
								if (!(name == "RediscoveryInterval"))
								{
									goto IL_841;
								}
								volume.RediscoveryInterval = DatabaseFunctions.GetInt32(reader, i);
							}
							else
							{
								if (!(name == "StatusLED"))
								{
									goto IL_841;
								}
								volume.StatusLED = DatabaseFunctions.GetString(reader, i);
							}
						}
						else if (num <= 586568282U)
						{
							if (num != 532412421U)
							{
								if (num != 586568282U)
								{
									goto IL_841;
								}
								if (!(name == "VolumeSpaceUsed"))
								{
									goto IL_841;
								}
								volume.VolumeSpaceUsed = DatabaseFunctions.GetDouble(reader, i);
							}
							else
							{
								if (!(name == "Caption"))
								{
									goto IL_841;
								}
								volume.Caption = DatabaseFunctions.GetString(reader, i);
							}
						}
						else if (num != 862906019U)
						{
							if (num != 914606840U)
							{
								if (num != 1195632823U)
								{
									goto IL_841;
								}
								if (!(name == "DiskTransfer"))
								{
									goto IL_841;
								}
								volume.DiskTransfer = DatabaseFunctions.GetNullableDouble(reader, i);
							}
							else
							{
								if (!(name == "InterfaceType"))
								{
									goto IL_841;
								}
								volume.InterfaceType = DatabaseFunctions.GetString(reader, i);
							}
						}
						else
						{
							if (!(name == "DiskQueueLength"))
							{
								goto IL_841;
							}
							volume.DiskQueueLength = DatabaseFunctions.GetNullableDouble(reader, i);
						}
					}
					else if (num <= 1694652947U)
					{
						if (num <= 1248395407U)
						{
							if (num != 1227082594U)
							{
								if (num != 1248395407U)
								{
									goto IL_841;
								}
								if (!(name == "SCSIPortOffset"))
								{
									goto IL_841;
								}
								volume.SCSIPortOffset = new int?(DatabaseFunctions.GetInt32(reader, i));
							}
							else
							{
								if (!(name == "VolumeID"))
								{
									goto IL_841;
								}
								volume.VolumeId = DatabaseFunctions.GetInt32(reader, i);
							}
						}
						else if (num != 1536158129U)
						{
							if (num != 1694652947U)
							{
								goto IL_841;
							}
							if (!(name == "SCSITargetId"))
							{
								goto IL_841;
							}
							volume.SCSITargetId = new int?(DatabaseFunctions.GetInt32(reader, i));
						}
						else
						{
							if (!(name == "DiskReads"))
							{
								goto IL_841;
							}
							volume.DiskReads = DatabaseFunctions.GetNullableDouble(reader, i);
						}
					}
					else if (num <= 2229825399U)
					{
						if (num != 1697467268U)
						{
							if (num != 2229825399U)
							{
								goto IL_841;
							}
							if (!(name == "FullName"))
							{
								goto IL_841;
							}
							volume.FullName = DatabaseFunctions.GetString(reader, i);
						}
						else
						{
							if (!(name == "VolumeResponding"))
							{
								goto IL_841;
							}
							volume.VolumeResponding = (DatabaseFunctions.GetString(reader, i) == "Y");
						}
					}
					else if (num != 2355672544U)
					{
						if (num != 2385770201U)
						{
							if (num != 2427376960U)
							{
								goto IL_841;
							}
							if (!(name == "VolumeAllocationFailuresThisHour"))
							{
								goto IL_841;
							}
							volume.VolumeAllocationFailuresThisHour = DatabaseFunctions.GetInt32(reader, i);
						}
						else
						{
							if (!(name == "TotalDiskIOPS"))
							{
								goto IL_841;
							}
							volume.TotalDiskIOPS = DatabaseFunctions.GetNullableDouble(reader, i);
						}
					}
					else
					{
						if (!(name == "DiskWrites"))
						{
							goto IL_841;
						}
						volume.DiskWrites = DatabaseFunctions.GetNullableDouble(reader, i);
					}
				}
				else if (num <= 3207278826U)
				{
					if (num <= 2715973202U)
					{
						if (num <= 2544514261U)
						{
							if (num != 2434820582U)
							{
								if (num != 2544514261U)
								{
									goto IL_841;
								}
								if (!(name == "VolumeIndex"))
								{
									goto IL_841;
								}
								volume.VolumeIndex = DatabaseFunctions.GetInt32(reader, i);
							}
							else
							{
								if (!(name == "VolumeSize"))
								{
									goto IL_841;
								}
								volume.VolumeSize = DatabaseFunctions.GetDouble(reader, i);
							}
						}
						else if (num != 2711404065U)
						{
							if (num != 2715973202U)
							{
								goto IL_841;
							}
							if (!(name == "VolumeSpaceAvailable"))
							{
								goto IL_841;
							}
							volume.VolumeSpaceAvailable = DatabaseFunctions.GetDouble(reader, i);
						}
						else
						{
							if (!(name == "PollInterval"))
							{
								goto IL_841;
							}
							volume.PollInterval = DatabaseFunctions.GetInt32(reader, i);
						}
					}
					else if (num <= 2972144755U)
					{
						if (num != 2869862648U)
						{
							if (num != 2972144755U)
							{
								goto IL_841;
							}
							if (!(name == "VolumeAllocationFailuresToday"))
							{
								goto IL_841;
							}
							volume.VolumeAllocationFailuresToday = DatabaseFunctions.GetInt32(reader, i);
						}
						else
						{
							if (!(name == "NodeID"))
							{
								goto IL_841;
							}
							volume.NodeID = DatabaseFunctions.GetInt32(reader, i);
						}
					}
					else if (num != 3144267703U)
					{
						if (num != 3190163158U)
						{
							if (num != 3207278826U)
							{
								goto IL_841;
							}
							if (!(name == "SCSIControllerId"))
							{
								goto IL_841;
							}
							volume.SCSIControllerId = DatabaseFunctions.GetString(reader, i);
						}
						else
						{
							if (!(name == "VolumeTypeIcon"))
							{
								goto IL_841;
							}
							volume.VolumeTypeIcon = DatabaseFunctions.GetString(reader, i);
						}
					}
					else
					{
						if (!(name == "SCSILunId"))
						{
							goto IL_841;
						}
						volume.SCSILunId = new int?(DatabaseFunctions.GetInt32(reader, i));
					}
				}
				else if (num <= 3697247331U)
				{
					if (num <= 3449004665U)
					{
						if (num != 3442739454U)
						{
							if (num != 3449004665U)
							{
								goto IL_841;
							}
							if (!(name == "NextRediscovery"))
							{
								goto IL_841;
							}
							volume.NextRediscovery = DatabaseFunctions.GetDateTime(reader, i);
						}
						else
						{
							if (!(name == "DeviceId"))
							{
								goto IL_841;
							}
							volume.DeviceId = DatabaseFunctions.GetString(reader, i);
						}
					}
					else if (num != 3521202693U)
					{
						if (num != 3646828407U)
						{
							if (num != 3697247331U)
							{
								goto IL_841;
							}
							if (!(name == "VolumeDescription"))
							{
								goto IL_841;
							}
							volume.VolumeDescription = DatabaseFunctions.GetString(reader, i);
						}
						else
						{
							if (!(name == "VolumeType"))
							{
								goto IL_841;
							}
							volume.VolumeType = DatabaseFunctions.GetString(reader, i);
						}
					}
					else
					{
						if (!(name == "NextPoll"))
						{
							goto IL_841;
						}
						volume.NextPoll = DatabaseFunctions.GetDateTime(reader, i);
					}
				}
				else if (num <= 3794922073U)
				{
					if (num != 3768670074U)
					{
						if (num != 3794922073U)
						{
							goto IL_841;
						}
						if (!(name == "DiskSerialNumber"))
						{
							goto IL_841;
						}
						volume.DiskSerialNumber = DatabaseFunctions.GetString(reader, i);
					}
					else
					{
						if (!(name == "VolumeTypeID"))
						{
							goto IL_841;
						}
						volume.VolumeTypeID = DatabaseFunctions.GetInt32(reader, i);
					}
				}
				else if (num != 3876107033U)
				{
					if (num != 3936513175U)
					{
						if (num != 4178420465U)
						{
							goto IL_841;
						}
						if (!(name == "VolumePercentUsed"))
						{
							goto IL_841;
						}
						volume.VolumePercentUsed = Convert.ToDecimal(DatabaseFunctions.GetFloat(reader, i));
					}
					else
					{
						if (!(name == "StatCollection"))
						{
							goto IL_841;
						}
						volume.StatCollection = DatabaseFunctions.GetInt32(reader, i);
					}
				}
				else
				{
					if (!(name == "SCSIPortId"))
					{
						goto IL_841;
					}
					volume.SCSIPortId = new int?(DatabaseFunctions.GetInt32(reader, i));
				}
				IL_87A:
				i++;
				continue;
				IL_841:
				if (CustomPropertyMgr.IsCustom("Volumes", name))
				{
					volume.CustomProperties[name] = reader[i];
					goto IL_87A;
				}
				VolumeDAL.log.DebugFormat("Skipping Volume property {0}, value {1}", name, reader[i]);
				goto IL_87A;
			}
			return volume;
		}

		// Token: 0x0600087F RID: 2175 RVA: 0x0003E068 File Offset: 0x0003C268
		public static void BulkUpdateVolumePollingInterval(int pollInterval, int engineId)
		{
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("IF (@EngineID <= 0) UPDATE Volumes SET PollInterval = @PollInterval ELSE UPDATE Volumes SET PollInterval = @PollInterval WHERE NodeID IN (SELECT NodeID FROM Nodes WITH (NOLOCK) WHERE EngineID = @EngineID)"))
			{
				textCommand.Parameters.Add("@PollInterval", SqlDbType.Int, 4).Value = pollInterval;
				textCommand.Parameters.Add("@engineID", SqlDbType.Int, 4).Value = engineId;
				SqlHelper.ExecuteNonQuery(textCommand);
			}
		}

		// Token: 0x06000880 RID: 2176 RVA: 0x0003E0E0 File Offset: 0x0003C2E0
		public static Dictionary<string, object> GetVolumeCustomProperties(int volumeId, ICollection<string> properties)
		{
			Volume volume = VolumeDAL.GetVolume(volumeId);
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (properties == null || properties.Count == 0)
			{
				properties = volume.CustomProperties.Keys;
			}
			MacroParser macroParser = new MacroParser(new Action<string, int>(BusinessLayerOrionEvent.WriteEvent))
			{
				ObjectType = "Volume",
				ActiveObject = volume.ID.ToString(),
				NetObjectID = volume.ID.ToString(),
				NetObjectName = volume.FullName,
				NodeID = volume.NodeID,
				NodeName = NodeDAL.GetNode(volume.NodeID).Name
			};
			using (macroParser.MyDBConnection = DatabaseFunctions.CreateConnection())
			{
				foreach (string text in properties)
				{
					string key = text.Trim();
					if (volume.CustomProperties.ContainsKey(key))
					{
						object obj = volume.CustomProperties[key];
						if (obj != null && obj.ToString().Contains("${"))
						{
							dictionary[key] = macroParser.ParseMacros(obj.ToString(), false);
						}
						else
						{
							dictionary[key] = obj;
						}
					}
				}
			}
			return dictionary;
		}

		// Token: 0x04000265 RID: 613
		private static readonly Log log = new Log();

		// Token: 0x04000266 RID: 614
		private static object insertVolumeLock = new object();
	}
}
