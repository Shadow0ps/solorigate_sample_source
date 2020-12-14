using System;
using System.Data;
using System.Data.SqlClient;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer.DAL
{
	// Token: 0x020000AE RID: 174
	public class WebResourcesDAL
	{
		// Token: 0x06000889 RID: 2185 RVA: 0x0003E590 File Offset: 0x0003C790
		public static WebResources GetSpecificResources(int viewID, string queryFilterString)
		{
			string commandString = "SELECT * FROM Resources WHERE ViewID = @viewID" + (string.IsNullOrEmpty(queryFilterString.Trim()) ? "" : (" AND " + queryFilterString));
			SqlParameter[] sqlParamList = new SqlParameter[]
			{
				new SqlParameter("viewID", viewID)
			};
			return Collection<int, WebResource>.FillCollection<WebResources>(new Collection<int, WebResource>.CreateElement(WebResourcesDAL.CreateResource), commandString, sqlParamList);
		}

		// Token: 0x0600088A RID: 2186 RVA: 0x0003E5F4 File Offset: 0x0003C7F4
		public static void DeleteResource(int resourceId)
		{
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("DELETE FROM Resources WHERE ResourceID=@id"))
			{
				textCommand.Parameters.AddWithValue("id", resourceId);
				SqlHelper.ExecuteNonQuery(textCommand);
			}
			WebResourcesDAL.DeleteResourceProperties(resourceId);
		}

		// Token: 0x0600088B RID: 2187 RVA: 0x0003E64C File Offset: 0x0003C84C
		public static void DeleteResourceProperties(int resourceID)
		{
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("DELETE FROM ResourceProperties WHERE ResourceID=@ResourceID"))
			{
				textCommand.Parameters.AddWithValue("@ResourceID", resourceID);
				SqlHelper.ExecuteNonQuery(textCommand);
			}
		}

		// Token: 0x0600088C RID: 2188 RVA: 0x0003E6A0 File Offset: 0x0003C8A0
		public static int InsertNewResource(WebResource resource, int viewID)
		{
			int result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("\r\nINSERT INTO Resources (ViewID, ViewColumn, Position, ResourceName, ResourceFile, ResourceTitle, ResourceSubTitle)\r\nSELECT @viewID, @column, ISNULL(MAX(Position),0)+1, @resourceName, @resourceFile, @title, @subtitle\r\nFROM Resources where ViewID=@viewID and ViewColumn=@column\r\nSELECT SCOPE_IDENTITY()\r\n"))
			{
				textCommand.Parameters.AddWithValue("viewID", viewID);
				textCommand.Parameters.AddWithValue("column", resource.Column);
				textCommand.Parameters.AddWithValue("resourceName", resource.Name);
				textCommand.Parameters.AddWithValue("resourceFile", resource.File);
				textCommand.Parameters.AddWithValue("title", resource.Title);
				textCommand.Parameters.AddWithValue("subtitle", resource.SubTitle);
				result = Convert.ToInt32(SqlHelper.ExecuteScalar(textCommand));
			}
			return result;
		}

		// Token: 0x0600088D RID: 2189 RVA: 0x0003E770 File Offset: 0x0003C970
		public static void InsertNewResourceProperty(int resourceID, string propertyName, string propertyValue)
		{
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("INSERT INTO ResourceProperties (ResourceID, PropertyName, PropertyValue) VALUES (@ResourceID, @PropertyName, @PropertyValue)"))
			{
				textCommand.Parameters.AddWithValue("@ResourceID", resourceID);
				textCommand.Parameters.AddWithValue("@PropertyName", propertyName);
				textCommand.Parameters.AddWithValue("@PropertyValue", propertyValue);
				SqlHelper.ExecuteNonQuery(textCommand);
			}
		}

		// Token: 0x0600088E RID: 2190 RVA: 0x0003E7E8 File Offset: 0x0003C9E8
		public static void UpdateResourceProperty(int resourceID, string propertyName, string propertyValue)
		{
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("IF (EXISTS (SELECT * FROM ResourceProperties\r\n  WHERE ResourceID = @ResourceID AND PropertyName = @PropertyName))\r\nbegin\r\n  UPDATE ResourceProperties\r\n  SET PropertyValue = @PropertyValue\r\n  WHERE ResourceID = @ResourceID AND PropertyName = @PropertyName\r\nend\r\nelse\r\nbegin\r\nIF (EXISTS (SELECT * FROM ResourceProperties\r\n\tWHERE ResourceID = @ResourceID))\r\nbegin\r\n  INSERT INTO ResourceProperties (ResourceID, PropertyName, PropertyValue)\r\n  VALUES(@ResourceID, @PropertyName, @PropertyValue)\r\nend\r\nend "))
			{
				textCommand.Parameters.AddWithValue("@ResourceID", resourceID);
				textCommand.Parameters.AddWithValue("@PropertyName", propertyName);
				textCommand.Parameters.AddWithValue("@PropertyValue", propertyValue);
				SqlHelper.ExecuteNonQuery(textCommand);
			}
		}

		// Token: 0x0600088F RID: 2191 RVA: 0x0003E860 File Offset: 0x0003CA60
		public static string GetSpecificResourceProperty(int resourceID, string queryFilterString)
		{
			string result;
			using (SqlCommand textCommand = SqlHelper.GetTextCommand("SELECT TOP 1 PropertyValue FROM ResourceProperties WHERE ResourceID=@ResourceID" + (string.IsNullOrEmpty(queryFilterString.Trim()) ? "" : (" AND " + queryFilterString))))
			{
				textCommand.Parameters.AddWithValue("@ResourceID", resourceID);
				using (IDataReader dataReader = SqlHelper.ExecuteReader(textCommand))
				{
					if (dataReader.Read())
					{
						result = DatabaseFunctions.GetString(dataReader, "PropertyValue");
					}
					else
					{
						result = string.Empty;
					}
				}
			}
			return result;
		}

		// Token: 0x06000890 RID: 2192 RVA: 0x0003E90C File Offset: 0x0003CB0C
		private static WebResource CreateResource(IDataReader reader)
		{
			if (reader == null)
			{
				throw new ArgumentNullException("reader");
			}
			return new WebResource
			{
				Id = (int)reader["ResourceID"],
				Column = (int)((short)reader["ViewColumn"]),
				Position = (int)((short)reader["Position"]),
				Title = (string)reader["ResourceTitle"],
				SubTitle = (string)reader["ResourceSubTitle"],
				Name = (string)reader["ResourceName"],
				File = ((string)reader["ResourceFile"]).Trim()
			};
		}

		// Token: 0x04000267 RID: 615
		private static readonly Log log = new Log();
	}
}
