using System;
using System.Net;
using System.Windows;

namespace Virtuoso.Portable.Extensions
{
    public class FormatHelper
    {
        public static string PartCommonPartPartFormat(string Part1, string Part2, string Part3)
        {
            try
            {
                var ln = string.IsNullOrEmpty(Part1) ? string.Empty : Part1;
                var fn = string.IsNullOrEmpty(Part2) ? string.Empty : Part2;
                var mn = string.IsNullOrEmpty(Part3) ? string.Empty : Part3;
                string result;

                if (ln == string.Empty)
                {
                    result = String.Format("{0} {1}", fn, mn).Trim();
                }
                else
                {
                    result = String.Format("{0}, {1} {2}", ln, fn, mn).Trim();
                }

                if (result.Substring(0,1) == ",")
                {
                    result = result.Substring(1);
                }

                if (result.Substring(result.Length - 1,1) == ",")
                {
                    result = result.Substring(0, result.Length - 1);
                }
                return string.IsNullOrEmpty(result) ? string.Empty : result;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string FormatName(string LN, string FN, string MN)
        {
            string result = PartCommonPartPartFormat(LN, FN, MN);
            return string.IsNullOrEmpty(result) ? " " : result;
        }

        public static string FormatCityStateZip(string City, string State, string ZipCode)
        {
            return PartCommonPartPartFormat(City, State, ZipCode);
        }
    }
}
