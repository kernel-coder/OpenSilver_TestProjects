#region Usings

using System.Text.RegularExpressions;

#endregion

namespace Virtuoso.Core.Validations
{
    public static class EmailAddressChecker
    {
        public static bool IsValidEmailAddress(string emailaddress)
        {
            var checkpattern = @"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*" + "@" +
                               @"((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$";
            var regex = new Regex(checkpattern);
            var match = regex.Match(emailaddress);
            return (match.Success);
        }
    }
}