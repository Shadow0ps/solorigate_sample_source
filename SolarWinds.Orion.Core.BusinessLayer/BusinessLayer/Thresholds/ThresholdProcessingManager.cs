using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using SolarWinds.Common.Threading;
using SolarWinds.Orion.Core.Common.Catalogs;
using SolarWinds.Orion.Core.Common.Settings;
using SolarWinds.Orion.Core.Common.Thresholds;

namespace SolarWinds.Orion.Core.BusinessLayer.Thresholds
{
	// Token: 0x02000055 RID: 85
	internal class ThresholdProcessingManager
	{
		// Token: 0x0600050F RID: 1295 RVA: 0x00020F24 File Offset: 0x0001F124
		internal ThresholdProcessingManager(ComposablePartCatalog catalog, ICollectorSettings settings)
		{
			this.ComposeParts(catalog);
			this._engine = new ThresholdProcessingEngine(this._thresholdProcessors, this._thresholdDataProviders, new ThresholdIndicator(), settings)
			{
				BatchSize = BusinessLayerSettings.Instance.ThresholdsProcessingBatchSize,
				BaselineTimeFrame = BusinessLayerSettings.Instance.ThresholdsProcessingDefaultTimeFrame
			};
		}

		// Token: 0x06000510 RID: 1296 RVA: 0x00020F94 File Offset: 0x0001F194
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

		// Token: 0x170000C2 RID: 194
		// (get) Token: 0x06000511 RID: 1297 RVA: 0x00020FDC File Offset: 0x0001F1DC
		public static ThresholdProcessingManager Instance
		{
			get
			{
				return ThresholdProcessingManager._instance.Value;
			}
		}

		// Token: 0x170000C3 RID: 195
		// (get) Token: 0x06000512 RID: 1298 RVA: 0x00020FE8 File Offset: 0x0001F1E8
		// (set) Token: 0x06000513 RID: 1299 RVA: 0x00020FEF File Offset: 0x0001F1EF
		internal static CompositionContainer CompositionContainer { get; set; }

		// Token: 0x170000C4 RID: 196
		// (get) Token: 0x06000514 RID: 1300 RVA: 0x00020FF7 File Offset: 0x0001F1F7
		public ThresholdProcessingEngine Engine
		{
			get
			{
				return this._engine;
			}
		}

		// Token: 0x04000163 RID: 355
		private static readonly LazyWithoutExceptionCache<ThresholdProcessingManager> _instance = new LazyWithoutExceptionCache<ThresholdProcessingManager>(delegate()
		{
			ThresholdProcessingManager result;
			using (ComposablePartCatalog catalogForArea = MEFPluginsLoader.Instance.GetCatalogForArea("Thresholds"))
			{
				result = new ThresholdProcessingManager(catalogForArea, CollectorSettings.Instance);
			}
			return result;
		});

		// Token: 0x04000164 RID: 356
		[ImportMany(typeof(IThresholdDataProcessor))]
		private IEnumerable<IThresholdDataProcessor> _thresholdProcessors = Enumerable.Empty<IThresholdDataProcessor>();

		// Token: 0x04000165 RID: 357
		[ImportMany(typeof(ThresholdDataProvider))]
		private IEnumerable<ThresholdDataProvider> _thresholdDataProviders = Enumerable.Empty<ThresholdDataProvider>();

		// Token: 0x04000166 RID: 358
		private readonly ThresholdProcessingEngine _engine;
	}
}
