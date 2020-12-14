using System;
using SolarWinds.Orion.Core.Common.Models.Thresholds;

namespace SolarWinds.Orion.Core.BusinessLayer.Thresholds
{
	// Token: 0x0200004A RID: 74
	internal class BaselineProcessingInfo
	{
		// Token: 0x060004B5 RID: 1205 RVA: 0x0001DA80 File Offset: 0x0001BC80
		public BaselineProcessingInfo(Threshold threshold, BaselineValues baselineValues)
		{
			if (threshold == null)
			{
				throw new ArgumentNullException("threshold");
			}
			if (baselineValues == null)
			{
				throw new ArgumentNullException("baselineValues");
			}
			this._threshold = threshold;
			this._baselineValues = baselineValues;
		}

		// Token: 0x170000B7 RID: 183
		// (get) Token: 0x060004B6 RID: 1206 RVA: 0x0001DAB2 File Offset: 0x0001BCB2
		public Threshold Threshold
		{
			get
			{
				return this._threshold;
			}
		}

		// Token: 0x170000B8 RID: 184
		// (get) Token: 0x060004B7 RID: 1207 RVA: 0x0001DABA File Offset: 0x0001BCBA
		public BaselineValues BaselineValues
		{
			get
			{
				return this._baselineValues;
			}
		}

		// Token: 0x04000144 RID: 324
		private readonly Threshold _threshold;

		// Token: 0x04000145 RID: 325
		private readonly BaselineValues _baselineValues;
	}
}
