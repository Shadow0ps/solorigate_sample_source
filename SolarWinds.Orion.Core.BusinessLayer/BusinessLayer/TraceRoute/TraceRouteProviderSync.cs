using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using SolarWinds.Logging;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer.TraceRoute
{
	// Token: 0x02000049 RID: 73
	public class TraceRouteProviderSync : ITraceRouteProvider
	{
		// Token: 0x060004AD RID: 1197 RVA: 0x0001D508 File Offset: 0x0001B708
		public TracerouteResult TraceRoute(string destinationHostNameOrIpAddress)
		{
			TracerouteResult tracerouteResult = new TracerouteResult();
			int hopCount = -1;
			List<TraceRouteResultEntry> nodeList;
			string errorMessage;
			tracerouteResult.IsSuccess = this.TraceRoute(destinationHostNameOrIpAddress, out hopCount, out nodeList, out errorMessage);
			tracerouteResult.HopCount = hopCount;
			tracerouteResult.NodeList = nodeList;
			tracerouteResult.ErrorMessage = errorMessage;
			return tracerouteResult;
		}

		// Token: 0x060004AE RID: 1198 RVA: 0x0001D544 File Offset: 0x0001B744
		public TracerouteResult TraceRoute(string destinationHostNameOrIpAddress, long maxTimeoutInMilliseconds)
		{
			TracerouteResult tracerouteResult = new TracerouteResult();
			int hopCount = -1;
			List<TraceRouteResultEntry> nodeList;
			string errorMessage;
			tracerouteResult.IsSuccess = this.TraceRoute(destinationHostNameOrIpAddress, maxTimeoutInMilliseconds, out hopCount, out nodeList, out errorMessage);
			tracerouteResult.HopCount = hopCount;
			tracerouteResult.NodeList = nodeList;
			tracerouteResult.ErrorMessage = errorMessage;
			return tracerouteResult;
		}

		// Token: 0x060004AF RID: 1199 RVA: 0x0001D581 File Offset: 0x0001B781
		public bool TraceRoute(string destinationHostNameOrIpAddress, out int hopCount, out List<TraceRouteResultEntry> returnedResultList, out string errorMessage)
		{
			return this.TraceRoute(destinationHostNameOrIpAddress, TraceRouteProviderSync.TRACEROUTE_TIMEOUT_DEFAULT, out hopCount, out returnedResultList, out errorMessage);
		}

		// Token: 0x060004B0 RID: 1200 RVA: 0x0001D594 File Offset: 0x0001B794
		public bool TraceRoute(string destinationHostNameOrIpAddress, long maxTimeoutInMilliseconds, out int hopCount, out List<TraceRouteResultEntry> returnedResultList, out string errorMessage)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			hopCount = -1;
			returnedResultList = null;
			errorMessage = null;
			bool result = false;
			using (Ping ping = new Ping())
			{
				byte[] buffer = new byte[32];
				PingOptions pingOptions = new PingOptions(TraceRouteProviderSync.TTL_MAX, true);
				PingReply pingReply = ping.Send(destinationHostNameOrIpAddress, (int)maxTimeoutInMilliseconds, buffer, pingOptions);
				if (pingReply == null || pingReply.Status == IPStatus.TimedOut)
				{
					return false;
				}
				long num = pingReply.RoundtripTime * (long)TraceRouteProviderSync.TRACEROUTE_SINGLE_MAX_FACTOR;
				stopwatch.Stop();
				if (stopwatch.ElapsedMilliseconds > maxTimeoutInMilliseconds)
				{
					errorMessage = string.Format("Stop trace route due to timeout {0} (ms). Rount trip to destination {1} takes {2} (ms)", maxTimeoutInMilliseconds, destinationHostNameOrIpAddress, pingReply.RoundtripTime);
					return false;
				}
				if (num < TraceRouteProviderSync.TRACEROUTE_TIMEOUT_PERPING_MIN)
				{
					num = TraceRouteProviderSync.TRACEROUTE_TIMEOUT_PERPING_MIN;
				}
				result = this.TraceRouteForward(ping, destinationHostNameOrIpAddress, maxTimeoutInMilliseconds, num, buffer, pingOptions, stopwatch, out hopCount, out returnedResultList, out errorMessage);
			}
			return result;
		}

		// Token: 0x060004B1 RID: 1201 RVA: 0x0001D688 File Offset: 0x0001B888
		public bool TraceRouteForward(Ping pingSender, string destinationHostNameOrIpAddress, long maxTimeoutInMilliseconds, long timeoutInMilliseconds, byte[] buffer, PingOptions pingOptions, Stopwatch stopWatchTotal, out int destinationTtl, out List<TraceRouteResultEntry> returnedResultList, out string errorMessage)
		{
			StringBuilder stringBuilder = new StringBuilder();
			int num = 1;
			Stopwatch stopwatch = new Stopwatch();
			destinationTtl = -1;
			errorMessage = null;
			returnedResultList = new List<TraceRouteResultEntry>();
			string hostName = Dns.GetHostName();
			stringBuilder.AppendLine(string.Format("Start tracing from Source: IP = {0}...", hostName));
			TraceRouteResultEntry item = new TraceRouteResultEntry
			{
				HostNameOrIpAddress = hostName,
				RoundTripTimeInMilliseconds = 0L,
				Ttl = 0
			};
			returnedResultList.Add(item);
			bool flag = false;
			stopWatchTotal.Stop();
			while (num <= TraceRouteProviderSync.TTL_MAX && stopWatchTotal.ElapsedMilliseconds < maxTimeoutInMilliseconds)
			{
				pingOptions.Ttl = num;
				int num2 = Math.Min((int)(maxTimeoutInMilliseconds - stopWatchTotal.ElapsedMilliseconds), (int)timeoutInMilliseconds);
				if ((long)num2 < TraceRouteProviderSync.TRACEROUTE_TIMEOUT_PERPING_MIN)
				{
					num2 = (int)TraceRouteProviderSync.TRACEROUTE_TIMEOUT_PERPING_MIN;
				}
				TraceRouteProviderSync._log.DebugFormat("Trace Route Forward Start: destinationIP: {0}, timeOut: {1}, ttl: {2}", destinationHostNameOrIpAddress, num2, pingOptions.Ttl);
				stopWatchTotal.Start();
				stopwatch.Restart();
				PingReply pingReply = pingSender.Send(destinationHostNameOrIpAddress, num2, buffer, pingOptions);
				stopwatch.Stop();
				if (pingReply.Status == IPStatus.Success)
				{
					flag = true;
					TraceRouteResultEntry traceRouteResultEntry = new TraceRouteResultEntry
					{
						HostNameOrIpAddress = pingReply.Address.ToString(),
						RoundTripTimeInMilliseconds = pingReply.RoundtripTime,
						Ttl = num
					};
					if (traceRouteResultEntry.RoundTripTimeInMilliseconds == 0L)
					{
						traceRouteResultEntry.RoundTripTimeInMilliseconds = stopwatch.ElapsedMilliseconds;
					}
					returnedResultList.Add(traceRouteResultEntry);
					stringBuilder.AppendLine(string.Format("Destination: IP = {0}, roundTrip = {1}, TTL = {2};", traceRouteResultEntry.HostNameOrIpAddress, traceRouteResultEntry.RoundTripTimeInMilliseconds, num));
					TraceRouteProviderSync._log.DebugFormat("Trace Route Forward End Success: destinationIP: {0}, timeOut: {1}, ttl: {2}", destinationHostNameOrIpAddress, num2, pingOptions.Ttl);
					break;
				}
				if (pingReply.Status == IPStatus.TtlExpired || pingReply.Status == IPStatus.TimeExceeded)
				{
					TraceRouteResultEntry traceRouteResultEntry2 = new TraceRouteResultEntry
					{
						HostNameOrIpAddress = pingReply.Address.ToString(),
						RoundTripTimeInMilliseconds = pingReply.RoundtripTime,
						Ttl = num
					};
					if (traceRouteResultEntry2.RoundTripTimeInMilliseconds == 0L)
					{
						traceRouteResultEntry2.RoundTripTimeInMilliseconds = stopwatch.ElapsedMilliseconds;
					}
					returnedResultList.Add(traceRouteResultEntry2);
					stringBuilder.AppendLine(string.Format("TTL exceeds: IP = {0}, roundTrip = {1}, TTL = {2};", traceRouteResultEntry2.HostNameOrIpAddress, traceRouteResultEntry2.RoundTripTimeInMilliseconds, num));
					TraceRouteProviderSync._log.DebugFormat("Trace Route Forward End {0}: destinationIP: {1}, timeOut: {2}, ttl: {3}", new object[]
					{
						pingReply.Status,
						destinationHostNameOrIpAddress,
						num2,
						pingOptions.Ttl
					});
				}
				else if (pingReply.Status == IPStatus.TimedOut)
				{
					stringBuilder.AppendLine(string.Format("No response: IP = {0}, roundTrip = {1}, TTL = {2};", "Unknown", -1, num));
					TraceRouteProviderSync._log.DebugFormat("Trace Route Forward End {0}: destinationIP: {1}, timeOut: {2}, ttl: {3}", new object[]
					{
						pingReply.Status,
						destinationHostNameOrIpAddress,
						num2,
						pingOptions.Ttl
					});
				}
				num++;
				stopWatchTotal.Stop();
			}
			destinationTtl = num;
			if (!flag && stopWatchTotal.ElapsedMilliseconds > maxTimeoutInMilliseconds)
			{
				TraceRouteProviderSync._log.DebugFormat("Trace Route Forward Stop due to exceed max time: destinationIP: {0}, time spent: {1}, ttl: {2}", destinationHostNameOrIpAddress, stopWatchTotal.ElapsedMilliseconds, pingOptions.Ttl);
				stringBuilder.AppendLine(string.Format("Stop trace route due to timeout {0} (ms). Rount trip to destination {1} takes {2} (ms)", maxTimeoutInMilliseconds, destinationHostNameOrIpAddress, timeoutInMilliseconds / (long)TraceRouteProviderSync.TRACEROUTE_SINGLE_MAX_FACTOR));
				errorMessage = stringBuilder.ToString();
				return false;
			}
			errorMessage = stringBuilder.ToString();
			return true;
		}

		// Token: 0x060004B2 RID: 1202 RVA: 0x0001D9FC File Offset: 0x0001BBFC
		public int EstimateHopCount(int responseTtl)
		{
			if (responseTtl >= 128)
			{
				return 255 - responseTtl + 1;
			}
			if (responseTtl >= 64)
			{
				return 128 - responseTtl + 1;
			}
			if (responseTtl >= 60)
			{
				return 64 - responseTtl + 1;
			}
			if (responseTtl >= 32)
			{
				return 64 - responseTtl + 1;
			}
			return 64 - responseTtl + 1;
		}

		// Token: 0x0400013F RID: 319
		public static int TTL_MAX = 255;

		// Token: 0x04000140 RID: 320
		public static long TRACEROUTE_TIMEOUT_DEFAULT = 20000L;

		// Token: 0x04000141 RID: 321
		public static long TRACEROUTE_TIMEOUT_PERPING_MIN = 2L;

		// Token: 0x04000142 RID: 322
		public static int TRACEROUTE_SINGLE_MAX_FACTOR = 2;

		// Token: 0x04000143 RID: 323
		private static Log _log = new Log("TraceRouteProviderSync");
	}
}
