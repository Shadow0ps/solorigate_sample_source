using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Common.PackageManager;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x0200009D RID: 157
	internal class InterfaceBLDAL
	{
		// Token: 0x060007A0 RID: 1952 RVA: 0x000348A0 File Offset: 0x00032AA0
		public static Interfaces GetNodesInterfaces(IEnumerable<int> nodeIDs)
		{
			if (!InterfaceBLDAL._areInterfacesAllowed)
			{
				return new Interfaces();
			}
			StringBuilder stringBuilder = new StringBuilder("SELECT * FROM Interfaces WHERE NodeID IN (");
			foreach (int value in nodeIDs)
			{
				stringBuilder.Append(value);
				stringBuilder.Append(',');
			}
			StringBuilder stringBuilder2 = stringBuilder;
			int length = stringBuilder2.Length;
			stringBuilder2.Length = length - 1;
			stringBuilder.Append(')');
			return Collection<int, Interface>.FillCollection<Interfaces>(new Collection<int, Interface>.CreateElement(InterfaceBLDAL.CreateNodeInterface), stringBuilder.ToString(), Array.Empty<SqlParameter>());
		}

		// Token: 0x060007A1 RID: 1953 RVA: 0x00034940 File Offset: 0x00032B40
		internal static Interface CreateNodeInterface(IDataReader reader)
		{
			Interface @interface = new Interface();
			int i = 0;
			while (i < reader.FieldCount)
			{
				string name = reader.GetName(i);
				uint num = <PrivateImplementationDetails>.ComputeStringHash(name);
				if (num <= 2359628423U)
				{
					if (num <= 1330915288U)
					{
						if (num <= 532412421U)
						{
							if (num <= 158572675U)
							{
								if (num <= 45007414U)
								{
									if (num != 6222351U)
									{
										if (num != 45007414U)
										{
											goto IL_E49;
										}
										if (!(name == "LastSync"))
										{
											goto IL_E49;
										}
										@interface.LastSync = DatabaseFunctions.GetDateTime(reader, i);
									}
									else
									{
										if (!(name == "Status"))
										{
											goto IL_E49;
										}
										@interface.Status = DatabaseFunctions.GetString(reader, i);
									}
								}
								else if (num != 109873071U)
								{
									if (num != 158572675U)
									{
										goto IL_E49;
									}
									if (!(name == "InUcastPps"))
									{
										goto IL_E49;
									}
									@interface.InUcastPps = DatabaseFunctions.GetFloat(reader, i);
								}
								else
								{
									if (!(name == "InDiscardsThisHour"))
									{
										goto IL_E49;
									}
									@interface.InDiscardsThisHour = DatabaseFunctions.GetFloat(reader, i);
								}
							}
							else if (num <= 256162167U)
							{
								if (num != 248528016U)
								{
									if (num != 256162167U)
									{
										goto IL_E49;
									}
									if (!(name == "RediscoveryInterval"))
									{
										goto IL_E49;
									}
									@interface.RediscoveryInterval = DatabaseFunctions.GetInt32(reader, i);
								}
								else
								{
									if (!(name == "StatusLED"))
									{
										goto IL_E49;
									}
									@interface.StatusLED = DatabaseFunctions.GetString(reader, i);
								}
							}
							else if (num != 319641761U)
							{
								if (num != 532412421U)
								{
									goto IL_E49;
								}
								if (!(name == "Caption"))
								{
									goto IL_E49;
								}
								@interface.Caption = DatabaseFunctions.GetString(reader, i);
							}
							else
							{
								if (!(name == "OutDiscardsToday"))
								{
									goto IL_E49;
								}
								@interface.OutDiscardsToday = DatabaseFunctions.GetFloat(reader, i);
							}
						}
						else if (num <= 914606840U)
						{
							if (num <= 588784106U)
							{
								if (num != 556656155U)
								{
									if (num != 588784106U)
									{
										goto IL_E49;
									}
									if (!(name == "MaxInBpsToday"))
									{
										goto IL_E49;
									}
									@interface.MaxInBpsToday = DatabaseFunctions.GetFloat(reader, i);
								}
								else
								{
									if (!(name == "InterfaceSpeed"))
									{
										goto IL_E49;
									}
									@interface.InterfaceSpeed = DatabaseFunctions.GetDouble(reader, i);
								}
							}
							else if (num != 773892499U)
							{
								if (num != 914606840U)
								{
									goto IL_E49;
								}
								if (!(name == "InterfaceType"))
								{
									goto IL_E49;
								}
								@interface.InterfaceType = DatabaseFunctions.GetInt32(reader, i);
							}
							else
							{
								if (!(name == "InPercentUtil"))
								{
									goto IL_E49;
								}
								@interface.InPercentUtil = DatabaseFunctions.GetFloat(reader, i);
							}
						}
						else if (num <= 1107068263U)
						{
							if (num != 1017626194U)
							{
								if (num != 1107068263U)
								{
									goto IL_E49;
								}
								if (!(name == "InPps"))
								{
									goto IL_E49;
								}
								@interface.InPps = DatabaseFunctions.GetFloat(reader, i);
							}
							else
							{
								if (!(name == "InterfaceTypeDescription"))
								{
									goto IL_E49;
								}
								@interface.InterfaceTypeDescription = DatabaseFunctions.GetString(reader, i);
							}
						}
						else if (num != 1257309411U)
						{
							if (num != 1330915288U)
							{
								goto IL_E49;
							}
							if (!(name == "MaxInBpsTime"))
							{
								goto IL_E49;
							}
							@interface.MaxInBpsTime = DatabaseFunctions.GetDateTime(reader, i);
						}
						else
						{
							if (!(name == "UnPluggable"))
							{
								goto IL_E49;
							}
							@interface.UnPluggable = DatabaseFunctions.GetBoolean(reader, i);
						}
					}
					else if (num <= 1887498962U)
					{
						if (num <= 1659882786U)
						{
							if (num <= 1567077553U)
							{
								if (num != 1390998017U)
								{
									if (num != 1567077553U)
									{
										goto IL_E49;
									}
									if (!(name == "InterfaceIcon"))
									{
										goto IL_E49;
									}
									@interface.InterfaceIcon = DatabaseFunctions.GetString(reader, i);
								}
								else
								{
									if (!(name == "Counter64"))
									{
										goto IL_E49;
									}
									@interface.Counter64 = DatabaseFunctions.GetString(reader, i);
								}
							}
							else if (num != 1571238303U)
							{
								if (num != 1659882786U)
								{
									goto IL_E49;
								}
								if (!(name == "OutMcastPps"))
								{
									goto IL_E49;
								}
								@interface.OutMcastPps = DatabaseFunctions.GetFloat(reader, i);
							}
							else
							{
								if (!(name == "MaxOutBpsToday"))
								{
									goto IL_E49;
								}
								@interface.MaxOutBpsToday = DatabaseFunctions.GetFloat(reader, i);
							}
						}
						else if (num <= 1798816559U)
						{
							if (num != 1798040858U)
							{
								if (num != 1798816559U)
								{
									goto IL_E49;
								}
								if (!(name == "InBandwidth"))
								{
									goto IL_E49;
								}
								@interface.InBandwidth = DatabaseFunctions.GetDouble(reader, i);
							}
							else
							{
								if (!(name == "InDiscardsToday"))
								{
									goto IL_E49;
								}
								@interface.InDiscardsToday = DatabaseFunctions.GetFloat(reader, i);
							}
						}
						else if (num != 1863478786U)
						{
							if (num != 1887498962U)
							{
								goto IL_E49;
							}
							if (!(name == "Severity"))
							{
								goto IL_E49;
							}
							@interface.Severity = DatabaseFunctions.GetInt32(reader, i);
						}
						else
						{
							if (!(name == "CollectAvailability"))
							{
								goto IL_E49;
							}
							@interface.CollectAvailability = (reader.IsDBNull(i) || DatabaseFunctions.GetBoolean(reader, i));
						}
					}
					else if (num <= 2155638092U)
					{
						if (num <= 2088678059U)
						{
							if (num != 1934659685U)
							{
								if (num != 2088678059U)
								{
									goto IL_E49;
								}
								if (!(name == "InMcastPps"))
								{
									goto IL_E49;
								}
								@interface.InMcastPps = DatabaseFunctions.GetFloat(reader, i);
							}
							else
							{
								if (!(name == "IfName"))
								{
									goto IL_E49;
								}
								@interface.IfName = DatabaseFunctions.GetString(reader, i);
							}
						}
						else if (num != 2117115014U)
						{
							if (num != 2155638092U)
							{
								goto IL_E49;
							}
							if (!(name == "OutPps"))
							{
								goto IL_E49;
							}
							@interface.OutPps = DatabaseFunctions.GetFloat(reader, i);
						}
						else
						{
							if (!(name == "OutErrorsThisHour"))
							{
								goto IL_E49;
							}
							@interface.OutErrorsThisHour = DatabaseFunctions.GetFloat(reader, i);
						}
					}
					else if (num <= 2236849620U)
					{
						if (num != 2229825399U)
						{
							if (num != 2236849620U)
							{
								goto IL_E49;
							}
							if (!(name == "InterfaceMTU"))
							{
								goto IL_E49;
							}
							@interface.InterfaceMTU = DatabaseFunctions.GetInt32(reader, i);
						}
						else
						{
							if (!(name == "FullName"))
							{
								goto IL_E49;
							}
							@interface.FullName = DatabaseFunctions.GetString(reader, i);
						}
					}
					else if (num != 2358485345U)
					{
						if (num != 2359628423U)
						{
							goto IL_E49;
						}
						if (!(name == "OperStatus"))
						{
							goto IL_E49;
						}
						@interface.OperStatus = DatabaseFunctions.GetInt16(reader, i);
					}
					else
					{
						if (!(name == "UnManageFrom"))
						{
							goto IL_E49;
						}
						@interface.UnManageFrom = DatabaseFunctions.GetDateTime(reader, i);
					}
				}
				else if (num <= 3449004665U)
				{
					if (num <= 2988209474U)
					{
						if (num <= 2711404065U)
						{
							if (num <= 2564163853U)
							{
								if (num != 2395316087U)
								{
									if (num != 2564163853U)
									{
										goto IL_E49;
									}
									if (!(name == "Inbps"))
									{
										goto IL_E49;
									}
									@interface.InBps = DatabaseFunctions.GetFloat(reader, i);
								}
								else
								{
									if (!(name == "AdminStatusLED"))
									{
										goto IL_E49;
									}
									@interface.AdminStatusLED = DatabaseFunctions.GetString(reader, i);
								}
							}
							else if (num != 2655931018U)
							{
								if (num != 2711404065U)
								{
									goto IL_E49;
								}
								if (!(name == "PollInterval"))
								{
									goto IL_E49;
								}
								@interface.PollInterval = DatabaseFunctions.GetInt32(reader, i);
							}
							else
							{
								if (!(name == "InErrorsToday"))
								{
									goto IL_E49;
								}
								@interface.InErrorsToday = DatabaseFunctions.GetFloat(reader, i);
							}
						}
						else if (num <= 2841884632U)
						{
							if (num != 2732404491U)
							{
								if (num != 2841884632U)
								{
									goto IL_E49;
								}
								if (!(name == "InterfaceLastChange"))
								{
									goto IL_E49;
								}
								@interface.InterfaceLastChange = DatabaseFunctions.GetDateTime(reader, i);
							}
							else
							{
								if (!(name == "OutPktSize"))
								{
									goto IL_E49;
								}
								@interface.OutPktSize = DatabaseFunctions.GetInt16(reader, i);
							}
						}
						else if (num != 2869862648U)
						{
							if (num != 2988209474U)
							{
								goto IL_E49;
							}
							if (!(name == "InterfaceSubType"))
							{
								goto IL_E49;
							}
							@interface.InterfaceSubType = DatabaseFunctions.GetInt32(reader, i);
						}
						else
						{
							if (!(name == "NodeID"))
							{
								goto IL_E49;
							}
							@interface.NodeID = DatabaseFunctions.GetInt32(reader, i);
						}
					}
					else if (num <= 3200264202U)
					{
						if (num <= 3143955903U)
						{
							if (num != 3035921756U)
							{
								if (num != 3143955903U)
								{
									goto IL_E49;
								}
								if (!(name == "InErrorsThisHour"))
								{
									goto IL_E49;
								}
								@interface.InErrorsThisHour = DatabaseFunctions.GetFloat(reader, i);
							}
							else
							{
								if (!(name == "InPktSize"))
								{
									goto IL_E49;
								}
								@interface.InPktSize = DatabaseFunctions.GetInt16(reader, i);
							}
						}
						else if (num != 3160731452U)
						{
							if (num != 3200264202U)
							{
								goto IL_E49;
							}
							if (!(name == "OutDiscardsThisHour"))
							{
								goto IL_E49;
							}
							@interface.OutDiscardsThisHour = DatabaseFunctions.GetFloat(reader, i);
						}
						else
						{
							if (!(name == "InterfaceAlias"))
							{
								goto IL_E49;
							}
							@interface.InterfaceAlias = DatabaseFunctions.GetString(reader, i);
						}
					}
					else if (num <= 3282255531U)
					{
						if (num != 3216090560U)
						{
							if (num != 3282255531U)
							{
								goto IL_E49;
							}
							if (!(name == "UnManageUntil"))
							{
								goto IL_E49;
							}
							@interface.UnManageUntil = DatabaseFunctions.GetDateTime(reader, i);
						}
						else
						{
							if (!(name == "ObjectSubType"))
							{
								goto IL_E49;
							}
							@interface.ObjectSubType = DatabaseFunctions.GetString(reader, i);
						}
					}
					else if (num != 3336839519U)
					{
						if (num != 3449004665U)
						{
							goto IL_E49;
						}
						if (!(name == "NextRediscovery"))
						{
							goto IL_E49;
						}
						@interface.NextRediscovery = DatabaseFunctions.GetDateTime(reader, i);
					}
					else
					{
						if (!(name == "CustomBandwidth"))
						{
							goto IL_E49;
						}
						@interface.CustomBandwidth = DatabaseFunctions.GetBoolean(reader, i);
					}
				}
				else if (num <= 3792413811U)
				{
					if (num <= 3654310386U)
					{
						if (num <= 3489145781U)
						{
							if (num != 3469824504U)
							{
								if (num != 3489145781U)
								{
									goto IL_E49;
								}
								if (!(name == "OutErrorsToday"))
								{
									goto IL_E49;
								}
								@interface.OutErrorsToday = DatabaseFunctions.GetFloat(reader, i);
							}
							else
							{
								if (!(name == "OutPercentUtil"))
								{
									goto IL_E49;
								}
								@interface.OutPercentUtil = DatabaseFunctions.GetFloat(reader, i);
							}
						}
						else if (num != 3521202693U)
						{
							if (num != 3654310386U)
							{
								goto IL_E49;
							}
							if (!(name == "Outbps"))
							{
								goto IL_E49;
							}
							@interface.OutBps = DatabaseFunctions.GetFloat(reader, i);
						}
						else
						{
							if (!(name == "NextPoll"))
							{
								goto IL_E49;
							}
							@interface.NextPoll = DatabaseFunctions.GetDateTime(reader, i);
						}
					}
					else if (num <= 3736301255U)
					{
						if (num != 3683179112U)
						{
							if (num != 3736301255U)
							{
								goto IL_E49;
							}
							if (!(name == "MaxOutBpsTime"))
							{
								goto IL_E49;
							}
							@interface.MaxOutBpsTime = DatabaseFunctions.GetDateTime(reader, i);
						}
						else
						{
							if (!(name == "OperStatusLED"))
							{
								goto IL_E49;
							}
							@interface.OperStatusLED = DatabaseFunctions.GetString(reader, i);
						}
					}
					else if (num != 3769769805U)
					{
						if (num != 3792413811U)
						{
							goto IL_E49;
						}
						if (!(name == "InterfaceName"))
						{
							goto IL_E49;
						}
						@interface.InterfaceName = DatabaseFunctions.GetString(reader, i);
					}
					else
					{
						if (!(name == "InterfaceID"))
						{
							goto IL_E49;
						}
						@interface.InterfaceID = DatabaseFunctions.GetInt32(reader, i);
					}
				}
				else if (num <= 3971248279U)
				{
					if (num <= 3885329530U)
					{
						if (num != 3854733374U)
						{
							if (num != 3885329530U)
							{
								goto IL_E49;
							}
							if (!(name == "OutUcastPps"))
							{
								goto IL_E49;
							}
							@interface.OutUcastPps = DatabaseFunctions.GetFloat(reader, i);
						}
						else
						{
							if (!(name == "AdminStatus"))
							{
								goto IL_E49;
							}
							@interface.AdminStatus = DatabaseFunctions.GetInt16(reader, i);
						}
					}
					else if (num != 3936513175U)
					{
						if (num != 3971248279U)
						{
							goto IL_E49;
						}
						if (!(name == "UnManaged"))
						{
							goto IL_E49;
						}
						@interface.UnManaged = DatabaseFunctions.GetBoolean(reader, i);
					}
					else
					{
						if (!(name == "StatCollection"))
						{
							goto IL_E49;
						}
						@interface.StatCollection = DatabaseFunctions.GetInt16(reader, i);
					}
				}
				else if (num <= 4037488300U)
				{
					if (num != 3995067068U)
					{
						if (num != 4037488300U)
						{
							goto IL_E49;
						}
						if (!(name == "InterfaceIndex"))
						{
							goto IL_E49;
						}
						@interface.InterfaceIndex = DatabaseFunctions.GetInt32(reader, i);
					}
					else
					{
						if (!(name == "OutBandwidth"))
						{
							goto IL_E49;
						}
						@interface.OutBandwidth = DatabaseFunctions.GetDouble(reader, i);
					}
				}
				else if (num != 4177086267U)
				{
					if (num != 4193613824U)
					{
						goto IL_E49;
					}
					if (!(name == "PhysicalAddress"))
					{
						goto IL_E49;
					}
					@interface.PhysicalAddress = DatabaseFunctions.GetString(reader, i);
				}
				else
				{
					if (!(name == "InterfaceTypeName"))
					{
						goto IL_E49;
					}
					@interface.InterfaceTypeName = DatabaseFunctions.GetString(reader, i);
				}
				IL_E82:
				i++;
				continue;
				IL_E49:
				if (CustomPropertyMgr.IsCustom("Interfaces", name))
				{
					@interface.CustomProperties[name] = reader[i];
					goto IL_E82;
				}
				InterfaceBLDAL.log.DebugFormat("Skipping Interface property {0}, value {1}", name, reader[i]);
				goto IL_E82;
			}
			return @interface;
		}

		// Token: 0x060007A2 RID: 1954 RVA: 0x000357E0 File Offset: 0x000339E0
		public static int CreateNewInterface(Interface _interface, bool checkIfAlreadyExists)
		{
			return InterfaceBLDAL.CreateNewInterface(_interface, checkIfAlreadyExists ? InterfaceBLDAL.SqlManagedInterfaceExists : string.Empty);
		}

		// Token: 0x060007A3 RID: 1955 RVA: 0x000357F8 File Offset: 0x000339F8
		public static int CreateNewInterface(Interface _interface, string checkQuery)
		{
			string text = "\r\n        INSERT INTO [Interfaces]\r\n        ([NodeID]\r\n        ,[ObjectSubType]\r\n        ,[InterfaceName]\r\n        ,[InterfaceIndex]\r\n        ,[InterfaceType]\r\n        ,[InterfaceSubType]\r\n        ,[InterfaceTypeName]\r\n        ,[InterfaceTypeDescription]\r\n        ,[InterfaceSpeed]\r\n        ,[InterfaceMTU]\r\n        ,[InterfaceLastChange]\r\n        ,[PhysicalAddress]\r\n        ,[UnManaged]\r\n        ,[UnManageFrom]\r\n        ,[UnManageUntil]\r\n        ,[AdminStatus]\r\n        ,[OperStatus]\r\n        ,[InBandwidth]\r\n        ,[OutBandwidth]\r\n        ,[Caption]\r\n        ,[PollInterval]\r\n        ,[RediscoveryInterval]\r\n        ,[FullName]\r\n        ,[Status]\r\n        ,[StatusLED]\r\n        ,[AdminStatusLED]\r\n        ,[OperStatusLED]\r\n        ,[InterfaceIcon]\r\n        ,[Outbps]\r\n        ,[Inbps]\r\n        ,[OutPercentUtil]\r\n        ,[InPercentUtil]\r\n        ,[OutPps]\r\n        ,[InPps]\r\n        ,[InPktSize]\r\n        ,[OutPktSize]\r\n        ,[OutUcastPps]\r\n        ,[OutMcastPps]\r\n        ,[InUcastPps]\r\n        ,[InMcastPps]\r\n        ,[InDiscardsThisHour]\r\n        ,[InDiscardsToday]\r\n        ,[InErrorsThisHour]\r\n        ,[InErrorsToday]\r\n        ,[OutDiscardsThisHour]\r\n        ,[OutDiscardsToday]\r\n        ,[OutErrorsThisHour]\r\n        ,[OutErrorsToday]\r\n        ,[MaxInBpsToday]\r\n        ,[MaxInBpsTime]\r\n        ,[MaxOutBpsToday]\r\n        ,[MaxOutBpsTime]\r\n        ,[NextRediscovery]\r\n        ,[NextPoll]\r\n        ,[Counter64]\r\n        ,[StatCollection]\r\n        ,[LastSync]\r\n        ,[InterfaceAlias]\r\n        ,[IfName]\r\n        ,[Severity]\r\n        ,[CustomBandwidth]\r\n        ,[UnPluggable]\r\n        ,[CollectAvailability]\r\n        )\r\n        VALUES\r\n        (@NodeID\r\n        ,@ObjectSubType\r\n        ,@InterfaceName\r\n        ,@InterfaceIndex\r\n        ,@InterfaceType\r\n        ,@InterfaceSubType\r\n        ,@InterfaceTypeName\r\n        ,@InterfaceTypeDescription\r\n        ,@InterfaceSpeed\r\n        ,@InterfaceMTU\r\n        ,@InterfaceLastChange\r\n        ,@PhysicalAddress\r\n        ,@UnManaged\r\n        ,@UnManageFrom\r\n        ,@UnManageUntil\r\n        ,@AdminStatus\r\n        ,@OperStatus\r\n        ,@InBandwidth\r\n        ,@OutBandwidth\r\n        ,@Caption\r\n        ,@PollInterval\r\n        ,@RediscoveryInterval\r\n        ,@FullName\r\n        ,@Status\r\n        ,@StatusLED\r\n        ,@AdminStatusLED\r\n        ,@OperStatusLED\r\n        ,@InterfaceIcon\r\n        ,@Outbps\r\n        ,@Inbps\r\n        ,@OutPercentUtil\r\n        ,@InPercentUtil\r\n        ,@OutPps\r\n        ,@InPps\r\n        ,@InPktSize\r\n        ,@OutPktSize\r\n        ,@OutUcastPps\r\n        ,@OutMcastPps\r\n        ,@InUcastPps\r\n        ,@InMcastPps\r\n        ,@InDiscardsThisHour\r\n        ,@InDiscardsToday\r\n        ,@InErrorsThisHour\r\n        ,@InErrorsToday\r\n        ,@OutDiscardsThisHour\r\n        ,@OutDiscardsToday\r\n        ,@OutErrorsThisHour\r\n        ,@OutErrorsToday\r\n        ,@MaxInBpsToday\r\n        ,@MaxInBpsTime\r\n        ,@MaxOutBpsToday\r\n        ,@MaxOutBpsTime\r\n        ,@NextRediscovery\r\n        ,@NextPoll\r\n        ,@Counter64\r\n        ,@StatCollection\r\n        ,@LastSync\r\n        ,@InterfaceAlias\r\n        ,@IfName\r\n        ,@Severity\r\n        ,@CustomBandwidth\r\n        ,@UnPluggable\r\n        ,@CollectAvailability\r\n        )\r\n\r\n        SELECT Scope_Identity();";
			if (!string.IsNullOrEmpty(checkQuery))
			{
				text = string.Concat(new string[]
				{
					"\r\n                ",
					checkQuery,
					"\r\n                BEGIN ",
					text,
					" \r\n                END\r\n                ELSE\r\n                BEGIN\r\n                    SELECT -1;\r\n                END"
				});
			}
			SqlCommand textCommand = SqlHelper.GetTextCommand(text);
			_interface = new DALHelper<Interface>().Initialize(_interface);
			textCommand.Parameters.AddWithValue("NodeID", _interface.NodeID);
			textCommand.Parameters.AddWithValue("ObjectSubType", _interface.ObjectSubType);
			textCommand.Parameters.AddWithValue("InterfaceName", _interface.InterfaceName);
			textCommand.Parameters.Add("@InterfaceIndex", SqlDbType.Int, 4).Value = _interface.InterfaceIndex;
			textCommand.Parameters.AddWithValue("InterfaceType", _interface.InterfaceType);
			textCommand.Parameters.AddWithValue("InterfaceSubType", _interface.InterfaceSubType);
			textCommand.Parameters.AddWithValue("InterfaceTypeName", _interface.InterfaceTypeName);
			textCommand.Parameters.AddWithValue("InterfaceTypeDescription", _interface.InterfaceTypeDescription);
			textCommand.Parameters.AddWithValue("InterfaceSpeed", _interface.InterfaceSpeed);
			textCommand.Parameters.AddWithValue("InterfaceMTU", _interface.InterfaceMTU);
			if (_interface.InterfaceLastChange == DateTime.MinValue)
			{
				textCommand.Parameters.Add("@InterfaceLastChange", SqlDbType.DateTime).Value = DBNull.Value;
			}
			else
			{
				textCommand.Parameters.Add("@InterfaceLastChange", SqlDbType.DateTime).Value = _interface.InterfaceLastChange;
			}
			textCommand.Parameters.AddWithValue("PhysicalAddress", _interface.PhysicalAddress);
			textCommand.Parameters.AddWithValue("AdminStatus", _interface.AdminStatus);
			textCommand.Parameters.AddWithValue("OperStatus", _interface.OperStatus);
			textCommand.Parameters.AddWithValue("InBandwidth", _interface.InBandwidth);
			textCommand.Parameters.AddWithValue("OutBandwidth", _interface.OutBandwidth);
			textCommand.Parameters.AddWithValue("Caption", _interface.Caption);
			textCommand.Parameters.AddWithValue("PollInterval", _interface.PollInterval);
			textCommand.Parameters.AddWithValue("RediscoveryInterval", _interface.RediscoveryInterval);
			textCommand.Parameters.AddWithValue("FullName", _interface.FullName);
			textCommand.Parameters.AddWithValue("Status", _interface.Status);
			textCommand.Parameters.AddWithValue("StatusLED", _interface.StatusLED);
			textCommand.Parameters.AddWithValue("AdminStatusLED", _interface.AdminStatusLED);
			textCommand.Parameters.AddWithValue("OperStatusLED", _interface.OperStatusLED);
			textCommand.Parameters.AddWithValue("InterfaceIcon", _interface.InterfaceIcon);
			textCommand.Parameters.AddWithValue("Outbps", _interface.OutBps);
			textCommand.Parameters.AddWithValue("Inbps", _interface.InBps);
			textCommand.Parameters.AddWithValue("OutPercentUtil", _interface.OutPercentUtil);
			textCommand.Parameters.AddWithValue("InPercentUtil", _interface.InPercentUtil);
			textCommand.Parameters.AddWithValue("OutPps", _interface.OutPps);
			textCommand.Parameters.AddWithValue("InPps", _interface.InPps);
			textCommand.Parameters.AddWithValue("InPktSize", _interface.InPktSize);
			textCommand.Parameters.AddWithValue("OutPktSize", _interface.OutPktSize);
			textCommand.Parameters.AddWithValue("OutUcastPps", _interface.OutUcastPps);
			textCommand.Parameters.AddWithValue("OutMcastPps", _interface.OutMcastPps);
			textCommand.Parameters.AddWithValue("InUcastPps", _interface.InUcastPps);
			textCommand.Parameters.AddWithValue("InMcastPps", _interface.InMcastPps);
			textCommand.Parameters.AddWithValue("InDiscardsThisHour", _interface.InDiscardsThisHour);
			textCommand.Parameters.AddWithValue("InDiscardsToday", _interface.InDiscardsToday);
			textCommand.Parameters.AddWithValue("InErrorsThisHour", _interface.InErrorsThisHour);
			textCommand.Parameters.AddWithValue("InErrorsToday", _interface.InErrorsToday);
			textCommand.Parameters.AddWithValue("OutDiscardsThisHour", _interface.OutDiscardsThisHour);
			textCommand.Parameters.AddWithValue("OutDiscardsToday", _interface.OutDiscardsToday);
			textCommand.Parameters.AddWithValue("OutErrorsThisHour", _interface.OutErrorsThisHour);
			textCommand.Parameters.AddWithValue("OutErrorsToday", _interface.OutErrorsToday);
			textCommand.Parameters.AddWithValue("MaxInBpsToday", _interface.MaxInBpsToday);
			if (_interface.MaxInBpsTime == DateTime.MinValue)
			{
				textCommand.Parameters.Add("@MaxInBpsTime", SqlDbType.DateTime).Value = DBNull.Value;
			}
			else
			{
				textCommand.Parameters.Add("@MaxInBpsTime", SqlDbType.DateTime).Value = _interface.MaxInBpsTime;
			}
			textCommand.Parameters.AddWithValue("MaxOutBpsToday", _interface.MaxOutBpsToday);
			if (_interface.MaxOutBpsTime == DateTime.MinValue)
			{
				textCommand.Parameters.Add("@MaxOutBpsTime", SqlDbType.DateTime).Value = DBNull.Value;
			}
			else
			{
				textCommand.Parameters.Add("@MaxOutBpsTime", SqlDbType.DateTime).Value = _interface.MaxOutBpsTime;
			}
			if (_interface.NextRediscovery == DateTime.MinValue)
			{
				textCommand.Parameters.Add("@NextRediscovery", SqlDbType.DateTime).Value = DBNull.Value;
			}
			else
			{
				textCommand.Parameters.Add("@NextRediscovery", SqlDbType.DateTime).Value = _interface.NextRediscovery;
			}
			if (_interface.NextPoll == DateTime.MinValue)
			{
				textCommand.Parameters.Add("@NextPoll", SqlDbType.DateTime).Value = DBNull.Value;
			}
			else
			{
				textCommand.Parameters.Add("@NextPoll", SqlDbType.DateTime).Value = _interface.NextPoll;
			}
			textCommand.Parameters.AddWithValue("Counter64", _interface.Counter64);
			textCommand.Parameters.AddWithValue("StatCollection", _interface.StatCollection);
			if (_interface.LastSync == DateTime.MinValue)
			{
				textCommand.Parameters.Add("@LastSync", SqlDbType.DateTime).Value = DBNull.Value;
			}
			else
			{
				textCommand.Parameters.Add("@LastSync", SqlDbType.DateTime).Value = _interface.LastSync;
			}
			textCommand.Parameters.AddWithValue("InterfaceAlias", _interface.InterfaceAlias);
			textCommand.Parameters.AddWithValue("IfName", _interface.IfName);
			textCommand.Parameters.AddWithValue("Severity", _interface.Severity);
			textCommand.Parameters.AddWithValue("CustomBandwidth", _interface.CustomBandwidth);
			textCommand.Parameters.AddWithValue("UnPluggable", _interface.UnPluggable);
			textCommand.Parameters.AddWithValue("UnManaged", _interface.UnManaged);
			textCommand.Parameters.AddWithValue("UnManageFrom", CommonHelper.GetDateTimeValue(_interface.UnManageFrom));
			textCommand.Parameters.AddWithValue("UnManageUntil", CommonHelper.GetDateTimeValue(_interface.UnManageUntil));
			textCommand.Parameters.AddWithValue("CollectAvailability", _interface.CollectAvailability);
			InterfaceBLDAL.log.DebugFormat("Inserting interface. Locking thread. NodeID: {0}, FullName: {1}", _interface.NodeID, _interface.FullName);
			object obj = InterfaceBLDAL.insertInterfaceLock;
			lock (obj)
			{
				InterfaceBLDAL.log.DebugFormat("Inserting interface. Thread locked. NodeID: {0}, FullName: {1}", _interface.NodeID, _interface.FullName);
				_interface.InterfaceID = Convert.ToInt32(SqlHelper.ExecuteScalar(textCommand));
				InterfaceBLDAL.log.DebugFormat("Interface inserted with ID: {0}. NodeID: {1}, FullName: {2}", _interface.InterfaceID, _interface.NodeID, _interface.FullName);
			}
			return _interface.ID;
		}

		// Token: 0x170000FE RID: 254
		// (get) Token: 0x060007A4 RID: 1956 RVA: 0x000360D8 File Offset: 0x000342D8
		private static string SqlManagedInterfaceExists
		{
			get
			{
				return "IF NOT EXISTS (SELECT * FROM Interfaces WHERE \r\n                    NodeID = @NodeID AND \r\n                    PhysicalAddress = @PhysicalAddress AND\r\n                    InterfaceName = @InterfaceName AND\r\n                    InterfaceType = @InterfaceType AND\r\n                    InterfaceSubType = @InterfaceSubType AND\r\n                    (IfName = @IfName OR @IfName = '')\r\n                    )\r\n";
			}
		}

		// Token: 0x060007A5 RID: 1957 RVA: 0x000360E0 File Offset: 0x000342E0
		internal static Interfaces GetInterfaces()
		{
			if (!InterfaceBLDAL._areInterfacesAllowed)
			{
				return new Interfaces();
			}
			string commandString = "SELECT * FROM Interfaces";
			return Collection<int, Interface>.FillCollection<Interfaces>(new Collection<int, Interface>.CreateElement(InterfaceBLDAL.CreateInterface), commandString, null);
		}

		// Token: 0x060007A6 RID: 1958 RVA: 0x00036114 File Offset: 0x00034314
		internal static Interfaces GetNodeInterfaces(int nodeID)
		{
			if (!InterfaceBLDAL._areInterfacesAllowed)
			{
				return new Interfaces();
			}
			string commandString = "SELECT * FROM Interfaces WHERE NodeID=@NodeId";
			SqlParameter[] sqlParamList = new SqlParameter[]
			{
				new SqlParameter("@NodeId", nodeID)
			};
			return Collection<int, Interface>.FillCollection<Interfaces>(new Collection<int, Interface>.CreateElement(InterfaceBLDAL.CreateInterface), commandString, sqlParamList);
		}

		// Token: 0x060007A7 RID: 1959 RVA: 0x00036164 File Offset: 0x00034364
		internal static Interface GetInterface(int interfaceID)
		{
			string commandString = "SELECT * FROM Interfaces WHERE InterfaceID=@InterfaceID";
			SqlParameter[] sqlParamList = new SqlParameter[]
			{
				new SqlParameter("@InterfaceID", interfaceID)
			};
			return Collection<int, Interface>.GetCollectionItem<Interfaces>(new Collection<int, Interface>.CreateElement(InterfaceBLDAL.CreateInterface), commandString, sqlParamList);
		}

		// Token: 0x060007A8 RID: 1960 RVA: 0x000361A4 File Offset: 0x000343A4
		[Obsolete("NPM module handles deleting interfaces. Core just sends SWIS InterfaceIndication.", true)]
		internal static void DeleteInterface(int interfaceID)
		{
			InterfaceBLDAL.DeleteInterface(InterfaceBLDAL.GetInterface(interfaceID));
		}

		// Token: 0x060007A9 RID: 1961 RVA: 0x000361B4 File Offset: 0x000343B4
		[Obsolete("NPM module handles deleting interfaces. Core just sends SWIS InterfaceIndication.", true)]
		internal static void DeleteInterface(Interface _interface)
		{
			SqlCommand sqlCommand = SqlHelper.GetStoredProcCommand("swsp_DeleteInterface");
			sqlCommand.Parameters.Add("@id", SqlDbType.Int).Value = _interface.ID;
			SqlHelper.ExecuteNonQuery(sqlCommand);
			SqlCommand textCommand;
			sqlCommand = (textCommand = SqlHelper.GetTextCommand("delete from Pollers where NetObject = @NetObject"));
			try
			{
				sqlCommand.Parameters.Add("@NetObject", SqlDbType.VarChar, 50).Value = "I:" + _interface.ID;
				SqlHelper.ExecuteNonQuery(sqlCommand);
			}
			finally
			{
				if (textCommand != null)
				{
					((IDisposable)textCommand).Dispose();
				}
			}
		}

		// Token: 0x060007AA RID: 1962 RVA: 0x00036254 File Offset: 0x00034454
		internal static Interface CreateInterface(IDataReader reader)
		{
			Interface @interface = new Interface();
			int i = 0;
			while (i < reader.FieldCount)
			{
				string name = reader.GetName(i);
				uint num = <PrivateImplementationDetails>.ComputeStringHash(name);
				if (num <= 2359628423U)
				{
					if (num <= 1330915288U)
					{
						if (num <= 532412421U)
						{
							if (num <= 158572675U)
							{
								if (num <= 45007414U)
								{
									if (num != 6222351U)
									{
										if (num != 45007414U)
										{
											goto IL_E49;
										}
										if (!(name == "LastSync"))
										{
											goto IL_E49;
										}
										@interface.LastSync = DatabaseFunctions.GetDateTime(reader, i);
									}
									else
									{
										if (!(name == "Status"))
										{
											goto IL_E49;
										}
										@interface.Status = DatabaseFunctions.GetString(reader, i);
									}
								}
								else if (num != 109873071U)
								{
									if (num != 158572675U)
									{
										goto IL_E49;
									}
									if (!(name == "InUcastPps"))
									{
										goto IL_E49;
									}
									@interface.InUcastPps = DatabaseFunctions.GetFloat(reader, i);
								}
								else
								{
									if (!(name == "InDiscardsThisHour"))
									{
										goto IL_E49;
									}
									@interface.InDiscardsThisHour = DatabaseFunctions.GetFloat(reader, i);
								}
							}
							else if (num <= 256162167U)
							{
								if (num != 248528016U)
								{
									if (num != 256162167U)
									{
										goto IL_E49;
									}
									if (!(name == "RediscoveryInterval"))
									{
										goto IL_E49;
									}
									@interface.RediscoveryInterval = DatabaseFunctions.GetInt32(reader, i);
								}
								else
								{
									if (!(name == "StatusLED"))
									{
										goto IL_E49;
									}
									@interface.StatusLED = DatabaseFunctions.GetString(reader, i);
								}
							}
							else if (num != 319641761U)
							{
								if (num != 532412421U)
								{
									goto IL_E49;
								}
								if (!(name == "Caption"))
								{
									goto IL_E49;
								}
								@interface.Caption = DatabaseFunctions.GetString(reader, i);
							}
							else
							{
								if (!(name == "OutDiscardsToday"))
								{
									goto IL_E49;
								}
								@interface.OutDiscardsToday = DatabaseFunctions.GetFloat(reader, i);
							}
						}
						else if (num <= 914606840U)
						{
							if (num <= 588784106U)
							{
								if (num != 556656155U)
								{
									if (num != 588784106U)
									{
										goto IL_E49;
									}
									if (!(name == "MaxInBpsToday"))
									{
										goto IL_E49;
									}
									@interface.MaxInBpsToday = DatabaseFunctions.GetFloat(reader, i);
								}
								else
								{
									if (!(name == "InterfaceSpeed"))
									{
										goto IL_E49;
									}
									@interface.InterfaceSpeed = DatabaseFunctions.GetDouble(reader, i);
								}
							}
							else if (num != 773892499U)
							{
								if (num != 914606840U)
								{
									goto IL_E49;
								}
								if (!(name == "InterfaceType"))
								{
									goto IL_E49;
								}
								@interface.InterfaceType = DatabaseFunctions.GetInt32(reader, i);
							}
							else
							{
								if (!(name == "InPercentUtil"))
								{
									goto IL_E49;
								}
								@interface.InPercentUtil = DatabaseFunctions.GetFloat(reader, i);
							}
						}
						else if (num <= 1107068263U)
						{
							if (num != 1017626194U)
							{
								if (num != 1107068263U)
								{
									goto IL_E49;
								}
								if (!(name == "InPps"))
								{
									goto IL_E49;
								}
								@interface.InPps = DatabaseFunctions.GetFloat(reader, i);
							}
							else
							{
								if (!(name == "InterfaceTypeDescription"))
								{
									goto IL_E49;
								}
								@interface.InterfaceTypeDescription = DatabaseFunctions.GetString(reader, i);
							}
						}
						else if (num != 1257309411U)
						{
							if (num != 1330915288U)
							{
								goto IL_E49;
							}
							if (!(name == "MaxInBpsTime"))
							{
								goto IL_E49;
							}
							@interface.MaxInBpsTime = DatabaseFunctions.GetDateTime(reader, i);
						}
						else
						{
							if (!(name == "UnPluggable"))
							{
								goto IL_E49;
							}
							@interface.UnPluggable = DatabaseFunctions.GetBoolean(reader, i);
						}
					}
					else if (num <= 1887498962U)
					{
						if (num <= 1659882786U)
						{
							if (num <= 1567077553U)
							{
								if (num != 1390998017U)
								{
									if (num != 1567077553U)
									{
										goto IL_E49;
									}
									if (!(name == "InterfaceIcon"))
									{
										goto IL_E49;
									}
									@interface.InterfaceIcon = DatabaseFunctions.GetString(reader, i);
								}
								else
								{
									if (!(name == "Counter64"))
									{
										goto IL_E49;
									}
									@interface.Counter64 = DatabaseFunctions.GetString(reader, i);
								}
							}
							else if (num != 1571238303U)
							{
								if (num != 1659882786U)
								{
									goto IL_E49;
								}
								if (!(name == "OutMcastPps"))
								{
									goto IL_E49;
								}
								@interface.OutMcastPps = DatabaseFunctions.GetFloat(reader, i);
							}
							else
							{
								if (!(name == "MaxOutBpsToday"))
								{
									goto IL_E49;
								}
								@interface.MaxOutBpsToday = DatabaseFunctions.GetFloat(reader, i);
							}
						}
						else if (num <= 1798816559U)
						{
							if (num != 1798040858U)
							{
								if (num != 1798816559U)
								{
									goto IL_E49;
								}
								if (!(name == "InBandwidth"))
								{
									goto IL_E49;
								}
								@interface.InBandwidth = DatabaseFunctions.GetDouble(reader, i);
							}
							else
							{
								if (!(name == "InDiscardsToday"))
								{
									goto IL_E49;
								}
								@interface.InDiscardsToday = DatabaseFunctions.GetFloat(reader, i);
							}
						}
						else if (num != 1863478786U)
						{
							if (num != 1887498962U)
							{
								goto IL_E49;
							}
							if (!(name == "Severity"))
							{
								goto IL_E49;
							}
							@interface.Severity = DatabaseFunctions.GetInt32(reader, i);
						}
						else
						{
							if (!(name == "CollectAvailability"))
							{
								goto IL_E49;
							}
							@interface.CollectAvailability = (reader.IsDBNull(i) || DatabaseFunctions.GetBoolean(reader, i));
						}
					}
					else if (num <= 2155638092U)
					{
						if (num <= 2088678059U)
						{
							if (num != 1934659685U)
							{
								if (num != 2088678059U)
								{
									goto IL_E49;
								}
								if (!(name == "InMcastPps"))
								{
									goto IL_E49;
								}
								@interface.InMcastPps = DatabaseFunctions.GetFloat(reader, i);
							}
							else
							{
								if (!(name == "IfName"))
								{
									goto IL_E49;
								}
								@interface.IfName = DatabaseFunctions.GetString(reader, i);
							}
						}
						else if (num != 2117115014U)
						{
							if (num != 2155638092U)
							{
								goto IL_E49;
							}
							if (!(name == "OutPps"))
							{
								goto IL_E49;
							}
							@interface.OutPps = DatabaseFunctions.GetFloat(reader, i);
						}
						else
						{
							if (!(name == "OutErrorsThisHour"))
							{
								goto IL_E49;
							}
							@interface.OutErrorsThisHour = DatabaseFunctions.GetFloat(reader, i);
						}
					}
					else if (num <= 2236849620U)
					{
						if (num != 2229825399U)
						{
							if (num != 2236849620U)
							{
								goto IL_E49;
							}
							if (!(name == "InterfaceMTU"))
							{
								goto IL_E49;
							}
							@interface.InterfaceMTU = DatabaseFunctions.GetInt32(reader, i);
						}
						else
						{
							if (!(name == "FullName"))
							{
								goto IL_E49;
							}
							@interface.FullName = DatabaseFunctions.GetString(reader, i);
						}
					}
					else if (num != 2358485345U)
					{
						if (num != 2359628423U)
						{
							goto IL_E49;
						}
						if (!(name == "OperStatus"))
						{
							goto IL_E49;
						}
						@interface.OperStatus = DatabaseFunctions.GetInt16(reader, i);
					}
					else
					{
						if (!(name == "UnManageFrom"))
						{
							goto IL_E49;
						}
						@interface.UnManageFrom = DatabaseFunctions.GetDateTime(reader, i);
					}
				}
				else if (num <= 3449004665U)
				{
					if (num <= 2988209474U)
					{
						if (num <= 2711404065U)
						{
							if (num <= 2564163853U)
							{
								if (num != 2395316087U)
								{
									if (num != 2564163853U)
									{
										goto IL_E49;
									}
									if (!(name == "Inbps"))
									{
										goto IL_E49;
									}
									@interface.InBps = DatabaseFunctions.GetFloat(reader, i);
								}
								else
								{
									if (!(name == "AdminStatusLED"))
									{
										goto IL_E49;
									}
									@interface.AdminStatusLED = DatabaseFunctions.GetString(reader, i);
								}
							}
							else if (num != 2655931018U)
							{
								if (num != 2711404065U)
								{
									goto IL_E49;
								}
								if (!(name == "PollInterval"))
								{
									goto IL_E49;
								}
								@interface.PollInterval = DatabaseFunctions.GetInt32(reader, i);
							}
							else
							{
								if (!(name == "InErrorsToday"))
								{
									goto IL_E49;
								}
								@interface.InErrorsToday = DatabaseFunctions.GetFloat(reader, i);
							}
						}
						else if (num <= 2841884632U)
						{
							if (num != 2732404491U)
							{
								if (num != 2841884632U)
								{
									goto IL_E49;
								}
								if (!(name == "InterfaceLastChange"))
								{
									goto IL_E49;
								}
								@interface.InterfaceLastChange = DatabaseFunctions.GetDateTime(reader, i);
							}
							else
							{
								if (!(name == "OutPktSize"))
								{
									goto IL_E49;
								}
								@interface.OutPktSize = DatabaseFunctions.GetInt16(reader, i);
							}
						}
						else if (num != 2869862648U)
						{
							if (num != 2988209474U)
							{
								goto IL_E49;
							}
							if (!(name == "InterfaceSubType"))
							{
								goto IL_E49;
							}
							@interface.InterfaceSubType = DatabaseFunctions.GetInt32(reader, i);
						}
						else
						{
							if (!(name == "NodeID"))
							{
								goto IL_E49;
							}
							@interface.NodeID = DatabaseFunctions.GetInt32(reader, i);
						}
					}
					else if (num <= 3200264202U)
					{
						if (num <= 3143955903U)
						{
							if (num != 3035921756U)
							{
								if (num != 3143955903U)
								{
									goto IL_E49;
								}
								if (!(name == "InErrorsThisHour"))
								{
									goto IL_E49;
								}
								@interface.InErrorsThisHour = DatabaseFunctions.GetFloat(reader, i);
							}
							else
							{
								if (!(name == "InPktSize"))
								{
									goto IL_E49;
								}
								@interface.InPktSize = DatabaseFunctions.GetInt16(reader, i);
							}
						}
						else if (num != 3160731452U)
						{
							if (num != 3200264202U)
							{
								goto IL_E49;
							}
							if (!(name == "OutDiscardsThisHour"))
							{
								goto IL_E49;
							}
							@interface.OutDiscardsThisHour = DatabaseFunctions.GetFloat(reader, i);
						}
						else
						{
							if (!(name == "InterfaceAlias"))
							{
								goto IL_E49;
							}
							@interface.InterfaceAlias = DatabaseFunctions.GetString(reader, i);
						}
					}
					else if (num <= 3282255531U)
					{
						if (num != 3216090560U)
						{
							if (num != 3282255531U)
							{
								goto IL_E49;
							}
							if (!(name == "UnManageUntil"))
							{
								goto IL_E49;
							}
							@interface.UnManageUntil = DatabaseFunctions.GetDateTime(reader, i);
						}
						else
						{
							if (!(name == "ObjectSubType"))
							{
								goto IL_E49;
							}
							@interface.ObjectSubType = DatabaseFunctions.GetString(reader, i);
						}
					}
					else if (num != 3336839519U)
					{
						if (num != 3449004665U)
						{
							goto IL_E49;
						}
						if (!(name == "NextRediscovery"))
						{
							goto IL_E49;
						}
						@interface.NextRediscovery = DatabaseFunctions.GetDateTime(reader, i);
					}
					else
					{
						if (!(name == "CustomBandwidth"))
						{
							goto IL_E49;
						}
						@interface.CustomBandwidth = DatabaseFunctions.GetBoolean(reader, i);
					}
				}
				else if (num <= 3792413811U)
				{
					if (num <= 3654310386U)
					{
						if (num <= 3489145781U)
						{
							if (num != 3469824504U)
							{
								if (num != 3489145781U)
								{
									goto IL_E49;
								}
								if (!(name == "OutErrorsToday"))
								{
									goto IL_E49;
								}
								@interface.OutErrorsToday = DatabaseFunctions.GetFloat(reader, i);
							}
							else
							{
								if (!(name == "OutPercentUtil"))
								{
									goto IL_E49;
								}
								@interface.OutPercentUtil = DatabaseFunctions.GetFloat(reader, i);
							}
						}
						else if (num != 3521202693U)
						{
							if (num != 3654310386U)
							{
								goto IL_E49;
							}
							if (!(name == "Outbps"))
							{
								goto IL_E49;
							}
							@interface.OutBps = DatabaseFunctions.GetFloat(reader, i);
						}
						else
						{
							if (!(name == "NextPoll"))
							{
								goto IL_E49;
							}
							@interface.NextPoll = DatabaseFunctions.GetDateTime(reader, i);
						}
					}
					else if (num <= 3736301255U)
					{
						if (num != 3683179112U)
						{
							if (num != 3736301255U)
							{
								goto IL_E49;
							}
							if (!(name == "MaxOutBpsTime"))
							{
								goto IL_E49;
							}
							@interface.MaxOutBpsTime = DatabaseFunctions.GetDateTime(reader, i);
						}
						else
						{
							if (!(name == "OperStatusLED"))
							{
								goto IL_E49;
							}
							@interface.OperStatusLED = DatabaseFunctions.GetString(reader, i);
						}
					}
					else if (num != 3769769805U)
					{
						if (num != 3792413811U)
						{
							goto IL_E49;
						}
						if (!(name == "InterfaceName"))
						{
							goto IL_E49;
						}
						@interface.InterfaceName = DatabaseFunctions.GetString(reader, i);
					}
					else
					{
						if (!(name == "InterfaceID"))
						{
							goto IL_E49;
						}
						@interface.InterfaceID = DatabaseFunctions.GetInt32(reader, i);
					}
				}
				else if (num <= 3971248279U)
				{
					if (num <= 3885329530U)
					{
						if (num != 3854733374U)
						{
							if (num != 3885329530U)
							{
								goto IL_E49;
							}
							if (!(name == "OutUcastPps"))
							{
								goto IL_E49;
							}
							@interface.OutUcastPps = DatabaseFunctions.GetFloat(reader, i);
						}
						else
						{
							if (!(name == "AdminStatus"))
							{
								goto IL_E49;
							}
							@interface.AdminStatus = DatabaseFunctions.GetInt16(reader, i);
						}
					}
					else if (num != 3936513175U)
					{
						if (num != 3971248279U)
						{
							goto IL_E49;
						}
						if (!(name == "UnManaged"))
						{
							goto IL_E49;
						}
						@interface.UnManaged = DatabaseFunctions.GetBoolean(reader, i);
					}
					else
					{
						if (!(name == "StatCollection"))
						{
							goto IL_E49;
						}
						@interface.StatCollection = DatabaseFunctions.GetInt16(reader, i);
					}
				}
				else if (num <= 4037488300U)
				{
					if (num != 3995067068U)
					{
						if (num != 4037488300U)
						{
							goto IL_E49;
						}
						if (!(name == "InterfaceIndex"))
						{
							goto IL_E49;
						}
						@interface.InterfaceIndex = DatabaseFunctions.GetInt32(reader, i);
					}
					else
					{
						if (!(name == "OutBandwidth"))
						{
							goto IL_E49;
						}
						@interface.OutBandwidth = DatabaseFunctions.GetDouble(reader, i);
					}
				}
				else if (num != 4177086267U)
				{
					if (num != 4193613824U)
					{
						goto IL_E49;
					}
					if (!(name == "PhysicalAddress"))
					{
						goto IL_E49;
					}
					@interface.PhysicalAddress = DatabaseFunctions.GetString(reader, i);
				}
				else
				{
					if (!(name == "InterfaceTypeName"))
					{
						goto IL_E49;
					}
					@interface.InterfaceTypeName = DatabaseFunctions.GetString(reader, i);
				}
				IL_E82:
				i++;
				continue;
				IL_E49:
				if (CustomPropertyMgr.IsCustom("Interfaces", name))
				{
					@interface.CustomProperties[name] = reader[i];
					goto IL_E82;
				}
				InterfaceBLDAL.log.DebugFormat("Skipping Interface property {0}, value {1}", name, reader[i]);
				goto IL_E82;
			}
			return @interface;
		}

		// Token: 0x04000246 RID: 582
		private static readonly Log log = new Log();

		// Token: 0x04000247 RID: 583
		private static object insertInterfaceLock = new object();

		// Token: 0x04000248 RID: 584
		private static bool _areInterfacesAllowed = PackageManager.InstanceWithCache.IsPackageInstalled("Orion.Interfaces");
	}
}
