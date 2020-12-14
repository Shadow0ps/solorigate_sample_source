using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common.Models.Mib;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x0200008E RID: 142
	internal class MibDAL
	{
		// Token: 0x170000F5 RID: 245
		// (get) Token: 0x060006DB RID: 1755 RVA: 0x0002B8A4 File Offset: 0x00029AA4
		// (set) Token: 0x060006DC RID: 1756 RVA: 0x0002B8E4 File Offset: 0x00029AE4
		private static CancellationTokenSource CancellationTokenSource
		{
			get
			{
				object tokenLock = MibDAL._tokenLock;
				CancellationTokenSource cancellationTokenSource;
				lock (tokenLock)
				{
					cancellationTokenSource = MibDAL._cancellationTokenSource;
				}
				return cancellationTokenSource;
			}
			set
			{
				object tokenLock = MibDAL._tokenLock;
				lock (tokenLock)
				{
					MibDAL._cancellationTokenSource = value;
				}
			}
		}

		// Token: 0x060006DD RID: 1757 RVA: 0x0002B924 File Offset: 0x00029B24
		public Oid GetOid(string oid)
		{
			Oid oid2 = this.GetOid(oid, true);
			if (oid2 == null)
			{
				oid2 = this.GetOid(oid, false);
			}
			return oid2;
		}

		// Token: 0x060006DE RID: 1758 RVA: 0x0002B94C File Offset: 0x00029B4C
		private Oid GetOid(string oid, bool clean)
		{
			Oid oid2;
			using (OleDbConnection dbconnection = MibHelper.GetDBConnection())
			{
				dbconnection.Open();
				oid2 = this.GetOid(oid, dbconnection, clean);
			}
			return oid2;
		}

		// Token: 0x060006DF RID: 1759 RVA: 0x0002B98C File Offset: 0x00029B8C
		private Oid GetOid(string oid, OleDbConnection connection, bool clean)
		{
			Oid result = null;
			using (OleDbCommand oleDbCommand = new OleDbCommand())
			{
				string commandText = string.Empty;
				if (clean)
				{
					commandText = string.Format("Select TOP 1 {0} from Tree WHERE Primary = -1 AND OID=@Oid AND Description <> 'unknown';", "Index, MIB, Name, Primary, OID, Description, Access, Status, Units, Enum, TypeS");
				}
				else
				{
					commandText = string.Format("Select TOP 1 {0} from Tree WHERE Primary = -1 AND OID=@Oid;", "Index, MIB, Name, Primary, OID, Description, Access, Status, Units, Enum, TypeS");
				}
				oleDbCommand.CommandText = commandText;
				oleDbCommand.Parameters.AddWithValue("Oid", oid);
				using (IDataReader dataReader = OleDbHelper.ExecuteReader(oleDbCommand, connection))
				{
					if (dataReader.Read())
					{
						result = this.CreateOid(dataReader, connection);
					}
				}
			}
			return result;
		}

		// Token: 0x060006E0 RID: 1760 RVA: 0x0000B019 File Offset: 0x00009219
		public MemoryStream GetIcon(string oid)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060006E1 RID: 1761 RVA: 0x0002BA34 File Offset: 0x00029C34
		public Dictionary<string, MemoryStream> GetIcons()
		{
			byte[] buffer = new byte[0];
			Dictionary<string, MemoryStream> dictionary = new Dictionary<string, MemoryStream>();
			using (OleDbConnection dbconnection = MibHelper.GetDBConnection())
			{
				using (OleDbCommand oleDbCommand = new OleDbCommand())
				{
					dbconnection.Open();
					oleDbCommand.CommandText = "Select OID, [Small Icon] From Icons";
					using (IDataReader dataReader = OleDbHelper.ExecuteReader(oleDbCommand, dbconnection))
					{
						while (dataReader.Read())
						{
							if (!(dataReader["Small Icon"] is DBNull))
							{
								buffer = (byte[])dataReader["Small Icon"];
								dictionary.Add(dataReader["OID"].ToString(), new MemoryStream(buffer, true));
							}
						}
					}
				}
			}
			return dictionary;
		}

		// Token: 0x060006E2 RID: 1762 RVA: 0x0002BB10 File Offset: 0x00029D10
		public Oids GetChildOids(string parentOid)
		{
			List<string> uniqueChildOids = this.GetUniqueChildOids(parentOid);
			Oids oids = new Oids();
			using (OleDbConnection dbconnection = MibHelper.GetDBConnection())
			{
				dbconnection.Open();
				foreach (string oid in uniqueChildOids)
				{
					Oid oid2 = this.GetOid(oid, dbconnection, true);
					if (oid2 == null)
					{
						oid2 = this.GetOid(oid, dbconnection, false);
					}
					oids.Add(oid2);
				}
			}
			return oids;
		}

		// Token: 0x060006E3 RID: 1763 RVA: 0x0002BBB0 File Offset: 0x00029DB0
		public List<string> GetUniqueChildOids(string parentOid)
		{
			List<string> list = new List<string>();
			using (OleDbConnection dbconnection = MibHelper.GetDBConnection())
			{
				dbconnection.Open();
				using (OleDbCommand oleDbCommand = new OleDbCommand())
				{
					oleDbCommand.CommandText = string.Format("Select DISTINCT Name, OID, Index from Tree WHERE Primary = -1 AND ParentOID=@parentOid order by index;", "Index, MIB, Name, Primary, OID, Description, Access, Status, Units, Enum, TypeS");
					oleDbCommand.Parameters.AddWithValue("parentOid", parentOid);
					using (IDataReader dataReader = OleDbHelper.ExecuteReader(oleDbCommand, dbconnection))
					{
						while (dataReader.Read())
						{
							list.Add(DatabaseFunctions.GetString(dataReader, "OID"));
						}
					}
				}
			}
			return list;
		}

		// Token: 0x060006E4 RID: 1764 RVA: 0x0002BC68 File Offset: 0x00029E68
		public OidEnums GetEnums(string enumName)
		{
			OidEnums oidEnums = new OidEnums();
			if (string.IsNullOrEmpty(enumName))
			{
				return oidEnums;
			}
			using (OleDbConnection dbconnection = MibHelper.GetDBConnection())
			{
				dbconnection.Open();
				using (OleDbCommand oleDbCommand = new OleDbCommand())
				{
					oleDbCommand.CommandText = string.Format("Select {0} from Enums WHERE Name=@name order by Value;", "Name, Value, Enum");
					oleDbCommand.Parameters.AddWithValue("name", enumName);
					using (IDataReader dataReader = OleDbHelper.ExecuteReader(oleDbCommand, dbconnection))
					{
						while (dataReader.Read())
						{
							oidEnums.Add(new OidEnum
							{
								Id = DatabaseFunctions.GetDouble(dataReader, 1).ToString(),
								Name = DatabaseFunctions.GetString(dataReader, 2)
							});
						}
					}
				}
			}
			return oidEnums;
		}

		// Token: 0x060006E5 RID: 1765 RVA: 0x0000B019 File Offset: 0x00009219
		public Oids GetSearchingOidsByDescription(string searchCriteria, string searchMIBsCriteria)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060006E6 RID: 1766 RVA: 0x0002BD50 File Offset: 0x00029F50
		public void CancelRunningCommand()
		{
			if (MibDAL.CancellationTokenSource != null)
			{
				try
				{
					MibDAL.CancellationTokenSource.Cancel();
				}
				catch (AggregateException)
				{
				}
			}
		}

		// Token: 0x060006E7 RID: 1767 RVA: 0x0002BD84 File Offset: 0x00029F84
		public Oids GetSearchingOidsByName(string searchCriteria)
		{
			new List<string>();
			Oids oids = new Oids();
			using (OleDbConnection connection = MibHelper.GetDBConnection())
			{
				connection.Open();
				MibDAL.CancellationTokenSource = new CancellationTokenSource();
				using (OleDbCommand oleDbCommand = new OleDbCommand())
				{
					oleDbCommand.CommandText = string.Format("SELECT TOP 250 {0} FROM Tree WHERE (Primary = -1) AND ( Name LIKE @SearchValue OR Description LIKE '%' + @SearchValue + '%' OR Mib LIKE @SearchValue)", "Index, MIB, Name, Primary, OID, Description, Access, Status, Units, Enum, TypeS");
					oleDbCommand.Parameters.AddWithValue("@SearchValue", searchCriteria);
					using (IDataReader reader = OleDbHelper.ExecuteReader(oleDbCommand, connection))
					{
						foreach (Oid value in Task.Factory.StartNew<Oids>(() => this.getOidsFromReader(reader, connection), MibDAL.CancellationTokenSource.Token).Result)
						{
							oids.Add(value);
						}
					}
				}
			}
			return oids;
		}

		// Token: 0x060006E8 RID: 1768 RVA: 0x0002BED8 File Offset: 0x0002A0D8
		private Oids getOidsFromReader(IDataReader reader, OleDbConnection connection)
		{
			Oids oids = new Oids();
			while (reader.Read())
			{
				MibDAL.CancellationTokenSource.Token.ThrowIfCancellationRequested();
				Oid value = this.CreateOid(reader, connection);
				oids.Add(value);
			}
			return oids;
		}

		// Token: 0x060006E9 RID: 1769 RVA: 0x0002BF18 File Offset: 0x0002A118
		public bool IsMibDatabaseAvailable()
		{
			return MibHelper.IsMIBDatabaseAvailable();
		}

		// Token: 0x060006EA RID: 1770 RVA: 0x0002BF20 File Offset: 0x0002A120
		private Oid CreateOid(IDataReader reader, OleDbConnection connection)
		{
			Oid oid = new Oid();
			oid.ID = DatabaseFunctions.GetString(reader, 4);
			oid.Name = DatabaseFunctions.GetString(reader, 2);
			oid.Description = DatabaseFunctions.GetString(reader, 5);
			oid.MIB = DatabaseFunctions.GetString(reader, 1);
			oid.Access = (AccessType)DatabaseFunctions.GetByte(reader, 6);
			oid.Status = (StatusType)DatabaseFunctions.GetByte(reader, 7);
			oid.Units = DatabaseFunctions.GetString(reader, 8);
			oid.StringType = DatabaseFunctions.GetString(reader, 10);
			oid.HasChildren = this.HasChildren(oid.ID, connection);
			oid.Enums = this.GetEnums(DatabaseFunctions.GetString(reader, 9));
			oid.TreeIndex = DatabaseFunctions.GetInt32(reader, 0).ToString();
			MibHelper.CleanupDescription(oid);
			MibHelper.SetTypeInfo(oid);
			return oid;
		}

		// Token: 0x060006EB RID: 1771 RVA: 0x0002BFE8 File Offset: 0x0002A1E8
		private bool HasChildren(string oid, OleDbConnection connection)
		{
			using (OleDbCommand oleDbCommand = new OleDbCommand())
			{
				oleDbCommand.CommandText = "Select COUNT(*) from Tree WHERE Primary = -1 AND ParentOID=@oid;";
				oleDbCommand.Parameters.AddWithValue("parentOid", oid);
				if ((int)OleDbHelper.ExecuteScalar(oleDbCommand, connection) > 0)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x04000222 RID: 546
		private const string TreeColumns = "Index, MIB, Name, Primary, OID, Description, Access, Status, Units, Enum, TypeS";

		// Token: 0x04000223 RID: 547
		private const string EnumColumns = "Name, Value, Enum";

		// Token: 0x04000224 RID: 548
		private static readonly Log _myLog = new Log();

		// Token: 0x04000225 RID: 549
		private static CancellationTokenSource _cancellationTokenSource;

		// Token: 0x04000226 RID: 550
		private static object _tokenLock = new object();

		// Token: 0x02000181 RID: 385
		private enum EnumColumnOrder
		{
			// Token: 0x040004E4 RID: 1252
			EnumName,
			// Token: 0x040004E5 RID: 1253
			EnumValue,
			// Token: 0x040004E6 RID: 1254
			EnumEnum
		}
	}
}
