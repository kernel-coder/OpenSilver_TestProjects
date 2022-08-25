using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Virtuoso.Client.Utils;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Medispan.Extensions;
using Virtuoso.Portable.Model;

namespace Virtuoso.Core.View
{
    public partial class MediSpanMedicationSelection : ChildWindow
    {
        private int? _TakeLimit = null;

        public int TakeLimit
        {
            get
            {
                if (_TakeLimit == null) _TakeLimit = TenantSettingsCache.Current.TenantSettingMedicationSearchTakeLimit;
                return (int)_TakeLimit;
            }
        }

        private DispatcherTimer _doubleClickTimer = null;

        private List<CachedMediSpanMedication> itemsSource
        {
            set
            {
                if (value == null)
                {
                    MediSpanMedication = null;
                    medListBox.Visibility = Visibility.Collapsed;
                    txtBlockNoMeds.Visibility = Visibility.Visible;
                    txtBlockCriteria.Visibility = Visibility.Collapsed;
                    txtBlockCriteriaError.Visibility = Visibility.Collapsed;
                    txtBlockTakeLimit.Visibility = Visibility.Collapsed;
                    buttonSelect.Visibility = Visibility.Collapsed;
                }
                else
                {
                    medListBox.ItemsSource = value;
                    medListBox.SelectedItem = value[0];
                    medListBox.Visibility = Visibility.Visible;
                    txtBlockNoMeds.Visibility = Visibility.Collapsed;
                    txtBlockCriteria.Visibility = Visibility.Collapsed;
                    txtBlockCriteriaError.Visibility = Visibility.Collapsed;
                    txtBlockTakeLimit.Visibility =
                        (value.Count > TakeLimit) ? Visibility.Visible : Visibility.Collapsed;
                    buttonSelect.Visibility = Visibility.Visible;
                }

                busyIndicator.IsBusy = false;
            }
        }

        public CachedMediSpanMedication MediSpanMedication { get; set; }
        public string SearchString { get; set; }
        private bool _DDIDDrugsOnly = false;
        private bool _RoutedDrugsOnly = false;

        public MediSpanMedicationSelection(string MedName, bool RoutedDrugsOnly = false, bool DDIDDrugsOnly = false)
        {
            InitializeComponent();
            _RoutedDrugsOnly = RoutedDrugsOnly;
            _DDIDDrugsOnly = DDIDDrugsOnly;
            if (!string.IsNullOrWhiteSpace(MedName)) txtBoxSearch.Text = MedName;
            MediSpanMedication = null;
            _doubleClickTimer = new DispatcherTimer();
            _doubleClickTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            _doubleClickTimer.Tick += new EventHandler(DoubleClick_Timer);
            this.Closing += new EventHandler<CancelEventArgs>(MediSpanMedicationSelection_Closing);
            this.Loaded += new RoutedEventHandler(MediSpanMedicationSelection_Loaded);
        }

        void MediSpanMedicationSelection_Loaded(object sender, RoutedEventArgs e)
        {
            txtBlockTakeLimit.Text = "Results are limited to the first " + TakeLimit.ToString() +
                                     " matching medications.  Refine your search criteria.";
            Deployment.Current.Dispatcher.BeginInvoke(() => { txtBoxSearch.Focus(); });
        }

        private void MediSpanMedicationSelection_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = busyIndicator.IsBusy;
            SearchString = txtBoxSearch.Text.Trim();
        }

        public void Cleanup()
        {
            itemsSource = null;
            medListBox.ItemsSource = null;
            this.Closing -= MediSpanMedicationSelection_Closing;
            this.Loaded -= MediSpanMedicationSelection_Loaded;
            _doubleClickTimer.Tick -= DoubleClick_Timer;
            _doubleClickTimer = null;
        }

        private void DoubleClick_Timer(object sender, EventArgs e)
        {
            _doubleClickTimer.Stop();
        }

        private void medListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_doubleClickTimer.IsEnabled)
            {
                buttonSelect_Click(null, new RoutedEventArgs()); // Perform doubleclick
            }
            else
            {
                _doubleClickTimer.Start();
            }
        }

        private async System.Threading.Tasks.Task<List<CachedMediSpanMedication>> SearchMediSpanMedications()
        {
            if (_DDIDDrugsOnly == true)
            {
                return await MediSpanMedicationCache.Current.SearchDDIDDrugsOnly(SearchString, TakeLimit + 1);
            }
            else if (_RoutedDrugsOnly == true)
            {
                return await MediSpanMedicationCache.Current.SearchRoutedDrugsOnly(SearchString, TakeLimit + 1);
            }
            else
            {
                return await MediSpanMedicationCache.Current.Search(SearchString, TakeLimit + 1);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        private void buttonSearch_Click(object sender, RoutedEventArgs e)
        {
            MediSpanMedication = null;
            if (string.IsNullOrWhiteSpace(txtBoxSearch.Text.Trim()))
            {
                medListBox.Visibility = Visibility.Collapsed;
                txtBlockNoMeds.Visibility = Visibility.Collapsed;
                buttonSelect.Visibility = Visibility.Collapsed;
                txtBlockCriteria.Visibility = Visibility.Visible;
                txtBlockCriteriaError.Visibility = Visibility.Collapsed;
                txtBlockTakeLimit.Visibility = Visibility.Collapsed;
                return;
            }

            if (txtBoxSearch.Text.Trim().Length < 3)
            {
                medListBox.Visibility = Visibility.Collapsed;
                txtBlockNoMeds.Visibility = Visibility.Collapsed;
                buttonSelect.Visibility = Visibility.Collapsed;
                txtBlockCriteria.Visibility = Visibility.Collapsed;
                txtBlockCriteriaError.Visibility = Visibility.Visible;
                txtBlockTakeLimit.Visibility = Visibility.Collapsed;
                return;
            }

            busyIndicator.IsBusy = true;
            SearchString = txtBoxSearch.Text.Trim();

            AsyncUtility.RunAsync(async () =>
            {
                var results = await SearchMediSpanMedications();
                AsyncUtility.RunOnMainThread(() => itemsSource = results);
            });

            return;
        }

        private void buttonSelect_Click(object sender, RoutedEventArgs e)
        {
            if (medListBox.SelectedItem == null) return;
            MediSpanMedication = medListBox.SelectedItem as CachedMediSpanMedication;
            this.DialogResult = true;
        }

        private void buttonClear_Click(object sender, RoutedEventArgs e)
        {
            txtBoxSearch.Text = "";
            medListBox.Visibility = Visibility.Collapsed;
            medListBox.ItemsSource = null;
            txtBlockNoMeds.Visibility = Visibility.Collapsed;
            buttonSelect.Visibility = Visibility.Collapsed;
            txtBlockCriteria.Visibility = Visibility.Collapsed;
            txtBlockCriteriaError.Visibility = Visibility.Collapsed;
            txtBlockTakeLimit.Visibility = Visibility.Collapsed;
        }

        private void txtBoxSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) buttonSearch_Click(null, new RoutedEventArgs());
        }
    }
}