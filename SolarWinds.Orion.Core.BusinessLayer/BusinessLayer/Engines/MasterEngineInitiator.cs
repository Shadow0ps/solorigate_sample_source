using System;
using System.Collections.Generic;
using System.Linq;
using SolarWinds.Licensing.Framework;
using SolarWinds.Licensing.Framework.Interfaces;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Common.JobEngine;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer.Engines
{
	// Token: 0x02000074 RID: 116
	public class MasterEngineInitiator : EngineHelper, IEngineInitiator
	{
		// Token: 0x060005EA RID: 1514 RVA: 0x0002357C File Offset: 0x0002177C
		public MasterEngineInitiator() : this(new ThrottlingStatusProvider())
		{
		}

		// Token: 0x060005EB RID: 1515 RVA: 0x0002358C File Offset: 0x0002178C
		public MasterEngineInitiator(IThrottlingStatusProvider throttlingStatusProvider)
		{
			if (throttlingStatusProvider == null)
			{
				throw new ArgumentNullException("throttlingStatusProvider");
			}
			this._throttlingStatusProvider = throttlingStatusProvider;
		}

		// Token: 0x060005EC RID: 1516 RVA: 0x000235FF File Offset: 0x000217FF
		public float GetPollingCompletion()
		{
			return this._throttlingStatusProvider.GetPollingCompletion();
		}

		// Token: 0x060005ED RID: 1517 RVA: 0x0002360C File Offset: 0x0002180C
		public void UpdateInfo()
		{
			this.UpdateInfo(true);
		}

		// Token: 0x060005EE RID: 1518 RVA: 0x00023618 File Offset: 0x00021818
		public void UpdateInfo(bool updateJobEngineThrottleInfo)
		{
			if (base.EngineID == 0)
			{
				throw new InvalidOperationException("Class wasn't initialized");
			}
			EngineDAL.UpdateEngineInfo(base.EngineID, new Dictionary<string, object>
			{
				{
					"IP",
					base.GetIPAddress()
				},
				{
					"PollingCompletion",
					this.GetPollingCompletion()
				}
			}, true, base.InterfacesSupported);
			if (updateJobEngineThrottleInfo)
			{
				this.UpdateEngineThrottleInfo();
			}
		}

		// Token: 0x060005EF RID: 1519 RVA: 0x00023680 File Offset: 0x00021880
		public void UpdateEngineThrottleInfo()
		{
			List<string> list = new List<string>();
			list.Add("Total Weight");
			list.Add("Scale Factor");
			try
			{
				List<EngineProperty> list2 = new List<EngineProperty>();
				list2.Add(new EngineProperty("Total Job Weight", "Total Weight", this._throttlingStatusProvider.GetTotalJobWeight().ToString()));
				foreach (KeyValuePair<string, int> keyValuePair in this._throttlingStatusProvider.GetScaleFactors())
				{
					list2.Add(new EngineProperty(keyValuePair.Key, "Scale Factor", keyValuePair.Value.ToString()));
				}
				try
				{
					list2.Add(new EngineProperty("Scale Licenses", "Scale Licenses", this.GetStackablePollersCount().ToString()));
					list.Add("Scale Licenses");
				}
				catch (Exception ex)
				{
					MasterEngineInitiator.log.Error("Can't load stackable poller licenses", ex);
				}
				EngineDAL.UpdateEngineProperties(base.EngineID, list2, list.ToArray());
			}
			catch (Exception ex2)
			{
				if (base.ThrowExceptions)
				{
					throw;
				}
				MasterEngineInitiator.log.Error(ex2);
			}
		}

		// Token: 0x060005F0 RID: 1520 RVA: 0x000237CC File Offset: 0x000219CC
		internal ulong GetStackablePollersCount()
		{
			MasterEngineInitiator.<>c__DisplayClass10_0 CS$<>8__locals1 = new MasterEngineInitiator.<>c__DisplayClass10_0();
			ulong num = 0UL;
			MasterEngineInitiator.<>c__DisplayClass10_0 CS$<>8__locals2 = CS$<>8__locals1;
			LicenseType[] array = new LicenseType[3];
			array[0] = 4;
			array[1] = 7;
			CS$<>8__locals2.allowedLicensesTypes = array;
			var list = (from license in this.GetLicenseManager().GetLicenses()
			where license.ExpirationDaysLeft > 0 && CS$<>8__locals1.allowedLicensesTypes.Contains(license.LicenseType) && license.GetFeature("Core.FeatureManager.Features.PollingEngine") != null
			select new
			{
				ProductName = license.ProductName,
				PollerFeatureValue = license.GetFeature("Core.FeatureManager.Features.PollingEngine").Available
			} into licInfo
			where licInfo.PollerFeatureValue > 0UL
			select licInfo).ToList();
			if (MasterEngineInitiator.log.IsDebugEnabled)
			{
				MasterEngineInitiator.log.Debug("All available commercial and not expired licenses with PollingEngine feature to be processed:");
				list.ForEach(delegate(l)
				{
					MasterEngineInitiator.log.Debug(string.Format("License product name: {0}, PollingEngine feature value:{1}", l.ProductName, l.PollerFeatureValue));
				});
			}
			checked
			{
				try
				{
					foreach (var <>f__AnonymousType in from l in list
					where l.ProductName.Equals("Core", StringComparison.OrdinalIgnoreCase)
					select l)
					{
						num += <>f__AnonymousType.PollerFeatureValue;
					}
					if (!this.GetIsAnyPoller())
					{
						num += (from license in list
						where !license.ProductName.Equals("Core", StringComparison.OrdinalIgnoreCase)
						select license.PollerFeatureValue).DefaultIfEmpty(1UL).Max<ulong>();
					}
					num = Math.Min(num, 4UL);
				}
				catch (OverflowException)
				{
					num = 4UL;
				}
				return num;
			}
		}

		// Token: 0x060005F2 RID: 1522 RVA: 0x00023998 File Offset: 0x00021B98
		string IEngineInitiator.get_ServerName()
		{
			return base.ServerName;
		}

		// Token: 0x060005F3 RID: 1523 RVA: 0x000239A0 File Offset: 0x00021BA0
		void IEngineInitiator.InitializeEngine()
		{
			base.InitializeEngine();
		}

		// Token: 0x040001D0 RID: 464
		private static readonly Log log = new Log();

		// Token: 0x040001D1 RID: 465
		internal Func<ILicenseManagerGen3> GetLicenseManager = () => LicenseManager.GetInstance();

		// Token: 0x040001D2 RID: 466
		internal Func<bool> GetIsAnyPoller = () => RegistrySettings.IsAnyPoller();

		// Token: 0x040001D3 RID: 467
		private readonly IThrottlingStatusProvider _throttlingStatusProvider;
	}
}
