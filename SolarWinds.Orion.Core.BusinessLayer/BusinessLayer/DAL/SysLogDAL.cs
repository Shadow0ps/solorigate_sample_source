using System;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using SolarWinds.Orion.Common;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x020000AA RID: 170
	internal class SysLogDAL
	{
		// Token: 0x0600086C RID: 2156 RVA: 0x0003C664 File Offset: 0x0003A864
		public static StringDictionary GetSeverities()
		{
			StringDictionary stringDictionary = new StringDictionary();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("Select SeverityCode, SeverityName From SysLogSeverities WITH(NOLOCK) Order By SeverityCode"))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						stringDictionary.Add(DatabaseFunctions.GetByte(dataReader, "SeverityCode").ToString(), DatabaseFunctions.GetString(dataReader, "SeverityName"));
					}
				}
			}
			return stringDictionary;
		}

		// Token: 0x0600086D RID: 2157 RVA: 0x0003C6EC File Offset: 0x0003A8EC
		public static StringDictionary GetFacilities()
		{
			StringDictionary stringDictionary = new StringDictionary();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("Select FacilityCode, FacilityName From SysLogFacilities WITH(NOLOCK) Order By FacilityCode"))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						stringDictionary.Add(DatabaseFunctions.GetByte(dataReader, "FacilityCode").ToString(), DatabaseFunctions.GetString(dataReader, "FacilityName"));
					}
				}
			}
			return stringDictionary;
		}
	}
}
