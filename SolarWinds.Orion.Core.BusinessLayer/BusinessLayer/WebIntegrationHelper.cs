using System;
using SolarWinds.Orion.Core.Models.WebIntegration;
using SolarWinds.Orion.Web.Integration.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000045 RID: 69
	internal static class WebIntegrationHelper
	{
		// Token: 0x0600043E RID: 1086 RVA: 0x0001CFA4 File Offset: 0x0001B1A4
		public static SupportCase ToSupportCase(this WebSupportCase webSupportCase)
		{
			return new SupportCase
			{
				CaseNumber = webSupportCase.CaseNumber,
				CaseURL = webSupportCase.CaseURL,
				LastUpdated = webSupportCase.LastUpdated,
				Status = webSupportCase.Status,
				Title = webSupportCase.Title
			};
		}

		// Token: 0x0600043F RID: 1087 RVA: 0x0001CFF2 File Offset: 0x0001B1F2
		public static MaintenanceStatus ToMaintenanceStatus(this WebMaintenanceStatus webMaintenanceStatus)
		{
			return new MaintenanceStatus
			{
				ExpirationDate = webMaintenanceStatus.ExpirationDate,
				ProductName = webMaintenanceStatus.ProductName,
				ShortName = webMaintenanceStatus.ShortName
			};
		}
	}
}
