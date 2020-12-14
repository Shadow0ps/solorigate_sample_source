using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000097 RID: 151
	public sealed class BlogItemDAL : NotificationItemDAL
	{
		// Token: 0x170000F8 RID: 248
		// (get) Token: 0x06000764 RID: 1892 RVA: 0x00032F85 File Offset: 0x00031185
		// (set) Token: 0x06000765 RID: 1893 RVA: 0x00032F8D File Offset: 0x0003118D
		public Guid PostGuid { get; set; }

		// Token: 0x170000F9 RID: 249
		// (get) Token: 0x06000766 RID: 1894 RVA: 0x00032F96 File Offset: 0x00031196
		// (set) Token: 0x06000767 RID: 1895 RVA: 0x00032F9E File Offset: 0x0003119E
		public long PostId { get; set; }

		// Token: 0x170000FA RID: 250
		// (get) Token: 0x06000768 RID: 1896 RVA: 0x00032FA7 File Offset: 0x000311A7
		// (set) Token: 0x06000769 RID: 1897 RVA: 0x00032FAF File Offset: 0x000311AF
		public string Owner { get; set; }

		// Token: 0x170000FB RID: 251
		// (get) Token: 0x0600076A RID: 1898 RVA: 0x00032FB8 File Offset: 0x000311B8
		// (set) Token: 0x0600076B RID: 1899 RVA: 0x00032FC0 File Offset: 0x000311C0
		public DateTime PublicationDate { get; set; }

		// Token: 0x170000FC RID: 252
		// (get) Token: 0x0600076C RID: 1900 RVA: 0x00032FC9 File Offset: 0x000311C9
		// (set) Token: 0x0600076D RID: 1901 RVA: 0x00032FD1 File Offset: 0x000311D1
		public string CommentsUrl { get; set; }

		// Token: 0x170000FD RID: 253
		// (get) Token: 0x0600076E RID: 1902 RVA: 0x00032FDA File Offset: 0x000311DA
		// (set) Token: 0x0600076F RID: 1903 RVA: 0x00032FE2 File Offset: 0x000311E2
		public int CommentsCount { get; set; }

		// Token: 0x06000770 RID: 1904 RVA: 0x00032FEC File Offset: 0x000311EC
		public BlogItemDAL()
		{
			this.PostGuid = Guid.Empty;
			this.PostId = 0L;
			this.Owner = string.Empty;
			this.PublicationDate = DateTime.MinValue;
			this.CommentsUrl = string.Empty;
			this.CommentsCount = 0;
		}

		// Token: 0x06000771 RID: 1905 RVA: 0x0003303A File Offset: 0x0003123A
		protected override Guid GetNotificationItemTypeId()
		{
			return BlogItem.BlogTypeGuid;
		}

		// Token: 0x06000772 RID: 1906 RVA: 0x00033044 File Offset: 0x00031244
		protected override SqlCommand ComposeSelectCollectionCommand(NotificationItemFilter filter)
		{
			SqlCommand sqlCommand = new SqlCommand("SELECT{0} * FROM NotificationBlogs LEFT JOIN NotificationItems ON \r\n                                         NotificationBlogs.BlogID = NotificationItems.NotificationID");
			SqlCommand result;
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				if (!filter.IncludeAcknowledged)
				{
					SqlHelper.AddCondition(stringBuilder, "AcknowledgedAt IS NULL", "AND");
				}
				if (!filter.IncludeIgnored)
				{
					SqlHelper.AddCondition(stringBuilder, "Ignored=0", "AND");
				}
				BlogFilter blogFilter = filter as BlogFilter;
				if (blogFilter != null && blogFilter.MaxResults > 0)
				{
					sqlCommand.CommandText = string.Format(sqlCommand.CommandText, " TOP " + blogFilter.MaxResults);
				}
				else
				{
					sqlCommand.CommandText = string.Format(sqlCommand.CommandText, string.Empty);
				}
				SqlCommand sqlCommand2 = sqlCommand;
				sqlCommand2.CommandText += stringBuilder.ToString();
				SqlCommand sqlCommand3 = sqlCommand;
				sqlCommand3.CommandText += " ORDER BY PublicationDate DESC";
				result = sqlCommand;
			}
			catch (Exception ex)
			{
				sqlCommand.Dispose();
				BlogItemDAL.log.Error(string.Format("Error while composing SELECT SQL command for {0} collection: ", base.GetType().Name) + ex.ToString());
				throw;
			}
			return result;
		}

		// Token: 0x06000773 RID: 1907 RVA: 0x0003315C File Offset: 0x0003135C
		protected override SqlCommand ComposeSelectItemCommand()
		{
			SqlCommand sqlCommand = new SqlCommand("SELECT * FROM NotificationBlogs LEFT JOIN NotificationItems ON \r\n                                         NotificationBlogs.BlogID = NotificationItems.NotificationID \r\n                                       WHERE BlogID=@BlogID");
			SqlCommand result;
			try
			{
				sqlCommand.Parameters.AddWithValue("@BlogID", base.Id);
				result = sqlCommand;
			}
			catch (Exception ex)
			{
				sqlCommand.Dispose();
				BlogItemDAL.log.Error(string.Format("Error while composing SELECT SQL command for {0}: ", base.GetType().Name) + ex.ToString());
				throw;
			}
			return result;
		}

		// Token: 0x06000774 RID: 1908 RVA: 0x000331D8 File Offset: 0x000313D8
		protected override SqlCommand ComposeSelectLatestItemCommand(NotificationItemFilter filter)
		{
			SqlCommand sqlCommand = new SqlCommand("SELECT TOP 1 * FROM NotificationBlogs LEFT JOIN NotificationItems ON \r\n                                         NotificationBlogs.BlogID = NotificationItems.NotificationID");
			SqlCommand result;
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
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
				sqlCommand3.CommandText += " ORDER BY PublicationDate DESC";
				result = sqlCommand;
			}
			catch (Exception ex)
			{
				sqlCommand.Dispose();
				BlogItemDAL.log.Error(string.Format("Error while composing SELECT SQL command for latest {0}: ", base.GetType().Name) + ex.ToString());
				throw;
			}
			return result;
		}

		// Token: 0x06000775 RID: 1909 RVA: 0x0003329C File Offset: 0x0003149C
		protected override SqlCommand ComposeSelectCountCommand(NotificationItemFilter filter)
		{
			SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(BlogID) FROM NotificationBlogs LEFT JOIN NotificationItems ON \r\n                                         NotificationBlogs.BlogID = NotificationItems.NotificationID");
			SqlCommand result;
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
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
				BlogItemDAL.log.Error(string.Format("Error while composing SELECT COUNT SQL command for {0}: ", base.GetType().Name) + ex.ToString());
				throw;
			}
			return result;
		}

		// Token: 0x06000776 RID: 1910 RVA: 0x0003334C File Offset: 0x0003154C
		public static BlogItemDAL GetItemById(Guid itemId)
		{
			return NotificationItemDAL.GetItemById<BlogItemDAL>(itemId);
		}

		// Token: 0x06000777 RID: 1911 RVA: 0x00033354 File Offset: 0x00031554
		public static BlogItemDAL GetLatestItem()
		{
			return NotificationItemDAL.GetLatestItem<BlogItemDAL>(new NotificationItemFilter(false, false));
		}

		// Token: 0x06000778 RID: 1912 RVA: 0x00033362 File Offset: 0x00031562
		public static ICollection<BlogItemDAL> GetItems(BlogFilter filter)
		{
			return NotificationItemDAL.GetItems<BlogItemDAL>(filter);
		}

		// Token: 0x06000779 RID: 1913 RVA: 0x0003336A File Offset: 0x0003156A
		public static int GetNotificationsCount()
		{
			return NotificationItemDAL.GetNotificationsCount<BlogItemDAL>(new NotificationItemFilter());
		}

		// Token: 0x0600077A RID: 1914 RVA: 0x00033376 File Offset: 0x00031576
		public static BlogItemDAL GetBlogItemForPost(Guid postGuid, long postId)
		{
			return BlogItemDAL.GetBlogItemForPost(postGuid, postId, null);
		}

		// Token: 0x0600077B RID: 1915 RVA: 0x00033380 File Offset: 0x00031580
		private static BlogItemDAL GetBlogItemForPost(Guid postGuid, long postId, SqlConnection connection)
		{
			BlogItemDAL result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT * FROM NotificationBlogs LEFT JOIN NotificationItems ON NotificationBlogs.BlogID=NotificationItems.NotificationID \r\n                                 WHERE PostGUID=@PostGUID AND PostID=@PostID"))
			{
				textCommand.Parameters.AddWithValue("@PostGUID", postGuid);
				textCommand.Parameters.AddWithValue("@PostID", postId);
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand, connection))
				{
					if (dataReader.Read())
					{
						BlogItemDAL blogItemDAL = new BlogItemDAL();
						blogItemDAL.LoadFromReader(dataReader);
						result = blogItemDAL;
					}
					else
					{
						result = null;
					}
				}
			}
			return result;
		}

		// Token: 0x0600077C RID: 1916 RVA: 0x0003341C File Offset: 0x0003161C
		public static void StoreBlogItems(List<BlogItemDAL> blogItems, int targetBlogsCount)
		{
			if (targetBlogsCount < 0)
			{
				throw new ArgumentOutOfRangeException("targetBlogsCount", targetBlogsCount, "Should be >= 0");
			}
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				List<BlogItemDAL> list = new List<BlogItemDAL>();
				List<BlogItemDAL> list2 = new List<BlogItemDAL>();
				foreach (BlogItemDAL blogItemDAL in blogItems)
				{
					BlogItemDAL blogItemForPost = BlogItemDAL.GetBlogItemForPost(blogItemDAL.PostGuid, blogItemDAL.PostId, sqlConnection);
					if (blogItemForPost != null)
					{
						blogItemForPost.Title = blogItemDAL.Title;
						blogItemForPost.Description = blogItemDAL.Description;
						blogItemForPost.Url = blogItemDAL.Url;
						blogItemForPost.Owner = blogItemDAL.Owner;
						blogItemForPost.PublicationDate = blogItemDAL.PublicationDate;
						blogItemForPost.CommentsUrl = blogItemDAL.CommentsUrl;
						blogItemForPost.CommentsCount = blogItemDAL.CommentsCount;
						list.Add(blogItemForPost);
					}
					else
					{
						list2.Add(blogItemDAL);
					}
				}
				using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction(IsolationLevel.Serializable))
				{
					try
					{
						foreach (BlogItemDAL blogItemDAL2 in list)
						{
							blogItemDAL2.Update(sqlConnection, sqlTransaction);
						}
						foreach (BlogItemDAL blogItemDAL3 in list2)
						{
							BlogItemDAL.Insert(sqlConnection, sqlTransaction, Guid.NewGuid(), blogItemDAL3.Title, blogItemDAL3.Description, false, blogItemDAL3.Url, null, null, blogItemDAL3.PostGuid, blogItemDAL3.PostId, blogItemDAL3.Owner, blogItemDAL3.PublicationDate, blogItemDAL3.CommentsUrl, blogItemDAL3.CommentsCount);
						}
						using (SqlCommand sqlCommand = new SqlCommand(string.Format("DELETE FROM NotificationBlogs WHERE BlogID NOT IN (SELECT TOP {0} BlogID FROM NotificationBlogs ORDER BY PublicationDate DESC)", targetBlogsCount)))
						{
							SqlHelper.ExecuteNonQuery(sqlCommand, sqlConnection, sqlTransaction);
						}
						using (SqlCommand sqlCommand2 = new SqlCommand("DELETE FROM NotificationItems WHERE NotificationTypeID=@TypeID AND NotificationID NOT IN (SELECT BlogID FROM NotificationBlogs)"))
						{
							sqlCommand2.Parameters.AddWithValue("@TypeID", BlogItem.BlogTypeGuid);
							SqlHelper.ExecuteNonQuery(sqlCommand2, sqlConnection, sqlTransaction);
						}
						sqlTransaction.Commit();
					}
					catch
					{
						sqlTransaction.Rollback();
						throw;
					}
				}
			}
		}

		// Token: 0x0600077D RID: 1917 RVA: 0x00033738 File Offset: 0x00031938
		private static BlogItemDAL Insert(SqlConnection con, SqlTransaction tr, Guid blogId, string title, string description, bool ignored, string url, DateTime? acknowledgedAt, string acknowledgedBy, Guid postGuid, long postId, string owner, DateTime publicationDate, string commentsUrl, int commentsCount)
		{
			if (tr == null)
			{
				throw new ArgumentNullException("tr");
			}
			if (postGuid == Guid.Empty)
			{
				throw new ArgumentException("postGuid GUID can't be Guid.Empty", "postGuid");
			}
			if (publicationDate == DateTime.MinValue)
			{
				throw new ArgumentNullException("publicationDate");
			}
			BlogItemDAL blogItemDAL = NotificationItemDAL.Insert<BlogItemDAL>(con, tr, blogId, title, description, ignored, url, acknowledgedAt, acknowledgedBy);
			if (blogItemDAL == null)
			{
				return null;
			}
			BlogItemDAL result;
			using (SqlCommand sqlCommand = new SqlCommand("INSERT INTO NotificationBlogs (BlogID, PostGUID, PostID, Owner, PublicationDate, CommentsUrl, CommentsCount)\r\n                                              VALUES (@BlogID, @PostGUID, @PostID, @Owner, @PublicationDate, @CommentsUrl, @CommentsCount)"))
			{
				sqlCommand.Parameters.AddWithValue("@BlogID", blogId);
				sqlCommand.Parameters.AddWithValue("@PostGUID", postGuid);
				sqlCommand.Parameters.AddWithValue("@PostID", postId);
				sqlCommand.Parameters.AddWithValue("@Owner", string.IsNullOrEmpty(owner) ? DBNull.Value : owner);
				sqlCommand.Parameters.AddWithValue("@PublicationDate", publicationDate);
				sqlCommand.Parameters.AddWithValue("@CommentsUrl", string.IsNullOrEmpty(commentsUrl) ? DBNull.Value : commentsUrl);
				sqlCommand.Parameters.AddWithValue("@CommentsCount", (commentsCount < 0) ? DBNull.Value : commentsCount);
				if (SqlHelper.ExecuteNonQuery(sqlCommand, con, tr) == 0)
				{
					result = null;
				}
				else
				{
					blogItemDAL.PostGuid = postGuid;
					blogItemDAL.PostId = postId;
					blogItemDAL.Owner = owner;
					blogItemDAL.PublicationDate = publicationDate;
					blogItemDAL.CommentsUrl = commentsUrl;
					blogItemDAL.CommentsCount = commentsCount;
					result = blogItemDAL;
				}
			}
			return result;
		}

		// Token: 0x0600077E RID: 1918 RVA: 0x000338E0 File Offset: 0x00031AE0
		public static BlogItemDAL Insert(Guid blogId, string title, string description, bool ignored, string url, DateTime? acknowledgedAt, string acknowledgedBy, Guid postGuid, long postId, string owner, DateTime publicationDate, string commentsUrl, int commentsCount)
		{
			BlogItemDAL result;
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
				{
					try
					{
						BlogItemDAL blogItemDAL = BlogItemDAL.Insert(sqlConnection, sqlTransaction, blogId, title, description, ignored, url, acknowledgedAt, acknowledgedBy, postGuid, postId, owner, publicationDate, commentsUrl, commentsCount);
						sqlTransaction.Commit();
						result = blogItemDAL;
					}
					catch (Exception ex)
					{
						sqlTransaction.Rollback();
						BlogItemDAL.log.Error(string.Format("Can't INSERT blog item: ", Array.Empty<object>()) + ex.ToString());
						throw;
					}
				}
			}
			return result;
		}

		// Token: 0x0600077F RID: 1919 RVA: 0x0003398C File Offset: 0x00031B8C
		protected override bool Update(SqlConnection con, SqlTransaction tr)
		{
			if (!base.Update(con, tr))
			{
				return false;
			}
			bool result;
			using (SqlCommand sqlCommand = new SqlCommand("UPDATE NotificationBlogs SET PostGUID=@PostGUID, PostID=@PostID, Owner=@Owner, PublicationDate=@PublicationDate, \r\n                                              CommentsUrl=@CommentsUrl, CommentsCount=@CommentsCount WHERE BlogID=@BlogID"))
			{
				sqlCommand.Parameters.AddWithValue("@BlogID", base.Id);
				sqlCommand.Parameters.AddWithValue("@PostGUID", this.PostGuid);
				sqlCommand.Parameters.AddWithValue("@PostID", this.PostId);
				sqlCommand.Parameters.AddWithValue("@Owner", string.IsNullOrEmpty(this.Owner) ? DBNull.Value : this.Owner);
				sqlCommand.Parameters.AddWithValue("@PublicationDate", this.PublicationDate);
				sqlCommand.Parameters.AddWithValue("@CommentsUrl", string.IsNullOrEmpty(this.CommentsUrl) ? DBNull.Value : this.CommentsUrl);
				sqlCommand.Parameters.AddWithValue("@CommentsCount", (this.CommentsCount < 0) ? DBNull.Value : this.CommentsCount);
				result = (SqlHelper.ExecuteNonQuery(sqlCommand, con, tr) > 0);
			}
			return result;
		}

		// Token: 0x06000780 RID: 1920 RVA: 0x00033AD8 File Offset: 0x00031CD8
		protected override bool Delete(SqlConnection con, SqlTransaction tr)
		{
			if (!base.Delete(con, tr))
			{
				return false;
			}
			bool result;
			using (SqlCommand sqlCommand = new SqlCommand("DELETE FROM NotificationBlogs WHERE BlogID=@BlogID"))
			{
				sqlCommand.Parameters.AddWithValue("@BlogID", base.Id);
				result = (SqlHelper.ExecuteNonQuery(sqlCommand, con, tr) > 0);
			}
			return result;
		}

		// Token: 0x06000781 RID: 1921 RVA: 0x00033B44 File Offset: 0x00031D44
		protected override void LoadFromReader(IDataReader rd)
		{
			base.LoadFromReader(rd);
			this.PostGuid = DatabaseFunctions.GetGuid(rd, "PostGUID");
			this.PostId = DatabaseFunctions.GetLong(rd, "PostID");
			this.Owner = DatabaseFunctions.GetString(rd, "Owner");
			this.PublicationDate = DatabaseFunctions.GetDateTime(rd, "PublicationDate");
			this.CommentsUrl = DatabaseFunctions.GetString(rd, "CommentsUrl");
			this.CommentsCount = DatabaseFunctions.GetInt32(rd, "CommentsCount");
		}

		// Token: 0x0400023D RID: 573
		private new static readonly Log log = new Log();
	}
}
