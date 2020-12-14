using System;
using System.Threading;
using System.Threading.Tasks;
using SolarWinds.Logging;

namespace SolarWinds.Orion.Core.BusinessLayer.Discovery
{
	// Token: 0x02000077 RID: 119
	internal class RescheduleDiscoveryJobsTask : IDisposable
	{
		// Token: 0x170000EC RID: 236
		// (get) Token: 0x0600060F RID: 1551 RVA: 0x000246A0 File Offset: 0x000228A0
		public bool IsPeriodicRescheduleTaskRunning
		{
			get
			{
				object reschedulingStartStopControlLock = this._reschedulingStartStopControlLock;
				bool result;
				lock (reschedulingStartStopControlLock)
				{
					result = (this._isPeriodicReschedulingEnabled != null && !this._isPeriodicReschedulingEnabled.IsCancellationRequested);
				}
				return result;
			}
		}

		// Token: 0x06000610 RID: 1552 RVA: 0x000246F8 File Offset: 0x000228F8
		public RescheduleDiscoveryJobsTask(Func<int, bool> updateDiscoveryJobsDelegate, int engineId, bool keepRunning, TimeSpan periodicRetryInterval)
		{
			if (periodicRetryInterval <= TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException("periodicRetryInterval", periodicRetryInterval, "Periodic retry interval has to be greater than zero");
			}
			if (updateDiscoveryJobsDelegate == null)
			{
				throw new ArgumentNullException("updateDiscoveryJobsDelegate");
			}
			this._updateDiscoveryJobsDelegate = updateDiscoveryJobsDelegate;
			this._engineId = engineId;
			this._keepRunning = keepRunning;
			this._periodicRetryInterval = periodicRetryInterval;
		}

		// Token: 0x06000611 RID: 1553 RVA: 0x00024780 File Offset: 0x00022980
		public void StartPeriodicRescheduleTask()
		{
			object reschedulingStartStopControlLock = this._reschedulingStartStopControlLock;
			lock (reschedulingStartStopControlLock)
			{
				if (!this._isDisposed)
				{
					if (!this.IsPeriodicRescheduleTaskRunning)
					{
						this._isPeriodicReschedulingEnabled = new CancellationTokenSource();
						RescheduleDiscoveryJobsTask.Log.InfoFormat("Starting periodic discovery jobs rescheduling for engine {0}", this._engineId);
						Task.Run(delegate()
						{
							this.PeriodicReschedule(this._isPeriodicReschedulingEnabled);
						}).ContinueWith(new Action<Task>(this.LogTaskUnhandledException));
					}
					else
					{
						RescheduleDiscoveryJobsTask.Log.WarnFormat("Periodic discovery jobs rescheduling is already running for engine {0}", this._engineId);
					}
				}
			}
		}

		// Token: 0x06000612 RID: 1554 RVA: 0x00024834 File Offset: 0x00022A34
		public void StopPeriodicRescheduleTask()
		{
			object reschedulingStartStopControlLock = this._reschedulingStartStopControlLock;
			lock (reschedulingStartStopControlLock)
			{
				CancellationTokenSource isPeriodicReschedulingEnabled = this._isPeriodicReschedulingEnabled;
				if (isPeriodicReschedulingEnabled != null)
				{
					isPeriodicReschedulingEnabled.Cancel();
				}
				this._isPeriodicReschedulingEnabled = null;
			}
		}

		// Token: 0x06000613 RID: 1555 RVA: 0x00024888 File Offset: 0x00022A88
		private void PeriodicReschedule(CancellationTokenSource isPeriodicReschedulingEnabled)
		{
			while (!isPeriodicReschedulingEnabled.IsCancellationRequested)
			{
				object reschedulingAttemptLock = this._reschedulingAttemptLock;
				lock (reschedulingAttemptLock)
				{
					if (this.TryRescheduleDiscoveryJobsTask())
					{
						bool keepRunning = this._keepRunning;
						RescheduleDiscoveryJobsTask.Log.DebugFormat("Periodic discovery jobs rescheduling  for engine {0} successful. Keep running: {1}", this._engineId, keepRunning);
						if (!keepRunning)
						{
							isPeriodicReschedulingEnabled.Cancel();
							break;
						}
					}
					else
					{
						RescheduleDiscoveryJobsTask.Log.DebugFormat("Periodic discovery jobs rescheduling for engine {0} failed - next attempt in {1}.", this._engineId, this._periodicRetryInterval);
					}
				}
				Task.Delay(this._periodicRetryInterval, isPeriodicReschedulingEnabled.Token).ContinueWith(new Action<Task>(this.LogTaskUnhandledException)).Wait();
			}
			RescheduleDiscoveryJobsTask.Log.DebugFormat("Periodic discovery jobs rescheduling stopped for engine {0}", this._engineId);
		}

		// Token: 0x06000614 RID: 1556 RVA: 0x00024974 File Offset: 0x00022B74
		public void QueueRescheduleAttempt()
		{
			object reschedulingStartStopControlLock = this._reschedulingStartStopControlLock;
			bool flag = false;
			try
			{
				Monitor.Enter(reschedulingStartStopControlLock, ref flag);
				if (!this._isDisposed)
				{
					DateTime invocationTime = DateTime.UtcNow;
					RescheduleDiscoveryJobsTask.Log.DebugFormat("Invoking manual discovery jobs rescheduling for engine {0}", this._engineId);
					Task.Run(delegate()
					{
						this.ManualRescheduleAttempt(invocationTime);
					}).ContinueWith(new Action<Task>(this.LogTaskUnhandledException));
				}
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(reschedulingStartStopControlLock);
				}
			}
		}

		// Token: 0x06000615 RID: 1557 RVA: 0x00024A10 File Offset: 0x00022C10
		private void ManualRescheduleAttempt(DateTime invocationTime)
		{
			object reschedulingAttemptLock = this._reschedulingAttemptLock;
			lock (reschedulingAttemptLock)
			{
				if (invocationTime > this._lastSuccess)
				{
					if (!this.TryRescheduleDiscoveryJobsTask() && !this.IsPeriodicRescheduleTaskRunning)
					{
						this.StartPeriodicRescheduleTask();
					}
				}
				else
				{
					RescheduleDiscoveryJobsTask.Log.DebugFormat("Manual discovery jobs rescheduling skipped. Last success is newer than invocation time", Array.Empty<object>());
				}
			}
		}

		// Token: 0x06000616 RID: 1558 RVA: 0x00024A84 File Offset: 0x00022C84
		private bool TryRescheduleDiscoveryJobsTask()
		{
			if (this._isDisposed)
			{
				return false;
			}
			try
			{
				if (this._updateDiscoveryJobsDelegate(this._engineId))
				{
					this._lastSuccess = DateTime.UtcNow;
					RescheduleDiscoveryJobsTask.Log.DebugFormat("Discovery jobs rescheduling finished for engine {0}", this._engineId);
					return true;
				}
				RescheduleDiscoveryJobsTask.Log.DebugFormat("Discovery jobs rescheduling failed for engine {0}", this._engineId);
			}
			catch (Exception ex)
			{
				RescheduleDiscoveryJobsTask.Log.Error(string.Format("RescheduleDiscoveryJobsTask.TryRescheduleDiscoveryJobsTask failed for engine {0}", this._engineId), ex);
			}
			return false;
		}

		// Token: 0x06000617 RID: 1559 RVA: 0x00024B2C File Offset: 0x00022D2C
		private void LogTaskUnhandledException(Task task)
		{
			if (task.IsFaulted)
			{
				RescheduleDiscoveryJobsTask.Log.Error("Task faulted with unhandled exception", task.Exception);
			}
		}

		// Token: 0x06000618 RID: 1560 RVA: 0x00024B4B File Offset: 0x00022D4B
		public void Dispose()
		{
			this._isDisposed = true;
			this.StopPeriodicRescheduleTask();
		}

		// Token: 0x040001E4 RID: 484
		private static readonly Log Log = new Log();

		// Token: 0x040001E5 RID: 485
		private readonly Func<int, bool> _updateDiscoveryJobsDelegate;

		// Token: 0x040001E6 RID: 486
		private readonly int _engineId;

		// Token: 0x040001E7 RID: 487
		private readonly bool _keepRunning;

		// Token: 0x040001E8 RID: 488
		private readonly TimeSpan _periodicRetryInterval;

		// Token: 0x040001E9 RID: 489
		private CancellationTokenSource _isPeriodicReschedulingEnabled;

		// Token: 0x040001EA RID: 490
		private readonly object _reschedulingAttemptLock = new object();

		// Token: 0x040001EB RID: 491
		private readonly object _reschedulingStartStopControlLock = new object();

		// Token: 0x040001EC RID: 492
		private DateTime _lastSuccess = DateTime.MinValue;

		// Token: 0x040001ED RID: 493
		private volatile bool _isDisposed;
	}
}
