using Newtonsoft.Json;

namespace SimpleWebSockerServer.Console.Models
{
    internal class TerminalStatusReq
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "STATUS_REQUEST";
        [JsonProperty("version")]
        public string Version { get; set; } = "V_1";
    }
}
