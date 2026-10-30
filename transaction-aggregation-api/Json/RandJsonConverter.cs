using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace transaction_aggregation_api.Json
{
    // Writes every amount of money in rands with exactly two decimals, like on a till slip.
    // Example: 14500 becomes 14500.00 and -5989.7 becomes -5989.70.
    //
    // Without it, the same kind of number could look different depending on where it came from
    // (the Excel file gives 14500, the database gives 5000.0).
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
