using System;
using SolarWinds.Orion.Core.Common.Indications;

namespace SolarWinds.Orion.Core.Auditing
{
	// Token: 0x02000008 RID: 8
	public class AuditDatabaseDecoratedContainer : AuditDataContainer
	{
		// Token: 0x06000021 RID: 33 RVA: 0x00002700 File Offset: 0x00000900
		public AuditDatabaseDecoratedContainer(AuditDataContainer adc, AuditNotificationContainer anc, string message) : base(adc)
		{
			if (anc == null)
			{
				throw new ArgumentNullException("anc");
			}
			if (string.IsNullOrEmpty(message))
			{
				throw new ArgumentNullException("message");
			}
			object obj;
			if (anc.IndicationProperties != null && anc.IndicationProperties.TryGetValue(IndicationConstants.AccountId, out obj))
			{
				this.accountId = (obj as string);
			}
			else
			{
				this.accountId = "SYSTEM";
			}
			this.indicationTime = anc.GetIndicationPropertyValue<DateTime>("IndicationTime");
			if (this.indicationTime.Kind == DateTimeKind.Unspecified)
			{
				this.indicationTime = DateTime.SpecifyKind(this.indicationTime, DateTimeKind.Utc);
			}
			else
			{
				this.indicationTime = this.indicationTime.ToUniversalTime();
			}
			this.message = message;
		}

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000022 RID: 34 RVA: 0x000027B4 File Offset: 0x000009B4
		public string AccountId
		{
			get
			{
				return this.accountId;
			}
		}

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000023 RID: 35 RVA: 0x000027BC File Offset: 0x000009BC
		public DateTime IndicationTime
		{
			get
			{
				return this.indicationTime;
			}
		}

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000024 RID: 36 RVA: 0x000027C4 File Offset: 0x000009C4
		public string Message
		{
			get
			{
				return this.message;
			}
		}

		// Token: 0x0400000D RID: 13
		private string accountId;

		// Token: 0x0400000E RID: 14
		private DateTime indicationTime;

		// Token: 0x0400000F RID: 15
		private string message;
	}
}
