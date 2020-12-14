using System;
using System.Collections.Generic;
using System.Linq;
using SolarWinds.InformationService.Contract2;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Auditing;
using SolarWinds.Orion.Core.Common.Indications;
using SolarWinds.Orion.Core.Common.InformationService;
using SolarWinds.Orion.Core.Models.Interfaces;
using SolarWinds.Orion.Core.Models.Technology;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000040 RID: 64
	public class TechnologyPollingIndicator
	{
		// Token: 0x06000424 RID: 1060 RVA: 0x0001C1E0 File Offset: 0x0001A3E0
		public static Action AuditTechnologiesChanges(IEnumerable<IDiscoveredObject> discoveredObjects, int nodeId)
		{
			if (!BusinessLayerSettings.Instance.EnableTechnologyPollingAssignmentsChangesAuditing)
			{
				return delegate()
				{
				};
			}
			Dictionary<string, ITechnology> technologies = TechnologyManager.Instance.TechnologyFactory.Items().ToDictionary((ITechnology k) => k.TechnologyID, (ITechnology v) => v, StringComparer.Ordinal);
			Dictionary<string, string> dictionary = (from tp in TechnologyManager.Instance.TechnologyPollingFactory.Items()
			where technologies.ContainsKey(tp.TechnologyID)
			select tp).ToDictionary((ITechnologyPolling k) => k.TechnologyPollingID, (ITechnologyPolling v) => technologies[v.TechnologyID].TargetEntity, StringComparer.Ordinal);
			IEnumerable<IDiscoveredObjectWithTechnology> enumerable = discoveredObjects.OfType<IDiscoveredObjectWithTechnology>();
			List<TechnologyPollingAssignment> changedAssignments = new List<TechnologyPollingAssignment>();
			foreach (IDiscoveredObjectWithTechnology discoveredObjectWithTechnology in enumerable)
			{
				if (dictionary.ContainsKey(discoveredObjectWithTechnology.TechnologyPollingID) && "Orion.Nodes".Equals(dictionary[discoveredObjectWithTechnology.TechnologyPollingID], StringComparison.Ordinal))
				{
					TechnologyPollingAssignment technologyPollingAssignment = TechnologyManager.Instance.TechnologyPollingFactory.GetAssignments(discoveredObjectWithTechnology.TechnologyPollingID, new int[]
					{
						nodeId
					}).FirstOrDefault<TechnologyPollingAssignment>();
					bool flag = technologyPollingAssignment != null && technologyPollingAssignment.Enabled;
					bool isSelected = discoveredObjectWithTechnology.IsSelected;
					if (flag != isSelected)
					{
						changedAssignments.Add(new TechnologyPollingAssignment
						{
							TechnologyPollingID = discoveredObjectWithTechnology.TechnologyPollingID,
							NetObjectID = nodeId,
							Enabled = isSelected
						});
					}
				}
			}
			return delegate()
			{
				if (changedAssignments.Count == 0)
				{
					return;
				}
				Dictionary<string, ITechnologyPolling> dictionary2 = TechnologyManager.Instance.TechnologyPollingFactory.Items().ToDictionary((ITechnologyPolling k) => k.TechnologyPollingID, (ITechnologyPolling v) => v, StringComparer.Ordinal);
				TechnologyPollingIndicator technologyPollingIndicator = new TechnologyPollingIndicator();
				foreach (TechnologyPollingAssignment technologyPollingAssignment2 in changedAssignments)
				{
					technologyPollingIndicator.ReportTechnologyPollingAssignmentIndication(dictionary2[technologyPollingAssignment2.TechnologyPollingID], new int[]
					{
						technologyPollingAssignment2.NetObjectID
					}, technologyPollingAssignment2.Enabled);
				}
			};
		}

		// Token: 0x06000425 RID: 1061 RVA: 0x0001C3C4 File Offset: 0x0001A5C4
		public TechnologyPollingIndicator() : this(new InformationServiceProxyFactory(), IndicationPublisher.CreateV3())
		{
		}

		// Token: 0x06000426 RID: 1062 RVA: 0x0001C3D6 File Offset: 0x0001A5D6
		public TechnologyPollingIndicator(IInformationServiceProxyFactory swisFactory, IndicationPublisher indicationReporter)
		{
			if (swisFactory == null)
			{
				throw new ArgumentNullException("swisFactory");
			}
			if (indicationReporter == null)
			{
				throw new ArgumentNullException("indicationReporter");
			}
			this.swisFactory = swisFactory;
			this.indicationReporter = indicationReporter;
		}

		// Token: 0x06000427 RID: 1063 RVA: 0x0001C408 File Offset: 0x0001A608
		public void ReportTechnologyPollingAssignmentIndication(ITechnologyPolling technologyPolling, int[] netObjectsInstanceIDs, bool enabledStateChangedTo)
		{
			if (technologyPolling == null)
			{
				throw new ArgumentNullException("technologyPolling");
			}
			if (netObjectsInstanceIDs == null)
			{
				throw new ArgumentNullException("netObjectsInstanceIDs");
			}
			if (netObjectsInstanceIDs.Length == 0)
			{
				return;
			}
			ITechnology technology = TechnologyManager.Instance.TechnologyFactory.GetTechnology(technologyPolling.TechnologyID);
			string netObjectPrefix = NetObjectTypesDAL.GetNetObjectPrefix(this.swisFactory, technology.TargetEntity);
			string entityName = NetObjectTypesDAL.GetEntityName(this.swisFactory, technology.TargetEntity);
			Dictionary<int, string> netObjectsCaptions = NetObjectTypesDAL.GetNetObjectsCaptions(this.swisFactory, technology.TargetEntity, netObjectsInstanceIDs);
			foreach (int num in netObjectsInstanceIDs)
			{
				PropertyBag propertyBag = new PropertyBag();
				propertyBag.Add("InstanceType", "Orion.TechnologyPollingAssignments");
				propertyBag.Add("InstanceID", num.ToString());
				propertyBag.Add("TechnologyPollingID", technologyPolling.TechnologyPollingID);
				propertyBag.Add("Enabled", enabledStateChangedTo);
				propertyBag.Add("TargetEntity", technology.TargetEntity);
				propertyBag.Add("TechPollingDispName", technologyPolling.DisplayName);
				propertyBag.Add("TechnologyDispName", technology.DisplayName);
				PropertyBag propertyBag2 = propertyBag;
				string value;
				if (netObjectsCaptions.TryGetValue(num, out value))
				{
					propertyBag2.Add("NetObjectCaption", value);
				}
				if (netObjectPrefix != null)
				{
					propertyBag2.Add("NetObjectPrefix", netObjectPrefix);
					propertyBag2.Add(KnownKeys.NetObject, string.Format("{0}:{1}", netObjectPrefix, num));
				}
				if (entityName != null)
				{
					propertyBag2.Add("NetObjectName", entityName);
				}
				this.indicationReporter.ReportIndication(new TechnologyPollingAssignmentIndication(2, propertyBag2));
			}
		}

		// Token: 0x040000F7 RID: 247
		private readonly IInformationServiceProxyFactory swisFactory;

		// Token: 0x040000F8 RID: 248
		private readonly IndicationPublisher indicationReporter;
	}
}
