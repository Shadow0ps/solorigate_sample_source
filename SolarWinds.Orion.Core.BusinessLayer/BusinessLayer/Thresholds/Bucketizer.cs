using System;
using System.Linq;
using SolarWinds.Orion.Core.Common.Models.Thresholds;
using SolarWinds.Orion.Core.Common.Thresholds;

namespace SolarWinds.Orion.Core.BusinessLayer.Thresholds
{
	// Token: 0x02000050 RID: 80
	internal class Bucketizer
	{
		// Token: 0x060004E0 RID: 1248 RVA: 0x0001EA9C File Offset: 0x0001CC9C
		public Bucket[] CreateBuckets(int bucketsCount, ThresholdMinMaxValue minMax)
		{
			if (bucketsCount == 0 || minMax.Max < minMax.Min)
			{
				return new Bucket[0];
			}
			Bucket[] array = new Bucket[bucketsCount];
			double num = (minMax.Max - minMax.Min) / (double)bucketsCount;
			double num2 = ThresholdsHelper.RoundThresholdsValue(minMax.Min);
			for (int i = 0; i < bucketsCount; i++)
			{
				double minValue = num2;
				num2 = ThresholdsHelper.RoundThresholdsValue(num2 + num);
				array[i] = new Bucket(minValue, num2);
			}
			array[array.Length - 1].MaxValue = minMax.Max;
			double referalMinValue = ThresholdsHelper.RoundThresholdsValue(minMax.Min);
			if (array.All((Bucket bucket) => bucket.MinValue.Equals(referalMinValue)))
			{
				return new Bucket[]
				{
					array[array.Length - 1]
				};
			}
			return array;
		}
	}
}
