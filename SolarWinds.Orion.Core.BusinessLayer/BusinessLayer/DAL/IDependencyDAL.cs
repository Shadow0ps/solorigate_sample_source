using System;
using System.Collections.Generic;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x0200009C RID: 156
	public interface IDependencyDAL
	{
		// Token: 0x0600079C RID: 1948
		IList<Dependency> GetAllDependencies();

		// Token: 0x0600079D RID: 1949
		Dependency GetDependency(int id);

		// Token: 0x0600079E RID: 1950
		void SaveDependency(Dependency depenedency);

		// Token: 0x0600079F RID: 1951
		void DeleteDependency(Dependency dependency);
	}
}
