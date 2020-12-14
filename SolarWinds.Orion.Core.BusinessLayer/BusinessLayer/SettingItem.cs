using System;
using System.Data.SqlClient;
using SolarWinds.Orion.Common;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200003C RID: 60
	internal class SettingItem : SynchronizeItem
	{
		// Token: 0x06000401 RID: 1025 RVA: 0x0001BA31 File Offset: 0x00019C31
		public SettingItem(string settingID) : this(settingID, SettingItem.ColumnType.CurrentValue)
		{
		}

		// Token: 0x06000402 RID: 1026 RVA: 0x0001BA3B File Offset: 0x00019C3B
		public SettingItem(string settingID, SettingItem.ColumnType columnType)
		{
			this.SettingID = settingID;
			this.Column = columnType;
		}

		// Token: 0x06000403 RID: 1027 RVA: 0x0001BA54 File Offset: 0x00019C54
		public override object GetDatabaseValue()
		{
			object result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Format("SELECT {0} FROM Settings WHERE SettingID=@SettingID", this.Column.ToString())))
			{
				textCommand.Parameters.AddWithValue("SettingID", this.SettingID);
				result = SqlHelper.ExecuteScalar(textCommand);
			}
			return result;
		}

		// Token: 0x06000404 RID: 1028 RVA: 0x0001BAC0 File Offset: 0x00019CC0
		public override string ToString()
		{
			return string.Format("{0}/{1}", this.SettingID, this.Column);
		}

		// Token: 0x040000E7 RID: 231
		public SettingItem.ColumnType Column;

		// Token: 0x040000E8 RID: 232
		public string SettingID;

		// Token: 0x02000142 RID: 322
		internal enum ColumnType
		{
			// Token: 0x04000413 RID: 1043
			CurrentValue,
			// Token: 0x04000414 RID: 1044
			Description
		}
	}
}
