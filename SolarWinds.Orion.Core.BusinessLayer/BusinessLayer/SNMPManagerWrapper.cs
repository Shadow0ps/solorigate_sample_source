using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using SolarWinds.Net.SNMP;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200003D RID: 61
	public class SNMPManagerWrapper
	{
		// Token: 0x06000405 RID: 1029 RVA: 0x0001BAE0 File Offset: 0x00019CE0
		public SNMPManagerWrapper()
		{
			this.bgworker.DoWork += this.bgworker_DoWork;
		}

		// Token: 0x06000406 RID: 1030 RVA: 0x0001BB34 File Offset: 0x00019D34
		private void bgworker_DoWork(object sender, DoWorkEventArgs e)
		{
			while (this._doWork)
			{
				bool flag = false;
				Queue<SNMPRequest> query = this._Query;
				lock (query)
				{
					if (this._Query.Count > 0)
					{
						if (this._manager.OutstandingQueries <= 5)
						{
							SNMPRequest snR = this._Query.Dequeue();
							int num = 0;
							string empty = string.Empty;
							this.BeginQuery(snR, true, out num, out empty);
						}
						else
						{
							flag = true;
						}
					}
					else
					{
						flag = true;
					}
				}
				if (flag)
				{
					Thread.Sleep(100);
				}
			}
		}

		// Token: 0x06000407 RID: 1031 RVA: 0x0001BBCC File Offset: 0x00019DCC
		public bool BeginQuery(SNMPRequest snR, bool used, out int err, out string ErrDes)
		{
			Queue<SNMPRequest> query = this._Query;
			bool result;
			lock (query)
			{
				if (this._manager.OutstandingQueries > 5)
				{
					this._Query.Enqueue(snR);
					if (!this.bgworker.IsBusy)
					{
						this.bgworker.RunWorkerAsync();
					}
					err = 0;
					ErrDes = string.Empty;
					result = true;
				}
				else
				{
					result = this._manager.BeginQuery(snR, used, ref err, ref ErrDes);
				}
			}
			return result;
		}

		// Token: 0x17000076 RID: 118
		// (get) Token: 0x06000408 RID: 1032 RVA: 0x0001BC5C File Offset: 0x00019E5C
		public int OutstandingQueries
		{
			get
			{
				Queue<SNMPRequest> query = this._Query;
				int result;
				lock (query)
				{
					result = this._manager.OutstandingQueries + this._Query.Count;
				}
				return result;
			}
		}

		// Token: 0x17000077 RID: 119
		// (get) Token: 0x06000409 RID: 1033 RVA: 0x0001BCB0 File Offset: 0x00019EB0
		public SNMPRequest DefaultInfo
		{
			get
			{
				return this._manager.DefaultInfo;
			}
		}

		// Token: 0x0600040A RID: 1034 RVA: 0x0001BCBD File Offset: 0x00019EBD
		public SNMPResponse Query(SNMPRequest snR, bool usDI)
		{
			return this._manager.Query(snR, usDI);
		}

		// Token: 0x0600040B RID: 1035 RVA: 0x0001BCCC File Offset: 0x00019ECC
		public void Cancel()
		{
			this._manager.Cancel();
			this._doWork = false;
		}

		// Token: 0x0600040C RID: 1036 RVA: 0x0001BCE0 File Offset: 0x00019EE0
		public void Dispose()
		{
			this._manager.Dispose();
			this._doWork = false;
		}

		// Token: 0x040000E9 RID: 233
		private SNMPManager _manager = new SNMPManager();

		// Token: 0x040000EA RID: 234
		private const int _maxCount = 5;

		// Token: 0x040000EB RID: 235
		private Queue<SNMPRequest> _Query = new Queue<SNMPRequest>();

		// Token: 0x040000EC RID: 236
		private BackgroundWorker bgworker = new BackgroundWorker();

		// Token: 0x040000ED RID: 237
		private bool _doWork = true;
	}
}
