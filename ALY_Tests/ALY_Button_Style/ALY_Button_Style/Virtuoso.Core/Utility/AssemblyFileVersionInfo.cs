#region Usings

using System;
using System.Linq;
using System.Reflection;

#endregion

namespace Virtuoso.Core.Utility
{
    public class AssemblyFileVersionInfo
    {
        public string Version => _assemblyFileVersion;
        public string FormattedVersion => FormatForDisplay();

        string _assemblyFileVersion { get; set; } //e.g. 1.0.0.1 or 5.1.100.10
        int _majorVersion { get; set; }
        int _minorVersion { get; set; }
        int _buildVersion { get; set; }
        int _revisionVersion { get; set; }

        public AssemblyFileVersionInfo(Assembly assembly)
        {
            object[] orr = assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true);
            if (orr.Length > 0)
            {
                _assemblyFileVersion = ((AssemblyFileVersionAttribute)(orr[0])).Version;
                var parts = _assemblyFileVersion.Split('.');
                if (parts.Length == 4)
                {
                    _majorVersion = Int32.Parse(parts[0]);
                    _minorVersion = Int32.Parse(parts[1]);
                    _buildVersion = Int32.Parse(parts[2]);
                    _revisionVersion = Int32.Parse(parts[3]);
                }
                else
                {
                    _majorVersion = _minorVersion = _buildVersion = _revisionVersion = 0;
                }
            }
            else
            {
                _majorVersion = _minorVersion = _buildVersion = _revisionVersion = 0;
                _assemblyFileVersion = string.Empty;
            }
        }

        public string FormatForDisplay()
        {
            string fileVersion = _assemblyFileVersion;

            if (String.IsNullOrEmpty(fileVersion))
            {
                return "UNKNOWN VERSION";
            }

            if (_majorVersion > 0)
            {
                //input = 1.0.0.1 or 5.1.100.10
                //output = 01.00.0000.1
                return string.Format("{0}.{1}.{2}.{3}",
                    _majorVersion.ToString().PadLeft(2, '0'), //major
                    _minorVersion.ToString().PadLeft(2, '0'),          //minor
                    _buildVersion.ToString().PadLeft(4, '0'),          //build/service pack
                    _revisionVersion);                                                  //revision/auto-increment build counter
            }

            return fileVersion;
        }

        public int Major => _majorVersion;
        public int Minor => _minorVersion;
        public int Build => _buildVersion;
        public int Revision => Revision;

        public bool isValid(string serverVersion)
        {
            if (_assemblyFileVersion.Equals(serverVersion))
            {
                return true;
            }

            return false;
        }

        public static Assembly GetAssemblyByName(string name)
        {
            var lst = AppDomain.CurrentDomain.GetAssemblies();

            //[assembly: AssemblyFileVersion("1.1.46.1")]
            //<Version VersionFile = "$(SolutionDir)\Common\version.txt" RevisionType = "Increment">
            //<Output TaskParameter = "Major" PropertyName = "Major" />
            //<Output TaskParameter = "Minor" PropertyName = "Minor" />
            //<Output TaskParameter = "Build" PropertyName = "Build" />
            //<Output TaskParameter = "Revision" PropertyName = "Revision" />
            //</Version>

            //FullName = "Virtuoso, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
            var ret = lst.SingleOrDefault(assembly => assembly.FullName.StartsWith(name));

            return ret;
        }
    }
}