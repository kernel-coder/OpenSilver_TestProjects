#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Xml.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class SearchRecord : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string Type { get; set; }
        public string Screen { get; set; }
        public string Label { get; set; }
        public string URI { get; set; }

        public Boolean AllowAddNew
        {
            get { return allowAddNew; }
            set
            {
                allowAddNew = value;
                NotifyPropertyChanged("AllowAddNew");
            }
        }

        private Boolean allowAddNew;
        public int? ServiceLineTypeFilter
        {
            get { return serviceLineTypeFilter; }
            set
            {
                serviceLineTypeFilter = value;
                NotifyPropertyChanged("ServiceLineTypeFilter");
            }
        }
        private int? serviceLineTypeFilter = null;
       

        public ObservableCollection<SearchField> SearchFields { get; set; }

        public string SearchResultsView { get; set; }       //resource string identifying the control to use 
        public ISearch SearchResultsViewModel { get; set; } //used as data context for SearchResultsView

        public bool Valid()
        {
            return !(SearchFields.OfType<INotifyDataErrorInfo>().ToList().Any(sf => sf.HasErrors));
        }
    }

    public class SearchFieldValue
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public abstract class SearchField
    {
        public string Field { get; set; }
        public string Label { get; set; }
        public string Type { get; set; }

        public bool SaveState { get; set; }

        public string Value { get; set; }

        public virtual IEnumerable<SearchFieldValue> FieldValues()
        {
            yield return new SearchFieldValue { Name = Field, Value = Value };
        }

        public virtual void Restore(List<SearchFieldValue> values)
        {
            foreach (var sfv in values) Value = sfv.Value;
        }

        public virtual void Clear()
        {
        }
    }

    public class TextBoxSearchField : SearchField, ICleanup
    {
        public string Condition { get; set; }

        public override void Clear()
        {
            Condition = "0";
        }

        public RelayCommand<KeyEventArgs> SearchFromEnterCommand { get; set; }

        public void Cleanup()
        {
            SearchFromEnterCommand = null;
        }
    }

    public class TextBoxSearchFieldFactory
    {
        public static SearchField Create(XElement searchfield, Action Search)
        {
            return new TextBoxSearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
                Condition = "0",
                SearchFromEnterCommand = new RelayCommand<KeyEventArgs>(e =>
                {
                    if (e != null && e.Key == Key.Enter)
                    {
                        Search();
                    }
                })
            };
        }
    }

    public class CheckBoxSearchField : SearchField
    {
        public override void Clear()
        {
            Value = "false";
        }
    }

    public static class CheckBoxSearchFieldFactory
    {
        public static SearchField Create(XElement searchfield, Action Search)
        {
            return new CheckBoxSearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
                Value = "false",
            };
        }
    }

    public class CodeLookupSearchField : SearchField
    {
        public string CodeType { get; set; }

        public override IEnumerable<SearchFieldValue> FieldValues()
        {
            if ((Value != null) && (Value.Equals("0")))
            {
                yield return new SearchFieldValue { Name = Field, Value = String.Empty };
            }
            else
            {
                yield return new SearchFieldValue { Name = Field, Value = Value };
            }
        }

        public override void Restore(List<SearchFieldValue> values)
        {
            foreach (var sfv in values) Value = sfv.Value;
        }
    }

    public static class CodeLookupSearchFieldFactory
    {
        public static SearchField Create(XElement searchfield, Action Search)
        {
            return new CodeLookupSearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
                CodeType = (string)searchfield.Attribute("codeType")
            };
        }
    }

    public class EmployerSearchField : SearchField
    {
        public override IEnumerable<SearchFieldValue> FieldValues()
        {
            if ((Value != null) && (Value.Equals("0")))
            {
                yield return new SearchFieldValue { Name = Field, Value = String.Empty };
            }
            else
            {
                yield return new SearchFieldValue { Name = Field, Value = Value };
            }
        }
    }

    public static class EmployerSearchFieldFactory
    {
        public static SearchField Create(XElement searchfield, Action Search)
        {
            return new FacilitySearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
            };
        }
    }

    public class FacilitySearchField : SearchField
    {
        public override IEnumerable<SearchFieldValue> FieldValues()
        {
            if ((Value != null) && (Value.Equals("0")))
            {
                yield return new SearchFieldValue { Name = Field, Value = String.Empty };
            }
            else
            {
                yield return new SearchFieldValue { Name = Field, Value = Value };
            }
        }
    }

    public static class FacilitySearchFieldFactory
    {
        public static SearchField Create(XElement searchfield, Action Search)
        {
            return new FacilitySearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
            };
        }
    }

    public class FacilityBranchSearchField : SearchField
    {
        public override IEnumerable<SearchFieldValue> FieldValues()
        {
            if ((Value != null) && (Value.Equals("0")))
            {
                yield return new SearchFieldValue { Name = Field, Value = String.Empty };
            }
            else
            {
                yield return new SearchFieldValue { Name = Field, Value = Value };
            }
        }
    }

    public static class FacilityBranchSearchFieldFactory
    {
        public static SearchField Create(XElement searchfield, Action Search)
        {
            return new FacilityBranchSearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
            };
        }
    }

    public class GoalSearchField : SearchField
    {
        public override IEnumerable<SearchFieldValue> FieldValues()
        {
            if ((Value != null) && (Value.Equals("0")))
            {
                yield return new SearchFieldValue { Name = Field, Value = String.Empty };
            }
            else
            {
                yield return new SearchFieldValue { Name = Field, Value = Value };
            }
        }
    }

    public static class GoalSearchFieldFactory
    {
        public static SearchField Create(XElement searchfield, Action Search)
        {
            return new GoalSearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
            };
        }
    }

    public class DateSearchField : SearchField
    {
    }

    public class DateSearchFieldFactory
    {
        public static SearchField Create(XElement searchfield, Action Search)
        {
            return new DateSearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
            };
        }
    }

    public class CodeTypeSearchField : SearchField
    {
        public override IEnumerable<SearchFieldValue> FieldValues()
        {
            if ((Value != null) && (Value.Equals("0")))
            {
                yield return new SearchFieldValue { Name = Field, Value = String.Empty };
            }
            else
            {
                yield return new SearchFieldValue { Name = Field, Value = Value };
            }
        }
    }

    public class CodeTypeSearchFieldFactory
    {
        public static SearchField Create(XElement searchfield, Action Search)
        {
            return new CodeTypeSearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
            };
        }
    }

    public class ServiceLineSearchField : SearchField, INotifyPropertyChanged, INotifyDataErrorInfo
    {
        public ServiceLineSearchField()
        {
            _currentErrors = new Dictionary<string, List<string>>();
            _ServiceLineItemsSource = ServiceLineCache.GetActiveUserServiceLinePlusMe(ServiceLineKey, true);
            var _firstServiceLine = _ServiceLineItemsSource.FirstOrDefault();
            if (_firstServiceLine != null)
            {
                ServiceLineKey = _firstServiceLine.ServiceLineKey;
            }
        }

        int _ServiceLineKey = -1;

        public int ServiceLineKey
        {
            get { return _ServiceLineKey; }
            set
            {
                if (_ServiceLineKey != value)
                {
                    _ServiceLineKey = value;

                    try
                    {
                        //Initialize dependent list
                        _ServiceLineGroupingItemsSource = ServiceLineCache
                            .GetActiveUserServiceLineGroupingForServiceLinePlusMe(ServiceLineKey, null, true)
                            .Where(slg => (slg.ServiceLineGroupHeader == null)
                                          || ((slg.ServiceLineGroupHeader != null)
                                              && (slg.ServiceLineGroupHeader.SequenceNumber == 0)
                                          )
                            ).ToList();
                    }
                    catch (Exception)
                    {
                        // ignore exceptions
                    }

                    //Initialize dependent list selection
                    var _firstServiceLineGrouping = _ServiceLineGroupingItemsSource.FirstOrDefault();
                    if (_firstServiceLineGrouping != null)
                    {
                        ServiceLineGroupingKey = _firstServiceLineGrouping.ServiceLineGroupingKey;
                    }
                    else
                    {
                        ServiceLineGroupingKey = 0;
                    }

                    RaisePropertyChanged("ServiceLineKey");
                    RaisePropertyChanged("ServiceLineGroupingItemsSource");
                    RaisePropertyChanged("ServiceLineGroupingKey");
                }
            }
        }

        int _ServiceLineGroupingKey;

        public int ServiceLineGroupingKey
        {
            get { return _ServiceLineGroupingKey; }
            set
            {
                if (_ServiceLineGroupingKey != value)
                {
                    _ServiceLineGroupingKey = value;
                    RaisePropertyChanged("ServiceLineGroupingKey");
                }
            }
        }

        public override void Clear()
        {
            ServiceLineKey = 0;
            ServiceLineGroupingKey = 0;
        }

        List<ServiceLine> _ServiceLineItemsSource;

        public List<ServiceLine> ServiceLineItemsSource
        {
            get
            {
                ClearErrorFromProperty("ServiceLineItemsSource");
                if (_ServiceLineItemsSource.Any() == false)
                {
                    AddErrorForProperty("ServiceLineItemsSource", "User not assigned to a service line");
                }

                return _ServiceLineItemsSource;
            }
        }

        List<ServiceLineGrouping> _ServiceLineGroupingItemsSource;

        public List<ServiceLineGrouping> ServiceLineGroupingItemsSource
        {
            get
            {
                ClearErrorFromProperty("ServiceLineGroupingItemsSource");
                if ((_ServiceLineGroupingItemsSource == null) || (_ServiceLineGroupingItemsSource.Any() == false))
                {
                    AddErrorForProperty("ServiceLineGroupingItemsSource", "User not assigned to a service line");
                }

                return _ServiceLineGroupingItemsSource;
            }
        }

        public override IEnumerable<SearchFieldValue> FieldValues()
        {
            yield return new SearchFieldValue { Name = "ServiceLineKey", Value = ServiceLineKey.ToString() };
            yield return new SearchFieldValue
                { Name = "ServiceLineGroupingKey", Value = ServiceLineGroupingKey.ToString() };
        }

        public override void Restore(List<SearchFieldValue> values)
        {
            foreach (var sfv in values)
                if (sfv.Name == "ServiceLineKey")
                {
                    ServiceLineKey = Int32.Parse(sfv.Value);
                }
                else if (sfv.Name == "ServiceLineGroupingKey")
                {
                    ServiceLineGroupingKey = Int32.Parse(sfv.Value);
                }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion INotifyPropertyChanged

        #region INotifyDataErrorInfo

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        readonly Dictionary<string, List<string>> _currentErrors;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                //FYI: if you are not supporting entity level errors, it is acceptable to return null
                var ret = _currentErrors.Values.Where(c => c.Any());
                return ret.Any() ? ret : null;
            }

            MakeOrCreatePropertyErrorList(propertyName);
            if (_currentErrors[propertyName].Any())
            {
                return _currentErrors[propertyName];
            }

            return null;
        }

        public bool HasErrors
        {
            get { return _currentErrors.Where(c => c.Value.Any()).Any(); }
        }

        void FireErrorsChanged(string property)
        {
            if (ErrorsChanged != null)
            {
                ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
            }
        }

        public void ClearErrorFromProperty(string property)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Clear();
            FireErrorsChanged(property);
        }

        public void AddErrorForProperty(string property, string error)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Add(error);
            FireErrorsChanged(property);
        }

        void MakeOrCreatePropertyErrorList(string propertyName)
        {
            if (!_currentErrors.ContainsKey(propertyName))
            {
                _currentErrors[propertyName] = new List<string>();
            }
        }

        #endregion INotifyDataErrorInfo
    }

    public class ServiceLineSearchFieldFactory
    {
        public static SearchField Create(XElement searchfield, Action Search)
        {
            return new ServiceLineSearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
            };
        }
    }

    public class ServiceLineOnlySearchField : SearchField, INotifyPropertyChanged, INotifyDataErrorInfo
    {
        public ServiceLineOnlySearchField()
        {
            _currentErrors = new Dictionary<string, List<string>>();
            _ServiceLineItemsSource = ServiceLineCache.GetActiveUserServiceLinePlusMe(ServiceLineKey, true);
            var _firstServiceLine = _ServiceLineItemsSource.FirstOrDefault();
            if (_firstServiceLine != null)
            {
                ServiceLineKey = _firstServiceLine.ServiceLineKey;
            }
        }

        int _ServiceLineKey = -1;

        public int ServiceLineKey
        {
            get { return _ServiceLineKey; }
            set
            {
                if (_ServiceLineKey != value)
                {
                    _ServiceLineKey = value;
                    RaisePropertyChanged("ServiceLineKey");
                }
            }
        }

        public override void Clear()
        {
            ServiceLineKey = 0;
        }

        List<ServiceLine> _ServiceLineItemsSource;

        public List<ServiceLine> ServiceLineItemsSource
        {
            get
            {
                ClearErrorFromProperty("ServiceLineItemsSource");
                if (_ServiceLineItemsSource.Any() == false)
                {
                    AddErrorForProperty("ServiceLineItemsSource", "User not assigned to a service line");
                }

                return _ServiceLineItemsSource;
            }
        }

        public override IEnumerable<SearchFieldValue> FieldValues()
        {
            yield return new SearchFieldValue { Name = "ServiceLineKey", Value = ServiceLineKey.ToString() };
        }

        public override void Restore(List<SearchFieldValue> values)
        {
            foreach (var sfv in values)
                if (sfv.Name == "ServiceLineKey")
                {
                    ServiceLineKey = Int32.Parse(sfv.Value);
                }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion INotifyPropertyChanged

        #region INotifyDataErrorInfo

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        readonly Dictionary<string, List<string>> _currentErrors;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                //FYI: if you are not supporting entity level errors, it is acceptable to return null
                var ret = _currentErrors.Values.Where(c => c.Any());
                return ret.Any() ? ret : null;
            }

            MakeOrCreatePropertyErrorList(propertyName);
            if (_currentErrors[propertyName].Any())
            {
                return _currentErrors[propertyName];
            }

            return null;
        }

        public bool HasErrors
        {
            get { return _currentErrors.Where(c => c.Value.Any()).Any(); }
        }

        void FireErrorsChanged(string property)
        {
            if (ErrorsChanged != null)
            {
                ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
            }
        }

        public void ClearErrorFromProperty(string property)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Clear();
            FireErrorsChanged(property);
        }

        public void AddErrorForProperty(string property, string error)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Add(error);
            FireErrorsChanged(property);
        }

        void MakeOrCreatePropertyErrorList(string propertyName)
        {
            if (!_currentErrors.ContainsKey(propertyName))
            {
                _currentErrors[propertyName] = new List<string>();
            }
        }

        #endregion INotifyDataErrorInfo
    }

    public class ServiceLineOnlySearchFieldFactory
    {
        public static SearchField Create(XElement searchfield, Action Search)
        {
            return new ServiceLineOnlySearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
            };
        }
    }

    public class RuleSearchField : SearchField, INotifyPropertyChanged, INotifyDataErrorInfo
    {
        public RuleSearchField()
        {
            _currentErrors = new Dictionary<string, List<string>>();
            ruleItemsSource = RuleDefinitionCache.GetRules(true).OrderBy(r => r.RuleDescription);
            var _firstRule = ruleItemsSource.FirstOrDefault();
            if (_firstRule != null)
            {
                RuleDefinitionKey = _firstRule.RuleDefinitionKey;
            }
        }

        int ruleDefinitionKey = -1;

        public int RuleDefinitionKey
        {
            get { return ruleDefinitionKey; }
            set
            {
                if (ruleDefinitionKey != value)
                {
                    ruleDefinitionKey = value;
                    RaisePropertyChanged("ServiceLineKey");
                }
            }
        }

        public override void Clear()
        {
            RuleDefinitionKey = 0;
        }

        IOrderedEnumerable<RuleDefinition> ruleItemsSource;
        public IOrderedEnumerable<RuleDefinition> RuleItemsSource => ruleItemsSource;

        public override IEnumerable<SearchFieldValue> FieldValues()
        {
            yield return new SearchFieldValue { Name = "RuleDefinitionKey", Value = RuleDefinitionKey.ToString() };
        }

        public override void Restore(List<SearchFieldValue> values)
        {
            foreach (var sfv in values)
                if (sfv.Name == "RuleDefinitionKey")
                {
                    RuleDefinitionKey = Int32.Parse(sfv.Value);
                }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion INotifyPropertyChanged

        #region INotifyDataErrorInfo

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        readonly Dictionary<string, List<string>> _currentErrors;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                //FYI: if you are not supporting entity level errors, it is acceptable to return null
                var ret = _currentErrors.Values.Where(c => c.Any());
                return ret.Any() ? ret : null;
            }

            MakeOrCreatePropertyErrorList(propertyName);
            if (_currentErrors[propertyName].Any())
            {
                return _currentErrors[propertyName];
            }

            return null;
        }

        public bool HasErrors
        {
            get { return _currentErrors.Where(c => c.Value.Any()).Any(); }
        }

        void FireErrorsChanged(string property)
        {
            if (ErrorsChanged != null)
            {
                ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
            }
        }

        public void ClearErrorFromProperty(string property)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Clear();
            FireErrorsChanged(property);
        }

        public void AddErrorForProperty(string property, string error)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Add(error);
            FireErrorsChanged(property);
        }

        void MakeOrCreatePropertyErrorList(string propertyName)
        {
            if (!_currentErrors.ContainsKey(propertyName))
            {
                _currentErrors[propertyName] = new List<string>();
            }
        }

        #endregion INotifyDataErrorInfo
    }

    public class RuleSearchFieldFactory
    {
        public static SearchField Create(XElement searchfield, Action Search)
        {
            return new RuleSearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
            };
        }
    }

    public class DisciplineSearchField : SearchField, INotifyPropertyChanged, INotifyDataErrorInfo
    {
        public DisciplineSearchField()
        {
            _currentErrors = new Dictionary<string, List<string>>();
            disciplineItemsSource = DisciplineCache.GetActiveDisciplines(true).ToObservableCollection();
        }

        int disciplineKey;

        public int DisciplineKey
        {
            get { return disciplineKey; }
            set
            {
                if (disciplineKey != value)
                {
                    disciplineKey = value;
                    RaisePropertyChanged("DisciplineKey");
                }
            }
        }

        public override void Clear()
        {
            DisciplineKey = 0;
        }

        ObservableCollection<Discipline> disciplineItemsSource;
        public ObservableCollection<Discipline> DisciplineItemsSource => disciplineItemsSource;

        public override IEnumerable<SearchFieldValue> FieldValues()
        {
            yield return new SearchFieldValue { Name = Field, Value = Value };
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion INotifyPropertyChanged

        #region INotifyDataErrorInfo

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        readonly Dictionary<string, List<string>> _currentErrors;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                //FYI: if you are not supporting entity level errors, it is acceptable to return null
                var ret = _currentErrors.Values.Where(c => c.Any());
                return ret.Any() ? ret : null;
            }

            MakeOrCreatePropertyErrorList(propertyName);
            if (_currentErrors[propertyName].Any())
            {
                return _currentErrors[propertyName];
            }

            return null;
        }

        public bool HasErrors
        {
            get { return _currentErrors.Where(c => c.Value.Any()).Any(); }
        }

        void FireErrorsChanged(string property)
        {
            if (ErrorsChanged != null)
            {
                ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
            }
        }

        public void ClearErrorFromProperty(string property)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Clear();
            FireErrorsChanged(property);
        }

        public void AddErrorForProperty(string property, string error)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Add(error);
            FireErrorsChanged(property);
        }

        void MakeOrCreatePropertyErrorList(string propertyName)
        {
            if (!_currentErrors.ContainsKey(propertyName))
            {
                _currentErrors[propertyName] = new List<string>();
            }
        }

        #endregion INotifyDataErrorInfo
    }

    public class DisciplineSearchFieldFactory
    {
        public static SearchField Create(XElement searchfield, Action Search)
        {
            return new DisciplineSearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
            };
        }
    }

    public class GuardAreaStateZipcodeSearchField : SearchField, INotifyPropertyChanged, INotifyDataErrorInfo
    {
        public GuardAreaStateZipcodeSearchField()
        {
            Clear();
        }

        public void LoadStatesCollection()
        {
            GuardAreaStateItemsSource = GuardAreaCache.GetGuardAreaStates(true);
        }

        public void LoadStateZipCodesCollection()
        {
            GuardAreaZipCodeItemsSource = GuardAreaCache.GetGuardAreaZipCodesByState(StateCode, true);
        }

        public void LoadGuardAreaZipCodesCollection()
        {
            GuardAreaZipCodeItemsSource = GuardAreaCache.GetGuardAreaZipCodes(true);
        }

        int? _stateCode;

        public int? StateCode
        {
            get { return _stateCode; }
            set
            {
                if (_stateCode != value)
                {
                    _stateCode = value;
                    if (_stateCode != null)
                    {
                        LoadStateZipCodesCollection();
                        ZipCode = "";
                    }
                    else
                    {
                        LoadGuardAreaZipCodesCollection();
                    }

                    RaisePropertyChanged("StateCode");
                }
            }
        }

        string _zipCode = "";

        public string ZipCode
        {
            get { return _zipCode; }
            set
            {
                if (_zipCode != value)
                {
                    _zipCode = value;
                    RaisePropertyChanged("ZipCode");
                }
            }
        }

        public override void Clear()
        {
            StateCode = null;
            ZipCode = "";
            LoadStatesCollection();
            LoadGuardAreaZipCodesCollection();
        }

        List<GuardArea> _guardAreaStateItemsSource;

        public List<GuardArea> GuardAreaStateItemsSource
        {
            get { return _guardAreaStateItemsSource; }
            set
            {
                _guardAreaStateItemsSource = value;
                StateCode = null;
                RaisePropertyChanged("GuardAreaStateItemsSource");

                ZipCode = "";
                GuardAreaZipCodeItemsSource = null;
            }
        }

        List<GuardArea> _guardAreaZipCodeItemsSource;

        public List<GuardArea> GuardAreaZipCodeItemsSource
        {
            get { return _guardAreaZipCodeItemsSource; }
            set
            {
                _guardAreaZipCodeItemsSource = value;
                RaisePropertyChanged("GuardAreaZipCodeItemsSource");
            }
        }

        public override IEnumerable<SearchFieldValue> FieldValues()
        {
            yield return new SearchFieldValue { Name = "StateCode", Value = StateCode.ToString() };
            yield return new SearchFieldValue { Name = "ZipCode", Value = ZipCode };
        }

        public override void Restore(List<SearchFieldValue> values)
        {
            foreach (var sfv in values)
                if (sfv.Name == "StateCode")
                {
                    StateCode = Int32.Parse(sfv.Value);
                }
                else if (sfv.Name == "ZipCode")
                {
                    ZipCode = sfv.Value;
                }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion INotifyPropertyChanged

        #region INotifyDataErrorInfo

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        readonly Dictionary<string, List<string>> _currentErrors = new Dictionary<string, List<string>>();

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                //FYI: if you are not supporting entity level errors, it is acceptable to return null
                var ret = _currentErrors.Values.Where(c => c.Any());
                return ret.Any() ? ret : null;
            }

            MakeOrCreatePropertyErrorList(propertyName);
            if (_currentErrors[propertyName].Any())
            {
                return _currentErrors[propertyName];
            }

            return null;
        }

        public bool HasErrors
        {
            get { return _currentErrors.Where(c => c.Value.Any()).Any(); }
        }

        void FireErrorsChanged(string property)
        {
            if (ErrorsChanged != null)
            {
                ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
            }
        }

        public void ClearErrorFromProperty(string property)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Clear();
            FireErrorsChanged(property);
        }

        public void AddErrorForProperty(string property, string error)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Add(error);
            FireErrorsChanged(property);
        }

        void MakeOrCreatePropertyErrorList(string propertyName)
        {
            if (!_currentErrors.ContainsKey(propertyName))
            {
                _currentErrors[propertyName] = new List<string>();
            }
        }

        #endregion INotifyDataErrorInfo
    }

    public class GuardAreaStateZipcodeSearchFieldFactory
    {
        public static SearchField Create(XElement searchfield, Action Search)
        {
            return new GuardAreaStateZipcodeSearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
            };
        }
    }

    public class FacilityNameBranchSearchField : SearchField, INotifyPropertyChanged, INotifyDataErrorInfo
    {
        public FacilityNameBranchSearchField()
        {
            _currentErrors = new Dictionary<string, List<string>>();
            LoadFacilityCollections();
        }

        public void LoadFacilityCollections()
        {
            _FacilityItemsSource = FacilityCache.GetActiveFacilities(true);
            var _firstFacility = _FacilityItemsSource.FirstOrDefault();
            if (_firstFacility != null)
            {
                FacilityKey = _firstFacility.FacilityKey;
            }

            _FacilityBranchItemsSource = FacilityCache.GetActiveBranches(true);
            var _firstFacilityBranch = _FacilityBranchItemsSource.FirstOrDefault();
            if (_firstFacilityBranch != null)
            {
                FacilityBranchKey = _firstFacilityBranch.FacilityBranchKey;
            }
        }

        int _FacilityKey = -1;

        public int FacilityKey
        {
            get { return _FacilityKey; }
            set
            {
                if (_FacilityKey != value)
                {
                    _FacilityKey = value;
                    if (value == 0)
                    {
                        LoadFacilityCollections();
                    }
                    else
                    {
                        _FacilityBranchItemsSource = FacilityCache.GetFacilityBranches(FacilityKey, true);
                    }

                    //Initialize dependent list selection
                    var firstFacilityBranch = _FacilityBranchItemsSource.FirstOrDefault();
                    if (firstFacilityBranch != null)
                    {
                        FacilityBranchKey = firstFacilityBranch.FacilityBranchKey;
                    }
                    else
                    {
                        FacilityBranchKey = 0;
                    }

                    RaisePropertyChanged("FacilityKey");
                    RaisePropertyChanged("FacilityBranchItemsSource");
                    RaisePropertyChanged("FacilityBranchKey");
                }
            }
        }

        int _FacilityBranchKey;

        public int FacilityBranchKey
        {
            get { return _FacilityBranchKey; }
            set
            {
                if (_FacilityBranchKey != value)
                {
                    ClearErrorFromProperty("FacilityBranchKey");

                    _FacilityBranchKey = value;
                    var facilityBranch = FacilityCache.GetFacilityBranchFromKey(_FacilityBranchKey);
                    if (facilityBranch == null)
                    {
                        if (_FacilityBranchKey != 0)
                        {
                            AddErrorForProperty("FacilityBranchKey", "Error Loading Facility Branches from cache");
                        }
                    }
                    else
                    {
                        _FacilityKey = facilityBranch.FacilityKey;
                        RaisePropertyChanged("FacilityKey");
                    }
                }
            }
        }

        public override void Clear()
        {
            FacilityKey = 0;
            FacilityBranchKey = 0;
            LoadFacilityCollections();
        }

        List<Facility> _FacilityItemsSource;
        public List<Facility> FacilityItemsSource => _FacilityItemsSource;

        List<FacilityBranch> _FacilityBranchItemsSource;
        public List<FacilityBranch> FacilityBranchItemsSource => _FacilityBranchItemsSource;

        public override IEnumerable<SearchFieldValue> FieldValues()
        {
            yield return new SearchFieldValue { Name = "FacilityKey", Value = FacilityKey.ToString() };
            yield return new SearchFieldValue { Name = "FacilityBranchKey", Value = FacilityBranchKey.ToString() };
        }

        public override void Restore(List<SearchFieldValue> values)
        {
            foreach (var sfv in values)
                if (sfv.Name == "FacilityKey")
                {
                    FacilityKey = Int32.Parse(sfv.Value);
                }
                else if (sfv.Name == "FacilityBranchKey")
                {
                    FacilityBranchKey = Int32.Parse(sfv.Value);
                }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion INotifyPropertyChanged

        #region INotifyDataErrorInfo

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        readonly Dictionary<string, List<string>> _currentErrors;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                //FYI: if you are not supporting entity level errors, it is acceptable to return null
                var ret = _currentErrors.Values.Where(c => c.Any());
                return ret.Any() ? ret : null;
            }

            MakeOrCreatePropertyErrorList(propertyName);
            if (_currentErrors[propertyName].Any())
            {
                return _currentErrors[propertyName];
            }

            return null;
        }

        public bool HasErrors
        {
            get { return _currentErrors.Where(c => c.Value.Any()).Any(); }
        }

        void FireErrorsChanged(string property)
        {
            if (ErrorsChanged != null)
            {
                ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
            }
        }

        public void ClearErrorFromProperty(string property)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Clear();
            FireErrorsChanged(property);
        }

        public void AddErrorForProperty(string property, string error)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Add(error);
            FireErrorsChanged(property);
        }

        void MakeOrCreatePropertyErrorList(string propertyName)
        {
            if (!_currentErrors.ContainsKey(propertyName))
            {
                _currentErrors[propertyName] = new List<string>();
            }
        }

        #endregion INotifyDataErrorInfo
    }

    public class FacilityNameBranchSearchFieldFactory
    {
        public static SearchField Create(XElement searchfield, Action Search)
        {
            return new FacilityNameBranchSearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
            };
        }
    }

    public class FacilityTitleNameBranchSearchField : SearchField, INotifyPropertyChanged, INotifyDataErrorInfo
    {
        public FacilityTitleNameBranchSearchField()
        {
            _currentErrors = new Dictionary<string, List<string>>();
            LoadAllCollections();
        }

        public void LoadAllCollections()
        {
            LoadAllFacilityTypes();
            LoadAllFacilities();
            LoadAllFacilityBranches();
        }


        #region "Facility Types"

        public void LoadAllFacilityTypes()
        {
            FacilityTypeItemsSource = CodeLookupCache.GetCodeLookupsFromType("FacilityType", true, false, true)
                .OrderBy(t => t.Code).ToList();
            FacilityTypeKey = DefaultFacilityTypeKey();
        }

        public int DefaultFacilityTypeKey()
        {
            int defaultFacilityTypeKey = 0;

            if (FacilityTypeItemsSource != null)
            {
                if (FacilityTypeItemsSource.Any())
                {
                    var firstFacilityType = FacilityTypeItemsSource.FirstOrDefault();
                    if (firstFacilityType != null)
                    {
                        defaultFacilityTypeKey = firstFacilityType.CodeLookupKey;
                    }
                }
            }

            return defaultFacilityTypeKey;
        }

        #endregion

        #region "Facilities"

        public void LoadDependentFacilities()
        {
            if (FacilityTypeKey == 0)
            {
                LoadAllFacilities();
            }
            else
            {
                LoadFacilitiesByFacilityType();
            }

            FacilityKey = DefaultFacilityKey();
            LoadDependentFacilityBranches();
            RaisePropertyChanged("FacilityItemsSource");
            RaisePropertyChanged("FacilityKey");
        }

        public void LoadAllFacilities()
        {
            FacilityItemsSource = FacilityCache.GetActiveFacilities(true);
            var _firstFacility = FacilityItemsSource.FirstOrDefault();
            if (_firstFacility != null)
            {
                FacilityKey = _firstFacility.FacilityKey;
            }
        }

        public void LoadFacilitiesByFacilityType()
        {
            FacilityItemsSource = FacilityCache.GetActiveFacilitiesByType(FacilityTypeKey, true);
            FacilityKey = DefaultFacilityKey();
        }

        public int DefaultFacilityKey()
        {
            int defaultFacilityKey = 0;

            if (FacilityItemsSource != null)
            {
                if (FacilityItemsSource.Any())
                {
                    var firstFacility = FacilityItemsSource.FirstOrDefault();
                    defaultFacilityKey = firstFacility.FacilityKey;
                }
            }

            return defaultFacilityKey;
        }

        #endregion

        #region "Facility Branches"

        public void LoadDependentFacilityBranches()
        {
            if (FacilityKey == 0)
            {
                if (FacilityTypeKey == 0)
                {
                    LoadAllFacilityBranches();
                }
                else
                {
                    LoadFacilityBranchesByFacilityType();
                }
            }
            else
            {
                LoadFacilityBranchesByFacility();
            }

            FacilityBranchKey = DefaultFacilityBranchesKey();
            RaisePropertyChanged("FacilityBranchItemsSource");
            RaisePropertyChanged("FacilityBranchKey");
        }

        public void LoadAllFacilityBranches()
        {
            FacilityBranchItemsSource = FacilityCache.GetActiveBranches(true).OrderBy(fb => fb.BranchName).ToList();
            FacilityBranchKey = DefaultFacilityBranchesKey();
        }

        public void LoadFacilityBranchesByFacilityType()
        {
            FacilityBranchItemsSource = FacilityCache.GetActiveFacilityBranchesByFacilityType(FacilityTypeKey, true)
                .OrderBy(fb => fb.BranchName).ToList();
            FacilityBranchKey = DefaultFacilityBranchesKey();
        }

        public void LoadFacilityBranchesByFacility()
        {
            FacilityBranchItemsSource = FacilityCache.GetFacilityBranches(FacilityKey, true)
                .OrderBy(fb => fb.BranchName).ToList();
            FacilityBranchKey = DefaultFacilityBranchesKey();
        }

        public int DefaultFacilityBranchesKey()
        {
            int defaultFacilityBranchKey = 0;

            if (FacilityBranchItemsSource != null)
            {
                if (FacilityBranchItemsSource.Any())
                {
                    var firstFacilityBranch = FacilityBranchItemsSource.FirstOrDefault();
                    defaultFacilityBranchKey = firstFacilityBranch.FacilityBranchKey;
                }
            }

            return defaultFacilityBranchKey;
        }

        #endregion

        int _facilityTypeKey = -1;

        public int FacilityTypeKey
        {
            get { return _facilityTypeKey; }
            set
            {
                if (_facilityTypeKey != value)
                {
                    _facilityTypeKey = value;
                    RaisePropertyChanged("FacilityTypeKey");
                    LoadDependentFacilities();
                }
            }
        }

        int _FacilityKey = -1;

        public int FacilityKey
        {
            get { return _FacilityKey; }
            set
            {
                if (_FacilityKey != value)
                {
                    _FacilityKey = value;
                    RaisePropertyChanged("FacilityKey");
                    LoadDependentFacilityBranches();
                }
            }
        }

        int _facilityBranchKey;

        public int FacilityBranchKey
        {
            get { return _facilityBranchKey; }
            set
            {
                if (_facilityBranchKey != value)
                {
                    _facilityBranchKey = value;
                    RaisePropertyChanged("FacilityBranchKey");
                }
            }
        }

        public override void Clear()
        {
            FacilityKey = 0;
            FacilityBranchKey = 0;
            LoadAllCollections();
        }

        List<CodeLookup> _facilityTypeItemsSource;

        public List<CodeLookup> FacilityTypeItemsSource
        {
            get { return _facilityTypeItemsSource; }
            set
            {
                _facilityTypeItemsSource = value;
                RaisePropertyChanged("FacilityTypeItemsSource");
            }
        }


        List<Facility> _facilityItemsSource;

        public List<Facility> FacilityItemsSource
        {
            get { return _facilityItemsSource; }
            set
            {
                _facilityItemsSource = value;
                RaisePropertyChanged("FacilityItemsSource");
            }
        }

        List<FacilityBranch> _facilityBranchItemsSource;

        public List<FacilityBranch> FacilityBranchItemsSource
        {
            get { return _facilityBranchItemsSource; }
            set
            {
                _facilityBranchItemsSource = value;
                RaisePropertyChanged("FacilityBranchItemsSource");
            }
        }

        public override IEnumerable<SearchFieldValue> FieldValues()
        {
            yield return new SearchFieldValue { Name = "FacilityTypeKey", Value = FacilityTypeKey.ToString() };
            yield return new SearchFieldValue { Name = "FacilityKey", Value = FacilityKey.ToString() };
            yield return new SearchFieldValue { Name = "FacilityBranchKey", Value = FacilityBranchKey.ToString() };
        }

        public override void Restore(List<SearchFieldValue> values)
        {
            foreach (var sfv in values)
                if (sfv.Name == "FacilityTypeKey")
                {
                    FacilityTypeKey = Int32.Parse(sfv.Value);
                }
                else if (sfv.Name == "FacilityKey")
                {
                    FacilityKey = Int32.Parse(sfv.Value);
                }
                else if (sfv.Name == "FacilityBranchKey")
                {
                    FacilityBranchKey = Int32.Parse(sfv.Value);
                }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion INotifyPropertyChanged

        #region INotifyDataErrorInfo

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        readonly Dictionary<string, List<string>> _currentErrors;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                //FYI: if you are not supporting entity level errors, it is acceptable to return null
                var ret = _currentErrors.Values.Where(c => c.Any());
                return ret.Any() ? ret : null;
            }

            MakeOrCreatePropertyErrorList(propertyName);
            if (_currentErrors[propertyName].Any())
            {
                return _currentErrors[propertyName];
            }

            return null;
        }

        public bool HasErrors
        {
            get { return _currentErrors.Where(c => c.Value.Any()).Any(); }
        }

        void FireErrorsChanged(string property)
        {
            if (ErrorsChanged != null)
            {
                ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
            }
        }

        public void ClearErrorFromProperty(string property)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Clear();
            FireErrorsChanged(property);
        }

        public void AddErrorForProperty(string property, string error)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Add(error);
            FireErrorsChanged(property);
        }

        void MakeOrCreatePropertyErrorList(string propertyName)
        {
            if (!_currentErrors.ContainsKey(propertyName))
            {
                _currentErrors[propertyName] = new List<string>();
            }
        }

        #endregion INotifyDataErrorInfo
    }

    public class FacilityTitleNameBranchSearchFieldFactory
    {
        public static SearchField Create(XElement searchfield, Action Search)
        {
            return new FacilityTitleNameBranchSearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
            };
        }
    }

    public class CensusTractZipCodeSearchFieldFactory
    {
        public static CensusTractZipCodeSearchField Create(XElement searchfield, Action Search)
        {
            var zipSearch = new CensusTractZipCodeSearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
                CodeType = (string)searchfield.Attribute("codeType")
            };
            Messenger.Default.Register<CensusTract>(zipSearch, "CensusTractNewZipCode",
                g => zipSearch.RefreshZipCodesList(g));
            return zipSearch;
        }
    }

    public class CensusTractZipCodeSearchField : SearchField, INotifyPropertyChanged, INotifyDataErrorInfo
    {
        public CensusTractZipCodeSearchField()
        {
            _currentErrors = new Dictionary<string, List<string>>();
            censusTractsItemsSource = CensusTractCache.GetCensusTracts().DistinctBy(x => x.ZipCode)
                .OrderBy(g => g.ZipCode).ToObservableCollection();
        }


        private string _codeType;

        public string CodeType
        {
            get { return _codeType; }
            set { _codeType = value; }
        }

        public void RefreshZipCodesList(CensusTract censusTract)
        {
            censusTractsItemsSource = CensusTractCache.GetCensusTracts()
                .DistinctBy(x => x.ZipCode)
                .OrderBy(g => g.ZipCode)
                .ToObservableCollection();
            RaisePropertyChanged("CensusTractsItemsSource");
        }

        int censusTractKey;

        public int CensusTractKey
        {
            get { return censusTractKey; }
            set
            {
                if (censusTractKey != value)
                {
                    censusTractKey = value;
                    RaisePropertyChanged("CensusTractKey");
                }
            }
        }

        public override void Clear()
        {
            CensusTractKey = 0;
        }

        ObservableCollection<CensusTract> censusTractsItemsSource;
        public ObservableCollection<CensusTract> CensusTractsItemsSource => censusTractsItemsSource;

        public override IEnumerable<SearchFieldValue> FieldValues()
        {
            yield return new SearchFieldValue { Name = Field, Value = Value };
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion INotifyPropertyChanged

        #region INotifyDataErrorInfo

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        readonly Dictionary<string, List<string>> _currentErrors;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                //FYI: if you are not supporting entity level errors, it is acceptable to return null
                var ret = _currentErrors.Values.Where(c => c.Any());
                return ret.Any() ? ret : null;
            }

            MakeOrCreatePropertyErrorList(propertyName);
            if (_currentErrors[propertyName].Any())
            {
                return _currentErrors[propertyName];
            }

            return null;
        }

        public bool HasErrors
        {
            get { return _currentErrors.Where(c => c.Value.Any()).Any(); }
        }

        void FireErrorsChanged(string property)
        {
            if (ErrorsChanged != null)
            {
                ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
            }
        }

        public void ClearErrorFromProperty(string property)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Clear();
            FireErrorsChanged(property);
        }

        public void AddErrorForProperty(string property, string error)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Add(error);
            FireErrorsChanged(property);
        }

        void MakeOrCreatePropertyErrorList(string propertyName)
        {
            if (!_currentErrors.ContainsKey(propertyName))
            {
                _currentErrors[propertyName] = new List<string>();
            }
        }

        #endregion INotifyDataErrorInfo
    }

    public class CensusTractStateCodeSearchFieldFactory
    {
        public static CensusTractStateCodeSearchField Create(XElement searchfield, Action Search)
        {
            var stateSearch = new CensusTractStateCodeSearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
                CodeType = (string)searchfield.Attribute("codeType")
            };
            Messenger.Default.Register<CensusTract>(stateSearch, "CensusTractNewStateCode",
                g => stateSearch.RefreshStateCodesList(g));
            return stateSearch;
        }
    }

    public class CensusTractStateCodeSearchField : SearchField, INotifyPropertyChanged, INotifyDataErrorInfo
    {
        public CensusTractStateCodeSearchField()
        {
            _currentErrors = new Dictionary<string, List<string>>();
            censusTractsItemsSource = CensusTractCache.GetCensusTracts().DistinctBy(g => g.State).OrderBy(g => g.State)
                .ToObservableCollection();
        }

        private string _codeType;

        public string CodeType
        {
            get { return _codeType; }
            set { _codeType = value; }
        }

        public void RefreshStateCodesList(CensusTract censusTract)
        {
            censusTractsItemsSource = CensusTractCache.GetCensusTracts()
                .DistinctBy(g => g.State)
                .OrderBy(g => g.State)
                .ToObservableCollection();
            RaisePropertyChanged("CensusTractsItemsSource");
        }

        int censusTractKey;

        public int CensusTractKey
        {
            get { return censusTractKey; }
            set
            {
                if (censusTractKey != value)
                {
                    censusTractKey = value;
                    RaisePropertyChanged("CensusTractKey");
                }
            }
        }

        public override void Clear()
        {
            CensusTractKey = 0;
        }

        ObservableCollection<CensusTract> censusTractsItemsSource;
        public ObservableCollection<CensusTract> CensusTractsItemsSource => censusTractsItemsSource;

        public override IEnumerable<SearchFieldValue> FieldValues()
        {
            yield return new SearchFieldValue { Name = Field, Value = Value };
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion INotifyPropertyChanged

        #region INotifyDataErrorInfo

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        readonly Dictionary<string, List<string>> _currentErrors;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                //FYI: if you are not supporting entity level errors, it is acceptable to return null
                var ret = _currentErrors.Values.Where(c => c.Any());
                return ret.Any() ? ret : null;
            }

            MakeOrCreatePropertyErrorList(propertyName);
            if (_currentErrors[propertyName].Any())
            {
                return _currentErrors[propertyName];
            }

            return null;
        }

        public bool HasErrors
        {
            get { return _currentErrors.Where(c => c.Value.Any()).Any(); }
        }

        void FireErrorsChanged(string property)
        {
            if (ErrorsChanged != null)
            {
                ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
            }
        }

        public void ClearErrorFromProperty(string property)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Clear();
            FireErrorsChanged(property);
        }

        public void AddErrorForProperty(string property, string error)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Add(error);
            FireErrorsChanged(property);
        }

        void MakeOrCreatePropertyErrorList(string propertyName)
        {
            if (!_currentErrors.ContainsKey(propertyName))
            {
                _currentErrors[propertyName] = new List<string>();
            }
        }

        #endregion INotifyDataErrorInfo
    }

    public class CensusTractCountySearchFieldFactory
    {
        public static CensusTractCountySearchField Create(XElement searchfield, Action Search)
        {
            var countySearch = new CensusTractCountySearchField
            {
                Field = (string)searchfield.Attribute("field"),
                Label = (string)searchfield.Attribute("label"),
                Type = (string)searchfield.Attribute("type"),
                SaveState = searchfield.Attribute("persist") != null && (bool)searchfield.Attribute("persist"),
                CodeType = (string)searchfield.Attribute("codeType")
            };
            Messenger.Default.Register<CensusTract>(countySearch, "CensusTractNewCountyCode",
                g => countySearch.RefreshCountyCodesList(g));
            return countySearch;
        }
    }

    public class CensusTractCountySearchField : SearchField, INotifyPropertyChanged, INotifyDataErrorInfo
    {
        public CensusTractCountySearchField()
        {
            _currentErrors = new Dictionary<string, List<string>>();
            censusTractsItemsSource = CensusTractCache.GetCensusTracts().DistinctBy(g => g.County)
                .OrderBy(g => g.County).ToObservableCollection();
        }

        private string _codeType;

        public string CodeType
        {
            get { return _codeType; }
            set { _codeType = value; }
        }

        public void RefreshCountyCodesList(CensusTract censusTract)
        {
            censusTractsItemsSource = CensusTractCache.GetCensusTracts()
                .DistinctBy(g => g.County)
                .OrderBy(g => g.County)
                .ToObservableCollection();
            RaisePropertyChanged("CensusTractsItemsSource");
        }

        int censusTractKey;

        public int CensusTractKey
        {
            get { return censusTractKey; }
            set
            {
                if (censusTractKey != value)
                {
                    censusTractKey = value;
                    RaisePropertyChanged("CensusTractKey");
                }
            }
        }

        public override void Clear()
        {
            CensusTractKey = 0;
        }

        ObservableCollection<CensusTract> censusTractsItemsSource;
        public ObservableCollection<CensusTract> CensusTractsItemsSource => censusTractsItemsSource;

        public override IEnumerable<SearchFieldValue> FieldValues()
        {
            yield return new SearchFieldValue { Name = Field, Value = Value };
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion INotifyPropertyChanged

        #region INotifyDataErrorInfo

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        readonly Dictionary<string, List<string>> _currentErrors;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                //FYI: if you are not supporting entity level errors, it is acceptable to return null
                var ret = _currentErrors.Values.Where(c => c.Any());
                return ret.Any() ? ret : null;
            }

            MakeOrCreatePropertyErrorList(propertyName);
            if (_currentErrors[propertyName].Any())
            {
                return _currentErrors[propertyName];
            }

            return null;
        }

        public bool HasErrors
        {
            get { return _currentErrors.Where(c => c.Value.Any()).Any(); }
        }

        void FireErrorsChanged(string property)
        {
            if (ErrorsChanged != null)
            {
                ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
            }
        }

        public void ClearErrorFromProperty(string property)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Clear();
            FireErrorsChanged(property);
        }

        public void AddErrorForProperty(string property, string error)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Add(error);
            FireErrorsChanged(property);
        }

        void MakeOrCreatePropertyErrorList(string propertyName)
        {
            if (!_currentErrors.ContainsKey(propertyName))
            {
                _currentErrors[propertyName] = new List<string>();
            }
        }

        #endregion INotifyDataErrorInfo
    }
}