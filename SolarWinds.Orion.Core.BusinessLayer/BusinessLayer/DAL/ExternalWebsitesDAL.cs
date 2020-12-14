using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Common.Swis;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x0200009A RID: 154
	internal static class ExternalWebsitesDAL
	{
		// Token: 0x06000790 RID: 1936 RVA: 0x00034520 File Offset: 0x00032720
		public static ExternalWebsite Get(int id)
		{
			return Collection<int, ExternalWebsite>.GetCollectionItem<ExternalWebsites>(new Collection<int, ExternalWebsite>.CreateElement(ExternalWebsitesDAL.Create), "SELECT * FROM ExternalWebsites WHERE ExternalWebsiteID=@site", new SqlParameter[]
			{
				new SqlParameter("@site", id)
			});
		}

		// Token: 0x06000791 RID: 1937 RVA: 0x00034551 File Offset: 0x00032751
		public static ExternalWebsites GetAll()
		{
			return Collection<int, ExternalWebsite>.FillCollection<ExternalWebsites>(new Collection<int, ExternalWebsite>.CreateElement(ExternalWebsitesDAL.Create), "SELECT * FROM ExternalWebsites", Array.Empty<SqlParameter>());
		}

		// Token: 0x06000792 RID: 1938 RVA: 0x00034570 File Offset: 0x00032770
		public static void Delete(int id)
		{
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("DELETE FROM ExternalWebsites WHERE ExternalWebsiteID=@site"))
			{
				textCommand.Parameters.AddWithValue("site", id);
				SqlHelper.ExecuteNonQuery(textCommand);
			}
			ExternalWebsitesDAL.ClearMenuCache();
		}

		// Token: 0x06000793 RID: 1939 RVA: 0x000345C8 File Offset: 0x000327C8
		public static int Insert(ExternalWebsite site)
		{
			int result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("INSERT ExternalWebsites (ShortTitle, FullTitle, URL) VALUES (@short, @full, @url)\nSELECT scope_identity()"))
			{
				ExternalWebsitesDAL.AddParams(textCommand, site);
				result = Convert.ToInt32(SqlHelper.ExecuteScalar(textCommand));
			}
			ExternalWebsitesDAL.ClearMenuCache();
			return result;
		}

		// Token: 0x06000794 RID: 1940 RVA: 0x00034618 File Offset: 0x00032818
		public static void Update(ExternalWebsite site)
		{
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("UPDATE ExternalWebsites SET ShortTitle=@short, FullTitle=@full, URL=@url WHERE ExternalWebsiteID=@site"))
			{
				ExternalWebsitesDAL.AddParams(textCommand, site);
				SqlHelper.ExecuteNonQuery(textCommand);
			}
			ExternalWebsitesDAL.ClearMenuCache();
		}

		// Token: 0x06000795 RID: 1941 RVA: 0x00034660 File Offset: 0x00032860
		private static void AddParams(SqlCommand command, ExternalWebsite site)
		{
			command.Parameters.AddWithValue("site", site.ID);
			command.Parameters.AddWithValue("short", site.ShortTitle);
			command.Parameters.AddWithValue("full", site.FullTitle);
			command.Parameters.AddWithValue("url", site.URL);
		}

		// Token: 0x06000796 RID: 1942 RVA: 0x000346D0 File Offset: 0x000328D0
		private static ExternalWebsite Create(IDataReader reader)
		{
			return new ExternalWebsite
			{
				ID = (int)reader["ExternalWebsiteID"],
				ShortTitle = (string)reader["ShortTitle"],
				FullTitle = (string)reader["FullTitle"],
				URL = (string)reader["URL"]
			};
		}

		// Token: 0x06000797 RID: 1943 RVA: 0x0003473C File Offset: 0x0003293C
		private static void ClearMenuCache()
		{
			try
			{
				using (SwisConnectionProxy swisConnectionProxy = ExternalWebsitesDAL._swisConnectionProxyFactory.Value.CreateConnection())
				{
					swisConnectionProxy.Invoke<object>("Orion.Web.Menu", "ClearCache", Array.Empty<object>());
				}
			}
			catch (Exception ex)
			{
				ExternalWebsitesDAL._log.Warn("Could not clear Orion.Web.Menu cache in $ExternalWebsitesDAL.", ex);
			}
		}

		// Token: 0x04000244 RID: 580
		private static readonly Log _log = new Log();

		// Token: 0x04000245 RID: 581
		private static readonly Lazy<ISwisConnectionProxyFactory> _swisConnectionProxyFactory = new Lazy<ISwisConnectionProxyFactory>(() => new SwisConnectionProxyFactory(), LazyThreadSafetyMode.PublicationOnly);

		// Token: 0x02000197 RID: 407
		private static class Fields
		{
			// Token: 0x04000511 RID: 1297
			public const string ID = "ExternalWebsiteID";

			// Token: 0x04000512 RID: 1298
			public const string ShortTitle = "ShortTitle";

			// Token: 0x04000513 RID: 1299
			public const string FullTitle = "FullTitle";

			// Token: 0x04000514 RID: 1300
			public const string URL = "URL";
		}
	}
}
