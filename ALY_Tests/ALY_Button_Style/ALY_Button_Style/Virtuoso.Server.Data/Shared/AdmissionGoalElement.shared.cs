using System.Runtime.Serialization;

namespace Virtuoso.Server.Data
{
    public partial class AdmissionGoalElement
    {
        private bool _Save = true; //should be moved to constructor

        //[System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool Save
        {
            get { return _Save; }
            set { this._Save = value; }
        }
    }
}
