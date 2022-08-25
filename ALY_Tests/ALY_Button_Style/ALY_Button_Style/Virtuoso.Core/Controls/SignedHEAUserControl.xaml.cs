using OpenRiaServices.DomainServices.Client;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Virtuoso.Core.Utility;

namespace Virtuoso.Core.Controls
{
    public partial class SignedHEAUserControl : UserControl, INotifyPropertyChanged
    {
        public SignedHEAUserControl()
        {
            InitializeComponent();
        }
        #region EncounterHospiceElectionAddendumDependencyProperty
        public Entity EncounterHospiceElectionAddendum
        {
            get { return (Entity)GetValue(EncounterHospiceElectionAddendumProperty); }
            set { SetValue(EncounterHospiceElectionAddendumProperty, value); }
        }
        public static readonly DependencyProperty EncounterHospiceElectionAddendumProperty =
            DependencyProperty.Register("EncounterHospiceElectionAddendum",
            typeof(Entity),
            typeof(SignedHEAUserControl),
            new PropertyMetadata(null, new PropertyChangedCallback(EncounterHospiceElectionAddendumChanged)));
        private static void EncounterHospiceElectionAddendumChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
