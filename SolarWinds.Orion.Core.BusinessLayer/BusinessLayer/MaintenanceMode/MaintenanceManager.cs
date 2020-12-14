using System;
using System.Collections.Generic;
using System.Data;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.InformationService;
using SolarWinds.Orion.Core.Models.MaintenanceMode;

namespace SolarWinds.Orion.Core.BusinessLayer.MaintenanceMode
{
	// Token: 0x02000066 RID: 102
	public class MaintenanceManager : IMaintenanceManager
	{
		// Token: 0x06000588 RID: 1416 RVA: 0x000219ED File Offset: 0x0001FBED
		public MaintenanceManager(IInformationServiceProxyCreator swisProxy, IMaintenanceModePlanDAL maintenancePlanDAL)
		{
			this.swisProxy = swisProxy;
			this.maintenancePlanDAL = maintenancePlanDAL;
		}

		// Token: 0x06000589 RID: 1417 RVA: 0x00021A04 File Offset: 0x0001FC04
		public void Unmanage(MaintenancePlanAssignment assignment)
		{
			MaintenancePlan maintenancePlan = this.maintenancePlanDAL.Get(assignment.MaintenancePlanID);
			if (maintenancePlan == null)
			{
				throw new Exception(string.Format("No maintenance plan found for PlanID={0}.", assignment.MaintenancePlanID));
			}
			string netObjectPrefix = this.GetNetObjectPrefix(assignment.EntityType);
			if (netObjectPrefix == null)
			{
				throw new Exception(string.Format("Cannot find net object prefix for EntityType='{0}'.", assignment.EntityType));
			}
			object obj = this.CreateNetObjectId(netObjectPrefix, assignment.EntityID);
			if (obj == null)
			{
				throw new Exception(string.Format("Cannot create net object id from prefix '{0}' and id '{1}'.", netObjectPrefix, assignment.EntityID));
			}
			using (IInformationServiceProxy2 informationServiceProxy = this.swisProxy.Create())
			{
				informationServiceProxy.Invoke<object>(assignment.EntityType, "Unmanage", new object[]
				{
					obj,
					maintenancePlan.UnmanageDate,
					maintenancePlan.RemanageDate,
					false
				});
			}
		}

		// Token: 0x0600058A RID: 1418 RVA: 0x00021B00 File Offset: 0x0001FD00
		public void Remanage(MaintenancePlanAssignment assignment)
		{
			string netObjectPrefix = this.GetNetObjectPrefix(assignment.EntityType);
			if (netObjectPrefix == null)
			{
				throw new Exception(string.Format("Cannot find net object prefix for EntityType='{0}'.", assignment.EntityType));
			}
			object obj = this.CreateNetObjectId(netObjectPrefix, assignment.EntityID);
			if (obj == null)
			{
				throw new Exception(string.Format("Cannot create net object id from prefix '{0}' and id '{1}'.", netObjectPrefix, assignment.EntityID));
			}
			using (IInformationServiceProxy2 informationServiceProxy = this.swisProxy.Create())
			{
				informationServiceProxy.Invoke<object>(assignment.EntityType, "Remanage", new object[]
				{
					obj
				});
			}
		}

		// Token: 0x0600058B RID: 1419 RVA: 0x00021BA4 File Offset: 0x0001FDA4
		internal object CreateNetObjectId(string prefix, int id)
		{
			if (string.IsNullOrEmpty(prefix))
			{
				return null;
			}
			return string.Join(":", new object[]
			{
				prefix,
				id
			});
		}

		// Token: 0x0600058C RID: 1420 RVA: 0x00021BD0 File Offset: 0x0001FDD0
		internal string GetNetObjectPrefix(string entityName)
		{
			if (string.IsNullOrEmpty(entityName))
			{
				return null;
			}
			string result;
			using (IInformationServiceProxy2 informationServiceProxy = this.swisProxy.Create())
			{
				DataTable dataTable = informationServiceProxy.Query("SELECT Prefix FROM Orion.NetObjectTypes WHERE EntityType = @entityName", new Dictionary<string, object>
				{
					{
						"entityName",
						entityName
					}
				});
				if (dataTable != null && dataTable.Rows != null && dataTable.Rows.Count == 1 && dataTable.Rows[0][0] != null)
				{
					result = dataTable.Rows[0][0].ToString();
				}
				else
				{
					result = null;
				}
			}
			return result;
		}

		// Token: 0x04000198 RID: 408
		private const string UnmanageVerbName = "Unmanage";

		// Token: 0x04000199 RID: 409
		private const string RemanageVerbName = "Remanage";

		// Token: 0x0400019A RID: 410
		private readonly IInformationServiceProxyCreator swisProxy;

		// Token: 0x0400019B RID: 411
		private readonly IMaintenanceModePlanDAL maintenancePlanDAL;
	}
}
