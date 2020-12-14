using System;
using System.Data;
using System.Data.SqlClient;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x020000A7 RID: 167
	[Obsolete("Please start using SolarWinds.Orion.Core.Common.DALs.PollersDAL instead")]
	public class PollerDAL
	{
		// Token: 0x06000857 RID: 2135 RVA: 0x0003BD18 File Offset: 0x00039F18
		public static PollerAssignment GetPoller(int pollerID)
		{
			PollerAssignment result;
			try
			{
				PollerAssignment assignment = PollerDAL.pollersDAL.GetAssignment(pollerID);
				if (assignment == null)
				{
					throw new NullReferenceException();
				}
				result = assignment;
			}
			catch (Exception)
			{
				throw new ArgumentOutOfRangeException("PollerID", string.Format("Poller with Id {0} does not exist", pollerID));
			}
			return result;
		}

		// Token: 0x06000858 RID: 2136 RVA: 0x0003BD6C File Offset: 0x00039F6C
		public static int InsertPoller(PollerAssignment poller)
		{
			int result;
			PollerDAL.pollersDAL.Insert(poller, out result);
			return result;
		}

		// Token: 0x06000859 RID: 2137 RVA: 0x0003BD87 File Offset: 0x00039F87
		public static void DeletePoller(int pollerID)
		{
			PollerDAL.pollersDAL.DeletePollerByID(pollerID);
		}

		// Token: 0x0600085A RID: 2138 RVA: 0x0003BD94 File Offset: 0x00039F94
		public static PollerAssignments GetPollersForNode(int nodeId)
		{
			return new PollerAssignments
			{
				PollerDAL.pollersDAL.GetNetObjectPollers("N", nodeId, Array.Empty<string>())
			};
		}

		// Token: 0x0600085B RID: 2139 RVA: 0x0003BDB8 File Offset: 0x00039FB8
		public static PollerAssignments GetAllPollersForNode(int nodeId, bool includeInterfacePollers)
		{
			PollerAssignments pollerAssignments = new PollerAssignments();
			string text = "SELECT PollerID, PollerType, NetObjectType, NetObjectID, Enabled FROM Pollers WHERE NetObject = @NetObject ";
			if (includeInterfacePollers)
			{
				text += "OR NetObject IN\r\n                        (\r\n                            SELECT 'I:' + RTRIM(LTRIM(STR(InterfaceID))) FROM Interfaces WHERE NodeID=@NodeID\r\n                        )";
			}
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(text))
			{
				if (includeInterfacePollers)
				{
					textCommand.Parameters.AddWithValue("@NodeID", nodeId);
				}
				textCommand.Parameters.Add("@NetObject", SqlDbType.VarChar, 50).Value = string.Format("N:{0}", nodeId);
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						PollerAssignment pollerAssignment = PollerDAL.CreatePoller(dataReader);
						pollerAssignments.Add(pollerAssignment.PollerID, pollerAssignment);
					}
				}
			}
			return pollerAssignments;
		}

		// Token: 0x0600085C RID: 2140 RVA: 0x0003BE88 File Offset: 0x0003A088
		public static PollerAssignments GetPollersForVolume(int volumeId)
		{
			return new PollerAssignments
			{
				PollerDAL.pollersDAL.GetNetObjectPollers("V", volumeId, Array.Empty<string>())
			};
		}

		// Token: 0x0600085D RID: 2141 RVA: 0x0003BEAC File Offset: 0x0003A0AC
		private static PollerAssignment CreatePoller(IDataReader reader)
		{
			PollerAssignment pollerAssignment = new PollerAssignment();
			for (int i = 0; i < reader.FieldCount; i++)
			{
				string name = reader.GetName(i);
				if (!(name == "PollerType"))
				{
					if (!(name == "NetObjectType"))
					{
						if (!(name == "NetObjectID"))
						{
							if (!(name == "PollerID"))
							{
								if (!(name == "Enabled"))
								{
									throw new ApplicationException("Couldn't create poller - unknown field.");
								}
								pollerAssignment.Enabled = DatabaseFunctions.GetBoolean(reader, i);
							}
							else
							{
								pollerAssignment.PollerID = DatabaseFunctions.GetInt32(reader, i);
							}
						}
						else
						{
							pollerAssignment.NetObjectID = DatabaseFunctions.GetInt32(reader, name);
						}
					}
					else
					{
						pollerAssignment.NetObjectType = DatabaseFunctions.GetString(reader, i);
					}
				}
				else
				{
					pollerAssignment.PollerType = DatabaseFunctions.GetString(reader, i);
				}
			}
			return pollerAssignment;
		}

		// Token: 0x04000262 RID: 610
		private static PollersDAL pollersDAL = new PollersDAL();
	}
}
