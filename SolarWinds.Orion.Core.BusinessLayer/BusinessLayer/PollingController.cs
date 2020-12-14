using System;
using System.ServiceModel;
using SolarWinds.Collector.Contract;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000032 RID: 50
	internal class PollingController : IPollingControllerServiceHelper, IPollingControllerService, IDisposable
	{
		// Token: 0x060003AD RID: 941 RVA: 0x000185FA File Offset: 0x000167FA
		public static IPollingControllerServiceHelper GetInstance()
		{
			return new PollingController(new ChannelProxy<IPollingControllerService>(new ChannelFactory<IPollingControllerService>("ToCollector")));
		}

		// Token: 0x060003AE RID: 942 RVA: 0x00018610 File Offset: 0x00016810
		public PollingController(IChannelProxy<IPollingControllerService> channel)
		{
			if (channel == null)
			{
				throw new ArgumentNullException("channel");
			}
			this.channel = channel;
		}

		// Token: 0x060003AF RID: 943 RVA: 0x00018630 File Offset: 0x00016830
		public void PollNow(string entityIdentifier)
		{
			this.channel.Invoke(delegate(IPollingControllerService n)
			{
				n.PollNow(entityIdentifier);
			});
		}

		// Token: 0x060003B0 RID: 944 RVA: 0x00018664 File Offset: 0x00016864
		public void RediscoverNow(string entityIdentifier)
		{
			this.channel.Invoke(delegate(IPollingControllerService n)
			{
				n.RediscoverNow(entityIdentifier);
			});
		}

		// Token: 0x060003B1 RID: 945 RVA: 0x00018698 File Offset: 0x00016898
		public void JobNow(JobExecutionCondition condition)
		{
			this.channel.Invoke(delegate(IPollingControllerService n)
			{
				n.JobNow(condition);
			});
		}

		// Token: 0x060003B2 RID: 946 RVA: 0x000186CC File Offset: 0x000168CC
		public void CancelJob(JobExecutionCondition condition)
		{
			this.channel.Invoke(delegate(IPollingControllerService n)
			{
				n.CancelJob(condition);
			});
		}

		// Token: 0x060003B3 RID: 947 RVA: 0x00018700 File Offset: 0x00016900
		~PollingController()
		{
			this.Dispose(false);
		}

		// Token: 0x060003B4 RID: 948 RVA: 0x00018730 File Offset: 0x00016930
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x060003B5 RID: 949 RVA: 0x0001873F File Offset: 0x0001693F
		protected void Dispose(bool disposing)
		{
			if (this.channel != null)
			{
				this.channel.Dispose();
			}
		}

		// Token: 0x040000C7 RID: 199
		private static readonly Log log = new Log();

		// Token: 0x040000C8 RID: 200
		private IChannelProxy<IPollingControllerService> channel;
	}
}
