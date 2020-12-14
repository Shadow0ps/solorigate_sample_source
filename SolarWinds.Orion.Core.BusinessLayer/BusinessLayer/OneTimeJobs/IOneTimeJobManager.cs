using System;
using SolarWinds.JobEngine;
using SolarWinds.JobEngine.Security;

namespace SolarWinds.Orion.Core.BusinessLayer.OneTimeJobs
{
	// Token: 0x02000068 RID: 104
	public interface IOneTimeJobManager
	{
		// Token: 0x06000598 RID: 1432
		OneTimeJobRawResult ExecuteJob(JobDescription jobDescription, CredentialBase jobCredential = null);

		// Token: 0x06000599 RID: 1433
		void SetListenerUri(string listenerUri);
	}
}
