using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SolarWinds.Logging;
using SolarWinds.Orion.Common;
using SolarWinds.Orion.Core.Common.Catalogs;

namespace SolarWinds.Orion.Core.BusinessLayer.BackgroundInventory
{
	// Token: 0x020000B6 RID: 182
	internal sealed class PluginsFactory<T>
	{
		// Token: 0x060008F2 RID: 2290 RVA: 0x00040648 File Offset: 0x0003E848
		public PluginsFactory() : this(OrionConfiguration.InstallPath, new string[]
		{
			"*.Pollers.dll",
			"*.Collector.dll",
			"*.Plugin.dll"
		})
		{
		}

		// Token: 0x060008F3 RID: 2291 RVA: 0x00040674 File Offset: 0x0003E874
		public PluginsFactory(string directory, params string[] filePatterns)
		{
			if (string.IsNullOrEmpty(directory))
			{
				throw new ArgumentNullException("directory");
			}
			this.Plugins = new List<T>();
			foreach (string searchPattern in filePatterns)
			{
				if (Directory.Exists(directory))
				{
					this.Process(Directory.EnumerateFiles(directory, searchPattern, SearchOption.TopDirectoryOnly));
					foreach (string path in Directory.EnumerateDirectories(directory))
					{
						this.Process(Directory.EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly));
					}
				}
			}
		}

		// Token: 0x17000122 RID: 290
		// (get) Token: 0x060008F4 RID: 2292 RVA: 0x00040718 File Offset: 0x0003E918
		// (set) Token: 0x060008F5 RID: 2293 RVA: 0x00040720 File Offset: 0x0003E920
		public List<T> Plugins { get; private set; }

		// Token: 0x060008F6 RID: 2294 RVA: 0x0004072C File Offset: 0x0003E92C
		private void Process(IEnumerable<string> files)
		{
			IEnumerable<PluginsFactory<T>.AssemblyVersionInfo> enumerable = (from name in files.Select(new Func<string, PluginsFactory<T>.AssemblyVersionInfo>(PluginsFactory<T>.AssemblyVersionInfo.Create))
			orderby name descending
			select name).Distinct<PluginsFactory<T>.AssemblyVersionInfo>();
			string basePath = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
			Dictionary<string, FileInfo> dictionary = (from plugin in (from n in new DirectoryXmlFilePluginsProvider(OrionConfiguration.InstallPath).ReadAreas()
			where n.Name.Equals("BackgroundInventory")
			select n).SelectMany((PluginsArea area) => area.Plugins)
			select new FileInfo(Path.Combine(basePath, plugin.AssemblyPath)) into info
			where info.Exists
			select info).ToDictionary((FileInfo info) => info.Name, StringComparer.OrdinalIgnoreCase);
			List<PluginsFactory<T>.AssemblyVersionInfo> list = new List<PluginsFactory<T>.AssemblyVersionInfo>();
			foreach (PluginsFactory<T>.AssemblyVersionInfo assemblyVersionInfo in enumerable)
			{
				try
				{
					FileInfo fileInfo;
					if (dictionary.TryGetValue(assemblyVersionInfo.FileInfo.Name, out fileInfo))
					{
						assemblyVersionInfo = new PluginsFactory<T>.AssemblyVersionInfo(fileInfo);
					}
					list.Add(assemblyVersionInfo);
				}
				catch (Exception ex)
				{
					PluginsFactory<T>.log.ErrorFormat("Failed to process {0} @ '{1}'. {2}", assemblyVersionInfo.AssemblyName, assemblyVersionInfo.FileInfo, ex);
				}
			}
			ResolveEventHandler value = new ResolveEventHandler(PluginsFactory<T>.CurrentDomain_ReflectionOnlyAssemblyResolve);
			AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += value;
			try
			{
				foreach (PluginsFactory<T>.AssemblyVersionInfo assemblyVersionInfo2 in list)
				{
					try
					{
						this.ProcessAssembly(assemblyVersionInfo2.FileInfo.FullName);
					}
					catch (Exception ex2)
					{
						PluginsFactory<T>.log.WarnFormat("Failed to initialize {0} @ '{1}'. {2}", assemblyVersionInfo2.AssemblyName, assemblyVersionInfo2.FileInfo, ex2);
					}
				}
			}
			finally
			{
				AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= value;
			}
		}

		// Token: 0x060008F7 RID: 2295 RVA: 0x00040988 File Offset: 0x0003EB88
		private static string FormatCodeBase(Assembly assembly)
		{
			string codeBase = PluginsFactory<T>.GetCodeBase(assembly);
			if (!string.IsNullOrWhiteSpace(codeBase))
			{
				return " at '" + codeBase + '\'';
			}
			return codeBase;
		}

		// Token: 0x060008F8 RID: 2296 RVA: 0x000409B8 File Offset: 0x0003EBB8
		private static string GetCodeBase(Assembly assembly)
		{
			if (assembly == null)
			{
				return string.Empty;
			}
			string codeBase = assembly.CodeBase;
			if (string.IsNullOrEmpty(codeBase))
			{
				return string.Empty;
			}
			try
			{
				Uri uri = new Uri(codeBase);
				if (uri.IsAbsoluteUri)
				{
					return uri.LocalPath;
				}
			}
			catch (Exception obj)
			{
				GC.KeepAlive(obj);
			}
			return codeBase;
		}

		// Token: 0x060008F9 RID: 2297 RVA: 0x00040A20 File Offset: 0x0003EC20
		private static string FormatRequest(Assembly requesting)
		{
			if (!(requesting != null))
			{
				return string.Empty;
			}
			return string.Concat(new object[]
			{
				" requested by [",
				requesting,
				"]",
				PluginsFactory<T>.FormatCodeBase(requesting)
			});
		}

		// Token: 0x060008FA RID: 2298 RVA: 0x00040A5C File Offset: 0x0003EC5C
		private static void ReportReflectionLoad(AssemblyName assmName, Assembly assembly, Assembly requesting)
		{
			if (PluginsFactory<T>.log.IsWarnEnabled && !StringComparer.Ordinal.Equals(assmName.FullName, assembly.FullName))
			{
				PluginsFactory<T>.log.WarnFormat("inspecting [{0}] as [{1}]{2}{3}", new object[]
				{
					assmName,
					assembly,
					PluginsFactory<T>.FormatCodeBase(assembly),
					PluginsFactory<T>.FormatRequest(requesting)
				});
				return;
			}
			if (PluginsFactory<T>.log.IsDebugEnabled)
			{
				PluginsFactory<T>.log.DebugFormat("inspecting [{0}] as [{1}]{2}{3}", new object[]
				{
					assmName,
					assembly,
					PluginsFactory<T>.FormatCodeBase(assembly),
					PluginsFactory<T>.FormatRequest(requesting)
				});
			}
		}

		// Token: 0x060008FB RID: 2299 RVA: 0x00040AF8 File Offset: 0x0003ECF8
		private static Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
		{
			AssemblyName assemblyName = new AssemblyName(args.Name);
			Assembly requestingAssembly = args.RequestingAssembly;
			List<Exception> list = new List<Exception>();
			try
			{
				Assembly assembly = Assembly.ReflectionOnlyLoad(args.Name);
				PluginsFactory<T>.ReportReflectionLoad(assemblyName, assembly, requestingAssembly);
				return assembly;
			}
			catch (Exception item)
			{
				list.Add(item);
			}
			foreach (Assembly assembly2 in AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies())
			{
				if (AssemblyName.ReferenceMatchesDefinition(assembly2.GetName(), assemblyName))
				{
					PluginsFactory<T>.ReportReflectionLoad(assemblyName, assembly2, requestingAssembly);
					return assembly2;
				}
			}
			if (requestingAssembly != null)
			{
				try
				{
					Uri uri = new Uri(requestingAssembly.CodeBase);
					if (uri.IsAbsoluteUri)
					{
						string text = Path.Combine(Path.GetDirectoryName(uri.LocalPath) ?? string.Empty, assemblyName.Name + ".dll");
						if (File.Exists(text))
						{
							AssemblyName assemblyName2 = AssemblyName.GetAssemblyName(text);
							if (AssemblyName.ReferenceMatchesDefinition(assemblyName, assemblyName2))
							{
								Assembly assembly3 = Assembly.ReflectionOnlyLoadFrom(text);
								PluginsFactory<T>.ReportReflectionLoad(assemblyName, assembly3, requestingAssembly);
								return assembly3;
							}
						}
					}
				}
				catch (Exception item2)
				{
					list.Add(item2);
				}
			}
			try
			{
				Assembly[] array = AppDomain.CurrentDomain.GetAssemblies();
				for (int i = 0; i < array.Length; i++)
				{
					AssemblyName name = array[i].GetName();
					if (AssemblyName.ReferenceMatchesDefinition(name, assemblyName))
					{
						Uri uri2 = new Uri(name.CodeBase);
						if (uri2.IsAbsoluteUri)
						{
							Assembly assembly4 = Assembly.ReflectionOnlyLoadFrom(uri2.LocalPath);
							PluginsFactory<T>.ReportReflectionLoad(assemblyName, assembly4, requestingAssembly);
							return assembly4;
						}
					}
				}
			}
			catch (Exception item3)
			{
				list.Add(item3);
			}
			if (!PluginsFactory<T>.log.IsErrorEnabled)
			{
				return null;
			}
			AggregateException ex = new AggregateException(new StringBuilder("inspecting [").Append(args.Name).Append(']').Append(PluginsFactory<T>.FormatRequest(requestingAssembly)).Append('.').ToString(), list);
			PluginsFactory<T>.log.WarnFormat("{0}", ex);
			return null;
		}

		// Token: 0x060008FC RID: 2300 RVA: 0x00040D1C File Offset: 0x0003EF1C
		private void ProcessAssembly(string fileName)
		{
			PluginsFactory<T>.log.DebugFormat("Loading plugins from {0}", fileName);
			AssemblyName assemblyName = AssemblyName.GetAssemblyName(fileName);
			Assembly assembly = null;
			foreach (Assembly assembly2 in AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies())
			{
				if (AssemblyName.ReferenceMatchesDefinition(assembly2.GetName(), assemblyName))
				{
					assembly = assembly2;
					try
					{
						string fullPath = Path.GetFullPath(new Uri(assembly.CodeBase).LocalPath);
						if (!StringComparer.OrdinalIgnoreCase.Equals(fileName, fullPath))
						{
							PluginsFactory<T>.log.WarnFormat("inspecting [{0}] at '{1}' as [{2}] at '{3}'", new object[]
							{
								assemblyName,
								fileName,
								assembly,
								fullPath
							});
						}
						break;
					}
					catch (Exception ex)
					{
						PluginsFactory<T>.log.WarnFormat("inspecting [{0}] at '{1}' as [{2}]. {3}", new object[]
						{
							assemblyName,
							fileName,
							assembly,
							ex
						});
						break;
					}
				}
			}
			if (assembly == null)
			{
				assembly = Assembly.ReflectionOnlyLoadFrom(fileName);
			}
			List<Type> list = PluginsFactory<T>.FindDerivedTypes<T>(assembly);
			if (list.Count != 0)
			{
				list = PluginsFactory<T>.FindDerivedTypes<T>(Assembly.LoadFrom(fileName));
			}
			foreach (Type type in list)
			{
				PluginsFactory<T>.log.DebugFormat("Creating plugin for {0}", type);
				T t = (T)((object)Activator.CreateInstance(type));
				this.Plugins.Add(t);
				string text = string.Empty;
				PropertyInfo property = t.GetType().GetProperty("FlagName");
				if (property != null)
				{
					text = (string)property.GetValue(t, null);
				}
				if (PluginsFactory<T>.log.IsInfoEnabled)
				{
					PluginsFactory<T>.log.InfoFormat("Loaded plugin {0} for {1} from {2}", type, text, fileName);
				}
			}
		}

		// Token: 0x060008FD RID: 2301 RVA: 0x00040EFC File Offset: 0x0003F0FC
		private static List<Type> FindDerivedTypes<K>(Assembly assembly)
		{
			Type[] types;
			try
			{
				types = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				types = ex.Types;
			}
			return (from n in types
			where n != null && !n.IsAbstract && !n.IsInterface && n.GetInterface(typeof(K).Name) != null
			select n).ToList<Type>();
		}

		// Token: 0x0400028C RID: 652
		private static readonly Log log = new Log();

		// Token: 0x020001A9 RID: 425
		[DebuggerDisplay("{ToString(),nq}")]
		private sealed class AssemblyVersionInfo : IComparable<PluginsFactory<T>.AssemblyVersionInfo>, IEquatable<PluginsFactory<T>.AssemblyVersionInfo>
		{
			// Token: 0x17000168 RID: 360
			// (get) Token: 0x06000C9C RID: 3228 RVA: 0x0004AFA7 File Offset: 0x000491A7
			// (set) Token: 0x06000C9D RID: 3229 RVA: 0x0004AFAF File Offset: 0x000491AF
			internal AssemblyName AssemblyName { get; private set; }

			// Token: 0x17000169 RID: 361
			// (get) Token: 0x06000C9E RID: 3230 RVA: 0x0004AFB8 File Offset: 0x000491B8
			// (set) Token: 0x06000C9F RID: 3231 RVA: 0x0004AFC0 File Offset: 0x000491C0
			internal Version FileVersion { get; private set; }

			// Token: 0x1700016A RID: 362
			// (get) Token: 0x06000CA0 RID: 3232 RVA: 0x0004AFC9 File Offset: 0x000491C9
			// (set) Token: 0x06000CA1 RID: 3233 RVA: 0x0004AFD1 File Offset: 0x000491D1
			internal FileInfo FileInfo { get; private set; }

			// Token: 0x06000CA2 RID: 3234 RVA: 0x0004AFDA File Offset: 0x000491DA
			internal static PluginsFactory<T>.AssemblyVersionInfo Create(string fileName)
			{
				return new PluginsFactory<T>.AssemblyVersionInfo(fileName);
			}

			// Token: 0x06000CA3 RID: 3235 RVA: 0x0004AFE4 File Offset: 0x000491E4
			internal AssemblyVersionInfo(FileInfo fileInfo)
			{
				this.FileInfo = fileInfo;
				this.AssemblyName = AssemblyName.GetAssemblyName(this.FileInfo.FullName);
				FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(this.FileInfo.FullName);
				this.FileVersion = new Version(versionInfo.FileMajorPart, versionInfo.FileMinorPart, versionInfo.FileBuildPart, versionInfo.FilePrivatePart);
			}

			// Token: 0x06000CA4 RID: 3236 RVA: 0x0004B048 File Offset: 0x00049248
			internal AssemblyVersionInfo(string fileName) : this(new FileInfo(Path.GetFullPath(fileName)))
			{
			}

			// Token: 0x06000CA5 RID: 3237 RVA: 0x0004B05C File Offset: 0x0004925C
			public int CompareTo(PluginsFactory<T>.AssemblyVersionInfo other)
			{
				int num = this.AssemblyName.Version.CompareTo(other.AssemblyName.Version);
				if (num == 0)
				{
					num = this.FileVersion.CompareTo(other.FileVersion);
				}
				return num;
			}

			// Token: 0x06000CA6 RID: 3238 RVA: 0x0004B09B File Offset: 0x0004929B
			public bool Equals(PluginsFactory<T>.AssemblyVersionInfo other)
			{
				return StringComparer.OrdinalIgnoreCase.Equals(this.AssemblyName.Name, other.AssemblyName.Name);
			}

			// Token: 0x06000CA7 RID: 3239 RVA: 0x0004B0BD File Offset: 0x000492BD
			public override string ToString()
			{
				return string.Concat(new object[]
				{
					this.AssemblyName,
					" @ ",
					this.FileInfo,
					':',
					this.FileVersion
				});
			}
		}
	}
}
