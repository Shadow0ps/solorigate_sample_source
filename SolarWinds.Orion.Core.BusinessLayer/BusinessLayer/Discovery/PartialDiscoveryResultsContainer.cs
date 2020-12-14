using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SolarWinds.JobEngine;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Models.Discovery;
using SolarWinds.Orion.Core.Models.Interfaces;
using SolarWinds.Orion.Discovery.Contract.DiscoveryPlugin;
using SolarWinds.Orion.Discovery.Job;

namespace SolarWinds.Orion.Core.BusinessLayer.Discovery
{
	// Token: 0x0200007B RID: 123
	internal class PartialDiscoveryResultsContainer : IDisposable
	{
		// Token: 0x1400000B RID: 11
		// (add) Token: 0x06000638 RID: 1592 RVA: 0x0002532C File Offset: 0x0002352C
		// (remove) Token: 0x06000639 RID: 1593 RVA: 0x00025364 File Offset: 0x00023564
		public event EventHandler<DiscoveryResultsCompletedEventArgs> DiscoveryResultsComplete = delegate(object <p0>, DiscoveryResultsCompletedEventArgs <p1>)
		{
		};

		// Token: 0x0600063A RID: 1594 RVA: 0x00025399 File Offset: 0x00023599
		public PartialDiscoveryResultsContainer() : this(new PartialDiscoveryResultsFilePersistence(), () => DateTime.UtcNow, TimeSpan.FromSeconds(10.0))
		{
		}

		// Token: 0x0600063B RID: 1595 RVA: 0x000253D4 File Offset: 0x000235D4
		internal PartialDiscoveryResultsContainer(IPartialDiscoveryResultsPersistence persistenceStore, Func<DateTime> dateTimeProvider, TimeSpan expirationCheckFrequency)
		{
			if (persistenceStore == null)
			{
				throw new ArgumentNullException("persistenceStore");
			}
			if (dateTimeProvider == null)
			{
				throw new ArgumentNullException("dateTimeProvider");
			}
			this._persistenceStore = persistenceStore;
			this._dateTimeProvider = dateTimeProvider;
			this._resultsByMainJobId = new Dictionary<Guid, List<PartialDiscoveryResultsContainer.PartialResult>>();
			this._resultsByOwnJobId = new Dictionary<Guid, PartialDiscoveryResultsContainer.PartialResult>();
			this._mainResultsReadyForComplete = new HashSet<Guid>();
			this._expirationCleanupTimer = new Timer(delegate(object x)
			{
				this.RunExpirationCheck();
			}, null, expirationCheckFrequency, expirationCheckFrequency);
		}

		// Token: 0x0600063C RID: 1596 RVA: 0x0002547C File Offset: 0x0002367C
		public void CreatePartialResult(Guid scheduledJobId, OrionDiscoveryJobResult result, SortedDictionary<int, List<IDiscoveryPlugin>> orderedPlugins, JobState jobState)
		{
			if (result == null)
			{
				throw new ArgumentNullException("result");
			}
			PartialDiscoveryResultsContainer.PartialResult partialResult = new PartialDiscoveryResultsContainer.PartialResult(scheduledJobId, scheduledJobId, orderedPlugins, jobState, result.ProfileId, this._persistenceStore, DateTime.MaxValue)
			{
				Result = result
			};
			object syncRoot = this._syncRoot;
			lock (syncRoot)
			{
				this._resultsByMainJobId[partialResult.JobId] = new List<PartialDiscoveryResultsContainer.PartialResult>();
				this._resultsByMainJobId[partialResult.JobId].Add(partialResult);
				this._resultsByOwnJobId[partialResult.JobId] = partialResult;
			}
		}

		// Token: 0x0600063D RID: 1597 RVA: 0x00025528 File Offset: 0x00023728
		public void AddExpectedPartialResult(Guid scheduledJobId, OrionDiscoveryJobResult result)
		{
			if (result == null)
			{
				throw new ArgumentNullException("result");
			}
			object syncRoot = this._syncRoot;
			List<PartialDiscoveryResultsContainer.PartialResult> list;
			lock (syncRoot)
			{
				PartialDiscoveryResultsContainer.PartialResult partialResult;
				if (!this._resultsByOwnJobId.TryGetValue(scheduledJobId, out partialResult))
				{
					throw new ArgumentException("Results with given job ID are not expected.", "scheduledJobId");
				}
				partialResult.Result = result;
				list = this.TryGetCompleteResults(partialResult.MainJobId);
			}
			if (list != null)
			{
				this.OnDiscoveryResultsComplete(list);
			}
		}

		// Token: 0x0600063E RID: 1598 RVA: 0x000255B0 File Offset: 0x000237B0
		public void ExpectPartialResult(Guid mainScheduledJobId, Guid scheduledJobId, TimeSpan timeout)
		{
			object syncRoot = this._syncRoot;
			lock (syncRoot)
			{
				List<PartialDiscoveryResultsContainer.PartialResult> list;
				if (!this._resultsByMainJobId.TryGetValue(mainScheduledJobId, out list))
				{
					throw new ArgumentException("Results with given main result ID are not in container.", "mainScheduledJobId");
				}
				PartialDiscoveryResultsContainer.PartialResult partialResult = new PartialDiscoveryResultsContainer.PartialResult(scheduledJobId, mainScheduledJobId, this._persistenceStore, this._dateTimeProvider().Add(timeout));
				list.Add(partialResult);
				this._resultsByOwnJobId[partialResult.JobId] = partialResult;
			}
		}

		// Token: 0x0600063F RID: 1599 RVA: 0x00025648 File Offset: 0x00023848
		public bool IsResultExpected(Guid scheduledJobId)
		{
			object syncRoot = this._syncRoot;
			bool result;
			lock (syncRoot)
			{
				result = (this._resultsByOwnJobId.ContainsKey(scheduledJobId) && !this._resultsByMainJobId.ContainsKey(scheduledJobId));
			}
			return result;
		}

		// Token: 0x06000640 RID: 1600 RVA: 0x000256A4 File Offset: 0x000238A4
		public void AllExpectedResultsRegistered(Guid mainScheduledJobId)
		{
			object syncRoot = this._syncRoot;
			List<PartialDiscoveryResultsContainer.PartialResult> list;
			lock (syncRoot)
			{
				this._mainResultsReadyForComplete.Add(mainScheduledJobId);
				list = this.TryGetCompleteResults(mainScheduledJobId);
			}
			if (list != null)
			{
				this.OnDiscoveryResultsComplete(list);
			}
		}

		// Token: 0x06000641 RID: 1601 RVA: 0x00025700 File Offset: 0x00023900
		public void ClearStore()
		{
			this._persistenceStore.ClearStore();
		}

		// Token: 0x06000642 RID: 1602 RVA: 0x00025710 File Offset: 0x00023910
		public void RemoveExpectedPartialResult(Guid scheduledJobId)
		{
			object syncRoot = this._syncRoot;
			List<PartialDiscoveryResultsContainer.PartialResult> list;
			lock (syncRoot)
			{
				PartialDiscoveryResultsContainer.PartialResult partialResult;
				if (!this._resultsByOwnJobId.TryGetValue(scheduledJobId, out partialResult))
				{
					throw new ArgumentException("Results with given job ID are not expected.", "scheduledJobId");
				}
				this._resultsByOwnJobId.Remove(scheduledJobId);
				this._resultsByMainJobId[partialResult.MainJobId].Remove(partialResult);
				this._persistenceStore.DeleteResult(partialResult.JobId);
				list = this.TryGetCompleteResults(partialResult.MainJobId);
			}
			if (list != null)
			{
				this.OnDiscoveryResultsComplete(list);
			}
		}

		// Token: 0x06000643 RID: 1603 RVA: 0x000257B8 File Offset: 0x000239B8
		private void RunExpirationCheck()
		{
			object syncRoot = this._syncRoot;
			lock (syncRoot)
			{
				foreach (PartialDiscoveryResultsContainer.PartialResult partialResult in this._resultsByOwnJobId.Values.ToList<PartialDiscoveryResultsContainer.PartialResult>())
				{
					if (!partialResult.HasResult && partialResult.Expiration < this._dateTimeProvider())
					{
						PartialDiscoveryResultsContainer._log.WarnFormat("Expected partial discovery results for job {0} were not received in defined time and are being discarded.", partialResult.JobId);
						this.RemoveExpectedPartialResult(partialResult.JobId);
					}
				}
			}
		}

		// Token: 0x06000644 RID: 1604 RVA: 0x0002587C File Offset: 0x00023A7C
		private void OnDiscoveryResultsComplete(List<PartialDiscoveryResultsContainer.PartialResult> results)
		{
			if (results == null || results.Count == 0)
			{
				PartialDiscoveryResultsContainer._log.WarnFormat("Attempt to report partial discovery results completion with empty results.", Array.Empty<object>());
				return;
			}
			OrionDiscoveryJobResult completeResult = this.MergePartialResults(results);
			object syncRoot = this._syncRoot;
			lock (syncRoot)
			{
				this.RemovePartialResults(results[0].MainJobId, results);
			}
			this.DiscoveryResultsComplete(this, new DiscoveryResultsCompletedEventArgs(completeResult, results[0].OrderedPlugins, results[0].MainJobId, results[0].JobState, results[0].ProfileId));
		}

		// Token: 0x06000645 RID: 1605 RVA: 0x00025934 File Offset: 0x00023B34
		private List<PartialDiscoveryResultsContainer.PartialResult> TryGetCompleteResults(Guid mainResultId)
		{
			object syncRoot = this._syncRoot;
			lock (syncRoot)
			{
				List<PartialDiscoveryResultsContainer.PartialResult> list;
				if (!this._resultsByMainJobId.TryGetValue(mainResultId, out list))
				{
					throw new ArgumentException("Main results for results with given ID are not in container.");
				}
				if (this.AreAllResultsReady(list))
				{
					return list;
				}
			}
			return null;
		}

		// Token: 0x06000646 RID: 1606 RVA: 0x0002599C File Offset: 0x00023B9C
		private bool AreAllResultsReady(List<PartialDiscoveryResultsContainer.PartialResult> results)
		{
			bool result = false;
			if (results.Count > 0)
			{
				if (results.All((PartialDiscoveryResultsContainer.PartialResult x) => x.HasResult))
				{
					result = this._mainResultsReadyForComplete.Contains(results[0].MainJobId);
				}
			}
			return result;
		}

		// Token: 0x06000647 RID: 1607 RVA: 0x000259F4 File Offset: 0x00023BF4
		private OrionDiscoveryJobResult MergePartialResults(List<PartialDiscoveryResultsContainer.PartialResult> results)
		{
			if (results.Count == 0)
			{
				throw new ArgumentException("Results for merge can't be empty.", "results");
			}
			OrionDiscoveryJobResult result = results[0].Result;
			if (result == null)
			{
				throw new ArgumentException("Main results for merge were not loaded.", "results");
			}
			OrionDiscoveryJobResult orionDiscoveryJobResult = (OrionDiscoveryJobResult)result.Copy();
			foreach (PartialDiscoveryResultsContainer.PartialResult partialResult in results.Skip(1))
			{
				this.MergePluginResults(orionDiscoveryJobResult.ProfileId, orionDiscoveryJobResult.PluginResults, partialResult.Result.PluginResults);
			}
			return orionDiscoveryJobResult;
		}

		// Token: 0x06000648 RID: 1608 RVA: 0x00025A9C File Offset: 0x00023C9C
		private void MergePluginResults(int? profileId, DiscoveryPluginItems<DiscoveryPluginResultBase> results, DiscoveryPluginItems<DiscoveryPluginResultBase> partialResultsToMerge)
		{
			DiscoveryPluginResultObjectMapping discoveryPluginResultObjectMapping = new DiscoveryPluginResultObjectMapping();
			List<DiscoveryPluginResultBase> list = new List<DiscoveryPluginResultBase>();
			using (IEnumerator<DiscoveryPluginResultBase> enumerator = results.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					DiscoveryPluginResultBase item = enumerator.Current;
					DiscoveryPluginResultBase discoveryPluginResultBase = partialResultsToMerge.Except(list).FirstOrDefault((DiscoveryPluginResultBase x) => x.GetType() == item.GetType());
					if (discoveryPluginResultBase != null)
					{
						IDiscoveryPluginResultMerge discoveryPluginResultMerge = item as IDiscoveryPluginResultMerge;
						if (discoveryPluginResultMerge == null)
						{
							PartialDiscoveryResultsContainer._log.WarnFormat("Plugin discovery results '{0}' do not implement IDiscoveryPluginResultMerge interface and will not be merged with other results instances.", item.GetType());
						}
						else
						{
							discoveryPluginResultMerge.MergeResults(discoveryPluginResultBase, discoveryPluginResultObjectMapping, profileId);
							list.Add(discoveryPluginResultBase);
						}
					}
				}
			}
			foreach (DiscoveryPluginResultBase discoveryPluginResultBase2 in partialResultsToMerge.Except(list))
			{
				results.Add(discoveryPluginResultBase2);
			}
		}

		// Token: 0x06000649 RID: 1609 RVA: 0x00025B94 File Offset: 0x00023D94
		private void RemovePartialResults(Guid mainResultId, IEnumerable<PartialDiscoveryResultsContainer.PartialResult> results)
		{
			object syncRoot = this._syncRoot;
			lock (syncRoot)
			{
				this._mainResultsReadyForComplete.Remove(mainResultId);
				this._resultsByMainJobId.Remove(mainResultId);
				foreach (PartialDiscoveryResultsContainer.PartialResult partialResult in results)
				{
					this._resultsByOwnJobId.Remove(partialResult.JobId);
					this._persistenceStore.DeleteResult(partialResult.JobId);
				}
			}
		}

		// Token: 0x0600064A RID: 1610 RVA: 0x00025C3C File Offset: 0x00023E3C
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x0600064B RID: 1611 RVA: 0x00025C4C File Offset: 0x00023E4C
		protected void Dispose(bool disposing)
		{
			if (this._disposed)
			{
				return;
			}
			if (disposing)
			{
				try
				{
					if (this._expirationCleanupTimer != null)
					{
						this._expirationCleanupTimer.Dispose();
						this._expirationCleanupTimer = null;
					}
				}
				catch (Exception ex)
				{
					PartialDiscoveryResultsContainer._log.Error("Error diposing PartialDiscoveryResultsContainer.", ex);
				}
				this._disposed = true;
			}
		}

		// Token: 0x0600064C RID: 1612 RVA: 0x00025CAC File Offset: 0x00023EAC
		~PartialDiscoveryResultsContainer()
		{
			this.Dispose(false);
		}

		// Token: 0x040001F5 RID: 501
		private static readonly Log _log = new Log();

		// Token: 0x040001F6 RID: 502
		private readonly object _syncRoot = new object();

		// Token: 0x040001F7 RID: 503
		private readonly Dictionary<Guid, List<PartialDiscoveryResultsContainer.PartialResult>> _resultsByMainJobId;

		// Token: 0x040001F8 RID: 504
		private readonly Dictionary<Guid, PartialDiscoveryResultsContainer.PartialResult> _resultsByOwnJobId;

		// Token: 0x040001F9 RID: 505
		private readonly HashSet<Guid> _mainResultsReadyForComplete;

		// Token: 0x040001FA RID: 506
		private readonly IPartialDiscoveryResultsPersistence _persistenceStore;

		// Token: 0x040001FB RID: 507
		private readonly Func<DateTime> _dateTimeProvider;

		// Token: 0x040001FC RID: 508
		private Timer _expirationCleanupTimer;

		// Token: 0x040001FD RID: 509
		private bool _disposed;

		// Token: 0x0200016A RID: 362
		private class PartialResult
		{
			// Token: 0x06000BCD RID: 3021 RVA: 0x000498D4 File Offset: 0x00047AD4
			public PartialResult(Guid jobId, Guid mainJobId, IPartialDiscoveryResultsPersistence persistenceStore, DateTime expiration) : this(jobId, mainJobId, null, 7, null, persistenceStore, expiration)
			{
			}

			// Token: 0x06000BCE RID: 3022 RVA: 0x000498F8 File Offset: 0x00047AF8
			public PartialResult(Guid jobId, Guid mainJobId, SortedDictionary<int, List<IDiscoveryPlugin>> orderedPlugins, JobState jobState, int? profileId, IPartialDiscoveryResultsPersistence persistenceStore, DateTime expiration)
			{
				if (persistenceStore == null)
				{
					throw new ArgumentNullException("persistenceStore");
				}
				this._persistenceStore = persistenceStore;
				this.JobId = jobId;
				this.MainJobId = mainJobId;
				this.OrderedPlugins = orderedPlugins;
				this.JobState = jobState;
				this.ProfileId = profileId;
				this.Expiration = expiration;
			}

			// Token: 0x17000150 RID: 336
			// (get) Token: 0x06000BCF RID: 3023 RVA: 0x0004994F File Offset: 0x00047B4F
			// (set) Token: 0x06000BD0 RID: 3024 RVA: 0x00049957 File Offset: 0x00047B57
			public Guid MainJobId { get; private set; }

			// Token: 0x17000151 RID: 337
			// (get) Token: 0x06000BD1 RID: 3025 RVA: 0x00049960 File Offset: 0x00047B60
			// (set) Token: 0x06000BD2 RID: 3026 RVA: 0x00049968 File Offset: 0x00047B68
			public Guid JobId { get; private set; }

			// Token: 0x17000152 RID: 338
			// (get) Token: 0x06000BD3 RID: 3027 RVA: 0x00049971 File Offset: 0x00047B71
			// (set) Token: 0x06000BD4 RID: 3028 RVA: 0x00049979 File Offset: 0x00047B79
			public SortedDictionary<int, List<IDiscoveryPlugin>> OrderedPlugins { get; private set; }

			// Token: 0x17000153 RID: 339
			// (get) Token: 0x06000BD5 RID: 3029 RVA: 0x00049982 File Offset: 0x00047B82
			// (set) Token: 0x06000BD6 RID: 3030 RVA: 0x0004998A File Offset: 0x00047B8A
			public JobState JobState { get; private set; }

			// Token: 0x17000154 RID: 340
			// (get) Token: 0x06000BD7 RID: 3031 RVA: 0x00049993 File Offset: 0x00047B93
			// (set) Token: 0x06000BD8 RID: 3032 RVA: 0x0004999B File Offset: 0x00047B9B
			public int? ProfileId { get; private set; }

			// Token: 0x17000155 RID: 341
			// (get) Token: 0x06000BD9 RID: 3033 RVA: 0x000499A4 File Offset: 0x00047BA4
			// (set) Token: 0x06000BDA RID: 3034 RVA: 0x000499AC File Offset: 0x00047BAC
			public bool HasResult { get; private set; }

			// Token: 0x17000156 RID: 342
			// (get) Token: 0x06000BDB RID: 3035 RVA: 0x000499B5 File Offset: 0x00047BB5
			// (set) Token: 0x06000BDC RID: 3036 RVA: 0x000499BD File Offset: 0x00047BBD
			public DateTime Expiration { get; private set; }

			// Token: 0x17000157 RID: 343
			// (get) Token: 0x06000BDD RID: 3037 RVA: 0x000499C6 File Offset: 0x00047BC6
			// (set) Token: 0x06000BDE RID: 3038 RVA: 0x000499F2 File Offset: 0x00047BF2
			public OrionDiscoveryJobResult Result
			{
				get
				{
					if (!this.HasResult)
					{
						return null;
					}
					if (this._result != null)
					{
						return this._result;
					}
					return this._persistenceStore.LoadResult(this.JobId);
				}
				set
				{
					if (!this._persistenceStore.SaveResult(this.JobId, value))
					{
						this._result = value;
					}
					this.HasResult = true;
				}
			}

			// Token: 0x04000499 RID: 1177
			private readonly IPartialDiscoveryResultsPersistence _persistenceStore;

			// Token: 0x0400049A RID: 1178
			private OrionDiscoveryJobResult _result;
		}
	}
}
