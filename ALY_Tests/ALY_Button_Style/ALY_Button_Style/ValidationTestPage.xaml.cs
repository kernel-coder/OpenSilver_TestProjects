using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace ALY_Button_Style
{
    public partial class ValidationTestPage : Page
    {
        public ValidationTestPage()
        {
            InitializeComponent();
            DataContext = new Person();
        }

        private void btnValidate_Click(object sender, RoutedEventArgs e)
        {
        }

        private void tbFirstName_BindingValidationError(object sender, ValidationErrorEventArgs e)
        {
            Trace.WriteLine($"BindingValidationError {e.Action} {e.OriginalSource} {e.Error}");
        }

        private void tbLastName_BindingValidationError(object sender, ValidationErrorEventArgs e)
        {
            Trace.WriteLine($"BindingValidationError {e.Action} {e.OriginalSource} {e.Error}");
        }
    }

    public class HelperClass
    {
        public B B { get; set; } = new B();
    }

    public class B
    {
        public Person Person { get; set; } = new Person();
    }

    public class Person : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private string _name = "First";

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;

                ClearErrors(nameof(Name));
                if (string.IsNullOrWhiteSpace(Name))
                    AddError(nameof(Name), "Name cannot be empty.");

                RaisePropertyChanged(nameof(Name));
            }
        }

        private string _lastName = "Last";

        public string LastName
        {
            get { return _lastName; }
            set
            {
                _lastName = value;

                ClearErrors(nameof(LastName));
                if (string.IsNullOrWhiteSpace(LastName))
                    AddError(nameof(LastName), "Last Name cannot be empty.");

                RaisePropertyChanged(nameof(LastName));
            }
        }

        private int _age;
        public int Age
        {
            get { return _age; }
            set
            {
                _age = value;
                RaisePropertyChanged("Age");

                if (value <= 0)
                {
                    throw new Exception("Age cannot be lower than 0.");
                }
            }
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        private void ClearErrors(string propertyName)
        {
            if (_errorsByPropertyName.ContainsKey(propertyName))
            {
                _errorsByPropertyName.Remove(propertyName);
                OnErrorsChanged(propertyName);
            }
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errorsByPropertyName.ContainsKey(propertyName))
                _errorsByPropertyName[propertyName] = new List<string>();

            if (!_errorsByPropertyName[propertyName].Contains(error))
            {
                _errorsByPropertyName[propertyName].Add(error);
                OnErrorsChanged(propertyName);
            }
        }

        private readonly Dictionary<string, List<string>> _errorsByPropertyName = new Dictionary<string, List<string>>();
        public bool HasErrors => _errorsByPropertyName.Count > 0;

        public IEnumerable GetErrors(string propertyName) =>
            _errorsByPropertyName.ContainsKey(propertyName) ? _errorsByPropertyName[propertyName] : null;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
    }
}
