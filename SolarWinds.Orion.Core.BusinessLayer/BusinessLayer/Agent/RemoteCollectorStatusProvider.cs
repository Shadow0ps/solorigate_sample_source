using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SolarWinds.AgentManagement.Contract;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Internals;
using SolarWinds.Orion.Core.Common.Swis;

namespace SolarWinds.Orion.Core.BusinessLayer.Agent
{
	// Token: 0x020000BF RID: 191
	internal class RemoteCollectorStatusProvider : IRemoteCollectorAgentStatusProvider
	{
		// Token: 0x06000940 RID: 2368 RVA: 0x000426B0 File Offset: 0x000408B0
		public RemoteCollectorStatusProvider(ISwisConnectionProxyCreator swisProxyCreator, int masterEngineId, int cacheExpiration) : this(cacheExpiration, () => RemoteCollectorStatusProvider.GetCurrentStatuses(swisProxyCreator, masterEngineId), () => DateTime.UtcNow)
		{
			if (swisProxyCreator == null)
			{
				throw new ArgumentNullException("swisProxyCreator");
			}
		}

		// Token: 0x06000941 RID: 2369 RVA: 0x00042716 File Offset: 0x00040916
		internal RemoteCollectorStatusProvider(int cacheExpiration, Func<IDictionary<int, AgentStatus>> refreshFunc, Func<DateTime> currentTimeFunc)
		{
			if (currentTimeFunc == null)
			{
				throw new ArgumentNullException("currentTimeFunc");
			}
			this._statusCache = new CacheWithExpiration<IDictionary<int, AgentStatus>>(cacheExpiration, refreshFunc, currentTimeFunc);
		}

		// Token: 0x06000942 RID: 2370 RVA: 0x0004273C File Offset: 0x0004093C
		public AgentStatus GetStatus(int engineId)
		{
			AgentStatus result;
			if (!this._statusCache.Get().TryGetValue(engineId, out result))
			{
				return 0;
			}
			return result;
		}

		// Token: 0x06000943 RID: 2371 RVA: 0x00042761 File Offset: 0x00040961
		public void InvalidateCache()
		{
			this._statusCache.Invalidate();
		}

		// Token: 0x06000944 RID: 2372 RVA: 0x00042770 File Offset: 0x00040970
		private static IDictionary<int, AgentStatus> GetCurrentStatuses(ISwisConnectionProxyCreator swisProxyCreator, int masterEngineId)
		{
			return RemoteCollectorStatusProvider.GetStatuses(swisProxyCreator, masterEngineId).ToDictionary((KeyValuePair<int, AgentStatus> i) => i.Key, (KeyValuePair<int, AgentStatus> i) => i.Value);
		}

		// Token: 0x06000945 RID: 2373 RVA: 0x000427C7 File Offset: 0x000409C7
		internal static IEnumerable<KeyValuePair<int, AgentStatus>> GetStatuses(ISwisConnectionProxyCreator swisProxyCreator, int masterEngineId)
		{
			using (IInformationServiceProxy2 proxy = swisProxyCreator.Create())
			{
				DataTable dataTable = proxy.Query("SELECT e.EngineID, a.AgentStatus FROM Orion.EngineProperties (nolock=true) p\r\nINNER JOIN Orion.AgentManagement.Agent (nolock=true) a\r\nON p.PropertyName='AgentId' AND a.AgentId=p.PropertyValue\r\nINNER JOIN Orion.Engines (nolock=true) e\r\nON e.EngineID=p.EngineID AND e.MasterEngineID=@MasterEngineId", new Dictionary<string, object>
				{
					{
						"MasterEngineId",
						masterEngineId
					}
				});
				foreach (object obj in dataTable.Rows)
				{
					DataRow dataRow = (DataRow)obj;
					yield return new KeyValuePair<int, AgentStatus>((int)dataRow[0], (AgentStatus)dataRow[1]);
				}
				IEnumerator enumerator = null;
			}
			IInformationServiceProxy2 proxy = null;
			yield break;
			yield break;
		}

		// Token: 0x040002B2 RID: 690
		private readonly CacheWithExpiration<IDictionary<int, AgentStatus>> _statusCache;
	}
}
