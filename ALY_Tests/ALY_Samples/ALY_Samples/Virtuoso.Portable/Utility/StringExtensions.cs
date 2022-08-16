using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Virtuoso.Portable.Extensions
{
    public static class StringExtensions
    {
        public static string GetValueOrDefault(this string instance)
        {
            return instance == null ? string.Empty : instance;
        }

        public static string RemoveSpecialCharacters(this string instance, bool keep_spaces = false)
        {
            var sb = new StringBuilder(instance.Length);
            foreach (char c in instance)
            {
                if ((keep_spaces && c == ' ') || (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') | c == '.' || c == '_')
                    sb.Append(c);
            }
            return sb.ToString();
        }

        public static string CombineStrings(params string[] values)
        {
            var ret = string.Join(" ",
                values
                .Where(s => string.IsNullOrWhiteSpace(s) == false)
                //.Distinct()
                //.OrderBy(s => s)
                );
            return ret; //.GetValueOrDefault().RemoveSpecialCharacters(keep_spaces: true);
        }

    }
}
