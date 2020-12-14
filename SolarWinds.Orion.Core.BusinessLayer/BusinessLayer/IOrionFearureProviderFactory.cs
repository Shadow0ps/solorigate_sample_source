using System;
using System.Collections.Generic;
using SolarWinds.Orion.Core.Models.OrionFeature;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000013 RID: 19
	public interface IOrionFearureProviderFactory
	{
		// Token: 0x0600028A RID: 650
		IEnumerable<IOrionFeatureProvider> GetProviders();
	}
}
