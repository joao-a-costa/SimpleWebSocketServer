using Newtonsoft.Json;

namespace SimpleWebSockerServer.Console.Models
{
    internal class TerminalStatusReqResponse
    {
        [JsonProperty("type")]
        public string Type;

        [JsonProperty("version")]
        public string Version;

        [JsonProperty("sdkId")]
        public string SdkId;

        [JsonProperty("sdkVersion")]
        public string SdkVersion;

        [JsonProperty("sdkStatus")]
        public string SdkStatus;

        [JsonProperty("hasCredentials")]
        public bool HasCredentials;

        [JsonProperty("deviceSerialNumber")]
        public string DeviceSerialNumber;

        [JsonProperty("smartConnectId")]
        public string SmartConnectId;

        [JsonProperty("smartConnectVersion")]
        public string SmartConnectVersion;

        [JsonProperty("terminalID")]
        public int TerminalID;

        [JsonProperty("merchantId")]
        public int MerchantId;
    }
}
