#region Usings

using System;
using System.Windows.Media;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Client.Core;
using Virtuoso.Core.Services;

#endregion

namespace Virtuoso.Core.ViewModel
{
    public sealed class YesNoViewModel : GalaSoft.MvvmLight.ViewModelBase, IDialogService
    {
        public YesNoViewModel(string caption)
        {
            Caption = caption;

            Yes_Command = new RelayCommand(YES_Clicked);
            No_Command = new RelayCommand(NO_Clicked);
        }

        public YesNoViewModel(string caption, string message, bool isWarningMessage = false)
        {
            Caption = caption;
            Message = message;
            IsWarningMessage = isWarningMessage;

            Yes_Command = new RelayCommand(YES_Clicked);
            No_Command = new RelayCommand(NO_Clicked);
        }

        #region Properties

        // NOTE: Message is optional
        private string _Message;

        public string Message
        {
            get { return _Message; }
            set { Set(() => Message, ref _Message, value); }
        }

        private bool _IsWarningMessage;

        public bool IsWarningMessage
        {
            get { return _IsWarningMessage; }
            set { Set(() => IsWarningMessage, ref _IsWarningMessage, value); }
        }

        private Color _MessageColor;

        public Color MessageColor
        {
            get
            {
                System.Windows.ResourceDictionary ControlResourceDictionary =
                    System.Windows.Application.Current.Resources;
                var key = (IsWarningMessage) ? "ValidationSummaryColor1" : "BlackColor";
                var color = (Color)ControlResourceDictionary[key];
                return color;
            }
            set { Set(() => MessageColor, ref _MessageColor, value); }
        }

        public RelayCommand Yes_Command { get; private set; }
        public RelayCommand No_Command { get; private set; }

        #endregion Properties

        #region methods

        private void YES_Clicked()
        {
            DialogResult = true;
        }

        private void NO_Clicked()
        {
            DialogResult = false;
        }

        #endregion Methods

        #region IDialogService

        bool? _dialogResult;

        public bool? DialogResult
        {
            get { return _dialogResult; }
            set
            {
                _dialogResult = value;
                RaisePropertyChanged("DialogResult");
            }
        }

        public bool CanClose()
        {
            return true;
        }

        public void CloseDialog()
        {
            if (DialogResult != true)
            {
                NO_Clicked(); // So escape and the 'X' behave like the No/Cancel button
            }
        }

        // NOTE: Caption is the CustomWindow's Title property.  Use it as the main dialog text, as it is large (size=18+) in relation to the buttons
        private string _caption = "Confirm";

        public string Caption
        {
            get { return _caption; }
            set { Set(() => Caption, ref _caption, value); }
        }

        public bool ResizeWindow => false;
        public bool DynamicSize => true;
        public bool SetMaxWidthAndHeight => false;
        public double? Height => null;
        public double? Width => 400;
        public double? MinHeight => 360;
        public double? MinWidth => 400;

        private Maybe<Action> EditableWidgetSelector = Maybe<Action>.None;

        public void SetSelectFirstEditableWidgetAction(Action editableWidgetSelector)
        {
            if (editableWidgetSelector != null)
            {
                EditableWidgetSelector = editableWidgetSelector.ToMaybe();
            }
        }

        #endregion IDialogService

        #region ICleanup

        public new void Cleanup()
        {
            base.Cleanup();
            Yes_Command = null;
            No_Command = null;
        }

        #endregion ICleanup

        public static void ShowDialog(string caption, string message, bool isWarningMessage = false,
            Action<bool> action = null)
        {
            YesNoViewModel viewModel = new YesNoViewModel(caption, message, isWarningMessage);
            DialogService ds = new DialogService();
            ds.ShowDialog(viewModel, ret =>
            {
                viewModel?.Cleanup();
                viewModel = null;

                action?.Invoke(ret.GetValueOrDefault());
            });
        }
    }
}