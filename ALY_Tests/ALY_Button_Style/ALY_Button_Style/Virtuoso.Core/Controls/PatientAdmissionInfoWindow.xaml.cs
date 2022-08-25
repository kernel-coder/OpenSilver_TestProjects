using System.Windows.Controls;
using System.Windows.Input;
using Virtuoso.Client.Core;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;


namespace Virtuoso.Core.Controls
{
    public partial class PatientAdmissionInfoWindow : ChildWindow
    {
        #region Constructors

        public PatientAdmissionInfoWindow()
        {
            InitializeComponent();
            this.DataContext = VirtuosoContainer.Current.GetExport<PatientAdmissionInfoViewModel>().Value;
           
        }

        public PatientAdmissionInfoWindow(int patientId, int? admissionId, IPatientService pateintServiceModel)
        {
            InitializeComponent();
            this.DataContext = new PatientAdmissionInfoViewModel(patientId, admissionId, pateintServiceModel);
        }

        #endregion

        // Offer ESC key support for closing the ChildWindow:
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

    }
}

