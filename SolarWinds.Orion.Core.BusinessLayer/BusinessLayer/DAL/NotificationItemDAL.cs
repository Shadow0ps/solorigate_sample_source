using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Discovery.DataAccess;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x020000A5 RID: 165
	public class NotificationItemDAL
	{
		// Token: 0x17000104 RID: 260
		// (get) Token: 0x0600080D RID: 2061 RVA: 0x00039B63 File Offset: 0x00037D63
		// (set) Token: 0x0600080E RID: 2062 RVA: 0x00039B6B File Offset: 0x00037D6B
		public Guid Id { get; private set; }

		// Token: 0x17000105 RID: 261
		// (get) Token: 0x0600080F RID: 2063 RVA: 0x00039B74 File Offset: 0x00037D74
		// (set) Token: 0x06000810 RID: 2064 RVA: 0x00039B7C File Offset: 0x00037D7C
		public string Title { get; set; }

		// Token: 0x17000106 RID: 262
		// (get) Token: 0x06000811 RID: 2065 RVA: 0x00039B85 File Offset: 0x00037D85
		// (set) Token: 0x06000812 RID: 2066 RVA: 0x00039B8D File Offset: 0x00037D8D
		public string Description { get; set; }

		// Token: 0x17000107 RID: 263
		// (get) Token: 0x06000813 RID: 2067 RVA: 0x00039B96 File Offset: 0x00037D96
		// (set) Token: 0x06000814 RID: 2068 RVA: 0x00039B9E File Offset: 0x00037D9E
		public DateTime CreatedAt { get; protected set; }

		// Token: 0x17000108 RID: 264
		// (get) Token: 0x06000815 RID: 2069 RVA: 0x00039BA7 File Offset: 0x00037DA7
		// (set) Token: 0x06000816 RID: 2070 RVA: 0x00039BAF File Offset: 0x00037DAF
		public bool Ignored { get; set; }

		// Token: 0x17000109 RID: 265
		// (get) Token: 0x06000817 RID: 2071 RVA: 0x00039BB8 File Offset: 0x00037DB8
		// (set) Token: 0x06000818 RID: 2072 RVA: 0x00039BC0 File Offset: 0x00037DC0
		public Guid TypeId { get; protected set; }

		// Token: 0x1700010A RID: 266
		// (get) Token: 0x06000819 RID: 2073 RVA: 0x00039BC9 File Offset: 0x00037DC9
		// (set) Token: 0x0600081A RID: 2074 RVA: 0x00039BD1 File Offset: 0x00037DD1
		public string Url { get; set; }

		// Token: 0x1700010B RID: 267
		// (get) Token: 0x0600081B RID: 2075 RVA: 0x00039BDA File Offset: 0x00037DDA
		// (set) Token: 0x0600081C RID: 2076 RVA: 0x00039BE2 File Offset: 0x00037DE2
		public DateTime? AcknowledgedAt { get; private set; }

		// Token: 0x1700010C RID: 268
		// (get) Token: 0x0600081D RID: 2077 RVA: 0x00039BEB File Offset: 0x00037DEB
		// (set) Token: 0x0600081E RID: 2078 RVA: 0x00039BF3 File Offset: 0x00037DF3
		public string AcknowledgedBy { get; private set; }

		// Token: 0x1700010D RID: 269
		// (get) Token: 0x0600081F RID: 2079 RVA: 0x00039BFC File Offset: 0x00037DFC
		public bool IsAcknowledged
		{
			get
			{
				return this.AcknowledgedAt != null;
			}
		}

		// Token: 0x1700010E RID: 270
		// (get) Token: 0x06000820 RID: 2080 RVA: 0x00039C17 File Offset: 0x00037E17
		public bool IsVisible
		{
			get
			{
				return !this.IsAcknowledged && !this.Ignored;
			}
		}

		// Token: 0x06000821 RID: 2081 RVA: 0x00039C2C File Offset: 0x00037E2C
		public void SetAcknowledged(DateTime at, string by)
		{
			if (at == DateTime.MinValue)
			{
				throw new ArgumentNullException("at");
			}
			if (string.IsNullOrEmpty(by))
			{
				throw new ArgumentNullException("by");
			}
			this.AcknowledgedAt = new DateTime?(at);
			this.AcknowledgedBy = by;
		}

		// Token: 0x06000822 RID: 2082 RVA: 0x00039C6C File Offset: 0x00037E6C
		public void SetNotAcknowledged()
		{
			this.AcknowledgedAt = null;
			this.AcknowledgedBy = null;
		}

		// Token: 0x06000823 RID: 2083 RVA: 0x00039C90 File Offset: 0x00037E90
		public NotificationItemDAL()
		{
			this.Id = Guid.Empty;
			this.Title = string.Empty;
			this.Description = string.Empty;
			this.CreatedAt = DateTime.MinValue;
			this.Ignored = false;
			this.TypeId = Guid.Empty;
			this.Url = null;
			this.AcknowledgedAt = null;
			this.AcknowledgedBy = null;
		}

		// Token: 0x06000824 RID: 2084 RVA: 0x000347D3 File Offset: 0x000329D3
		protected virtual Guid GetNotificationItemTypeId()
		{
			return Guid.Empty;
		}

		// Token: 0x06000825 RID: 2085 RVA: 0x00039D00 File Offset: 0x00037F00
		public static TNotificationItem GetItemById<TNotificationItem>(Guid itemId) where TNotificationItem : NotificationItemDAL, new()
		{
			TNotificationItem result;
			try
			{
				TNotificationItem tnotificationItem = Activator.CreateInstance<TNotificationItem>();
				tnotificationItem.Id = itemId;
				tnotificationItem.LoadFromDB();
				result = tnotificationItem;
			}
			catch (ResultCountException)
			{
				NotificationItemDAL.log.DebugFormat("Can't find notification item in database: ID={0}, Type={1}", itemId, typeof(TNotificationItem).Name);
				result = default(TNotificationItem);
			}
			return result;
		}

		// Token: 0x06000826 RID: 2086 RVA: 0x00039D70 File Offset: 0x00037F70
		public static ICollection<TNotificationItem> GetItems<TNotificationItem>(NotificationItemFilter filter) where TNotificationItem : NotificationItemDAL, new()
		{
			ICollection<TNotificationItem> result;
			try
			{
				result = NotificationItemDAL.LoadCollectionFromDB<TNotificationItem>(filter);
			}
			catch (ResultCountException)
			{
				NotificationItemDAL.log.DebugFormat("Can't get notification item collection from database: Type={0}", typeof(TNotificationItem).Name);
				result = null;
			}
			return result;
		}

		// Token: 0x06000827 RID: 2087 RVA: 0x00039DBC File Offset: 0x00037FBC
		public static ICollection<NotificationItemDAL> GetItemsByTypeId(Guid typeId, NotificationItemFilter filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException("filter");
			}
			if (typeId == Guid.Empty)
			{
				throw new ArgumentException("Value can't be empty GUID", "typeId");
			}
			ICollection<NotificationItemDAL> result;
			try
			{
				using (SqlCommand sqlCommand = new NotificationItemDAL().ComposeSelectCollectionCommand(typeId, filter))
				{
					using (IDataReader dataReader = SqlHelper.ExecuteReader(sqlCommand))
					{
						List<NotificationItemDAL> list = new List<NotificationItemDAL>();
						while (dataReader.Read())
						{
							NotificationItemDAL notificationItemDAL = new NotificationItemDAL();
							notificationItemDAL.LoadFromReader(dataReader);
							list.Add(notificationItemDAL);
						}
						result = list;
					}
				}
			}
			catch (ResultCountException)
			{
				NotificationItemDAL.log.DebugFormat("Can't get notification item collection from database: TypeID={0}", typeId);
				result = null;
			}
			return result;
		}

		// Token: 0x06000828 RID: 2088 RVA: 0x00039E8C File Offset: 0x0003808C
		public static TNotificationItem GetLatestItem<TNotificationItem>(NotificationItemFilter filter) where TNotificationItem : NotificationItemDAL, new()
		{
			TNotificationItem tnotificationItem = Activator.CreateInstance<TNotificationItem>();
			TNotificationItem result;
			using (SqlCommand sqlCommand = tnotificationItem.ComposeSelectLatestItemCommand(filter))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(sqlCommand))
				{
					if (dataReader.Read())
					{
						tnotificationItem.LoadFromReader(dataReader);
						result = tnotificationItem;
					}
					else
					{
						result = default(TNotificationItem);
					}
				}
			}
			return result;
		}

		// Token: 0x06000829 RID: 2089 RVA: 0x00039F0C File Offset: 0x0003810C
		public static NotificationItemDAL GetLatestItemByType(Guid typeId, NotificationItemFilter filter)
		{
			NotificationItemDAL notificationItemDAL = new NotificationItemDAL();
			NotificationItemDAL result;
			using (SqlCommand sqlCommand = notificationItemDAL.ComposeSelectLatestItemCommand(typeId, filter))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(sqlCommand))
				{
					if (dataReader.Read())
					{
						notificationItemDAL.LoadFromReader(dataReader);
						result = notificationItemDAL;
					}
					else
					{
						result = null;
					}
				}
			}
			return result;
		}

		// Token: 0x0600082A RID: 2090 RVA: 0x00039F78 File Offset: 0x00038178
		public static void GetLatestItemsWithCount(NotificationItemFilter filter, Action<NotificationItemDAL, int> readerDelegate)
		{
			using (SqlCommand sqlCommand = new NotificationItemDAL().ComposeSelectLatestItemsWithCountCommand(filter))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(sqlCommand))
				{
					while (dataReader.Read())
					{
						NotificationItemDAL notificationItemDAL = new NotificationItemDAL();
						notificationItemDAL.LoadFromReader(dataReader);
						readerDelegate(notificationItemDAL, DatabaseFunctions.GetInt32(dataReader, "NotificationCount"));
					}
				}
			}
		}

		// Token: 0x0600082B RID: 2091 RVA: 0x00039FF4 File Offset: 0x000381F4
		protected virtual SqlCommand ComposeSelectCollectionCommand(NotificationItemFilter filter)
		{
			return this.ComposeSelectCollectionCommand(this.GetNotificationItemTypeId(), filter);
		}

		// Token: 0x0600082C RID: 2092 RVA: 0x0003A004 File Offset: 0x00038204
		private SqlCommand ComposeSelectCollectionCommand(Guid typeId, NotificationItemFilter filter)
		{
			SqlCommand sqlCommand = new SqlCommand("SELECT * FROM NotificationItems");
			SqlCommand result;
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				if (typeId != Guid.Empty)
				{
					SqlHelper.AddCondition(stringBuilder, "NotificationTypeID=@NotificationTypeID", "AND");
					sqlCommand.Parameters.AddWithValue("@NotificationTypeID", typeId);
				}
				if (!filter.IncludeAcknowledged)
				{
					SqlHelper.AddCondition(stringBuilder, "AcknowledgedAt IS NULL", "AND");
				}
				if (!filter.IncludeIgnored)
				{
					SqlHelper.AddCondition(stringBuilder, "Ignored=0", "AND");
				}
				SqlCommand sqlCommand2 = sqlCommand;
				sqlCommand2.CommandText += stringBuilder.ToString();
				SqlCommand sqlCommand3 = sqlCommand;
				sqlCommand3.CommandText += " ORDER BY CreatedAt DESC";
				result = sqlCommand;
			}
			catch (Exception ex)
			{
				sqlCommand.Dispose();
				NotificationItemDAL.log.Error(string.Format("Error while composing SELECT SQL command for {0} collection: ", base.GetType().Name) + ex.ToString());
				throw;
			}
			return result;
		}

		// Token: 0x0600082D RID: 2093 RVA: 0x0003A0FC File Offset: 0x000382FC
		protected virtual SqlCommand ComposeSelectItemCommand()
		{
			SqlCommand sqlCommand = new SqlCommand("SELECT * FROM NotificationItems WHERE NotificationID=@NotificationID");
			SqlCommand result;
			try
			{
				sqlCommand.Parameters.AddWithValue("@NotificationID", this.Id);
				result = sqlCommand;
			}
			catch (Exception ex)
			{
				sqlCommand.Dispose();
				NotificationItemDAL.log.Error(string.Format("Error while composing SELECT SQL command for {0}: ", base.GetType().Name) + ex.ToString());
				throw;
			}
			return result;
		}

		// Token: 0x0600082E RID: 2094 RVA: 0x0003A178 File Offset: 0x00038378
		protected virtual SqlCommand ComposeSelectLatestItemCommand(NotificationItemFilter filter)
		{
			return this.ComposeSelectLatestItemCommand(this.GetNotificationItemTypeId(), filter);
		}

		// Token: 0x0600082F RID: 2095 RVA: 0x0003A188 File Offset: 0x00038388
		protected virtual SqlCommand ComposeSelectLatestItemCommand(Guid typeId, NotificationItemFilter filter)
		{
			SqlCommand sqlCommand = new SqlCommand("SELECT TOP 1 * FROM NotificationItems");
			SqlCommand result;
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				if (typeId != Guid.Empty)
				{
					SqlHelper.AddCondition(stringBuilder, "NotificationTypeID=@NotificationTypeID", "AND");
					sqlCommand.Parameters.AddWithValue("@NotificationTypeID", typeId);
				}
				if (!filter.IncludeAcknowledged)
				{
					SqlHelper.AddCondition(stringBuilder, "AcknowledgedAt IS NULL", "AND");
				}
				if (!filter.IncludeIgnored)
				{
					SqlHelper.AddCondition(stringBuilder, "Ignored=0", "AND");
				}
				SqlCommand sqlCommand2 = sqlCommand;
				sqlCommand2.CommandText += stringBuilder.ToString();
				SqlCommand sqlCommand3 = sqlCommand;
				sqlCommand3.CommandText += " ORDER BY CreatedAt DESC";
				result = sqlCommand;
			}
			catch (Exception ex)
			{
				sqlCommand.Dispose();
				NotificationItemDAL.log.Error(string.Format("Error while composing SELECT SQL command for latest {0}: ", base.GetType().Name) + ex.ToString());
				throw;
			}
			return result;
		}

		// Token: 0x06000830 RID: 2096 RVA: 0x0003A280 File Offset: 0x00038480
		protected virtual SqlCommand ComposeSelectLatestItemsWithCountCommand(NotificationItemFilter filter)
		{
			SqlCommand sqlCommand = new SqlCommand("SELECT (SELECT COUNT(NotificationID) FROM NotificationItems {0}) as NotificationCount, i1.*\r\n     FROM NotificationItems i1 LEFT OUTER JOIN \r\n     NotificationItems i2 ON (i1.NotificationTypeID = i2.NotificationTypeID AND i1.CreatedAt < i2.CreatedAt)");
			SqlCommand result;
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				SqlHelper.AddCondition(stringBuilder, "NotificationTypeID = i1.NotificationTypeID", "AND");
				StringBuilder stringBuilder2 = new StringBuilder();
				SqlHelper.AddCondition(stringBuilder2, "i2.NotificationID IS NULL", "AND");
				if (!filter.IncludeAcknowledged)
				{
					SqlHelper.AddCondition(stringBuilder, "AcknowledgedAt IS NULL", "AND");
					SqlHelper.AddCondition(stringBuilder2, "i1.AcknowledgedAt IS NULL", "AND");
				}
				if (!filter.IncludeIgnored)
				{
					SqlHelper.AddCondition(stringBuilder, "Ignored=0", "AND");
					SqlHelper.AddCondition(stringBuilder2, "i1.Ignored=0", "AND");
				}
				sqlCommand.CommandText = string.Format(CultureInfo.InvariantCulture, sqlCommand.CommandText, stringBuilder);
				SqlCommand sqlCommand2 = sqlCommand;
				sqlCommand2.CommandText += stringBuilder2.ToString();
				result = sqlCommand;
			}
			catch (Exception ex)
			{
				sqlCommand.Dispose();
				NotificationItemDAL.log.Error(string.Format("Error while composing SELECT SQL command for latest {0}: ", base.GetType().Name) + ex.ToString());
				throw;
			}
			return result;
		}

		// Token: 0x06000831 RID: 2097 RVA: 0x0003A38C File Offset: 0x0003858C
		protected virtual SqlCommand ComposeSelectCountCommand(NotificationItemFilter filter)
		{
			return this.ComposeSelectCountCommand(this.GetNotificationItemTypeId(), filter);
		}

		// Token: 0x06000832 RID: 2098 RVA: 0x0003A39C File Offset: 0x0003859C
		private SqlCommand ComposeSelectCountCommand(Guid typeId, NotificationItemFilter filter)
		{
			SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(NotificationID) FROM NotificationItems");
			SqlCommand result;
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				if (typeId != Guid.Empty)
				{
					SqlHelper.AddCondition(stringBuilder, "NotificationTypeID=@NotificationTypeID", "AND");
					sqlCommand.Parameters.AddWithValue("@NotificationTypeID", typeId);
				}
				if (!filter.IncludeAcknowledged)
				{
					SqlHelper.AddCondition(stringBuilder, "AcknowledgedAt IS NULL", "AND");
				}
				if (!filter.IncludeIgnored)
				{
					SqlHelper.AddCondition(stringBuilder, "Ignored=0", "AND");
				}
				SqlCommand sqlCommand2 = sqlCommand;
				sqlCommand2.CommandText += stringBuilder.ToString();
				result = sqlCommand;
			}
			catch (Exception ex)
			{
				sqlCommand.Dispose();
				NotificationItemDAL.log.Error(string.Format("Error while composing SELECT COUNT SQL command for {0}: ", base.GetType().Name) + ex.ToString());
				throw;
			}
			return result;
		}

		// Token: 0x06000833 RID: 2099 RVA: 0x0003A480 File Offset: 0x00038680
		protected virtual void LoadFromReader(IDataReader rd)
		{
			if (rd == null)
			{
				throw new ArgumentNullException("rd");
			}
			this.Id = DatabaseFunctions.GetGuid(rd, "NotificationID");
			this.Title = DatabaseFunctions.GetString(rd, "Title");
			this.Description = DatabaseFunctions.GetString(rd, "Description");
			this.CreatedAt = DatabaseFunctions.GetDateTime(rd, "CreatedAt");
			this.Ignored = DatabaseFunctions.GetBoolean(rd, "Ignored");
			this.TypeId = DatabaseFunctions.GetGuid(rd, "NotificationTypeID");
			this.Url = DatabaseFunctions.GetString(rd, "Url");
			DateTime dateTime = DatabaseFunctions.GetDateTime(rd, "AcknowledgedAt");
			if (dateTime == DateTime.MinValue)
			{
				this.AcknowledgedAt = null;
			}
			else
			{
				this.AcknowledgedAt = new DateTime?(dateTime);
			}
			this.AcknowledgedBy = DatabaseFunctions.GetString(rd, "AcknowledgedBy");
		}

		// Token: 0x06000834 RID: 2100 RVA: 0x0003A55C File Offset: 0x0003875C
		protected void LoadFromDB()
		{
			using (SqlCommand sqlCommand = this.ComposeSelectItemCommand())
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(sqlCommand))
				{
					if (!dataReader.Read())
					{
						throw new ResultCountException(1, 0);
					}
					this.LoadFromReader(dataReader);
				}
			}
		}

		// Token: 0x06000835 RID: 2101 RVA: 0x0003A5C0 File Offset: 0x000387C0
		private static ICollection<TNotificationItem> LoadCollectionFromDB<TNotificationItem>(NotificationItemFilter filter) where TNotificationItem : NotificationItemDAL, new()
		{
			if (filter == null)
			{
				throw new ArgumentNullException("filter");
			}
			ICollection<TNotificationItem> result;
			using (SqlCommand sqlCommand = Activator.CreateInstance<TNotificationItem>().ComposeSelectCollectionCommand(filter))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(sqlCommand))
				{
					List<TNotificationItem> list = new List<TNotificationItem>();
					while (dataReader.Read())
					{
						TNotificationItem tnotificationItem = Activator.CreateInstance<TNotificationItem>();
						tnotificationItem.LoadFromReader(dataReader);
						list.Add(tnotificationItem);
					}
					result = list;
				}
			}
			return result;
		}

		// Token: 0x06000836 RID: 2102 RVA: 0x0003A654 File Offset: 0x00038854
		protected virtual bool Update(SqlConnection con, SqlTransaction tr)
		{
			if (con == null)
			{
				throw new ArgumentNullException("con");
			}
			bool result;
			using (SqlCommand sqlCommand = new SqlCommand("UPDATE NotificationItems SET Title=@Title, Description=@Description, CreatedAt=@CreatedAt, NotificationTypeID=@NotificationTypeID, \r\n                                               Url=@Url, Ignored=@Ignored, AcknowledgedAt=@AcknowledgedAt, AcknowledgedBy=@AcknowledgedBy\r\n                                               WHERE NotificationID=@NotificationID"))
			{
				sqlCommand.Parameters.AddWithValue("@NotificationID", this.Id);
				sqlCommand.Parameters.AddWithValue("@Title", this.Title);
				sqlCommand.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(this.Description) ? DBNull.Value : this.Description);
				sqlCommand.Parameters.AddWithValue("@CreatedAt", this.CreatedAt);
				sqlCommand.Parameters.AddWithValue("@Ignored", this.Ignored);
				sqlCommand.Parameters.AddWithValue("@NotificationTypeID", this.TypeId);
				sqlCommand.Parameters.AddWithValue("@Url", string.IsNullOrEmpty(this.Url) ? DBNull.Value : this.Url);
				sqlCommand.Parameters.AddWithValue("@AcknowledgedAt", (this.AcknowledgedAt != null) ? this.AcknowledgedAt.Value : DBNull.Value);
				sqlCommand.Parameters.AddWithValue("@AcknowledgedBy", string.IsNullOrEmpty(this.AcknowledgedBy) ? DBNull.Value : this.AcknowledgedBy);
				result = (SqlHelper.ExecuteNonQuery(sqlCommand, con, tr) > 0);
			}
			return result;
		}

		// Token: 0x06000837 RID: 2103 RVA: 0x0003A7F4 File Offset: 0x000389F4
		public bool Update()
		{
			bool result;
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
				{
					try
					{
						bool flag = this.Update(sqlConnection, sqlTransaction);
						sqlTransaction.Commit();
						result = flag;
					}
					catch (Exception ex)
					{
						sqlTransaction.Rollback();
						NotificationItemDAL.log.Error(string.Format("Can't UPDATE item of type {0}", base.GetType().Name) + ex.ToString());
						throw;
					}
				}
			}
			return result;
		}

		// Token: 0x06000838 RID: 2104 RVA: 0x0003A890 File Offset: 0x00038A90
		public static void Update(Guid notificationId, Guid typeId, string title, string description, bool ignored, string url, DateTime createdAt, DateTime? acknowledgedAt, string acknowledgedBy)
		{
			new NotificationItemDAL
			{
				Id = notificationId,
				TypeId = typeId,
				Title = title,
				Description = description,
				Ignored = ignored,
				Url = url,
				CreatedAt = createdAt,
				AcknowledgedAt = acknowledgedAt,
				AcknowledgedBy = acknowledgedBy
			}.Update();
		}

		// Token: 0x06000839 RID: 2105 RVA: 0x0003A8EC File Offset: 0x00038AEC
		private static TNotificationItem Insert<TNotificationItem>(SqlConnection con, SqlTransaction tr, Guid notificationId, Guid typeId, string title, string description, bool ignored, string url, DateTime? acknowledgedAt, string acknowledgedBy) where TNotificationItem : NotificationItemDAL, new()
		{
			TNotificationItem tnotificationItem = Activator.CreateInstance<TNotificationItem>();
			if (con == null)
			{
				throw new ArgumentNullException("con");
			}
			if (notificationId == Guid.Empty)
			{
				throw new ArgumentException("notificationId GUID can't be Guid.Empty", "notificationId");
			}
			if (string.IsNullOrEmpty(title))
			{
				throw new ArgumentNullException("title");
			}
			if (typeId == Guid.Empty)
			{
				typeId = tnotificationItem.GetNotificationItemTypeId();
				if (typeId == Guid.Empty)
				{
					throw new ArgumentException("Can't obtain Type GUID", "TNotificationItem");
				}
			}
			DateTime utcNow = DateTime.UtcNow;
			TNotificationItem tnotificationItem2;
			using (SqlCommand sqlCommand = new SqlCommand("INSERT INTO NotificationItems (NotificationID, Title, Description, CreatedAt, Ignored, NotificationTypeID, Url, AcknowledgedAt, AcknowledgedBy)\r\n                                                VALUES (@NotificationID, @Title, @Description, @CreatedAt, @Ignored, @NotificationTypeID, @Url, @AcknowledgedAt, @AcknowledgedBy)"))
			{
				sqlCommand.Parameters.AddWithValue("@NotificationID", notificationId);
				sqlCommand.Parameters.AddWithValue("@Title", title);
				sqlCommand.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(description) ? DBNull.Value : description);
				sqlCommand.Parameters.AddWithValue("@CreatedAt", utcNow);
				sqlCommand.Parameters.AddWithValue("@Ignored", ignored);
				sqlCommand.Parameters.AddWithValue("@NotificationTypeID", typeId);
				sqlCommand.Parameters.AddWithValue("@Url", string.IsNullOrEmpty(url) ? DBNull.Value : url);
				sqlCommand.Parameters.AddWithValue("@AcknowledgedAt", (acknowledgedAt != null) ? acknowledgedAt.Value : DBNull.Value);
				sqlCommand.Parameters.AddWithValue("@AcknowledgedBy", string.IsNullOrEmpty(acknowledgedBy) ? DBNull.Value : acknowledgedBy);
				if (SqlHelper.ExecuteNonQuery(sqlCommand, con, tr) == 0)
				{
					tnotificationItem2 = default(TNotificationItem);
					tnotificationItem2 = tnotificationItem2;
				}
				else
				{
					tnotificationItem.Id = notificationId;
					tnotificationItem.Title = title;
					tnotificationItem.Description = description;
					tnotificationItem.CreatedAt = utcNow;
					tnotificationItem.Ignored = ignored;
					tnotificationItem.TypeId = tnotificationItem.GetNotificationItemTypeId();
					tnotificationItem.Url = url;
					tnotificationItem.AcknowledgedAt = acknowledgedAt;
					tnotificationItem.AcknowledgedBy = acknowledgedBy;
					tnotificationItem2 = tnotificationItem;
				}
			}
			return tnotificationItem2;
		}

		// Token: 0x0600083A RID: 2106 RVA: 0x0003AB4C File Offset: 0x00038D4C
		protected static TNotificationItem Insert<TNotificationItem>(SqlConnection con, SqlTransaction tr, Guid notificationId, string title, string description, bool ignored, string url, DateTime? acknowledgedAt, string acknowledgedBy) where TNotificationItem : NotificationItemDAL, new()
		{
			return NotificationItemDAL.Insert<TNotificationItem>(con, tr, notificationId, Guid.Empty, title, description, ignored, url, acknowledgedAt, acknowledgedBy);
		}

		// Token: 0x0600083B RID: 2107 RVA: 0x0003AB74 File Offset: 0x00038D74
		protected static TNotificationItem Insert<TNotificationItem>(Guid notificationId, string title, string description, bool ignored, string url, DateTime? acknowledgedAt, string acknowledgedBy) where TNotificationItem : NotificationItemDAL, new()
		{
			TNotificationItem result;
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
				{
					try
					{
						TNotificationItem tnotificationItem = NotificationItemDAL.Insert<TNotificationItem>(sqlConnection, sqlTransaction, notificationId, title, description, ignored, url, acknowledgedAt, acknowledgedBy);
						sqlTransaction.Commit();
						result = tnotificationItem;
					}
					catch (Exception ex)
					{
						sqlTransaction.Rollback();
						NotificationItemDAL.log.Error(string.Format("Can't INSERT item of type {0}", typeof(TNotificationItem).Name) + ex.ToString());
						throw;
					}
				}
			}
			return result;
		}

		// Token: 0x0600083C RID: 2108 RVA: 0x0003AC1C File Offset: 0x00038E1C
		public static NotificationItemDAL Insert(Guid notificationId, Guid typeId, string title, string description, bool ignored, string url, DateTime? acknowledgedAt, string acknowledgedBy)
		{
			NotificationItemDAL result;
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
				{
					try
					{
						NotificationItemDAL notificationItemDAL = NotificationItemDAL.Insert<NotificationItemDAL>(sqlConnection, sqlTransaction, notificationId, typeId, title, description, ignored, url, acknowledgedAt, acknowledgedBy);
						sqlTransaction.Commit();
						result = notificationItemDAL;
					}
					catch (Exception ex)
					{
						sqlTransaction.Rollback();
						NotificationItemDAL.log.Error(string.Format("Can't INSERT item with ID {0}, typeId {1} ", notificationId, typeId) + ex.ToString());
						throw;
					}
				}
			}
			return result;
		}

		// Token: 0x0600083D RID: 2109 RVA: 0x0003ACC4 File Offset: 0x00038EC4
		private static bool Delete(SqlConnection con, SqlTransaction tr, Guid notificationId)
		{
			if (con == null)
			{
				throw new ArgumentNullException("con");
			}
			if (notificationId == Guid.Empty)
			{
				throw new ArgumentException("notificationId GUID can't be Guid.Empty", "notificationId");
			}
			bool result;
			using (SqlCommand sqlCommand = new SqlCommand("DELETE FROM NotificationItems WHERE NotificationID=@NotificationID"))
			{
				sqlCommand.Parameters.AddWithValue("@NotificationID", notificationId);
				result = (SqlHelper.ExecuteNonQuery(sqlCommand, con, tr) > 0);
			}
			return result;
		}

		// Token: 0x0600083E RID: 2110 RVA: 0x0003AD48 File Offset: 0x00038F48
		protected virtual bool Delete(SqlConnection con, SqlTransaction tr)
		{
			return NotificationItemDAL.Delete(con, tr, this.Id);
		}

		// Token: 0x0600083F RID: 2111 RVA: 0x0003AD58 File Offset: 0x00038F58
		public bool Delete()
		{
			bool result;
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
				{
					try
					{
						bool flag = this.Delete(sqlConnection, sqlTransaction);
						sqlTransaction.Commit();
						result = flag;
					}
					catch (Exception ex)
					{
						sqlTransaction.Rollback();
						NotificationItemDAL.log.Error(string.Format("Can't DELETE item of type {0}", base.GetType().Name) + ex.ToString());
						throw;
					}
				}
			}
			return result;
		}

		// Token: 0x06000840 RID: 2112 RVA: 0x0003ADF4 File Offset: 0x00038FF4
		public static bool Delete(Guid notificationId)
		{
			bool result;
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
				{
					try
					{
						bool flag = NotificationItemDAL.Delete(sqlConnection, sqlTransaction, notificationId);
						sqlTransaction.Commit();
						result = flag;
					}
					catch (Exception ex)
					{
						sqlTransaction.Rollback();
						NotificationItemDAL.log.Error(string.Format("Can't DELETE item with ID {0}", notificationId) + ex.ToString());
						throw;
					}
				}
			}
			return result;
		}

		// Token: 0x06000841 RID: 2113 RVA: 0x0003AE8C File Offset: 0x0003908C
		public static int GetNotificationsCount<TNotificationItem>(NotificationItemFilter filter) where TNotificationItem : NotificationItemDAL, new()
		{
			int result;
			using (SqlCommand sqlCommand = Activator.CreateInstance<TNotificationItem>().ComposeSelectCountCommand(filter))
			{
				object obj = SqlHelper.ExecuteScalar(sqlCommand);
				result = ((obj == null || obj == DBNull.Value) ? 0 : ((int)obj));
			}
			return result;
		}

		// Token: 0x06000842 RID: 2114 RVA: 0x0003AEE4 File Offset: 0x000390E4
		public static int GetNotificationsCountByType(Guid typeId, NotificationItemFilter filter)
		{
			int result;
			using (SqlCommand sqlCommand = new NotificationItemDAL().ComposeSelectCountCommand(typeId, filter))
			{
				object obj = SqlHelper.ExecuteScalar(sqlCommand);
				result = ((obj == null || obj == DBNull.Value) ? 0 : ((int)obj));
			}
			return result;
		}

		// Token: 0x06000843 RID: 2115 RVA: 0x0003AF38 File Offset: 0x00039138
		public static Dictionary<Guid, int> GetNotificationsCounts()
		{
			Dictionary<Guid, int> dictionary = new Dictionary<Guid, int>();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT NotificationTypeID, COUNT(NotificationID) as TheCount FROM NotificationItems WHERE AcknowledgedAt IS NULL AND Ignored=0 GROUP BY NotificationTypeID"))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						Guid guid = DatabaseFunctions.GetGuid(dataReader, 0);
						int @int = DatabaseFunctions.GetInt32(dataReader, 1);
						dictionary[guid] = @int;
					}
				}
			}
			return dictionary;
		}

		// Token: 0x06000844 RID: 2116 RVA: 0x0003AFB4 File Offset: 0x000391B4
		private static bool AcknowledgeItems(Guid? notificationId, Guid? typeId, string accountId, DateTime acknowledgedAt, DateTime createdBefore)
		{
			if (string.IsNullOrEmpty(accountId))
			{
				throw new ArgumentNullException("accountID");
			}
			if (acknowledgedAt == DateTime.MinValue)
			{
				throw new ArgumentException("Value has to be specified", "acknowledgedAt");
			}
			if (createdBefore == DateTime.MinValue)
			{
				throw new ArgumentException("Value has to be specified", "createdBefore");
			}
			StringBuilder stringBuilder = new StringBuilder("UPDATE NotificationItems SET AcknowledgedBy=@AccountID, AcknowledgedAt=@AcknowledgedAt \r\n                                                WHERE AcknowledgedAt IS NULL AND CreatedAt <= @CreatedBefore");
			bool result;
			using (SqlCommand sqlCommand = new SqlCommand())
			{
				sqlCommand.Parameters.AddWithValue("@AccountID", accountId);
				sqlCommand.Parameters.AddWithValue("@AcknowledgedAt", acknowledgedAt);
				sqlCommand.Parameters.AddWithValue("@CreatedBefore", createdBefore);
				if (notificationId != null)
				{
					stringBuilder.Append(" AND NotificationID=@NotificationID");
					sqlCommand.Parameters.AddWithValue("@NotificationID", notificationId.Value);
				}
				if (typeId != null)
				{
					stringBuilder.Append(" AND NotificationTypeID=@TypeID");
					sqlCommand.Parameters.AddWithValue("@TypeID", typeId.Value);
				}
				sqlCommand.CommandText = stringBuilder.ToString();
				result = (SqlHelper.ExecuteNonQuery(sqlCommand) > 0);
			}
			return result;
		}

		// Token: 0x06000845 RID: 2117 RVA: 0x0003B0F8 File Offset: 0x000392F8
		public static bool AcknowledgeItemsByType(Guid typeId, string accountId, DateTime createdBefore)
		{
			return NotificationItemDAL.AcknowledgeItems(null, new Guid?(typeId), accountId, DateTime.UtcNow, createdBefore);
		}

		// Token: 0x06000846 RID: 2118 RVA: 0x0003B120 File Offset: 0x00039320
		public static bool AcknowledgeItem(Guid notificationId, string accountId, DateTime acknowledgedAt, DateTime createdBefore)
		{
			if (notificationId == Guid.Empty)
			{
				throw new ArgumentException("notificationId GUID can't be Guid.Empty", "notificationId");
			}
			return NotificationItemDAL.AcknowledgeItems(new Guid?(notificationId), null, accountId, acknowledgedAt, createdBefore);
		}

		// Token: 0x06000847 RID: 2119 RVA: 0x0003B164 File Offset: 0x00039364
		public static bool AcknowledgeItem(Guid notificationId, string accountId, DateTime createdBefore)
		{
			if (notificationId == Guid.Empty)
			{
				throw new ArgumentException("notificationId GUID can't be Guid.Empty", "notificationId");
			}
			return NotificationItemDAL.AcknowledgeItems(new Guid?(notificationId), null, accountId, DateTime.UtcNow, createdBefore);
		}

		// Token: 0x06000848 RID: 2120 RVA: 0x0003B1AC File Offset: 0x000393AC
		public static bool AcknowledgeItems<TNotificationItem>(string accountId, DateTime acknowledgedAt, DateTime createdBefore) where TNotificationItem : NotificationItemDAL, new()
		{
			Guid notificationItemTypeId = Activator.CreateInstance<TNotificationItem>().GetNotificationItemTypeId();
			if (notificationItemTypeId == Guid.Empty)
			{
				throw new ArgumentException("Can't obtain Type GUID", "TNotificationItem");
			}
			return NotificationItemDAL.AcknowledgeItems(null, new Guid?(notificationItemTypeId), accountId, acknowledgedAt, createdBefore);
		}

		// Token: 0x06000849 RID: 2121 RVA: 0x0003B1FD File Offset: 0x000393FD
		public static bool AcknowledgeItems<TNotificationItem>(string accountId, DateTime createdBefore) where TNotificationItem : NotificationItemDAL, new()
		{
			return NotificationItemDAL.AcknowledgeItems<TNotificationItem>(accountId, DateTime.UtcNow, createdBefore);
		}

		// Token: 0x0600084A RID: 2122 RVA: 0x0003B20C File Offset: 0x0003940C
		public static bool AcknowledgeAllItems(string accountId, DateTime acknowledgedAt, DateTime createdBefore)
		{
			return NotificationItemDAL.AcknowledgeItems(null, null, accountId, acknowledgedAt, createdBefore);
		}

		// Token: 0x0600084B RID: 2123 RVA: 0x0003B234 File Offset: 0x00039434
		public static bool AcknowledgeAllItems(string accountId, DateTime createdBefore)
		{
			return NotificationItemDAL.AcknowledgeItems(null, null, accountId, DateTime.UtcNow, createdBefore);
		}

		// Token: 0x0600084C RID: 2124 RVA: 0x0003B260 File Offset: 0x00039460
		public static bool IgnoreItem(Guid notificationId)
		{
			if (notificationId == Guid.Empty)
			{
				throw new ArgumentException("notificationId GUID can't be Guid.Empty", "notificationId");
			}
			bool result;
			using (SqlCommand sqlCommand = new SqlCommand("UPDATE NotificationItems SET Ignored=1 WHERE NotificationID=@NotificationID"))
			{
				sqlCommand.Parameters.AddWithValue("@NotificationID", notificationId);
				result = (SqlHelper.ExecuteNonQuery(sqlCommand) > 0);
			}
			return result;
		}

		// Token: 0x0600084D RID: 2125 RVA: 0x0003B2D4 File Offset: 0x000394D4
		public static bool IgnoreItems(ICollection<Guid> notificationIds)
		{
			bool result = true;
			if (notificationIds != null && notificationIds.Count > 0)
			{
				using (IEnumerator<Guid> enumerator = notificationIds.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (!NotificationItemDAL.IgnoreItem(enumerator.Current))
						{
							result = false;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x04000256 RID: 598
		protected static readonly Log log = new Log();
	}
}
