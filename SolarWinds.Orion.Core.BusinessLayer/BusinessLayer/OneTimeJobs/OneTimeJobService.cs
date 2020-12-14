using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Xml;
using System.Xml.Serialization;
using SolarWinds.JobEngine;
using SolarWinds.JobEngine.Security;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.BusinessLayer.BL;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Strings;
using SolarWinds.Serialization.Json;

namespace SolarWinds.Orion.Core.BusinessLayer.OneTimeJobs
{
	// Token: 0x0200006D RID: 109
	public class OneTimeJobService : IOneTimeJobService, IDisposable
	{
		// Token: 0x060005BB RID: 1467 RVA: 0x0002261D File Offset: 0x0002081D
		public OneTimeJobService() : this(new OneTimeJobManager(), new EngineDAL())
		{
		}

		// Token: 0x060005BC RID: 1468 RVA: 0x0002262F File Offset: 0x0002082F
		public OneTimeJobService(IOneTimeJobManager oneTimeJobManager, IEngineDAL engineDal)
		{
			if (oneTimeJobManager == null)
			{
				throw new ArgumentNullException("oneTimeJobManager");
			}
			this.oneTimeJobManager = oneTimeJobManager;
			if (engineDal == null)
			{
				throw new ArgumentNullException("engineDal");
			}
			this.engineDal = engineDal;
		}

		// Token: 0x060005BD RID: 1469 RVA: 0x00022664 File Offset: 0x00020864
		public void Start()
		{
			this.oneTimeJobsCallbackHost = new ServiceHost(this.oneTimeJobManager, Array.Empty<Uri>());
			string absoluteUri = this.oneTimeJobsCallbackHost.Description.Endpoints.First<ServiceEndpoint>().ListenUri.AbsoluteUri;
			this.oneTimeJobManager.SetListenerUri(absoluteUri);
			this.oneTimeJobsCallbackHost.Open();
		}

		// Token: 0x060005BE RID: 1470 RVA: 0x000226C0 File Offset: 0x000208C0
		public void Start(string endpointAddress)
		{
			this.oneTimeJobsCallbackHost = new ServiceHost(this.oneTimeJobManager, Array.Empty<Uri>());
			NetNamedPipeBinding binding = new NetNamedPipeBinding
			{
				SendTimeout = TimeSpan.FromMinutes(3.0),
				TransferMode = TransferMode.StreamedResponse,
				MaxBufferSize = int.MaxValue,
				MaxReceivedMessageSize = 2147483647L,
				ReaderQuotas = new XmlDictionaryReaderQuotas
				{
					MaxStringContentLength = int.MaxValue,
					MaxArrayLength = int.MaxValue
				}
			};
			this.oneTimeJobsCallbackHost.AddServiceEndpoint(typeof(IJobSchedulerEvents), binding, endpointAddress);
			this.oneTimeJobManager.SetListenerUri(endpointAddress);
			this.oneTimeJobsCallbackHost.Open();
		}

		// Token: 0x060005BF RID: 1471 RVA: 0x0002276C File Offset: 0x0002096C
		public OneTimeJobResult<T> ExecuteJobAndGetResult<T>(int engineId, JobDescription jobDescription, CredentialBase jobCredential, JobResultDataFormatType resultDataFormat, string jobType) where T : class, new()
		{
			string serverName = this.engineDal.GetEngine(engineId).ServerName;
			return this.ExecuteJobAndGetResult<T>(serverName, jobDescription, jobCredential, resultDataFormat, jobType);
		}

		// Token: 0x060005C0 RID: 1472 RVA: 0x00022798 File Offset: 0x00020998
		public OneTimeJobResult<T> ExecuteJobAndGetResult<T>(string engineName, JobDescription jobDescription, CredentialBase jobCredential, JobResultDataFormatType resultDataFormat, string jobType) where T : class, new()
		{
			this.RouteJobToEngine(jobDescription, engineName);
			OneTimeJobResult<T> result;
			using (OneTimeJobRawResult oneTimeJobRawResult = this.oneTimeJobManager.ExecuteJob(jobDescription, jobCredential))
			{
				string error = oneTimeJobRawResult.Error;
				if (!oneTimeJobRawResult.Success)
				{
					OneTimeJobService.log.WarnFormat(jobType + " credential test failed: " + oneTimeJobRawResult.Error, Array.Empty<object>());
					string localizedErrorMessageFromException = this.GetLocalizedErrorMessageFromException(oneTimeJobRawResult.ExceptionFromJob);
					result = new OneTimeJobResult<T>
					{
						Success = false,
						Message = (string.IsNullOrEmpty(localizedErrorMessageFromException) ? error : localizedErrorMessageFromException)
					};
				}
				else
				{
					try
					{
						T value;
						if (resultDataFormat == JobResultDataFormatType.Xml)
						{
							using (XmlTextReader xmlTextReader = new XmlTextReader(oneTimeJobRawResult.JobResultStream))
							{
								xmlTextReader.Namespaces = false;
								value = (T)((object)new XmlSerializer(typeof(T)).Deserialize(xmlTextReader));
								goto IL_CF;
							}
						}
						value = SerializationHelper.Deserialize<T>(oneTimeJobRawResult.JobResultStream);
						IL_CF:
						result = new OneTimeJobResult<T>
						{
							Success = true,
							Value = value
						};
					}
					catch (Exception arg)
					{
						OneTimeJobService.log.Error(string.Format("Failed to deserialize {0} credential test job result: {1}", jobType, arg));
						result = new OneTimeJobResult<T>
						{
							Success = false,
							Message = this.GetLocalizedErrorMessageFromException(oneTimeJobRawResult.ExceptionFromJob)
						};
					}
				}
			}
			return result;
		}

		// Token: 0x060005C1 RID: 1473 RVA: 0x00022924 File Offset: 0x00020B24
		private void RouteJobToEngine(JobDescription jobDescription, string engineName)
		{
			if (!string.IsNullOrEmpty(jobDescription.LegacyEngine))
			{
				return;
			}
			jobDescription.LegacyEngine = engineName;
		}

		// Token: 0x060005C2 RID: 1474 RVA: 0x0002293B File Offset: 0x00020B3B
		private string GetLocalizedErrorMessageFromException(Exception exception)
		{
			if (exception is FaultException<JobEngineConnectionFault>)
			{
				return Resources.LIBCODE_PS0_20;
			}
			return string.Empty;
		}

		// Token: 0x060005C3 RID: 1475 RVA: 0x00022950 File Offset: 0x00020B50
		public void Dispose()
		{
			MessageUtilities.ShutdownCommunicationObject(this.oneTimeJobsCallbackHost);
		}

		// Token: 0x040001AF RID: 431
		private static readonly Log log = new Log();

		// Token: 0x040001B0 RID: 432
		private readonly IOneTimeJobManager oneTimeJobManager;

		// Token: 0x040001B1 RID: 433
		private ServiceHost oneTimeJobsCallbackHost;

		// Token: 0x040001B2 RID: 434
		private IEngineDAL engineDal;
	}
}
