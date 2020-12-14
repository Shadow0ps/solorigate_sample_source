using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using SolarWinds.InformationService.Linq.Plugins.Core.Orion;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Data;
using SolarWinds.Orion.Core.Common.i18n;
using SolarWinds.Orion.Core.Common.Licensing;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Discovery.DataAccess;
using SolarWinds.Orion.Core.Models.Discovery;
using SolarWinds.Orion.Core.Strings;
using SolarWinds.Orion.Discovery.Contract.DiscoveryPlugin;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000019 RID: 25
	public class DiscoveryImportManager
	{
		// Token: 0x060002A9 RID: 681 RVA: 0x000105BC File Offset: 0x0000E7BC
		public static DiscoveryImportProgressInfo GetImportProgress(Guid importId)
		{
			object syncRoot = ((ICollection)DiscoveryImportManager.imports).SyncRoot;
			DiscoveryImportProgressInfo result;
			lock (syncRoot)
			{
				if (DiscoveryImportManager.imports.ContainsKey(importId))
				{
					DiscoveryImportProgressInfo discoveryImportProgressInfo = DiscoveryImportManager.imports[importId];
					if (discoveryImportProgressInfo.LogBuilder.Length > 131072)
					{
						StringBuilder stringBuilder = new StringBuilder();
						StringBuilder stringBuilder2 = new StringBuilder();
						using (StringReader stringReader = new StringReader(discoveryImportProgressInfo.NewLogText))
						{
							bool flag2 = false;
							string text;
							while ((text = stringReader.ReadLine()) != null)
							{
								if ((stringBuilder.Length + text.Length <= 131072 || stringBuilder.Length == 0) && !flag2)
								{
									stringBuilder.AppendLine(text);
								}
								else
								{
									flag2 = true;
									stringBuilder2.AppendLine(text);
								}
							}
						}
						discoveryImportProgressInfo.NewLogText = stringBuilder2.ToString();
						result = new DiscoveryImportProgressInfo(discoveryImportProgressInfo)
						{
							NewLogText = stringBuilder.ToString(),
							Finished = false
						};
					}
					else
					{
						DiscoveryImportManager.imports.Remove(importId);
						result = discoveryImportProgressInfo;
					}
				}
				else
				{
					result = null;
				}
			}
			return result;
		}

		// Token: 0x060002AA RID: 682 RVA: 0x000106EC File Offset: 0x0000E8EC
		private static void UpdateProgress(Guid importId, string text, double progress, double phaseProgress, string phaseName, bool finished)
		{
			object syncRoot = ((ICollection)DiscoveryImportManager.imports).SyncRoot;
			lock (syncRoot)
			{
				if (!DiscoveryImportManager.imports.ContainsKey(importId))
				{
					DiscoveryImportManager.imports[importId] = new DiscoveryImportProgressInfo();
				}
				DiscoveryImportProgressInfo discoveryImportProgressInfo = DiscoveryImportManager.imports[importId];
				discoveryImportProgressInfo.LogBuilder.AppendLine(text);
				discoveryImportProgressInfo.Finished = finished;
				discoveryImportProgressInfo.OverallProgress = progress;
				discoveryImportProgressInfo.PhaseProgress = phaseProgress;
				discoveryImportProgressInfo.PhaseName = phaseName;
				if (DiscoveryImportManager.log.IsInfoEnabled)
				{
					DiscoveryImportManager.log.InfoFormat("{0} {1}/{2}: {3}", new object[]
					{
						phaseName,
						progress,
						phaseProgress,
						text
					});
				}
			}
		}

		// Token: 0x060002AB RID: 683 RVA: 0x000107B8 File Offset: 0x0000E9B8
		public static void UpdateProgress(Guid importId, string text, string phaseName, bool finished)
		{
			DiscoveryImportManager.UpdateProgress(importId, text, 0.0, 0.0, phaseName, finished);
		}

		// Token: 0x060002AC RID: 684 RVA: 0x000107D5 File Offset: 0x0000E9D5
		public static StartImportStatus StartImport(Guid importId, DiscoveryResultBase result, SortedDictionary<int, List<IDiscoveryPlugin>> importingPlugins)
		{
			return DiscoveryImportManager.StartImport(importId, result, importingPlugins, false, null);
		}

		// Token: 0x060002AD RID: 685 RVA: 0x000107E4 File Offset: 0x0000E9E4
		internal static StartImportStatus StartImport(Guid importId, DiscoveryResultBase result, SortedDictionary<int, List<IDiscoveryPlugin>> importingPlugins, bool checkLicenseLimits, DiscoveryImportManager.CallbackDiscoveryImportFinished callbackAfterImport)
		{
			if (result == null)
			{
				throw new ArgumentNullException("result");
			}
			ThreadPool.QueueUserWorkItem(delegate(object state)
			{
				try
				{
					DiscoveryImportManager.StartImportInternal(importId, result, importingPlugins, checkLicenseLimits, callbackAfterImport);
				}
				catch (Exception ex)
				{
					DiscoveryImportManager.log.Error("Error in StartImport", ex);
				}
			});
			return StartImportStatus.Started;
		}

		// Token: 0x060002AE RID: 686 RVA: 0x00010840 File Offset: 0x0000EA40
		private static void StartImportInternal(Guid importId, DiscoveryResultBase result, SortedDictionary<int, List<IDiscoveryPlugin>> importingPlugins, bool checkLicenseLimits, DiscoveryImportManager.CallbackDiscoveryImportFinished callbackAfterImport)
		{
			string webjs_PS0_ = Resources.WEBJS_PS0_17;
			StartImportStatus status = StartImportStatus.Failed;
			List<DiscoveryLogItem> list = new List<DiscoveryLogItem>();
			try
			{
				DiscoveryConfiguration discoveryConfiguration = DiscoveryDatabase.GetDiscoveryConfiguration(result.ProfileID);
				if (discoveryConfiguration != null)
				{
					string name = discoveryConfiguration.Name;
				}
			}
			catch (Exception ex)
			{
				DiscoveryImportManager.log.Warn("Unable to load profile name", ex);
			}
			using (LocaleThreadState.EnsurePrimaryLocale())
			{
				try
				{
					DiscoveryNetObjectStatusManager.Instance.BeginOrionDatabaseChanges();
					if (checkLicenseLimits)
					{
						if (DiscoveryImportManager.GetLicensedStatus(result).Any((ElementLicenseInfo n) => n.ExceededBy != 0))
						{
							DiscoveryImportManager.log.Debug("Can't import discovery result, because license was exceeded");
							status = StartImportStatus.LicenseExceeded;
							return;
						}
					}
					double progress = 0.0;
					double num = (double)(100 / importingPlugins.Keys.Count);
					foreach (int key in importingPlugins.Keys)
					{
						using (List<IDiscoveryPlugin>.Enumerator enumerator2 = importingPlugins[key].GetEnumerator())
						{
							while (enumerator2.MoveNext())
							{
								IDiscoveryPlugin plugin = enumerator2.Current;
								ISupportImportLog supportImportLog = plugin as ISupportImportLog;
								if (supportImportLog != null)
								{
									supportImportLog.SetImportLogCallback(new Action<DiscoveryLogItem>(list.Add));
								}
								plugin.ImportResults(result, delegate(string message, double phaseProgress)
								{
									DiscoveryImportManager.UpdateProgress(importId, message, progress + phaseProgress / (double)importingPlugins.Keys.Count, phaseProgress, plugin.GetImportPhaseName(), false);
								});
							}
						}
						progress += num;
					}
					DiscoveryImportManager.UpdateProgress(importId, Resources.LIBCODE_VB0_28, 100.0, 100.0, string.Empty, true);
					status = StartImportStatus.Finished;
				}
				catch (Exception ex2)
				{
					status = StartImportStatus.Failed;
					DiscoveryImportManager.log.Error("Exception occurred during discovery import", ex2);
					DiscoveryImportManager.UpdateProgress(importId, Resources.LIBCODE_TM0_30, 100.0, 100.0, string.Empty, true);
				}
				finally
				{
					DiscoveryNetObjectStatusManager.Instance.EndOrionDatabaseChanges();
					result.BatchID = Guid.NewGuid();
					try
					{
						DiscoveryImportManager.InsertDiscoveryLogItems(list, result.BatchID);
					}
					catch (Exception ex3)
					{
						DiscoveryImportManager.log.Error("Unable to store discovery import items", ex3);
					}
					if (callbackAfterImport != null)
					{
						try
						{
							callbackAfterImport(result, importId, status);
						}
						catch (Exception ex4)
						{
							DiscoveryImportManager.log.Error("Error while calling callback after import.", ex4);
						}
					}
					DiscoveryNetObjectStatusManager.Instance.RequestUpdateAsync(null, BusinessLayerSettings.Instance.DiscoveryUpdateNetObjectStatusWaitForChangesDelay);
				}
			}
		}

		// Token: 0x060002AF RID: 687 RVA: 0x00010BD4 File Offset: 0x0000EDD4
		public static List<ElementLicenseInfo> GetLicensedStatus(DiscoveryResultBase discoveryResult)
		{
			if (discoveryResult == null)
			{
				throw new ArgumentNullException("discoveryResult");
			}
			List<ElementLicenseInfo> list = new List<ElementLicenseInfo>();
			IFeatureManager featureManager = new FeatureManager();
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			Dictionary<string, int> elementsManagedCount = LicenseSaturationLogic.GetElementsManagedCount();
			foreach (string text in elementsManagedCount.Keys)
			{
				dictionary[text] = featureManager.GetMaxElementCount(text);
			}
			using (IEnumerator<DiscoveryPluginResultBase> enumerator2 = discoveryResult.PluginResults.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					List<ElementLicenseInfo> collection;
					if (!enumerator2.Current.CheckLicensingStatusForImport(elementsManagedCount, dictionary, ref collection))
					{
						list.AddRange(collection);
					}
				}
			}
			return list;
		}

		// Token: 0x060002B0 RID: 688 RVA: 0x00010CA8 File Offset: 0x0000EEA8
		private static void InsertDiscoveryLogItems(List<DiscoveryLogItem> items, Guid batchID)
		{
			using (IDataReader dataReader = new EnumerableDataReader<DiscoveryLogItem>(new SinglePropertyAccessor<DiscoveryLogItem>().AddColumn("BatchID", (DiscoveryLogItem i) => batchID).AddColumn("EntityType", (DiscoveryLogItem i) => i.EntityType).AddColumn("DisplayName", (DiscoveryLogItem i) => i.DisplayName).AddColumn("NetObjectID", (DiscoveryLogItem i) => i.NetObjectID), items))
			{
				SqlHelper.ExecuteBulkCopy("DiscoveryLogItems", dataReader, SqlBulkCopyOptions.Default);
			}
		}

		// Token: 0x060002B1 RID: 689 RVA: 0x00010D88 File Offset: 0x0000EF88
		internal static void FillDiscoveryLogEntity(DiscoveryLogs discoveryLog, DiscoveryResultBase result, StartImportStatus status)
		{
			DateTime utcNow = DateTime.UtcNow;
			discoveryLog.FinishedTimeStamp = utcNow.AddTicks(-(utcNow.Ticks % 10000000L));
			discoveryLog.BatchID = new Guid?(result.BatchID);
			discoveryLog.ProfileID = result.ProfileID;
			switch (status)
			{
			case StartImportStatus.Failed:
				discoveryLog.Result = 3;
				discoveryLog.ResultDescription = Resources2.DiscoveryLogResult_ImportFailed;
				discoveryLog.ErrorMessage = Resources2.DiscoveryLogError_UnknownError;
				return;
			case StartImportStatus.LicenseExceeded:
				discoveryLog.Result = 4;
				discoveryLog.ResultDescription = Resources2.DiscoveryLogResult_ImportFailedLicenseExceeded;
				return;
			case StartImportStatus.Finished:
				discoveryLog.Result = 2;
				discoveryLog.ResultDescription = Resources2.DiscoveryLogResult_ImportFinished;
				return;
			default:
				return;
			}
		}

		// Token: 0x04000072 RID: 114
		private const int MAX_TEXT_LENGTH = 131072;

		// Token: 0x04000073 RID: 115
		private static readonly Log log = new Log();

		// Token: 0x04000074 RID: 116
		private static readonly Dictionary<Guid, DiscoveryImportProgressInfo> imports = new Dictionary<Guid, DiscoveryImportProgressInfo>();

		// Token: 0x02000105 RID: 261
		// (Invoke) Token: 0x06000A79 RID: 2681
		public delegate void CallbackDiscoveryImportFinished(DiscoveryResultBase result, Guid importID, StartImportStatus status);
	}
}
