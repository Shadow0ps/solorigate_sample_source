using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Data;
using System.Data.SqlClient;
using SolarWinds.Common.Threading;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Catalogs;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Common.i18n;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Strings;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x0200009E RID: 158
	internal class NetworkDeviceDAL
	{
		// Token: 0x060007AD RID: 1965 RVA: 0x0003711E File Offset: 0x0003531E
		internal NetworkDeviceDAL(ComposablePartCatalog catalog)
		{
			this.ComposeParts(catalog);
		}

		// Token: 0x060007AE RID: 1966 RVA: 0x00037130 File Offset: 0x00035330
		private void ComposeParts(ComposablePartCatalog catalog)
		{
			using (CompositionContainer compositionContainer = new CompositionContainer(catalog, Array.Empty<ExportProvider>()))
			{
				compositionContainer.ComposeParts(new object[]
				{
					this
				});
			}
		}

		// Token: 0x170000FF RID: 255
		// (get) Token: 0x060007AF RID: 1967 RVA: 0x00037178 File Offset: 0x00035378
		public static NetworkDeviceDAL Instance
		{
			get
			{
				NetworkDeviceDAL value = NetworkDeviceDAL.instance.Value;
				if (value == null)
				{
					throw new InvalidOperationException("Unable to instantiate NetworkDeviceDAL. That was most likely caused by failed try of MEF trying to resolve parts.");
				}
				return value;
			}
		}

		// Token: 0x060007B0 RID: 1968 RVA: 0x00037194 File Offset: 0x00035394
		public List<Node> GetNetworkDevices(CorePageType pageType, List<int> limitationIDs)
		{
			switch (pageType)
			{
			case 1:
				return AlertDAL.GetAlertNetObjects(limitationIDs);
			case 2:
			{
				INetworkDeviceDal networkDeviceDal = this.syslogDal;
				return ((networkDeviceDal != null) ? networkDeviceDal.GetNetObjects(limitationIDs) : null) ?? new List<Node>(0);
			}
			case 3:
			{
				INetworkDeviceDal networkDeviceDal2 = this.trapDal;
				return ((networkDeviceDal2 != null) ? networkDeviceDal2.GetNetObjects(limitationIDs) : null) ?? new List<Node>(0);
			}
			default:
				throw new NotImplementedException("Unsupported page type");
			}
		}

		// Token: 0x060007B1 RID: 1969 RVA: 0x00037204 File Offset: 0x00035404
		public Dictionary<int, string> GetNetworkDeviceNamesForPage(CorePageType pageType, List<int> limitationIDs, bool includeBasic)
		{
			switch (pageType)
			{
			case 1:
				return AlertDAL.GetNodeData(limitationIDs, includeBasic);
			case 2:
			{
				INetworkDeviceDal networkDeviceDal = this.syslogDal;
				return ((networkDeviceDal != null) ? networkDeviceDal.GetNodeData(limitationIDs) : null) ?? new Dictionary<int, string>(0);
			}
			case 3:
			{
				INetworkDeviceDal networkDeviceDal2 = this.trapDal;
				return ((networkDeviceDal2 != null) ? networkDeviceDal2.GetNodeData(limitationIDs) : null) ?? new Dictionary<int, string>(0);
			}
			case 4:
				return EventsDAL.GetNodeData(limitationIDs);
			default:
				throw new NotImplementedException("Unsupported page type");
			}
		}

		// Token: 0x060007B2 RID: 1970 RVA: 0x00037280 File Offset: 0x00035480
		public Dictionary<int, string> GetNetworkDeviceNamesForPage(CorePageType pageType, List<int> limitationIDs)
		{
			return this.GetNetworkDeviceNamesForPage(pageType, limitationIDs, true);
		}

		// Token: 0x060007B3 RID: 1971 RVA: 0x0003728C File Offset: 0x0003548C
		public Dictionary<string, string> GetNetworkDeviceTypes(List<int> limitationIDs)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(Limitation.LimitSQL(" SELECT Vendor + '%' AS Value, '' AS Name \r\n FROM Nodes \r\n WHERE (Vendor <> '')  \r\n GROUP BY Vendor HAVING (Count(Vendor) > 1) \r\n UNION\r\n SELECT MachineType AS Value, MachineType AS Name \r\n FROM Nodes\r\n WHERE MachineType <> '' \r\n GROUP BY MachineType ", limitationIDs)))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					int ordinal = dataReader.GetOrdinal("Value");
					int ordinal2 = dataReader.GetOrdinal("Name");
					string libcode_VB0_;
					using (LocaleThreadState.EnsurePrimaryLocale())
					{
						libcode_VB0_ = Resources.LIBCODE_VB0_61;
						goto IL_97;
					}
					IL_53:
					string text = dataReader.GetString(ordinal2);
					string @string = dataReader.GetString(ordinal);
					text = ((!string.IsNullOrEmpty(text)) ? text : string.Format(libcode_VB0_, @string.Substring(0, @string.Length - 1)));
					dictionary.Add(@string, text);
					IL_97:
					if (dataReader.Read())
					{
						goto IL_53;
					}
				}
			}
			return dictionary;
		}

		// Token: 0x060007B4 RID: 1972 RVA: 0x00037378 File Offset: 0x00035578
		public List<string> GetAllVendors(List<int> limitationIDs)
		{
			List<string> list = new List<string>();
			using (SqlCommand textCommand = SqlHelper.GetTextCommand(Limitation.LimitSQL("SELECT DISTINCT Vendor \r\n From Nodes WHERE (Vendor <> '')", limitationIDs)))
			{
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						list.Add(DatabaseFunctions.GetString(dataReader, "Vendor"));
					}
				}
			}
			return list;
		}

		// Token: 0x04000249 RID: 585
		private static readonly LazyWithoutExceptionCache<NetworkDeviceDAL> instance = new LazyWithoutExceptionCache<NetworkDeviceDAL>(delegate()
		{
			NetworkDeviceDAL result;
			using (ComposablePartCatalog catalogForArea = MEFPluginsLoader.Instance.GetCatalogForArea("NetworkDevice"))
			{
				result = new NetworkDeviceDAL(catalogForArea);
			}
			return result;
		});

		// Token: 0x0400024A RID: 586
		[Import("Syslog", typeof(INetworkDeviceDal), AllowDefault = true, RequiredCreationPolicy = CreationPolicy.Shared)]
		private INetworkDeviceDal syslogDal;

		// Token: 0x0400024B RID: 587
		[Import("Trap", typeof(INetworkDeviceDal), AllowDefault = true, RequiredCreationPolicy = CreationPolicy.Shared)]
		private INetworkDeviceDal trapDal;
	}
}
