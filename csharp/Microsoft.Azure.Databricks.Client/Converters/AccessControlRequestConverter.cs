using Microsoft.Azure.Databricks.Client.Models;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Databricks.Client.Converters
{
    public class AccessControlRequestConverter : JsonConverter<AccessControlRequest>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(AccessControlRequest).IsAssignableFrom(typeToConvert);
        }

        public override bool HandleNull => true;

        public override AccessControlRequest Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var enumOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                Converters = { new JsonStringEnumConverter() }
            };

            var acr = JsonDocument.ParseValue(ref reader).RootElement;

            if (acr.TryGetProperty("user_name", out _))
            {
                return acr.Deserialize<AccessControlRequestForUser>(enumOptions);
            }

            if (acr.TryGetProperty("group_name", out _))
            {
                return acr.Deserialize<AccessControlRequestForGroup>(enumOptions);
            }

            if (acr.TryGetProperty("service_principal_name", out _))
            {
                return acr.Deserialize<AccessControlRequestForServicePrincipal>(enumOptions);
            }

            throw new NotSupportedException("AccessControlRequest not recognized");
        }

        public override void Write(Utf8JsonWriter writer, AccessControlRequest value, JsonSerializerOptions options)
        {
            JsonNode node = value switch
            {
                AccessControlRequestForUser user => JsonSerializer.SerializeToNode(user, options),
                AccessControlRequestForGroup group => JsonSerializer.SerializeToNode(group, options),
                AccessControlRequestForServicePrincipal sp => JsonSerializer.SerializeToNode(sp, options),
                _ => throw new NotImplementedException($"JsonConverter not implemented for type {value.GetType()}"),
            };

            node.WriteTo(writer);
        }
    }
}
