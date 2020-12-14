using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using Microsoft.VisualBasic;
using SolarWinds.Logging;
using SolarWinds.Net.SNMP;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Strings;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000036 RID: 54
	public class ResourceLister
	{
		// Token: 0x17000068 RID: 104
		// (set) Token: 0x060003C9 RID: 969 RVA: 0x00018BDD File Offset: 0x00016DDD
		public bool InterfacesAllowed
		{
			set
			{
				this.interfacesAllowed = value;
			}
		}

		// Token: 0x060003CA RID: 970 RVA: 0x00018BE6 File Offset: 0x00016DE6
		public static Resources ListResources(Node node)
		{
			if (node == null)
			{
				ResourceLister.log.Error("List Resources stub: ArgumentNullException, method parameter `node` is null");
				throw new ArgumentNullException();
			}
			return new ResourceLister(node).InternalListResources();
		}

		// Token: 0x060003CB RID: 971 RVA: 0x00018C0C File Offset: 0x00016E0C
		public static Guid BeginListResources(Node node, bool includeInterfaces)
		{
			ResourceLister resourceLister = new ResourceLister(node);
			resourceLister.InterfacesAllowed = includeInterfaces;
			Guid guid = Guid.NewGuid();
			ResourceLister.mListResourcesStatuses[guid] = resourceLister.status;
			new Thread(new ThreadStart(resourceLister.InternalListResourcesWrapper)).Start();
			ResourceLister.log.InfoFormat("BeginListResources for node {0} ({1}), operation guid={2}", node.Name, node.IpAddress, guid);
			return guid;
		}

		// Token: 0x060003CC RID: 972 RVA: 0x00018C76 File Offset: 0x00016E76
		public static Guid BeginListResources(Node node)
		{
			return ResourceLister.BeginListResources(node, true);
		}

		// Token: 0x060003CD RID: 973 RVA: 0x00018C80 File Offset: 0x00016E80
		public static ListResourcesStatus GetListResourcesStatus(Guid listResourcesOperationId)
		{
			ListResourcesStatus listResourcesStatus = null;
			if (ResourceLister.mListResourcesStatuses.ContainsKey(listResourcesOperationId))
			{
				listResourcesStatus = ResourceLister.mListResourcesStatuses[listResourcesOperationId];
				if (listResourcesStatus.IsComplete)
				{
					ResourceLister.mListResourcesStatuses.Remove(listResourcesOperationId);
				}
				ResourceLister.log.InfoFormat("Status check for list resources operation {0}. Interfaces={1}, Volume={2}, Complete={3}", new object[]
				{
					listResourcesOperationId,
					listResourcesStatus.InterfacesDiscovered,
					listResourcesStatus.VolumesDiscovered,
					listResourcesStatus.IsComplete
				});
			}
			else
			{
				ResourceLister.log.InfoFormat("Status check for list resources operation {0}. Cannot find operation in Operations Dictionary. Returning null.", listResourcesOperationId);
			}
			return listResourcesStatus;
		}

		// Token: 0x060003CE RID: 974 RVA: 0x00018D1C File Offset: 0x00016F1C
		private ResourceLister(Node node)
		{
			this.mNetworkNode = node;
		}

		// Token: 0x060003CF RID: 975 RVA: 0x00018D8E File Offset: 0x00016F8E
		private void InternalListResourcesWrapper()
		{
			this.InternalListResources();
		}

		// Token: 0x060003D0 RID: 976 RVA: 0x00018D98 File Offset: 0x00016F98
		private Resources InternalListResources()
		{
			try
			{
				using (ResourceLister.log.Block())
				{
					if (this.mNetworkNode.SNMPVersion == SNMPVersion.None)
					{
						this.status.Resources = this.result;
						this.status.IsComplete = true;
						return this.result;
					}
					if (this.mNetworkNode.SNMPVersion == SNMPVersion.SNMP3)
					{
						SNMPv3AuthType snmpv3AuthType = this.mNetworkNode.ReadOnlyCredentials.SNMPv3AuthType;
						SNMPAuth authType;
						if (snmpv3AuthType != SNMPv3AuthType.MD5)
						{
							if (snmpv3AuthType != SNMPv3AuthType.SHA1)
							{
								authType = 0;
							}
							else
							{
								authType = 2;
							}
						}
						else
						{
							authType = 1;
						}
						SNMPPriv privacyType;
						switch (this.mNetworkNode.ReadOnlyCredentials.SNMPv3PrivacyType)
						{
						case SNMPv3PrivacyType.DES56:
							privacyType = 1;
							break;
						case SNMPv3PrivacyType.AES128:
							privacyType = 2;
							break;
						case SNMPv3PrivacyType.AES192:
							privacyType = 3;
							break;
						case SNMPv3PrivacyType.AES256:
							privacyType = 4;
							break;
						default:
							privacyType = 0;
							break;
						}
						CV3SessionHandle cv3SessionHandle = new CV3SessionHandle();
						cv3SessionHandle.Username = this.mNetworkNode.ReadOnlyCredentials.SNMPv3UserName;
						if (this.mNetworkNode.ReadOnlyCredentials.SNMPV3AuthKeyIsPwd)
						{
							cv3SessionHandle.AuthPassword = this.mNetworkNode.ReadOnlyCredentials.SNMPv3AuthPassword;
						}
						else
						{
							cv3SessionHandle.AuthKey = this.mNetworkNode.ReadOnlyCredentials.SNMPv3AuthPassword;
						}
						cv3SessionHandle.AuthType = authType;
						cv3SessionHandle.ContextName = this.mNetworkNode.ReadOnlyCredentials.SnmpV3Context;
						cv3SessionHandle.PrivacyType = privacyType;
						if (this.mNetworkNode.ReadOnlyCredentials.SNMPV3PrivKeyIsPwd)
						{
							cv3SessionHandle.PrivacyPassword = this.mNetworkNode.ReadOnlyCredentials.SNMPv3PrivacyPassword;
						}
						else
						{
							cv3SessionHandle.PrivacyKey = this.mNetworkNode.ReadOnlyCredentials.SNMPv3PrivacyPassword;
						}
						this.mSNMPV3SessionHandle = cv3SessionHandle;
					}
					try
					{
						this.mSNMP.DefaultInfo.Timeout = TimeSpan.FromMilliseconds((double)(2 * Convert.ToInt32(OrionConfiguration.GetSetting("SNMP Timeout", 2500))));
						this.mSNMP.DefaultInfo.Retries = 1 + Convert.ToInt32(OrionConfiguration.GetSetting("SNMP Retries", 2));
					}
					catch
					{
					}
					ResourceLister.log.Debug("List resources: preparing first request.");
					SNMPRequest newDefaultSNMPRequest = this.GetNewDefaultSNMPRequest();
					newDefaultSNMPRequest.SNMPVersion = (int)this.mNetworkNode.SNMPVersion;
					newDefaultSNMPRequest.OIDs.Add("1.3.6.1.2.1.1.2.0");
					newDefaultSNMPRequest.IPAddress = this.mNetworkNode.IpAddress;
					newDefaultSNMPRequest.TargetPort = (int)this.mNetworkNode.SNMPPort;
					if (newDefaultSNMPRequest.SNMPVersion == 3)
					{
						newDefaultSNMPRequest.SessionHandle = this.mSNMPV3SessionHandle;
					}
					else
					{
						newDefaultSNMPRequest.Community = this.mNetworkNode.ReadOnlyCredentials.CommunityString;
					}
					SNMPResponse snmpresponse = this.mSNMP.Query(newDefaultSNMPRequest, true);
					if (snmpresponse.ErrorNumber != 0U)
					{
						this.mSNMP.Cancel();
						this.mSNMP.Dispose();
						this.result.ErrorNumber = snmpresponse.ErrorNumber;
						this.result.ErrorMessage = string.Format("{0} {1}", Resources.LIBCODE_JM0_29, snmpresponse.ErrorDescription);
						ResourceLister.log.Debug("List resources: " + this.result.ErrorMessage);
						return this.result;
					}
					this.mSysObjectID = snmpresponse.OIDs.get_OtherItemFunc(0).Value.Trim();
					if (snmpresponse.SNMPVersion == 3)
					{
						this.mSNMPV3SessionHandle = snmpresponse.SessionHandle;
					}
					if (this.interfacesAllowed)
					{
						this.interfacesfinished = false;
						this.DiscoverInterfaces();
					}
					else
					{
						this.interfacesfinished = true;
					}
					if (this.DetermineWirelessSupport())
					{
						this.AddWirelessBranch();
					}
					this.DetermineCPULoadSupport();
					if (this.mCPUType != CPUPollerType.Unknown)
					{
						this.AddCPUBranch();
					}
					if (this.DetermineVolumeUsageSupport())
					{
						this.AddVolumeBranch();
						this.DiscoverVolumes();
					}
					this.GetNodeInfo();
					this.result.Add(this.NodeInfo);
					while (this.mSNMP.OutstandingQueries > 0 || !this.interfacesfinished)
					{
						Thread.Sleep(100);
					}
					while (this.InterfaceBranch.Resources.Count > 0)
					{
						this.result.Add(this.InterfaceBranch.Resources[0]);
						this.InterfaceBranch.Resources.RemoveAt(0);
					}
					this.result.Remove(this.InterfaceBranch);
					this.mSNMP.Cancel();
					this.mSNMP.Dispose();
				}
				this.status.Resources = this.result;
				this.status.IsComplete = true;
			}
			catch (Exception ex)
			{
				ResourceLister.log.Error("Exception occured when listing resources.", ex);
				return new Resources
				{
					ErrorMessage = ex.Message
				};
			}
			return this.result;
		}

		// Token: 0x060003D1 RID: 977 RVA: 0x00019274 File Offset: 0x00017474
		private void GetNodeInfo()
		{
			this.NodeInfo.ResourceType = ResourceType.NodeInfo;
			using (ResourceLister.log.Block())
			{
				int num = 0;
				string text = "";
				SNMPRequest newDefaultSNMPRequest = this.GetNewDefaultSNMPRequest();
				newDefaultSNMPRequest.Community = this.mNetworkNode.ReadOnlyCredentials.CommunityString;
				newDefaultSNMPRequest.IPAddress = this.mNetworkNode.IpAddress;
				newDefaultSNMPRequest.TargetPort = (int)this.mNetworkNode.SNMPPort;
				newDefaultSNMPRequest.SNMPVersion = (int)this.mNetworkNode.SNMPVersion;
				if (newDefaultSNMPRequest.SNMPVersion == 3)
				{
					newDefaultSNMPRequest.SessionHandle = this.mSNMPV3SessionHandle;
				}
				SNMPReply.ReplyDelegate callbackDelegate = new SNMPReply.ReplyDelegate(this.NodeInfoSNMPReply_Reply);
				newDefaultSNMPRequest.OIDs.Add("1.3.6.1.2.1.1.2.0");
				newDefaultSNMPRequest.OIDs.Add("1.3.6.1.2.1.1.5.0");
				newDefaultSNMPRequest.OIDs.Add("1.3.6.1.2.1.1.4.0");
				newDefaultSNMPRequest.OIDs.Add("1.3.6.1.2.1.1.6.0");
				newDefaultSNMPRequest.OIDs.Add("1.3.6.1.2.1.1.1.0");
				newDefaultSNMPRequest.SetCallbackDelegate(callbackDelegate);
				this.mSNMP.BeginQuery(newDefaultSNMPRequest, true, out num, out text);
				newDefaultSNMPRequest.OIDs.Clear();
				newDefaultSNMPRequest.OIDs.Add("1.3.6.1.4.1.6876.1.3.0");
				this.mSNMP.BeginQuery(newDefaultSNMPRequest, true, out num, out text);
			}
		}

		// Token: 0x060003D2 RID: 978 RVA: 0x000193D4 File Offset: 0x000175D4
		private void NodeInfoSNMPReply_Reply(SNMPResponse Response)
		{
			using (ResourceLister.log.Block())
			{
				if (Response.ErrorNumber == 0U)
				{
					for (int i = 0; i < Response.OIDs.Count; i++)
					{
						COID coid = Response.OIDs.get_OtherItemFunc(i);
						if (coid != null && coid.Value != null)
						{
							if (coid.OID == "1.3.6.1.2.1.1.2.0")
							{
								this.NodeInfo.SysObjectId = coid.Value;
								this.NodeInfo.MachineType = NodeDetailHelper.GetSWDiscoveryVendor(coid.Value);
								this.NodeInfo.Vendor = NodeDetailHelper.GetVendorName(coid.Value);
							}
							else if (coid.OID == "1.3.6.1.2.1.1.1.0")
							{
								this.NodeInfo.Description = coid.Value;
							}
							else if (coid.OID == "1.3.6.1.2.1.1.6.0")
							{
								this.NodeInfo.SysLocation = coid.Value;
							}
							else if (coid.OID == "1.3.6.1.2.1.1.5.0")
							{
								this.NodeInfo.SysName = coid.Value;
							}
							else if (coid.OID == "1.3.6.1.2.1.1.4.0")
							{
								this.NodeInfo.SysContact = coid.Value;
							}
							else if (coid.Value.StartsWith("1.3.6.1.4.1.6876.60.1"))
							{
								this.NodeInfo.SysObjectId = coid.Value;
								this.NodeInfo.Vendor = NodeDetailHelper.GetSWDiscoveryVendor(this.NodeInfo.SysObjectId);
								Response.OIDs.Clear();
								Response.OIDs.Add("1.3.6.1.4.1.6876.1.1.0");
								Response.OIDs.Add("1.3.6.1.4.1.6876.1.2.0");
								int num = 0;
								string empty = string.Empty;
								this.mSNMP.BeginQuery(new SNMPRequest(Response), true, out num, out empty);
							}
							else if (coid.OID == "1.3.6.1.4.1.6876.1.1.0")
							{
								this.NodeInfo.MachineType = coid.Value;
							}
							else if (coid.OID == "1.3.6.1.4.1.6876.1.2.0")
							{
								this.NodeInfo.IOSVersion = coid.Value;
							}
						}
					}
				}
			}
		}

		// Token: 0x060003D3 RID: 979 RVA: 0x00019620 File Offset: 0x00017820
		private bool DetermineWirelessSupport()
		{
			bool flag = false;
			using (ResourceLister.log.Block())
			{
				if (!File.Exists(Path.Combine(OrionConfiguration.InstallPath, "WirelessNetworks\\WirelessPollingService.exe")))
				{
					return flag;
				}
				ResourceLister.log.Debug("List resources: preparing wireless support request");
				SNMPRequest newDefaultSNMPRequest = this.GetNewDefaultSNMPRequest();
				newDefaultSNMPRequest.NodeID = "Wireless";
				newDefaultSNMPRequest.OIDs.Add("1.2.840.10036.1.1.1.1.");
				newDefaultSNMPRequest.Community = this.mNetworkNode.ReadOnlyCredentials.CommunityString;
				newDefaultSNMPRequest.IPAddress = this.mNetworkNode.IpAddress;
				newDefaultSNMPRequest.TargetPort = (int)this.mNetworkNode.SNMPPort;
				newDefaultSNMPRequest.SNMPVersion = (int)this.mNetworkNode.SNMPVersion;
				if (this.mNetworkNode.SNMPVersion == SNMPVersion.SNMP3)
				{
					newDefaultSNMPRequest.SessionHandle = this.mSNMPV3SessionHandle;
				}
				SNMPResponse snmpresponse = this.mSNMP.Query(newDefaultSNMPRequest, true);
				if (snmpresponse.ErrorNumber == 0U && snmpresponse.OIDs.get_OtherItemFunc(0).HexValue.Length > 0 && snmpresponse.OIDs.get_OtherItemFunc(0).OID.Length >= 21 && snmpresponse.OIDs.get_OtherItemFunc(0).OID.Substring(0, 21) == "1.2.840.10036.1.1.1.1")
				{
					flag = true;
				}
			}
			return flag;
		}

		// Token: 0x060003D4 RID: 980 RVA: 0x00019788 File Offset: 0x00017988
		private void DetermineCPULoadSupport()
		{
			using (ResourceLister.log.Block())
			{
				ResourceLister.log.Debug("List resources: Preparing Cpu Load support query");
				SNMPRequest newDefaultSNMPRequest = this.GetNewDefaultSNMPRequest();
				COID coid = new COID();
				this.mCPUType = CPUPollerType.Unknown;
				newDefaultSNMPRequest.OIDs.Add("1.3.6.1.2.1.1.2.0");
				newDefaultSNMPRequest.Community = this.mNetworkNode.ReadOnlyCredentials.CommunityString;
				newDefaultSNMPRequest.IPAddress = this.mNetworkNode.IpAddress;
				newDefaultSNMPRequest.TargetPort = (int)this.mNetworkNode.SNMPPort;
				newDefaultSNMPRequest.SNMPVersion = (int)this.mNetworkNode.SNMPVersion;
				if (this.mNetworkNode.SNMPVersion == SNMPVersion.SNMP3)
				{
					newDefaultSNMPRequest.SessionHandle = this.mSNMPV3SessionHandle;
				}
				SNMPResponse snmpresponse = this.mSNMP.Query(newDefaultSNMPRequest, true);
				if (snmpresponse.ErrorNumber == 0U)
				{
					coid = snmpresponse.OIDs.get_OtherItemFunc(0);
					string value = coid.Value;
					if (value.Length > 0)
					{
						newDefaultSNMPRequest.OIDs.Clear();
						newDefaultSNMPRequest.OIDs.Add("1.3.6.1.4.1.6876.1.3.0");
						snmpresponse = this.mSNMP.Query(newDefaultSNMPRequest, true);
						if (snmpresponse.ErrorNumber == 0U)
						{
							coid = snmpresponse.OIDs.get_OtherItemFunc(0);
						}
						if (coid != null && coid.Value != null && coid.Value.StartsWith("1.3.6.1.4.1.6876.60.1"))
						{
							value = coid.Value;
							this.mCPUType = CPUPollerType.VMWareESX;
						}
						else if (OIDHelper.IsNexusDevice(value))
						{
							this.mCPUType = CPUPollerType.Cisco;
						}
						else if (value.StartsWith("1.3.6.1.4.1.9."))
						{
							newDefaultSNMPRequest.OIDs.Clear();
							newDefaultSNMPRequest.OIDs.Add("1.3.6.1.4.1.9.9.109.1.1.1.1.4.");
							newDefaultSNMPRequest.QueryType = 1;
							snmpresponse = this.mSNMP.Query(newDefaultSNMPRequest, true);
							if (snmpresponse.ErrorNumber == 0U)
							{
								coid = snmpresponse.OIDs.get_OtherItemFunc(0);
								if (coid.OID.StartsWith("1.3.6.1.4.1.9.9.109.1.1.1.1.4."))
								{
									this.mCPUType = CPUPollerType.Cisco;
								}
								else
								{
									this.DetermineCPULoadSupportTryOldCisco(newDefaultSNMPRequest, snmpresponse);
								}
							}
							else
							{
								this.DetermineCPULoadSupportTryOldCisco(newDefaultSNMPRequest, snmpresponse);
							}
						}
						else if (value.StartsWith("1.3.6.1.4.1.1991."))
						{
							newDefaultSNMPRequest.OIDs.Clear();
							newDefaultSNMPRequest.OIDs.Add("1.3.6.1.4.1.1991.1.1.2.1.52.0");
							snmpresponse = this.mSNMP.Query(newDefaultSNMPRequest, true);
							if (snmpresponse.ErrorNumber == 0U)
							{
								coid = newDefaultSNMPRequest.OIDs.get_OtherItemFunc(0);
								if (coid.OID.StartsWith("1.3.6.1.4.1.1991.1.1.2.1.52.0"))
								{
									this.mCPUType = CPUPollerType.Cisco;
								}
							}
						}
						else if (value.StartsWith("1.3.6.1.4.1.1916."))
						{
							newDefaultSNMPRequest.OIDs.Clear();
							newDefaultSNMPRequest.OIDs.Add("1.3.6.1.4.1.1916.1.1.1.28.0");
							snmpresponse = this.mSNMP.Query(newDefaultSNMPRequest, true);
							if (snmpresponse.ErrorNumber == 0U)
							{
								coid = snmpresponse.OIDs.get_OtherItemFunc(0);
								if (coid.OID.StartsWith("1.3.6.1.4.1.1916.1.1.1.28.0"))
								{
									this.mCPUType = CPUPollerType.Cisco;
								}
							}
						}
						else if (value.StartsWith("1.3.6.1.4.1.2272."))
						{
							newDefaultSNMPRequest.OIDs.Clear();
							newDefaultSNMPRequest.OIDs.Add("1.3.6.1.4.1.2272.1.1.20.0");
							snmpresponse = this.mSNMP.Query(newDefaultSNMPRequest, true);
							if (snmpresponse.ErrorNumber == 0U)
							{
								coid = snmpresponse.OIDs.get_OtherItemFunc(0);
								if (coid.OID.StartsWith("1.3.6.1.4.1.2272.1.1.20.0"))
								{
									this.mCPUType = CPUPollerType.Cisco;
								}
							}
						}
						else if (value.StartsWith("1.3.6.1.4.1.4981."))
						{
							newDefaultSNMPRequest.OIDs.Clear();
							newDefaultSNMPRequest.OIDs.Add("1.3.6.1.4.1.4981.1.20.1.1.1.8.");
							newDefaultSNMPRequest.QueryType = 1;
							snmpresponse = this.mSNMP.Query(newDefaultSNMPRequest, true);
							if (snmpresponse.ErrorNumber == 0U)
							{
								coid = snmpresponse.OIDs.get_OtherItemFunc(0);
								if (coid.OID.StartsWith("1.3.6.1.4.1.4981.1.20.1.1.1.8."))
								{
									this.mCPUType = CPUPollerType.Cisco;
								}
							}
						}
						else if (value.StartsWith("1.3.6.1.4.1.4998."))
						{
							newDefaultSNMPRequest.OIDs.Clear();
							newDefaultSNMPRequest.OIDs.Add("1.3.6.1.4.1.4998.1.1.5.3.1.1.1.2.");
							newDefaultSNMPRequest.QueryType = 1;
							newDefaultSNMPRequest.SNMPVersion = 2;
							snmpresponse = this.mSNMP.Query(newDefaultSNMPRequest, true);
							if (snmpresponse.ErrorNumber == 0U)
							{
								coid = snmpresponse.OIDs.get_OtherItemFunc(0);
								if (coid.OID.Substring(0, "1.3.6.1.4.1.4998.1.1.5.3.1.1.1.2.".Length) == "1.3.6.1.4.1.4981.1.20.1.1.1.8.")
								{
									this.mCPUType = CPUPollerType.Cisco;
								}
							}
						}
						else if (value.StartsWith("1.3.6.1.4.1.25506.1.") || value.StartsWith("1.3.6.1.4.1.2011."))
						{
							this.mCPUType = CPUPollerType.Huawei;
						}
						else
						{
							newDefaultSNMPRequest.OIDs.Clear();
							newDefaultSNMPRequest.OIDs.Add("1.3.6.1.2.1.25.3.3.1.2.0");
							newDefaultSNMPRequest.QueryType = 1;
							snmpresponse = this.mSNMP.Query(newDefaultSNMPRequest, true);
							if (snmpresponse.ErrorNumber == 0U)
							{
								coid = snmpresponse.OIDs.get_OtherItemFunc(0);
								if (coid.OID.StartsWith("1.3.6.1.2.1.25.3.3.1.2") && Information.IsNumeric(coid.Value))
								{
									this.mCPUType = CPUPollerType.Host;
								}
								else
								{
									this.DetermineCPULoadSupportTryNETSNMP(newDefaultSNMPRequest, snmpresponse);
								}
							}
							else
							{
								this.DetermineCPULoadSupportTryNETSNMP(newDefaultSNMPRequest, snmpresponse);
							}
						}
					}
				}
			}
		}

		// Token: 0x060003D5 RID: 981 RVA: 0x00019CCC File Offset: 0x00017ECC
		private void DetermineCPULoadSupportTryOldCisco(SNMPRequest CPUSNMPRequest, SNMPResponse mSNMPResponse)
		{
			CPUSNMPRequest.OIDs.Clear();
			CPUSNMPRequest.OIDs.Add("1.3.6.1.4.1.9.2.1.57.0");
			CPUSNMPRequest.QueryType = 0;
			mSNMPResponse = this.mSNMP.Query(CPUSNMPRequest, true);
			if (mSNMPResponse.ErrorNumber == 0U && mSNMPResponse.OIDs.get_OtherItemFunc(0).OID.StartsWith("1.3.6.1.4.1.9.2.1.57.0"))
			{
				this.mCPUType = CPUPollerType.Cisco;
			}
		}

		// Token: 0x060003D6 RID: 982 RVA: 0x00019D38 File Offset: 0x00017F38
		private void DetermineCPULoadSupportTryNETSNMP(SNMPRequest CPUSNMPRequest, SNMPResponse mSNMPResponse)
		{
			CPUSNMPRequest.OIDs.Clear();
			CPUSNMPRequest.OIDs.Add("1.3.6.1.4.1.2021.11.11.0");
			CPUSNMPRequest.QueryType = 0;
			mSNMPResponse = this.mSNMP.Query(CPUSNMPRequest, true);
			if (mSNMPResponse.ErrorNumber == 0U)
			{
				COID coid = mSNMPResponse.OIDs.get_OtherItemFunc(0);
				if (coid.OID.StartsWith("1.3.6.1.4.1.2021.11.11.0") && Information.IsNumeric(coid.Value))
				{
					this.mCPUType = CPUPollerType.Host;
				}
				else
				{
					this.DetermineCPULoadSupportTryNewNETSNMP(CPUSNMPRequest);
				}
				return;
			}
			this.DetermineCPULoadSupportTryNewNETSNMP(CPUSNMPRequest);
		}

		// Token: 0x060003D7 RID: 983 RVA: 0x00019DC4 File Offset: 0x00017FC4
		private void DetermineCPULoadSupportTryNewNETSNMP(SNMPRequest lCPUSNMPReuest)
		{
			lCPUSNMPReuest.OIDs.Clear();
			lCPUSNMPReuest.OIDs.Add("1.3.6.1.4.1.2021.11.53.0");
			lCPUSNMPReuest.QueryType = 0;
			SNMPResponse snmpresponse = this.mSNMP.Query(lCPUSNMPReuest, true);
			if (snmpresponse.ErrorNumber == 0U)
			{
				COID coid = snmpresponse.OIDs.get_OtherItemFunc(0);
				if (coid.OID.StartsWith("1.3.6.1.4.1.2021.11.53.0") && Information.IsNumeric(coid.Value))
				{
					this.mCPUType = CPUPollerType.Host;
				}
			}
		}

		// Token: 0x060003D8 RID: 984 RVA: 0x00019E40 File Offset: 0x00018040
		private void AddCPUBranch()
		{
			ResourceLister.log.Debug("List resources: Adding `Cpu and Memory` resource");
			Resource resource = new Resource();
			resource.Name = "CPU and Memory Utilization";
			if (this.mCPUType == CPUPollerType.VMWareESX)
			{
				Resource resource2 = resource;
				resource2.Name += " for VMWare ESX";
			}
			resource.Data = -1;
			if (this.mCPUType == CPUPollerType.Cisco)
			{
				resource.DataVariant = "Poller_CR";
			}
			else if (this.mCPUType == CPUPollerType.Host)
			{
				resource.DataVariant = "Poller_HT";
			}
			else if (this.mCPUType == CPUPollerType.VMWareESX)
			{
				resource.DataVariant = "Poller_VX";
			}
			else if (this.mCPUType == CPUPollerType.Huawei)
			{
				resource.DataVariant = "Poller_H3C";
			}
			this.result.Add(resource);
		}

		// Token: 0x060003D9 RID: 985 RVA: 0x00019EF8 File Offset: 0x000180F8
		private void AddWirelessBranch()
		{
			ResourceLister.log.Debug("List resources: Adding `Wireless` resource");
			Resource resource = new Resource();
			SqlCommand sqlCommand = new SqlCommand("Select WirelessAP From Nodes Where NodeID=" + this.mNetworkNode.ID.ToString());
			IDataReader dataReader;
			try
			{
				dataReader = SqlHelper.ExecuteReader(sqlCommand);
			}
			catch
			{
				return;
			}
			resource.Name = "Wireless Network Performance Monitoring";
			resource.Data = -1;
			resource.DataVariant = "Wireless";
			if (dataReader != null && !dataReader.IsClosed)
			{
				dataReader.Close();
			}
			this.result.Add(resource);
		}

		// Token: 0x060003DA RID: 986 RVA: 0x00019F94 File Offset: 0x00018194
		private bool DetermineVolumeUsageSupport()
		{
			bool flag = false;
			using (ResourceLister.log.Block())
			{
				ResourceLister.log.Debug("List resources: Preparing Volume Usage support request");
				SNMPRequest newDefaultSNMPRequest = this.GetNewDefaultSNMPRequest();
				new COID();
				newDefaultSNMPRequest.OIDs.Add("1.3.6.1.2.1.25.2.3.1.1");
				newDefaultSNMPRequest.QueryType = 1;
				newDefaultSNMPRequest.Community = this.mNetworkNode.ReadOnlyCredentials.CommunityString;
				newDefaultSNMPRequest.IPAddress = this.mNetworkNode.IpAddress;
				newDefaultSNMPRequest.TargetPort = (int)this.mNetworkNode.SNMPPort;
				newDefaultSNMPRequest.SNMPVersion = (int)this.mNetworkNode.SNMPVersion;
				if (this.mNetworkNode.SNMPVersion == SNMPVersion.SNMP3)
				{
					newDefaultSNMPRequest.SessionHandle = this.mSNMPV3SessionHandle;
				}
				SNMPResponse snmpresponse = this.mSNMP.Query(newDefaultSNMPRequest, true);
				if (snmpresponse.ErrorNumber == 0U)
				{
					if (snmpresponse.OIDs.get_OtherItemFunc(0).OID.StartsWith("1.3.6.1.2.1.25.2.3.1.1."))
					{
						flag = true;
					}
				}
				else
				{
					flag = false;
				}
			}
			return flag;
		}

		// Token: 0x060003DB RID: 987 RVA: 0x0001A09C File Offset: 0x0001829C
		private void AddVolumeBranch()
		{
			ResourceLister.log.Debug("List resources: Adding `Volumes` resource group");
			this.VolumeBranch.Name = "Volume Utilization";
			this.VolumeBranch.Data = -1;
			this.VolumeBranch.DataVariant = "Volumes";
			this.VolumeBranch.ResourceType = ResourceType.Volume;
			this.result.Add(this.VolumeBranch);
		}

		// Token: 0x060003DC RID: 988 RVA: 0x0001A104 File Offset: 0x00018304
		public void DiscoverVolumes()
		{
			ResourceLister.log.Debug("List resources: Discovering volumes resources");
			SNMPRequest newDefaultSNMPRequest = this.GetNewDefaultSNMPRequest();
			newDefaultSNMPRequest.Community = this.mNetworkNode.ReadOnlyCredentials.CommunityString;
			newDefaultSNMPRequest.IPAddress = this.mNetworkNode.IpAddress;
			newDefaultSNMPRequest.TargetPort = (int)this.mNetworkNode.SNMPPort;
			newDefaultSNMPRequest.SNMPVersion = (int)this.mNetworkNode.SNMPVersion;
			if (this.mNetworkNode.SNMPVersion == SNMPVersion.SNMP3)
			{
				newDefaultSNMPRequest.SessionHandle = this.mSNMPV3SessionHandle;
			}
			newDefaultSNMPRequest.QueryType = 1;
			newDefaultSNMPRequest.OIDs.Add("1.3.6.1.2.1.25.2.3.1.2");
			SNMPReply.ReplyDelegate callbackDelegate = new SNMPReply.ReplyDelegate(this.VolumesSNMPReply_Reply);
			newDefaultSNMPRequest.SetCallbackDelegate(callbackDelegate);
			int num = 0;
			string text = "";
			this.mSNMP.BeginQuery(newDefaultSNMPRequest, true, out num, out text);
		}

		// Token: 0x060003DD RID: 989 RVA: 0x0001A1D0 File Offset: 0x000183D0
		private void VolumesSNMPReply_Reply(SNMPResponse Response)
		{
			COID coid = new COID();
			if (Response.ErrorNumber == 0U)
			{
				coid = Response.OIDs.get_OtherItemFunc(0);
				if (coid.OID.StartsWith("1.3.6.1.2.1.25.2.3.1.2."))
				{
					int num = Convert.ToInt32(coid.OID.Substring("1.3.6.1.2.1.25.2.3.1.2".Length + 1, coid.OID.Length - "1.3.6.1.2.1.25.2.3.1.2".Length - 1));
					string value = coid.Value;
					uint num2 = <PrivateImplementationDetails>.ComputeStringHash(value);
					if (num2 <= 628700853U)
					{
						if (num2 <= 77191831U)
						{
							if (num2 <= 43636593U)
							{
								if (num2 != 10081355U)
								{
									if (num2 != 43636593U)
									{
										goto IL_3EF;
									}
									if (!(value == "1.3.6.1.4.1.23.2.27.2.1.3"))
									{
										goto IL_3EF;
									}
								}
								else
								{
									if (!(value == "1.3.6.1.4.1.23.2.27.2.1.1"))
									{
										goto IL_3EF;
									}
									goto IL_367;
								}
							}
							else if (num2 != 60414212U)
							{
								if (num2 != 77191831U)
								{
									goto IL_3EF;
								}
								if (!(value == "1.3.6.1.4.1.23.2.27.2.1.5"))
								{
									goto IL_3EF;
								}
							}
							else if (!(value == "1.3.6.1.4.1.23.2.27.2.1.4"))
							{
								goto IL_3EF;
							}
						}
						else if (num2 <= 110747069U)
						{
							if (num2 != 93969450U)
							{
								if (num2 != 110747069U)
								{
									goto IL_3EF;
								}
								if (!(value == "1.3.6.1.4.1.23.2.27.2.1.7"))
								{
									goto IL_3EF;
								}
							}
							else if (!(value == "1.3.6.1.4.1.23.2.27.2.1.6"))
							{
								goto IL_3EF;
							}
						}
						else if (num2 != 611923234U)
						{
							if (num2 != 628700853U)
							{
								goto IL_3EF;
							}
							if (!(value == "1.3.6.1.2.1.25.2.1.8"))
							{
								goto IL_3EF;
							}
							this.tempVolumesFound.Add(num, "RAMDisk");
							goto IL_400;
						}
						else
						{
							if (!(value == "1.3.6.1.2.1.25.2.1.9"))
							{
								goto IL_3EF;
							}
							this.tempVolumesFound.Add(num, "FlashMemory");
							goto IL_400;
						}
					}
					else if (num2 <= 695811329U)
					{
						if (num2 <= 662256091U)
						{
							if (num2 != 645478472U)
							{
								if (num2 != 662256091U)
								{
									goto IL_3EF;
								}
								if (!(value == "1.3.6.1.2.1.25.2.1.6"))
								{
									goto IL_3EF;
								}
								this.tempVolumesFound.Add(num, "FloppyDisk");
								goto IL_400;
							}
							else
							{
								if (!(value == "1.3.6.1.2.1.25.2.1.7"))
								{
									goto IL_3EF;
								}
								this.tempVolumesFound.Add(num, "CompactDisk");
								goto IL_400;
							}
						}
						else if (num2 != 679033710U)
						{
							if (num2 != 695811329U)
							{
								goto IL_3EF;
							}
							if (!(value == "1.3.6.1.2.1.25.2.1.4"))
							{
								goto IL_3EF;
							}
							goto IL_367;
						}
						else
						{
							if (!(value == "1.3.6.1.2.1.25.2.1.5"))
							{
								goto IL_3EF;
							}
							this.tempVolumesFound.Add(num, "RemovableDisk");
							goto IL_400;
						}
					}
					else if (num2 <= 729366567U)
					{
						if (num2 != 712588948U)
						{
							if (num2 != 729366567U)
							{
								goto IL_3EF;
							}
							if (!(value == "1.3.6.1.2.1.25.2.1.2"))
							{
								goto IL_3EF;
							}
						}
						else
						{
							if (!(value == "1.3.6.1.2.1.25.2.1.3"))
							{
								goto IL_3EF;
							}
							this.tempVolumesFound.Add(num, "VirtualMemory");
							goto IL_400;
						}
					}
					else if (num2 != 746144186U)
					{
						if (num2 != 2363632702U)
						{
							if (num2 != 4154050080U)
							{
								goto IL_3EF;
							}
							if (!(value == "1.3.6.1.4.1.23.2.27.2.1.8"))
							{
								goto IL_3EF;
							}
						}
						else
						{
							if (!(value == "1.3.6.1.2.1.25.2.1.10"))
							{
								goto IL_3EF;
							}
							this.tempVolumesFound.Add(num, "NetworkDisk");
							goto IL_400;
						}
					}
					else
					{
						if (!(value == "1.3.6.1.2.1.25.2.1.1"))
						{
							goto IL_3EF;
						}
						this.tempVolumesFound.Add(num, "Other");
						goto IL_400;
					}
					this.tempVolumesFound.Add(num, "RAM");
					goto IL_400;
					IL_367:
					this.tempVolumesFound.Add(num, "FixedDisk");
					goto IL_400;
					IL_3EF:
					this.tempVolumesFound.Add(num, "FixedDisk");
					IL_400:
					SNMPRequest snmprequest = new SNMPRequest(Response);
					snmprequest.OIDs.Clear();
					snmprequest.OIDs.Add(coid.OID);
					int num3 = 0;
					string text = "";
					this.mSNMP.BeginQuery(snmprequest, true, out num3, out text);
					return;
				}
				if (coid.OID.StartsWith("1.3.6.1.2.1.25.2.3.1.3."))
				{
					int num = Convert.ToInt32(coid.OID.Substring("1.3.6.1.2.1.25.2.3.1.3".Length + 1, coid.OID.Length - "1.3.6.1.2.1.25.2.3.1.3".Length - 1));
					string value2 = coid.Value;
					Response.OIDs.Clear();
					Response.OIDs.Add(coid.OID);
					int num4 = 0;
					string text2 = "";
					this.mSNMP.BeginQuery(new SNMPRequest(Response), true, out num4, out text2);
					int volumesDiscovered;
					if (this.tempVolumesFound.ContainsKey(num))
					{
						ResourceLister.log.Debug("List resources: Volume resource founded");
						Resource resource = new Resource();
						resource.Data = num;
						resource.DataVariant = "Poller_VO";
						resource.ResourceType = ResourceType.Volume;
						resource.Name = value2;
						resource.SubType = this.tempVolumesFound[num];
						this.VolumeBranch.Resources.Add(resource);
						ListResourcesStatus listResourcesStatus = this.status;
						volumesDiscovered = listResourcesStatus.VolumesDiscovered;
						listResourcesStatus.VolumesDiscovered = volumesDiscovered + 1;
						return;
					}
					ResourceLister.log.Debug("List resources: Volume resource founded");
					Resource resource2 = new Resource();
					resource2.Data = num;
					resource2.DataVariant = "Poller_VO";
					resource2.ResourceType = ResourceType.Volume;
					resource2.Name = value2;
					resource2.SubType = "FixedDisk";
					this.VolumeBranch.Resources.Add(resource2);
					ListResourcesStatus listResourcesStatus2 = this.status;
					volumesDiscovered = listResourcesStatus2.VolumesDiscovered;
					listResourcesStatus2.VolumesDiscovered = volumesDiscovered + 1;
				}
			}
		}

		// Token: 0x060003DE RID: 990 RVA: 0x0001A7A0 File Offset: 0x000189A0
		private void DiscoverInterfaces()
		{
			using (ResourceLister.log.Block())
			{
				this.InterfaceBranch.Name = "";
				this.InterfaceBranch.Data = -1;
				this.InterfaceBranch.DataVariant = "Interfaces";
				this.InterfaceBranch.ResourceType = ResourceType.Interface;
				this.result.Add(this.InterfaceBranch);
				SNMPRequest newDefaultSNMPRequest = this.GetNewDefaultSNMPRequest();
				newDefaultSNMPRequest.IPAddress = this.mNetworkNode.IpAddress;
				newDefaultSNMPRequest.Community = this.mNetworkNode.ReadOnlyCredentials.CommunityString;
				newDefaultSNMPRequest.SNMPVersion = (int)this.mNetworkNode.SNMPVersion;
				newDefaultSNMPRequest.TargetPort = (int)this.mNetworkNode.SNMPPort;
				if (this.mNetworkNode.SNMPVersion == SNMPVersion.SNMP3)
				{
					newDefaultSNMPRequest.SessionHandle = this.mSNMPV3SessionHandle;
				}
				newDefaultSNMPRequest.QueryType = 4;
				newDefaultSNMPRequest.MaxReps = 50;
				newDefaultSNMPRequest.OIDs.Add("1.3.6.1.2.1.2.2.1.2");
				SNMPReply.ReplyDelegate callbackDelegate = new SNMPReply.ReplyDelegate(this.InterfacesSNMPReply_Reply);
				newDefaultSNMPRequest.SetCallbackDelegate(callbackDelegate);
				int num = 0;
				string text = "";
				this.mSNMP.BeginQuery(newDefaultSNMPRequest, true, out num, out text);
			}
		}

		// Token: 0x060003DF RID: 991 RVA: 0x0001A8E4 File Offset: 0x00018AE4
		private void InterfacesSNMPReply_Reply(SNMPResponse Response)
		{
			COID coid = new COID();
			if (Response.ErrorNumber == 0U)
			{
				for (int i = 0; i < Response.OIDs.Count; i++)
				{
					coid = Response.OIDs.get_OtherItemFunc(i);
					if (coid.OID.StartsWith("1.3.6.1.2.1.2.2.1.2."))
					{
						coid.Value = coid.Value.Trim();
						int num = Convert.ToInt32(coid.OID.Substring("1.3.6.1.2.1.2.2.1.2".Length + 1, coid.OID.Length - "1.3.6.1.2.1.2.2.1.2".Length - 1));
						ResourceLister.log.DebugFormat("List resources: Interface resource founded {0}", num);
						ResourceInterface resourceInterface = new ResourceInterface();
						resourceInterface.ResourceType = ResourceType.Interface;
						resourceInterface.Data = num;
						resourceInterface.ifDescr = coid.Value.Replace("\0", "");
						this.InterfaceBranch.Resources.Add(resourceInterface);
						this.InterfaceDiscover((long)num);
					}
					else
					{
						this.interfacesfinished = true;
					}
				}
				if (!this.interfacesfinished)
				{
					Response.OIDs.Clear();
					Response.OIDs.Add(coid.OID);
					int num2 = 0;
					string text = "";
					SNMPRequest snmprequest = new SNMPRequest(Response);
					snmprequest.MaxReps = 50;
					snmprequest.QueryType = 4;
					this.mSNMP.BeginQuery(snmprequest, true, out num2, out text);
					return;
				}
			}
			else
			{
				ResourceLister.log.WarnFormat("Error encountered while listing resources: {0}", Response.ErrorNumber);
				if (Response.OIDs.Count > 0)
				{
					Dictionary<string, int> requestRetries = this.RequestRetries;
					lock (requestRetries)
					{
						if (this.RequestRetries.ContainsKey(Response.OIDs.get_OtherItemFunc(0).OID))
						{
							Dictionary<string, int> requestRetries2 = this.RequestRetries;
							string oid = Response.OIDs.get_OtherItemFunc(0).OID;
							int num3 = requestRetries2[oid];
							requestRetries2[oid] = num3 + 1;
						}
						else
						{
							this.RequestRetries[Response.OIDs.get_OtherItemFunc(0).OID] = 1;
						}
						if (this.RequestRetries[Response.OIDs.get_OtherItemFunc(0).OID] < 3)
						{
							ResourceLister.log.WarnFormat("Retrying Request {0}", Response.OIDs.get_OtherItemFunc(0).OID);
							int num4 = 0;
							string text2 = "";
							SNMPRequest newDefaultSNMPRequest = this.GetNewDefaultSNMPRequest();
							newDefaultSNMPRequest.IPAddress = this.mNetworkNode.IpAddress;
							newDefaultSNMPRequest.Community = this.mNetworkNode.ReadOnlyCredentials.CommunityString;
							newDefaultSNMPRequest.SNMPVersion = (int)this.mNetworkNode.SNMPVersion;
							newDefaultSNMPRequest.TargetPort = (int)this.mNetworkNode.SNMPPort;
							if (this.mNetworkNode.SNMPVersion == SNMPVersion.SNMP3)
							{
								newDefaultSNMPRequest.SessionHandle = this.mSNMPV3SessionHandle;
							}
							newDefaultSNMPRequest.QueryType = 1;
							newDefaultSNMPRequest.OIDs.Add(Response.OIDs.get_OtherItemFunc(0).OID);
							SNMPReply.ReplyDelegate callbackDelegate = new SNMPReply.ReplyDelegate(this.InterfacesSNMPReply_Reply);
							newDefaultSNMPRequest.SetCallbackDelegate(callbackDelegate);
							this.mSNMP.BeginQuery(newDefaultSNMPRequest, true, out num4, out text2);
							return;
						}
						ResourceLister.log.WarnFormat("Cannot get response after several retries. IP={0}, OID={1}", this.mNetworkNode.IpAddress, Response.OIDs.get_OtherItemFunc(0).OID);
						this.interfacesfinished = true;
						return;
					}
				}
				this.interfacesfinished = true;
			}
		}

		// Token: 0x060003E0 RID: 992 RVA: 0x0001AC64 File Offset: 0x00018E64
		private SNMPRequest GetNewDefaultSNMPRequest()
		{
			SNMPRequest snmprequest = new SNMPRequest();
			try
			{
				snmprequest.Timeout = TimeSpan.FromMilliseconds((double)(2 * Convert.ToInt32(OrionConfiguration.GetSetting("SNMP Timeout", 2500))));
				snmprequest.Retries = 1 + Convert.ToInt32(OrionConfiguration.GetSetting("SNMP Retries", 2));
			}
			catch
			{
			}
			return snmprequest;
		}

		// Token: 0x060003E1 RID: 993 RVA: 0x0001ACD4 File Offset: 0x00018ED4
		private SNMPRequest GetNewInterfaceSNMPRequest()
		{
			SNMPRequest newDefaultSNMPRequest = this.GetNewDefaultSNMPRequest();
			newDefaultSNMPRequest.Community = this.mNetworkNode.ReadOnlyCredentials.CommunityString;
			newDefaultSNMPRequest.IPAddress = this.mNetworkNode.IpAddress;
			newDefaultSNMPRequest.TargetPort = (int)this.mNetworkNode.SNMPPort;
			newDefaultSNMPRequest.SNMPVersion = (int)this.mNetworkNode.SNMPVersion;
			newDefaultSNMPRequest.QueryType = 0;
			if (newDefaultSNMPRequest.SNMPVersion == 3)
			{
				newDefaultSNMPRequest.SessionHandle = this.mSNMPV3SessionHandle;
			}
			return newDefaultSNMPRequest;
		}

		// Token: 0x060003E2 RID: 994 RVA: 0x0001AD50 File Offset: 0x00018F50
		private void InterfaceDiscover(long Index)
		{
			using (ResourceLister.log.Block())
			{
				int num = 0;
				string text = "";
				SNMPRequest newInterfaceSNMPRequest = this.GetNewInterfaceSNMPRequest();
				newInterfaceSNMPRequest.OIDs.Add("1.3.6.1.2.1.31.1.1.1.18." + Index);
				SNMPReply.ReplyDelegate callbackDelegate = new SNMPReply.ReplyDelegate(this.InterfaceSNMPReply_Reply);
				newInterfaceSNMPRequest.SetCallbackDelegate(callbackDelegate);
				this.mSNMP.BeginQuery(newInterfaceSNMPRequest, true, out num, out text);
				newInterfaceSNMPRequest = this.GetNewInterfaceSNMPRequest();
				newInterfaceSNMPRequest.OIDs.Add("1.3.6.1.2.1.31.1.1.1.1." + Index);
				newInterfaceSNMPRequest.OIDs.Add("1.3.6.1.2.1.2.2.1.8." + Index);
				newInterfaceSNMPRequest.OIDs.Add("1.3.6.1.2.1.2.2.1.3." + Index);
				newInterfaceSNMPRequest.OIDs.Add("1.3.6.1.2.1.2.2.1.4." + Index);
				newInterfaceSNMPRequest.OIDs.Add("1.3.6.1.2.1.2.2.1.6." + Index);
				newInterfaceSNMPRequest.OIDs.Add("1.3.6.1.2.1.3.1.1.2." + Index);
				newInterfaceSNMPRequest.OIDs.Add("1.3.6.1.2.1.2.2.1.5." + Index);
				newInterfaceSNMPRequest.OIDs.Add("1.3.6.1.2.1.2.2.1.10." + Index);
				newInterfaceSNMPRequest.OIDs.Add("1.3.6.1.2.1.2.2.1.14." + Index);
				newInterfaceSNMPRequest.SetCallbackDelegate(callbackDelegate);
				this.mSNMP.BeginQuery(newInterfaceSNMPRequest, true, out num, out text);
				ListResourcesStatus listResourcesStatus = this.status;
				int interfacesDiscovered = listResourcesStatus.InterfacesDiscovered;
				listResourcesStatus.InterfacesDiscovered = interfacesDiscovered + 1;
				ResourceLister.log.Debug("List resources: Discovering interface - Queries are sended. Waiting for replies.");
			}
		}

		// Token: 0x060003E3 RID: 995 RVA: 0x0001AF1C File Offset: 0x0001911C
		private ResourceInterface GetInterfaceByIndex(int i)
		{
			foreach (Resource resource in this.InterfaceBranch.Resources)
			{
				ResourceInterface resourceInterface = (ResourceInterface)resource;
				if (resourceInterface.Data == i)
				{
					return resourceInterface;
				}
			}
			return null;
		}

		// Token: 0x060003E4 RID: 996 RVA: 0x0001AF84 File Offset: 0x00019184
		private bool isResourceExist(ResourceInterface _interface, string DataVariant)
		{
			using (List<Resource>.Enumerator enumerator = _interface.Resources.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.DataVariant == DataVariant)
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x060003E5 RID: 997 RVA: 0x0001AFE4 File Offset: 0x000191E4
		private void InterfaceSNMPReply_Reply(SNMPResponse Response)
		{
			using (ResourceLister.log.Block())
			{
				COID coid = new COID();
				if (Response.ErrorNumber == 0U)
				{
					using (OIDIndexer enumerator = Response.OIDs.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							coid = enumerator.Current;
							if (coid != null && coid.Value != null)
							{
								int num = Convert.ToInt32(coid.OID.Substring(coid.OID.LastIndexOf(".") + 1));
								ResourceInterface interfaceByIndex = this.GetInterfaceByIndex(num);
								ResourceInterface resourceInterface = Response.UserObject as ResourceInterface;
								string oid = coid.OID;
								if (oid == "1.3.6.1.2.1.31.1.1.1.18." + num)
								{
									if (string.IsNullOrEmpty(interfaceByIndex.ifAlias))
									{
										interfaceByIndex.ifAlias = coid.Value;
									}
								}
								else if (oid == "1.3.6.1.2.1.31.1.1.1.1." + num)
								{
									interfaceByIndex.ifName = coid.Value;
									if (this.NodeInfo.Description.Contains("Cisco Catalyst Operating System") && interfaceByIndex.ifName.Contains("/"))
									{
										string[] array = interfaceByIndex.ifName.Split(new char[]
										{
											'/'
										});
										int num2;
										int num3;
										if (array.Length == 2 && int.TryParse(array[0], out num2) && int.TryParse(array[1], out num3))
										{
											SNMPRequest newInterfaceSNMPRequest = this.GetNewInterfaceSNMPRequest();
											newInterfaceSNMPRequest.OIDs.Add(string.Concat(new object[]
											{
												"1.3.6.1.4.1.9.5.1.4.1.1.4.",
												num2,
												".",
												num3
											}));
											newInterfaceSNMPRequest.SetCallbackDelegate(new SNMPReply.ReplyDelegate(this.InterfaceSNMPReply_Reply));
											newInterfaceSNMPRequest.UserObject = interfaceByIndex;
											int num4;
											string text;
											this.mSNMP.BeginQuery(newInterfaceSNMPRequest, true, out num4, out text);
										}
									}
								}
								else if (resourceInterface != null && resourceInterface.ifName != null && oid == "1.3.6.1.4.1.9.5.1.4.1.1.4." + resourceInterface.ifName.Replace('/', '.'))
								{
									resourceInterface.ifAlias = coid.Value;
								}
								else if (oid == "1.3.6.1.2.1.2.2.1.8." + num)
								{
									interfaceByIndex.ifOperStatus = Convert.ToInt32(coid.Value);
								}
								else if (coid.OID.StartsWith("1.3.6.1.2.1.2.2.1.14"))
								{
									if (coid.ValueType != 129 && coid.ValueType != 128 && !this.isResourceExist(interfaceByIndex, "Poller_IE"))
									{
										Resource resource = new Resource();
										resource.ResourceType = ResourceType.InterfacePoller;
										resource.Data = -1;
										resource.DataVariant = "Poller_IE";
										resource.Name = "Interface Error Statistics";
										interfaceByIndex.Resources.Add(resource);
									}
								}
								else if (coid.OID.StartsWith("1.3.6.1.2.1.2.2.1.10"))
								{
									if (coid.ValueType != 129 && coid.ValueType != 128 && !this.isResourceExist(interfaceByIndex, "Poller_IT"))
									{
										Resource resource2 = new Resource();
										resource2.ResourceType = ResourceType.InterfacePoller;
										resource2.Data = -1;
										resource2.DataVariant = "Poller_IT";
										resource2.Name = "Interface Traffic Statistics";
										interfaceByIndex.Resources.Add(resource2);
									}
								}
								else if (oid == "1.3.6.1.2.1.2.2.1.3." + num)
								{
									interfaceByIndex.ifType = Convert.ToInt32(coid.Value);
									interfaceByIndex.ifTypeName = DiscoveryDatabaseDAL.GetInterfaceTypeName(interfaceByIndex.ifType);
									interfaceByIndex.ifTypeDescription = DiscoveryDatabaseDAL.GetInterfaceTypeDescription(interfaceByIndex.ifType);
								}
								else if (oid == "1.3.6.1.2.1.2.2.1.6." + num || oid == "1.3.6.1.2.1.3.1.1.2." + num)
								{
									if (!string.IsNullOrEmpty(coid.Value))
									{
										interfaceByIndex.ifMACAddress = coid.Value;
									}
									if (string.IsNullOrEmpty(interfaceByIndex.ifMACAddress))
									{
										interfaceByIndex.ifMACAddress = coid.HexValue;
									}
								}
								else if (oid == "1.3.6.1.2.1.2.2.1.4." + num)
								{
									int ifMTU = 0;
									int.TryParse(coid.Value, out ifMTU);
									interfaceByIndex.ifMTU = ifMTU;
								}
								else if (oid == "1.3.6.1.2.1.2.2.1.5." + num)
								{
									interfaceByIndex.ifSpeed = coid.Value;
								}
							}
						}
						return;
					}
				}
				if (Response.ErrorNumber == 31040U)
				{
					ResourceLister.log.Warn("Timeout while processing interface details.");
					if (Response.OIDs.Count > 0)
					{
						Dictionary<string, int> requestRetries = this.RequestRetries;
						lock (requestRetries)
						{
							if (this.RequestRetries.ContainsKey(Response.OIDs.get_OtherItemFunc(0).OID))
							{
								Dictionary<string, int> requestRetries2 = this.RequestRetries;
								string oid2 = Response.OIDs.get_OtherItemFunc(0).OID;
								int num5 = requestRetries2[oid2];
								requestRetries2[oid2] = num5 + 1;
							}
							else
							{
								this.RequestRetries[Response.OIDs.get_OtherItemFunc(0).OID] = 1;
							}
							if (this.RequestRetries[Response.OIDs.get_OtherItemFunc(0).OID] < 3)
							{
								ResourceLister.log.WarnFormat("Retrying Request {0}", Response.OIDs.get_OtherItemFunc(0).OID);
								int num6 = 0;
								string text2 = "";
								SNMPRequest newInterfaceSNMPRequest2 = this.GetNewInterfaceSNMPRequest();
								newInterfaceSNMPRequest2.OIDs.Add(Response.OIDs.get_OtherItemFunc(0).OID);
								SNMPReply.ReplyDelegate callbackDelegate = new SNMPReply.ReplyDelegate(this.InterfaceSNMPReply_Reply);
								newInterfaceSNMPRequest2.SetCallbackDelegate(callbackDelegate);
								this.mSNMP.BeginQuery(newInterfaceSNMPRequest2, true, out num6, out text2);
							}
							else
							{
								ResourceLister.log.WarnFormat("Cannot get response after several retries. IP={0}, OID={1}", this.mNetworkNode.IpAddress, Response.OIDs.get_OtherItemFunc(0).OID);
							}
						}
					}
				}
			}
		}

		// Token: 0x040000D1 RID: 209
		private static Log log = new Log();

		// Token: 0x040000D2 RID: 210
		private Node mNetworkNode;

		// Token: 0x040000D3 RID: 211
		private CPUPollerType mCPUType;

		// Token: 0x040000D4 RID: 212
		private string mSysObjectID;

		// Token: 0x040000D5 RID: 213
		private CV3SessionHandle mSNMPV3SessionHandle;

		// Token: 0x040000D6 RID: 214
		private SNMPManagerWrapper mSNMP = new SNMPManagerWrapper();

		// Token: 0x040000D7 RID: 215
		private bool interfacesfinished;

		// Token: 0x040000D8 RID: 216
		private bool interfacesAllowed;

		// Token: 0x040000D9 RID: 217
		private Resources result = new Resources();

		// Token: 0x040000DA RID: 218
		private ListResourcesStatus status = new ListResourcesStatus();

		// Token: 0x040000DB RID: 219
		private Dictionary<int, string> tempVolumesFound = new Dictionary<int, string>();

		// Token: 0x040000DC RID: 220
		private Resource VolumeBranch = new Resource();

		// Token: 0x040000DD RID: 221
		private Resource InterfaceBranch = new Resource();

		// Token: 0x040000DE RID: 222
		private NodeInfoResource NodeInfo = new NodeInfoResource();

		// Token: 0x040000DF RID: 223
		private Dictionary<string, int> RequestRetries = new Dictionary<string, int>();

		// Token: 0x040000E0 RID: 224
		private const int maxRetries = 3;

		// Token: 0x040000E1 RID: 225
		private static Dictionary<Guid, ListResourcesStatus> mListResourcesStatuses = new Dictionary<Guid, ListResourcesStatus>();
	}
}
