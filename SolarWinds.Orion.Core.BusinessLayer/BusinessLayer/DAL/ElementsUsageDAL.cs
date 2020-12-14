using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x0200008A RID: 138
	internal class ElementsUsageDAL
	{
		// Token: 0x060006C7 RID: 1735 RVA: 0x0002B06C File Offset: 0x0002926C
		public static void Save(IEnumerable<ElementLicenseSaturationInfo> saturationInfoCollection)
		{
			if (saturationInfoCollection == null)
			{
				throw new ArgumentNullException("saturationInfoCollection");
			}
			if (!saturationInfoCollection.Any<ElementLicenseSaturationInfo>())
			{
				return;
			}
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				using (SqlCommand textCommand = SqlHelper.GetTextCommand(" IF NOT EXISTS (SELECT 1 FROM [dbo].[ElementUsage_Daily] WHERE Date = @Date AND ElementType = @ElementType)\r\n                INSERT INTO [dbo].[ElementUsage_Daily] (Date, ElementType, Count, MaxCount) VALUES (@Date, @ElementType, @Count, @MaxCount)"))
				{
					textCommand.Parameters.Add("@Date", SqlDbType.Date);
					textCommand.Parameters.Add("@ElementType", SqlDbType.NVarChar);
					textCommand.Parameters.Add("@Count", SqlDbType.Int);
					textCommand.Parameters.Add("@MaxCount", SqlDbType.Int);
					foreach (ElementLicenseSaturationInfo elementLicenseSaturationInfo in saturationInfoCollection)
					{
						textCommand.Parameters["@Date"].Value = DateTime.UtcNow.Date;
						textCommand.Parameters["@ElementType"].Value = elementLicenseSaturationInfo.ElementType;
						textCommand.Parameters["@Count"].Value = elementLicenseSaturationInfo.Count;
						textCommand.Parameters["@MaxCount"].Value = elementLicenseSaturationInfo.MaxCount;
						SqlHelper.ExecuteNonQuery(textCommand, sqlConnection);
					}
				}
			}
		}
	}
}
