using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Extensions;
using SolarWinds.Orion.Core.Common.InformationService;
using SolarWinds.Orion.Core.Models.MaintenanceMode;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000083 RID: 131
	public class MaintenanceModePlanDAL : IMaintenanceModePlanDAL
	{
		// Token: 0x06000675 RID: 1653 RVA: 0x00026900 File Offset: 0x00024B00
		internal static Dictionary<string, object> RemoveKeysFromDictionary(Dictionary<string, object> source, params string[] keysToRemove)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if (keysToRemove == null)
			{
				throw new ArgumentNullException("keysToRemove");
			}
			return (from kvp in source
			where !keysToRemove.Contains(kvp.Key)
			select kvp).ToDictionary((KeyValuePair<string, object> kvp) => kvp.Key, (KeyValuePair<string, object> kvp) => kvp.Value);
		}

		// Token: 0x06000676 RID: 1654 RVA: 0x00026990 File Offset: 0x00024B90
		internal static T GetValue<T>(DataRow dataRow, string columnName, Func<object, T> convertFunction, T defaultValue)
		{
			if (!dataRow.Table.Columns.Contains(columnName) || dataRow[columnName] == DBNull.Value)
			{
				return defaultValue;
			}
			return convertFunction(dataRow[columnName]);
		}

		// Token: 0x06000677 RID: 1655 RVA: 0x000269C4 File Offset: 0x00024BC4
		internal static MaintenancePlan DataRowToPlan(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException("dataRow");
			}
			int id = Convert.ToInt32(dataRow["ID"]);
			string value = MaintenanceModePlanDAL.GetValue<string>(dataRow, "AccountID", new Func<object, string>(Convert.ToString), null);
			string value2 = MaintenanceModePlanDAL.GetValue<string>(dataRow, "Name", new Func<object, string>(Convert.ToString), null);
			string value3 = MaintenanceModePlanDAL.GetValue<string>(dataRow, "Reason", new Func<object, string>(Convert.ToString), null);
			bool value4 = MaintenanceModePlanDAL.GetValue<bool>(dataRow, "KeepPolling", new Func<object, bool>(Convert.ToBoolean), false);
			bool value5 = MaintenanceModePlanDAL.GetValue<bool>(dataRow, "Favorite", new Func<object, bool>(Convert.ToBoolean), false);
			bool value6 = MaintenanceModePlanDAL.GetValue<bool>(dataRow, "Enabled", new Func<object, bool>(Convert.ToBoolean), false);
			DateTime value7 = MaintenanceModePlanDAL.GetValue<DateTime>(dataRow, "UnmanageDate", new Func<object, DateTime>(Convert.ToDateTime), DateTime.MinValue);
			DateTime value8 = MaintenanceModePlanDAL.GetValue<DateTime>(dataRow, "RemanageDate", new Func<object, DateTime>(Convert.ToDateTime), DateTime.MinValue);
			return new MaintenancePlan
			{
				AccountID = value,
				Enabled = value6,
				Favorite = value5,
				ID = id,
				KeepPolling = value4,
				Name = value2,
				Reason = value3,
				RemanageDate = value8,
				UnmanageDate = value7
			};
		}

		// Token: 0x170000F4 RID: 244
		// (get) Token: 0x06000678 RID: 1656 RVA: 0x00026B0E File Offset: 0x00024D0E
		// (set) Token: 0x06000679 RID: 1657 RVA: 0x00026B29 File Offset: 0x00024D29
		public IInformationServiceProxyCreator SwisFactory
		{
			get
			{
				if (this._SwisFactory == null)
				{
					this._SwisFactory = new InformationServiceProxyFactory();
				}
				return this._SwisFactory;
			}
			set
			{
				this._SwisFactory = value;
			}
		}

		// Token: 0x0600067A RID: 1658 RVA: 0x00026B34 File Offset: 0x00024D34
		public string Create(MaintenancePlan plan)
		{
			string result;
			using (IInformationServiceProxy2 informationServiceProxy = this.SwisFactory.Create())
			{
				Dictionary<string, object> dictionary = MaintenanceModePlanDAL.RemoveKeysFromDictionary(plan.ToDictionary<MaintenancePlan>(), new string[]
				{
					"ID"
				});
				result = informationServiceProxy.Create("Orion.MaintenancePlan", dictionary);
			}
			return result;
		}

		// Token: 0x0600067B RID: 1659 RVA: 0x00026B94 File Offset: 0x00024D94
		public void Update(string entityUri, MaintenancePlan plan)
		{
			using (IInformationServiceProxy2 informationServiceProxy = this.SwisFactory.Create())
			{
				Dictionary<string, object> dictionary = MaintenanceModePlanDAL.RemoveKeysFromDictionary(plan.ToDictionary<MaintenancePlan>(), new string[]
				{
					"ID"
				});
				informationServiceProxy.Update(entityUri, dictionary);
			}
		}

		// Token: 0x0600067C RID: 1660 RVA: 0x00026BEC File Offset: 0x00024DEC
		public MaintenancePlan Get(string entityUri)
		{
			using (IInformationServiceProxy2 informationServiceProxy = this.SwisFactory.Create())
			{
				DataTable dataTable = informationServiceProxy.Query("\r\n                SELECT TOP 1 ID, AccountID, Name, Reason, KeepPolling, Favorite, Enabled, UnmanageDate, RemanageDate\r\n                FROM Orion.MaintenancePlan\r\n                WHERE Uri = @EntityUri", new Dictionary<string, object>
				{
					{
						"EntityUri",
						entityUri
					}
				});
				if (dataTable != null && dataTable.Rows.Count == 1)
				{
					return MaintenanceModePlanDAL.DataRowToPlan(dataTable.Rows.Cast<DataRow>().FirstOrDefault<DataRow>());
				}
			}
			return null;
		}

		// Token: 0x0600067D RID: 1661 RVA: 0x00026C6C File Offset: 0x00024E6C
		public MaintenancePlan Get(int planID)
		{
			using (IInformationServiceProxy2 informationServiceProxy = this.SwisFactory.Create())
			{
				DataTable dataTable = informationServiceProxy.Query("\r\n                SELECT TOP 1 ID, AccountID, Name, Reason, KeepPolling, Favorite, Enabled, UnmanageDate, RemanageDate\r\n                FROM Orion.MaintenancePlan\r\n                WHERE ID = @PlanID", new Dictionary<string, object>
				{
					{
						"PlanID",
						planID
					}
				});
				if (dataTable != null && dataTable.Rows.Count == 1)
				{
					return MaintenanceModePlanDAL.DataRowToPlan(dataTable.Rows.Cast<DataRow>().FirstOrDefault<DataRow>());
				}
			}
			return null;
		}

		// Token: 0x0400020B RID: 523
		private const string entityName = "Orion.MaintenancePlan";

		// Token: 0x0400020C RID: 524
		private static readonly Log log = new Log();

		// Token: 0x0400020D RID: 525
		private IInformationServiceProxyCreator _SwisFactory;
	}
}
