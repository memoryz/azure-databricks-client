using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Databricks.Client.Converters
{
    public class LibraryConverter : JsonConverter<Library>
    {
        public override bool HandleNull => true;

        public override Library Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var library = JsonDocument.ParseValue(ref reader).RootElement;
            if (library.TryGetProperty("jar", out var jar))
            {
                return jar.Deserialize<JarLibrary>();
            }

            if (library.TryGetProperty("egg", out var egg))
            {
                return egg.Deserialize<EggLibrary>();
            }

            if (library.TryGetProperty("whl", out var whl))
            {
                return whl.Deserialize<WheelLibrary>();
            }

            if (library.TryGetProperty("maven", out var maven))
            {
                return maven.Deserialize<MavenLibrary>();
            }

            if (library.TryGetProperty("pypi", out var pypi))
            {
                return pypi.Deserialize<PythonPyPiLibrary>();
            }

            if (library.TryGetProperty("cran", out var cran))
            {
                return cran.Deserialize<RCranLibrary>();
            }

            throw new NotSupportedException("Library not recognized");

        }

        public override void Write(Utf8JsonWriter writer, Library value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}