#region Usings

using System;

#endregion

namespace Virtuoso.Core.Framework
{
    public class ConfigurationException : SystemException
    {
        public ConfigurationException()
        {
        }

        public ConfigurationException(string message)
            : base(message)
        {
        }

        public ConfigurationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}