using System;
using System.Collections.Generic;
using System.Linq;
using SolarWinds.Data.Utility;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000016 RID: 22
	internal class SwisUriParser : ISwisUriParser
	{
		// Token: 0x06000293 RID: 659 RVA: 0x000100A0 File Offset: 0x0000E2A0
		public string GetEntityId(string uriStr)
		{
			SwisUri swisUri = SwisUri.Parse(uriStr);
			List<SwisUriFilter> list = new List<SwisUriFilter>
			{
				swisUri.Filter
			};
			for (SwisUriNavigation navigation = swisUri.Navigation; navigation != null; navigation = navigation.Navigation)
			{
				list.Add(navigation.Filter);
			}
			if (list.Last<SwisUriFilter>().Values.Count > 1)
			{
				throw new InvalidOperationException("GetEntityId does not support multiple key entities");
			}
			return list.SelectMany((SwisUriFilter uriFilter) => uriFilter.Values).Last<SwisUriFilterValue>().Value;
		}
	}
}
