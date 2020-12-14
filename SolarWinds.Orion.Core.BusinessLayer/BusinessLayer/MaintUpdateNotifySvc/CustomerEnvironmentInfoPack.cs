using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace SolarWinds.Orion.Core.BusinessLayer.MaintUpdateNotifySvc
{
	// Token: 0x0200005B RID: 91
	[GeneratedCode("System.Xml", "4.8.3761.0")]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "http://www.solarwinds.com/contracts/IMaintUpdateNotifySvc/2009/09")]
	[Serializable]
	public class CustomerEnvironmentInfoPack : INotifyPropertyChanged
	{
		// Token: 0x170000C7 RID: 199
		// (get) Token: 0x0600052F RID: 1327 RVA: 0x00021328 File Offset: 0x0001F528
		// (set) Token: 0x06000530 RID: 1328 RVA: 0x00021330 File Offset: 0x0001F530
		[XmlArray(Order = 0)]
		[XmlArrayItem("Module")]
		public ModuleInfo[] Modules
		{
			get
			{
				return this.modulesField;
			}
			set
			{
				this.modulesField = value;
				this.RaisePropertyChanged("Modules");
			}
		}

		// Token: 0x170000C8 RID: 200
		// (get) Token: 0x06000531 RID: 1329 RVA: 0x00021344 File Offset: 0x0001F544
		// (set) Token: 0x06000532 RID: 1330 RVA: 0x0002134C File Offset: 0x0001F54C
		[XmlAttribute]
		public string OSVersion
		{
			get
			{
				return this.oSVersionField;
			}
			set
			{
				this.oSVersionField = value;
				this.RaisePropertyChanged("OSVersion");
			}
		}

		// Token: 0x170000C9 RID: 201
		// (get) Token: 0x06000533 RID: 1331 RVA: 0x00021360 File Offset: 0x0001F560
		// (set) Token: 0x06000534 RID: 1332 RVA: 0x00021368 File Offset: 0x0001F568
		[XmlAttribute]
		public string OrionDBVersion
		{
			get
			{
				return this.orionDBVersionField;
			}
			set
			{
				this.orionDBVersionField = value;
				this.RaisePropertyChanged("OrionDBVersion");
			}
		}

		// Token: 0x170000CA RID: 202
		// (get) Token: 0x06000535 RID: 1333 RVA: 0x0002137C File Offset: 0x0001F57C
		// (set) Token: 0x06000536 RID: 1334 RVA: 0x00021384 File Offset: 0x0001F584
		[XmlAttribute]
		public string SQLVersion
		{
			get
			{
				return this.sQLVersionField;
			}
			set
			{
				this.sQLVersionField = value;
				this.RaisePropertyChanged("SQLVersion");
			}
		}

		// Token: 0x170000CB RID: 203
		// (get) Token: 0x06000537 RID: 1335 RVA: 0x00021398 File Offset: 0x0001F598
		// (set) Token: 0x06000538 RID: 1336 RVA: 0x000213A0 File Offset: 0x0001F5A0
		[XmlAttribute]
		public Guid CustomerUniqueId
		{
			get
			{
				return this.customerUniqueIdField;
			}
			set
			{
				this.customerUniqueIdField = value;
				this.RaisePropertyChanged("CustomerUniqueId");
			}
		}

		// Token: 0x170000CC RID: 204
		// (get) Token: 0x06000539 RID: 1337 RVA: 0x000213B4 File Offset: 0x0001F5B4
		// (set) Token: 0x0600053A RID: 1338 RVA: 0x000213BC File Offset: 0x0001F5BC
		[XmlAttribute]
		public DateTime LastUpdateCheck
		{
			get
			{
				return this.lastUpdateCheckField;
			}
			set
			{
				this.lastUpdateCheckField = value;
				this.RaisePropertyChanged("LastUpdateCheck");
			}
		}

		// Token: 0x14000003 RID: 3
		// (add) Token: 0x0600053B RID: 1339 RVA: 0x000213D0 File Offset: 0x0001F5D0
		// (remove) Token: 0x0600053C RID: 1340 RVA: 0x00021408 File Offset: 0x0001F608
		public event PropertyChangedEventHandler PropertyChanged;

		// Token: 0x0600053D RID: 1341 RVA: 0x00021440 File Offset: 0x0001F640
		protected void RaisePropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
			if (propertyChanged != null)
			{
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		// Token: 0x04000172 RID: 370
		private ModuleInfo[] modulesField;

		// Token: 0x04000173 RID: 371
		private string oSVersionField;

		// Token: 0x04000174 RID: 372
		private string orionDBVersionField;

		// Token: 0x04000175 RID: 373
		private string sQLVersionField;

		// Token: 0x04000176 RID: 374
		private Guid customerUniqueIdField;

		// Token: 0x04000177 RID: 375
		private DateTime lastUpdateCheckField;
	}
}
