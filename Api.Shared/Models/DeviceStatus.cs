namespace RaspberryPiApi.Models
{
    // 主 DTO
    public class DeviceStatus
    {
        public double? CpuTemperatureCelsius { get; set; }
        public string? Uptime { get; set; }
        public string? Model { get; set; }
        public string? OsVersion { get; set; }
        public MemoryInfo? Memory { get; set; }
        public StorageInfo? Storage { get; set; }
        //public string? LastLogin { get; set; }
        public string? Ip { get; set; }
    }

    // 内存信息子 DTO
    public class MemoryInfo
    {
        public long TotalMB { get; set; }
        public long UsedMB { get; set; }
        public long AvailableMB { get; set; }
        public double UsagePercentage { get; set; }
    }

    // 存储信息子 DTO
    public class StorageInfo
    {
        public string? Name { get; set; }
        public long TotalGB { get; set; }
        public long UsedGB { get; set; }
        public long FreeGB { get; set; }
        public double UsagePercentage { get; set; }
    }
}
