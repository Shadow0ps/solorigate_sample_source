using System;
using SolarWinds.Orion.Core.Common;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000039 RID: 57
	internal static class Settings
	{
		// Token: 0x17000069 RID: 105
		// (get) Token: 0x060003EE RID: 1006 RVA: 0x0001B7E8 File Offset: 0x000199E8
		[Obsolete("Core-Split cleanup. If you need this member please contact Core team", true)]
		internal static TimeSpan DiscoverESXNodesTimer
		{
			get
			{
				return TimeSpan.FromMinutes(5.0);
			}
		}

		// Token: 0x1700006A RID: 106
		// (get) Token: 0x060003EF RID: 1007 RVA: 0x0001B7F8 File Offset: 0x000199F8
		[Obsolete("Core-Split cleanup. If you need this member please contact Core team", true)]
		internal static TimeSpan UpdateESXNotificationsTimer
		{
			get
			{
				return TimeSpan.FromMinutes(2.0);
			}
		}

		// Token: 0x1700006B RID: 107
		// (get) Token: 0x060003F0 RID: 1008 RVA: 0x0001B808 File Offset: 0x00019A08
		[Obsolete("Core-Split cleanup. If you need this member please contact Core team", true)]
		internal static TimeSpan VMwareESXJobTimeout
		{
			get
			{
				return TimeSpan.FromMinutes(10.0);
			}
		}

		// Token: 0x1700006C RID: 108
		// (get) Token: 0x060003F1 RID: 1009 RVA: 0x0001B818 File Offset: 0x00019A18
		internal static TimeSpan CheckMaintenanceRenewalsTimer
		{
			get
			{
				return TimeSpan.FromDays(7.0);
			}
		}

		// Token: 0x1700006D RID: 109
		// (get) Token: 0x060003F2 RID: 1010 RVA: 0x0001B818 File Offset: 0x00019A18
		internal static TimeSpan CheckOrionProductTeamBlogTimer
		{
			get
			{
				return TimeSpan.FromDays(7.0);
			}
		}

		// Token: 0x1700006E RID: 110
		// (get) Token: 0x060003F3 RID: 1011 RVA: 0x0001B828 File Offset: 0x00019A28
		internal static bool IsProductsBlogDisabled
		{
			get
			{
				return SettingsDAL.Get("ProductsBlog-Disable").Equals("1");
			}
		}

		// Token: 0x1700006F RID: 111
		// (get) Token: 0x060003F4 RID: 1012 RVA: 0x0001B843 File Offset: 0x00019A43
		internal static bool IsMaintenanceRenewalsDisabled
		{
			get
			{
				return SettingsDAL.Get("MaintenanceRenewals-Disable").Equals("1");
			}
		}

		// Token: 0x17000070 RID: 112
		// (get) Token: 0x060003F5 RID: 1013 RVA: 0x0001B85E File Offset: 0x00019A5E
		internal static bool IsLicenseSaturationDisabled
		{
			get
			{
				return SettingsDAL.Get("LicenseSaturation-Disable").Equals("1");
			}
		}

		// Token: 0x17000071 RID: 113
		// (get) Token: 0x060003F6 RID: 1014 RVA: 0x0001B879 File Offset: 0x00019A79
		internal static bool IsAutomaticGeolocationEnabled
		{
			get
			{
				return SettingsDAL.Get("AutomaticGeolocation-Enable").Equals("1");
			}
		}

		// Token: 0x17000072 RID: 114
		// (get) Token: 0x060003F7 RID: 1015 RVA: 0x0001B894 File Offset: 0x00019A94
		internal static TimeSpan AutomaticGeolocationCheckInterval
		{
			get
			{
				string s;
				TimeSpan result;
				if (WebSettingsDAL.TryGet("AutomaticGeolocationCheckInterval", ref s) && TimeSpan.TryParse(s, out result))
				{
					return result;
				}
				return TimeSpan.FromHours(1.0);
			}
		}

		// Token: 0x17000073 RID: 115
		// (get) Token: 0x060003F8 RID: 1016 RVA: 0x0001B8CC File Offset: 0x00019ACC
		internal static int LicenseSaturationPercentage
		{
			get
			{
				int result;
				if (int.TryParse(SettingsDAL.Get("LicenseSaturation-WarningPercentage"), out result))
				{
					return result;
				}
				return 80;
			}
		}

		// Token: 0x17000074 RID: 116
		// (get) Token: 0x060003F9 RID: 1017 RVA: 0x0001B8F0 File Offset: 0x00019AF0
		internal static int PollerLimitWarningScaleFactor
		{
			get
			{
				int result;
				if (int.TryParse(SettingsDAL.Get("PollerLimitWarningScaleFactor"), out result))
				{
					return result;
				}
				return 85;
			}
		}

		// Token: 0x17000075 RID: 117
		// (get) Token: 0x060003FA RID: 1018 RVA: 0x0001B914 File Offset: 0x00019B14
		internal static int PollerLimitreachedScaleFactor
		{
			get
			{
				int result;
				if (int.TryParse(SettingsDAL.Get("PollerLimitReachedScaleFactor"), out result))
				{
					return result;
				}
				return 100;
			}
		}
	}
}
