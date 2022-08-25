#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;

#endregion

namespace Virtuoso.Core.Services
{
    public interface ISearchMetadata
    {
        string FieldName { get; }
    }

    public interface ISearch : INotifyPropertyChanged
    {
        Action OnItemSelected
        {
            set;
        } //used to pass 'callback' to VM to notify parent screen that item was selected - to enable base screen UI updates

        Action SelectAction { set; } //used to pass 'callback' to VM for issuing a select on double click

        object SelectedItem { get; }
        int TotalRecords { get; }
        string SelectedField { get; }
        string SelectedValue { get; }

        //isSystemSearch == true means that search was launched from Main menu, instead of from SmartCombo
        void Search(bool isSystemSearch, List<SearchParameter> parameters);

        void ClearResults();
    }

    public interface ISearchWithQueryLimit : ISearch
    {
        int MAX_RECORDS { get; }
        string MaxRecordsDisplayText { get; }
    }
    public interface ISearchWithServiceLineTypeFilter
    {
        int? ServiceLineTypeFilter { get; set; }
    }
}