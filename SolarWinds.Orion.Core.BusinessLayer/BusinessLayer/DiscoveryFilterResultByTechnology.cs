using System;
using System.Collections.Generic;
using System.Linq;
using SolarWinds.Orion.Core.Models.DiscoveredObjects;
using SolarWinds.Orion.Core.Models.Discovery;
using SolarWinds.Orion.Core.Models.Interfaces;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000018 RID: 24
	public class DiscoveryFilterResultByTechnology
	{
		// Token: 0x060002A2 RID: 674 RVA: 0x00010271 File Offset: 0x0000E471
		public static IEnumerable<IDiscoveredObjectGroup> GetDiscoveryGroups(TechnologyManager mgr)
		{
			return DiscoveryFilterResultByTechnology.GetDiscoveryGroupsInternal(mgr);
		}

		// Token: 0x060002A3 RID: 675 RVA: 0x00010279 File Offset: 0x0000E479
		private static IEnumerable<TechnologyDiscoveryGroup> GetDiscoveryGroupsInternal(TechnologyManager mgr)
		{
			if (mgr == null)
			{
				throw new ArgumentNullException("mgr");
			}
			foreach (ITechnologyDiscovery technologyDiscovery in mgr.TechnologyFactory.Items().OfType<ITechnologyDiscovery>())
			{
				string[] array = (from n in mgr.TechnologyPollingFactory.ItemsByTechnology(technologyDiscovery.TechnologyID)
				orderby n.Priority descending
				select n.TechnologyPollingID).ToArray<string>();
				TechnologyDiscoveryGroup technologyDiscoveryGroup = new TechnologyDiscoveryGroup(technologyDiscovery.ParentType, technologyDiscovery.DisplayName, technologyDiscovery.Icon, technologyDiscovery.TreeOrder, array);
				IDiscoveredObjectGroupWithSelectionMode discoveredObjectGroupWithSelectionMode = technologyDiscovery as IDiscoveredObjectGroupWithSelectionMode;
				if (discoveredObjectGroupWithSelectionMode != null)
				{
					technologyDiscoveryGroup.SelectionMode = discoveredObjectGroupWithSelectionMode.SelectionMode;
					technologyDiscoveryGroup.SelectionDisabled = discoveredObjectGroupWithSelectionMode.SelectionDisabled;
				}
				yield return technologyDiscoveryGroup;
			}
			IEnumerator<ITechnologyDiscovery> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x060002A4 RID: 676 RVA: 0x0001028C File Offset: 0x0000E48C
		private static DiscoveryResultBase FilterByPriority(DiscoveryResultBase result, TechnologyManager mgr, bool onlyMandatory)
		{
			if (result == null)
			{
				throw new ArgumentNullException("result");
			}
			if (mgr == null)
			{
				throw new ArgumentNullException("mgr");
			}
			ILookup<string, ITechnologyPolling> technologyPollingsById = mgr.TechnologyPollingFactory.Items().ToLookup((ITechnologyPolling tp) => tp.TechnologyPollingID, StringComparer.Ordinal);
			List<IDiscoveredObject> list = new List<IDiscoveredObject>();
			foreach (DiscoveryPluginResultBase discoveryPluginResultBase in result.PluginResults)
			{
				IEnumerable<IDiscoveredObject> discoveredObjects = discoveryPluginResultBase.GetDiscoveredObjects();
				list.AddRange(discoveredObjects);
			}
			List<IDiscoveredObjectWithTechnology> source = list.OfType<IDiscoveredObjectWithTechnology>().ToList<IDiscoveredObjectWithTechnology>();
			foreach (TechnologyDiscoveryGroup group2 in DiscoveryFilterResultByTechnology.GetDiscoveryGroupsInternal(mgr))
			{
				TechnologyDiscoveryGroup group = group2;
				if (!onlyMandatory || group.SelectionDisabled)
				{
					IEnumerable<IDiscoveredObjectWithTechnology> enumerable = (from n in source
					where @group.IsMyGroupedObjectType(n)
					select n).ToList<IDiscoveredObjectWithTechnology>();
					List<List<IDiscoveredObjectWithTechnology>> list2 = new List<List<IDiscoveredObjectWithTechnology>>();
					foreach (IDiscoveredObject discoveredObject in list)
					{
						if (group.IsChildOf(discoveredObject))
						{
							List<IDiscoveredObjectWithTechnology> list3 = new List<IDiscoveredObjectWithTechnology>();
							foreach (IDiscoveredObjectWithTechnology discoveredObjectWithTechnology in enumerable)
							{
								if (discoveredObjectWithTechnology.IsChildOf(discoveredObject))
								{
									list3.Add(discoveredObjectWithTechnology);
								}
							}
							list2.Add(list3);
						}
					}
					foreach (List<IDiscoveredObjectWithTechnology> list4 in list2)
					{
						if (onlyMandatory)
						{
							if (list4.Any((IDiscoveredObjectWithTechnology to) => to.IsSelected))
							{
								continue;
							}
						}
						else
						{
							list4.ForEach(delegate(IDiscoveredObjectWithTechnology to)
							{
								to.IsSelected = false;
							});
						}
						DiscoveryFilterResultByTechnology.SelectObjectWithHigherPriority(list4, technologyPollingsById);
					}
				}
			}
			return result;
		}

		// Token: 0x060002A5 RID: 677 RVA: 0x00010540 File Offset: 0x0000E740
		public static DiscoveryResultBase FilterByPriority(DiscoveryResultBase result, TechnologyManager mgr)
		{
			return DiscoveryFilterResultByTechnology.FilterByPriority(result, mgr, false);
		}

		// Token: 0x060002A6 RID: 678 RVA: 0x0001054A File Offset: 0x0000E74A
		public static DiscoveryResultBase FilterMandatoryByPriority(DiscoveryResultBase result, TechnologyManager mgr)
		{
			return DiscoveryFilterResultByTechnology.FilterByPriority(result, mgr, true);
		}

		// Token: 0x060002A7 RID: 679 RVA: 0x00010554 File Offset: 0x0000E754
		private static void SelectObjectWithHigherPriority(IEnumerable<IDiscoveredObjectWithTechnology> technologyObjects, ILookup<string, ITechnologyPolling> technologyPollingsById)
		{
			var <>f__AnonymousType = (from n in technologyObjects
			select new
			{
				Object = n,
				SelectionPriority = (from tp in technologyPollingsById[n.TechnologyPollingID]
				select tp.Priority).DefaultIfEmpty(0).First<int>()
			} into n
			orderby n.SelectionPriority descending
			select n).FirstOrDefault();
			if (<>f__AnonymousType != null)
			{
				<>f__AnonymousType.Object.IsSelected = true;
			}
		}
	}
}
