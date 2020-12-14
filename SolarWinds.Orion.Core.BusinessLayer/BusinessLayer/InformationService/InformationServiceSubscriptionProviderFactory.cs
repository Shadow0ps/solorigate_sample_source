using System;
using SolarWinds.Orion.Core.Common.Interfaces;

namespace SolarWinds.Orion.Core.BusinessLayer.InformationService
{
	// Token: 0x02000057 RID: 87
	public class InformationServiceSubscriptionProviderFactory
	{
		// Token: 0x0600051E RID: 1310 RVA: 0x000211C7 File Offset: 0x0001F3C7
		public static IInformationServiceSubscriptionProvider GetInformationServiceSubscriptionProviderFactory(string netObjectOperationEndpoint)
		{
			return new InformationServiceSubscriptionProvider(netObjectOperationEndpoint);
		}

		// Token: 0x0600051F RID: 1311 RVA: 0x000211CF File Offset: 0x0001F3CF
		public static IInformationServiceSubscriptionProvider GetInformationServiceSubscriptionProviderFactoryV3(string netObjectOperationEndpoint)
		{
			return InformationServiceSubscriptionProvider.CreateV3(netObjectOperationEndpoint);
		}
	}
}
