using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TvMazeScrapper.Core.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class Actor
    {
        [JsonProperty("raw_id")]
        public int RawId { get; set; }

        [JsonProperty("name")]
        public string Name{ get; set; }

        [JsonProperty("birthday")]
        public string Birthday { get; set; }
    }
}