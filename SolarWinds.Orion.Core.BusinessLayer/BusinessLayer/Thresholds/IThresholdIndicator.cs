using System;
using SolarWinds.Orion.Core.Common.Models.Thresholds;

namespace SolarWinds.Orion.Core.BusinessLayer.Thresholds
{
	// Token: 0x02000052 RID: 82
	public interface IThresholdIndicator
	{
		// Token: 0x060004E7 RID: 1255
		void LoadPreviousThresholdData(int instanceId, string thresholdName);

		// Token: 0x060004E8 RID: 1256
		void ReportThresholdIndication(Threshold threshold);
	}
}
