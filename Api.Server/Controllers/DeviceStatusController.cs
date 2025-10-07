using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using RaspberryPiApi.Models;

namespace RaspberryPiApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DeviceStatusController : ControllerBase
    {
        private readonly ILogger<DeviceStatusController> _logger;

        public DeviceStatusController(ILogger<DeviceStatusController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetDeviceStatus")]
        public async Task<DeviceStatus> GetAsync()
        {
            var info = new DeviceStatus();

            // ���л�ȡ������Ϣ�����Ч��
            var tasks = new List<Task>
            {
                Task.Run(() => info.Ip = GetIpAddress()),
                Task.Run(() => info.CpuTemperatureCelsius = GetCpuTemperature()),
                Task.Run(() => info.Uptime = GetUptime()),
                Task.Run(() => info.Model = GetModel()),
                Task.Run(() => info.OsVersion = GetOsVersion()),
                Task.Run(() => info.Memory = GetMemoryUsage()),
                Task.Run(() => info.Storage = GetStorageUsage()),
                //Task.Run(async () => info.LastLogin = await GetLastLoginAsync())
            };

            await Task.WhenAll(tasks);

            return info;
        }

        #region Private Helper Methods

        private string? GetIpAddress()
        {
            string name = Dns.GetHostName();
            IPAddress[] ipadrlist = Dns.GetHostAddresses(name);
            foreach (IPAddress ipa in ipadrlist)
            {
                if (ipa.AddressFamily == AddressFamily.InterNetworkV6
                && !ipa.ToString().StartsWith("fe80"))
                {
                    Console.WriteLine(ipa.ToString());
                    return ipa.ToString();
                }
            }
            return null;
        }

        private double? GetCpuTemperature()
        {
            try
            {
                // �� /sys/class/thermal/ ��ȡ����ɿ��ķ�ʽ
                var tempText = System.IO.File.ReadAllText("/sys/class/thermal/thermal_zone0/temp");
                // �¶�ֵ�Ǻ����϶ȣ���Ҫ����1000
                return double.Parse(tempText.Trim()) / 1000.0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read CPU temperature.");
                return null;
            }
        }

        private string? GetUptime()
        {
            try
            {
                var uptimeText = System.IO.File.ReadAllText("/proc/uptime");
                var totalSeconds = double.Parse(uptimeText.Split(' ')[0]);
                var uptime = TimeSpan.FromSeconds(totalSeconds);
                // ��ʽ��Ϊ "X ��, Y Сʱ, Z ����"
                return $"{uptime.Days} ��, {uptime.Hours} Сʱ, {uptime.Minutes} ����";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read uptime.");
                return null;
            }
        }

        private string? GetModel()
        {
            try
            {
                // ���豸����ȡ�ͺ���Ϣ
                return System.IO.File.ReadAllText("/proc/device-tree/model").TrimEnd('\0');
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read model.");
                return null;
            }
        }

        private string? GetOsVersion()
        {
            try
            {
                var osReleaseText = System.IO.File.ReadAllText("/etc/os-release");
                var match = Regex.Match(osReleaseText, @"PRETTY_NAME=""([^""]+)""");
                return match.Success ? match.Groups[1].Value : "Unknown";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read OS version.");
                return null;
            }
        }

        private MemoryInfo? GetMemoryUsage()
        {
            try
            {
                var memInfoText = System.IO.File.ReadAllLines("/proc/meminfo");
                long totalKb = 0, availableKb = 0;

                foreach (var line in memInfoText)
                {
                    if (line.StartsWith("MemTotal:"))
                        totalKb = long.Parse(line.Split(':', StringSplitOptions.RemoveEmptyEntries)[1].Trim().Split(' ')[0]);
                    if (line.StartsWith("MemAvailable:"))
                        availableKb = long.Parse(line.Split(':', StringSplitOptions.RemoveEmptyEntries)[1].Trim().Split(' ')[0]);
                }

                if (totalKb == 0) return null;

                long usedKb = totalKb - availableKb;

                return new MemoryInfo
                {
                    TotalMB = totalKb / 1024,
                    UsedMB = usedKb / 1024,
                    AvailableMB = availableKb / 1024,
                    UsagePercentage = (double)usedKb / totalKb * 100
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read memory usage.");
                return null;
            }
        }

        private StorageInfo? GetStorageUsage()
        {
            try
            {
                // .NET �� DriveInfo ����Կ�ƽ̨����
                var rootDrive = new DriveInfo("/");
                if (!rootDrive.IsReady) return null;

                long totalBytes = rootDrive.TotalSize;
                long freeBytes = rootDrive.TotalFreeSpace;
                long usedBytes = totalBytes - freeBytes;

                return new StorageInfo
                {
                    Name = rootDrive.Name,
                    TotalGB = totalBytes / (1024 * 1024 * 1024),
                    UsedGB = usedBytes / (1024 * 1024 * 1024),
                    FreeGB = freeBytes / (1024 * 1024 * 1024),
                    UsagePercentage = (double)usedBytes / totalBytes * 100
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read storage usage.");
                return null;
            }
        }

        //private async Task<string?> GetLastLoginAsync()
        //{
        //    try
        //    {
        //        // ִ�� 'last' �����ȡ�����¼��Ϣ
        //        var result = await ExecuteZshCommandAsync("last -n 1 --fulltimes");
        //        // 'last' ������������һ�������У�����ȡ�ڶ��У�������ڣ�
        //        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        //        return lines.Length > 1 ? lines[1].Trim() : lines[0].Trim();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to get last login info.");
        //        return null;
        //    }
        //}

        private async Task<string> ExecuteZshCommandAsync(string command)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/zsh",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return output;
        }

        #endregion
    }
}