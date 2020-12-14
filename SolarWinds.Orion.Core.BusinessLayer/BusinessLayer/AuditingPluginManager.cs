using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common;
using SolarWinds.Orion.Core.Common.Indications;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200000B RID: 11
	internal sealed class AuditingPluginManager
	{
		// Token: 0x0600003D RID: 61 RVA: 0x00003BC4 File Offset: 0x00001DC4
		static AuditingPluginManager()
		{
			AppDomain.CurrentDomain.AssemblyResolve += AssemblySatelliteResolver.AssemblyResolve;
			AppDomain.CurrentDomain.AssemblyLoad += AssemblySatelliteResolver.AssemblyLoad;
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x0600003E RID: 62 RVA: 0x00003BFC File Offset: 0x00001DFC
		public ReadOnlyCollection<IAuditing2> AuditingInstances
		{
			get
			{
				if (!this.init)
				{
					throw new InvalidOperationException("Object has not been initialized yet. Call Start method before using.");
				}
				return this.auditingInstancesReadOnly;
			}
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00003C18 File Offset: 0x00001E18
		public IAuditing2 GetAuditingInstancesOfActionType(AuditActionType actionType)
		{
			if (!this.init)
			{
				throw new InvalidOperationException("Object has not been initialized yet. Call Start method before using.");
			}
			Func<AuditActionType, bool> <>9__0;
			foreach (KeyValuePair<string, IEnumerable<IAuditing2>> keyValuePair in this.cacheTypeInstancesReadOnly)
			{
				foreach (IAuditing2 auditing in keyValuePair.Value)
				{
					if (auditing != null)
					{
						IEnumerable<AuditActionType> supportedActionTypes = auditing.SupportedActionTypes;
						Func<AuditActionType, bool> predicate;
						if ((predicate = <>9__0) == null)
						{
							predicate = (<>9__0 = ((AuditActionType supportedType) => supportedType == actionType));
						}
						if (supportedActionTypes.Any(predicate))
						{
							return auditing;
						}
					}
				}
			}
			return null;
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00003CF8 File Offset: 0x00001EF8
		[Obsolete("Core-Split cleanup. If you need this member please contact Core team", true)]
		public IEnumerable<IAuditing2> GetAuditingInstancesOfType(string type)
		{
			if (!this.init)
			{
				throw new InvalidOperationException("Object has not been initialized yet. Call Start method before using.");
			}
			if (!this.cacheTypeInstancesReadOnly.ContainsKey(type))
			{
				AuditingPluginManager.log.ErrorFormat("Cache does not contain requested key {0}.", type);
				return Enumerable.Empty<IAuditing2>();
			}
			return this.cacheTypeInstancesReadOnly[type];
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00003D48 File Offset: 0x00001F48
		public void Initialize()
		{
			this.LoadPlugins();
			this.auditingInstancesReadOnly = this.auditingInstances.AsReadOnly();
			foreach (IAuditing2 auditing in this.auditingInstancesReadOnly)
			{
				string supportedIndicationType = auditing.SupportedIndicationType;
				if (this.cacheTypeInstances.ContainsKey(supportedIndicationType))
				{
					this.cacheTypeInstances[supportedIndicationType].Add(auditing);
				}
				else
				{
					this.cacheTypeInstances.Add(supportedIndicationType, new List<IAuditing2>
					{
						auditing
					});
				}
			}
			foreach (KeyValuePair<string, List<IAuditing2>> keyValuePair in this.cacheTypeInstances)
			{
				this.cacheTypeInstancesReadOnly.Add(keyValuePair.Key, keyValuePair.Value);
			}
			this.init = true;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00003E44 File Offset: 0x00002044
		private IEnumerable<Type> FindDerivedTypes(Assembly assembly)
		{
			if (AuditingPluginManager.log.IsTraceEnabled)
			{
				AuditingPluginManager.log.Trace("Processing assembly " + assembly.FullName);
			}
			Type[] source = null;
			try
			{
				if (AuditingPluginManager.log.IsTraceEnabled)
				{
					AuditingPluginManager.log.Trace("Calling GetTypes()");
				}
				source = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				AuditingPluginManager.log.Warn("Caught ReflectionTypeLoadException. Trying to get types from exception details.");
				foreach (Exception ex2 in ex.LoaderExceptions)
				{
					AuditingPluginManager.log.Warn("LoaderException message: " + ex2.Message + ((ex2 is TypeLoadException) ? (" (Type=" + ((TypeLoadException)ex2).TypeName + ")") : string.Empty));
				}
				source = ex.Types;
			}
			Type[] array = source.Where(new Func<Type, bool>(this.CheckAuditType)).ToArray<Type>();
			if (AuditingPluginManager.log.IsTraceEnabled)
			{
				AuditingPluginManager.log.Trace("Returning " + array.Length + " types.");
			}
			return array;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00003F74 File Offset: 0x00002174
		private bool CheckAuditType(Type c)
		{
			return c != null && typeof(IAuditing2).IsAssignableFrom(c) && !c.IsAbstract && !c.IsInterface;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00003FA4 File Offset: 0x000021A4
		private void LoadPlugins()
		{
			AuditingPluginManager.log.Trace("LoadPlugins");
			string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			AuditingPluginManager.log.DebugFormat("PluginDir:'{0}'", baseDirectory);
			if (!Directory.Exists(baseDirectory))
			{
				AuditingPluginManager.log.Warn(string.Format("Directory '{0}' was not found.", baseDirectory));
				return;
			}
			string[] array = null;
			try
			{
				if (AuditingPluginManager.log.IsVerboseEnabled)
				{
					AuditingPluginManager.log.Verbose("Searching files...");
				}
				array = Directory.GetFiles(baseDirectory, "*Auditing.dll", SearchOption.AllDirectories);
				if (AuditingPluginManager.log.IsVerboseEnabled)
				{
					AuditingPluginManager.log.Verbose("Searching files done.");
				}
			}
			catch (Exception ex)
			{
				AuditingPluginManager.log.ErrorFormat("GetFiles failed on '{0}'. Exception: {1}", baseDirectory, ex);
			}
			if (array == null)
			{
				return;
			}
			foreach (string text in array)
			{
				try
				{
					AuditingPluginManager.log.DebugFormat("Loading library '{0}'.", text);
					AssemblyName assemblyName = null;
					Assembly assembly = null;
					try
					{
						assemblyName = AssemblyName.GetAssemblyName(text);
						assembly = Assembly.Load(assemblyName);
					}
					catch (FileLoadException ex2)
					{
						AuditingPluginManager.log.WarnFormat("Unable to Load '{0}' - trying LoadFrom. {1}", assemblyName ?? text, ex2);
					}
					if (assembly == null)
					{
						assembly = Assembly.LoadFrom(text);
					}
					foreach (Type type in this.FindDerivedTypes(assembly))
					{
						IAuditing2 auditing = (IAuditing2)assembly.CreateInstance(type.FullName);
						if (auditing != null)
						{
							AuditingPluginManager.log.InfoFormat("Instance of {0} created.", type);
							this.auditingInstances.Add(auditing);
						}
						else
						{
							AuditingPluginManager.log.ErrorFormat("Instance of {0} coudn't be created. Library: '{1}'", type.FullName, text);
						}
					}
				}
				catch (Exception ex3)
				{
					AuditingPluginManager.log.ErrorFormat("Unable to Load library '{0}'. Exception: {1}", text, ex3);
				}
			}
		}

		// Token: 0x06000045 RID: 69 RVA: 0x000041A8 File Offset: 0x000023A8
		[Conditional("DEBUG")]
		private static void DebugAuditingPluginNPM(Type derivedType, IAuditing pluginInstance)
		{
			if (AuditingPluginManager.log.IsDebugEnabled && derivedType.FullName == "SolarWinds.NPM.Auditing.InterfaceAdded")
			{
				AuditDataContainer auditDataContainer = new AuditDataContainer(pluginInstance.SupportedActionTypes.First<AuditActionType>(), new Dictionary<string, string>
				{
					{
						"ObjectType",
						"dummy"
					}
				}, "dummy");
				string message = pluginInstance.GetMessage(auditDataContainer);
				AuditingPluginManager.log.DebugFormat("\"{0}::{1}\" Installed Successfully. Example message: \"{2}\".", pluginInstance.GetType(), pluginInstance.SupportedIndicationType, message);
				GC.KeepAlive(message);
			}
		}

		// Token: 0x0400001C RID: 28
		private static readonly Log log = new Log();

		// Token: 0x0400001D RID: 29
		private readonly List<IAuditing2> auditingInstances = new List<IAuditing2>();

		// Token: 0x0400001E RID: 30
		private ReadOnlyCollection<IAuditing2> auditingInstancesReadOnly;

		// Token: 0x0400001F RID: 31
		private bool init;

		// Token: 0x04000020 RID: 32
		private Dictionary<string, List<IAuditing2>> cacheTypeInstances = new Dictionary<string, List<IAuditing2>>();

		// Token: 0x04000021 RID: 33
		private Dictionary<string, IEnumerable<IAuditing2>> cacheTypeInstancesReadOnly = new Dictionary<string, IEnumerable<IAuditing2>>();
	}
}
