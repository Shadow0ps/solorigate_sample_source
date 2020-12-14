using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Common.PackageManager;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x020000A3 RID: 163
	public class NodeChildStatusParticipationDAL
	{
		// Token: 0x06000801 RID: 2049 RVA: 0x000398A4 File Offset: 0x00037AA4
		public static void ResyncAfterStartup()
		{
			try
			{
				bool flag;
				NodeChildStatusParticipationDAL.UpdateParticipationFromInstalledProducts(out flag);
				if (flag)
				{
					NodeChildStatusParticipationDAL.ReflowAllNodeChildStatus();
				}
			}
			catch (Exception ex)
			{
				NodeChildStatusParticipationDAL.log.Error("Unhandled exception when reinitailizing node child status", ex);
			}
		}

		// Token: 0x06000802 RID: 2050 RVA: 0x000398E8 File Offset: 0x00037AE8
		public static void UpdateParticipationFromInstalledProducts(out bool needsreflow)
		{
			IEnumerable<PackageInfo> installedPackages = PackageManager.Instance.GetInstalledPackages();
			List<ModuleInfo> installedModules = ModulesCollector.GetInstalledModules();
			IEnumerable<string> values = from name in (from package in installedPackages
			select package.PackageId).Concat(from module in installedModules
			select module.ProductShortName)
			select '\'' + name.Replace("'", "''") + '\'';
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(new StringBuilder("UPDATE dbo.[NodeChildStatusParticipation] set Installed=0 Where ModuleName not in (").Append(string.Join(",", values)).Append(')').ToString()))
			{
				int num = SqlHelper.ExecuteNonQuery(textCommand);
				needsreflow = (num > 0);
			}
		}

		// Token: 0x06000803 RID: 2051 RVA: 0x000399D0 File Offset: 0x00037BD0
		private static SqlCommand MakeParticipationChangeQuery(Dictionary<string, bool> changes, bool value)
		{
			StringBuilder stringBuilder = new StringBuilder();
			SqlCommand sqlCommand = new SqlCommand();
			int num = 0;
			Func<KeyValuePair<string, bool>, bool> <>9__0;
			Func<KeyValuePair<string, bool>, bool> predicate;
			if ((predicate = <>9__0) == null)
			{
				predicate = (<>9__0 = ((KeyValuePair<string, bool> x) => x.Value == value));
			}
			foreach (string value2 in from x in changes.Where(predicate)
			select x.Key)
			{
				if (num == 0)
				{
					stringBuilder.AppendFormat("UPDATE dbo.NodeChildStatusParticipation set Enabled={0} WHERE Excluded=0 AND EntityType in (", value ? "1" : "0");
				}
				stringBuilder.AppendFormat("{0}@e{1}", (num == 0) ? "" : ",", num);
				sqlCommand.Parameters.AddWithValue("@e" + num.ToString(CultureInfo.InvariantCulture), value2);
				num++;
			}
			if (num != 0)
			{
				stringBuilder.Append(")");
				sqlCommand.CommandText = stringBuilder.ToString();
			}
			return sqlCommand;
		}

		// Token: 0x06000804 RID: 2052 RVA: 0x00039B04 File Offset: 0x00037D04
		public static void ReflowAllNodeChildStatus()
		{
			SqlHelper.ExecuteStoredProc("swsp_ReflowAllNodeChildStatus", Array.Empty<SqlParameter>());
		}

		// Token: 0x04000253 RID: 595
		private static readonly Log log = new Log();
	}
}
