using System;

namespace SolarWinds.Orion.Core.BusinessLayer.Engines
{
	// Token: 0x02000073 RID: 115
	public interface IEngineComponent
	{
		// Token: 0x170000E8 RID: 232
		// (get) Token: 0x060005E8 RID: 1512
		int EngineId { get; }

		// Token: 0x060005E9 RID: 1513
		EngineComponentStatus GetStatus();
	}
}
