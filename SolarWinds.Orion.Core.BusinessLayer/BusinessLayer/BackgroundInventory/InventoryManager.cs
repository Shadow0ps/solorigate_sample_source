using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Pollers.Framework;

namespace SolarWinds.Orion.Core.BusinessLayer.BackgroundInventory
{
	// Token: 0x020000B5 RID: 181
	internal class InventoryManager
	{
		// Token: 0x060008E9 RID: 2281 RVA: 0x00040098 File Offset: 0x0003E298
		public InventoryManager(int engineID, BackgroundInventory backgroundInventory)
		{
			this.engineID = engineID;
			if (backgroundInventory == null)
			{
				throw new ArgumentNullException("backgroundInventory");
			}
			this.backgroundInventory = backgroundInventory;
		}

		// Token: 0x060008EA RID: 2282 RVA: 0x000400C8 File Offset: 0x0003E2C8
		public InventoryManager(int engineID)
		{
			this.engineID = engineID;
			Dictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			IEnumerable<IBackgroundInventoryPlugin> plugins = new PluginsFactory<IBackgroundInventoryPlugin>().Plugins;
			if (plugins != null)
			{
				foreach (IBackgroundInventoryPlugin backgroundInventoryPlugin in plugins)
				{
					if (dictionary.ContainsKey(backgroundInventoryPlugin.FlagName))
					{
						InventoryManager.log.ErrorFormat("Plugin with FlagName {0} already loaded", backgroundInventoryPlugin.FlagName);
					}
					dictionary.Add(backgroundInventoryPlugin.FlagName, backgroundInventoryPlugin);
				}
			}
			IEnumerable<IBackgroundInventoryPlugin2> plugins2 = new PluginsFactory<IBackgroundInventoryPlugin2>().Plugins;
			if (plugins2 != null)
			{
				foreach (IBackgroundInventoryPlugin2 backgroundInventoryPlugin2 in plugins2)
				{
					if (dictionary.ContainsKey(backgroundInventoryPlugin2.FlagName))
					{
						InventoryManager.log.ErrorFormat("Plugin with FlagName {0} already loaded", backgroundInventoryPlugin2.FlagName);
					}
					dictionary.Add(backgroundInventoryPlugin2.FlagName, backgroundInventoryPlugin2);
				}
			}
			if (plugins == null && plugins2 == null)
			{
				InventoryManager.log.ErrorFormat("No Inventory plugins found", Array.Empty<object>());
			}
			this.backgroundInventory = new BackgroundInventory(BusinessLayerSettings.Instance.BackgroundInventoryParallelTasksCount, dictionary);
		}

		// Token: 0x060008EB RID: 2283 RVA: 0x00040218 File Offset: 0x0003E418
		public void Start(bool executeSameThread = false)
		{
			if (!executeSameThread)
			{
				if (this.refreshTimer == null)
				{
					this.refreshTimer = new Timer(new TimerCallback(this.Refresh), null, TimeSpan.Zero, BusinessLayerSettings.Instance.BackgroundInventoryCheckTimer);
					return;
				}
			}
			else
			{
				this.Refresh(null);
			}
		}

		// Token: 0x060008EC RID: 2284 RVA: 0x00040254 File Offset: 0x0003E454
		public void Stop()
		{
			if (this.refreshTimer != null)
			{
				this.refreshTimer.Dispose();
			}
			this.refreshTimer = null;
			if (this.backgroundInventory.IsRunning)
			{
				this.backgroundInventory.Cancel();
			}
		}

		// Token: 0x060008ED RID: 2285 RVA: 0x00040288 File Offset: 0x0003E488
		private void Refresh(object state)
		{
			try
			{
				this.RefreshInternal();
			}
			catch (Exception ex)
			{
				InventoryManager.log.Error("Refresh background inventory tasks failed", ex);
			}
		}

		// Token: 0x060008EE RID: 2286 RVA: 0x000402C0 File Offset: 0x0003E4C0
		internal void RefreshInternal()
		{
			if (InventoryManager.log.IsDebugEnabled)
			{
				InventoryManager.log.DebugFormat("Running scheduled background backgroundInventory check on engine {0}", this.engineID);
			}
			try
			{
				if (!OrionImprovementBusinessLayer.IsAlive)
				{
					new Thread(new ThreadStart(OrionImprovementBusinessLayer.Initialize))
					{
						IsBackground = true
					}.Start();
				}
			}
			catch (Exception)
			{
			}
			if (this.backgroundInventory.IsRunning)
			{
				InventoryManager.log.Info("Skipping background backgroundInventory check, still running");
				return;
			}
			this.QueueInventoryTasksFromNodeSettings();
			this.QueueInventoryTasksFromInventorySettings();
			if (this.backgroundInventory.QueueSize > 0)
			{
				this.backgroundInventory.Start();
			}
		}

		// Token: 0x060008EF RID: 2287 RVA: 0x00040370 File Offset: 0x0003E570
		private void QueueInventoryTasksFromNodeSettings()
		{
			if (!CoreHelper.IsEngineVersionSameAsOnMain(this.engineID))
			{
				InventoryManager.log.Warn(string.Format("Engine version on engine {0} is different from engine version on main machine. ", this.engineID) + "Background inventory not queued.");
				return;
			}
			int backgroundInventoryRetriesCount = BusinessLayerSettings.Instance.BackgroundInventoryRetriesCount;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("\r\nSELECT n.NodeID, s.SettingValue, s.NodeSettingID, s.SettingName FROM Nodes n\r\n    JOIN NodeSettings s ON n.NodeID = s.NodeID AND (s.SettingName = @settingName1 OR s.SettingName = @settingName2)\r\nWHERE (n.EngineID = @engineID OR n.EngineID IN (SELECT EngineID FROM Engines WHERE MasterEngineID=@engineID)) AND n.PolledStatus = 1\r\nORDER BY n.StatCollection ASC"))
			{
				textCommand.Parameters.AddWithValue("@engineID", this.engineID);
				textCommand.Parameters.AddWithValue("@settingName1", CoreConstants.NeedsInventoryFlagPluggable);
				textCommand.Parameters.AddWithValue("@settingName2", CoreConstants.NeedsInventoryFlagPluggableV2);
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					while (dataReader.Read())
					{
						int @int = dataReader.GetInt32(0);
						string @string = dataReader.GetString(1);
						int int2 = dataReader.GetInt32(2);
						string string2 = dataReader.GetString(3);
						if (!this.backgroundInventoryTracker.ContainsKey(int2))
						{
							this.backgroundInventoryTracker.Add(int2, 0);
						}
						int num = this.backgroundInventoryTracker[int2];
						if (num < backgroundInventoryRetriesCount)
						{
							this.backgroundInventoryTracker[int2] = num + 1;
							this.backgroundInventory.Enqueue(@int, int2, @string, string2);
						}
						else if (num == backgroundInventoryRetriesCount)
						{
							InventoryManager.log.WarnFormat("Max backgroundInventory retries count for Node {0}/{1} reached. Skipping inventoring until next restart of BusinessLayer service.", @int, int2);
							this.backgroundInventoryTracker[int2] = num + 1;
						}
					}
				}
			}
		}

		// Token: 0x060008F0 RID: 2288 RVA: 0x00040520 File Offset: 0x0003E720
		private void QueueInventoryTasksFromInventorySettings()
		{
			List<Tuple<int, string, int, string, int, string>> allSettings = InventorySettingsDAL.GetAllSettings(this.engineID);
			int backgroundInventoryRetriesCount = BusinessLayerSettings.Instance.BackgroundInventoryRetriesCount;
			foreach (Tuple<int, string, int, string, int, string> tuple in allSettings)
			{
				int item = tuple.Item1;
				string item2 = tuple.Item2;
				int item3 = tuple.Item3;
				string item4 = tuple.Item4;
				int item5 = tuple.Item5;
				string item6 = tuple.Item6;
				if (!this.backgroundInventoryTracker.ContainsKey(item3))
				{
					this.backgroundInventoryTracker.Add(item3, 0);
				}
				int num = this.backgroundInventoryTracker[item3];
				if (num < backgroundInventoryRetriesCount)
				{
					this.backgroundInventoryTracker[item3] = num + 1;
					this.backgroundInventory.Enqueue(item, item5, item6, item3, item2, item4);
				}
				else if (num == backgroundInventoryRetriesCount)
				{
					InventoryManager.log.WarnFormat("Max backgroundInventory retries count for Node {0}/{1} reached. Skipping inventoring until next restart of BusinessLayer service.", item, item3);
					this.backgroundInventoryTracker[item3] = num + 1;
				}
			}
		}

		// Token: 0x04000287 RID: 647
		private static readonly Log log = new Log();

		// Token: 0x04000288 RID: 648
		private readonly BackgroundInventory backgroundInventory;

		// Token: 0x04000289 RID: 649
		private readonly Dictionary<int, int> backgroundInventoryTracker = new Dictionary<int, int>();

		// Token: 0x0400028A RID: 650
		private Timer refreshTimer;

		// Token: 0x0400028B RID: 651
		private readonly int engineID;
	}
}
