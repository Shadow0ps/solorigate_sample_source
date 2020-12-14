using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace SolarWinds.Orion.Core.BusinessLayer.MaintUpdateNotifySvc
{
	// Token: 0x0200005A RID: 90
	[GeneratedCode("System.Xml", "4.8.3761.0")]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "http://www.solarwinds.com/contracts/IMaintUpdateNotifySvc/2009/09")]
	[Serializable]
	public class UpdateRequest : INotifyPropertyChanged
	{
		// Token: 0x170000C5 RID: 197
		// (get) Token: 0x06000527 RID: 1319 RVA: 0x0002125C File Offset: 0x0001F45C
		// (set) Token: 0x06000528 RID: 1320 RVA: 0x00021264 File Offset: 0x0001F464
		[XmlElement(Order = 0)]
		public string ContractVersion
		{
			get
			{
				return this.contractVersionField;
			}
			set
			{
				this.contractVersionField = value;
				this.RaisePropertyChanged("ContractVersion");
			}
		}

		// Token: 0x170000C6 RID: 198
		// (get) Token: 0x06000529 RID: 1321 RVA: 0x00021278 File Offset: 0x0001F478
		// (set) Token: 0x0600052A RID: 1322 RVA: 0x00021280 File Offset: 0x0001F480
		[XmlElement(Order = 1)]
		public CustomerEnvironmentInfoPack CustomerInfo
		{
			get
			{
				return this.customerInfoField;
			}
			set
			{
				this.customerInfoField = value;
				this.RaisePropertyChanged("CustomerInfo");
			}
		}

		// Token: 0x14000002 RID: 2
		// (add) Token: 0x0600052B RID: 1323 RVA: 0x00021294 File Offset: 0x0001F494
		// (remove) Token: 0x0600052C RID: 1324 RVA: 0x000212CC File Offset: 0x0001F4CC
		public event PropertyChangedEventHandler PropertyChanged;

		// Token: 0x0600052D RID: 1325 RVA: 0x00021304 File Offset: 0x0001F504
		protected void RaisePropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
			if (propertyChanged != null)
			{
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		// Token: 0x0400016F RID: 367
		private string contractVersionField;

		// Token: 0x04000170 RID: 368
		private CustomerEnvironmentInfoPack customerInfoField;
	}
}
