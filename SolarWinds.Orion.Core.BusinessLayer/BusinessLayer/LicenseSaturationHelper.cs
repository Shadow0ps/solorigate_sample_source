using System;
using System.Collections.Generic;
using System.Linq;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.Common.Licensing;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200001F RID: 31
	internal static class LicenseSaturationHelper
	{
		// Token: 0x060002D4 RID: 724 RVA: 0x000118B4 File Offset: 0x0000FAB4
		internal static void CheckLicenseSaturation()
		{
			try
			{
				LicenseSaturationHelper.Log.Debug("Checking license saturation");
				List<ModuleLicenseSaturationInfo> modulesSaturationInfo = LicenseSaturationLogic.GetModulesSaturationInfo(new int?(LicenseSaturationHelper.SaturationLimit));
				if (modulesSaturationInfo.Count == 0)
				{
					LicenseSaturationHelper.Log.DebugFormat("All modules below {0}% of their license", LicenseSaturationHelper.SaturationLimit);
					LicenseSaturationNotificationItemDAL.Hide();
					LicensePreSaturationNotificationItemDAL.Hide();
				}
				else
				{
					List<ModuleLicenseSaturationInfo> list = (from q in modulesSaturationInfo
					where q.ElementList.Any((ElementLicenseSaturationInfo l) => l.Saturation > 99.0)
					select q).ToList<ModuleLicenseSaturationInfo>();
					List<ModuleLicenseSaturationInfo> list2 = (from q in modulesSaturationInfo
					where q.ElementList.Any((ElementLicenseSaturationInfo l) => l.Saturation > (double)LicenseSaturationHelper.SaturationLimit && l.Saturation < 100.0)
					select q).ToList<ModuleLicenseSaturationInfo>();
					List<ElementLicenseSaturationInfo> overUsedElements = new List<ElementLicenseSaturationInfo>();
					list.ForEach(delegate(ModuleLicenseSaturationInfo l)
					{
						overUsedElements.AddRange(l.ElementList.ToArray());
					});
					if (LicenseSaturationHelper.Log.IsInfoEnabled)
					{
						LicenseSaturationHelper.Log.InfoFormat("These elements are at 100% of their license: {0}", string.Join(";", from q in overUsedElements
						select q.ElementType));
					}
					LicenseSaturationNotificationItemDAL.Show(from q in overUsedElements
					select q.ElementType);
					List<ElementLicenseSaturationInfo> warningElements = new List<ElementLicenseSaturationInfo>();
					list2.ForEach(delegate(ModuleLicenseSaturationInfo l)
					{
						warningElements.AddRange(l.ElementList.ToArray());
					});
					if (LicenseSaturationHelper.Log.IsInfoEnabled)
					{
						LicenseSaturationHelper.Log.InfoFormat("These elements are above {0}% of their license: {1}", LicenseSaturationHelper.SaturationLimit, string.Join(";", from q in warningElements
						select q.ElementType));
					}
					LicensePreSaturationNotificationItemDAL.Show();
				}
			}
			catch (Exception ex)
			{
				LicenseSaturationHelper.Log.Error("Exception running CheckLicenseSaturation:", ex);
			}
		}

		// Token: 0x060002D5 RID: 725 RVA: 0x00011AB4 File Offset: 0x0000FCB4
		internal static void SaveElementsUsageInfo()
		{
			try
			{
				LicenseSaturationHelper.Log.Debug("Collecting elements usage information to store in history");
				List<ModuleLicenseSaturationInfo> modulesSaturationInfo = LicenseSaturationLogic.GetModulesSaturationInfo(null);
				if (modulesSaturationInfo.Count != 0)
				{
					List<ElementLicenseSaturationInfo> elements = new List<ElementLicenseSaturationInfo>();
					modulesSaturationInfo.ForEach(delegate(ModuleLicenseSaturationInfo m)
					{
						elements.AddRange(m.ElementList.ToArray());
					});
					ElementsUsageDAL.Save(elements);
				}
				else
				{
					LicenseSaturationHelper.Log.DebugFormat("There is no elements usage information to store in history", Array.Empty<object>());
				}
			}
			catch (Exception ex)
			{
				LicenseSaturationHelper.Log.Error("Exception running SaveElementsUsageInfo:", ex);
			}
		}

		// Token: 0x0400007F RID: 127
		private static readonly Log Log = new Log();

		// Token: 0x04000080 RID: 128
		private static readonly int SaturationLimit = Settings.LicenseSaturationPercentage;
	}
}
