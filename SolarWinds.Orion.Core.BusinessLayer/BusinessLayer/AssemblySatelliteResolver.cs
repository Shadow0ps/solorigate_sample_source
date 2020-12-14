using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common.i18n;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x0200000A RID: 10
	internal static class AssemblySatelliteResolver
	{
		// Token: 0x06000030 RID: 48 RVA: 0x00002FD4 File Offset: 0x000011D4
		internal static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
		{
			try
			{
				AssemblyName assemblyName = new AssemblyName(args.Name);
				if (!assemblyName.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
				{
					return null;
				}
				AssemblySatelliteResolver.IntRef intRef;
				bool flag = AssemblySatelliteResolver.EntryCount.GetOrAdd(assemblyName.FullName, (string _) => new AssemblySatelliteResolver.IntRef(0)).Increment() > 2 && !AssemblySatelliteResolver.ResolveCnt.TryGetValue(assemblyName.FullName, out intRef);
				if (flag)
				{
					AssemblySatelliteResolver.log.DebugFormat("Resolving satellite assembly \"{0}\"", assemblyName);
				}
				string[] array = assemblyName.Name.Split(new char[]
				{
					'.'
				});
				string sourceName = string.Join(".", array.Take(array.Length - 1));
				string resolveFileName = assemblyName.Name + ".dll";
				List<Assembly> list = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
				Assembly requestingAssembly = args.RequestingAssembly;
				if (requestingAssembly != null)
				{
					list.Remove(requestingAssembly);
					list.Insert(0, requestingAssembly);
				}
				foreach (Assembly assembly in list)
				{
					AssemblyName name = assembly.GetName();
					if (AssemblySatelliteResolver.SatelliteMatchesDefinition(assemblyName, name))
					{
						bool flag2 = AssemblySatelliteResolver.ResolveCnt.GetOrAdd(assembly.FullName, (string _) => new AssemblySatelliteResolver.IntRef(0)).Increment() == 1;
						if (flag || flag2)
						{
							AssemblySatelliteResolver.log.InfoFormat("Resolved \"{0}\" as \"{1}\" at \"{2}\"", assemblyName, name, AssemblySatelliteResolver.GetSymbolicLocation(assembly));
						}
						return assembly;
					}
				}
				Assembly result;
				if (AssemblySatelliteResolver.ProbeViaLoadedAssemblies(list, assemblyName, resolveFileName, requestingAssembly, sourceName, flag, out result))
				{
					return result;
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError("AssemblyResove failed. {0}", new object[]
				{
					ex
				});
				AssemblySatelliteResolver.log.FatalFormat("AssemblyResove failed. {0}", ex);
				GC.KeepAlive(ex);
			}
			return null;
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00003208 File Offset: 0x00001408
		internal static void AssemblyLoad(object sender, AssemblyLoadEventArgs args)
		{
			if (!AssemblySatelliteResolver.log.IsErrorEnabled)
			{
				return;
			}
			try
			{
				Assembly loadedAssembly = args.LoadedAssembly;
				AssemblyName name = loadedAssembly.GetName();
				StringBuilder stringBuilder = new StringBuilder();
				int num = 1;
				bool flag = name.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase);
				bool flag2 = name.Name.IndexOf("Auditing", StringComparison.OrdinalIgnoreCase) > -1;
				bool flag3 = name.Name.IndexOf(".Strings", StringComparison.OrdinalIgnoreCase) > -1;
				if (flag)
				{
					AssemblySatelliteResolver.IntRef intRef;
					if (!AssemblySatelliteResolver.EntryCount.TryGetValue(name.FullName, out intRef) && !AssemblySatelliteResolver.ResolveCnt.TryGetValue(name.FullName, out intRef))
					{
						num = 2;
					}
					string[] array = name.Name.Split(new char[]
					{
						'.'
					});
					string sourceName = string.Join(".", array.Take(array.Length - 1));
					Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly a) => a.GetName().Name.Equals(sourceName, StringComparison.OrdinalIgnoreCase));
					if (assembly != null)
					{
						Assembly left = null;
						try
						{
							left = assembly.GetSatelliteAssembly(name.CultureInfo);
						}
						catch (FileNotFoundException)
						{
						}
						catch (FileLoadException)
						{
						}
						if (left == null || left != loadedAssembly)
						{
							stringBuilder.AppendLine().AppendFormat("Unexpected satellite for \"{0}\" : \"{1}\"", loadedAssembly, name.CultureInfo.Name);
							num = 3;
						}
					}
				}
				if (flag || flag2 || flag3)
				{
					StringBuilder stringBuilder2 = new StringBuilder().AppendFormat("Loaded \"{0}\" at \"{1}\".{2}", name, AssemblySatelliteResolver.GetSymbolicLocation(loadedAssembly), AssemblySatelliteResolver.GetDebugStackTrace()).Append(stringBuilder);
					switch (num)
					{
					case 1:
						AssemblySatelliteResolver.log.Info(stringBuilder2);
						break;
					case 2:
						AssemblySatelliteResolver.log.Warn(stringBuilder2);
						break;
					case 3:
						AssemblySatelliteResolver.log.Error(stringBuilder2);
						break;
					}
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError("AssemblyLoad failed. {0}", new object[]
				{
					ex
				});
				AssemblySatelliteResolver.log.FatalFormat("AssemblyLoad failed. {0}", ex);
				GC.KeepAlive(ex);
			}
		}

		// Token: 0x06000032 RID: 50 RVA: 0x0000344C File Offset: 0x0000164C
		internal static bool SatelliteMatchesDefinition(AssemblyName reference, AssemblyName comparee)
		{
			if (!AssemblyName.ReferenceMatchesDefinition(reference, comparee))
			{
				return false;
			}
			if (reference.CultureInfo == null != (comparee.CultureInfo == null))
			{
				return false;
			}
			if (reference.CultureInfo != null && comparee.CultureInfo != null && !reference.CultureInfo.Equals(comparee.CultureInfo))
			{
				bool isNeutralCulture = reference.CultureInfo.IsNeutralCulture;
				bool isNeutralCulture2 = comparee.CultureInfo.IsNeutralCulture;
				if (isNeutralCulture && isNeutralCulture2)
				{
					return false;
				}
				object obj = isNeutralCulture ? reference.CultureInfo : reference.CultureInfo.Parent;
				CultureInfo obj2 = isNeutralCulture2 ? comparee.CultureInfo : comparee.CultureInfo.Parent;
				if (!obj.Equals(obj2))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000033 RID: 51 RVA: 0x000034F4 File Offset: 0x000016F4
		private static string NormalizePath(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return string.Empty;
			}
			return path.Trim(AssemblySatelliteResolver.PathWhiteSpace).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).TrimEnd(new char[]
			{
				Path.DirectorySeparatorChar
			}).ToUpperInvariant();
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00003541 File Offset: 0x00001741
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static object GetDebugStackTrace()
		{
			if (AssemblySatelliteResolver.log.IsDebugEnabled)
			{
				return Environment.NewLine + new StackTrace(1, false);
			}
			return string.Empty;
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00003568 File Offset: 0x00001768
		private static IEnumerable<Uri> GetAssemblyBaseUris(Assembly assembly)
		{
			List<Uri> list = new List<Uri>(2);
			try
			{
				if (assembly.IsDynamic || assembly.GlobalAssemblyCache)
				{
					return list;
				}
				string codeBase = assembly.CodeBase;
				if (!string.IsNullOrEmpty(codeBase))
				{
					list.Add(new Uri(codeBase, UriKind.Absolute));
				}
			}
			catch (NotImplementedException)
			{
			}
			catch (Exception ex)
			{
				AssemblySatelliteResolver.log.DebugFormat("Cannot get CodeBase of \"{0}\". {1}", assembly, ex);
			}
			try
			{
				string location = assembly.Location;
				if (!string.IsNullOrEmpty(location))
				{
					Uri locUri = new Uri(location, UriKind.Absolute);
					if (!list.Any((Uri uri) => uri.Equals(locUri)))
					{
						list.Add(locUri);
					}
				}
			}
			catch (NotImplementedException)
			{
			}
			catch (Exception ex2)
			{
				AssemblySatelliteResolver.log.DebugFormat("Cannot get Location of \"{0}\". {1}", assembly, ex2);
			}
			return list;
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00003664 File Offset: 0x00001864
		private static Uri GetAssemblyLocation(Assembly assembly)
		{
			return AssemblySatelliteResolver.GetAssemblyBaseUris(assembly).LastOrDefault<Uri>();
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00003674 File Offset: 0x00001874
		private static string GetSymbolicLocation(Assembly assembly)
		{
			Uri assemblyLocation = AssemblySatelliteResolver.GetAssemblyLocation(assembly);
			if (assemblyLocation != null)
			{
				return assemblyLocation.ToString();
			}
			if (assembly.IsDynamic)
			{
				return "«dynamic»";
			}
			if (assembly.GlobalAssemblyCache)
			{
				return "«GAC»";
			}
			return "«unknown»";
		}

		// Token: 0x06000038 RID: 56 RVA: 0x000036B9 File Offset: 0x000018B9
		private static IEnumerable<string> ExpandCulture(CultureInfo culture)
		{
			while (culture != null && !CultureInfo.InvariantCulture.Equals(culture))
			{
				yield return culture.Name;
				culture = culture.Parent;
			}
			yield return string.Empty;
			yield break;
		}

		// Token: 0x06000039 RID: 57 RVA: 0x000036CC File Offset: 0x000018CC
		private static bool ProbeViaLoadedAssemblies(ICollection<Assembly> loadedAssemblies, AssemblyName resolve, string resolveFileName, Assembly requesting, string sourceName, bool isLog, out Assembly resolvedAssembly)
		{
			bool first = AssemblySatelliteResolver.EntryCount.GetOrAdd(resolve.Name, (string _) => new AssemblySatelliteResolver.IntRef(0)).Increment() == 1;
			List<Uri> list = new List<Uri>(loadedAssemblies.Count);
			foreach (Assembly assembly in loadedAssemblies)
			{
				AssemblyName name = assembly.GetName();
				if (!(requesting != null) || requesting.Equals(assembly) || name.Name.Equals(sourceName, StringComparison.OrdinalIgnoreCase))
				{
					list.AddRange(AssemblySatelliteResolver.GetAssemblyBaseUris(assembly));
				}
			}
			List<string> list2 = AssemblySatelliteResolver.ExpandCulture(resolve.CultureInfo).ToList<string>();
			Assembly assembly2;
			if (AssemblySatelliteResolver.ProbeForAssemblySatellite(list, list2, resolve, resolveFileName, isLog, first, out assembly2))
			{
				resolvedAssembly = assembly2;
				return true;
			}
			if (isLog)
			{
				string text = string.Format("Cannot resolve \"{0}\".{1}", resolve, AssemblySatelliteResolver.GetDebugStackTrace());
				if (resolve.Name.StartsWith("SolarWinds", StringComparison.OrdinalIgnoreCase) && LocaleConfiguration.InstalledLocales.Intersect(list2, StringComparer.OrdinalIgnoreCase).Any<string>())
				{
					AssemblySatelliteResolver.log.Warn(text);
				}
				else
				{
					AssemblySatelliteResolver.log.Debug(text);
				}
			}
			resolvedAssembly = null;
			return false;
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00003818 File Offset: 0x00001A18
		private static bool ProbeForAssemblySatellite(IEnumerable<Uri> loadedUris, List<string> cultures, AssemblyName resolve, string resolveFileName, bool isLog, bool first, out Assembly resolvedAssembly)
		{
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			foreach (Uri uri in loadedUris)
			{
				string localPath;
				if (uri.IsAbsoluteUri && !string.IsNullOrEmpty(localPath = uri.LocalPath))
				{
					string loadedPath = null;
					try
					{
						loadedPath = AssemblySatelliteResolver.NormalizePath(Path.GetDirectoryName(localPath));
					}
					catch (Exception ex)
					{
						AssemblySatelliteResolver.log.DebugFormat("Cannot get directory path of \"{0}\". {1}", localPath, ex);
					}
					if (!string.IsNullOrEmpty(loadedPath) && hashSet.Add(loadedPath))
					{
						List<string> list = (from culture in cultures
						select Path.Combine(loadedPath, culture, resolveFileName)).Select(new Func<string, string>(AssemblySatelliteResolver.NormalizePath)).Where(new Func<string, bool>(File.Exists)).ToList<string>();
						if (isLog && AssemblySatelliteResolver.log.IsDebugEnabled && list.Count < 1)
						{
							AssemblySatelliteResolver.log.DebugFormat("Resolving \"{0}\" cannot find ({2}) satellite for \"{1}\"", resolve, loadedPath, string.Join(", ", cultures));
						}
						foreach (string text in list)
						{
							try
							{
								if (AssemblySatelliteResolver.LoadSatelliteByPath(resolve, text, isLog, first, out resolvedAssembly))
								{
									return true;
								}
							}
							catch (Exception ex2)
							{
								AssemblySatelliteResolver.log.ErrorFormat("Error resolving \"{0}\" as \"{1}\". {2}", resolve, text, ex2);
							}
						}
					}
				}
			}
			resolvedAssembly = null;
			return false;
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00003A2C File Offset: 0x00001C2C
		private static bool LoadSatelliteByPath(AssemblyName resolve, string fullPath, bool isLog, bool first, out Assembly resolvedAssembly)
		{
			AssemblyName assemblyName = AssemblyName.GetAssemblyName(fullPath);
			if (!AssemblySatelliteResolver.SatelliteMatchesDefinition(resolve, assemblyName))
			{
				if (isLog)
				{
					AssemblySatelliteResolver.log.DebugFormat("Resolving \"{0}\" does not match \"{1}\"", resolve, assemblyName);
				}
				resolvedAssembly = null;
				return false;
			}
			Exception ex = null;
			Assembly assembly = null;
			if (first)
			{
				try
				{
					assembly = Assembly.Load(assemblyName);
				}
				catch (FileNotFoundException)
				{
				}
				catch (FileLoadException)
				{
				}
				catch (Exception ex)
				{
				}
			}
			if (assembly == null)
			{
				if (ex != null)
				{
					AssemblySatelliteResolver.log.DebugFormat("Cannot load \"{0}\", falling back to load-from. {1}", assemblyName, ex);
				}
				assembly = Assembly.LoadFrom(fullPath);
			}
			try
			{
				if (AssemblySatelliteResolver.ResolveCnt.GetOrAdd(assembly.FullName, (string _) => new AssemblySatelliteResolver.IntRef(0)).Increment() == 1 || isLog)
				{
					Uri assemblyLocation = AssemblySatelliteResolver.GetAssemblyLocation(assembly);
					string text = (assemblyLocation != null && assemblyLocation.IsAbsoluteUri) ? AssemblySatelliteResolver.NormalizePath(assemblyLocation.LocalPath) : string.Empty;
					if (!AssemblyName.ReferenceMatchesDefinition(resolve, assembly.GetName()) || !fullPath.Equals(text, StringComparison.Ordinal))
					{
						AssemblySatelliteResolver.log.ErrorFormat("Resolved \"{0}\" as \"{1}\" at \"{2}\"", resolve, assembly, text);
					}
				}
			}
			catch (Exception ex2)
			{
				AssemblySatelliteResolver.log.ErrorFormat("Error reporting assembly-resolved. {0}", ex2);
			}
			resolvedAssembly = assembly;
			return true;
		}

		// Token: 0x04000018 RID: 24
		private static readonly Log log = new Log();

		// Token: 0x04000019 RID: 25
		private static readonly ConcurrentDictionary<string, AssemblySatelliteResolver.IntRef> ResolveCnt = new ConcurrentDictionary<string, AssemblySatelliteResolver.IntRef>(StringComparer.OrdinalIgnoreCase);

		// Token: 0x0400001A RID: 26
		private static readonly ConcurrentDictionary<string, AssemblySatelliteResolver.IntRef> EntryCount = new ConcurrentDictionary<string, AssemblySatelliteResolver.IntRef>(StringComparer.OrdinalIgnoreCase);

		// Token: 0x0400001B RID: 27
		private static readonly char[] PathWhiteSpace = new char[]
		{
			'\t',
			'\n',
			'\v',
			'\f',
			'\r',
			' ',
			'\u0085',
			'\u00a0'
		};

		// Token: 0x020000C3 RID: 195
		private sealed class IntRef
		{
			// Token: 0x0600094D RID: 2381 RVA: 0x000428D0 File Offset: 0x00040AD0
			internal IntRef(int value)
			{
				this.Value = value;
			}

			// Token: 0x0600094E RID: 2382 RVA: 0x000428DF File Offset: 0x00040ADF
			internal int Increment()
			{
				return Interlocked.Increment(ref this.Value);
			}

			// Token: 0x040002C0 RID: 704
			private int Value;
		}
	}
}
