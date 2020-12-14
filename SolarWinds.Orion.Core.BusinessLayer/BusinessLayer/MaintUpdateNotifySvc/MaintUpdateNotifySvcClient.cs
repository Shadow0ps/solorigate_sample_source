using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;

namespace SolarWinds.Orion.Core.BusinessLayer.MaintUpdateNotifySvc
{
	// Token: 0x02000063 RID: 99
	[DebuggerStepThrough]
	[GeneratedCode("System.ServiceModel", "4.0.0.0")]
	public class MaintUpdateNotifySvcClient : ClientBase<IMaintUpdateNotifySvc>, IMaintUpdateNotifySvc
	{
		// Token: 0x0600057B RID: 1403 RVA: 0x00021978 File Offset: 0x0001FB78
		public MaintUpdateNotifySvcClient()
		{
		}

		// Token: 0x0600057C RID: 1404 RVA: 0x00021980 File Offset: 0x0001FB80
		public MaintUpdateNotifySvcClient(string endpointConfigurationName) : base(endpointConfigurationName)
		{
		}

		// Token: 0x0600057D RID: 1405 RVA: 0x00021989 File Offset: 0x0001FB89
		public MaintUpdateNotifySvcClient(string endpointConfigurationName, string remoteAddress) : base(endpointConfigurationName, remoteAddress)
		{
		}

		// Token: 0x0600057E RID: 1406 RVA: 0x00021993 File Offset: 0x0001FB93
		public MaintUpdateNotifySvcClient(string endpointConfigurationName, EndpointAddress remoteAddress) : base(endpointConfigurationName, remoteAddress)
		{
		}

		// Token: 0x0600057F RID: 1407 RVA: 0x0002199D File Offset: 0x0001FB9D
		public MaintUpdateNotifySvcClient(Binding binding, EndpointAddress remoteAddress) : base(binding, remoteAddress)
		{
		}

		// Token: 0x06000580 RID: 1408 RVA: 0x000219A7 File Offset: 0x0001FBA7
		public UpdateResponse GetData(UpdateRequest request)
		{
			return base.Channel.GetData(request);
		}

		// Token: 0x06000581 RID: 1409 RVA: 0x000219B5 File Offset: 0x0001FBB5
		public Task<UpdateResponse> GetDataAsync(UpdateRequest request)
		{
			return base.Channel.GetDataAsync(request);
		}

		// Token: 0x06000582 RID: 1410 RVA: 0x000219C3 File Offset: 0x0001FBC3
		public UpdateResponse GetLocalizedData(UpdateRequest request, string locale)
		{
			return base.Channel.GetLocalizedData(request, locale);
		}

		// Token: 0x06000583 RID: 1411 RVA: 0x000219D2 File Offset: 0x0001FBD2
		public Task<UpdateResponse> GetLocalizedDataAsync(UpdateRequest request, string locale)
		{
			return base.Channel.GetLocalizedDataAsync(request, locale);
		}
	}
}
