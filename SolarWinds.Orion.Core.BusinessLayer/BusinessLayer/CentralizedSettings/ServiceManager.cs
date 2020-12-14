using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.BusinessLayer.ConfigurationSettings;
using SolarWinds.Orion.Core.Common.CentralizedSettings;

namespace SolarWinds.Orion.Core.BusinessLayer.CentralizedSettings
{
	// Token: 0x020000B2 RID: 178
	public class ServiceManager
	{
		// Token: 0x1700011F RID: 287
		// (get) Token: 0x060008CD RID: 2253 RVA: 0x0003F600 File Offset: 0x0003D800
		public static ServiceManager Instance
		{
			get
			{
				ServiceManager result;
				if ((result = ServiceManager.instance) == null)
				{
					result = (ServiceManager.instance = new ServiceManager());
				}
				return result;
			}
		}

		// Token: 0x060008CE RID: 2254 RVA: 0x0003F616 File Offset: 0x0003D816
		protected ServiceManager()
		{
			ServiceManager.services = ServiceManager.GetAllWindowsServices();
		}

		// Token: 0x060008CF RID: 2255 RVA: 0x0003F628 File Offset: 0x0003D828
		protected ServiceManager(List<IWindowsServiceController> servicesLst)
		{
			ServiceManager.services = servicesLst;
		}

		// Token: 0x060008D0 RID: 2256 RVA: 0x0003F636 File Offset: 0x0003D836
		private static List<IWindowsServiceController> GetAllWindowsServices()
		{
			return new List<IWindowsServiceController>((from s in ServiceController.GetServices()
			select new WindowsServiceController(s.ServiceName)).ToList<WindowsServiceController>());
		}

		// Token: 0x060008D1 RID: 2257 RVA: 0x0003F66C File Offset: 0x0003D86C
		public Dictionary<string, string> GetServicesDisplayNames(List<string> servicesNames)
		{
			return (from s in ServiceManager.services
			where servicesNames.Any((string x) => x.Equals(s.ServiceName, StringComparison.OrdinalIgnoreCase)) && s.Status != ServiceControllerStatus.StopPending && s.Status != ServiceControllerStatus.Stopped
			select s).Distinct<IWindowsServiceController>().ToDictionary((IWindowsServiceController s) => s.ServiceName, (IWindowsServiceController s) => s.DisplayName);
		}

		// Token: 0x060008D2 RID: 2258 RVA: 0x0003F6E4 File Offset: 0x0003D8E4
		public Dictionary<string, WindowsServiceRestartState> GetServicesStates(List<string> servicesNames)
		{
			return (from s in ServiceManager.services
			where servicesNames.Any((string x) => x.Equals(s.ServiceName, StringComparison.OrdinalIgnoreCase))
			select s).Distinct<IWindowsServiceController>().ToDictionary((IWindowsServiceController s) => s.ServiceName, (IWindowsServiceController s) => s.RestartState);
		}

		// Token: 0x060008D3 RID: 2259 RVA: 0x0003F75C File Offset: 0x0003D95C
		public void RestartServices(List<string> servicesNames)
		{
			Parallel.ForEach<IWindowsServiceController>(from s in ServiceManager.services
			where servicesNames.Any((string x) => x.Equals(s.ServiceName, StringComparison.OrdinalIgnoreCase))
			select s, delegate(IWindowsServiceController currentElement)
			{
				try
				{
					ServiceManager.Log.DebugFormat("Restarting service {0} started", currentElement.DisplayName);
					currentElement.RestartState = WindowsServiceRestartState.Restarting;
					int serviceTimeout = WindowsServiceSettings.Instance.ServiceTimeout;
					int tickCount = Environment.TickCount;
					TimeSpan timeout = TimeSpan.FromMilliseconds((double)serviceTimeout);
					if (currentElement.Status == ServiceControllerStatus.Running)
					{
						currentElement.Stop();
						currentElement.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
					}
					int tickCount2 = Environment.TickCount;
					timeout = TimeSpan.FromMilliseconds((double)(serviceTimeout - (tickCount2 - tickCount)));
					if (currentElement.Status == ServiceControllerStatus.Stopped)
					{
						currentElement.Start();
						currentElement.WaitForStatus(ServiceControllerStatus.Running, timeout);
					}
					currentElement.RestartState = WindowsServiceRestartState.Ready;
					ServiceManager.Log.DebugFormat("Restarting service {0} ended", currentElement.DisplayName);
				}
				catch (Exception ex)
				{
					currentElement.RestartState = WindowsServiceRestartState.Fault;
					ServiceManager.Log.DebugFormat("Restarting service {0} failed. {1}", currentElement.DisplayName, ex);
				}
			});
		}

		// Token: 0x0400027A RID: 634
		private static readonly Log Log = new Log();

		// Token: 0x0400027B RID: 635
		protected static List<IWindowsServiceController> services;

		// Token: 0x0400027C RID: 636
		private static ServiceManager instance;
	}
}
