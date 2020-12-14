using System;
using System.Collections.Generic;
using System.Linq;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.Models.OrionFeature;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000015 RID: 21
	internal class OrionFeatureResolver
	{
		// Token: 0x0600028E RID: 654 RVA: 0x0000FFCC File Offset: 0x0000E1CC
		public OrionFeatureResolver(IOrionFeaturesDAL dal, IOrionFearureProviderFactory providerFactory)
		{
			if (dal == null)
			{
				throw new ArgumentNullException("dal");
			}
			if (providerFactory == null)
			{
				throw new ArgumentNullException("providerFactory");
			}
			this.dal = dal;
			this.providerFactory = providerFactory;
		}

		// Token: 0x0600028F RID: 655 RVA: 0x0000FFFE File Offset: 0x0000E1FE
		public IEnumerable<IOrionFeatureProvider> GetProviders()
		{
			return this.providerFactory.GetProviders();
		}

		// Token: 0x06000290 RID: 656 RVA: 0x0001000C File Offset: 0x0000E20C
		public void Resolve()
		{
			using (OrionFeatureResolver.log.Block())
			{
				this.dal.Update(this.GetProviders().SelectMany((IOrionFeatureProvider n) => n.GetFeatures()));
			}
		}

		// Token: 0x06000291 RID: 657 RVA: 0x00010078 File Offset: 0x0000E278
		internal void Resolve(string providerName)
		{
			if (string.IsNullOrEmpty(providerName))
			{
				throw new ArgumentNullException("providerName");
			}
			this.Resolve();
		}

		// Token: 0x04000069 RID: 105
		private static readonly Log log = new Log();

		// Token: 0x0400006A RID: 106
		private readonly IOrionFeaturesDAL dal;

		// Token: 0x0400006B RID: 107
		private readonly IOrionFearureProviderFactory providerFactory;
	}
}
