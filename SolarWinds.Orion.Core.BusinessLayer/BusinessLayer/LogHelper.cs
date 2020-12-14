using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolarWinds.Logging;
using SolarWinds.Orion.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000028 RID: 40
	public class LogHelper
	{
		// Token: 0x06000356 RID: 854 RVA: 0x000149E4 File Offset: 0x00012BE4
		public static void DeleteOldLogs(object state)
		{
			LogHelper.log.InfoFormat("Deleting old log files", Array.Empty<object>());
			try
			{
				foreach (RemoveOldOnetimeJobResultsInfo removeOldOnetimeJobResultsInfo in (RemoveOldOnetimeJobResultsInfo[])state)
				{
					try
					{
						using (LogHelper.log.Block())
						{
							if (removeOldOnetimeJobResultsInfo.LogFilesPath == null)
							{
								LogHelper.log.InfoFormat("No directory specified.", Array.Empty<object>());
							}
							else
							{
								foreach (string text in removeOldOnetimeJobResultsInfo.LogFilesPath)
								{
									if (string.IsNullOrEmpty(text))
									{
										LogHelper.log.Info("No log file path found");
									}
									else
									{
										string directoryName = Path.GetDirectoryName(text);
										if (!Directory.Exists(directoryName))
										{
											LogHelper.log.InfoFormat("Directory {0} not found", directoryName);
										}
										else
										{
											foreach (string pattern in removeOldOnetimeJobResultsInfo.LogFileNamePattern)
											{
												LogHelper.DeleteOldFiles(directoryName, pattern, removeOldOnetimeJobResultsInfo.MaxLogFileAge);
											}
										}
									}
								}
							}
						}
					}
					catch (Exception ex)
					{
						LogHelper.log.Error("Unable to delete old log files", ex);
					}
				}
			}
			catch (Exception ex2)
			{
				LogHelper.log.Error("Unable to delete old log files", ex2);
			}
		}

		// Token: 0x06000357 RID: 855 RVA: 0x00014B6C File Offset: 0x00012D6C
		private static void TryDeleteFile(string fileName)
		{
			LogHelper.log.DebugFormat("Deleting log file {0}", fileName);
			try
			{
				File.Delete(fileName);
			}
			catch (IOException ex)
			{
				LogHelper.log.WarnFormat("Error deleting file {0} - ({1}).", fileName, ex.Message);
			}
		}

		// Token: 0x06000358 RID: 856 RVA: 0x00014BBC File Offset: 0x00012DBC
		public static void DeleteOldFiles(string path, string pattern, TimeSpan age)
		{
			using (LogHelper.log.Block())
			{
				if (path == null)
				{
					throw new ArgumentNullException("path");
				}
				if (pattern == null)
				{
					throw new ArgumentNullException("pattern");
				}
				if (LogHelper.log.IsDebugEnabled)
				{
					LogHelper.log.DebugFormat("path = {0}, pattern = {1}, age = {2}", path, pattern, age);
				}
				DateTime thresholdDate = DateTime.Now - age.Duration();
				IEnumerable<FileInfo> files = new DirectoryInfo(path).GetFiles(pattern);
				Func<FileInfo, bool> predicate;
				Func<FileInfo, bool> <>9__0;
				if ((predicate = <>9__0) == null)
				{
					predicate = (<>9__0 = ((FileInfo x) => x.LastWriteTime < thresholdDate));
				}
				foreach (FileInfo fileInfo in files.Where(predicate))
				{
					LogHelper.TryDeleteFile(fileInfo.FullName);
				}
			}
		}

		// Token: 0x040000AC RID: 172
		private static readonly Log log = new Log();
	}
}
