using System;
using System.Collections.Generic;
using SolarWinds.Orion.Core.Models.OrionFeature;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000084 RID: 132
	internal interface IOrionFeaturesDAL
	{
		// Token: 0x06000680 RID: 1664
		void Update(IEnumerable<OrionFeature> features);

		// Token: 0x06000681 RID: 1665
		IEnumerable<OrionFeature> GetItems();
	}
}
