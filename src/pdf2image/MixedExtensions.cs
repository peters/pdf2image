using System;

namespace pdf2image
{
    static class MixedExtensions
    {
        public static bool Between<T>(this T source, T low, T high) where T : IComparable
        {
            return source.CompareTo(low) >= 0 && source.CompareTo(high) <= 0;
        }

        public static string Prepend(this string source, string text)
        {
            return source.Insert(0, text);
        }

    }
}