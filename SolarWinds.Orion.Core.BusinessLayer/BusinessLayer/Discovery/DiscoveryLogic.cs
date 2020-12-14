using System;
using System.Collections.Generic;
using System.Linq;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Discovery;
using SolarWinds.Orion.Core.Discovery.DataAccess;
using SolarWinds.Orion.Core.Models.DiscoveredObjects;
using SolarWinds.Orion.Core.Models.Discovery;
using SolarWinds.Orion.Core.Models.Interfaces;
using SolarWinds.Orion.Discovery.Contract.DiscoveryPlugin;
using SolarWinds.Orion.Discovery.Framework.Pluggability;

namespace SolarWinds.Orion.Core.BusinessLayer.Discovery
{
	// Token: 0x02000078 RID: 120
	internal class DiscoveryLogic
	{
		// Token: 0x170000ED RID: 237
		// (get) Token: 0x0600061B RID: 1563 RVA: 0x00024B78 File Offset: 0x00022D78
		// (set) Token: 0x0600061C RID: 1564 RVA: 0x00024B9D File Offset: 0x00022D9D
		internal IJobFactory JobFactory
		{
			get
			{
				IJobFactory result;
				if ((result = this._jobFactory) == null)
				{
					result = (this._jobFactory = new OrionDiscoveryJobFactory());
				}
				return result;
			}
			set
			{
				this._jobFactory = value;
			}
		}

		// Token: 0x0600061D RID: 1565 RVA: 0x00024BA8 File Offset: 0x00022DA8
		public DiscoveryResultBase FilterIgnoredItems(DiscoveryResultBase discoveryResult)
		{
			DiscoveryResultBase discoveryResultBase = this.FilterItems(discoveryResult, (IDiscoveryPlugin plugin, DiscoveryResultBase result) => plugin.FilterOutItemsFromIgnoreList(result));
			DiscoveryConfiguration config = DiscoveryDatabase.GetDiscoveryConfiguration(discoveryResult.GetPluginResultOfType<CoreDiscoveryPluginResult>().ProfileId ?? 0);
			if (config != null && config.IsAutoImport)
			{
				discoveryResultBase = this.FilterItems(discoveryResultBase, delegate(IDiscoveryPlugin plugin, DiscoveryResultBase result)
				{
					if (!(plugin is ISupportAutoImport))
					{
						return null;
					}
					return ((ISupportAutoImport)plugin).FilterAutoImportItems(result, config);
				});
			}
			return discoveryResultBase;
		}

		// Token: 0x0600061E RID: 1566 RVA: 0x00024C38 File Offset: 0x00022E38
		private DiscoveryResultBase FilterItems(DiscoveryResultBase discoveryResult, Func<IDiscoveryPlugin, DiscoveryResultBase, DiscoveryPluginResultBase> filterFunction)
		{
			foreach (IDiscoveryPlugin discoveryPlugin in DiscoveryHelper.GetOrderedDiscoveryPlugins())
			{
				DiscoveryPluginResultBase discoveryPluginResultBase = filterFunction(discoveryPlugin, discoveryResult);
				if (discoveryPluginResultBase != null)
				{
					discoveryPluginResultBase.PluginTypeName = discoveryPlugin.GetType().FullName;
					Type returnedType = discoveryPluginResultBase.GetType();
					List<DiscoveryPluginResultBase> list = (from item in discoveryResult.PluginResults
					where item.GetType() != returnedType
					select item).ToList<DiscoveryPluginResultBase>();
					discoveryResult.PluginResults.Clear();
					discoveryResult.PluginResults.Add(discoveryPluginResultBase);
					foreach (DiscoveryPluginResultBase discoveryPluginResultBase2 in list)
					{
						discoveryResult.PluginResults.Add(discoveryPluginResultBase2);
					}
				}
			}
			return discoveryResult;
		}

		// Token: 0x0600061F RID: 1567 RVA: 0x00024D30 File Offset: 0x00022F30
		public void DeleteOrionDiscoveryProfile(int profileID)
		{
			DiscoveryLogic.log.DebugFormat("Deleting profile {0}", profileID);
			DiscoveryProfileEntry profileByID = DiscoveryProfileEntry.GetProfileByID(profileID);
			if (profileByID == null)
			{
				throw new ArgumentNullException(string.Format("Profile {0} not found.", profileID));
			}
			this.DeleteDiscoveryProfileInternal(profileByID);
		}

		// Token: 0x06000620 RID: 1568 RVA: 0x00024D7C File Offset: 0x00022F7C
		public void DeleteHiddenOrionDiscoveryProfilesByName(string profileName)
		{
			DiscoveryLogic.log.DebugFormat("Deleting hidden profile '{0}'", profileName);
			foreach (DiscoveryProfileEntry profile in from x in DiscoveryProfileEntry.GetProfilesByName(profileName)
			where x.IsHidden
			select x)
			{
				this.DeleteDiscoveryProfileInternal(profile);
			}
		}

		// Token: 0x06000621 RID: 1569 RVA: 0x00024E00 File Offset: 0x00023000
		private void DeleteDiscoveryProfileInternal(DiscoveryProfileEntry profile)
		{
			if (profile.JobID != Guid.Empty)
			{
				DiscoveryLogic.log.DebugFormat("Deleting job for profile {0}", profile.ProfileID);
				try
				{
					if (this.JobFactory.DeleteJob(profile.JobID))
					{
						DiscoveryLogic.log.ErrorFormat("Error when deleting job {0}.", profile.ProfileID);
					}
					DiscoveryLogic.log.DebugFormat("Job for profile {0} deleted.", profile.ProfileID);
				}
				catch (Exception ex)
				{
					DiscoveryLogic.log.ErrorFormat("Exception when deleting job {0}. Exception: {1}", profile.ProfileID, ex);
				}
			}
			DiscoveryLogic.log.DebugFormat("Removing profile {0} from database.", profile.ProfileID);
			DiscoveryDatabase.DeleteProfile(profile);
			DiscoveryLogic.log.DebugFormat("Profile {0} removed from database.", profile.ProfileID);
		}

		// Token: 0x06000622 RID: 1570 RVA: 0x00024EF0 File Offset: 0x000230F0
		public void ImportDiscoveryResultForProfile(int profileID, bool deleteProfileAfterImport, DiscoveryImportManager.CallbackDiscoveryImportFinished callback = null, bool checkLicenseLimits = false, Guid? importID = null)
		{
			IList<IDiscoveryPlugin> orderedDiscoveryPlugins = DiscoveryHelper.GetOrderedDiscoveryPlugins();
			SortedDictionary<int, List<IDiscoveryPlugin>> orderedPlugins = DiscoveryPluginHelper.GetOrderedPlugins(orderedDiscoveryPlugins, DiscoveryHelper.GetDiscoveryPluginInfos());
			DiscoveryResultBase discoveryResult = DiscoveryResultManager.GetDiscoveryResult(profileID, orderedDiscoveryPlugins);
			DiscoveryResultBase result2 = this.FilterIgnoredItems(discoveryResult);
			Guid importId = Guid.NewGuid();
			if (importID != null)
			{
				importId = importID.Value;
			}
			DiscoveryImportManager.CallbackDiscoveryImportFinished callbackAfterImport = callback;
			if (deleteProfileAfterImport)
			{
				callbackAfterImport = delegate(DiscoveryResultBase result, Guid id, StartImportStatus status)
				{
					this.DeleteOrionDiscoveryProfile(result.ProfileID);
					if (callback != null)
					{
						callback(result, id, status);
					}
				};
			}
			DiscoveryImportManager.StartImport(importId, result2, orderedPlugins, checkLicenseLimits, callbackAfterImport);
		}

		// Token: 0x06000623 RID: 1571 RVA: 0x00024F78 File Offset: 0x00023178
		public IEnumerable<DiscoveryPluginConfigurationBase> DeserializePluginConfigurationItems(List<string> discoveryPluginConfigurationBaseItems)
		{
			List<DiscoveryPluginConfigurationBase> list = new List<DiscoveryPluginConfigurationBase>();
			foreach (string text in discoveryPluginConfigurationBaseItems)
			{
				DiscoveryPluginItems<DiscoveryPluginConfigurationBase> collection = new DiscoveryPluginItems<DiscoveryPluginConfigurationBase>(text);
				list.AddRange(collection);
			}
			return list;
		}

		// Token: 0x06000624 RID: 1572 RVA: 0x00024FD4 File Offset: 0x000231D4
		public void ImportDiscoveryResultsForConfiguration(DiscoveryImportConfiguration importCfg, Guid importID)
		{
			DiscoveryLogic.log.DebugFormat("Loading discovery results.", Array.Empty<object>());
			if (DiscoveryProfileEntry.GetProfileByID(importCfg.ProfileID) == null)
			{
				throw new Exception(string.Format("Requested profile {0} not found.", importCfg.ProfileID));
			}
			DiscoveryImportManager.UpdateProgress(importID, "ImportDiscoveryResults Started", "Loading Plugins", false);
			IList<IDiscoveryPlugin> orderedDiscoveryPlugins = DiscoveryHelper.GetOrderedDiscoveryPlugins();
			SortedDictionary<int, List<IDiscoveryPlugin>> orderedPlugins = DiscoveryPluginHelper.GetOrderedPlugins(orderedDiscoveryPlugins, DiscoveryHelper.GetDiscoveryPluginInfos());
			DiscoveryResultBase discoveryResult = DiscoveryResultManager.GetDiscoveryResult(importCfg.ProfileID, orderedDiscoveryPlugins);
			DiscoveryResultBase discoveryResultBase;
			if (importCfg.NodeIDs.Count > 0)
			{
				DiscoveryLogic.log.DebugFormat("Nodes to be imported : {0}", importCfg.NodeIDs.Count);
				foreach (DiscoveredNode discoveredNode in discoveryResult.GetPluginResultOfType<CoreDiscoveryPluginResult>().DiscoveredNodes)
				{
					if (importCfg.NodeIDs.Contains(discoveredNode.NodeID))
					{
						discoveredNode.IsSelected = true;
					}
					else
					{
						discoveredNode.IsSelected = false;
					}
				}
				foreach (DiscoveryPluginResultBase discoveryPluginResultBase in this.Linearize(discoveryResult.PluginResults))
				{
					IDiscoveryPluginResultContextFiltering discoveryPluginResultContextFiltering = discoveryPluginResultBase as IDiscoveryPluginResultContextFiltering;
					DiscoveryPluginResultBase discoveryPluginResultBase2;
					if (discoveryPluginResultContextFiltering != null)
					{
						discoveryPluginResultBase2 = discoveryPluginResultContextFiltering.GetFilteredPluginResultFromContext(discoveryResult);
					}
					else
					{
						discoveryPluginResultBase2 = discoveryPluginResultBase.GetFilteredPluginResult();
					}
					discoveryResult.PluginResults.Remove(discoveryPluginResultBase);
					discoveryResult.PluginResults.Add(discoveryPluginResultBase2);
					DiscoveryLogic.log.DebugFormat("Applying filters for pluggin - {0}.", discoveryPluginResultBase.PluginTypeName);
				}
				discoveryResultBase = this.FilterIgnoredItems(discoveryResult);
			}
			else
			{
				discoveryResultBase = discoveryResult;
			}
			discoveryResultBase.ProfileID = importCfg.ProfileID;
			DiscoveryLogic.log.DebugFormat("Importing started.", Array.Empty<object>());
			if (importCfg.DeleteProfileAfterImport)
			{
				DiscoveryImportManager.StartImport(importID, discoveryResultBase, orderedPlugins, false, delegate(DiscoveryResultBase result, Guid importId, StartImportStatus importStatus)
				{
					this.DeleteOrionDiscoveryProfile(result.ProfileID);
				});
				return;
			}
			DiscoveryImportManager.StartImport(importID, discoveryResultBase, orderedPlugins);
		}

		// Token: 0x06000625 RID: 1573 RVA: 0x000251D8 File Offset: 0x000233D8
		private List<DiscoveryPluginResultBase> Linearize(IEnumerable<DiscoveryPluginResultBase> input)
		{
			List<DiscoveryPluginResultBase> list = Linearizer.Linearize<DiscoveryPluginResultBase>((from item in input
			select Linearizer.CreateInputItem<DiscoveryPluginResultBase>(item, item.GetPrerequisites(input))).ToArray<Linearizer.Input<DiscoveryPluginResultBase>>(), true, true);
			IEnumerable<DiscoveryPluginResultBase> collection = from item in list
			where item is CoreDiscoveryPluginResult && item.PluginTypeName == "SolarWinds.Orion.Core.DiscoveryPlugin.CoreDiscoveryPlugin"
			select item;
			List<DiscoveryPluginResultBase> list2 = new List<DiscoveryPluginResultBase>();
			list2.AddRange(collection);
			for (int i = 0; i < list.Count; i++)
			{
				DiscoveryPluginResultBase discoveryPluginResultBase = list[i];
				if (!(discoveryPluginResultBase is CoreDiscoveryPluginResult) || !(discoveryPluginResultBase.PluginTypeName == "SolarWinds.Orion.Core.DiscoveryPlugin.CoreDiscoveryPlugin"))
				{
					list2.Add(discoveryPluginResultBase);
				}
			}
			return list2;
		}

		// Token: 0x040001EE RID: 494
		private static Log log = new Log();

		// Token: 0x040001EF RID: 495
		private IJobFactory _jobFactory;
	}
}
