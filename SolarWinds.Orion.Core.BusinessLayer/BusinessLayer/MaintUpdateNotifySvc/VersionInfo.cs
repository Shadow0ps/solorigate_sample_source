using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace SolarWinds.Orion.Core.BusinessLayer.MaintUpdateNotifySvc
{
	// Token: 0x0200005E RID: 94
	[GeneratedCode("System.Xml", "4.8.3761.0")]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "http://www.solarwinds.com/contracts/IMaintUpdateNotifySvc/2009/09")]
	[Serializable]
	public class VersionInfo : INotifyPropertyChanged
	{
		// Token: 0x170000D4 RID: 212
		// (get) Token: 0x06000555 RID: 1365 RVA: 0x00021650 File Offset: 0x0001F850
		// (set) Token: 0x06000556 RID: 1366 RVA: 0x00021658 File Offset: 0x0001F858
		[XmlElement(Order = 0)]
		public string ModuleName
		{
			get
			{
				return this.moduleNameField;
			}
			set
			{
				this.moduleNameField = value;
				this.RaisePropertyChanged("ModuleName");
			}
		}

		// Token: 0x170000D5 RID: 213
		// (get) Token: 0x06000557 RID: 1367 RVA: 0x0002166C File Offset: 0x0001F86C
		// (set) Token: 0x06000558 RID: 1368 RVA: 0x00021674 File Offset: 0x0001F874
		[XmlElement(Order = 1)]
		public DateTime DateReleased
		{
			get
			{
				return this.dateReleasedField;
			}
			set
			{
				this.dateReleasedField = value;
				this.RaisePropertyChanged("DateReleased");
			}
		}

		// Token: 0x170000D6 RID: 214
		// (get) Token: 0x06000559 RID: 1369 RVA: 0x00021688 File Offset: 0x0001F888
		// (set) Token: 0x0600055A RID: 1370 RVA: 0x00021690 File Offset: 0x0001F890
		[XmlElement(Order = 2)]
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

		// Token: 0x170000D7 RID: 215
		// (get) Token: 0x0600055B RID: 1371 RVA: 0x000216A4 File Offset: 0x0001F8A4
		// (set) Token: 0x0600055C RID: 1372 RVA: 0x000216AC File Offset: 0x0001F8AC
		[XmlElement(Order = 3)]
		public string Link
		{
			get
			{
				return this.linkField;
			}
			set
			{
				this.linkField = value;
				this.RaisePropertyChanged("Link");
			}
		}

		// Token: 0x170000D8 RID: 216
		// (get) Token: 0x0600055D RID: 1373 RVA: 0x000216C0 File Offset: 0x0001F8C0
		// (set) Token: 0x0600055E RID: 1374 RVA: 0x000216C8 File Offset: 0x0001F8C8
		[XmlElement(Order = 4)]
		public string ReleaseNotes
		{
			get
			{
				return this.releaseNotesField;
			}
			set
			{
				this.releaseNotesField = value;
				this.RaisePropertyChanged("ReleaseNotes");
			}
		}

		// Token: 0x170000D9 RID: 217
		// (get) Token: 0x0600055F RID: 1375 RVA: 0x000216DC File Offset: 0x0001F8DC
		// (set) Token: 0x06000560 RID: 1376 RVA: 0x000216E4 File Offset: 0x0001F8E4
		[XmlElement(Order = 5)]
		public UpdateMessage Message
		{
			get
			{
				return this.messageField;
			}
			set
			{
				this.messageField = value;
				this.RaisePropertyChanged("Message");
			}
		}

		// Token: 0x170000DA RID: 218
		// (get) Token: 0x06000561 RID: 1377 RVA: 0x000216F8 File Offset: 0x0001F8F8
		// (set) Token: 0x06000562 RID: 1378 RVA: 0x00021700 File Offset: 0x0001F900
		[XmlElement(Order = 6)]
		public ModuleStatusType ModuleStatus
		{
			get
			{
				return this.moduleStatusField;
			}
			set
			{
				this.moduleStatusField = value;
				this.RaisePropertyChanged("ModuleStatus");
			}
		}

		// Token: 0x170000DB RID: 219
		// (get) Token: 0x06000563 RID: 1379 RVA: 0x00021714 File Offset: 0x0001F914
		// (set) Token: 0x06000564 RID: 1380 RVA: 0x0002171C File Offset: 0x0001F91C
		[XmlElement(Order = 7)]
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

		// Token: 0x170000DC RID: 220
		// (get) Token: 0x06000565 RID: 1381 RVA: 0x00021730 File Offset: 0x0001F930
		// (set) Token: 0x06000566 RID: 1382 RVA: 0x00021738 File Offset: 0x0001F938
		[XmlElement(Order = 8)]
		public string Hotfix
		{
			get
			{
				return this.hotfixField;
			}
			set
			{
				this.hotfixField = value;
				this.RaisePropertyChanged("Hotfix");
			}
		}

		// Token: 0x14000006 RID: 6
		// (add) Token: 0x06000567 RID: 1383 RVA: 0x0002174C File Offset: 0x0001F94C
		// (remove) Token: 0x06000568 RID: 1384 RVA: 0x00021784 File Offset: 0x0001F984
		public event PropertyChangedEventHandler PropertyChanged;

		// Token: 0x06000569 RID: 1385 RVA: 0x000217BC File Offset: 0x0001F9BC
		protected void RaisePropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
			if (propertyChanged != null)
			{
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		// Token: 0x04000182 RID: 386
		private string moduleNameField;

		// Token: 0x04000183 RID: 387
		private DateTime dateReleasedField;

		// Token: 0x04000184 RID: 388
		private string productTagField;

		// Token: 0x04000185 RID: 389
		private string linkField;

		// Token: 0x04000186 RID: 390
		private string releaseNotesField;

		// Token: 0x04000187 RID: 391
		private UpdateMessage messageField;

		// Token: 0x04000188 RID: 392
		private ModuleStatusType moduleStatusField;

		// Token: 0x04000189 RID: 393
		private string versionField;

		// Token: 0x0400018A RID: 394
		private string hotfixField;
	}
}
