using System;
using System.Net;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common.Configuration;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000046 RID: 70
	internal class WebRequestHelper
	{
		// Token: 0x06000440 RID: 1088 RVA: 0x0001D020 File Offset: 0x0001B220
		internal static HttpWebResponse SendHttpWebRequest(string query)
		{
			HttpWebRequest httpWebRequest = WebRequest.Create(query) as HttpWebRequest;
			httpWebRequest.Proxy = HttpProxySettings.Instance.AsWebProxy();
			httpWebRequest.Method = "GET";
			try
			{
				HttpWebResponse httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse;
				if (httpWebResponse.StatusCode == HttpStatusCode.OK)
				{
					return httpWebResponse;
				}
			}
			catch (Exception ex)
			{
				WebRequestHelper._log.ErrorFormat("Caught exception while trying to make http-request: {0}", ex);
			}
			return null;
		}

		// Token: 0x0400010C RID: 268
		private static readonly Log _log = new Log();
	}
}
