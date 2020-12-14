using System;
using System.Collections.Generic;
using System.ServiceModel;
using SolarWinds.InformationService.Contract2;
using SolarWinds.InformationService.Contract2.PubSub;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Interfaces;

namespace SolarWinds.Orion.Core.BusinessLayer.InformationService
{
	// Token: 0x02000056 RID: 86
	internal class InformationServiceSubscriptionProvider : IInformationServiceSubscriptionProvider
	{
		// Token: 0x06000516 RID: 1302 RVA: 0x0002101C File Offset: 0x0001F21C
		private InformationServiceSubscriptionProvider(Func<string, InfoServiceProxy> proxyFactory, string netObjectOperationEndpoint)
		{
			if (!RegistrySettings.IsFullOrion())
			{
				throw new InvalidOperationException("Subscription of Indications on non primary poller.");
			}
			if (string.IsNullOrEmpty(netObjectOperationEndpoint))
			{
				throw new ArgumentException("netObjectOperationEndpoint");
			}
			this.netObjectOperationEndpoint = netObjectOperationEndpoint;
			this.swisProxy = proxyFactory("localhost");
		}

		// Token: 0x06000517 RID: 1303 RVA: 0x00021082 File Offset: 0x0001F282
		internal InformationServiceSubscriptionProvider() : this(new Func<string, InfoServiceProxy>(InformationServiceConnectionProvider.CreateProxyForCertificate), "net.tcp://localhost:17777/Orion/Core/BusinessLayer/OperationSubscriber")
		{
		}

		// Token: 0x06000518 RID: 1304 RVA: 0x0002109B File Offset: 0x0001F29B
		internal static InformationServiceSubscriptionProvider CreateV3()
		{
			return new InformationServiceSubscriptionProvider(new Func<string, InfoServiceProxy>(InformationServiceConnectionProvider.CreateProxyForCertificateV3), "net.tcp://localhost:17777/Orion/Core/BusinessLayer/OperationSubscriber");
		}

		// Token: 0x06000519 RID: 1305 RVA: 0x000210B3 File Offset: 0x0001F2B3
		public InformationServiceSubscriptionProvider(string netObjectOperationEndpoint) : this(new Func<string, InfoServiceProxy>(InformationServiceConnectionProvider.CreateProxyForCertificate), netObjectOperationEndpoint)
		{
		}

		// Token: 0x0600051A RID: 1306 RVA: 0x000210C8 File Offset: 0x0001F2C8
		public static InformationServiceSubscriptionProvider CreateV3(string netObjectOperationEndpoint)
		{
			return new InformationServiceSubscriptionProvider(new Func<string, InfoServiceProxy>(InformationServiceConnectionProvider.CreateProxyForCertificateV3), netObjectOperationEndpoint);
		}

		// Token: 0x0600051B RID: 1307 RVA: 0x000210DC File Offset: 0x0001F2DC
		public string Subscribe(string subscribeQuery, INotificationSubscriber notificationSubscriber)
		{
			if (string.IsNullOrEmpty(subscribeQuery))
			{
				throw new ArgumentException("subscribeQuery");
			}
			if (notificationSubscriber == null)
			{
				throw new ArgumentNullException("notificationSubscriber");
			}
			ServiceHost serviceHost = new ServiceHost(notificationSubscriber, Array.Empty<Uri>());
			serviceHost.AddServiceEndpoint(typeof(INotificationSubscriber), new NetTcpBinding
			{
				PortSharingEnabled = true
			}, this.netObjectOperationEndpoint);
			serviceHost.Open();
			PropertyBag propertyBag = new PropertyBag();
			propertyBag.Add("Query", subscribeQuery);
			propertyBag.Add("EndpointAddress", this.netObjectOperationEndpoint);
			PropertyBag propertyBag2 = propertyBag;
			string text = this.swisProxy.Create("System.Subscription", propertyBag2);
			this.subscriptionUriList.Add(text);
			this.log.DebugFormat("Swis subscribed with subscriptionUri: {0}", text);
			this.log.DebugFormat("Swis subscribed with query: {0}", subscribeQuery);
			return text;
		}

		// Token: 0x0600051C RID: 1308 RVA: 0x000211A1 File Offset: 0x0001F3A1
		public void Unsubscribe(string subscriptionUri)
		{
			this.swisProxy.Delete(subscriptionUri);
		}

		// Token: 0x0600051D RID: 1309 RVA: 0x000211AF File Offset: 0x0001F3AF
		public void UnsubscribeAll()
		{
			this.swisProxy.BulkDelete(this.subscriptionUriList.ToArray());
		}

		// Token: 0x04000168 RID: 360
		private readonly Log log = new Log();

		// Token: 0x04000169 RID: 361
		private readonly InfoServiceProxy swisProxy;

		// Token: 0x0400016A RID: 362
		private readonly string netObjectOperationEndpoint;

		// Token: 0x0400016B RID: 363
		private readonly List<string> subscriptionUriList = new List<string>();

		// Token: 0x0400016C RID: 364
		private const string SubscriptionEntity = "System.Subscription";
	}
}
