using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x020000B1 RID: 177
	public sealed class MaintenanceRenewalItemDAL : NotificationItemDAL
	{
		// Token: 0x1700011C RID: 284
		// (get) Token: 0x060008B7 RID: 2231 RVA: 0x0003EE5D File Offset: 0x0003D05D
		// (set) Token: 0x060008B8 RID: 2232 RVA: 0x0003EE65 File Offset: 0x0003D065
		public string ProductTag { get; set; }

		// Token: 0x1700011D RID: 285
		// (get) Token: 0x060008B9 RID: 2233 RVA: 0x0003EE6E File Offset: 0x0003D06E
		// (set) Token: 0x060008BA RID: 2234 RVA: 0x0003EE76 File Offset: 0x0003D076
		public DateTime DateReleased { get; set; }

		// Token: 0x1700011E RID: 286
		// (get) Token: 0x060008BB RID: 2235 RVA: 0x0003EE7F File Offset: 0x0003D07F
		// (set) Token: 0x060008BC RID: 2236 RVA: 0x0003EE87 File Offset: 0x0003D087
		public string NewVersion { get; set; }

		// Token: 0x060008BD RID: 2237 RVA: 0x0003EE90 File Offset: 0x0003D090
		public MaintenanceRenewalItemDAL()
		{
			this.ProductTag = string.Empty;
			this.DateReleased = DateTime.MinValue;
			this.NewVersion = string.Empty;
		}

		// Token: 0x060008BE RID: 2238 RVA: 0x0003EEB9 File Offset: 0x0003D0B9
		protected override Guid GetNotificationItemTypeId()
		{
			return MaintenanceRenewalItem.MaintenanceRenewalsTypeGuid;
		}

		// Token: 0x060008BF RID: 2239 RVA: 0x0003EEC0 File Offset: 0x0003D0C0
		protected override SqlCommand ComposeSelectCollectionCommand(NotificationItemFilter filter)
		{
			SqlCommand sqlCommand = new SqlCommand("SELECT * FROM NotificationMaintenanceRenewals LEFT JOIN NotificationItems ON \r\n                                         NotificationMaintenanceRenewals.RenewalID = NotificationItems.NotificationID");
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
				MaintenanceRenewalFilter maintenanceRenewalFilter = filter as MaintenanceRenewalFilter;
				if (maintenanceRenewalFilter != null && !string.IsNullOrEmpty(maintenanceRenewalFilter.ProductTag))
				{
					SqlHelper.AddCondition(stringBuilder, "ProductTag=@ProductTag", "AND");
					sqlCommand.Parameters.AddWithValue("@ProductTag", maintenanceRenewalFilter.ProductTag);
				}
				SqlCommand sqlCommand2 = sqlCommand;
				sqlCommand2.CommandText += stringBuilder.ToString();
				SqlCommand sqlCommand3 = sqlCommand;
				sqlCommand3.CommandText += " ORDER BY DateReleased DESC";
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

		// Token: 0x060008C0 RID: 2240 RVA: 0x0003EFC4 File Offset: 0x0003D1C4
		protected override SqlCommand ComposeSelectItemCommand()
		{
			SqlCommand sqlCommand = new SqlCommand("SELECT * FROM NotificationMaintenanceRenewals LEFT JOIN NotificationItems ON \r\n                                         NotificationMaintenanceRenewals.RenewalID = NotificationItems.NotificationID \r\n                                       WHERE RenewalID=@RenewalID");
			SqlCommand result;
			try
			{
				sqlCommand.Parameters.AddWithValue("@RenewalID", base.Id);
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

		// Token: 0x060008C1 RID: 2241 RVA: 0x0003F040 File Offset: 0x0003D240
		protected override SqlCommand ComposeSelectLatestItemCommand(NotificationItemFilter filter)
		{
			SqlCommand sqlCommand = new SqlCommand("SELECT TOP 1 * FROM NotificationMaintenanceRenewals LEFT JOIN NotificationItems ON \r\n                                         NotificationMaintenanceRenewals.RenewalID = NotificationItems.NotificationID");
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
				MaintenanceRenewalFilter maintenanceRenewalFilter = filter as MaintenanceRenewalFilter;
				if (maintenanceRenewalFilter != null && !string.IsNullOrEmpty(maintenanceRenewalFilter.ProductTag))
				{
					SqlHelper.AddCondition(stringBuilder, "ProductTag=@ProductTag", "AND");
					sqlCommand.Parameters.AddWithValue("@ProductTag", maintenanceRenewalFilter.ProductTag);
				}
				SqlCommand sqlCommand2 = sqlCommand;
				sqlCommand2.CommandText += stringBuilder.ToString();
				SqlCommand sqlCommand3 = sqlCommand;
				sqlCommand3.CommandText += " ORDER BY DateReleased DESC";
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

		// Token: 0x060008C2 RID: 2242 RVA: 0x0003F144 File Offset: 0x0003D344
		protected override SqlCommand ComposeSelectCountCommand(NotificationItemFilter filter)
		{
			SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(RenewalID) FROM NotificationMaintenanceRenewals LEFT JOIN NotificationItems ON \r\n                                         NotificationMaintenanceRenewals.RenewalID = NotificationItems.NotificationID");
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
				MaintenanceRenewalFilter maintenanceRenewalFilter = filter as MaintenanceRenewalFilter;
				if (maintenanceRenewalFilter != null && !string.IsNullOrEmpty(maintenanceRenewalFilter.ProductTag))
				{
					SqlHelper.AddCondition(stringBuilder, "ProductTag=@ProductTag", "AND");
					sqlCommand.Parameters.AddWithValue("@ProductTag", maintenanceRenewalFilter.ProductTag);
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

		// Token: 0x060008C3 RID: 2243 RVA: 0x0003F234 File Offset: 0x0003D434
		public static MaintenanceRenewalItemDAL GetItemById(Guid itemId)
		{
			return NotificationItemDAL.GetItemById<MaintenanceRenewalItemDAL>(itemId);
		}

		// Token: 0x060008C4 RID: 2244 RVA: 0x0003F23C File Offset: 0x0003D43C
		public static MaintenanceRenewalItemDAL GetLatestItem(NotificationItemFilter filter)
		{
			return NotificationItemDAL.GetLatestItem<MaintenanceRenewalItemDAL>(filter);
		}

		// Token: 0x060008C5 RID: 2245 RVA: 0x0003F244 File Offset: 0x0003D444
		public static ICollection<MaintenanceRenewalItemDAL> GetItems(MaintenanceRenewalFilter filter)
		{
			return NotificationItemDAL.GetItems<MaintenanceRenewalItemDAL>(filter);
		}

		// Token: 0x060008C6 RID: 2246 RVA: 0x0003F24C File Offset: 0x0003D44C
		public static int GetNotificationsCount()
		{
			return NotificationItemDAL.GetNotificationsCount<MaintenanceRenewalItemDAL>(new NotificationItemFilter());
		}

		// Token: 0x060008C7 RID: 2247 RVA: 0x0003F258 File Offset: 0x0003D458
		public static MaintenanceRenewalItemDAL GetItemForProduct(string productTag)
		{
			if (string.IsNullOrEmpty(productTag))
			{
				throw new ArgumentNullException("productTag");
			}
			MaintenanceRenewalItemDAL result;
			using (SqlCommand sqlCommand = new SqlCommand("SELECT * FROM NotificationMaintenanceRenewals LEFT JOIN NotificationItems ON \r\n                                                NotificationMaintenanceRenewals.RenewalID = NotificationItems.NotificationID\r\n                                              WHERE ProductTag=@ProductTag"))
			{
				sqlCommand.Parameters.AddWithValue("@ProductTag", productTag);
				using (IDataReader dataReader = SqlHelper.ExecuteReader(sqlCommand))
				{
					if (dataReader.Read())
					{
						MaintenanceRenewalItemDAL maintenanceRenewalItemDAL = new MaintenanceRenewalItemDAL();
						maintenanceRenewalItemDAL.LoadFromReader(dataReader);
						result = maintenanceRenewalItemDAL;
					}
					else
					{
						result = null;
					}
				}
			}
			return result;
		}

		// Token: 0x060008C8 RID: 2248 RVA: 0x0003F2E8 File Offset: 0x0003D4E8
		private static MaintenanceRenewalItemDAL Insert(SqlConnection con, SqlTransaction tr, Guid renewalId, string title, string description, bool ignored, string url, DateTime? acknowledgedAt, string acknowledgedBy, string productTag, DateTime dateReleased, string newVersion)
		{
			if (tr == null)
			{
				throw new ArgumentNullException("tr");
			}
			if (string.IsNullOrEmpty(productTag))
			{
				throw new ArgumentNullException("productTag");
			}
			if (dateReleased == DateTime.MinValue)
			{
				throw new ArgumentNullException("dateReleased");
			}
			if (string.IsNullOrEmpty(newVersion))
			{
				throw new ArgumentNullException("newVersion");
			}
			MaintenanceRenewalItemDAL maintenanceRenewalItemDAL = NotificationItemDAL.Insert<MaintenanceRenewalItemDAL>(con, tr, renewalId, title, description, ignored, url, acknowledgedAt, acknowledgedBy);
			if (maintenanceRenewalItemDAL == null)
			{
				return null;
			}
			using (SqlCommand sqlCommand = new SqlCommand("INSERT INTO NotificationMaintenanceRenewals (RenewalID, ProductTag, DateReleased, Version) VALUES (@RenewalID, @ProductTag, @DateReleased, @NewVersion)"))
			{
				sqlCommand.Parameters.AddWithValue("@RenewalID", renewalId);
				sqlCommand.Parameters.AddWithValue("@ProductTag", productTag);
				sqlCommand.Parameters.AddWithValue("@DateReleased", dateReleased);
				sqlCommand.Parameters.AddWithValue("@NewVersion", newVersion);
				if (SqlHelper.ExecuteNonQuery(sqlCommand, con, tr) == 0)
				{
					maintenanceRenewalItemDAL = null;
				}
				else
				{
					maintenanceRenewalItemDAL.ProductTag = productTag;
					maintenanceRenewalItemDAL.DateReleased = dateReleased;
					maintenanceRenewalItemDAL.NewVersion = newVersion;
				}
			}
			return maintenanceRenewalItemDAL;
		}

		// Token: 0x060008C9 RID: 2249 RVA: 0x0003F400 File Offset: 0x0003D600
		public static MaintenanceRenewalItemDAL Insert(Guid renewalId, string title, string description, bool ignored, string url, DateTime? acknowledgedAt, string acknowledgedBy, string productTag, DateTime dateReleased, string newVersion)
		{
			MaintenanceRenewalItemDAL result;
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
				{
					try
					{
						MaintenanceRenewalItemDAL maintenanceRenewalItemDAL = MaintenanceRenewalItemDAL.Insert(sqlConnection, sqlTransaction, renewalId, title, description, ignored, url, acknowledgedAt, acknowledgedBy, productTag, dateReleased, newVersion);
						sqlTransaction.Commit();
						result = maintenanceRenewalItemDAL;
					}
					catch (Exception ex)
					{
						sqlTransaction.Rollback();
						NotificationItemDAL.log.Error(string.Format("Can't INSERT maintenance renewal item: ", Array.Empty<object>()) + ex.ToString());
						throw;
					}
				}
			}
			return result;
		}

		// Token: 0x060008CA RID: 2250 RVA: 0x0003F4A4 File Offset: 0x0003D6A4
		protected override bool Update(SqlConnection con, SqlTransaction tr)
		{
			if (!base.Update(con, tr))
			{
				return false;
			}
			bool result;
			using (SqlCommand sqlCommand = new SqlCommand("UPDATE NotificationMaintenanceRenewals SET ProductTag=@ProductTag, DateReleased=@DateReleased, Version=@NewVersion WHERE RenewalID=@RenewalID"))
			{
				sqlCommand.Parameters.AddWithValue("@RenewalID", base.Id);
				sqlCommand.Parameters.AddWithValue("@ProductTag", this.ProductTag);
				sqlCommand.Parameters.AddWithValue("@DateReleased", this.DateReleased);
				sqlCommand.Parameters.AddWithValue("@NewVersion", this.NewVersion);
				result = (SqlHelper.ExecuteNonQuery(sqlCommand, con, tr) > 0);
			}
			return result;
		}

		// Token: 0x060008CB RID: 2251 RVA: 0x0003F558 File Offset: 0x0003D758
		protected override bool Delete(SqlConnection con, SqlTransaction tr)
		{
			if (!base.Delete(con, tr))
			{
				return false;
			}
			bool result;
			using (SqlCommand sqlCommand = new SqlCommand("DELETE FROM NotificationMaintenanceRenewals WHERE RenewalID=@RenewalID"))
			{
				sqlCommand.Parameters.AddWithValue("@RenewalID", base.Id);
				result = (SqlHelper.ExecuteNonQuery(sqlCommand, con, tr) > 0);
			}
			return result;
		}

		// Token: 0x060008CC RID: 2252 RVA: 0x0003F5C4 File Offset: 0x0003D7C4
		protected override void LoadFromReader(IDataReader rd)
		{
			base.LoadFromReader(rd);
			this.ProductTag = DatabaseFunctions.GetString(rd, "ProductTag");
			this.DateReleased = DatabaseFunctions.GetDateTime(rd, "DateReleased");
			this.NewVersion = DatabaseFunctions.GetString(rd, "Version");
		}
	}
}
