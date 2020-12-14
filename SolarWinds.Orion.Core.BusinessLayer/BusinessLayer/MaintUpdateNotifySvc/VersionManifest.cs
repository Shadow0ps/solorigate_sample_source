using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace SolarWinds.Orion.Core.BusinessLayer.MaintUpdateNotifySvc
{
	// Token: 0x02000060 RID: 96
	[GeneratedCode("System.Xml", "4.8.3761.0")]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "http://www.solarwinds.com/contracts/IMaintUpdateNotifySvc/2009/09")]
	[Serializable]
	public class VersionManifest : INotifyPropertyChanged
	{
		// Token: 0x170000DD RID: 221
		// (get) Token: 0x0600056B RID: 1387 RVA: 0x000217E0 File Offset: 0x0001F9E0
		// (set) Token: 0x0600056C RID: 1388 RVA: 0x000217E8 File Offset: 0x0001F9E8
		[XmlArray(Order = 0)]
		public VersionInfo[] CurrentVersions
		{
			get
			{
				return this.currentVersionsField;
			}
			set
			{
				this.currentVersionsField = value;
				this.RaisePropertyChanged("CurrentVersions");
			}
		}

		// Token: 0x14000007 RID: 7
		// (add) Token: 0x0600056D RID: 1389 RVA: 0x000217FC File Offset: 0x0001F9FC
		// (remove) Token: 0x0600056E RID: 1390 RVA: 0x00021834 File Offset: 0x0001FA34
		public event PropertyChangedEventHandler PropertyChanged;

		// Token: 0x0600056F RID: 1391 RVA: 0x0002186C File Offset: 0x0001FA6C
		protected void RaisePropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
			if (propertyChanged != null)
			{
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		// Token: 0x04000190 RID: 400
		private VersionInfo[] currentVersionsField;
	}
}
