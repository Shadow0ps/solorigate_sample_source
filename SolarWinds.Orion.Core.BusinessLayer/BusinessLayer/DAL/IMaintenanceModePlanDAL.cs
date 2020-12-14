using System;
using SolarWinds.Orion.Core.Models.MaintenanceMode;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000081 RID: 129
	public interface IMaintenanceModePlanDAL
	{
		// Token: 0x0600066A RID: 1642
		string Create(MaintenancePlan plan);

		// Token: 0x0600066B RID: 1643
		MaintenancePlan Get(int planID);

		// Token: 0x0600066C RID: 1644
		MaintenancePlan Get(string entityUri);

		// Token: 0x0600066D RID: 1645
		void Update(string entityUri, MaintenancePlan plan);
	}
}
