using System;
using SolarWinds.Settings;

namespace SolarWinds.Orion.Core.BusinessLayer.ConfigurationSettings
{
	// Token: 0x02000064 RID: 100
	internal class WindowsServiceSettings : SettingsBase
	{
		// Token: 0x06000584 RID: 1412 RVA: 0x0001D0A8 File Offset: 0x0001B2A8
		private WindowsServiceSettings()
		{
		}

		// Token: 0x04000196 RID: 406
		public static readonly WindowsServiceSettings Instance = new WindowsServiceSettings();

		// Token: 0x04000197 RID: 407
		[Setting(Default = 20000, AllowServerOverride = true, ServiceRestartDependencies = new string[]
		{
			"OrionModuleEngine"
		})]
		public int ServiceTimeout;
	}
}
