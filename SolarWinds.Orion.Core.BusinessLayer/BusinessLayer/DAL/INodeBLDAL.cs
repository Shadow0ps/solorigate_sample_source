using System;
using System.Collections.Generic;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x020000A1 RID: 161
	public interface INodeBLDAL
	{
		// Token: 0x060007C9 RID: 1993
		Node GetNode(int nodeId);

		// Token: 0x060007CA RID: 1994
		Node GetNodeWithOptions(int nodeId, bool includeInterfaces, bool includeVolumes);

		// Token: 0x060007CB RID: 1995
		void UpdateNode(Node node);

		// Token: 0x060007CC RID: 1996
		Nodes GetNodes(bool includeInterfaces, bool includeVolumes);

		// Token: 0x060007CD RID: 1997
		void UpdateNode(IDictionary<string, object> properties, int nodeId);
	}
}
