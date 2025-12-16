using System.Text.Json;
using System.Text.Json.Serialization;

namespace LlamaApi.Core.Domain;

public class ModelStatusJsonConverter : JsonConverter<ModelStatus>
{
    public override ModelStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return ModelStatusExtensions.FromString(value);
    }

    public override void Write(Utf8JsonWriter writer, ModelStatus value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToStringValue());
    }
}
