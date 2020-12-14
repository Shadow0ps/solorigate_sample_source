using System;
using System.ComponentModel.Composition.Primitives;
using System.Threading;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common.Catalogs;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000042 RID: 66
	public class TechnologyManager
	{
		// Token: 0x0600042A RID: 1066 RVA: 0x0001C598 File Offset: 0x0001A798
		internal TechnologyManager(ComposablePartCatalog catalog)
		{
			this.Initialize(catalog);
		}

		// Token: 0x0600042B RID: 1067 RVA: 0x0001C5A8 File Offset: 0x0001A7A8
		public TechnologyManager()
		{
			using (ComposablePartCatalog catalogForArea = MEFPluginsLoader.Instance.GetCatalogForArea(TechnologyManager.TechonologyMEFPluginAreaID))
			{
				this.Initialize(catalogForArea);
			}
		}

		// Token: 0x0600042C RID: 1068 RVA: 0x0001C5F0 File Offset: 0x0001A7F0
		private void Initialize(ComposablePartCatalog catalog)
		{
			this.techs = new TechnologyFactory(catalog);
			this.impls = new TechnologyPollingFactory(catalog);
		}

		// Token: 0x1700007D RID: 125
		// (get) Token: 0x0600042D RID: 1069 RVA: 0x0001C60A File Offset: 0x0001A80A
		public static TechnologyManager Instance
		{
			get
			{
				return TechnologyManager.cachedLazyInstance.Value;
			}
		}

		// Token: 0x1700007E RID: 126
		// (get) Token: 0x0600042E RID: 1070 RVA: 0x0001C616 File Offset: 0x0001A816
		public TechnologyFactory TechnologyFactory
		{
			get
			{
				return this.techs;
			}
		}

		// Token: 0x1700007F RID: 127
		// (get) Token: 0x0600042F RID: 1071 RVA: 0x0001C61E File Offset: 0x0001A81E
		public TechnologyPollingFactory TechnologyPollingFactory
		{
			get
			{
				return this.impls;
			}
		}

		// Token: 0x040000F9 RID: 249
		private static readonly Log log = new Log();

		// Token: 0x040000FA RID: 250
		public static readonly string TechonologyMEFPluginAreaID = "Technology";

		// Token: 0x040000FB RID: 251
		private TechnologyFactory techs;

		// Token: 0x040000FC RID: 252
		private TechnologyPollingFactory impls;

		// Token: 0x040000FD RID: 253
		private static Lazy<TechnologyManager> cachedLazyInstance = new Lazy<TechnologyManager>(LazyThreadSafetyMode.ExecutionAndPublication);
	}
}
