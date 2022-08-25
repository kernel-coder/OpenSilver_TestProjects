namespace Virtuoso.Client.Core
{
    public class XamlHelper
    {
        public static string EncodeAsXaml(string data)
        {
            //http://msdn.microsoft.com/en-us/library/aa970677.aspx
            return data.Replace("&", "&amp;") //must do '&' first...
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }
    }
}