using System;
using SolarWinds.Orion.Core.Models.MaintenanceMode;

namespace SolarWinds.Orion.Core.BusinessLayer.MaintenanceMode
{
	// Token: 0x02000065 RID: 101
	public interface IMaintenanceManager
	{
		// Token: 0x06000586 RID: 1414
		void Unmanage(MaintenancePlanAssignment assignment);

		// Token: 0x06000587 RID: 1415
		void Remanage(MaintenancePlanAssignment assignment);
	}
}
