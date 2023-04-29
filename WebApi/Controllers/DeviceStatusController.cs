using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Sockets;

namespace WebApi.Controllers
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
        public DeviceStatus Get()
        {
            string ipaddress = "";
            string name = Dns.GetHostName();
            IPAddress[] ipadrlist = Dns.GetHostAddresses(name);
            foreach (IPAddress ipa in ipadrlist)
            {
                if (ipa.AddressFamily == AddressFamily.InterNetworkV6
                && !ipa.ToString().StartsWith("fe80")){
                    Console.WriteLine(ipa.ToString());
                    ipaddress = ipa.ToString(); 
                }
            }
            return new DeviceStatus() {
                Ip = ipaddress,
                Time = TimeOnly.FromDateTime(DateTime.Now)
            };
        }
    }
}