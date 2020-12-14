using System;
using System.Collections.Generic;
using System.Linq;
using SolarWinds.Orion.Core.Strings;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x02000090 RID: 144
	public sealed class PollerLimitNotificationItemDAL : NotificationItemDAL
	{
		// Token: 0x060006F3 RID: 1779 RVA: 0x0002C4AC File Offset: 0x0002A6AC
		public static PollerLimitNotificationItemDAL GetItem()
		{
			return NotificationItemDAL.GetItemById<PollerLimitNotificationItemDAL>(PollerLimitNotificationItemDAL.PollerStatusNotificationItemId);
		}

		// Token: 0x060006F4 RID: 1780 RVA: 0x0002C4B8 File Offset: 0x0002A6B8
		public static void Show(Dictionary<string, int> warningEngines, Dictionary<string, int> reachedEngines)
		{
			if (warningEngines == null)
			{
				throw new ArgumentNullException("warningEngines");
			}
			if (reachedEngines == null)
			{
				throw new ArgumentNullException("reachedEngines");
			}
			bool isWarning = reachedEngines.Count == 0;
			Guid typeId = (reachedEngines.Count > 0) ? PollerLimitNotificationItemDAL.PollerLimitReachedNotificationTypeGuid : PollerLimitNotificationItemDAL.PollerLimitWarningNotificationTypeGuid;
			PollerLimitNotificationItemDAL.EnginesStatus enginesStatus = new PollerLimitNotificationItemDAL.EnginesStatus(warningEngines, reachedEngines);
			string description = enginesStatus.Serialize();
			PollerLimitNotificationItemDAL item = PollerLimitNotificationItemDAL.GetItem();
			string url = "javascript:SW.Core.SalesTrigger.ShowPollerLimitPopupAsync();";
			if (item == null)
			{
				NotificationItemDAL.Insert(PollerLimitNotificationItemDAL.PollerStatusNotificationItemId, typeId, PollerLimitNotificationItemDAL.GetNotificationMessage(isWarning), description, false, url, null, null);
				return;
			}
			PollerLimitNotificationItemDAL.EnginesStatus value = new PollerLimitNotificationItemDAL.EnginesStatus(item.Description);
			if (enginesStatus.Extends(value))
			{
				item.SetNotAcknowledged();
			}
			item.TypeId = typeId;
			item.Description = description;
			item.Url = url;
			item.Title = PollerLimitNotificationItemDAL.GetNotificationMessage(isWarning);
			item.Update();
		}

		// Token: 0x060006F5 RID: 1781 RVA: 0x0002C591 File Offset: 0x0002A791
		public static void Hide()
		{
			NotificationItemDAL.Delete(PollerLimitNotificationItemDAL.PollerStatusNotificationItemId);
		}

		// Token: 0x060006F6 RID: 1782 RVA: 0x0002C59E File Offset: 0x0002A79E
		private static string GetNotificationMessage(bool isWarning)
		{
			if (!isWarning)
			{
				return PollerLimitNotificationItemDAL.LimitReachedTitle;
			}
			return PollerLimitNotificationItemDAL.WarningTitle;
		}

		// Token: 0x04000227 RID: 551
		public static readonly Guid PollerStatusNotificationItemId = new Guid("{C7070869-B2B8-42ED-8472-7F24056435D9}");

		// Token: 0x04000228 RID: 552
		public static readonly Guid PollerLimitWarningNotificationTypeGuid = new Guid("{68DF81BD-4025-4D7B-9296-C62C397AAC88}");

		// Token: 0x04000229 RID: 553
		public static readonly Guid PollerLimitReachedNotificationTypeGuid = new Guid("{25130585-7C09-4052-AF01-C706CC032940}");

		// Token: 0x0400022A RID: 554
		public static readonly string WarningTitle = Resources.COREBUSINESSLAYERDAL_CODE_YK0_1;

		// Token: 0x0400022B RID: 555
		public static readonly string LimitReachedTitle = Resources.COREBUSINESSLAYERDAL_CODE_YK0_2;

		// Token: 0x02000184 RID: 388
		private sealed class EnginesStatus
		{
			// Token: 0x17000161 RID: 353
			// (get) Token: 0x06000C35 RID: 3125 RVA: 0x0004A115 File Offset: 0x00048315
			private Dictionary<string, int> WarningEngines
			{
				get
				{
					return this.warningEngines;
				}
			}

			// Token: 0x17000162 RID: 354
			// (get) Token: 0x06000C36 RID: 3126 RVA: 0x0004A11D File Offset: 0x0004831D
			private Dictionary<string, int> LimitReachedEngines
			{
				get
				{
					return this.limitReachedEngines;
				}
			}

			// Token: 0x06000C37 RID: 3127 RVA: 0x0004A125 File Offset: 0x00048325
			public EnginesStatus(Dictionary<string, int> warn, Dictionary<string, int> reached)
			{
				this.warningEngines = warn;
				this.limitReachedEngines = reached;
			}

			// Token: 0x06000C38 RID: 3128 RVA: 0x0004A13C File Offset: 0x0004833C
			public EnginesStatus(string enginesStatusString)
			{
				new Dictionary<string, Dictionary<string, int>>();
				if (!string.IsNullOrEmpty(enginesStatusString))
				{
					List<string> list = enginesStatusString.Split(new string[]
					{
						"||"
					}, StringSplitOptions.None).ToList<string>();
					if (list.Count < 2)
					{
						throw new ArgumentException("enginesStatusString");
					}
					this.warningEngines = new Dictionary<string, int>();
					list[0].Split(new char[]
					{
						'|'
					}, StringSplitOptions.RemoveEmptyEntries).ToList<string>().ForEach(delegate(string g)
					{
						string[] array = g.Split(new char[]
						{
							';'
						});
						this.warningEngines[array[0]] = Convert.ToInt32(array[1]);
					});
					this.limitReachedEngines = new Dictionary<string, int>();
					list[1].Split(new char[]
					{
						'|'
					}, StringSplitOptions.RemoveEmptyEntries).ToList<string>().ForEach(delegate(string g)
					{
						string[] array = g.Split(new char[]
						{
							';'
						});
						this.limitReachedEngines[array[0]] = Convert.ToInt32(array[1]);
					});
				}
			}

			// Token: 0x06000C39 RID: 3129 RVA: 0x0004A204 File Offset: 0x00048404
			public string Serialize()
			{
				string text = string.Join("|", (from e in this.warningEngines
				select e.Key + ";" + e.Value).ToArray<string>());
				string separator = "||";
				string[] array = new string[2];
				array[0] = text;
				array[1] = string.Join("|", (from e in this.limitReachedEngines
				select e.Key + ";" + e.Value).ToArray<string>());
				return string.Join(separator, array);
			}

			// Token: 0x06000C3A RID: 3130 RVA: 0x0004A2A0 File Offset: 0x000484A0
			public bool Extends(PollerLimitNotificationItemDAL.EnginesStatus value)
			{
				return value == null || this.WarningEngines.Any((KeyValuePair<string, int> engine) => !value.WarningEngines.ContainsKey(engine.Key)) || this.LimitReachedEngines.Any((KeyValuePair<string, int> engine) => !value.LimitReachedEngines.ContainsKey(engine.Key));
			}

			// Token: 0x040004EB RID: 1259
			private Dictionary<string, int> warningEngines;

			// Token: 0x040004EC RID: 1260
			private Dictionary<string, int> limitReachedEngines;
		}
	}
}
