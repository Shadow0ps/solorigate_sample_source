using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace SolarWinds.Orion.Core.BusinessLayer.MaintUpdateNotifySvc
{
	// Token: 0x0200005C RID: 92
	[GeneratedCode("System.Xml", "4.8.3761.0")]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "http://www.solarwinds.com/contracts/IMaintUpdateNotifySvc/2009/09")]
	[Serializable]
	public class ModuleInfo : INotifyPropertyChanged
	{
		// Token: 0x170000CD RID: 205
		// (get) Token: 0x0600053F RID: 1343 RVA: 0x00021464 File Offset: 0x0001F664
		// (set) Token: 0x06000540 RID: 1344 RVA: 0x0002146C File Offset: 0x0001F66C
		[XmlAttribute]
		public string ProductDisplayName
		{
			get
			{
				return this.productDisplayNameField;
			}
			set
			{
				this.productDisplayNameField = value;
				this.RaisePropertyChanged("ProductDisplayName");
			}
		}

		// Token: 0x170000CE RID: 206
		// (get) Token: 0x06000541 RID: 1345 RVA: 0x00021480 File Offset: 0x0001F680
		// (set) Token: 0x06000542 RID: 1346 RVA: 0x00021488 File Offset: 0x0001F688
		[XmlAttribute]
		public string HotfixVersion
		{
			get
			{
				return this.hotfixVersionField;
			}
			set
			{
				this.hotfixVersionField = value;
				this.RaisePropertyChanged("HotfixVersion");
			}
		}

		// Token: 0x170000CF RID: 207
		// (get) Token: 0x06000543 RID: 1347 RVA: 0x0002149C File Offset: 0x0001F69C
		// (set) Token: 0x06000544 RID: 1348 RVA: 0x000214A4 File Offset: 0x0001F6A4
		[XmlAttribute]
		public string Version
		{
			get
			{
				return this.versionField;
			}
			set
			{
				this.versionField = value;
				this.RaisePropertyChanged("Version");
			}
		}

		// Token: 0x170000D0 RID: 208
		// (get) Token: 0x06000545 RID: 1349 RVA: 0x000214B8 File Offset: 0x0001F6B8
		// (set) Token: 0x06000546 RID: 1350 RVA: 0x000214C0 File Offset: 0x0001F6C0
		[XmlAttribute]
		public string ProductTag
		{
			get
			{
				return this.productTagField;
			}
			set
			{
				this.productTagField = value;
				this.RaisePropertyChanged("ProductTag");
			}
		}

		// Token: 0x170000D1 RID: 209
		// (get) Token: 0x06000547 RID: 1351 RVA: 0x000214D4 File Offset: 0x0001F6D4
		// (set) Token: 0x06000548 RID: 1352 RVA: 0x000214DC File Offset: 0x0001F6DC
		[XmlAttribute]
		public string LicenseInfo
		{
			get
			{
				return this.licenseInfoField;
			}
			set
			{
				this.licenseInfoField = value;
				this.RaisePropertyChanged("LicenseInfo");
			}
		}

		// Token: 0x14000004 RID: 4
		// (add) Token: 0x06000549 RID: 1353 RVA: 0x000214F0 File Offset: 0x0001F6F0
		// (remove) Token: 0x0600054A RID: 1354 RVA: 0x00021528 File Offset: 0x0001F728
		public event PropertyChangedEventHandler PropertyChanged;

		// Token: 0x0600054B RID: 1355 RVA: 0x00021560 File Offset: 0x0001F760
		protected void RaisePropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
			if (propertyChanged != null)
			{
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		// Token: 0x04000179 RID: 377
		private string productDisplayNameField;

		// Token: 0x0400017A RID: 378
		private string hotfixVersionField;

		// Token: 0x0400017B RID: 379
		private string versionField;

		// Token: 0x0400017C RID: 380
		private string productTagField;

		// Token: 0x0400017D RID: 381
		private string licenseInfoField;
	}
}
