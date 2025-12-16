using System.Text.Json;
using System.Text.Json.Serialization;

namespace LlamaApi.Core.Domain;

public class JobStatusEnumJsonConverter : JsonConverter<JobStatusEnum>
{
    public override JobStatusEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return JobStatusEnumExtensions.FromString(value);
    }

    public override void Write(Utf8JsonWriter writer, JobStatusEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToStringValue());
    }
}
