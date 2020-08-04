#nullable enable

namespace H.Formatters.Utilities
{
    /// <summary>
    /// Full Array support for net40.
    /// <![CDATA[Version: 1.0.0.0]]> <br/>
    /// </summary>
    public static class ArrayUtilities
    {
        /// <summary>
        /// System.Array.Empty or new T[0].
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] Empty<T>()
        {
#if NETSTANDARD1_0 || NETSTANDARD1_1 || NETSTANDARD1_2 || NET40 || NET45 || NET452
            return new T[0];
#else
            return System.Array.Empty<T>();
#endif
        }
    }
}
