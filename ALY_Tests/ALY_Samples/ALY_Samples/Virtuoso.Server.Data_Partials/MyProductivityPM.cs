#region Usings

using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class MyProductivityPM
    {
        public string Time
        {
            get
            {
                var useMilitaryTime = TenantSettingsCache.Current.TenantSetting.UseMilitaryTime;
                var startTime = useMilitaryTime ? StartDateTime.ToString("HHmm") : StartDateTime.ToShortTimeString();
                var endTime = useMilitaryTime
                    ? EndDateTime.GetValueOrDefault().ToString("HHmm")
                    : EndDateTime.GetValueOrDefault().ToShortTimeString();
                return $"{startTime} - {endTime}";
            }
        }
    }
}