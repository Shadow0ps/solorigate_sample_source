using System;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Common.Models.Thresholds;

namespace SolarWinds.Orion.Core.BusinessLayer.Thresholds
{
	// Token: 0x0200004E RID: 78
	internal class HistogramCalculator
	{
		// Token: 0x060004D6 RID: 1238 RVA: 0x0001E840 File Offset: 0x0001CA40
		public StatisticalDataHistogram[] CreateHistogram(StatisticalData[] data, TimeFrame[] timeFrames, int intervalsCount)
		{
			ThresholdMinMaxValue minMax = new MinMaxCalculator().Calculate(data);
			return HistogramCalculator.CreateBucketsAndHistogram(data, timeFrames, intervalsCount, minMax);
		}

		// Token: 0x060004D7 RID: 1239 RVA: 0x0001E864 File Offset: 0x0001CA64
		public StatisticalDataHistogram[] CreateHistogramWithScaledInterval(StatisticalData[] data, TimeFrame[] timeFrames, int intervalsCount, Type dataType)
		{
			ThresholdMinMaxValue thresholdMinMaxValue = new MinMaxCalculator().Calculate(data);
			if (dataType != null && dataType == typeof(int))
			{
				int num = (int)(thresholdMinMaxValue.Max - thresholdMinMaxValue.Min);
				if (num == 0)
				{
					num = 1;
				}
				intervalsCount = Math.Min(num, intervalsCount);
			}
			return HistogramCalculator.CreateBucketsAndHistogram(data, timeFrames, intervalsCount, thresholdMinMaxValue);
		}

		// Token: 0x060004D8 RID: 1240 RVA: 0x0001E8C0 File Offset: 0x0001CAC0
		private static StatisticalDataHistogram[] CreateBucketsAndHistogram(StatisticalData[] data, TimeFrame[] timeFrames, int intervalsCount, ThresholdMinMaxValue minMax)
		{
			Bucket[] array = new Bucketizer().CreateBuckets(intervalsCount, minMax);
			StatisticalDataHistogram[] histograms = HistogramCalculator.CreateHistogramForTimeFrames(timeFrames, array.Length);
			histograms = HistogramCalculator.CreateHistogramsPointsFromBuckets(histograms, array);
			return HistogramCalculator.CalculatePointsFrequencyFromStatistics(data, histograms);
		}

		// Token: 0x060004D9 RID: 1241 RVA: 0x0001E8F8 File Offset: 0x0001CAF8
		private static StatisticalDataHistogram[] CalculatePointsFrequencyFromStatistics(StatisticalData[] values, StatisticalDataHistogram[] histograms)
		{
			foreach (StatisticalData statisticalData in values)
			{
				for (int j = 0; j < histograms.Length; j++)
				{
					if (histograms[j].TimeFrame.IsInTimeFrame(statisticalData.Date))
					{
						HistogramCalculator.IncrementPointFrequency(histograms[j], statisticalData.Value);
					}
				}
			}
			return histograms;
		}

		// Token: 0x060004DA RID: 1242 RVA: 0x0001E94C File Offset: 0x0001CB4C
		private static void IncrementPointFrequency(StatisticalDataHistogram histograms, double value)
		{
			for (int i = 0; i < histograms.DataPoints.Length; i++)
			{
				if (histograms.DataPoints[i].EndValue >= value)
				{
					HistogramDataPoint histogramDataPoint = histograms.DataPoints[i];
					uint frequency = histogramDataPoint.Frequency;
					histogramDataPoint.Frequency = frequency + 1U;
					return;
				}
			}
		}

		// Token: 0x060004DB RID: 1243 RVA: 0x0001E994 File Offset: 0x0001CB94
		private static StatisticalDataHistogram[] CreateHistogramsPointsFromBuckets(StatisticalDataHistogram[] histograms, Bucket[] buckets)
		{
			for (int i = 0; i < buckets.Length; i++)
			{
				for (int j = 0; j < histograms.Length; j++)
				{
					histograms[j].DataPoints[i] = new HistogramDataPoint(buckets[i].MinValue, buckets[i].MaxValue);
				}
			}
			return histograms;
		}

		// Token: 0x060004DC RID: 1244 RVA: 0x0001E9E0 File Offset: 0x0001CBE0
		private static StatisticalDataHistogram[] CreateHistogramForTimeFrames(TimeFrame[] timeFrames, int bucketsCount)
		{
			StatisticalDataHistogram[] array = new StatisticalDataHistogram[timeFrames.Length];
			for (int i = 0; i < timeFrames.Length; i++)
			{
				array[i] = new StatisticalDataHistogram(timeFrames[i], bucketsCount);
			}
			return array;
		}
	}
}
