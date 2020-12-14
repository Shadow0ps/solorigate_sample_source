using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x020000A9 RID: 169
	public class SnmpV3CredentialsDAL
	{
		// Token: 0x06000866 RID: 2150 RVA: 0x0003BFC0 File Offset: 0x0003A1C0
		public static List<string> GetCredentialsSet()
		{
			string text = "SELECT CredentialName FROM SNMPV3Credentials ORDER BY CredentialName";
			List<string> list = new List<string>();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(text))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						list.Add(dataReader[0].ToString());
					}
				}
			}
			return list;
		}

		// Token: 0x06000867 RID: 2151 RVA: 0x0003C034 File Offset: 0x0003A234
		public static void InsertCredentials(SnmpCredentials crendentials)
		{
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("INSERT INTO [SNMPV3Credentials]\r\n           ([CredentialName]\r\n           ,[SNMPV3Username]\r\n           ,[SNMPV3Context]\r\n           ,[SNMPV3PrivMethod]\r\n           ,[SNMPV3PrivKey]\r\n           ,[SNMPV3PrivKeyIsPwd]\r\n           ,[SNMPV3AuthKey]\r\n           ,[SNMPV3AuthMethod]\r\n           ,[SNMPV3AuthKeyIsPwd])\r\n     VALUES\r\n           (@CredentialName\r\n           ,@SNMPV3Username\r\n           ,@SNMPV3Context\r\n           ,@SNMPV3PrivMethod\r\n           ,@SNMPV3PrivKey\r\n           ,@SNMPV3PrivKeyIsPwd\r\n           ,@SNMPV3AuthKey\r\n           ,@SNMPV3AuthMethod\r\n           ,@SNMPV3AuthKeyIsPwd)"))
			{
				textCommand.Parameters.Add("@CredentialName", SqlDbType.NVarChar, 200).Value = crendentials.CredentialName;
				textCommand.Parameters.Add("@SNMPV3Username", SqlDbType.NVarChar, 50).Value = crendentials.SNMPv3UserName;
				textCommand.Parameters.Add("@SNMPV3Context", SqlDbType.NVarChar, 50).Value = crendentials.SnmpV3Context;
				textCommand.Parameters.Add("@SNMPV3PrivMethod", SqlDbType.NVarChar, 50).Value = crendentials.SNMPv3PrivacyType.ToString();
				textCommand.Parameters.Add("@SNMPV3PrivKey", SqlDbType.NVarChar, 50).Value = crendentials.SNMPv3PrivacyPassword;
				textCommand.Parameters.Add("@SNMPV3PrivKeyIsPwd", SqlDbType.Bit).Value = crendentials.SNMPV3PrivKeyIsPwd;
				textCommand.Parameters.Add("@SNMPV3AuthKey", SqlDbType.NVarChar, 50).Value = crendentials.SNMPv3AuthPassword;
				textCommand.Parameters.Add("@SNMPV3AuthMethod", SqlDbType.NVarChar, 50).Value = crendentials.SNMPv3AuthType.ToString();
				textCommand.Parameters.Add("@SNMPV3AuthKeyIsPwd", SqlDbType.Bit).Value = crendentials.SNMPV3AuthKeyIsPwd;
				SqlHelper.ExecuteNonQuery(textCommand);
			}
		}

		// Token: 0x06000868 RID: 2152 RVA: 0x0003C1B8 File Offset: 0x0003A3B8
		public static void DeleteCredentials(string CredentialName)
		{
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("DELETE FROM [SNMPV3Credentials] WHERE CredentialName = @CredentialName"))
			{
				textCommand.Parameters.Add("@CredentialName", SqlDbType.NVarChar, 200).Value = CredentialName;
				SqlHelper.ExecuteNonQuery(textCommand);
			}
		}

		// Token: 0x06000869 RID: 2153 RVA: 0x0003C210 File Offset: 0x0003A410
		public static SnmpCredentials GetCredentials(string CredentialName)
		{
			string text = "SELECT * FROM SNMPV3Credentials WHERE CredentialName = @CredentialName";
			SnmpCredentials snmpCredentials = new SnmpCredentials();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(text))
			{
				textCommand.Parameters.Add("@CredentialName", SqlDbType.NVarChar, 200).Value = CredentialName;
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					if (dataReader.Read())
					{
						for (int i = 0; i < dataReader.FieldCount; i++)
						{
							string name = dataReader.GetName(i);
							uint num = <PrivateImplementationDetails>.ComputeStringHash(name);
							if (num <= 2220914390U)
							{
								if (num <= 943772474U)
								{
									if (num != 79874626U)
									{
										if (num == 943772474U)
										{
											if (name == "SNMPV3PrivMethod")
											{
												snmpCredentials.SNMPv3PrivacyType = (SNMPv3PrivacyType)Enum.Parse(typeof(SNMPv3PrivacyType), DatabaseFunctions.GetString(dataReader, i));
											}
										}
									}
									else if (name == "SNMPV3AuthKeyIsPwd")
									{
										snmpCredentials.SNMPV3AuthKeyIsPwd = DatabaseFunctions.GetBoolean(dataReader, i);
									}
								}
								else if (num != 1780899965U)
								{
									if (num == 2220914390U)
									{
										if (name == "SNMPV3Username")
										{
											snmpCredentials.SNMPv3UserName = DatabaseFunctions.GetString(dataReader, i);
										}
									}
								}
								else if (name == "SNMPV3Context")
								{
									snmpCredentials.SnmpV3Context = DatabaseFunctions.GetString(dataReader, i);
								}
							}
							else if (num <= 3748776610U)
							{
								if (num != 3070255433U)
								{
									if (num == 3748776610U)
									{
										if (name == "SNMPV3PrivKey")
										{
											snmpCredentials.SNMPv3PrivacyPassword = DatabaseFunctions.GetString(dataReader, i);
										}
									}
								}
								else if (name == "SNMPV3AuthMethod")
								{
									snmpCredentials.SNMPv3AuthType = (SNMPv3AuthType)Enum.Parse(typeof(SNMPv3AuthType), DatabaseFunctions.GetString(dataReader, i));
								}
							}
							else if (num != 3846471719U)
							{
								if (num != 3971732635U)
								{
									if (num == 4144601169U)
									{
										if (name == "SNMPV3PrivKeyIsPwd")
										{
											snmpCredentials.SNMPV3PrivKeyIsPwd = DatabaseFunctions.GetBoolean(dataReader, i);
										}
									}
								}
								else if (name == "CredentialName")
								{
									snmpCredentials.CredentialName = DatabaseFunctions.GetString(dataReader, i);
								}
							}
							else if (name == "SNMPV3AuthKey")
							{
								snmpCredentials.SNMPv3AuthPassword = DatabaseFunctions.GetString(dataReader, i);
							}
						}
					}
				}
			}
			return snmpCredentials;
		}

		// Token: 0x0600086A RID: 2154 RVA: 0x0003C4E0 File Offset: 0x0003A6E0
		public static void UpdateCredentials(SnmpCredentials credentials)
		{
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("UPDATE [SNMPV3Credentials]\r\n\t\t\t\t\t\t\t\tSET [SNMPV3Username] = @SNMPV3Username\r\n\t\t\t\t\t\t\t\t,[SNMPV3Context] = @SNMPV3Context\r\n\t\t\t\t\t\t\t\t,[SNMPV3PrivMethod] = @SNMPV3PrivMethod\r\n\t\t\t\t\t\t\t\t,[SNMPV3PrivKey] = @SNMPV3PrivKey\r\n\t\t\t\t\t\t\t\t,[SNMPV3PrivKeyIsPwd] = @SNMPV3PrivKeyIsPwd\r\n\t\t\t\t\t\t\t\t,[SNMPV3AuthKey] = @SNMPV3AuthKey\r\n\t\t\t\t\t\t\t\t,[SNMPV3AuthMethod] = @SNMPV3AuthMethod\r\n\t\t\t\t\t\t\t\t,[SNMPV3AuthKeyIsPwd] = @SNMPV3AuthKeyIsPwd\r\n\t\t\t\t\t\t\t\tWHERE [CredentialName] = @CredentialName"))
			{
				textCommand.Parameters.Add("@CredentialName", SqlDbType.NVarChar, 200).Value = credentials.CredentialName;
				textCommand.Parameters.Add("@SNMPV3Username", SqlDbType.NVarChar, 50).Value = credentials.SNMPv3UserName;
				textCommand.Parameters.Add("@SNMPV3Context", SqlDbType.NVarChar, 50).Value = credentials.SnmpV3Context;
				textCommand.Parameters.Add("@SNMPV3PrivMethod", SqlDbType.NVarChar, 50).Value = credentials.SNMPv3PrivacyType.ToString();
				textCommand.Parameters.Add("@SNMPV3PrivKey", SqlDbType.NVarChar, 50).Value = credentials.SNMPv3PrivacyPassword;
				textCommand.Parameters.Add("@SNMPV3PrivKeyIsPwd", SqlDbType.Bit).Value = credentials.SNMPV3PrivKeyIsPwd;
				textCommand.Parameters.Add("@SNMPV3AuthKey", SqlDbType.NVarChar, 50).Value = credentials.SNMPv3AuthPassword;
				textCommand.Parameters.Add("@SNMPV3AuthMethod", SqlDbType.NVarChar, 50).Value = credentials.SNMPv3AuthType.ToString();
				textCommand.Parameters.Add("@SNMPV3AuthKeyIsPwd", SqlDbType.Bit).Value = credentials.SNMPV3AuthKeyIsPwd;
				SqlHelper.ExecuteNonQuery(textCommand);
			}
		}
	}
}
