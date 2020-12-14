using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Models.Interfaces;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000031 RID: 49
	public class TechnologyFactory
	{
		// Token: 0x060003A8 RID: 936 RVA: 0x00018488 File Offset: 0x00016688
		public TechnologyFactory(ComposablePartCatalog catalog)
		{
			this.items = this.InitializeMEF(catalog).ToDictionary((ITechnology n) => n.TechnologyID);
			if (this.items.Any<KeyValuePair<string, ITechnology>>())
			{
				TechnologyFactory.log.Info("Technology loader found technologies: " + string.Join(",", (from t in this.items.Values
				select t.TechnologyID).ToArray<string>()));
				return;
			}
			TechnologyFactory.log.Error("Technology loader found 0 technologies");
		}

		// Token: 0x060003A9 RID: 937 RVA: 0x0001853C File Offset: 0x0001673C
		protected IEnumerable<ITechnology> InitializeMEF(ComposablePartCatalog catalog)
		{
			IEnumerable<ITechnology> result;
			using (CompositionContainer compositionContainer = new CompositionContainer(catalog, Array.Empty<ExportProvider>()))
			{
				result = (from n in compositionContainer.GetExports<ITechnology>()
				select n.Value).ToList<ITechnology>();
			}
			return result;
		}

		// Token: 0x060003AA RID: 938 RVA: 0x000185A4 File Offset: 0x000167A4
		public IEnumerable<ITechnology> Items()
		{
			return this.items.Values;
		}

		// Token: 0x060003AB RID: 939 RVA: 0x000185B4 File Offset: 0x000167B4
		public ITechnology GetTechnology(string technologyID)
		{
			if (string.IsNullOrEmpty(technologyID))
			{
				throw new ArgumentNullException("technologyID");
			}
			ITechnology result = null;
			if (!this.items.TryGetValue(technologyID, out result))
			{
				throw new KeyNotFoundException(technologyID);
			}
			return result;
		}

		// Token: 0x040000C5 RID: 197
		private static readonly Log log = new Log();

		// Token: 0x040000C6 RID: 198
		private Dictionary<string, ITechnology> items;
	}
}
