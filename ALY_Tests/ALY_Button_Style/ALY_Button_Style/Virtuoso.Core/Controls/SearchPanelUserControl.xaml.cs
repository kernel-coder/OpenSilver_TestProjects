using GalaSoft.MvvmLight;
using System;
using System.Windows;
using System.Windows.Controls;
using Virtuoso.Core.ViewModel;

namespace Virtuoso.Controls
{
    public partial class SearchPanelUserControl : UserControl, ICleanup
    {
        public event RoutedEventHandler SearchClick;
        public event RoutedEventHandler ViewModelChanged;

        public SearchPanelUserControl()
        {
            InitializeComponent();

            Export = Virtuoso.Client.Core.VirtuosoContainer.Current.GetExport<SearchPanelViewModel>();
            this.DataContext = Export.Value;

            ((SearchPanelViewModel)(this.DataContext)).OnSearchClicked = () =>
            {
                if (SearchClick != null)
                {
                    //var obj = ((SearchPanelViewModel)(this.DataContext)).SearchResult;
                    SearchClick(this.DataContext, new RoutedEventArgs());
                }
            };

#if OPENSILVER
            this.searchItemComboBox.CustomLayout = true;
#endif
            this.Loaded += new RoutedEventHandler(Control_Loaded);
            this.GotFocus += new RoutedEventHandler(Control_GotFocus);
        }

        void Control_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();

            loaded = false;
        }

        bool loaded = false;
        bool firsttime = true;

        void Control_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!loaded)
            {
                Dispatcher.BeginInvoke(() => { this.searchItemComboBox.Focus(); });

                if (firsttime)
                    firsttime = false;
                else
                    loaded = true;
            }
        }

        Lazy<SearchPanelViewModel> Export { get; set; }

        public void SaveState()
        {
            ((SearchPanelViewModel)(this.DataContext)).SaveState();
        }

        public void InitSearch(bool restoreSearchState, int? serviceLineKey)
        {
            ((SearchPanelViewModel)(this.DataContext)).InitSearch(restoreSearchState, serviceLineKey);
        }

        public void Cleanup()
        {
            //NOTE: Cleanup can be called multiple times

            if (Export?.Value != null)
            {
                Export.Value.Cleanup();

                Virtuoso.Client.Core.VirtuosoContainer.Current.ReleaseExport<SearchPanelViewModel>(Export);
                this.Export = null;
            }

            this.Loaded -= Control_Loaded;
            this.GotFocus -= Control_GotFocus;

            this.ClearValue(SearchPanelUserControl.CurrentSearchOverrideProperty);
            this.ClearValue(SearchPanelUserControl.ParentViewModelProperty);
        }

        public static DependencyProperty CurrentSearchOverrideProperty =
              DependencyProperty.Register("CurrentSearchOverride", typeof(string), typeof(SearchPanelUserControl), new PropertyMetadata((o, e) =>
              {
              }));

        public object CurrentSearchOverride
        {
            get { string s = ((string)(base.GetValue(SearchPanelUserControl.CurrentSearchOverrideProperty))); return string.IsNullOrWhiteSpace(s) ? null : s.Trim(); }
            set { base.SetValue(SearchPanelUserControl.CurrentSearchOverrideProperty, value); }
        }

        public object ParentViewModel
        {
            get { return (object)GetValue(ParentViewModelProperty); }
            set 
            {
                bool changed = value != GetValue(ParentViewModelProperty);
                SetValue(ParentViewModelProperty, value);

                if (changed
                    && (ViewModelChanged != null)
                  )
                {
                    ViewModelChanged(this, new RoutedEventArgs());
                }
            }
        }

        public static readonly DependencyProperty ParentViewModelProperty =
            DependencyProperty.Register("ParentViewModel", typeof(object), typeof(SearchPanelUserControl),
            new PropertyMetadata(null)); //Callback invoked on property value has changes

    }
}
