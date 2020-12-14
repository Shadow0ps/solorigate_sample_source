using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SolarWinds.Common.Utility;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Actions.Utility;
using SolarWinds.Orion.Core.Common.DALs;
using SolarWinds.Orion.Core.Common.Models;
using SolarWinds.Orion.Core.Models.Actions;
using SolarWinds.Orion.Core.Models.Actions.Contexts;
using SolarWinds.Orion.Core.Models.MacroParsing;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000030 RID: 48
	public class ReportJobInitializer
	{
		// Token: 0x060003A4 RID: 932 RVA: 0x00017FE4 File Offset: 0x000161E4
		public static void AddActionsToScheduler(ReportJobConfiguration config, CoreBusinessLayerService service)
		{
			if (!config.Enabled)
			{
				return;
			}
			ReportingActionContext reportingContext = new ReportingActionContext
			{
				AccountID = config.AccountID,
				UrlsGroupedByLeftPart = ReportJobInitializer.GroupUrls(config),
				WebsiteID = config.WebsiteID
			};
			reportingContext.MacroContext.Add(new ReportingContext
			{
				AccountID = config.AccountID,
				ScheduleName = config.Name,
				ScheduleDescription = config.Description,
				LastRun = config.LastRun,
				WebsiteID = config.WebsiteID
			});
			reportingContext.MacroContext.Add(new GenericContext());
			int num = 0;
			if (config.Schedules != null)
			{
				TimerCallback <>9__0;
				foreach (ReportSchedule reportSchedule in config.Schedules)
				{
					DateTime dateTime = (reportSchedule.EndTime == null) ? DateTime.MaxValue : reportSchedule.EndTime.Value;
					string text = string.Format("ReportJob-{0}_{1}", config.ReportJobID, num);
					TimerCallback timerCallback;
					if ((timerCallback = <>9__0) == null)
					{
						timerCallback = (<>9__0 = delegate(object o)
						{
							ReportJobInitializer.log.Info("Starting action execution");
							foreach (ActionDefinition actionDefinition in config.Actions)
							{
								service.ExecuteAction(actionDefinition, reportingContext);
							}
							config.LastRun = new DateTime?(DateTime.Now.ToUniversalTime());
							ReportJobDAL.UpdateLastRun(config.ReportJobID, config.LastRun);
						});
					}
					ScheduledTask scheduledTask = new ScheduledTask(text, timerCallback, null, reportSchedule.CronExpression, reportSchedule.StartTime, dateTime, config.LastRun, reportSchedule.CronExpressionTimeZoneInfo);
					Scheduler.Instance.Add(scheduledTask, true);
					num++;
				}
			}
		}

		// Token: 0x060003A5 RID: 933 RVA: 0x000181D0 File Offset: 0x000163D0
		public static Dictionary<string, List<string>> GroupUrls(ReportJobConfiguration config)
		{
			StringBuilder errors = new StringBuilder();
			StringComparer strcmp = StringComparer.OrdinalIgnoreCase;
			Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>(strcmp);
			if (config == null)
			{
				ReportJobInitializer.log.ErrorFormat("GroupUrls(ReportJobConfiguration) config is NULL {0}", Environment.StackTrace);
				return dictionary;
			}
			try
			{
				List<string> list = (from report in config.Reports
				select string.Format("{0}/Orion/Report.aspx?ReportID={1}", WebsitesDAL.GetSiteAddress(config.WebsiteID), report.ID)).Union(config.Urls.Select(delegate(string url)
				{
					if (!url.Contains('?'))
					{
						return url + "?";
					}
					return url;
				})).ToList<string>();
				foreach (string text in list)
				{
					if (text.IndexOf("/Orion/", StringComparison.OrdinalIgnoreCase) < 0)
					{
						if (!dictionary.ContainsKey(OrionWebClient.UseDefaultWebsiteIdentifier))
						{
							dictionary.Add(OrionWebClient.UseDefaultWebsiteIdentifier, new List<string>());
						}
						dictionary[OrionWebClient.UseDefaultWebsiteIdentifier].Add(text);
					}
					else
					{
						string uriLeftPart;
						try
						{
							Uri uri;
							if (!Uri.TryCreate(text, UriKind.Absolute, out uri))
							{
								errors.AppendFormat("Invalid URL {0} \r\n", text);
								continue;
							}
							uriLeftPart = uri.GetLeftPart(UriPartial.Authority);
						}
						catch (Exception arg)
						{
							errors.AppendFormat("Invalid URL {0}. {1}\r\n", text, arg);
							continue;
						}
						if (!dictionary.ContainsKey(uriLeftPart))
						{
							dictionary.Add(uriLeftPart, list.Where(delegate(string u)
							{
								bool result;
								try
								{
									Uri uri2;
									if (!Uri.TryCreate(u, UriKind.Absolute, out uri2))
									{
										errors.AppendFormat("Invalid URL {0} \r\n", u);
										result = false;
									}
									else
									{
										string leftPart = uri2.GetLeftPart(UriPartial.Authority);
										result = strcmp.Equals(uriLeftPart, leftPart);
									}
								}
								catch (Exception arg3)
								{
									errors.AppendFormat("Invalid URL {0}. {1}\r\n", u, arg3);
									result = false;
								}
								return result;
							}).ToList<string>());
						}
					}
				}
			}
			catch (Exception arg2)
			{
				errors.AppendFormat("Unexpected exception {0}", arg2);
			}
			if (errors.Length > 0)
			{
				StringBuilder stringBuilder = new StringBuilder().AppendFormat("Errors in ReportJob-{0}({1}) @ Engine {2} & Website {3} \r\n", new object[]
				{
					config.ReportJobID,
					config.Name,
					config.EngineId,
					config.WebsiteID
				}).Append(errors);
				ReportJobInitializer.log.Error(stringBuilder);
			}
			return dictionary;
		}

		// Token: 0x040000C4 RID: 196
		private static readonly Log log = new Log();
	}
}
