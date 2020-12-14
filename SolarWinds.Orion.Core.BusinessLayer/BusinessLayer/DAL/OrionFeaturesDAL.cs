using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Common.Data;
using SolarWinds.Orion.Core.Models.OrionFeature;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000085 RID: 133
	internal class OrionFeaturesDAL : IOrionFeaturesDAL
	{
		// Token: 0x06000682 RID: 1666 RVA: 0x00026CFC File Offset: 0x00024EFC
		public IEnumerable<OrionFeature> GetItems()
		{
			return EntityHydrator.PopulateCollectionFromSql<OrionFeature>("SELECT Name, Enabled FROM OrionFeatures");
		}

		// Token: 0x06000683 RID: 1667 RVA: 0x00026D08 File Offset: 0x00024F08
		public void Update(IEnumerable<OrionFeature> features)
		{
			using (SqlConnection sqlConnection = DatabaseFunctions.CreateConnection())
			{
				using (SqlTransaction sqlTransaction = sqlConnection.BeginTransaction())
				{
					SqlHelper.ExecuteNonQuery(SqlHelper.GetTextCommand("TRUNCATE TABLE OrionFeatures"), sqlConnection, sqlTransaction);
					using (EnumerableDataReader<OrionFeature> enumerableDataReader = new EnumerableDataReader<OrionFeature>(new SinglePropertyAccessor<OrionFeature>().AddColumn("Name", (OrionFeature n) => n.Name).AddColumn("Enabled", (OrionFeature n) => n.Enabled), features))
					{
						SqlHelper.ExecuteBulkCopy("OrionFeatures", enumerableDataReader, sqlConnection, sqlTransaction, SqlBulkCopyOptions.Default);
					}
					sqlTransaction.Commit();
				}
			}
		}
	}
}
