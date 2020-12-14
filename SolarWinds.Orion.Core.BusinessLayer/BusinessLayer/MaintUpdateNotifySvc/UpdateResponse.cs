using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace SolarWinds.Orion.Core.BusinessLayer.MaintUpdateNotifySvc
{
	// Token: 0x02000061 RID: 97
	[GeneratedCode("System.Xml", "4.8.3761.0")]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "http://www.solarwinds.com/contracts/IMaintUpdateNotifySvc/2009/09")]
	[Serializable]
	public class UpdateResponse : INotifyPropertyChanged
	{
		// Token: 0x170000DE RID: 222
		// (get) Token: 0x06000571 RID: 1393 RVA: 0x00021890 File Offset: 0x0001FA90
		// (set) Token: 0x06000572 RID: 1394 RVA: 0x00021898 File Offset: 0x0001FA98
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

		// Token: 0x170000DF RID: 223
		// (get) Token: 0x06000573 RID: 1395 RVA: 0x000218AC File Offset: 0x0001FAAC
		// (set) Token: 0x06000574 RID: 1396 RVA: 0x000218B4 File Offset: 0x0001FAB4
		[XmlElement(Order = 1)]
		public bool Success
		{
			get
			{
				return this.successField;
			}
			set
			{
				this.successField = value;
				this.RaisePropertyChanged("Success");
			}
		}

		// Token: 0x170000E0 RID: 224
		// (get) Token: 0x06000575 RID: 1397 RVA: 0x000218C8 File Offset: 0x0001FAC8
		// (set) Token: 0x06000576 RID: 1398 RVA: 0x000218D0 File Offset: 0x0001FAD0
		[XmlElement(Order = 2)]
		public VersionManifest Manifest
		{
			get
			{
				return this.manifestField;
			}
			set
			{
				this.manifestField = value;
				this.RaisePropertyChanged("Manifest");
			}
		}

		// Token: 0x14000008 RID: 8
		// (add) Token: 0x06000577 RID: 1399 RVA: 0x000218E4 File Offset: 0x0001FAE4
		// (remove) Token: 0x06000578 RID: 1400 RVA: 0x0002191C File Offset: 0x0001FB1C
		public event PropertyChangedEventHandler PropertyChanged;

		// Token: 0x06000579 RID: 1401 RVA: 0x00021954 File Offset: 0x0001FB54
		protected void RaisePropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
			if (propertyChanged != null)
			{
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		// Token: 0x04000192 RID: 402
		private string contractVersionField;

		// Token: 0x04000193 RID: 403
		private bool successField;

		// Token: 0x04000194 RID: 404
		private VersionManifest manifestField;
	}
}
