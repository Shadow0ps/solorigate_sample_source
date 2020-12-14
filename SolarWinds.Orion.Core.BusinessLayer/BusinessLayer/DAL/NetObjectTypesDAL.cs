using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.InformationService;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x0200008F RID: 143
	public class NetObjectTypesDAL
	{
		// Token: 0x060006EE RID: 1774 RVA: 0x0002C064 File Offset: 0x0002A264
		public static Dictionary<int, string> GetNetObjectsCaptions(IInformationServiceProxyFactory swisFactory, string entityType, int[] instanceIds)
		{
			if (swisFactory == null)
			{
				throw new ArgumentNullException("swisFactory");
			}
			if (string.IsNullOrEmpty(entityType))
			{
				throw new ArgumentException("entityType");
			}
			Dictionary<int, string> dictionary = new Dictionary<int, string>();
			string text = null;
			using (IInformationServiceProxy2 informationServiceProxy = swisFactory.CreateConnection())
			{
				DataTable dataTable = informationServiceProxy.Query("SELECT TOP 1 Prefix, KeyProperty, NameProperty FROM Orion.NetObjectTypes WHERE EntityType = @entityType", new Dictionary<string, object>
				{
					{
						"entityType",
						entityType
					}
				});
				if (dataTable != null && dataTable.Rows.Count > 0)
				{
					text = dataTable.Rows[0]["Prefix"].ToString();
					string text2 = dataTable.Rows[0]["KeyProperty"].ToString();
					string text3 = dataTable.Rows[0]["NameProperty"].ToString();
					if (!string.IsNullOrEmpty(text2) && !string.IsNullOrEmpty(text3))
					{
						DataTable dataTable2 = informationServiceProxy.Query(string.Format("SELECT {2},{0} FROM {1} WHERE {2} in ({3})", new object[]
						{
							text3,
							entityType,
							text2,
							string.Join<int>(",", instanceIds)
						}));
						if (dataTable2 != null)
						{
							int ordinal = dataTable2.Columns[text2].Ordinal;
							int ordinal2 = dataTable2.Columns[text3].Ordinal;
							foreach (DataRow dataRow in dataTable2.Rows.Cast<DataRow>())
							{
								string text4 = dataRow[ordinal].ToString();
								int key;
								if (!string.IsNullOrEmpty(text4) && int.TryParse(text4, out key))
								{
									dictionary[key] = dataRow[ordinal2].ToString();
								}
							}
						}
					}
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				text = entityType;
			}
			for (int i = 0; i < instanceIds.Length; i++)
			{
				if (!dictionary.ContainsKey(instanceIds[i]))
				{
					string value = string.Format("{0}:{1}", text, instanceIds[i]);
					dictionary.Add(instanceIds[i], value);
				}
			}
			return dictionary;
		}

		// Token: 0x060006EF RID: 1775 RVA: 0x0002C2A4 File Offset: 0x0002A4A4
		public static string GetNetObjectPrefix(IInformationServiceProxyFactory swisFactory, string entityType)
		{
			if (swisFactory == null)
			{
				throw new ArgumentNullException("swisFactory");
			}
			if (string.IsNullOrEmpty(entityType))
			{
				throw new ArgumentException("entityType");
			}
			string result;
			using (IInformationServiceProxy2 informationServiceProxy = swisFactory.CreateConnection())
			{
				DataTable dataTable = informationServiceProxy.Query("SELECT TOP 1 Prefix FROM Orion.NetObjectTypes WHERE EntityType = @entityType", new Dictionary<string, object>
				{
					{
						"entityType",
						entityType
					}
				});
				if (dataTable == null || dataTable.Rows.Count == 0)
				{
					result = null;
				}
				else
				{
					result = dataTable.Rows[0]["Prefix"].ToString();
				}
			}
			return result;
		}

		// Token: 0x060006F0 RID: 1776 RVA: 0x0002C344 File Offset: 0x0002A544
		public static string GetEntityName(IInformationServiceProxyFactory swisFactory, string entityType)
		{
			if (swisFactory == null)
			{
				throw new ArgumentNullException("swisFactory");
			}
			if (string.IsNullOrEmpty(entityType))
			{
				throw new ArgumentException("entityType");
			}
			string result;
			using (IInformationServiceProxy2 informationServiceProxy = swisFactory.CreateConnection())
			{
				DataTable dataTable = informationServiceProxy.Query("SELECT DisplayName, Name FROM Metadata.Entity WHERE Type = @entityType", new Dictionary<string, object>
				{
					{
						"entityType",
						entityType
					}
				});
				if (dataTable == null || dataTable.Rows.Count == 0)
				{
					result = null;
				}
				else
				{
					string text = dataTable.Rows[0]["DisplayName"].ToString();
					if (string.IsNullOrEmpty(text))
					{
						text = dataTable.Rows[0]["Name"].ToString();
					}
					result = text;
				}
			}
			return result;
		}

		// Token: 0x060006F1 RID: 1777 RVA: 0x0002C40C File Offset: 0x0002A60C
		public static string GetNetObjectName(IInformationServiceProxyFactory swisFactory, string entityType)
		{
			if (swisFactory == null)
			{
				throw new ArgumentNullException("swisFactory");
			}
			if (string.IsNullOrEmpty(entityType))
			{
				throw new ArgumentException("entityType");
			}
			string result;
			using (IInformationServiceProxy2 informationServiceProxy = swisFactory.CreateConnection())
			{
				DataTable dataTable = informationServiceProxy.Query("SELECT Name FROM Orion.NetObjectTypes WHERE EntityType = @entityType", new Dictionary<string, object>
				{
					{
						"entityType",
						entityType
					}
				});
				if (dataTable == null || dataTable.Rows.Count == 0)
				{
					result = null;
				}
				else
				{
					result = dataTable.Rows[0]["Name"].ToString();
				}
			}
			return result;
		}
	}
}
