using System;
using System.Collections.Generic;
using System.Threading;
using Amib.Threading;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.i18n;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000044 RID: 68
	public class UpdateTaskScheduler<TTaskKey, TCallbackArg> : IDisposable where TTaskKey : IComparable
	{
		// Token: 0x17000080 RID: 128
		// (get) Token: 0x06000434 RID: 1076 RVA: 0x0001C6E8 File Offset: 0x0001A8E8
		public bool ChangesActive
		{
			get
			{
				object obj = this.syncLock;
				bool result;
				lock (obj)
				{
					result = (this.ongoingChangesCounter > 0);
				}
				return result;
			}
		}

		// Token: 0x06000435 RID: 1077 RVA: 0x0001C730 File Offset: 0x0001A930
		public UpdateTaskScheduler(Action<TTaskKey> taskRoutine, int maxRunningTasks, TimeSpan postponeTaskDelay, TimeSpan mandatorySchedulerDelay)
		{
			this.taskRoutine = taskRoutine;
			this.postponeTaskDelay = postponeTaskDelay;
			this.mandatorySchedulerDelay = mandatorySchedulerDelay;
			this.ongoingChangesCounter = 0;
			this.ongoingTasks = new HashSet<UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask>(UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask.IdentityComparer.Instance);
			this.scheduledTasks = new PriorityQueue<UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask>(UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask.SchedulingComparer.Instance);
			this.schedulerThread = new Thread(new ThreadStart(this.SchedulerRoutine));
			this.schedulerThread.IsBackground = true;
			this.schedulerThread.Start();
			this.processingThreadPool = new SmartThreadPool(new STPStartInfo
			{
				MaxWorkerThreads = maxRunningTasks,
				MinWorkerThreads = 0,
				StartSuspended = false
			});
			this.processingGroup = this.processingThreadPool.CreateWorkItemsGroup(maxRunningTasks);
		}

		// Token: 0x06000436 RID: 1078 RVA: 0x0001C7FC File Offset: 0x0001A9FC
		public void BeginChanges()
		{
			object obj = this.syncLock;
			lock (obj)
			{
				this.ongoingChangesCounter++;
				if (this.log.IsDebugEnabled)
				{
					this.log.DebugFormat("BeginChanges {0}", this.ongoingChangesCounter);
				}
			}
		}

		// Token: 0x06000437 RID: 1079 RVA: 0x0001C86C File Offset: 0x0001AA6C
		public void EndChanges()
		{
			object obj = this.syncLock;
			lock (obj)
			{
				if (this.ongoingChangesCounter <= 0)
				{
					throw new InvalidOperationException("Unable to find matching BeginChanges call");
				}
				this.ongoingChangesCounter--;
				if (this.log.IsDebugEnabled)
				{
					this.log.DebugFormat("EndChanges {0}", this.ongoingChangesCounter);
				}
				if (this.ongoingChangesCounter == 0)
				{
					this.log.Debug("EndChanges - waking up scheduler");
					Monitor.PulseAll(this.syncLock);
				}
			}
		}

		// Token: 0x06000438 RID: 1080 RVA: 0x0001C914 File Offset: 0x0001AB14
		public void RequestUpdateAsync(TTaskKey taskKey, UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTaskCallback callback, TimeSpan waitForChangesDelay)
		{
			UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask scheduledTask = new UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask();
			scheduledTask.TaskKey = taskKey;
			scheduledTask.PlannedExecution = DateTime.UtcNow.Add(waitForChangesDelay);
			object callbacks;
			if (callback == null)
			{
				callbacks = null;
			}
			else
			{
				(callbacks = new List<UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTaskCallback>()).Add(callback);
			}
			scheduledTask.Callbacks = callbacks;
			UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask scheduledTask2 = scheduledTask;
			if (this.log.IsDebugEnabled)
			{
				this.log.DebugFormat("RequestUpdate for Task {0} - Enter", taskKey);
			}
			object obj = this.syncLock;
			lock (obj)
			{
				UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask scheduledTask3 = null;
				if (this.scheduledTasks.TryFind(scheduledTask2, UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask.IdentityComparer.Instance, out scheduledTask3))
				{
					if (scheduledTask2.PlannedExecution < scheduledTask3.PlannedExecution)
					{
						if (scheduledTask3.Callbacks != null)
						{
							if (scheduledTask2.Callbacks != null)
							{
								scheduledTask2.Callbacks.AddRange(scheduledTask3.Callbacks);
							}
							else
							{
								scheduledTask2.Callbacks = scheduledTask3.Callbacks;
							}
						}
						this.scheduledTasks.Remove(scheduledTask3, UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask.IdentityComparer.Instance);
						this.scheduledTasks.Enqueue(scheduledTask2);
						if (this.log.IsInfoEnabled)
						{
							this.log.InfoFormat("Task {0} was rescheduled from {1} to {2}", scheduledTask2, scheduledTask3.PlannedExecution, scheduledTask2.PlannedExecution);
						}
						Monitor.PulseAll(this.syncLock);
					}
					else
					{
						if (scheduledTask2.Callbacks != null)
						{
							if (scheduledTask3.Callbacks != null)
							{
								scheduledTask3.Callbacks.AddRange(scheduledTask2.Callbacks);
							}
							else
							{
								scheduledTask3.Callbacks = scheduledTask2.Callbacks;
							}
						}
						if (this.log.IsInfoEnabled)
						{
							this.log.InfoFormat("Task {0} has been scheduled already at {1}, requested time {2}", scheduledTask2, scheduledTask3.PlannedExecution, scheduledTask2.PlannedExecution);
						}
					}
				}
				else
				{
					this.scheduledTasks.Enqueue(scheduledTask2);
					if (this.log.IsInfoEnabled)
					{
						this.log.InfoFormat("Task {0} has been scheduled for {1}", scheduledTask2, scheduledTask2.PlannedExecution);
					}
					Monitor.PulseAll(this.syncLock);
				}
			}
			if (this.log.IsDebugEnabled)
			{
				this.log.DebugFormat("RequestUpdate for Task {0} - Leave", taskKey);
			}
		}

		// Token: 0x06000439 RID: 1081 RVA: 0x0001CB4C File Offset: 0x0001AD4C
		private void SchedulerRoutine()
		{
			this.log.Debug("Scheduler: scheduling thread started");
			for (;;)
			{
				UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask scheduledTask = null;
				object obj = this.syncLock;
				lock (obj)
				{
					if (this.ChangesActive)
					{
						this.log.Debug("Suspending Scheduler: ongoing changes detected");
						Monitor.Wait(this.syncLock);
					}
					else if (this.scheduledTasks.Count == 0)
					{
						this.log.Debug("Suspending Scheduler: no pending tasks to process");
						Monitor.Wait(this.syncLock);
					}
					else
					{
						DateTime utcNow = DateTime.UtcNow;
						scheduledTask = this.scheduledTasks.Peek();
						if (scheduledTask.PlannedExecution > utcNow)
						{
							TimeSpan timeSpan = scheduledTask.PlannedExecution.Subtract(utcNow);
							if (this.log.IsDebugEnabled)
							{
								this.log.DebugFormat("Suspending Scheduler: woke up too early, next task {0} is scheduled for {1}, will be suspended for {2}", scheduledTask, scheduledTask.PlannedExecution, timeSpan);
							}
							scheduledTask = null;
							Monitor.Wait(this.syncLock, timeSpan);
						}
						else if (this.ongoingTasks.Contains(scheduledTask))
						{
							this.scheduledTasks.Remove(scheduledTask, UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask.IdentityComparer.Instance);
							scheduledTask.PlannedExecution = utcNow.Add(this.postponeTaskDelay);
							this.scheduledTasks.Enqueue(scheduledTask);
							if (this.log.IsDebugEnabled)
							{
								this.log.DebugFormat("Scheduler: task {0} is being executed, rescheduling its next execution to {1}", scheduledTask, scheduledTask.PlannedExecution);
							}
							scheduledTask = null;
						}
						else
						{
							scheduledTask = this.scheduledTasks.Dequeue();
							this.ongoingTasks.Add(scheduledTask);
							if (this.log.IsDebugEnabled)
							{
								this.log.DebugFormat("Scheduler: Task {0} is planed to get executed now", scheduledTask);
							}
						}
					}
				}
				if (scheduledTask != null)
				{
					this.processingGroup.QueueWorkItem(new WorkItemCallback(this.ThreadPoolRoutine), scheduledTask);
					if (this.log.IsDebugEnabled)
					{
						this.log.DebugFormat("Scheduler: Task {0} was executed", scheduledTask);
					}
				}
				Thread.Sleep(this.mandatorySchedulerDelay);
			}
		}

		// Token: 0x0600043A RID: 1082 RVA: 0x0001CD5C File Offset: 0x0001AF5C
		private object ThreadPoolRoutine(object state)
		{
			UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask scheduledTask = state as UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask;
			if (scheduledTask == null)
			{
				throw new ArgumentException("Unexpected state object or null", "state");
			}
			try
			{
				bool taskExecuted = false;
				Exception ex = null;
				using (LocaleThreadState.EnsurePrimaryLocale())
				{
					if (!SmartThreadPool.IsWorkItemCanceled)
					{
						try
						{
							taskExecuted = true;
							this.taskRoutine(scheduledTask.TaskKey);
						}
						catch (Exception ex2)
						{
							ex = ex2;
							this.log.Error(string.Format("Task {0} cought unhandled exception from task routine", scheduledTask), ex2);
						}
					}
					if (scheduledTask.Callbacks != null)
					{
						foreach (UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTaskCallback scheduledTaskCallback in scheduledTask.Callbacks)
						{
							try
							{
								scheduledTaskCallback.Callback(new UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTaskCallbackEventArgs(scheduledTask.TaskKey, scheduledTaskCallback.State, ex, taskExecuted));
							}
							catch (Exception ex3)
							{
								this.log.Error(string.Format("Task {0} callback failed", scheduledTask), ex3);
							}
						}
					}
				}
			}
			catch (Exception ex4)
			{
				this.log.Error(string.Format("Task {0} cought unhandled exception during task processing", scheduledTask), ex4);
			}
			finally
			{
				object obj = this.syncLock;
				lock (obj)
				{
					this.ongoingTasks.Remove(scheduledTask);
				}
			}
			return null;
		}

		// Token: 0x0600043B RID: 1083 RVA: 0x0001CEF4 File Offset: 0x0001B0F4
		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					if (this.schedulerThread != null)
					{
						this.schedulerThread.Abort();
						this.schedulerThread = null;
					}
					if (this.processingGroup != null)
					{
						this.processingGroup.Cancel(false);
						this.processingGroup = null;
					}
					if (this.processingThreadPool != null)
					{
						this.processingThreadPool.Dispose();
						this.processingThreadPool = null;
					}
				}
				this.disposed = true;
			}
		}

		// Token: 0x0600043C RID: 1084 RVA: 0x0001CF62 File Offset: 0x0001B162
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x0600043D RID: 1085 RVA: 0x0001CF74 File Offset: 0x0001B174
		~UpdateTaskScheduler()
		{
			this.Dispose(false);
		}

		// Token: 0x04000100 RID: 256
		private Log log = new Log();

		// Token: 0x04000101 RID: 257
		private object syncLock = new object();

		// Token: 0x04000102 RID: 258
		private TimeSpan postponeTaskDelay;

		// Token: 0x04000103 RID: 259
		private TimeSpan mandatorySchedulerDelay;

		// Token: 0x04000104 RID: 260
		private Action<TTaskKey> taskRoutine;

		// Token: 0x04000105 RID: 261
		private Thread schedulerThread;

		// Token: 0x04000106 RID: 262
		private SmartThreadPool processingThreadPool;

		// Token: 0x04000107 RID: 263
		private IWorkItemsGroup processingGroup;

		// Token: 0x04000108 RID: 264
		private int ongoingChangesCounter;

		// Token: 0x04000109 RID: 265
		private PriorityQueue<UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask> scheduledTasks;

		// Token: 0x0400010A RID: 266
		private HashSet<UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask> ongoingTasks;

		// Token: 0x0400010B RID: 267
		private bool disposed;

		// Token: 0x0200014E RID: 334
		public class ScheduledTaskCallbackEventArgs : EventArgs
		{
			// Token: 0x1700013D RID: 317
			// (get) Token: 0x06000B51 RID: 2897 RVA: 0x00048E6E File Offset: 0x0004706E
			// (set) Token: 0x06000B52 RID: 2898 RVA: 0x00048E76 File Offset: 0x00047076
			public TTaskKey TaskKey { get; private set; }

			// Token: 0x1700013E RID: 318
			// (get) Token: 0x06000B53 RID: 2899 RVA: 0x00048E7F File Offset: 0x0004707F
			// (set) Token: 0x06000B54 RID: 2900 RVA: 0x00048E87 File Offset: 0x00047087
			public TCallbackArg State { get; private set; }

			// Token: 0x1700013F RID: 319
			// (get) Token: 0x06000B55 RID: 2901 RVA: 0x00048E90 File Offset: 0x00047090
			// (set) Token: 0x06000B56 RID: 2902 RVA: 0x00048E98 File Offset: 0x00047098
			public Exception TaskException { get; private set; }

			// Token: 0x17000140 RID: 320
			// (get) Token: 0x06000B57 RID: 2903 RVA: 0x00048EA1 File Offset: 0x000470A1
			// (set) Token: 0x06000B58 RID: 2904 RVA: 0x00048EA9 File Offset: 0x000470A9
			public bool TaskExecuted { get; private set; }

			// Token: 0x17000141 RID: 321
			// (get) Token: 0x06000B59 RID: 2905 RVA: 0x00048EB2 File Offset: 0x000470B2
			public bool TaskFailed
			{
				get
				{
					return this.TaskException != null;
				}
			}

			// Token: 0x06000B5A RID: 2906 RVA: 0x00048EBD File Offset: 0x000470BD
			internal ScheduledTaskCallbackEventArgs(TTaskKey taskKey, TCallbackArg state, Exception ex, bool taskExecuted)
			{
				this.TaskKey = taskKey;
				this.State = state;
				this.TaskException = ex;
				this.TaskExecuted = taskExecuted;
			}
		}

		// Token: 0x0200014F RID: 335
		public class ScheduledTaskCallback
		{
			// Token: 0x17000142 RID: 322
			// (get) Token: 0x06000B5B RID: 2907 RVA: 0x00048EE2 File Offset: 0x000470E2
			// (set) Token: 0x06000B5C RID: 2908 RVA: 0x00048EEA File Offset: 0x000470EA
			public Action<UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTaskCallbackEventArgs> Callback { get; private set; }

			// Token: 0x17000143 RID: 323
			// (get) Token: 0x06000B5D RID: 2909 RVA: 0x00048EF3 File Offset: 0x000470F3
			// (set) Token: 0x06000B5E RID: 2910 RVA: 0x00048EFB File Offset: 0x000470FB
			public TCallbackArg State { get; private set; }

			// Token: 0x06000B5F RID: 2911 RVA: 0x00048F04 File Offset: 0x00047104
			public ScheduledTaskCallback(Action<UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTaskCallbackEventArgs> callback, TCallbackArg state)
			{
				this.Callback = callback;
				this.State = state;
			}
		}

		// Token: 0x02000150 RID: 336
		private class ScheduledTask
		{
			// Token: 0x06000B60 RID: 2912 RVA: 0x00048F1A File Offset: 0x0004711A
			public override string ToString()
			{
				return this.TaskKey.ToString();
			}

			// Token: 0x0400044A RID: 1098
			public TTaskKey TaskKey;

			// Token: 0x0400044B RID: 1099
			public List<UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTaskCallback> Callbacks;

			// Token: 0x0400044C RID: 1100
			public DateTime PlannedExecution;

			// Token: 0x020001CB RID: 459
			public class IdentityComparer : Comparer<UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask>, IEqualityComparer<UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask>
			{
				// Token: 0x06000CFC RID: 3324 RVA: 0x0004B622 File Offset: 0x00049822
				public override int Compare(UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask x, UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask y)
				{
					return x.TaskKey.CompareTo(y.TaskKey);
				}

				// Token: 0x06000CFD RID: 3325 RVA: 0x0004B640 File Offset: 0x00049840
				public bool Equals(UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask x, UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask y)
				{
					return x.TaskKey.CompareTo(y.TaskKey) == 0;
				}

				// Token: 0x06000CFE RID: 3326 RVA: 0x0004B661 File Offset: 0x00049861
				public int GetHashCode(UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask obj)
				{
					return obj.TaskKey.GetHashCode();
				}

				// Token: 0x040005CE RID: 1486
				public static readonly UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask.IdentityComparer Instance = new UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask.IdentityComparer();
			}

			// Token: 0x020001CC RID: 460
			public class SchedulingComparer : Comparer<UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask>
			{
				// Token: 0x06000D01 RID: 3329 RVA: 0x0004B688 File Offset: 0x00049888
				public override int Compare(UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask x, UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask y)
				{
					return DateTime.Compare(y.PlannedExecution, x.PlannedExecution);
				}

				// Token: 0x040005CF RID: 1487
				public static readonly UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask.SchedulingComparer Instance = new UpdateTaskScheduler<TTaskKey, TCallbackArg>.ScheduledTask.SchedulingComparer();
			}
		}
	}
}
