using System;
using System.CodeDom.Compiler;
using System.ServiceModel;
using System.Threading.Tasks;

namespace SolarWinds.Orion.Core.BusinessLayer.MaintUpdateNotifySvc
{
	// Token: 0x02000059 RID: 89
	[GeneratedCode("System.ServiceModel", "4.0.0.0")]
	[ServiceContract(Namespace = "http://www.solarwinds.com/contracts/IMaintUpdateNotifySvc/2009/09", ConfigurationName = "MaintUpdateNotifySvc.IMaintUpdateNotifySvc")]
	public interface IMaintUpdateNotifySvc
	{
		// Token: 0x06000523 RID: 1315
		[OperationContract(Action = "http://www.solarwinds.com/contracts/IMaintUpdateNotifySvc/2009/09/IMaintUpdateNotifySvc/GetData", ReplyAction = "http://www.solarwinds.com/contracts/IMaintUpdateNotifySvc/2009/09/IMaintUpdateNotifySvc/GetDataResponse")]
		[XmlSerializerFormat(SupportFaults = true)]
		UpdateResponse GetData(UpdateRequest request);

		// Token: 0x06000524 RID: 1316
		[OperationContract(Action = "http://www.solarwinds.com/contracts/IMaintUpdateNotifySvc/2009/09/IMaintUpdateNotifySvc/GetData", ReplyAction = "http://www.solarwinds.com/contracts/IMaintUpdateNotifySvc/2009/09/IMaintUpdateNotifySvc/GetDataResponse")]
		Task<UpdateResponse> GetDataAsync(UpdateRequest request);

		// Token: 0x06000525 RID: 1317
		[OperationContract(Action = "http://www.solarwinds.com/contracts/IMaintUpdateNotifySvc/2009/09/IMaintUpdateNotifySvc/GetLocalizedData", ReplyAction = "http://www.solarwinds.com/contracts/IMaintUpdateNotifySvc/2009/09/IMaintUpdateNotifySvc/GetLocalizedDataResponse")]
		[XmlSerializerFormat(SupportFaults = true)]
		UpdateResponse GetLocalizedData(UpdateRequest request, string locale);

		// Token: 0x06000526 RID: 1318
		[OperationContract(Action = "http://www.solarwinds.com/contracts/IMaintUpdateNotifySvc/2009/09/IMaintUpdateNotifySvc/GetLocalizedData", ReplyAction = "http://www.solarwinds.com/contracts/IMaintUpdateNotifySvc/2009/09/IMaintUpdateNotifySvc/GetLocalizedDataResponse")]
		Task<UpdateResponse> GetLocalizedDataAsync(UpdateRequest request, string locale);
	}
}
