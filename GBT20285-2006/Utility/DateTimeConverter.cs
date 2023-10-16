using System.Text.Json;
using System.Text.Json.Serialization;

namespace GBT20285_2006.Utility
{
    public class DateTimeConverter : JsonConverter<DateTime>
    {
        // 从Json转换为类型T
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
           return DateTime.Parse(reader.GetString()!);
        }
        // 从类型T转换至Json
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
