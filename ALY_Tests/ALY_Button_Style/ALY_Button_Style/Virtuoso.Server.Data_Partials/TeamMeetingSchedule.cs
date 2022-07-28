#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Data;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class TeamMeetingSchedule
    {
        public CollectionViewSource AlphaEndsWithList;
        public CollectionViewSource AlphaStartsWithList;

        public bool LoadAllLetters { get; set; } = false;

        public int StartLetterAscii =>
            string.IsNullOrEmpty(StartLetter)
                ? 0
                : Convert.ToInt32(Convert.ToChar(StartLetter));

        public int EndLetterAscii =>
            string.IsNullOrEmpty(EndLetter)
                ? 0
                : Convert.ToInt32(Convert.ToChar(EndLetter));

        public ICollectionView AlphaStartsWithView
        {
            get
            {
                try
                {
                    if (AlphaStartsWithList == null)
                    {
                        SetupStartsWithList();
                    }

                    return AlphaStartsWithList.View;
                }
                catch
                {
                    return null;
                }
            }
        }

        public ICollectionView AlphaEndsWithView
        {
            get
            {
                try
                {
                    if (AlphaEndsWithList == null)
                    {
                        SetupEndsWithList();
                    }

                    return AlphaEndsWithList.View;
                }
                catch
                {
                    return null;
                }
            }
        }

        public bool CanEditEndLetter
        {
            get
            {
                return ServiceLineGrouping == null
                    ? true
                    : ServiceLineGrouping.TeamMeetingSchedule.Count == 1 ||
                      !ServiceLineGrouping.TeamMeetingSchedule.Any(ts => ts.EndLetterAscii > EndLetterAscii);
            }
        }

        public void SetupStartsWithList()
        {
            if (AlphaStartsWithList == null)
            {
                AlphaStartsWithList = new CollectionViewSource();
            }

            AlphaStartsWithList.Source = null;
            var charList = new List<string>();

            for (var i = 65; i < 91; i++) charList.Add(((char)i).ToString());
            AlphaStartsWithList.Source = charList;
        }

        public void SetupEndsWithList()
        {
            if (AlphaEndsWithList == null)
            {
                AlphaEndsWithList = new CollectionViewSource();
                AlphaEndsWithList.Filter += (s, e) =>
                {
                    var a = Convert.ToChar(e.Item);
                    if (LoadAllLetters)
                    {
                        e.Accepted = true;
                    }
                    else if (Convert.ToInt32(a) <= EndLetterAscii && Convert.ToInt32(a) >= StartLetterAscii)
                    {
                        e.Accepted = true;
                    }
                    else
                    {
                        e.Accepted = false;
                    }
                };
            }

            AlphaEndsWithList.Source = null;
            var charList = new List<string>();
            for (var i = 65; i < 91; i++) charList.Add(((char)i).ToString());
            AlphaEndsWithList.Source = charList;
        }
    }
}