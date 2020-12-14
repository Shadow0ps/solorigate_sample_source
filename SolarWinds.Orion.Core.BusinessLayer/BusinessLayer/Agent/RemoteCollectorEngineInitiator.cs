using System;
using System.Collections.Generic;
using SolarWinds.Orion.Core.BusinessLayer.Engines;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Common.JobEngine;

namespace SolarWinds.Orion.Core.BusinessLayer.Agent
{
	// Token: 0x020000BE RID: 190
	internal class RemoteCollectorEngineInitiator : IEngineInitiator
	{
		// Token: 0x06000937 RID: 2359 RVA: 0x00042560 File Offset: 0x00040760
		public RemoteCollectorEngineInitiator(int engineId, string engineName, bool interfaceAvailable, IEngineDAL engineDal, IThrottlingStatusProvider throttlingStatusProvider, IEngineComponent engineComponent)
		{
			if (engineName == null)
			{
				throw new ArgumentNullException("engineName");
			}
			if (engineDal == null)
			{
				throw new ArgumentNullException("engineDal");
			}
			this._engineDal = engineDal;
			if (throttlingStatusProvider == null)
			{
				throw new ArgumentNullException("throttlingStatusProvider");
			}
			this._throttlingStatusProvider = throttlingStatusProvider;
			if (engineComponent == null)
			{
				throw new ArgumentNullException("engineComponent");
			}
			this._engineComponent = engineComponent;
			this.EngineId = engineId;
			this.ServerName = engineName.ToUpperInvariant();
			this._interfaceAvailable = interfaceAvailable;
		}

		// Token: 0x17000127 RID: 295
		// (get) Token: 0x06000938 RID: 2360 RVA: 0x000425E0 File Offset: 0x000407E0
		public int EngineId { get; }

		// Token: 0x17000128 RID: 296
		// (get) Token: 0x06000939 RID: 2361 RVA: 0x000425E8 File Offset: 0x000407E8
		public string ServerName { get; }

		// Token: 0x17000129 RID: 297
		// (get) Token: 0x0600093A RID: 2362 RVA: 0x000425F0 File Offset: 0x000407F0
		public EngineComponentStatus ComponentStatus
		{
			get
			{
				return this._engineComponent.GetStatus();
			}
		}

		// Token: 0x1700012A RID: 298
		// (get) Token: 0x0600093B RID: 2363 RVA: 0x000425FD File Offset: 0x000407FD
		public bool AllowKeepAlive
		{
			get
			{
				return this.ComponentStatus == EngineComponentStatus.Up;
			}
		}

		// Token: 0x1700012B RID: 299
		// (get) Token: 0x0600093C RID: 2364 RVA: 0x000425FD File Offset: 0x000407FD
		public bool AllowPollingCompletion
		{
			get
			{
				return this.ComponentStatus == EngineComponentStatus.Up;
			}
		}

		// Token: 0x0600093D RID: 2365 RVA: 0x00042608 File Offset: 0x00040808
		public void InitializeEngine()
		{
			this._engineDal.UpdateEngineInfo(this.EngineId, RemoteCollectorEngineInitiator.DefaultValues, false, this._interfaceAvailable, this.AllowKeepAlive);
		}

		// Token: 0x0600093E RID: 2366 RVA: 0x00042630 File Offset: 0x00040830
		public void UpdateInfo(bool updateJobEngineThrottleInfo)
		{
			float num = (this.AllowPollingCompletion && updateJobEngineThrottleInfo) ? this._throttlingStatusProvider.GetPollingCompletion() : 0f;
			Dictionary<string, object> values = new Dictionary<string, object>
			{
				{
					"PollingCompletion",
					num
				}
			};
			this._engineDal.UpdateEngineInfo(this.EngineId, values, true, this._interfaceAvailable, this.AllowKeepAlive);
		}

		// Token: 0x040002AA RID: 682
		private readonly bool _interfaceAvailable;

		// Token: 0x040002AB RID: 683
		private readonly IEngineDAL _engineDal;

		// Token: 0x040002AC RID: 684
		private readonly IThrottlingStatusProvider _throttlingStatusProvider;

		// Token: 0x040002AD RID: 685
		private readonly IEngineComponent _engineComponent;

		// Token: 0x040002AE RID: 686
		private const float DefaultPollingCompletion = 0f;

		// Token: 0x040002AF RID: 687
		private static readonly Dictionary<string, object> DefaultValues = new Dictionary<string, object>
		{
			{
				"EngineVersion",
				RegistrySettings.GetVersionDisplayString()
			}
		};
	}
}
