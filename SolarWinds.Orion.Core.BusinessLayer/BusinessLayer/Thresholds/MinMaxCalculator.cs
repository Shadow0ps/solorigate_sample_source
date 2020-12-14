using System;
using SolarWinds.Orion.Core.Common.Models.Thresholds;

namespace SolarWinds.Orion.Core.BusinessLayer.Thresholds
{
	// Token: 0x0200004F RID: 79
	internal class MinMaxCalculator
	{
		// Token: 0x060004DE RID: 1246 RVA: 0x0001EA14 File Offset: 0x0001CC14
		public ThresholdMinMaxValue Calculate(StatisticalData[] values)
		{
			if (values.Length == 0)
			{
				return new ThresholdMinMaxValue
				{
					Max = 0.0,
					Min = 0.0
				};
			}
			double num = values[0].Value;
			double num2 = values[0].Value;
			for (int i = 1; i < values.Length; i++)
			{
				num = Math.Min(num, values[i].Value);
				num2 = Math.Max(num2, values[i].Value);
			}
			return new ThresholdMinMaxValue
			{
				Min = num,
				Max = num2
			};
		}
	}
}
