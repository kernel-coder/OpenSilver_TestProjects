#region Usings

using System.Collections.Generic;

#endregion

namespace Virtuoso.Core
{
    public class SearchParameter
    {
        public string Field { get; set; }
        public string Value { get; set; }
        public string Condition { get; set; }

        public static void ParseQueryString(Dictionary<string, string> items, List<SearchParameter> sps)
        {
            sps.Clear();

            if (items.Count > 0)
            {
                foreach (var item in items)
                {
                    string searchvalue = item.Value.Substring(0,
                        item.Value.EndsWith("=")
                            ? item.Value.Length - 1
                            : item.Value.Length); //seem to have an extra = at the end of the last value

                    sps.Add(new SearchParameter
                    {
                        Field = item.Key,
                        Value = searchvalue
                    });
                }
            }
        }
    }
}