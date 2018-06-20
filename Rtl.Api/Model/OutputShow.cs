using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TvMazeScrapper.Core.Model;

namespace Rtl.Api.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class OutputShow
    {
        [JsonProperty("id")]
        public int RawId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("cast")]
        public OutputActor[] Cast { get; set; }
    }
}