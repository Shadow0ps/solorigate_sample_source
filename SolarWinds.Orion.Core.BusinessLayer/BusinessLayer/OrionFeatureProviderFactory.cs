using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using SolarWinds.Orion.Core.Common.Catalogs;
using SolarWinds.Orion.Core.Models.OrionFeature;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000014 RID: 20
	public class OrionFeatureProviderFactory : IOrionFearureProviderFactory
	{
		// Token: 0x0600028B RID: 651 RVA: 0x0000FF1C File Offset: 0x0000E11C
		public static OrionFeatureProviderFactory CreateInstance()
		{
			OrionFeatureProviderFactory result;
			using (ComposablePartCatalog catalogForArea = MEFPluginsLoader.Instance.GetCatalogForArea("OrionFeature"))
			{
				result = new OrionFeatureProviderFactory(catalogForArea);
			}
			return result;
		}

		// Token: 0x0600028C RID: 652 RVA: 0x0000FF60 File Offset: 0x0000E160
		public OrionFeatureProviderFactory(ComposablePartCatalog catalog)
		{
			if (catalog == null)
			{
				throw new ArgumentNullException("catalog");
			}
			using (CompositionContainer compositionContainer = new CompositionContainer(catalog, Array.Empty<ExportProvider>()))
			{
				compositionContainer.ComposeParts(new object[]
				{
					this
				});
			}
		}

		// Token: 0x0600028D RID: 653 RVA: 0x0000FFC4 File Offset: 0x0000E1C4
		public IEnumerable<IOrionFeatureProvider> GetProviders()
		{
			return this._providers;
		}

		// Token: 0x04000068 RID: 104
		[ImportMany(typeof(IOrionFeatureProvider))]
		private IEnumerable<IOrionFeatureProvider> _providers = Enumerable.Empty<IOrionFeatureProvider>();
	}
}
