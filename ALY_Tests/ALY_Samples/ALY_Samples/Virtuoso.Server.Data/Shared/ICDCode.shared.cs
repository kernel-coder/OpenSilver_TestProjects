using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Virtuoso.Server.Data
{
    public partial class ICDCode
    {
        //[System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string VolumeCode
        {
            get
            {
                return "Diagnosis";
            }
        }
    }
}
