using System;

namespace Microsoft.Azure.Databricks.Client
{
    public static class NullableExt
    {
        public static void ForEach<T>(this T? source, Action<T> action) where T : struct
        {
            if (source.HasValue)
            {
                action(source.Value);
            }
        }

        public static V? Map<T, V>(this T? @this, Func<T, V> f)
            where T : struct
            where V : struct
            => @this.HasValue ? f(@this.Value) : null;
    }
}
