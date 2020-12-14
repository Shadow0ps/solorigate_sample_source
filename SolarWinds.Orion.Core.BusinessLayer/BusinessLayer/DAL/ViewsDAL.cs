using System;
using System.Data;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x020000AB RID: 171
	public class ViewsDAL
	{
		// Token: 0x0600086F RID: 2159 RVA: 0x0003C774 File Offset: 0x0003A974
		public static Views GetSummaryDetailsViews()
		{
			string commandString = "SELECT * FROM Views WHERE (NOT(ViewType LIKE 'Volume%')) AND ((ViewType LIKE 'Summary') OR (ViewType LIKE '%Details'))";
			return Collection<int, WebView>.FillCollection<Views>(new Collection<int, WebView>.CreateElement(ViewsDAL.CreateView), commandString, null);
		}

		// Token: 0x06000870 RID: 2160 RVA: 0x0003C79C File Offset: 0x0003A99C
		private static WebView CreateView(IDataReader reader)
		{
			return new WebView
			{
				ViewType = reader["ViewType"].ToString().Trim(),
				ViewID = Convert.ToInt32(reader["ViewID"]),
				ViewTitle = reader["ViewTitle"].ToString().Trim(),
				ViewGroupName = reader["ViewGroupName"].ToString().Trim()
			};
		}

		// Token: 0x04000264 RID: 612
		private static readonly Log log = new Log();
	}
}
