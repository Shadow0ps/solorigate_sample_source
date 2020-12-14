using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Xml;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Discovery.DataAccess;
using SolarWinds.Orion.Core.Models.Discovery;
using SolarWinds.Orion.Discovery.Contract.DiscoveryPlugin;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200001B RID: 27
	public static class DiscoveryResultManager
	{
		// Token: 0x060002C0 RID: 704 RVA: 0x00011310 File Offset: 0x0000F510
		public static DiscoveryResultBase GetDiscoveryResult(int profileId, IList<IDiscoveryPlugin> discoveryPlugins)
		{
			if (discoveryPlugins == null)
			{
				throw new ArgumentNullException("discoveryPlugins");
			}
			if (profileId <= 0)
			{
				throw new ArgumentException(string.Format("Invalid profile ID [{0}]", profileId));
			}
			DiscoveryResultBase discoveryResultBase = new DiscoveryResultBase();
			try
			{
				DiscoveryProfileEntry profileByID = DiscoveryProfileEntry.GetProfileByID(profileId);
				discoveryResultBase.EngineId = profileByID.EngineID;
				discoveryResultBase.ProfileID = profileByID.ProfileID;
			}
			catch (Exception ex)
			{
				string text = string.Format("Unable to load profile {0}", profileId);
				DiscoveryResultManager.log.Error(text, ex);
				throw new Exception(text, ex);
			}
			if (discoveryPlugins.Count == 0)
			{
				return discoveryResultBase;
			}
			int millisecondsTimeout = 300000;
			bool flag = Environment.StackTrace.Contains("ServiceModel");
			if (flag)
			{
				try
				{
					Configuration configuration = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.Load(configuration.FilePath);
					XmlNode xmlNode = xmlDocument.SelectSingleNode("/configuration/system.serviceModel/bindings/netTcpBinding/binding[@name=\"Core.NetTcpBinding\"]");
					if (xmlNode != null && xmlNode.Attributes != null)
					{
						millisecondsTimeout = (int)TimeSpan.Parse(xmlNode.Attributes["receiveTimeout"].Value).TotalMilliseconds;
					}
				}
				catch (Exception)
				{
					DiscoveryResultManager.log.Warn("Unable to read WCF timeout from Config file.");
				}
			}
			Thread thread = new Thread(new ParameterizedThreadStart(DiscoveryResultManager.LoadResults));
			DiscoveryResultManager.LoadResultsArgs loadResultsArgs = new DiscoveryResultManager.LoadResultsArgs
			{
				discoveryPlugins = discoveryPlugins,
				profileId = profileId,
				result = discoveryResultBase
			};
			thread.Start(loadResultsArgs);
			if (flag)
			{
				if (!thread.Join(millisecondsTimeout))
				{
					DiscoveryResultManager.log.Error("Loading results takes more time than WCF timeout is set. Enable debug logging to see which plugin takes too long.");
					return discoveryResultBase;
				}
			}
			else
			{
				thread.Join();
			}
			discoveryResultBase = loadResultsArgs.result;
			DiscoveryFilterResultByTechnology.FilterByPriority(discoveryResultBase, TechnologyManager.Instance);
			Stopwatch stopwatch = Stopwatch.StartNew();
			List<DiscoveryPluginResultBase> list = discoveryResultBase.PluginResults.ToList<DiscoveryPluginResultBase>();
			discoveryResultBase.PluginResults.Clear();
			foreach (DiscoveryPluginResultBase discoveryPluginResultBase in list)
			{
				discoveryResultBase.PluginResults.Add(discoveryPluginResultBase.GetFilteredPluginResult());
			}
			DiscoveryResultManager.log.DebugFormat("Filtering results took {0} milliseconds.", stopwatch.ElapsedMilliseconds);
			GC.Collect();
			return discoveryResultBase;
		}

		// Token: 0x060002C1 RID: 705 RVA: 0x00011544 File Offset: 0x0000F744
		private static void LoadResults(object args)
		{
			DiscoveryResultManager.LoadResultsArgs loadResultsArgs = (DiscoveryResultManager.LoadResultsArgs)args;
			Stopwatch stopwatch = new Stopwatch();
			foreach (IDiscoveryPlugin discoveryPlugin in loadResultsArgs.discoveryPlugins)
			{
				stopwatch.Restart();
				DiscoveryResultManager.log.DebugFormat("Loading results from plugin {0}", discoveryPlugin.GetType());
				DiscoveryPluginResultBase discoveryPluginResultBase = discoveryPlugin.LoadResults(loadResultsArgs.profileId);
				DiscoveryResultManager.log.DebugFormat("Loading results from plugin {0} took {1} milliseconds.", discoveryPlugin.GetType(), stopwatch.ElapsedMilliseconds);
				if (discoveryPluginResultBase == null)
				{
					throw new Exception(string.Format("unable to get valid result for plugin {0}", discoveryPlugin.GetType()));
				}
				discoveryPluginResultBase.PluginTypeName = discoveryPlugin.GetType().FullName;
				loadResultsArgs.result.PluginResults.Add(discoveryPluginResultBase);
			}
		}

		// Token: 0x060002C2 RID: 706 RVA: 0x00011624 File Offset: 0x0000F824
		private static string GetFilename(Type type)
		{
			Guid guid = Guid.NewGuid();
			return string.Format("C:\\{1}{0}.dat", guid, type);
		}

		// Token: 0x060002C3 RID: 707 RVA: 0x00011648 File Offset: 0x0000F848
		private static void XmlSerializer(DiscoveryResultBase data)
		{
			Type typeFromHandle = typeof(DiscoveryResultBase);
			DataContractSerializer dataContractSerializer = new DataContractSerializer(typeFromHandle);
			using (FileStream fileStream = new FileStream(DiscoveryResultManager.GetFilename(typeFromHandle), FileMode.Create))
			{
				XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(fileStream);
				dataContractSerializer.WriteObject(writer, data);
				fileStream.Flush();
				fileStream.Close();
			}
		}

		// Token: 0x060002C4 RID: 708 RVA: 0x000116AC File Offset: 0x0000F8AC
		private static void BinarySerializer(DiscoveryResultBase data)
		{
			FileStream fileStream = new FileStream(DiscoveryResultManager.GetFilename(typeof(DiscoveryResultBase)), FileMode.Create);
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			try
			{
				binaryFormatter.Serialize(fileStream, data);
			}
			finally
			{
				fileStream.Close();
			}
		}

		// Token: 0x0400007B RID: 123
		private static readonly Log log = new Log();

		// Token: 0x02000110 RID: 272
		private class LoadResultsArgs
		{
			// Token: 0x0400039C RID: 924
			public int profileId;

			// Token: 0x0400039D RID: 925
			public IList<IDiscoveryPlugin> discoveryPlugins;

			// Token: 0x0400039E RID: 926
			public DiscoveryResultBase result;
		}
	}
}
