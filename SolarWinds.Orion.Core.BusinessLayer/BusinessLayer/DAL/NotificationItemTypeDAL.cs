using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common.Enums;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Discovery.DataAccess;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x020000AF RID: 175
	public sealed class NotificationItemTypeDAL
	{
		// Token: 0x1700010F RID: 271
		// (get) Token: 0x06000893 RID: 2195 RVA: 0x0003E9D7 File Offset: 0x0003CBD7
		// (set) Token: 0x06000894 RID: 2196 RVA: 0x0003E9DF File Offset: 0x0003CBDF
		public Guid Id { get; set; }

		// Token: 0x17000110 RID: 272
		// (get) Token: 0x06000895 RID: 2197 RVA: 0x0003E9E8 File Offset: 0x0003CBE8
		// (set) Token: 0x06000896 RID: 2198 RVA: 0x0003E9F0 File Offset: 0x0003CBF0
		public string TypeName { get; set; }

		// Token: 0x17000111 RID: 273
		// (get) Token: 0x06000897 RID: 2199 RVA: 0x0003E9F9 File Offset: 0x0003CBF9
		// (set) Token: 0x06000898 RID: 2200 RVA: 0x0003EA01 File Offset: 0x0003CC01
		public string Module { get; set; }

		// Token: 0x17000112 RID: 274
		// (get) Token: 0x06000899 RID: 2201 RVA: 0x0003EA0A File Offset: 0x0003CC0A
		// (set) Token: 0x0600089A RID: 2202 RVA: 0x0003EA12 File Offset: 0x0003CC12
		public string Caption { get; set; }

		// Token: 0x17000113 RID: 275
		// (get) Token: 0x0600089B RID: 2203 RVA: 0x0003EA1B File Offset: 0x0003CC1B
		// (set) Token: 0x0600089C RID: 2204 RVA: 0x0003EA23 File Offset: 0x0003CC23
		public string DetailsUrl { get; set; }

		// Token: 0x17000114 RID: 276
		// (get) Token: 0x0600089D RID: 2205 RVA: 0x0003EA2C File Offset: 0x0003CC2C
		// (set) Token: 0x0600089E RID: 2206 RVA: 0x0003EA34 File Offset: 0x0003CC34
		public string DetailsCaption { get; set; }

		// Token: 0x17000115 RID: 277
		// (get) Token: 0x0600089F RID: 2207 RVA: 0x0003EA3D File Offset: 0x0003CC3D
		// (set) Token: 0x060008A0 RID: 2208 RVA: 0x0003EA45 File Offset: 0x0003CC45
		public string Icon { get; set; }

		// Token: 0x17000116 RID: 278
		// (get) Token: 0x060008A1 RID: 2209 RVA: 0x0003EA4E File Offset: 0x0003CC4E
		// (set) Token: 0x060008A2 RID: 2210 RVA: 0x0003EA56 File Offset: 0x0003CC56
		public string Description { get; set; }

		// Token: 0x17000117 RID: 279
		// (get) Token: 0x060008A3 RID: 2211 RVA: 0x0003EA5F File Offset: 0x0003CC5F
		// (set) Token: 0x060008A4 RID: 2212 RVA: 0x0003EA67 File Offset: 0x0003CC67
		public List<NotificationItemType.Roles> RequiredRoles { get; private set; }

		// Token: 0x17000118 RID: 280
		// (get) Token: 0x060008A5 RID: 2213 RVA: 0x0003EA70 File Offset: 0x0003CC70
		// (set) Token: 0x060008A6 RID: 2214 RVA: 0x0003EA78 File Offset: 0x0003CC78
		public NotificationTypeDisplayAs DisplayAs { get; set; }

		// Token: 0x17000119 RID: 281
		// (get) Token: 0x060008A7 RID: 2215 RVA: 0x0003EA81 File Offset: 0x0003CC81
		// (set) Token: 0x060008A8 RID: 2216 RVA: 0x0003EA89 File Offset: 0x0003CC89
		public string CustomDismissButtonText { get; set; }

		// Token: 0x1700011A RID: 282
		// (get) Token: 0x060008A9 RID: 2217 RVA: 0x0003EA92 File Offset: 0x0003CC92
		// (set) Token: 0x060008AA RID: 2218 RVA: 0x0003EA9A File Offset: 0x0003CC9A
		public bool HideDismissButton { get; set; }

		// Token: 0x060008AB RID: 2219 RVA: 0x0003EAA4 File Offset: 0x0003CCA4
		private NotificationItemTypeDAL()
		{
			this.Id = Guid.Empty;
			this.TypeName = string.Empty;
			this.Module = string.Empty;
			this.Caption = string.Empty;
			this.DetailsUrl = string.Empty;
			this.DetailsCaption = string.Empty;
			this.Icon = string.Empty;
			this.Description = string.Empty;
			this.DisplayAs = NotificationTypeDisplayAs.Caption;
			this.RequiredRoles = new List<NotificationItemType.Roles>();
		}

		// Token: 0x060008AC RID: 2220 RVA: 0x0003EB24 File Offset: 0x0003CD24
		public static NotificationItemTypeDAL GetTypeById(Guid typeId)
		{
			NotificationItemTypeDAL result;
			try
			{
				result = NotificationItemTypeDAL.GetTypes().FirstOrDefault((NotificationItemTypeDAL x) => x.Id == typeId);
			}
			catch (ResultCountException)
			{
				NotificationItemTypeDAL.log.DebugFormat("Can't find notification item type in database: ID={0}", typeId);
				result = null;
			}
			return result;
		}

		// Token: 0x060008AD RID: 2221 RVA: 0x0003EB88 File Offset: 0x0003CD88
		public static ICollection<NotificationItemTypeDAL> GetTypes()
		{
			List<NotificationItemTypeDAL> cachedTypes = NotificationItemTypeDAL._cachedTypes;
			if (cachedTypes != null)
			{
				return cachedTypes;
			}
			NotificationItemTypeDAL._cachedTypes = NotificationItemTypeDAL.LoadAllTypes();
			return NotificationItemTypeDAL._cachedTypes;
		}

		// Token: 0x060008AE RID: 2222 RVA: 0x0003EBB0 File Offset: 0x0003CDB0
		private static List<NotificationItemTypeDAL> LoadAllTypes()
		{
			List<NotificationItemTypeDAL> list = NotificationItemTypeDAL.LoadCollectionFromDB();
			Dictionary<Guid, List<NotificationItemType.Roles>> dictionary = NotificationItemTypeDAL.LoadRolesFromDB();
			foreach (NotificationItemTypeDAL notificationItemTypeDAL in list)
			{
				if (dictionary.ContainsKey(notificationItemTypeDAL.Id))
				{
					notificationItemTypeDAL.RequiredRoles = dictionary[notificationItemTypeDAL.Id];
				}
			}
			return list;
		}

		// Token: 0x060008AF RID: 2223 RVA: 0x0003EC24 File Offset: 0x0003CE24
		private static List<NotificationItemTypeDAL> LoadCollectionFromDB()
		{
			List<NotificationItemTypeDAL> result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT * FROM NotificationItemTypes"))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					List<NotificationItemTypeDAL> list = new List<NotificationItemTypeDAL>();
					while (dataReader.Read())
					{
						NotificationItemTypeDAL notificationItemTypeDAL = new NotificationItemTypeDAL();
						notificationItemTypeDAL.LoadFromReader(dataReader);
						list.Add(notificationItemTypeDAL);
					}
					result = list;
				}
			}
			return result;
		}

		// Token: 0x060008B0 RID: 2224 RVA: 0x0003ECA0 File Offset: 0x0003CEA0
		private void LoadFromReader(IDataReader reader)
		{
			this.Id = reader.GetGuid(reader.GetOrdinal("TypeID"));
			this.TypeName = reader.GetString(reader.GetOrdinal("TypeName"));
			this.Module = reader.GetString(reader.GetOrdinal("Module"));
			this.Caption = DatabaseFunctions.GetString(reader, "Caption");
			this.DetailsUrl = DatabaseFunctions.GetString(reader, "DetailsUrl");
			this.DetailsCaption = DatabaseFunctions.GetString(reader, "DetailsCaption");
			this.Icon = DatabaseFunctions.GetString(reader, "Icon");
			this.Description = DatabaseFunctions.GetString(reader, "Description");
			this.CustomDismissButtonText = DatabaseFunctions.GetString(reader, "CustomDismissButtonText");
			this.HideDismissButton = DatabaseFunctions.GetBoolean(reader, "HideDismissButton");
			this.DisplayAs = SqlHelper.ParseEnum<NotificationTypeDisplayAs>(reader.GetString(reader.GetOrdinal("DisplayAs")));
		}

		// Token: 0x060008B1 RID: 2225 RVA: 0x0003ED88 File Offset: 0x0003CF88
		private static Dictionary<Guid, List<NotificationItemType.Roles>> LoadRolesFromDB()
		{
			Dictionary<Guid, List<NotificationItemType.Roles>> dictionary = new Dictionary<Guid, List<NotificationItemType.Roles>>();
			Dictionary<Guid, List<NotificationItemType.Roles>> result;
			using (SqlCommand sqlCommand = new SqlCommand("SELECT NotificationTypeID, RequiredRoleID FROM NotificationTypePermissions"))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(sqlCommand))
				{
					while (dataReader.Read())
					{
						Guid guid = DatabaseFunctions.GetGuid(dataReader, 0);
						int @int = DatabaseFunctions.GetInt32(dataReader, 1);
						if (!dictionary.ContainsKey(guid))
						{
							dictionary[guid] = new List<NotificationItemType.Roles>();
						}
						dictionary[guid].Add((NotificationItemType.Roles)@int);
					}
					result = dictionary;
				}
			}
			return result;
		}

		// Token: 0x04000268 RID: 616
		private static readonly Log log = new Log();

		// Token: 0x04000269 RID: 617
		private static List<NotificationItemTypeDAL> _cachedTypes;
	}
}
