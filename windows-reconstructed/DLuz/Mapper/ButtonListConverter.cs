using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DLuz.Mapper;

internal sealed class ButtonListConverter : JsonConverter<List<ButtonConfig>>
{
	public override List<ButtonConfig> Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions options)
	{
		List<ButtonConfig> list = new List<ButtonConfig>();
		using JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader);
		if (jsonDocument.RootElement.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement item in jsonDocument.RootElement.EnumerateArray())
			{
				ButtonConfig buttonConfig = JsonSerializer.Deserialize<ButtonConfig>(item.GetRawText(), options);
				if (buttonConfig != null)
				{
					list.Add(buttonConfig);
				}
			}
		}
		else if (jsonDocument.RootElement.ValueKind == JsonValueKind.Object)
		{
			foreach (JsonProperty item2 in jsonDocument.RootElement.EnumerateObject())
			{
				ButtonConfig buttonConfig2 = JsonSerializer.Deserialize<ButtonConfig>(item2.Value.GetRawText(), options) ?? new ButtonConfig();
				if (string.IsNullOrWhiteSpace(buttonConfig2.Key))
				{
					buttonConfig2.Key = item2.Name;
				}
				list.Add(buttonConfig2);
			}
		}
		return list;
	}

	public override void Write(Utf8JsonWriter writer, List<ButtonConfig> value, JsonSerializerOptions options)
	{
		writer.WriteStartArray();
		foreach (ButtonConfig item in value)
		{
			JsonSerializer.Serialize(writer, item, options);
		}
		writer.WriteEndArray();
	}
}
