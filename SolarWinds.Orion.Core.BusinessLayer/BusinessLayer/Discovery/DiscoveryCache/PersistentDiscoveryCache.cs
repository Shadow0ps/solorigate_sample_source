using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Discovery;
using SolarWinds.Orion.Core.Models.DiscoveredObjects;
using SolarWinds.Orion.Core.Models.Discovery;

namespace SolarWinds.Orion.Core.BusinessLayer.Discovery.DiscoveryCache
{
	// Token: 0x0200007E RID: 126
	internal class PersistentDiscoveryCache : IPersistentDiscoveryCache
	{
		// Token: 0x0600065B RID: 1627 RVA: 0x00026124 File Offset: 0x00024324
		public DiscoveryResultItem GetResultForNode(int nodeId)
		{
			DiscoveryResultItem result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT CachedTime, CacheBlob FROM NodeListResourcesCache WHERE NodeId=@nodeId"))
			{
				textCommand.Parameters.AddWithValue("@nodeId", nodeId);
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					if (dataReader.Read())
					{
						result = this.DeserializeResults(nodeId, dataReader);
					}
					else
					{
						PersistentDiscoveryCache._log.DebugFormat("Cache for Node {0} not found", nodeId);
						result = null;
					}
				}
			}
			return result;
		}

		// Token: 0x0600065C RID: 1628 RVA: 0x000261B8 File Offset: 0x000243B8
		private DiscoveryResultItem DeserializeResults(int nodeId, IDataReader result)
		{
			Guid guid = Guid.NewGuid();
			DateTime dateTime = (DateTime)result[0];
			string s = (string)result[1];
			PersistentDiscoveryCache._log.DebugFormat("Found data in cache for Node {0} from {1}", nodeId, dateTime);
			DiscoveryResultItem discoveryResultItem = new DiscoveryResultItem(guid, new int?(nodeId), dateTime);
			DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(DiscoveredObjectTree));
			DiscoveryResultItem result2;
			try
			{
				using (MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(s)))
				{
					XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(memoryStream, Encoding.UTF8, new XmlDictionaryReaderQuotas(), null);
					discoveryResultItem.Progress = new OrionDiscoveryJobProgressInfo
					{
						Status = new DiscoveryComplexStatus(2, string.Empty),
						JobId = guid
					};
					discoveryResultItem.ResultTree = (DiscoveredObjectTree)dataContractSerializer.ReadObject(reader);
				}
				result2 = discoveryResultItem;
			}
			catch (Exception ex)
			{
				PersistentDiscoveryCache._log.Error("Error while deserializing result tree!", ex);
				result2 = null;
			}
			return result2;
		}

		// Token: 0x0600065D RID: 1629 RVA: 0x000262C8 File Offset: 0x000244C8
		public void StoreResultForNode(int nodeId, DiscoveryResultItem result)
		{
			DateTime now = DateTime.Now;
			try
			{
				DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(DiscoveredObjectTree));
				string @string;
				using (MemoryStream memoryStream = new MemoryStream())
				{
					dataContractSerializer.WriteObject(memoryStream, result.ResultTree);
					@string = Encoding.UTF8.GetString(memoryStream.ToArray());
				}
				using (SqlCommand textCommand = SqlHelper.GetTextCommand("UPDATE NodeListResourcesCache SET \r\nCachedTime = @time, CacheBlob = @blob WHERE NodeId=@nodeId\r\nIF @@ROWCOUNT = 0\r\n    INSERT INTO NodeListResourcesCache (NodeId, CachedTime, CacheBlob) VALUES(\r\n     @nodeId, @time, @blob\r\n    )"))
				{
					textCommand.Parameters.AddWithValue("@nodeId", nodeId);
					textCommand.Parameters.AddWithValue("@time", now);
					textCommand.Parameters.AddWithValue("@blob", @string);
					SqlHelper.ExecuteNonQuery(textCommand);
				}
			}
			catch (Exception ex)
			{
				PersistentDiscoveryCache._log.Error(string.Format("Error occured storing cached results for node [{0}]", nodeId), ex);
			}
		}

		// Token: 0x04000201 RID: 513
		private static readonly Log _log = new Log();
	}
}
