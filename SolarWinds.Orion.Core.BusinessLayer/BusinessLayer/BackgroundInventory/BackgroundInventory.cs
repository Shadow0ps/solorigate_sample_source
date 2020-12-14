using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Models.Enums;
using SolarWinds.Orion.Core.SharedCredentials;
using SolarWinds.Orion.Pollers.Framework;
using SolarWinds.Orion.Pollers.Framework.SNMP;
using SolarWinds.Orion.Pollers.Framework.WMI;

namespace SolarWinds.Orion.Core.BusinessLayer.BackgroundInventory
{
	// Token: 0x020000B4 RID: 180
	public class BackgroundInventory : IDisposable
	{
		// Token: 0x17000120 RID: 288
		// (get) Token: 0x060008D5 RID: 2261 RVA: 0x0003F7BD File Offset: 0x0003D9BD
		public bool IsRunning
		{
			get
			{
				return this.scheduler.IsRunning;
			}
		}

		// Token: 0x17000121 RID: 289
		// (get) Token: 0x060008D6 RID: 2262 RVA: 0x0003F7CA File Offset: 0x0003D9CA
		public int QueueSize
		{
			get
			{
				return this.scheduler.QueueSize;
			}
		}

		// Token: 0x060008D7 RID: 2263 RVA: 0x0003F7D7 File Offset: 0x0003D9D7
		public virtual bool IsScheduledTaskCanceled()
		{
			return this.scheduler.IsTaskCanceled;
		}

		// Token: 0x060008D8 RID: 2264 RVA: 0x0003F7E4 File Offset: 0x0003D9E4
		public BackgroundInventory(int parallelTasksCount, Dictionary<string, object> plugins)
		{
			if (plugins == null)
			{
				throw new ArgumentNullException("plugins");
			}
			this.scheduler = new QueuedTaskScheduler<BackgroundInventory.InventoryTask>(new QueuedTaskScheduler<BackgroundInventory.InventoryTask>.TaskProcessingRoutine(this.DoInventory), parallelTasksCount);
			this.scheduler.TaskProcessingFinished += this.scheduler_TaskProcessingFinished;
			this.snmpGlobals = new SnmpGlobalSettings();
			this.snmpGlobals.MaxReplies = SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-SNMP MaxReps", 5);
			this.snmpGlobals.RequestTimeout = SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-SNMP Timeout", 2500);
			this.snmpGlobals.RequestRetries = SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-SNMP Retries", 2);
			this.snmpGlobals.HsrpEnabled = SettingsDAL.GetCurrent<bool>("SWNetPerfMon-Settings-SNMP HSRPEnabled", true);
			this.wmiGlobals = new WmiGlobalSettings();
			this.wmiGlobals.UserImpersonationLevel = SettingsDAL.GetCurrent<ImpersonationLevel>("SWNetPerfMon-Settings-Wmi UserImpersonationLevel", ImpersonationLevel.Default);
			this.wmiGlobals.ConnectionRationMode = SettingsDAL.GetCurrent<WmiConnectionRationMode>("SWNetPerfMon-Settings-Wmi ConnectionRationMode", 1);
			this.wmiGlobals.MaxRationedConnections = SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-Wmi MaxRationedConnections", 0);
			this.wmiGlobals.KillProcessExcessiveError = SettingsDAL.GetCurrent<bool>("SWNetPerfMon-Settings-Wmi KillProcessExcessiveError", true);
			this.wmiGlobals.ExcessiveErrorThreshhold = SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-Wmi ExcessiveErrorThreshhold", 50);
			this.wmiGlobals.WmiRetries = SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-WMI Retries", 0);
			this.wmiGlobals.WmiRetryInterval = SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-WMI Retry Interval", 0);
			this.wmiGlobals.WmiAutoCorrectRDNSInconsistency = Convert.ToBoolean(SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-WMI Auto Correct Reverse DNS", 0));
			this.wmiGlobals.WmiDefaultRootNamespaceOverrideIndex = SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-WMI Default Root Namespace Override Index", 0);
			this.inventories = plugins;
		}

		// Token: 0x060008D9 RID: 2265 RVA: 0x0003F984 File Offset: 0x0003DB84
		private void scheduler_TaskProcessingFinished(object sender, EventArgs e)
		{
			BackgroundInventory.log.Info("Background Inventorying Finished");
		}

		// Token: 0x060008DA RID: 2266 RVA: 0x0003F995 File Offset: 0x0003DB95
		public virtual void Enqueue(int nodeID, int objectID, string objectType, int nodeSettingID, string settings, string inventorySettingName)
		{
			this.scheduler.EnqueueTask(new BackgroundInventory.InventoryTask(nodeID, objectID, objectType, nodeSettingID, settings, inventorySettingName, BackgroundInventory.InventoryTask.InventoryInputSource.InventorySettings));
		}

		// Token: 0x060008DB RID: 2267 RVA: 0x0003F9B1 File Offset: 0x0003DBB1
		public virtual void Enqueue(int nodeID, int nodeSettingID, string settings, string inventorySettingName)
		{
			this.scheduler.EnqueueTask(new BackgroundInventory.InventoryTask(nodeID, -1, string.Empty, nodeSettingID, settings, inventorySettingName, BackgroundInventory.InventoryTask.InventoryInputSource.NodeSettings));
		}

		// Token: 0x060008DC RID: 2268 RVA: 0x0003F9CF File Offset: 0x0003DBCF
		public void Start()
		{
			this.scheduler.Start();
		}

		// Token: 0x060008DD RID: 2269 RVA: 0x0003F9DC File Offset: 0x0003DBDC
		public void Cancel()
		{
			this.scheduler.Cancel();
		}

		// Token: 0x060008DE RID: 2270 RVA: 0x0000CB18 File Offset: 0x0000AD18
		public virtual Node GetNode(int nodeId)
		{
			return NodeBLDAL.GetNode(nodeId);
		}

		// Token: 0x060008DF RID: 2271 RVA: 0x0003F9E9 File Offset: 0x0003DBE9
		public virtual Credential GetCredentialsForNode(Node node)
		{
			return CredentialHelper.ParseCredentialsFromNode(node);
		}

		// Token: 0x060008E0 RID: 2272 RVA: 0x0003F9F4 File Offset: 0x0003DBF4
		public void DoInventory(BackgroundInventory.InventoryTask task)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			Node node = this.GetNode(task.NodeID);
			if (node == null || node.PolledStatus != 1)
			{
				BackgroundInventory.log.InfoFormat("Skipping inventorying of Node {0}, status is not UP.", task.NodeID);
				return;
			}
			Credential credentialsForNode = this.GetCredentialsForNode(node);
			GlobalSettingsBase globals = this.snmpGlobals;
			if (node.NodeSubType == NodeSubType.WMI)
			{
				globals = this.wmiGlobals;
			}
			if (BackgroundInventory.log.IsInfoEnabled)
			{
				BackgroundInventory.log.InfoFormat("Starting inventorying of Node {0}, NeedsInventory = '{1}'", task.NodeID, task.Settings);
			}
			string[] array = task.Settings.Split(new char[]
			{
				':'
			});
			List<string> failedTasks = new List<string>();
			List<string> completedTasks = new List<string>();
			Func<string, bool> <>9__0;
			foreach (string text in array)
			{
				BackgroundInventory.log.InfoFormat("Attempting to inventory with plugin '{0}' on Node {1}", text, task.NodeID);
				if (!this.inventories.ContainsKey(text))
				{
					failedTasks.Add(text);
					if (BackgroundInventory.log.IsErrorEnabled)
					{
						BackgroundInventory.log.ErrorFormat("Unable to inventory '{0}' on Node {1}", text, task.NodeID);
					}
				}
				else
				{
					if (this.IsScheduledTaskCanceled())
					{
						if (BackgroundInventory.log.IsInfoEnabled)
						{
							BackgroundInventory.log.InfoFormat("Inventorying of Node {0} was canceled. ElapsedTime = {1}", task.NodeID, stopwatch.ElapsedMilliseconds);
						}
						stopwatch.Stop();
						return;
					}
					if (!this.IsValidPlugin(this.inventories[text]))
					{
						failedTasks.Add(text);
						if (BackgroundInventory.log.IsErrorEnabled)
						{
							BackgroundInventory.log.ErrorFormat("No plugins are available to execute Inventory '{0}' on Node {1} returned null result", text, task.NodeID);
						}
					}
					else
					{
						InventoryResultBase inventoryResultBase = this.DoInventory(this.inventories[text], task, globals, credentialsForNode, node);
						if (inventoryResultBase == null)
						{
							failedTasks.Add(text);
							if (BackgroundInventory.log.IsErrorEnabled)
							{
								BackgroundInventory.log.ErrorFormat("Inventory '{0}' on Node {1} returned null result", text, task.NodeID);
							}
						}
						else
						{
							if (inventoryResultBase.Outcome == 1)
							{
								bool flag = false;
								try
								{
									flag = this.ProcessResults(this.inventories[text], task, inventoryResultBase, node);
								}
								catch (Exception ex)
								{
									BackgroundInventory.log.Error(string.Format("Inventory '{0}' failed to import results for {1}", task, text), ex);
								}
								if (flag)
								{
									completedTasks.Add(text);
								}
								else
								{
									failedTasks.Add(text);
								}
							}
							else
							{
								failedTasks.Add(text);
								if (inventoryResultBase.Error != null)
								{
									if (BackgroundInventory.log.IsWarnEnabled)
									{
										BackgroundInventory.log.WarnFormat("Inventory '{0}' on Node {1} failed with code {2}", text, task, inventoryResultBase.Error.ErrorCode);
									}
									if (inventoryResultBase.Error.ErrorCode != 31002U)
									{
										IEnumerable<string> source = array;
										Func<string, bool> predicate;
										if ((predicate = <>9__0) == null)
										{
											predicate = (<>9__0 = ((string n) => !completedTasks.Contains(n) && !failedTasks.Contains(n)));
										}
										List<string> list = source.Where(predicate).ToList<string>();
										if (list.Count > 0)
										{
											failedTasks.AddRange(list);
											if (BackgroundInventory.log.IsWarnEnabled)
											{
												BackgroundInventory.log.WarnFormat("Skipping inventory for '{0}' on Node {1}", string.Join(":", list.ToArray()), task.NodeID);
												break;
											}
											break;
										}
									}
								}
								else if (BackgroundInventory.log.IsWarnEnabled)
								{
									BackgroundInventory.log.WarnFormat("Inventory '{0}' on Node {1} failed on unknown error", text, task.NodeID);
								}
							}
							BackgroundInventory.log.InfoFormat("Inventory with plugin '{0}' on Node {1} is completed", text, task.NodeID);
						}
					}
				}
			}
			string settingsForTask = this.GetSettingsForTask(task);
			if ((string.IsNullOrEmpty(settingsForTask) || !settingsForTask.Equals(task.Settings, StringComparison.OrdinalIgnoreCase)) && BackgroundInventory.log.IsInfoEnabled)
			{
				BackgroundInventory.log.InfoFormat("Skipping inventory result processing for {0}, NeedsInventory flag changed. OldValue = '{1}', NewValue = '{2}'.", task, task.Settings, settingsForTask);
				return;
			}
			if (failedTasks.Count == 0)
			{
				if (task.InventoryInput == BackgroundInventory.InventoryTask.InventoryInputSource.NodeSettings)
				{
					NodeSettingsDAL.DeleteSpecificSettings(task.ObjectSettingID, task.InventorySettingName);
				}
				else
				{
					InventorySettingsDAL.DeleteSpecificSettings(task.ObjectSettingID, task.InventorySettingName);
				}
				if (BackgroundInventory.log.IsInfoEnabled)
				{
					BackgroundInventory.log.InfoFormat("Inventorying of {0} completed in {1}ms.", task, stopwatch.ElapsedMilliseconds);
				}
			}
			else if (failedTasks.Count < array.Length)
			{
				string text2 = string.Join(":", failedTasks.ToArray());
				if (task.InventoryInput == BackgroundInventory.InventoryTask.InventoryInputSource.NodeSettings)
				{
					NodeSettingsDAL.UpdateSettingValue(task.ObjectSettingID, task.InventorySettingName, text2);
				}
				else
				{
					InventorySettingsDAL.UpdateSettingValue(task.ObjectSettingID, task.InventorySettingName, text2);
				}
				if (BackgroundInventory.log.IsInfoEnabled)
				{
					BackgroundInventory.log.InfoFormat("Inventorying of {0} partially completed in {1}ms. NeedsInventory updated to '{2}'", task, stopwatch.ElapsedMilliseconds, text2);
				}
			}
			else if (BackgroundInventory.log.IsInfoEnabled)
			{
				BackgroundInventory.log.InfoFormat("Inventorying of {0} failed. Elapsed time {1}ms.", task, stopwatch.ElapsedMilliseconds);
			}
			stopwatch.Stop();
		}

		// Token: 0x060008E1 RID: 2273 RVA: 0x0003FF38 File Offset: 0x0003E138
		private bool IsValidPlugin(object plugin)
		{
			bool flag = plugin is IBackgroundInventoryPlugin;
			IBackgroundInventoryPlugin2 backgroundInventoryPlugin = plugin as IBackgroundInventoryPlugin2;
			return flag || backgroundInventoryPlugin != null;
		}

		// Token: 0x060008E2 RID: 2274 RVA: 0x0003FF5C File Offset: 0x0003E15C
		private InventoryResultBase DoInventory(object plugin, BackgroundInventory.InventoryTask task, GlobalSettingsBase globals, Credential credentials, Node node)
		{
			IBackgroundInventoryPlugin backgroundInventoryPlugin = plugin as IBackgroundInventoryPlugin;
			if (backgroundInventoryPlugin != null)
			{
				return backgroundInventoryPlugin.DoInventory(globals, credentials, node);
			}
			IBackgroundInventoryPlugin2 backgroundInventoryPlugin2 = plugin as IBackgroundInventoryPlugin2;
			if (backgroundInventoryPlugin2 != null)
			{
				return backgroundInventoryPlugin2.DoInventory(globals, credentials, new BackgroundInventoryObject(node, task.ObjectID, task.ObjectType));
			}
			return null;
		}

		// Token: 0x060008E3 RID: 2275 RVA: 0x0003FFA8 File Offset: 0x0003E1A8
		private bool ProcessResults(object plugin, BackgroundInventory.InventoryTask task, InventoryResultBase result, Node node)
		{
			IBackgroundInventoryPlugin backgroundInventoryPlugin = plugin as IBackgroundInventoryPlugin;
			if (backgroundInventoryPlugin != null)
			{
				return backgroundInventoryPlugin.ProcessResults(result, node);
			}
			IBackgroundInventoryPlugin2 backgroundInventoryPlugin2 = plugin as IBackgroundInventoryPlugin2;
			return backgroundInventoryPlugin2 != null && backgroundInventoryPlugin2.ProcessResults(result, new BackgroundInventoryObject(node, task.ObjectID, task.ObjectType));
		}

		// Token: 0x060008E4 RID: 2276 RVA: 0x0003FFEF File Offset: 0x0003E1EF
		private string GetSettingsForTask(BackgroundInventory.InventoryTask task)
		{
			if (task.InventoryInput != BackgroundInventory.InventoryTask.InventoryInputSource.NodeSettings)
			{
				return InventorySettingsDAL.GetInventorySettings(task.ObjectSettingID, task.InventorySettingName);
			}
			return NodeSettingsDAL.GetNodeSettings(task.ObjectSettingID, task.InventorySettingName);
		}

		// Token: 0x060008E5 RID: 2277 RVA: 0x0004001C File Offset: 0x0003E21C
		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing && this.scheduler != null)
				{
					this.scheduler.Dispose();
					this.scheduler = null;
				}
				this.disposed = true;
			}
		}

		// Token: 0x060008E6 RID: 2278 RVA: 0x0004004A File Offset: 0x0003E24A
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x060008E7 RID: 2279 RVA: 0x0004005C File Offset: 0x0003E25C
		~BackgroundInventory()
		{
			this.Dispose(false);
		}

		// Token: 0x04000280 RID: 640
		private static readonly Log log = new Log();

		// Token: 0x04000281 RID: 641
		private QueuedTaskScheduler<BackgroundInventory.InventoryTask> scheduler;

		// Token: 0x04000282 RID: 642
		private IPollersDAL pollersDAL = new PollersDAL();

		// Token: 0x04000283 RID: 643
		private Dictionary<string, object> inventories;

		// Token: 0x04000284 RID: 644
		private SnmpGlobalSettings snmpGlobals;

		// Token: 0x04000285 RID: 645
		private WmiGlobalSettings wmiGlobals;

		// Token: 0x04000286 RID: 646
		private bool disposed;

		// Token: 0x020001A7 RID: 423
		public class InventoryTask
		{
			// Token: 0x06000C98 RID: 3224 RVA: 0x0004AEE5 File Offset: 0x000490E5
			public InventoryTask(int nodeID, int objectID, string objectType, int objectSettingID, string settings, string inventorySettingName, BackgroundInventory.InventoryTask.InventoryInputSource inventoryInputSource)
			{
				this.NodeID = nodeID;
				this.ObjectSettingID = objectSettingID;
				this.Settings = settings;
				this.InventorySettingName = inventorySettingName;
				this.ObjectID = objectID;
				this.ObjectType = objectType;
				this.InventoryInput = inventoryInputSource;
			}

			// Token: 0x06000C99 RID: 3225 RVA: 0x0004AF24 File Offset: 0x00049124
			public override string ToString()
			{
				return string.Format("NodeID = {0}, NodeSettingID = {1}, Settings = {2}, InventorySettingName = {3}, ObjectID = {4}, ObjectType = {5}", new object[]
				{
					this.NodeID,
					this.ObjectSettingID,
					this.Settings,
					this.InventorySettingName,
					this.ObjectID,
					this.ObjectType
				});
			}

			// Token: 0x04000564 RID: 1380
			public int NodeID;

			// Token: 0x04000565 RID: 1381
			public int ObjectSettingID;

			// Token: 0x04000566 RID: 1382
			public string Settings;

			// Token: 0x04000567 RID: 1383
			public string InventorySettingName;

			// Token: 0x04000568 RID: 1384
			public int ObjectID;

			// Token: 0x04000569 RID: 1385
			public string ObjectType;

			// Token: 0x0400056A RID: 1386
			public BackgroundInventory.InventoryTask.InventoryInputSource InventoryInput;

			// Token: 0x020001CF RID: 463
			public enum InventoryInputSource
			{
				// Token: 0x040005D5 RID: 1493
				NodeSettings,
				// Token: 0x040005D6 RID: 1494
				InventorySettings
			}
		}
	}
}
