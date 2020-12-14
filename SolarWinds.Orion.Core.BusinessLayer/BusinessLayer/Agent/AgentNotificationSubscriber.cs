using System;
using System.Collections.Generic;
using SolarWinds.InformationService.Contract2;
using SolarWinds.InformationService.Contract2.PubSub;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common.InformationService;
using SolarWinds.Orion.Core.Common.Interfaces;

namespace SolarWinds.Orion.Core.BusinessLayer.Agent
{
	// Token: 0x020000BA RID: 186
	internal class AgentNotificationSubscriber : INotificationSubscriber
	{
		// Token: 0x06000925 RID: 2341 RVA: 0x00042020 File Offset: 0x00040220
		public AgentNotificationSubscriber(Action<int> onIndication) : this(onIndication, InformationServiceSubscriptionProviderShared.Instance())
		{
		}

		// Token: 0x06000926 RID: 2342 RVA: 0x00042030 File Offset: 0x00040230
		public AgentNotificationSubscriber(Action<int> onIndication, IInformationServiceSubscriptionProvider subscriptionProvider)
		{
			this.onIndication = onIndication;
			this.subscriptionProvider = subscriptionProvider;
		}

		// Token: 0x06000927 RID: 2343 RVA: 0x00042088 File Offset: 0x00040288
		public void Subscribe()
		{
			this.Unsubscribe();
			foreach (string text in this.subscriptionQueries)
			{
				this.subscriptionIds.Add(this.subscriptionProvider.Subscribe(text, this));
			}
		}

		// Token: 0x06000928 RID: 2344 RVA: 0x000420CC File Offset: 0x000402CC
		public void Unsubscribe()
		{
			while (this.subscriptionIds.Count > 0)
			{
				string text = this.subscriptionIds[0];
				if (!string.IsNullOrEmpty(text))
				{
					try
					{
						this.subscriptionProvider.Unsubscribe(text);
					}
					catch (Exception ex)
					{
						AgentNotificationSubscriber.log.ErrorFormat("Error unsubscribing 'agent change' subscription '{0}'. {1}", text, ex);
					}
				}
				this.subscriptionIds.RemoveAt(0);
			}
		}

		// Token: 0x06000929 RID: 2345 RVA: 0x0004213C File Offset: 0x0004033C
		public bool IsSubscribed()
		{
			return this.subscriptionIds.Count > 0;
		}

		// Token: 0x0600092A RID: 2346 RVA: 0x0004214C File Offset: 0x0004034C
		public void OnIndication(string subscriptionId, string indicationType, PropertyBag indicationProperties, PropertyBag sourceInstanceProperties)
		{
			if (!this.subscriptionIds.Contains(subscriptionId))
			{
				return;
			}
			try
			{
				int num = Convert.ToInt32(sourceInstanceProperties["AgentId"]);
				if (this.onIndication != null && num != 0)
				{
					this.onIndication(num);
				}
			}
			catch (Exception ex)
			{
				AgentNotificationSubscriber.log.ErrorFormat("Error processing agent notification. {0}", ex);
				throw;
			}
		}

		// Token: 0x0400029D RID: 669
		private static readonly Log log = new Log();

		// Token: 0x0400029E RID: 670
		private IInformationServiceSubscriptionProvider subscriptionProvider;

		// Token: 0x0400029F RID: 671
		private Action<int> onIndication;

		// Token: 0x040002A0 RID: 672
		private List<string> subscriptionIds = new List<string>();

		// Token: 0x040002A1 RID: 673
		private readonly string[] subscriptionQueries = new string[]
		{
			"SUBSCRIBE CHANGES TO Orion.AgentManagement.Agent INCLUDE AgentId WHEN AgentStatus CHANGES OR ConnectionStatus CHANGES",
			"SUBSCRIBE CHANGES TO Orion.AgentManagement.Agent INCLUDE AgentId WHEN ADDED",
			"SUBSCRIBE CHANGES TO Orion.AgentManagement.AgentPlugin INCLUDE AgentId WHEN Status CHANGES",
			"SUBSCRIBE CHANGES TO Orion.AgentManagement.AgentPlugin INCLUDE AgentId WHEN ADDED"
		};
	}
}
