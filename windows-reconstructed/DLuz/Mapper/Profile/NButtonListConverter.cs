using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DLuz.Mapper.Profile;

internal sealed class NButtonListConverter : JsonConverter<List<NButton>>
{
	public override List<NButton> Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions options)
	{
		List<NButton> list = new List<NButton>();
		using JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader);
		if (jsonDocument.RootElement.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement item in jsonDocument.RootElement.EnumerateArray())
			{
				NButton nButton = JsonSerializer.Deserialize<NButton>(item.GetRawText(), options);
				if (nButton != null)
				{
					list.Add(nButton);
				}
			}
		}
		else if (jsonDocument.RootElement.ValueKind == JsonValueKind.Object)
		{
			foreach (JsonProperty item2 in jsonDocument.RootElement.EnumerateObject())
			{
				NButton nButton2 = JsonSerializer.Deserialize<NButton>(item2.Value.GetRawText(), options) ?? new NButton();
				if (string.IsNullOrWhiteSpace(nButton2.Key))
				{
					nButton2.Key = item2.Name;
				}
				list.Add(nButton2);
			}
		}
		return list;
	}

	public override void Write(Utf8JsonWriter writer, List<NButton> value, JsonSerializerOptions options)
	{
		writer.WriteStartArray();
		foreach (NButton item in value)
		{
			JsonSerializer.Serialize(writer, item, options);
		}
		writer.WriteEndArray();
	}
}

