using System;
using SolarWinds.JobEngine;
using SolarWinds.JobEngine.Security;
using SolarWinds.Orion.Core.BusinessLayer.BL;

namespace SolarWinds.Orion.Core.BusinessLayer.OneTimeJobs
{
	// Token: 0x02000069 RID: 105
	public interface IOneTimeJobService : IDisposable
	{
		// Token: 0x0600059A RID: 1434
		void Start(string listenerUri);

		// Token: 0x0600059B RID: 1435
		OneTimeJobResult<T> ExecuteJobAndGetResult<T>(int engineId, JobDescription jobDescription, CredentialBase jobCredential, JobResultDataFormatType resultDataFormat, string jobType) where T : class, new();

		// Token: 0x0600059C RID: 1436
		OneTimeJobResult<T> ExecuteJobAndGetResult<T>(string engineName, JobDescription jobDescription, CredentialBase jobCredential, JobResultDataFormatType resultDataFormat, string jobType) where T : class, new();
	}
}
