using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Models.Credentials;
using SolarWinds.Orion.Core.Pollers;
using SolarWinds.Orion.Core.Pollers.Cpu.SNMP;
using SolarWinds.Orion.Core.Pollers.Memory.SNMP;
using SolarWinds.Orion.Pollers.Framework;
using SolarWinds.Orion.Pollers.Framework.SNMP;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200000E RID: 14
	internal class BackgroundInventoryManager : IDisposable
	{
		// Token: 0x1700001F RID: 31
		// (get) Token: 0x0600023A RID: 570 RVA: 0x0000F718 File Offset: 0x0000D918
		public bool IsRunning
		{
			get
			{
				return this.scheduler.IsRunning;
			}
		}

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x0600023B RID: 571 RVA: 0x0000F725 File Offset: 0x0000D925
		public int QueueSize
		{
			get
			{
				return this.scheduler.QueueSize;
			}
		}

		// Token: 0x0600023C RID: 572 RVA: 0x0000F734 File Offset: 0x0000D934
		public BackgroundInventoryManager(int parallelTasksCount)
		{
			this.scheduler = new QueuedTaskScheduler<BackgroundInventoryManager.InventoryTask>(new QueuedTaskScheduler<BackgroundInventoryManager.InventoryTask>.TaskProcessingRoutine(this.DoInventory), parallelTasksCount);
			this.scheduler.TaskProcessingFinished += this.scheduler_TaskProcessingFinished;
			this.globals = new SnmpGlobalSettings();
			this.globals.MaxReplies = SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-SNMP MaxReps", 5);
			this.globals.RequestTimeout = SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-SNMP Timeout", 2500);
			this.globals.RequestRetries = SettingsDAL.GetCurrentInt("SWNetPerfMon-Settings-SNMP Retries", 2);
			this.globals.HsrpEnabled = SettingsDAL.GetCurrent<bool>("SWNetPerfMon-Settings-SNMP HSRPEnabled", true);
			this.inventories = new Dictionary<string, BackgroundInventoryManager.DoInventoryDelegate>(StringComparer.OrdinalIgnoreCase);
			this.inventories.Add("Cpu", new BackgroundInventoryManager.DoInventoryDelegate(this.DoCpuInventory));
			this.inventories.Add("Memory", new BackgroundInventoryManager.DoInventoryDelegate(this.DoMemoryInventory));
		}

		// Token: 0x0600023D RID: 573 RVA: 0x0000F830 File Offset: 0x0000DA30
		private void scheduler_TaskProcessingFinished(object sender, EventArgs e)
		{
			BackgroundInventoryManager.log.Info("Background Inventorying Finished");
		}

		// Token: 0x0600023E RID: 574 RVA: 0x0000F841 File Offset: 0x0000DA41
		public void Enqueue(int nodeID, string settings)
		{
			this.scheduler.EnqueueTask(new BackgroundInventoryManager.InventoryTask(nodeID, settings));
		}

		// Token: 0x0600023F RID: 575 RVA: 0x0000F855 File Offset: 0x0000DA55
		public void Start()
		{
			this.scheduler.Start();
		}

		// Token: 0x06000240 RID: 576 RVA: 0x0000F862 File Offset: 0x0000DA62
		public void Cancel()
		{
			this.scheduler.Cancel();
		}

		// Token: 0x06000241 RID: 577 RVA: 0x0000F870 File Offset: 0x0000DA70
		internal void DoInventory(BackgroundInventoryManager.InventoryTask task)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			Node node = NodeBLDAL.GetNode(task.NodeID);
			if (node == null || node.PolledStatus != 1)
			{
				BackgroundInventoryManager.log.InfoFormat("Skipping inventorying of Node {0}, status is not UP.", task.NodeID);
				return;
			}
			SnmpSettings nodeSettings = new SnmpSettings
			{
				AgentPort = (int)node.SNMPPort,
				ProtocolVersion = node.SNMPVersion,
				TargetIP = IPAddress.Parse(node.IpAddress)
			};
			SnmpInventorySettings inventorySettings = new SnmpInventorySettings(node.SysObjectID);
			SnmpCredentials credentials = CredentialHelper.ParseCredentialsFromNode(node) as SnmpCredentials;
			List<string> list = new List<string>();
			if (BackgroundInventoryManager.log.IsInfoEnabled)
			{
				BackgroundInventoryManager.log.InfoFormat("Starting inventorying of Node {0}, NeedsInventory = '{1}'", task.NodeID, task.Settings);
			}
			string[] array = task.Settings.Split(new char[]
			{
				':'
			}).Distinct<string>().ToArray<string>();
			List<string> failedTasks = new List<string>();
			List<string> completedTasks = new List<string>();
			Func<string, bool> <>9__0;
			foreach (string text in array)
			{
				if (!this.inventories.ContainsKey(text))
				{
					failedTasks.Add(text);
					if (BackgroundInventoryManager.log.IsErrorEnabled)
					{
						BackgroundInventoryManager.log.ErrorFormat("Unable to inventory '{0}' on Node {1}", text, task.NodeID);
					}
				}
				else
				{
					if (this.scheduler.IsTaskCanceled)
					{
						if (BackgroundInventoryManager.log.IsInfoEnabled)
						{
							BackgroundInventoryManager.log.InfoFormat("Inventorying of Node {0} was canceled. ElapsedTime = {1}", task.NodeID, stopwatch.ElapsedMilliseconds);
						}
						stopwatch.Stop();
						return;
					}
					InventoryPollersResult inventoryPollersResult = this.inventories[text](nodeSettings, inventorySettings, credentials);
					if (inventoryPollersResult == null)
					{
						failedTasks.Add(text);
						if (BackgroundInventoryManager.log.IsErrorEnabled)
						{
							BackgroundInventoryManager.log.ErrorFormat("Inventory '{0}' on Node {1} returned null result", text, task.NodeID);
						}
					}
					else if (inventoryPollersResult.Outcome == 1)
					{
						completedTasks.Add(text);
						list.AddRange(inventoryPollersResult.PollerTypes);
					}
					else
					{
						failedTasks.Add(text);
						if (inventoryPollersResult.Error != null)
						{
							if (BackgroundInventoryManager.log.IsWarnEnabled)
							{
								BackgroundInventoryManager.log.WarnFormat("Inventory '{0}' on Node {1} failed with code {2}", text, task.NodeID, inventoryPollersResult.Error.ErrorCode);
							}
							if (inventoryPollersResult.Error.ErrorCode != 31002U)
							{
								IEnumerable<string> source = array;
								Func<string, bool> predicate;
								if ((predicate = <>9__0) == null)
								{
									predicate = (<>9__0 = ((string n) => !completedTasks.Contains(n) && !failedTasks.Contains(n)));
								}
								List<string> list2 = source.Where(predicate).ToList<string>();
								if (list2.Count > 0)
								{
									failedTasks.AddRange(list2);
									if (BackgroundInventoryManager.log.IsWarnEnabled)
									{
										BackgroundInventoryManager.log.WarnFormat("Skipping inventory for '{0}' on Node {1}", string.Join(":", list2.ToArray()), task.NodeID);
										break;
									}
									break;
								}
							}
						}
						else if (BackgroundInventoryManager.log.IsWarnEnabled)
						{
							BackgroundInventoryManager.log.WarnFormat("Inventory '{0}' on Node {1} failed on unknown error", text, task.NodeID);
						}
					}
				}
			}
			string lastNodeSettings = NodeSettingsDAL.GetLastNodeSettings(task.NodeID, CoreConstants.NeedsInventoryFlag);
			if ((string.IsNullOrEmpty(lastNodeSettings) || !lastNodeSettings.Equals(task.Settings, StringComparison.OrdinalIgnoreCase)) && BackgroundInventoryManager.log.IsInfoEnabled)
			{
				BackgroundInventoryManager.log.InfoFormat("Skipping inventory result processing for Node {0}, NeedsInventory flag changed. OldValue = '{1}', NewValue = '{2}'.", task.NodeID, task.Settings, lastNodeSettings);
				return;
			}
			this.InsertDetectedPollers(task, list);
			if (failedTasks.Count == 0)
			{
				NodeSettingsDAL.DeleteSpecificSettingForNode(task.NodeID, CoreConstants.NeedsInventoryFlag);
				if (BackgroundInventoryManager.log.IsInfoEnabled)
				{
					BackgroundInventoryManager.log.InfoFormat("Inventorying of Node {0} completed in {1}ms.", task.NodeID, stopwatch.ElapsedMilliseconds);
				}
			}
			else if (failedTasks.Count < array.Length)
			{
				string text2 = string.Join(":", failedTasks.ToArray());
				NodeSettingsDAL.SafeInsertNodeSetting(task.NodeID, CoreConstants.NeedsInventoryFlag, text2);
				if (BackgroundInventoryManager.log.IsInfoEnabled)
				{
					BackgroundInventoryManager.log.InfoFormat("Inventorying of Node {0} partially completed in {1}ms. NeedsInventory updated to '{2}'", task.NodeID, stopwatch.ElapsedMilliseconds, text2);
				}
			}
			else if (BackgroundInventoryManager.log.IsInfoEnabled)
			{
				BackgroundInventoryManager.log.InfoFormat("Inventorying of Node {0} failed. Elapsed time {1}ms.", task.NodeID, stopwatch.ElapsedMilliseconds);
			}
			stopwatch.Stop();
		}

		// Token: 0x06000242 RID: 578 RVA: 0x0000FD17 File Offset: 0x0000DF17
		internal void InsertDetectedPollers(BackgroundInventoryManager.InventoryTask task, List<string> detectedPollers)
		{
			if (detectedPollers.Count > 0)
			{
				this.pollersDAL.UpdateNetObjectPollers("N", task.NodeID, detectedPollers.ToArray());
			}
		}

		// Token: 0x06000243 RID: 579 RVA: 0x0000FD3E File Offset: 0x0000DF3E
		private InventoryPollersResult DoCpuInventory(SnmpSettings nodeSettings, SnmpInventorySettings inventorySettings, SnmpCredentials credentials)
		{
			return new CpuSnmpInventory().DoInventory(this.globals, nodeSettings, inventorySettings, credentials) as InventoryPollersResult;
		}

		// Token: 0x06000244 RID: 580 RVA: 0x0000FD58 File Offset: 0x0000DF58
		private InventoryPollersResult DoMemoryInventory(SnmpSettings nodeSettings, SnmpInventorySettings inventorySettings, SnmpCredentials credentials)
		{
			return new MemorySnmpInventory().DoInventory(this.globals, nodeSettings, inventorySettings, credentials) as InventoryPollersResult;
		}

		// Token: 0x06000245 RID: 581 RVA: 0x0000FD72 File Offset: 0x0000DF72
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

		// Token: 0x06000246 RID: 582 RVA: 0x0000FDA0 File Offset: 0x0000DFA0
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06000247 RID: 583 RVA: 0x0000FDB0 File Offset: 0x0000DFB0
		~BackgroundInventoryManager()
		{
			this.Dispose(false);
		}

		// Token: 0x0400005E RID: 94
		private static readonly Log log = new Log();

		// Token: 0x0400005F RID: 95
		private QueuedTaskScheduler<BackgroundInventoryManager.InventoryTask> scheduler;

		// Token: 0x04000060 RID: 96
		private SnmpGlobalSettings globals;

		// Token: 0x04000061 RID: 97
		private PollersDAL pollersDAL = new PollersDAL();

		// Token: 0x04000062 RID: 98
		private Dictionary<string, BackgroundInventoryManager.DoInventoryDelegate> inventories;

		// Token: 0x04000063 RID: 99
		private bool disposed;

		// Token: 0x020000FB RID: 251
		// (Invoke) Token: 0x06000A51 RID: 2641
		public delegate InventoryPollersResult DoInventoryDelegate(SnmpSettings nodeSettings, SnmpInventorySettings inventorySettings, SnmpCredentials credentials);

		// Token: 0x020000FC RID: 252
		public class InventoryTask
		{
			// Token: 0x06000A54 RID: 2644 RVA: 0x000477AF File Offset: 0x000459AF
			public InventoryTask(int nodeID, string settings)
			{
				this.NodeID = nodeID;
				this.Settings = settings;
			}

			// Token: 0x06000A55 RID: 2645 RVA: 0x000477C5 File Offset: 0x000459C5
			public override string ToString()
			{
				return string.Format("NodeID = {0}, Settings = {1}", this.NodeID, this.Settings);
			}

			// Token: 0x04000369 RID: 873
			public int NodeID;

			// Token: 0x0400036A RID: 874
			public string Settings;
		}
	}
}
