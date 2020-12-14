using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Auditing;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Auditing;
using SolarWinds.Orion.Core.Common.Indications;
using SolarWinds.Orion.Core.Common.InformationService;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Common.Swis;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000088 RID: 136
	public class AuditingDAL
	{
		// Token: 0x060006B1 RID: 1713 RVA: 0x0002A0A8 File Offset: 0x000282A8
		public List<AuditActionTypeInfo> GetAuditingActionTypes()
		{
			object obj = this.locker;
			List<AuditActionTypeInfo> result;
			lock (obj)
			{
				if (this.actionTypes.Count == 0)
				{
					this.LoadKeys();
				}
				result = (from i in this.actionTypes
				select new AuditActionTypeInfo
				{
					ActionType = i.Key.ToString(),
					ActionTypeId = i.Value
				}).ToList<AuditActionTypeInfo>();
			}
			return result;
		}

		// Token: 0x060006B2 RID: 1714 RVA: 0x0002A128 File Offset: 0x00028328
		public bool LoadKeys()
		{
			AuditingDAL.log.Verbose("LoadKeys...");
			bool result = false;
			object obj = this.locker;
			lock (obj)
			{
				this.actionTypes.Clear();
				try
				{
					using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT ActionTypeID, ActionType FROM AuditingActionTypes;"))
					{
						using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
						{
							while (dataReader.Read())
							{
								try
								{
									this.actionTypes.Add(new AuditActionType(dataReader["ActionType"].ToString()), Convert.ToInt32(dataReader["ActionTypeID"]));
								}
								catch (ArgumentException ex)
								{
									AuditingDAL.log.ErrorFormat("AuditingDAL had problems with loading list of actionTypes. Exception: {0}", ex);
								}
								result = true;
							}
						}
					}
				}
				catch (Exception ex2)
				{
					AuditingDAL.log.ErrorFormat("AuditingDAL couldn't get list of actionTypes. Exception: {0}", ex2);
				}
			}
			AuditingDAL.log.Verbose("LoadKeys finished.");
			return result;
		}

		// Token: 0x060006B3 RID: 1715 RVA: 0x0002A258 File Offset: 0x00028458
		protected int GetActionIdFromActionType(AuditActionType actionType)
		{
			if (AuditingDAL.log.IsDebugEnabled)
			{
				AuditingDAL.log.DebugFormat("GetActionIdFromActionType for {0}", actionType);
			}
			int result = -1;
			object obj = this.locker;
			lock (obj)
			{
				if (this.actionTypes.TryGetValue(actionType, out result))
				{
					return result;
				}
			}
			if (this.LoadKeys())
			{
				obj = this.locker;
				lock (obj)
				{
					if (this.actionTypes.TryGetValue(actionType, out result))
					{
						return result;
					}
				}
			}
			throw new ArgumentException(string.Format("ActionType {0} was not found in dictionary.", actionType));
		}

		// Token: 0x060006B4 RID: 1716 RVA: 0x0002A31C File Offset: 0x0002851C
		public AuditActionType GetActionTypeFromActionId(int actionTypeId)
		{
			if (AuditingDAL.log.IsDebugEnabled)
			{
				AuditingDAL.log.DebugFormat("GetActionTypeFromActionId for {0}", actionTypeId);
			}
			object obj = this.locker;
			lock (obj)
			{
				IEnumerable<KeyValuePair<AuditActionType, int>> source = this.actionTypes;
				Func<KeyValuePair<AuditActionType, int>, bool> <>9__0;
				Func<KeyValuePair<AuditActionType, int>, bool> predicate;
				if ((predicate = <>9__0) == null)
				{
					predicate = (<>9__0 = ((KeyValuePair<AuditActionType, int> actionType) => actionType.Value == actionTypeId));
				}
				using (IEnumerator<KeyValuePair<AuditActionType, int>> enumerator = source.Where(predicate).GetEnumerator())
				{
					if (enumerator.MoveNext())
					{
						KeyValuePair<AuditActionType, int> keyValuePair = enumerator.Current;
						return keyValuePair.Key;
					}
				}
			}
			if (this.LoadKeys())
			{
				obj = this.locker;
				lock (obj)
				{
					IEnumerable<KeyValuePair<AuditActionType, int>> source2 = this.actionTypes;
					Func<KeyValuePair<AuditActionType, int>, bool> <>9__1;
					Func<KeyValuePair<AuditActionType, int>, bool> predicate2;
					if ((predicate2 = <>9__1) == null)
					{
						predicate2 = (<>9__1 = ((KeyValuePair<AuditActionType, int> actionType) => actionType.Value == actionTypeId));
					}
					using (IEnumerator<KeyValuePair<AuditActionType, int>> enumerator = source2.Where(predicate2).GetEnumerator())
					{
						if (enumerator.MoveNext())
						{
							KeyValuePair<AuditActionType, int> keyValuePair2 = enumerator.Current;
							return keyValuePair2.Key;
						}
					}
				}
			}
			throw new ArgumentException(string.Format("ActionTypeId {0} was not found in dictionary.", actionTypeId));
		}

		// Token: 0x060006B5 RID: 1717 RVA: 0x0002A4A4 File Offset: 0x000286A4
		public static string GetNodeCaption(int nodeId)
		{
			string text = string.Format("SELECT Caption FROM Nodes WHERE NodeId = @NodeId", Array.Empty<object>());
			string result;
			try
			{
				using (SqlCommand textCommand = SqlHelper.GetTextCommand(text))
				{
					textCommand.Parameters.AddWithValue("@NodeId", nodeId);
					result = (string)SqlHelper.ExecuteScalar(textCommand);
				}
			}
			catch (Exception ex)
			{
				AuditingDAL.log.Warn("GetNodeCaption failed.", ex);
				result = string.Empty;
			}
			return result;
		}

		// Token: 0x060006B6 RID: 1718 RVA: 0x0002A530 File Offset: 0x00028730
		public static KeyValuePair<string, string> GetNodeCaptionAndStatus(int nodeId)
		{
			string text = string.Format("SELECT Caption, Status FROM Nodes WHERE NodeId = @NodeId", Array.Empty<object>());
			try
			{
				using (SqlCommand textCommand = SqlHelper.GetTextCommand(text))
				{
					textCommand.Parameters.AddWithValue("@NodeId", nodeId);
					using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
					{
						if (dataReader.Read())
						{
							return new KeyValuePair<string, string>(dataReader["Caption"].ToString(), dataReader["Status"].ToString());
						}
					}
				}
			}
			catch (Exception ex)
			{
				AuditingDAL.log.Warn("GetNodeCaptionAndStatus failed.", ex);
			}
			return default(KeyValuePair<string, string>);
		}

		// Token: 0x060006B7 RID: 1719 RVA: 0x0002A608 File Offset: 0x00028808
		public int StoreNotification(AuditDatabaseDecoratedContainer container)
		{
			if (container == null)
			{
				throw new ArgumentNullException("container");
			}
			return this.StoreNotification(container, this.GetActionIdFromActionType(container.ActionType));
		}

		// Token: 0x060006B8 RID: 1720 RVA: 0x0002A62C File Offset: 0x0002882C
		private AuditingDAL.NetObjectInfo GetNetObjectInfo(IDictionary<string, string> arguments)
		{
			int? networkNodeID = null;
			int? num = null;
			string text = null;
			if (arguments.ContainsKey(KnownKeys.NetObject) && arguments[KnownKeys.NetObject] != null)
			{
				string[] array = NetObjectHelper.ParseNetObject(arguments[KnownKeys.NetObject]);
				text = array[0];
				num = new int?(int.Parse(array[1]));
			}
			if (arguments.ContainsKey(KnownKeys.NodeID))
			{
				networkNodeID = new int?(int.Parse(arguments[KnownKeys.NodeID]));
			}
			else if (text != null && text.Equals("N", StringComparison.OrdinalIgnoreCase))
			{
				networkNodeID = num;
			}
			return new AuditingDAL.NetObjectInfo
			{
				NetObjectID = num,
				NetObjectType = text,
				NetworkNodeID = networkNodeID
			};
		}

		// Token: 0x060006B9 RID: 1721 RVA: 0x0002A6DC File Offset: 0x000288DC
		protected int StoreNotification(AuditDatabaseDecoratedContainer decoratedDecoratedContainer, int actionTypeId)
		{
			if (AuditingDAL.log.IsTraceEnabled)
			{
				AuditingDAL.log.Trace("StoreNotification actionTypeId: " + actionTypeId);
			}
			int count = decoratedDecoratedContainer.Args.Count;
			if (AuditingDAL.log.IsDebugEnabled)
			{
				AuditingDAL.log.Debug("args.Count: " + count);
			}
			AuditingDAL.NetObjectInfo netObjectInfo = this.GetNetObjectInfo(decoratedDecoratedContainer.Args);
			StringBuilder stringBuilder = new StringBuilder("\r\nDECLARE @msg VARCHAR(max), @sev INT, @st INT;\r\n\r\n    INSERT INTO [dbo].[AuditingEvents] \r\n    (\r\n        [TimeLoggedUtc], \r\n        [AccountID], \r\n        [ActionTypeID], \r\n        [AuditEventMessage],\r\n        [NetworkNode],\r\n        [NetObjectID],\r\n        [NetObjectType]\r\n    )\r\n    VALUES\r\n    (\r\n        @TimeLoggedUtc, \r\n        @AccountID, \r\n        @ActionTypeID, \r\n        @AuditEventMessage,\r\n        @NetworkNode,\r\n        @NetObjectID,\r\n        @NetObjectType\r\n    );\r\n");
			if (count > 0)
			{
				stringBuilder.Append("  SELECT @lastID = @@IDENTITY;\r\n\r\n    INSERT INTO [dbo].[AuditingArguments] \r\n    ([AuditEventID], [ArgsKey], [ArgsValue])\r\n     ");
				stringBuilder.Append(string.Join(" UNION ALL ", from i in Enumerable.Range(0, count)
				select string.Format(" SELECT @lastID, @ArgsKey{0}, @ArgsValue{0} ", i)));
			}
			int result;
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
				{
					using (SqlCommand textCommand = SqlHelper.GetTextCommand(stringBuilder.ToString()))
					{
						try
						{
							textCommand.Parameters.AddWithValue("@TimeLoggedUtc", decoratedDecoratedContainer.IndicationTime.ToUniversalTime());
							textCommand.Parameters.AddWithValue("@AccountID", decoratedDecoratedContainer.AccountId.ToLower());
							textCommand.Parameters.AddWithValue("@ActionTypeID", actionTypeId);
							textCommand.Parameters.AddWithValue("@AuditEventMessage", decoratedDecoratedContainer.Message);
							textCommand.Parameters.AddWithValue("@NetworkNode", (netObjectInfo.NetworkNodeID != null) ? netObjectInfo.NetworkNodeID.Value : DBNull.Value);
							textCommand.Parameters.AddWithValue("@NetObjectID", (netObjectInfo.NetObjectID != null) ? netObjectInfo.NetObjectID.Value : DBNull.Value);
							textCommand.Parameters.AddWithValue("@NetObjectType", (netObjectInfo.NetObjectType != null) ? netObjectInfo.NetObjectType : DBNull.Value);
							textCommand.Parameters.Add(new SqlParameter("@lastID", SqlDbType.Int)
							{
								Direction = ParameterDirection.InputOutput,
								Value = 0
							});
							for (int j = 0; j < count; j++)
							{
								string key = decoratedDecoratedContainer.Args.ElementAt(j).Key;
								string value = decoratedDecoratedContainer.Args.ElementAt(j).Value;
								if (AuditingDAL.log.IsDebugEnabled)
								{
									AuditingDAL.log.DebugFormat("Adding argument: '{0}':'{1}'", key, value);
								}
								textCommand.Parameters.AddWithValue(string.Format("@ArgsKey{0}", j), key);
								textCommand.Parameters.AddWithValue(string.Format("@ArgsValue{0}", j), value ?? string.Empty);
							}
							AuditingDAL.log.Trace("Executing query.");
							SqlHelper.ExecuteScalar(textCommand, sqlConnection, sqlTransaction);
							sqlTransaction.Commit();
							int num = 0;
							int.TryParse(textCommand.Parameters["@lastID"].Value.ToString(), out num);
							result = num;
						}
						catch (Exception ex)
						{
							sqlTransaction.Rollback();
							AuditingDAL.log.Error("Error while storing notification.", ex);
							throw;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x060006BA RID: 1722 RVA: 0x0002AAA0 File Offset: 0x00028CA0
		internal static DataTable GetAuditingTable(int maxRecords, int netObjectId, string netObjectType, int nodeId, string actionTypeIds, DateTime startTime, DateTime endTime)
		{
			string format = "\r\n;WITH TOPNAUDITS\r\nAS\r\n(\r\n\tSELECT TOP (@parTopLimit)\r\n\t\tAE.TimeLoggedUtc AS DateTime,\r\n\t\tAE.AccountID,\r\n\t\tAE.AuditEventMessage AS Message,\r\n\t\tAE.AuditEventID,\r\n\t\tAE.ActionTypeID    \r\n\tFROM AuditingEvents AS AE WITH(NOLOCK)\r\n\tWHERE\r\n\t{0}        \r\n\tORDER BY\r\n\t\tAE.TimeLoggedUtc DESC\r\n)\r\nSELECT \r\n    TNA.DateTime,\r\n    TNA.AccountID,\r\n    TNA.Message,\r\n    TNA.AuditEventID,\r\n    TNA.ActionTypeID,\r\n    AAT.ActionType,\r\n    ARGS.ArgsKey,\r\n    ARGS.ArgsValue\r\nFROM TOPNAUDITS AS TNA\r\nLEFT JOIN AuditingActionTypes AS AAT WITH(NOLOCK)\r\nON\r\n    TNA.ActionTypeID = AAT.ActionTypeID\r\nLEFT JOIN AuditingArguments AS ARGS WITH(NOLOCK)\r\n\tON TNA.AuditEventID = ARGS.AuditEventID\r\nORDER BY TNA.[DateTime] DESC\r\n";
			DataTable result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Empty))
			{
				List<string> list = new List<string>();
				list.Add("1 = 1");
				textCommand.Parameters.AddWithValue("@parTopLimit", maxRecords);
				list.Add("AE.TimeLoggedUtc >= @parStartTime");
				textCommand.Parameters.AddWithValue("@parStartTime", startTime);
				list.Add("AE.TimeLoggedUtc <= @parEndTime");
				textCommand.Parameters.AddWithValue("@parEndTime", endTime);
				if (nodeId >= 0 && netObjectId < 0)
				{
					list.Add("AE.NetworkNode = @parNodeID");
					textCommand.Parameters.Add(new SqlParameter("@parNodeID", nodeId));
				}
				if (netObjectId >= 0)
				{
					list.Add("AE.NetObjectID = @parNetObjectID");
					textCommand.Parameters.Add(new SqlParameter("@parNetObjectID", netObjectId));
				}
				if (!string.IsNullOrEmpty(netObjectType))
				{
					list.Add("AE.NetObjectType LIKE @parNetObjectType");
					textCommand.Parameters.Add(new SqlParameter("@parNetObjectType", SqlDbType.Char, 10)
					{
						Value = netObjectType
					});
				}
				if (!string.IsNullOrEmpty(actionTypeIds))
				{
					list.Add(" AE.ActionTypeID IN (" + actionTypeIds + ")");
				}
				string arg = string.Join(" AND " + Environment.NewLine, list);
				string commandText = string.Format(format, arg);
				textCommand.CommandText = commandText;
				DataTable dataTable = SqlHelper.ExecuteDataTable(textCommand);
				dataTable.TableName = "AuditingTable";
				result = dataTable;
			}
			return result;
		}

		// Token: 0x060006BB RID: 1723 RVA: 0x0002AC40 File Offset: 0x00028E40
		public static DataTable GetAuditingTypesTable()
		{
			DataTable result;
			using (IInformationServiceProxy2 informationServiceProxy = AuditingDAL.creator.Create())
			{
				DataTable dataTable = informationServiceProxy.QueryWithAppendedErrors("SELECT DISTINCT ActionTypeID, ActionType, ActionTypeDisplayName FROM [Orion].[AuditingActionTypes] (nolock=true)", SwisFederationInfo.IsFederationEnabled);
				dataTable.TableName = "AuditingTypesTable";
				result = dataTable;
			}
			return result;
		}

		// Token: 0x060006BC RID: 1724 RVA: 0x0002AC94 File Offset: 0x00028E94
		public AuditDataContainer GetAuditDataContainer(int auditEventId)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT [ArgsKey], [ArgsValue] FROM [dbo].[AuditingArguments] WITH(NOLOCK) WHERE [AuditEventID] = @AuditEventID;"))
			{
				textCommand.Parameters.AddWithValue("@AuditEventID", auditEventId);
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						dictionary.Add(dataReader["ArgsKey"].ToString(), dataReader["ArgsValue"].ToString());
					}
				}
			}
			AuditDataContainer result;
			using (SqlCommand textCommand2 = SqlHelper.GetTextCommand("SELECT TOP 1 [AccountID], [ActionTypeID] FROM [dbo].[AuditingEvents] WITH(NOLOCK) WHERE [AuditEventID] = @AuditEventID;"))
			{
				textCommand2.Parameters.AddWithValue("@AuditEventID", auditEventId);
				using (IDataReader dataReader2 = SqlHelper.ExecuteReader(textCommand2))
				{
					dataReader2.Read();
					result = new AuditDataContainer(this.GetActionTypeFromActionId((int)dataReader2["ActionTypeID"]), dictionary, dataReader2["AccountID"].ToString());
				}
			}
			return result;
		}

		// Token: 0x04000212 RID: 530
		protected static readonly Log log = new Log();

		// Token: 0x04000213 RID: 531
		protected readonly object locker = new object();

		// Token: 0x04000214 RID: 532
		protected readonly Dictionary<AuditActionType, int> actionTypes = new Dictionary<AuditActionType, int>();

		// Token: 0x04000215 RID: 533
		private static readonly IInformationServiceProxyCreator creator = SwisConnectionProxyPool.GetCreator();

		// Token: 0x02000179 RID: 377
		private class NetObjectInfo
		{
			// Token: 0x1700015B RID: 347
			// (get) Token: 0x06000C11 RID: 3089 RVA: 0x00049DEF File Offset: 0x00047FEF
			// (set) Token: 0x06000C12 RID: 3090 RVA: 0x00049DF7 File Offset: 0x00047FF7
			public int? NetworkNodeID { get; set; }

			// Token: 0x1700015C RID: 348
			// (get) Token: 0x06000C13 RID: 3091 RVA: 0x00049E00 File Offset: 0x00048000
			// (set) Token: 0x06000C14 RID: 3092 RVA: 0x00049E08 File Offset: 0x00048008
			public int? NetObjectID { get; set; }

			// Token: 0x1700015D RID: 349
			// (get) Token: 0x06000C15 RID: 3093 RVA: 0x00049E11 File Offset: 0x00048011
			// (set) Token: 0x06000C16 RID: 3094 RVA: 0x00049E19 File Offset: 0x00048019
			public string NetObjectType { get; set; }
		}
	}
}
