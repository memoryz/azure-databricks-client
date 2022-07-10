using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Microsoft.Azure.Databricks.Client.Test
{
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    internal class EquatableJToken : IEquatable<EquatableJToken?>
    {
        private readonly JToken jToken;

        private EquatableJToken(JToken jToken)
        {
            this.jToken = jToken;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as EquatableJToken);
        }

        public bool Equals(EquatableJToken? other)
        {
            return other is not null &&
                   JToken.EqualityComparer.Equals(jToken, other.jToken);
        }

        public override int GetHashCode()
        {
            return JToken.EqualityComparer.GetHashCode(jToken);
        }

        private string GetDebuggerDisplay() => ToString()!;

        public override string? ToString()
        {
            return jToken.ToString();
        }

        public static implicit operator EquatableJToken(JToken token) => new(token);
    }
}