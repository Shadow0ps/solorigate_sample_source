using System;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer.TraceRoute
{
	// Token: 0x02000048 RID: 72
	public interface ITraceRouteProvider
	{
		// Token: 0x060004AB RID: 1195
		TracerouteResult TraceRoute(string destinationHostNameOrIpAddress);

		// Token: 0x060004AC RID: 1196
		TracerouteResult TraceRoute(string destinationHostNameOrIpAddress, long maxTimeoutInMilliseconds);
	}
}
