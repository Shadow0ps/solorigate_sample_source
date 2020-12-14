using System;
using System.CodeDom.Compiler;
using System.Xml.Serialization;

namespace SolarWinds.Orion.Core.BusinessLayer.MaintUpdateNotifySvc
{
	// Token: 0x0200005F RID: 95
	[GeneratedCode("System.Xml", "4.8.3761.0")]
	[XmlType(Namespace = "http://www.solarwinds.com/contracts/IMaintUpdateNotifySvc/2009/09")]
	[Serializable]
	public enum ModuleStatusType
	{
		// Token: 0x0400018D RID: 397
		Updated,
		// Token: 0x0400018E RID: 398
		Current,
		// Token: 0x0400018F RID: 399
		NotFound
	}
}
