using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000098 RID: 152
	public class DependencyDAL
	{
		// Token: 0x06000783 RID: 1923 RVA: 0x00033BCC File Offset: 0x00031DCC
		public static IList<Dependency> GetAllDependencies()
		{
			IList<Dependency> list = new List<Dependency>();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT * FROM [dbo].[Dependencies]"))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						list.Add(DependencyDAL.CreateDependency(dataReader));
					}
				}
			}
			return list;
		}

		// Token: 0x06000784 RID: 1924 RVA: 0x00033C3C File Offset: 0x00031E3C
		public static Dependency GetDependency(int id)
		{
			Dependency result = null;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT * FROM [dbo].[Dependencies] WHERE DependencyId = @id"))
			{
				textCommand.Parameters.AddWithValue("@id", id);
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					if (dataReader.Read())
					{
						result = DependencyDAL.CreateDependency(dataReader);
					}
				}
			}
			return result;
		}

		// Token: 0x06000785 RID: 1925 RVA: 0x00033CB8 File Offset: 0x00031EB8
		public static void SaveDependency(Dependency dependency)
		{
			if (dependency != null)
			{
				SqlParameter[] array = new SqlParameter[]
				{
					new SqlParameter("@DependencyId", dependency.Id),
					new SqlParameter("@Name", dependency.Name),
					new SqlParameter("@ParentUri", dependency.ParentUri),
					new SqlParameter("@ChildUri", dependency.ChildUri),
					new SqlParameter("@AutoManaged", dependency.AutoManaged),
					new SqlParameter("@EngineID", dependency.EngineID),
					new SqlParameter("@Category", dependency.Category)
				};
				using (IDataReader dataReader = SqlHelper.ExecuteStoredProcReader("swsp_DependencyUpsert", array))
				{
					if (dataReader != null && !dataReader.IsClosed && dataReader.Read())
					{
						dependency.Id = dataReader.GetInt32(0);
						dependency.LastUpdateUTC = dataReader.GetDateTime(1);
					}
				}
			}
		}

		// Token: 0x06000786 RID: 1926 RVA: 0x00033DC0 File Offset: 0x00031FC0
		public static void DeleteDependency(Dependency dependency)
		{
			if (dependency != null)
			{
				using (SqlCommand textCommand = SqlHelper.GetTextCommand("\r\nSET NOCOUNT OFF;\r\nDelete FROM [dbo].[Dependencies]\r\n WHERE DependencyId = @id"))
				{
					textCommand.Parameters.AddWithValue("@id", dependency.Id);
					SqlHelper.ExecuteNonQuery(textCommand);
				}
			}
		}

		// Token: 0x06000787 RID: 1927 RVA: 0x00033E1C File Offset: 0x0003201C
		public static int DeleteDependencies(List<int> listIds)
		{
			if (listIds.Count == 0)
			{
				return 0;
			}
			string arg = string.Empty;
			string arg2 = string.Empty;
			foreach (int num in listIds)
			{
				arg = string.Format("{0}{1}'{2}'", arg, arg2, num);
				arg2 = ", ";
			}
			int result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(string.Format("\r\nSET NOCOUNT OFF;\r\nDelete FROM [dbo].[Dependencies]\r\n WHERE DependencyId in ({0})", arg)))
			{
				result = SqlHelper.ExecuteNonQuery(textCommand);
			}
			return result;
		}

		// Token: 0x06000788 RID: 1928 RVA: 0x00033ECC File Offset: 0x000320CC
		private static Dependency CreateDependency(IDataReader reader)
		{
			Dependency result = null;
			if (reader != null)
			{
				result = new Dependency
				{
					Id = reader.GetInt32(reader.GetOrdinal("DependencyId")),
					Name = reader.GetString(reader.GetOrdinal("Name")),
					ParentUri = reader.GetString(reader.GetOrdinal("ParentUri")),
					ChildUri = reader.GetString(reader.GetOrdinal("ChildUri")),
					LastUpdateUTC = reader.GetDateTime(reader.GetOrdinal("LastUpdateUTC")),
					AutoManaged = reader.GetBoolean(reader.GetOrdinal("AutoManaged")),
					EngineID = reader.GetInt32(reader.GetOrdinal("EngineID")),
					Category = reader.GetInt32(reader.GetOrdinal("Category"))
				};
			}
			return result;
		}
	}
}
