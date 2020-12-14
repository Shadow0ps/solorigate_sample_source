using System;
using System.Data;
using System.Data.SqlClient;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Discovery.DataAccess;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x020000A0 RID: 160
	public sealed class MaintenanceRenewalsCheckStatusDAL
	{
		// Token: 0x17000100 RID: 256
		// (get) Token: 0x060007BD RID: 1981 RVA: 0x00037454 File Offset: 0x00035654
		// (set) Token: 0x060007BE RID: 1982 RVA: 0x0003745C File Offset: 0x0003565C
		public DateTime? LastUpdateCheck { get; set; }

		// Token: 0x17000101 RID: 257
		// (get) Token: 0x060007BF RID: 1983 RVA: 0x00037465 File Offset: 0x00035665
		// (set) Token: 0x060007C0 RID: 1984 RVA: 0x0003746D File Offset: 0x0003566D
		public DateTime? NextUpdateCheck { get; set; }

		// Token: 0x060007C1 RID: 1985 RVA: 0x00037478 File Offset: 0x00035678
		public MaintenanceRenewalsCheckStatusDAL()
		{
			this.LastUpdateCheck = null;
			this.NextUpdateCheck = null;
		}

		// Token: 0x060007C2 RID: 1986 RVA: 0x000374AC File Offset: 0x000356AC
		public static MaintenanceRenewalsCheckStatusDAL GetCheckStatus()
		{
			MaintenanceRenewalsCheckStatusDAL result;
			try
			{
				MaintenanceRenewalsCheckStatusDAL maintenanceRenewalsCheckStatusDAL = new MaintenanceRenewalsCheckStatusDAL();
				maintenanceRenewalsCheckStatusDAL.LoadFromDB();
				result = maintenanceRenewalsCheckStatusDAL;
			}
			catch (ResultCountException)
			{
				MaintenanceRenewalsCheckStatusDAL.log.DebugFormat("Can't find maintenance renewals check status record in DB.", Array.Empty<object>());
				result = null;
			}
			return result;
		}

		// Token: 0x060007C3 RID: 1987 RVA: 0x000374F4 File Offset: 0x000356F4
		public static MaintenanceRenewalsCheckStatusDAL Insert(DateTime? lastCheck, DateTime? nextCheck)
		{
			MaintenanceRenewalsCheckStatusDAL result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("INSERT INTO MaintenanceRenewalsCheckStatus (LastUpdateCheck, NextUpdateCheck)\r\n                                                        VALUES (@LastUpdateCheck, @NextUpdateCheck)"))
			{
				textCommand.Parameters.AddWithValue("@LastUpdateCheck", (lastCheck != null) ? lastCheck : DBNull.Value);
				textCommand.Parameters.AddWithValue("@NextUpdateCheck", (nextCheck != null) ? nextCheck : DBNull.Value);
				if (SqlHelper.ExecuteNonQuery(textCommand) == 0)
				{
					result = null;
				}
				else
				{
					result = new MaintenanceRenewalsCheckStatusDAL
					{
						LastUpdateCheck = lastCheck,
						NextUpdateCheck = nextCheck
					};
				}
			}
			return result;
		}

		// Token: 0x060007C4 RID: 1988 RVA: 0x00037598 File Offset: 0x00035798
		public bool Update()
		{
			bool result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("UPDATE MaintenanceRenewalsCheckStatus SET LastUpdateCheck=@LastUpdateCheck, NextUpdateCheck=@NextUpdateCheck"))
			{
				textCommand.Parameters.AddWithValue("@LastUpdateCheck", (this.LastUpdateCheck != null) ? this.LastUpdateCheck : DBNull.Value);
				textCommand.Parameters.AddWithValue("@NextUpdateCheck", (this.NextUpdateCheck != null) ? this.NextUpdateCheck : DBNull.Value);
				result = (SqlHelper.ExecuteNonQuery(textCommand) > 0);
			}
			return result;
		}

		// Token: 0x060007C5 RID: 1989 RVA: 0x00037640 File Offset: 0x00035840
		public static void SetLastUpdateCheck(double timeout, bool forceCheck)
		{
			DateTime utcNow = DateTime.UtcNow;
			MaintenanceRenewalsCheckStatusDAL checkStatus = MaintenanceRenewalsCheckStatusDAL.GetCheckStatus();
			if (checkStatus == null)
			{
				MaintenanceRenewalsCheckStatusDAL.Insert(new DateTime?(utcNow), new DateTime?(utcNow.AddMinutes(timeout)));
				return;
			}
			checkStatus.LastUpdateCheck = new DateTime?(utcNow);
			if (forceCheck)
			{
				checkStatus.NextUpdateCheck = new DateTime?(utcNow.AddMinutes(timeout));
			}
			checkStatus.Update();
		}

		// Token: 0x060007C6 RID: 1990 RVA: 0x0003769F File Offset: 0x0003589F
		private void LoadFromReader(IDataReader rd)
		{
			if (rd == null)
			{
				throw new ArgumentNullException("rd");
			}
			this.LastUpdateCheck = new DateTime?(DatabaseFunctions.GetDateTime(rd, "LastUpdateCheck"));
			this.NextUpdateCheck = new DateTime?(DatabaseFunctions.GetDateTime(rd, "NextUpdateCheck"));
		}

		// Token: 0x060007C7 RID: 1991 RVA: 0x000376DC File Offset: 0x000358DC
		private void LoadFromDB()
		{
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT * FROM MaintenanceRenewalsCheckStatus"))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					if (!dataReader.Read())
					{
						throw new ResultCountException(1, 0);
					}
					this.LoadFromReader(dataReader);
				}
			}
		}

		// Token: 0x0400024C RID: 588
		private static readonly Log log = new Log();
	}
}
