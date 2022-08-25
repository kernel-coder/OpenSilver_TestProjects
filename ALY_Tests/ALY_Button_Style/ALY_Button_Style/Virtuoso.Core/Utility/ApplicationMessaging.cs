#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Occasional;

#endregion

namespace Virtuoso.Core.Utility
{
    public class ApplicationMessaging
    {
        public static async System.Threading.Tasks.Task<string> GetApplicationInfoMessage(bool useXAMLFormatting = false)
        {
            var ret = string.Empty;
            var haveSIPData = await DynamicFormSipManager.Instance.HavePersistedData(OfflineStoreType.SAVE);
            var serverMessage = Client.Offline.EntityManager.Current.ServerMessage;
            if (haveSIPData || string.IsNullOrEmpty(serverMessage) == false)
            {
                List<string> messages = new List<string>();
                if (haveSIPData)
                {
                    var totalEncounters =
                        await DynamicFormSipManager.Instance.TotalPersistedData(OfflineStoreType.SAVE);
                    if (useXAMLFormatting)
                    {
                        messages.Add("<Run Foreground=\"Red\">You have " + totalEncounters +
                                     " encounters to be uploaded.</Run>");
                    }
                    else
                    {
                        messages.Add("You have " + totalEncounters + " encounters to be uploaded.");
                    }
                }

                if (string.IsNullOrEmpty(serverMessage) == false)
                {
                    messages.Add(serverMessage);
                }

                if (useXAMLFormatting)
                {
                    ret = messages.Aggregate((i, j) => i + "<LineBreak/><LineBreak/>" + j);
                }
                else
                {
                    ret = messages.Aggregate((i, j) => i + Environment.NewLine + j);
                }
            }

            return ret;
        }
    }
}