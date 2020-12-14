using System;
using System.Data;
using System.Data.SqlClient;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x020000AD RID: 173
	internal static class WebMenubarDAL
	{
		// Token: 0x06000883 RID: 2179 RVA: 0x0003E260 File Offset: 0x0003C460
		public static int InsertItem(WebMenuItem item)
		{
			int result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("INSERT MenuItems (Title, Link, System, NewWindow, Description)\r\nVALUES (@title, @link, 'N', @newwindow, @description)\r\nSELECT scope_identity()"))
			{
				textCommand.Parameters.AddWithValue("title", item.Title);
				textCommand.Parameters.AddWithValue("link", item.Link);
				textCommand.Parameters.AddWithValue("newwindow", item.NewWindow ? "Y" : "N");
				textCommand.Parameters.AddWithValue("description", item.Description);
				result = Convert.ToInt32(SqlHelper.ExecuteScalar(textCommand));
			}
			return result;
		}

		// Token: 0x06000884 RID: 2180 RVA: 0x0003E30C File Offset: 0x0003C50C
		public static void AppendItemToMenu(string menuName, int itemId)
		{
			int num;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT MAX(Position) FROM MenuBars WHERE MenuName=@menu"))
			{
				textCommand.Parameters.AddWithValue("menu", menuName);
				num = Convert.ToInt32(SqlHelper.ExecuteScalar(textCommand));
			}
			num++;
			using (SqlCommand textCommand2 = SqlHelper.GetTextCommand("INSERT MenuBars (MenuName, MenuItemID, Position)\r\nVALUES (@menu, @item, @position)"))
			{
				textCommand2.Parameters.AddWithValue("menu", menuName);
				textCommand2.Parameters.AddWithValue("item", itemId);
				textCommand2.Parameters.AddWithValue("position", num);
				SqlHelper.ExecuteNonQuery(textCommand2);
			}
		}

		// Token: 0x06000885 RID: 2181 RVA: 0x0003E3CC File Offset: 0x0003C5CC
		public static bool MenuItemExists(string link)
		{
			bool result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT TOP 1 1 FROM [dbo].[MenuItems] WHERE Link = @link"))
			{
				textCommand.Parameters.AddWithValue("@link", link);
				object obj = SqlHelper.ExecuteScalar(textCommand);
				if (obj == DBNull.Value)
				{
					result = false;
				}
				else
				{
					result = Convert.ToBoolean(obj);
				}
			}
			return result;
		}

		// Token: 0x06000886 RID: 2182 RVA: 0x0003E430 File Offset: 0x0003C630
		private static WebMenuItem Create(IDataReader reader)
		{
			return new WebMenuItem
			{
				ID = (int)reader["MenuItemID"],
				Title = (string)reader["Title"],
				Link = (string)reader["Link"],
				NewWindow = ((string)reader["NewWindow"] == "Y"),
				Description = (string)reader["Description"]
			};
		}

		// Token: 0x06000887 RID: 2183 RVA: 0x0003E4BC File Offset: 0x0003C6BC
		internal static void DeleteItemByLink(string link)
		{
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("DELETE MenuBars FROM MenuBars, MenuItems\r\nWHERE MenuItems.Link=@link\r\nAND MenuBars.MenuItemID=MenuItems.MenuItemID\r\n\r\nDELETE MenuItems FROM MenuItems WHERE MenuItems.Link=@link"))
			{
				textCommand.Parameters.AddWithValue("link", link);
				SqlHelper.ExecuteNonQuery(textCommand);
			}
		}

		// Token: 0x06000888 RID: 2184 RVA: 0x0003E50C File Offset: 0x0003C70C
		internal static void RenameItemByLink(string newName, string newDescription, string newMenuBar, string link)
		{
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(" declare @position int\r\n\t\t\t\t\t\t\tdeclare @oldMenuBar varchar(200)\r\n\t\t\t\t\t\t\tdeclare @menuItemID int\r\n\r\n\t\t\t\t\t\t\tSELECT TOP 1 @oldMenuBar = MenuBars.MenuName, @menuItemID = MenuItems.MenuItemID FROM MenuBars\r\n\t\t\t\t\t\t\tINNER JOIN MenuItems ON MenuItems.MenuItemID = MenuBars.MenuItemID\r\n\t\t\t\t\t\t\tWHERE MenuItems.Link=@link\r\n\r\n\t\t\t\t\t\t\tIF @oldMenuBar = @menuName\r\n\t\t\t\t\t\t\t\tBEGIN\r\n\t\t\t\t\t\t\t\t\tUPDATE MenuItems SET Title=@title, Description=@description WHERE Link=@link\r\n\t\t\t\t\t\t\t\tEND\r\n\t\t\t\t\t\t\tELSE\r\n\t\t\t\t\t\t\t\tBEGIN\r\n\t\t\t\t\t\t\t\t\tSELECT @position = (SELECT MAX(Position) FROM MenuBars WHERE MenuName LIKE @menuName) + 1\r\n\r\n\t\t\t\t\t\t\t\t\tUPDATE MenuItems SET Title=@title, Description=@description WHERE Link=@link\r\n\t\t\t\t\t\t\t\t\tUPDATE MenuBars SET MenuName=@menuName, Position=@position WHERE MenuItemID=@menuItemID\r\n\t\t\t\t\t\t\t\tEND"))
			{
				textCommand.Parameters.AddWithValue("title", newName);
				textCommand.Parameters.AddWithValue("description", newDescription);
				textCommand.Parameters.AddWithValue("menuName", newMenuBar);
				textCommand.Parameters.AddWithValue("link", link);
				SqlHelper.ExecuteNonQuery(textCommand);
			}
		}

		// Token: 0x0200019E RID: 414
		private static class Fields
		{
			// Token: 0x04000551 RID: 1361
			public const string ID = "MenuItemID";

			// Token: 0x04000552 RID: 1362
			public const string Title = "Title";

			// Token: 0x04000553 RID: 1363
			public const string Link = "Link";

			// Token: 0x04000554 RID: 1364
			public const string NewWindow = "NewWindow";

			// Token: 0x04000555 RID: 1365
			public const string Description = "Description";
		}
	}
}
