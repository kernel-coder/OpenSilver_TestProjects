#region Usings

using System;
using System.Collections.ObjectModel;
using Virtuoso.Core;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class CodeLookupHeader
    {
        public string TabHeader
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(CodeTypeDescription))
                {
                    return string.Format("CodeLookup {0}", CodeTypeDescription.Trim());
                }

                return IsNew ? "New CodeLookup" : "Edit CodeLookup";
            }
        }

        public bool HasParent => ParentCodeLookupHeaderKey.HasValue;

        public bool ShowChildrenWhenPresent => WebContext.Current.User != null && WebContext.Current.User.DeltaUser;

        public string ParentCodeTypeDescription
        {
            get
            {
                var key = ParentCodeLookupHeaderKey;
                if (key.HasValue)
                {
                    var Parent = ParentCodeLookupHeader;
                    if (Parent == null)
                    {
                        Parent = CodeLookupCache.GetCodeLookupHeaderFromKey(key.Value);
                    }

                    if (Parent != null)
                    {
                        return Parent.CodeTypeDescription;
                    }
                }

                return string.Empty;
            }
        }

        partial void OnCodeTypeDescriptionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TabHeader");
        }
    }

    public partial class CodeLookup
    {
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

        public int? NullableCodeLookupKey
        {
            get { return CodeLookupKey <= 0 ? null : (int?)CodeLookupKey; }
            set { CodeLookupKey = value ?? 0; }
        }

        public string DisplayMember
        {
            get { return _displayMember; }
            set
            {
                _displayMember = value;
                RaisePropertyChanged("DisplayMember");
            }
        }

        public bool HasParent => ParentCodeLookupKey.HasValue;

        public string ParentCodeType
        {
            get
            {
                var parentheaderkey = CodeLookupHeader.ParentCodeLookupHeaderKey;
                if (parentheaderkey.HasValue)
                {
                    var parentheader = CodeLookupCache.GetCodeLookupHeaderFromKey(parentheaderkey.Value);
                    if (parentheader != null)
                    {
                        return parentheader.CodeType;
                    }
                }

                return string.Empty;
            }
        }

        public ObservableCollection<CodeLookup> ChildrenForView
        {
            get
            {
                var results = CodeLookupCache.GetChildrenFromKey(CodeLookupKey);
                return results.ToObservableCollection();
            }
        }

        public bool HasChildren => ChildrenForView.Count > 0;

        public bool ShowChildren => CodeLookupHeader.ShowChildrenWhenPresent && HasChildren;

        public string InactivationInformation
        {
            get
            {
                if (Inactive)
                {
                    var DateFormatted =
                        InactiveDate.HasValue ? InactiveDate.Value.ToString("MM/dd/yyyy") : string.Empty;
                    var UserName = InactivatedBy != null
                        ? UserCache.Current.GetFormalNameFromUserId(InactivatedBy)
                        : string.Empty;
                    return string.IsNullOrEmpty(UserName)
                        ? string.Empty
                        : "Inactivated By " + UserName + " " + DateFormatted;
                }

                return string.Empty;
            }
        }

        public bool IsInactive
        {
            get { return Inactive; }
            set
            {
                var newValue = value;
                if (newValue)
                {
                    if (InactiveDate == null)
                    {
                        InactiveDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                        InactivatedBy = WebContext.Current.User.MemberID;
                    }
                }
                else
                {
                    InactiveDate = null;
                    InactivatedBy = null;
                }

                Inactive = newValue;
                RaisePropertyChanged("InactivationInformation");
                RaisePropertyChanged("Inactive");
            }
        }
        public int SequenceSortable
        {
            get
            {
                return Sequence == null ? Int32.MaxValue : Sequence.Value;
            }
        }
        public void RaisePropertyChangedDisplayMember()
        {
            RaisePropertyChanged("DisplayMember");
        }

        public override string ToString()
        {
            // NOTE: this method overridden for control autoCompleteCombo
            return CodeDescription;
        }

        public bool ApplicationDataContains(string s)
        {
            if (string.IsNullOrWhiteSpace(ApplicationData))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(s))
            {
                return true;
            }

            return ApplicationData.Trim().ToLower().Contains(s.Trim().ToLower());
        }
    }
}