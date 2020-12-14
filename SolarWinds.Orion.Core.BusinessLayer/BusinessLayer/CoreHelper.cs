using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.Xml.Serialization;
using SolarWinds.Common.Utility;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.BusinessLayer.MaintUpdateNotifySvc;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Configuration;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Common.Swis;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000021 RID: 33
	public class CoreHelper
	{
		// Token: 0x06000317 RID: 791 RVA: 0x00013824 File Offset: 0x00011A24
		private static void ProcessUpdateResponse(UpdateResponse response)
		{
			if (response == null)
			{
				throw new ArgumentNullException("response");
			}
			if (!response.Success)
			{
				CoreHelper._log.Error("Process update response failed.");
				return;
			}
			if (response.Manifest != null && response.Manifest.CurrentVersions != null && response.Manifest.CurrentVersions.Length != 0)
			{
				CoreHelper._log.Debug(string.Format("ProcessUpdateResponse: Following response has been received from SolarWind's Server: {0}", Serializer.ToXmlString(response.Manifest.CurrentVersions)));
				foreach (VersionInfo versionInfo in response.Manifest.CurrentVersions)
				{
					ModuleStatusType moduleStatus = versionInfo.ModuleStatus;
					if (moduleStatus != ModuleStatusType.Updated)
					{
						if (moduleStatus == ModuleStatusType.Current)
						{
							MaintenanceRenewalItemDAL itemForProduct = MaintenanceRenewalItemDAL.GetItemForProduct(versionInfo.ProductTag);
							if (itemForProduct != null)
							{
								itemForProduct.Ignored = true;
								itemForProduct.Update();
							}
						}
					}
					else if (CoreHelper.ShowUpdateProductNotification(versionInfo.ProductTag))
					{
						MaintenanceRenewalItemDAL maintenanceRenewalItemDAL = MaintenanceRenewalItemDAL.GetItemForProduct(versionInfo.ProductTag);
						if (maintenanceRenewalItemDAL == null)
						{
							CoreHelper._log.DebugFormat("Inserting new MaintenanceRenewalItem for product {0}.", versionInfo.ProductTag);
							maintenanceRenewalItemDAL = MaintUpdateNotifySvcWrapper.GetNotificationItem(versionInfo);
							MaintenanceRenewalItemDAL.Insert(Guid.NewGuid(), maintenanceRenewalItemDAL.Title, maintenanceRenewalItemDAL.Description, maintenanceRenewalItemDAL.Ignored, maintenanceRenewalItemDAL.Url, maintenanceRenewalItemDAL.AcknowledgedAt, maintenanceRenewalItemDAL.AcknowledgedBy, maintenanceRenewalItemDAL.ProductTag, maintenanceRenewalItemDAL.DateReleased, maintenanceRenewalItemDAL.NewVersion);
						}
						else
						{
							CoreHelper._log.DebugFormat("Updating existing MaintenanceRenewalItem for product {0}.", versionInfo.ProductTag);
							MaintUpdateNotifySvcWrapper.UpdateNotificationItem(maintenanceRenewalItemDAL, versionInfo);
							maintenanceRenewalItemDAL.Update();
						}
					}
				}
				return;
			}
			CoreHelper._log.Info("No valid modules were submitted, nor found.");
		}

		// Token: 0x06000318 RID: 792 RVA: 0x000139C8 File Offset: 0x00011BC8
		private static bool ShowUpdateProductNotification(string productTag)
		{
			ModuleInfo moduleInfo = ModulesCollector.GetModuleInfo(productTag);
			if (string.IsNullOrEmpty(moduleInfo.ValidateUpdateNotification))
			{
				return true;
			}
			bool result;
			try
			{
				using (IInformationServiceProxy2 informationServiceProxy = SwisConnectionProxyPool.GetSystemCreator().Create())
				{
					CoreHelper._log.DebugFormat("Calling SWQL query: {0}", moduleInfo.ValidateUpdateNotification);
					DataTable dataTable = informationServiceProxy.Query(moduleInfo.ValidateUpdateNotification);
					if (dataTable.Columns.Count != 1 && dataTable.Rows.Count != 1)
					{
						CoreHelper._log.WarnFormat("Invalid query: {0}", moduleInfo.ValidateUpdateNotification);
						result = true;
					}
					else
					{
						result = (dataTable.Rows[0][0] == null || Convert.ToBoolean(dataTable.Rows[0][0]));
					}
				}
			}
			catch (Exception ex)
			{
				CoreHelper._log.ErrorFormat("Execution of ValidateUpdateNotification '{0}' has failed. Exception: {1}", moduleInfo.ValidateUpdateNotification, ex);
				result = true;
			}
			return result;
		}

		// Token: 0x06000319 RID: 793 RVA: 0x00013AC4 File Offset: 0x00011CC4
		public static void CheckMaintenanceRenewals(bool forceCheck)
		{
			UpdateRequest updateRequest = new UpdateRequest();
			updateRequest.ContractVersion = "1";
			updateRequest.CustomerInfo = CustomerEnvironmentManager.GetEnvironmentInfoPack();
			MaintUpdateNotifySvcClient maintUpdateNotifySvcClient = new MaintUpdateNotifySvcClient("WSHttpBinding_IMaintUpdateNotifySvc");
			HttpProxySettings.Instance.SetBinding(new WSHttpBinding("WSHttpBinding_IMaintUpdateNotifySvc"));
			try
			{
				CoreHelper._log.Debug(string.Format("CheckMaintenanceRenewals: Send following Customer Info to SolarWind's Server: {0}", Serializer.ToXmlString(updateRequest.CustomerInfo)));
				string primaryLanguage = new I18NHelper().PrimaryLanguage;
				CoreHelper.ProcessUpdateResponse(maintUpdateNotifySvcClient.GetLocalizedData(updateRequest, primaryLanguage));
			}
			catch (Exception ex)
			{
				CoreHelper._log.Error("CheckMaintenanceRenewals: Error connecting to MaintUpdateNotifySvcClient - " + ex.Message);
			}
			maintUpdateNotifySvcClient.Close();
			MaintenanceRenewalsCheckStatusDAL.SetLastUpdateCheck(Settings.CheckMaintenanceRenewalsTimer.TotalMinutes, forceCheck);
		}

		// Token: 0x0600031A RID: 794 RVA: 0x00013B8C File Offset: 0x00011D8C
		public static void CheckOrionProductTeamBlog()
		{
			HttpWebResponse httpWebResponse = WebRequestHelper.SendHttpWebRequest(BusinessLayerSettings.Instance.OrionProductTeamBlogUrl);
			if (httpWebResponse != null)
			{
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(RssBlogItems));
				using (StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
				{
					BlogItemDAL.StoreBlogItems(ProductBlogSvcWrapper.GetBlogItems((RssBlogItems)xmlSerializer.Deserialize(streamReader)), Convert.ToInt32(SettingsDAL.Get("ProductsBlog-StoredPostsCount")));
				}
			}
		}

		// Token: 0x0600031B RID: 795 RVA: 0x00013C0C File Offset: 0x00011E0C
		public static bool IsEngineVersionSameAsOnMain(int engineId)
		{
			string versionOfPrimaryEngine = CoreHelper.GetVersionOfPrimaryEngine();
			string versionOfEngine = CoreHelper.GetVersionOfEngine(engineId);
			bool flag = !string.IsNullOrEmpty(versionOfEngine) && !string.IsNullOrEmpty(versionOfPrimaryEngine) && versionOfEngine.Equals(versionOfPrimaryEngine, StringComparison.InvariantCultureIgnoreCase);
			if (!flag)
			{
				CoreHelper._log.Debug(string.Format("Engine ({0}) version [{1}] is different from the primary engine version [{2}]", engineId, versionOfEngine, versionOfPrimaryEngine));
			}
			return flag;
		}

		// Token: 0x0600031C RID: 796 RVA: 0x00013C60 File Offset: 0x00011E60
		public static string GetVersionOfEngine(int engineId)
		{
			string result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT EngineVersion FROM Engines WHERE EngineID = @engineID"))
			{
				try
				{
					textCommand.Parameters.AddWithValue("engineID", engineId);
					result = Convert.ToString(SqlHelper.ExecuteScalar(textCommand));
				}
				catch (Exception ex)
				{
					CoreHelper._log.Error("Error while trying to get engine version of the engine.", ex);
					result = null;
				}
			}
			return result;
		}

		// Token: 0x0600031D RID: 797 RVA: 0x00013CDC File Offset: 0x00011EDC
		public static string GetVersionOfPrimaryEngine()
		{
			string result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT EngineVersion FROM Engines WHERE ServerType = 'Primary'"))
			{
				try
				{
					result = Convert.ToString(SqlHelper.ExecuteScalar(textCommand));
				}
				catch (Exception ex)
				{
					CoreHelper._log.Error("Error while trying to get engine version of the primary engine.", ex);
					result = null;
				}
			}
			return result;
		}

		// Token: 0x040000A4 RID: 164
		private static readonly Log _log = new Log();
	}
}
