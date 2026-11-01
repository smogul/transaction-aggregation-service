using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace transaction_aggregation_api.Json
{
    public class RandJsonConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDecimal();
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            writer.WriteRawValue(value.ToString("0.00", CultureInfo.InvariantCulture));
        }
    }
}
