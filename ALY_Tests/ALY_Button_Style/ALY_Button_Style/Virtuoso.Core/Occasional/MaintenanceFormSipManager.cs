#region Usings

using System;

#endregion

namespace Virtuoso.Core.Occasional
{
    public class MaintenanceSipManager : SipManager
    {
        private static volatile MaintenanceSipManager instance;
        private static readonly object syncRoot = new Object();

        private MaintenanceSipManager()
        {
        }

        public static MaintenanceSipManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new MaintenanceSipManager();
                        }
                    }
                }

                return instance;
            }
        }

        string _FileFormatStr = "{0}-PH.{1}"; //refactor to a singleton registry of concreate SipManagers to FileFormat
        protected override string FileFormatStr => _FileFormatStr;
    }
}