using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Virtuoso.Server.Data
{
    public partial class CodeLookupHeader
    {
        public string CodeType { get; set; }

        public string CodeTypeDescription { get; set; }

        public bool IsNew { get; set; }

        public string TabHeader
        {
            get
            {
                return IsNew ? "New CodeLookup" : "Edit CodeLookup";
            }
        }

        public bool HasParent => false;

        public bool ShowChildrenWhenPresent => false;

        public string ParentCodeTypeDescription
        {
            get
            {
                return string.Empty;
            }
        }
    }

    public partial class CodeLookup
    {
        public int? CodeLookupKey { get; set; }
        public bool Inactive { get; set; }

        private string _displayMember;


        // Used to display * in SearchResultsView
        public string IsInactiveIndicator
        {
            get
            {
                if (Inactive)
                {
                    return "*";
                }

                return string.Empty;
            }
        }

        public string Code { get; set; }
        public string CodeDescription { get; set; }

        public string CodeDashDescription
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Code) && string.IsNullOrWhiteSpace(CodeDescription))
                {
                    return null;
                }

                return string.Format("{0} - {1}",
                    Code == null ? "" : Code.Trim(),
                    CodeDescription == null ? "" : CodeDescription.Trim());
            }
            set { }
        }

        public int? NullableCodeLookupKey { get; set; }

        public string DisplayMember
        {
            get { return _displayMember; }
            set
            {
                _displayMember = value;
            }
        }

        public bool HasParent => false;

        public string ParentCodeType
        {
            get
            {

                return string.Empty;
            }
        }

        public ObservableCollection<CodeLookup> ChildrenForView
        {
            get
            {
                return new ObservableCollection<CodeLookup>();
            }
        }

        public bool HasChildren => ChildrenForView.Count > 0;

        public bool ShowChildren => false;

        public string InactivationInformation
        {
            get
            {
                return string.Empty;
            }
        }

        public bool IsInactive
        {
            get { return Inactive; }
            set
            {
            }
        }

        public void RaisePropertyChangedDisplayMember()
        {

        }

        public override string ToString()
        {
            // NOTE: this method overridden for control autoCompleteCombo
            return CodeDescription;
        }

        public bool ApplicationDataContains(string s)
        {
            return false;
        }
    }
}
