using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace SolarWinds.Orion.Core.BusinessLayer.MaintUpdateNotifySvc
{
	// Token: 0x0200005D RID: 93
	[GeneratedCode("System.Xml", "4.8.3761.0")]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "http://www.solarwinds.com/contracts/IMaintUpdateNotifySvc/2009/09")]
	[Serializable]
	public class UpdateMessage : INotifyPropertyChanged
	{
		// Token: 0x170000D2 RID: 210
		// (get) Token: 0x0600054D RID: 1357 RVA: 0x00021584 File Offset: 0x0001F784
		// (set) Token: 0x0600054E RID: 1358 RVA: 0x0002158C File Offset: 0x0001F78C
		[XmlElement(Order = 0)]
		public DateTime PublishDate
		{
			get
			{
				return this.publishDateField;
			}
			set
			{
				this.publishDateField = value;
				this.RaisePropertyChanged("PublishDate");
			}
		}

		// Token: 0x170000D3 RID: 211
		// (get) Token: 0x0600054F RID: 1359 RVA: 0x000215A0 File Offset: 0x0001F7A0
		// (set) Token: 0x06000550 RID: 1360 RVA: 0x000215A8 File Offset: 0x0001F7A8
		[XmlElement(Order = 1)]
		public string MaintenanceMessage
		{
			get
			{
				return this.maintenanceMessageField;
			}
			set
			{
				this.maintenanceMessageField = value;
				this.RaisePropertyChanged("MaintenanceMessage");
			}
		}

		// Token: 0x14000005 RID: 5
		// (add) Token: 0x06000551 RID: 1361 RVA: 0x000215BC File Offset: 0x0001F7BC
		// (remove) Token: 0x06000552 RID: 1362 RVA: 0x000215F4 File Offset: 0x0001F7F4
		public event PropertyChangedEventHandler PropertyChanged;

		// Token: 0x06000553 RID: 1363 RVA: 0x0002162C File Offset: 0x0001F82C
		protected void RaisePropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
			if (propertyChanged != null)
			{
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		// Token: 0x0400017F RID: 383
		private DateTime publishDateField;

		// Token: 0x04000180 RID: 384
		private string maintenanceMessageField;
	}
}
