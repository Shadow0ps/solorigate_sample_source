using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using SolarWinds.Logging;
using SolarWinds.Orion.Discovery.Job;

namespace SolarWinds.Orion.Core.BusinessLayer.Discovery
{
	// Token: 0x0200007C RID: 124
	public class PartialDiscoveryResultsFilePersistence : IPartialDiscoveryResultsPersistence
	{
		// Token: 0x0600064F RID: 1615 RVA: 0x00025CF0 File Offset: 0x00023EF0
		public PartialDiscoveryResultsFilePersistence()
		{
		}

		// Token: 0x06000650 RID: 1616 RVA: 0x00025D0F File Offset: 0x00023F0F
		internal PartialDiscoveryResultsFilePersistence(string customPersistencePath)
		{
			this._persistenceFolderPath = customPersistencePath;
		}

		// Token: 0x06000651 RID: 1617 RVA: 0x00025D38 File Offset: 0x00023F38
		public bool SaveResult(Guid jobId, OrionDiscoveryJobResult result)
		{
			if (result == null)
			{
				throw new ArgumentNullException("result");
			}
			string text = null;
			try
			{
				text = this.GetResultsTempFileName(jobId);
				DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(OrionDiscoveryJobResult));
				using (AesCryptoServiceProvider aesCryptoServiceProvider = new AesCryptoServiceProvider())
				{
					aesCryptoServiceProvider.Key = this.GetEncryptionKey(jobId);
					aesCryptoServiceProvider.IV = this.GetEncryptionIV(aesCryptoServiceProvider, jobId);
					aesCryptoServiceProvider.Mode = CipherMode.CBC;
					aesCryptoServiceProvider.Padding = PaddingMode.PKCS7;
					using (ICryptoTransform cryptoTransform = aesCryptoServiceProvider.CreateEncryptor(aesCryptoServiceProvider.Key, aesCryptoServiceProvider.IV))
					{
						using (FileStream fileStream = new FileStream(text, FileMode.Create, FileAccess.Write))
						{
							using (CryptoStream cryptoStream = new CryptoStream(fileStream, cryptoTransform, CryptoStreamMode.Write))
							{
								using (GZipStream gzipStream = new GZipStream(cryptoStream, CompressionMode.Compress))
								{
									dataContractSerializer.WriteObject(gzipStream, result);
								}
							}
						}
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				PartialDiscoveryResultsFilePersistence._log.ErrorFormat("Error saving partial discovery result for job {0} to temporary file {1}. {2}", jobId, text ?? "<unable to get filename>", ex);
			}
			return false;
		}

		// Token: 0x06000652 RID: 1618 RVA: 0x00025E94 File Offset: 0x00024094
		public OrionDiscoveryJobResult LoadResult(Guid jobId)
		{
			string text = null;
			try
			{
				text = this.GetResultsTempFileName(jobId);
				DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(OrionDiscoveryJobResult));
				using (AesCryptoServiceProvider aesCryptoServiceProvider = new AesCryptoServiceProvider())
				{
					aesCryptoServiceProvider.Key = this.GetEncryptionKey(jobId);
					aesCryptoServiceProvider.IV = this.GetEncryptionIV(aesCryptoServiceProvider, jobId);
					aesCryptoServiceProvider.Mode = CipherMode.CBC;
					aesCryptoServiceProvider.Padding = PaddingMode.PKCS7;
					using (ICryptoTransform cryptoTransform = aesCryptoServiceProvider.CreateDecryptor(aesCryptoServiceProvider.Key, aesCryptoServiceProvider.IV))
					{
						using (FileStream fileStream = new FileStream(text, FileMode.Open, FileAccess.Read))
						{
							using (CryptoStream cryptoStream = new CryptoStream(fileStream, cryptoTransform, CryptoStreamMode.Read))
							{
								using (GZipStream gzipStream = new GZipStream(cryptoStream, CompressionMode.Decompress))
								{
									return (OrionDiscoveryJobResult)dataContractSerializer.ReadObject(gzipStream);
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				PartialDiscoveryResultsFilePersistence._log.ErrorFormat("Error loading partial discovery result for job {0} from temporary file {1}. {2}", jobId, text ?? "<unable to get filename>", ex);
			}
			return null;
		}

		// Token: 0x06000653 RID: 1619 RVA: 0x00025FDC File Offset: 0x000241DC
		public void DeleteResult(Guid jobId)
		{
			string text = null;
			try
			{
				text = this.GetResultsTempFileName(jobId);
				File.Delete(text);
			}
			catch (Exception ex)
			{
				PartialDiscoveryResultsFilePersistence._log.ErrorFormat("Error deleting partial discovery result for job {0} temporary file {1}. {2}", jobId, text ?? "<unable to get filename>", ex);
			}
		}

		// Token: 0x06000654 RID: 1620 RVA: 0x00026030 File Offset: 0x00024230
		public void ClearStore()
		{
			try
			{
				if (Directory.Exists(this._persistenceFolderPath))
				{
					Directory.Delete(this._persistenceFolderPath, true);
				}
			}
			catch (Exception ex)
			{
				PartialDiscoveryResultsFilePersistence._log.ErrorFormat("Error clearing partial discovery results persistence store '{0}'. {1}", this._persistenceFolderPath, ex);
			}
		}

		// Token: 0x06000655 RID: 1621 RVA: 0x00026084 File Offset: 0x00024284
		private byte[] GetEncryptionKey(Guid jobId)
		{
			return ProtectedData.Protect(Encoding.UTF8.GetBytes(jobId.ToString()), null, DataProtectionScope.LocalMachine).Take(32).ToArray<byte>();
		}

		// Token: 0x06000656 RID: 1622 RVA: 0x000260B0 File Offset: 0x000242B0
		private byte[] GetEncryptionIV(AesCryptoServiceProvider aesAlg, Guid jobId)
		{
			return Encoding.UTF8.GetBytes(jobId.ToString()).Take(aesAlg.BlockSize / 8).ToArray<byte>();
		}

		// Token: 0x06000657 RID: 1623 RVA: 0x000260DB File Offset: 0x000242DB
		private string GetResultsTempFileName(Guid jobId)
		{
			if (!Directory.Exists(this._persistenceFolderPath))
			{
				Directory.CreateDirectory(this._persistenceFolderPath);
			}
			return Path.Combine(this._persistenceFolderPath, jobId.ToString() + ".result");
		}

		// Token: 0x040001FF RID: 511
		private static readonly Log _log = new Log();

		// Token: 0x04000200 RID: 512
		private readonly string _persistenceFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SolarWinds/Discovery Engine/PartialResults");
	}
}
