using System;
using Amib.Threading;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common.Extensions;
using SolarWinds.Orion.Core.Common.i18n;
using SolarWinds.Orion.Core.Strings;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000035 RID: 53
	public class QueuedTaskScheduler<TTask> : IDisposable where TTask : class
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x060003BA RID: 954 RVA: 0x0001884C File Offset: 0x00016A4C
		// (remove) Token: 0x060003BB RID: 955 RVA: 0x00018884 File Offset: 0x00016A84
		public event EventHandler TaskProcessingFinished;

		// Token: 0x17000065 RID: 101
		// (get) Token: 0x060003BC RID: 956 RVA: 0x000188B9 File Offset: 0x00016AB9
		public bool IsRunning
		{
			get
			{
				return this.isRunning;
			}
		}

		// Token: 0x17000066 RID: 102
		// (get) Token: 0x060003BD RID: 957 RVA: 0x000188C3 File Offset: 0x00016AC3
		public int QueueSize
		{
			get
			{
				return this.processingThreadPool.WaitingCallbacks + this.processingGroup.WaitingCallbacks;
			}
		}

		// Token: 0x060003BE RID: 958 RVA: 0x000188DC File Offset: 0x00016ADC
		public QueuedTaskScheduler(QueuedTaskScheduler<TTask>.TaskProcessingRoutine routine, int paralleltasksCount)
		{
			this.isRunning = false;
			this.processingRoutine = routine;
			this.processingStartInfo = new STPStartInfo
			{
				MaxWorkerThreads = paralleltasksCount,
				MinWorkerThreads = 0,
				StartSuspended = true
			};
			this.processingThreadPool = new SmartThreadPool(this.processingStartInfo);
			this.processingGroup = this.processingThreadPool.CreateWorkItemsGroup(paralleltasksCount);
			this.processingGroup.OnIdle += new WorkItemsGroupIdleHandler(this.processingGroup_OnIdle);
		}

		// Token: 0x060003BF RID: 959 RVA: 0x00018959 File Offset: 0x00016B59
		public void EnqueueTask(TTask task)
		{
			this.processingGroup.QueueWorkItem(new WorkItemCallback(this.ThreadPoolCallBack), task);
		}

		// Token: 0x060003C0 RID: 960 RVA: 0x0001897C File Offset: 0x00016B7C
		public void Start()
		{
			if (this.IsRunning)
			{
				throw new InvalidOperationException(Resources.LIBCODE_JM0_30);
			}
			if (this.QueueSize > 0)
			{
				this.isRunning = true;
				this.processingGroup.Start();
				this.processingThreadPool.Start();
				QueuedTaskScheduler<TTask>.log.InfoFormat("Queued tasks processing started: QueuedTasksCount = {0}, ParallelTasksCount = {1}", this.QueueSize, this.processingGroup.Concurrency);
				return;
			}
			this.isRunning = true;
			QueuedTaskScheduler<TTask>.log.InfoFormat("Queued tasks processing started: Queue is empty", Array.Empty<object>());
			if (this.TaskProcessingFinished != null)
			{
				this.TaskProcessingFinished(this, new EventArgs());
			}
			this.isRunning = false;
		}

		// Token: 0x060003C1 RID: 961 RVA: 0x00018A30 File Offset: 0x00016C30
		private void processingGroup_OnIdle(IWorkItemsGroup workItemsGroup)
		{
			if (this.isRunning)
			{
				this.isRunning = false;
				this.processingGroup.Suspend();
				this.processingThreadPool.Suspend();
				if (this.TaskProcessingFinished != null)
				{
					this.TaskProcessingFinished(this, new EventArgs());
				}
			}
		}

		// Token: 0x060003C2 RID: 962 RVA: 0x00018A7F File Offset: 0x00016C7F
		public void Cancel()
		{
			this.processingGroup.Cancel(false);
			QueuedTaskScheduler<TTask>.log.InfoFormat("Task processing recieved cancel signal, there are {0} active threads", this.processingThreadPool.ActiveThreads);
			this.processingThreadPool.WaitForIdle();
		}

		// Token: 0x060003C3 RID: 963 RVA: 0x00018AB8 File Offset: 0x00016CB8
		private object ThreadPoolCallBack(object state)
		{
			TTask ttask = state as TTask;
			if (ttask != null)
			{
				try
				{
					if (!SmartThreadPool.IsWorkItemCanceled)
					{
						using (LocaleThreadState.EnsurePrimaryLocale())
						{
							this.processingRoutine(ttask);
						}
					}
				}
				catch (Exception ex)
				{
					QueuedTaskScheduler<TTask>.log.Error("Unhandled exception in queued task processing:", ex);
				}
			}
			return null;
		}

		// Token: 0x17000067 RID: 103
		// (get) Token: 0x060003C4 RID: 964 RVA: 0x00018B30 File Offset: 0x00016D30
		public bool IsTaskCanceled
		{
			get
			{
				return SmartThreadPool.IsWorkItemCanceled;
			}
		}

		// Token: 0x060003C5 RID: 965 RVA: 0x00018B38 File Offset: 0x00016D38
		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
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

		// Token: 0x060003C6 RID: 966 RVA: 0x00018B8C File Offset: 0x00016D8C
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x060003C7 RID: 967 RVA: 0x00018B9C File Offset: 0x00016D9C
		~QueuedTaskScheduler()
		{
			this.Dispose(false);
		}

		// Token: 0x040000C9 RID: 201
		private static readonly Log log = new Log("QueuedTaskScheduler");

		// Token: 0x040000CB RID: 203
		private SmartThreadPool processingThreadPool;

		// Token: 0x040000CC RID: 204
		private IWorkItemsGroup processingGroup;

		// Token: 0x040000CD RID: 205
		private STPStartInfo processingStartInfo;

		// Token: 0x040000CE RID: 206
		private QueuedTaskScheduler<TTask>.TaskProcessingRoutine processingRoutine;

		// Token: 0x040000CF RID: 207
		private volatile bool isRunning;

		// Token: 0x040000D0 RID: 208
		private bool disposed;

		// Token: 0x02000140 RID: 320
		// (Invoke) Token: 0x06000B17 RID: 2839
		public delegate void TaskProcessingRoutine(TTask task);
	}
}
