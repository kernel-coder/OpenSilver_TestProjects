#region Usings

using System;

#endregion

namespace Virtuoso.Core.Occasional
{
    public class AlertManagerSipManager : SipManager
    {
        private static volatile AlertManagerSipManager instance;
        private static object syncRoot = new Object();

        private AlertManagerSipManager()
        {
        }

        public static AlertManagerSipManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new AlertManagerSipManager();
                        }
                    }
                }

                return instance;
            }
        }

        string _FileFormatStr = "{0}-AL.{1}"; //refactor to a singleton registry of concreate SipManagers to FileFormat
        protected override string FileFormatStr => _FileFormatStr;
    }
}