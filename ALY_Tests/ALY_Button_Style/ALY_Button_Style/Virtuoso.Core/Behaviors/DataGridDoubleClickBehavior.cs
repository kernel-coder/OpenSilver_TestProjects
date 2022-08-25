#region Usings

using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

#endregion

namespace Virtuoso.Core.Behaviors
{
    //http://www.dansoltesz.com/post/2010/02/19/Silverlight-datagrid-double-click-behavior.aspx
    /// <summary> 
    /// Class to attach mouse click events to UIElements 
    /// </summary> 
    public class MouseClickManager
    {
        #region Private members

        private event MouseButtonEventHandler _click;
        private event MouseButtonEventHandler _doubleClick;

        #endregion

        #region Constructor

        /// <summary> 
        /// Initializes a new instance of the <see cref="MouseClickManager"/> class. 
        /// </summary> 
        /// <param name="control">The control.</param> 
        public MouseClickManager(int doubleClickTimeout)
        {
            Clicked = false;
            DoubleClickTimeout = doubleClickTimeout;
        }

        #endregion

        #region Events

        public event MouseButtonEventHandler Click
        {
            add { _click += value; }
            remove { _click -= value; }
        }

        public event MouseButtonEventHandler DoubleClick
        {
            add { _doubleClick += value; }
            remove { _doubleClick -= value; }
        }

        /// <summary> 
        /// Called when [click]. 
        /// </summary> 
        /// <param name="sender">The sender.</param> 
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param> 
        private void OnClick(object sender, MouseButtonEventArgs e)
        {
            if (_click != null)
            {
                Debug.Assert(sender is Control);
                (sender as Control).Dispatcher.BeginInvoke(_click, sender, e);
            }
        }

        /// <summary> 
        /// Called when [double click]. 
        /// </summary> 
        /// <param name="sender">The sender.</param> 
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param> 
        private void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_doubleClick != null)
            {
                _doubleClick(sender, e);
            }
        }

        #region Private
#if OPENSILVER
        private System.Windows.Threading.DispatcherTimer _doubleClickCheckTimer;
        private MouseButtonEventArgs _doubleClickEventArgs;
        private void ResetDoubleClickTimer()
        {
            _doubleClickCheckTimer?.Stop();
            _doubleClickCheckTimer = null;
        }

        private void OnDoubleClickTimer_Tick(object sender, EventArgs e)
        {
            ResetDoubleClickTimer();
            lock (this)
            {
                if (Clicked)
                {
                    Clicked = false;
                    OnClick(this, _doubleClickEventArgs);
                }
            }
        }
#else
        /// <summary> 
        /// Resets the thread. 
        /// </summary> 
        /// <param name="state">The state.</param> 
        private void ResetThread(object state)
        {
            Thread.Sleep(DoubleClickTimeout);

            lock (this)
            {
                if (Clicked)
                {
                    Clicked = false;
                    OnClick(this, (MouseButtonEventArgs)state);
                }
            }
        }
#endif

        #endregion

        /// <summary> 
        /// Handles the click. 
        /// </summary> 
        /// <param name="sender">The sender.</param> 
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param> 
        public void HandleClick(object sender, MouseButtonEventArgs e)
        {
            lock (this)
            {
                if (Clicked)
                {
#if OPENSILVER
                    ResetDoubleClickTimer();
#endif
                    Clicked = false;                    
                    OnDoubleClick(sender, e);
                }
                else
                {
                    Clicked = true;
#if OPENSILVER
                    ResetDoubleClickTimer();
                    _doubleClickEventArgs = e;
                    _doubleClickCheckTimer = new System.Windows.Threading.DispatcherTimer();
                    _doubleClickCheckTimer.Interval = TimeSpan.FromMilliseconds(DoubleClickTimeout);
                    _doubleClickCheckTimer.Tick += this.OnDoubleClickTimer_Tick;
                    _doubleClickCheckTimer.Start();
#else
                    ParameterizedThreadStart threadStart = ResetThread;
                    Thread thread = new Thread(threadStart);
                    thread.Start(e);
#endif
    }
            }
        }

        #endregion

        #region Properties

        /// <summary> 
        /// Gets or sets a value indicating whether this <see cref="MouseClickManager"/> is clicked. 
        /// </summary> 
        /// <value><c>true</c> if clicked; otherwise, <c>false</c>.</value> 
        private bool Clicked { get; set; }

        /// <summary> 
        /// Gets or sets the timeout. 
        /// </summary> 
        /// <value>The timeout.</value> 
        public int DoubleClickTimeout { get; set; }

        #endregion

        #region Methods

        #endregion
    }

    //http://www.dansoltesz.com/post/2010/02/19/Silverlight-datagrid-double-click-behavior.aspx
    //http://www.dansoltesz.com/post/2010/10/01/Silverlight-datagrid-double-click-behavior-MVVM.aspx
    public class DataGridDoubleClickBehavior : Behavior<DataGrid>
    {
        private readonly MouseClickManager _gridClickManager;
        public event EventHandler<MouseButtonEventArgs> DoubleClick;

        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(
                "CommandParameter",
                typeof(object),
                typeof(DataGridDoubleClickBehavior),
                new PropertyMetadata(CommandParameterChanged));

        private static void CommandParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Code for dealing with your property changes        
        }

        public ICommand DoubleClickCommand
        {
            get { return (ICommand)GetValue(DoubleClickCommandProperty); }
            set { SetValue(DoubleClickCommandProperty, value); }
        }

        public static readonly DependencyProperty DoubleClickCommandProperty =
            DependencyProperty.Register(
                "DoubleClickCommand",
                typeof(ICommand),
                typeof(DataGridDoubleClickBehavior),
                new PropertyMetadata(DoubleClickCommandChanged));

        private static void DoubleClickCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Code for dealing with your property changes        
        }

        public DataGridDoubleClickBehavior()
        {
            _gridClickManager = new MouseClickManager(300);
            _gridClickManager.DoubleClick += _gridClickManager_DoubleClick;
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.LoadingRow += OnLoadingRow;
            AssociatedObject.UnloadingRow += OnUnloadingRow;
        }

        void OnUnloadingRow(object sender, DataGridRowEventArgs e)
        {
            //row is no longer visible so remove double click event otherwise            
            //row events will miss fire           
            e.Row.MouseLeftButtonUp -= _gridClickManager.HandleClick;
        }

        void OnLoadingRow(object sender, DataGridRowEventArgs e)
        {
            //row is visible in grid, wire up double click event            
            e.Row.MouseLeftButtonUp += _gridClickManager.HandleClick;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.LoadingRow -= OnLoadingRow;
            AssociatedObject.UnloadingRow -= OnUnloadingRow;
        }

        void _gridClickManager_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DoubleClick?.Invoke(sender, e);
            DoubleClickCommand?.Execute(CommandParameter);
        }
    }
}