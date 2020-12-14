using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Models.Interfaces;
using SolarWinds.Orion.Core.Models.Technology;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200003F RID: 63
	public class TechnologyPollingFactory
	{
		// Token: 0x0600041A RID: 1050 RVA: 0x0001BE34 File Offset: 0x0001A034
		public TechnologyPollingFactory(ComposablePartCatalog catalog)
		{
			this.providers = TechnologyPollingFactory.InitializeMEF(catalog).ToList<ITechnologyPollingProvider>();
			if (this.providers.Any<ITechnologyPollingProvider>())
			{
				TechnologyPollingFactory.log.Info("Technology loader found technology polling providers: " + string.Join(",", (from t in this.providers
				select t.GetType().FullName).ToArray<string>()));
				return;
			}
			TechnologyPollingFactory.log.Error("Technology loader found 0 technology polling providers");
		}

		// Token: 0x0600041B RID: 1051 RVA: 0x0001BED0 File Offset: 0x0001A0D0
		protected static IEnumerable<ITechnologyPollingProvider> InitializeMEF(ComposablePartCatalog catalog)
		{
			IEnumerable<ITechnologyPollingProvider> result;
			using (CompositionContainer compositionContainer = new CompositionContainer(catalog, Array.Empty<ExportProvider>()))
			{
				result = (from n in compositionContainer.GetExports<ITechnologyPollingProvider>()
				select n.Value).ToList<ITechnologyPollingProvider>();
			}
			return result;
		}

		// Token: 0x0600041C RID: 1052 RVA: 0x0001BF38 File Offset: 0x0001A138
		public IEnumerable<ITechnologyPolling> Items()
		{
			return this.providers.SelectMany((ITechnologyPollingProvider n) => n.Items);
		}

		// Token: 0x0600041D RID: 1053 RVA: 0x0001BF64 File Offset: 0x0001A164
		public IEnumerable<ITechnologyPolling> ItemsByTechnology(string technologyID)
		{
			if (string.IsNullOrEmpty(technologyID))
			{
				throw new ArgumentNullException("technologyID");
			}
			return from n in this.Items()
			where n.TechnologyID == technologyID
			select n;
		}

		// Token: 0x0600041E RID: 1054 RVA: 0x0001BFB0 File Offset: 0x0001A1B0
		public ITechnologyPolling GetTechnologyPolling(string technologyPollingID)
		{
			if (string.IsNullOrEmpty(technologyPollingID))
			{
				throw new ArgumentNullException("technologyPollingID");
			}
			return this.Items().Single((ITechnologyPolling n) => n.TechnologyPollingID == technologyPollingID);
		}

		// Token: 0x0600041F RID: 1055 RVA: 0x0001BFFC File Offset: 0x0001A1FC
		public int[] EnableDisableAssignments(string technologyPollingID, bool enable, int[] netObjectIDs = null)
		{
			if (string.IsNullOrEmpty(technologyPollingID))
			{
				throw new ArgumentNullException("technologyPollingID");
			}
			ITechnologyPolling technologyPolling = this.GetTechnologyPolling(technologyPollingID);
			int[] array = (netObjectIDs == null) ? technologyPolling.EnableDisableAssignment(enable) : technologyPolling.EnableDisableAssignment(enable, netObjectIDs);
			array = (array ?? new int[0]);
			TechnologyPollingFactory.log.DebugFormat("{0} TechnologyPolling:'{1}' of Technology:'{2}' on NetObjects:'{3}'", new object[]
			{
				enable ? "Enabled" : "Disabled",
				technologyPollingID,
				technologyPolling.TechnologyID,
				(array == null) ? "" : string.Join<int>(",", array)
			});
			if (enable)
			{
				foreach (ITechnologyPolling technologyPolling2 in this.ItemsByTechnology(technologyPolling.TechnologyID))
				{
					if (!technologyPollingID.Equals(technologyPolling2.TechnologyPollingID, StringComparison.Ordinal))
					{
						int[] array2 = technologyPolling2.EnableDisableAssignment(false, array);
						array2 = (array2 ?? new int[0]);
						TechnologyPollingFactory.log.DebugFormat("{0} TechnologyPolling:'{1}' of Technology:'{2}' on NetObjects:'{3}'", new object[]
						{
							"Disabled",
							technologyPolling2.TechnologyPollingID,
							technologyPolling2.TechnologyID,
							(array2 == null) ? "" : string.Join<int>(",", array2)
						});
					}
				}
			}
			if (BusinessLayerSettings.Instance.EnableTechnologyPollingAssignmentsChangesAuditing)
			{
				this.changesIndicator.ReportTechnologyPollingAssignmentIndication(technologyPolling, array, enable);
			}
			return array;
		}

		// Token: 0x06000420 RID: 1056 RVA: 0x0001C164 File Offset: 0x0001A364
		public IEnumerable<TechnologyPollingAssignment> GetAssignments(string technologyPollingID)
		{
			if (string.IsNullOrEmpty(technologyPollingID))
			{
				throw new ArgumentNullException("technologyPollingID");
			}
			return this.GetTechnologyPolling(technologyPollingID).GetAssignments();
		}

		// Token: 0x06000421 RID: 1057 RVA: 0x0001C185 File Offset: 0x0001A385
		public IEnumerable<TechnologyPollingAssignment> GetAssignments(string technologyPollingID, int[] netObjectIDs)
		{
			if (string.IsNullOrEmpty(technologyPollingID))
			{
				throw new ArgumentNullException("technologyPollingID");
			}
			return this.GetTechnologyPolling(technologyPollingID).GetAssignments(netObjectIDs);
		}

		// Token: 0x06000422 RID: 1058 RVA: 0x0001C1A7 File Offset: 0x0001A3A7
		public IEnumerable<TechnologyPollingAssignment> GetAssignmentsFiltered(string[] technologyPollingIDsFilter, int[] netObjectIDsFilter, string[] targetEntitiesFilter, bool[] enabledFilter)
		{
			bool? enabledFilterValue = null;
			if (enabledFilter != null)
			{
				if (enabledFilter.Length == 0)
				{
					yield break;
				}
				if (enabledFilter.Distinct<bool>().Count<bool>() == 1)
				{
					enabledFilterValue = new bool?(enabledFilter.First<bool>());
				}
			}
			ILookup<string, ITechnologyPolling> technologyPollingsByTechnology = (from tp in this.Items()
			where technologyPollingIDsFilter == null || technologyPollingIDsFilter.Contains(tp.TechnologyPollingID, StringComparer.Ordinal)
			select tp).ToLookup((ITechnologyPolling k) => k.TechnologyID);
			IEnumerable<ITechnology> enumerable = from t in TechnologyManager.Instance.TechnologyFactory.Items()
			where targetEntitiesFilter == null || targetEntitiesFilter.Contains(t.TargetEntity, StringComparer.Ordinal)
			select t;
			Func<TechnologyPollingAssignment, bool> <>9__3;
			foreach (ITechnology technology in enumerable)
			{
				foreach (ITechnologyPolling technologyPolling in technologyPollingsByTechnology[technology.TechnologyID])
				{
					IEnumerable<TechnologyPollingAssignment> enumerable2 = (netObjectIDsFilter == null) ? technologyPolling.GetAssignments() : technologyPolling.GetAssignments(netObjectIDsFilter);
					if (enabledFilterValue != null)
					{
						IEnumerable<TechnologyPollingAssignment> source = enumerable2;
						Func<TechnologyPollingAssignment, bool> predicate;
						if ((predicate = <>9__3) == null)
						{
							predicate = (<>9__3 = ((TechnologyPollingAssignment a) => a.Enabled == enabledFilterValue.Value));
						}
						enumerable2 = source.Where(predicate);
					}
					foreach (TechnologyPollingAssignment technologyPollingAssignment in enumerable2)
					{
						yield return technologyPollingAssignment;
					}
					IEnumerator<TechnologyPollingAssignment> enumerator3 = null;
				}
				IEnumerator<ITechnologyPolling> enumerator2 = null;
			}
			IEnumerator<ITechnology> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x040000F4 RID: 244
		private static readonly Log log = new Log();

		// Token: 0x040000F5 RID: 245
		private TechnologyPollingIndicator changesIndicator = new TechnologyPollingIndicator();

		// Token: 0x040000F6 RID: 246
		internal List<ITechnologyPollingProvider> providers;
	}
}
