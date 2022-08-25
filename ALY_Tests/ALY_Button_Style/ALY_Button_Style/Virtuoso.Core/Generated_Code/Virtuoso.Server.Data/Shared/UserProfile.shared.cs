using System;

namespace Virtuoso.Server.Data
{
    public partial class UserProfile
    {
        public string UserAge
        {
            get
            {
                if (BirthDate == null) return null;
                DateTime birthdate = (DateTime)BirthDate;
                DateTime endDate = DateTime.Today;
                if ((DateTime)birthdate > endDate) return null;
                string userAge = null;
                TimeSpan diff = endDate.Subtract((DateTime)birthdate);
                if (diff.Days < 14) userAge = ((int)(diff.Days)).ToString() + " day"; // < 14 days return n days
                else if (diff.Days < 56) userAge = ((int)(diff.Days / 7)).ToString() + " week"; // < 8 weeks return n weeks
                else
                {
                    int years = ((int)(endDate.Year - birthdate.Year - 1) + (((endDate.Month > birthdate.Month) || ((endDate.Month == birthdate.Month) && (endDate.Day >= birthdate.Day))) ? 1 : 0));
                    if (years < 2) userAge = ((int)(diff.Days / 30)).ToString() + " month"; // < 24 months return n months
                    else userAge = years.ToString() + " year"; // else return n years; 
                }
                return userAge + ((userAge.StartsWith("1 ")) ? "" : "s") + " old";
            }
        }
    }

}
