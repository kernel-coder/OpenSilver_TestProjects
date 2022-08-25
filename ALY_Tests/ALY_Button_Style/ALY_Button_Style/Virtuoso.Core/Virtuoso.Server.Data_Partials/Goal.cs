#region Usings

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class Goal
    {
        public bool IsLongDescriptionParameterized
        {
            get
            {
                if (string.IsNullOrWhiteSpace(LongDescription))
                {
                    return false;
                }

                for (var i = 0; i < 11; i++)
                    if (LongDescription.Contains("{" + i.ToString().Trim() + "}"))
                    {
                        return true;
                    }

                return false;
            }
        }

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

        public string EditCodeValue
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(CodeValue))
                {
                    return string.Format("Goal {0}", CodeValue.Trim());
                }

                return IsNew ? "New Goal" : "Edit Goal";
            }
        }

        public QuestionGoal CurrentQuestionGoal
        {
            get
            {
                var cq = QuestionGoal.FirstOrDefault();
                if (cq == null)
                {
                    cq = new QuestionGoal();
                    QuestionGoal.Add(cq);
                }

                cq.PropertyChanged += ChildPropertyChanged;
                return cq;
            }
        }

        partial void OnAllowEditChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (AllowEdit)
            {
                MustEdit = false;
            }
        }

        partial void OnMustEditChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (MustEdit)
            {
                AllowEdit = false;
            }
        }

        partial void OnLongDescriptionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsLongDescriptionParameterized");
            if (IsLongDescriptionParameterized)
            {
                MustEdit = true;
            }
        }

        partial void OnCodeValueChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("EditCodeValue");
        }

        private void ChildPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IsEditting && e.PropertyName.Equals("Condition"))
            {
                RefreshRaiseChanged();
            }
        }

        public void RefreshRaiseChanged()
        {
            RaisePropertyChanged("This");
        }
    }

    public partial class GoalElement
    {
        // Used to display * in SearchResultsView
        public string IsInactiveIndicator => Inactive ? "*" : string.Empty;

        public string ShortDescriptionFirst20
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ShortDescription))
                {
                    return null;
                }

                return ShortDescription.Length <= 20 ? ShortDescription : ShortDescription.Substring(0, 20);
            }
        }

        public int LongDescriptionMaxLength => TelephonyFlag ? 50 : 250;

        public int ShortDescriptionMaxLength => TelephonyFlag ? 100 : 100;

        public bool IsLongDescriptionParameterized
        {
            get
            {
                if (string.IsNullOrWhiteSpace(LongDescription))
                {
                    return false;
                }

                for (var i = 0; i < 11; i++)
                    if (LongDescription.Contains("{" + i.ToString().Trim() + "}"))
                    {
                        return true;
                    }

                return false;
            }
        }

        public string EditShortDescription
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ShortDescription))
                {
                    return string.Format("Goal Element {0}", ShortDescription.Trim());
                }

                return IsNew ? "New Goal Element (Treatments/Orders)" : "Edit Goal Element (Treatments/Orders)";
            }
        }

        public string POCOvrTextOrLongDescription =>
            string.IsNullOrEmpty(POCOverrideText) ? LongDescription : POCOverrideText;

        partial void OnCreated()
        {
            GoalElementResponseTypeKey = 1;
            TelephonyFlag = false;
        }

        public bool Validate()
        {
            var allValid = true;
            TelephonyFlagSetup();
            if (Orders == false)
            {
                POCOverrideCode = null;
                POCOverrideText = null;
            }

            return allValid;
        }

        private void TelephonyFlagSetup()
        {
            LongDescription = string.IsNullOrWhiteSpace(LongDescription)
                ? null
                : LongDescription = LongDescription.Trim();

            POCOverrideText = string.IsNullOrWhiteSpace(POCOverrideText)
                ? null
                : POCOverrideText = POCOverrideText.Trim();

            ShortDescription = string.IsNullOrWhiteSpace(ShortDescription) ? null : ShortDescription.Trim();

            if (string.IsNullOrWhiteSpace(LongDescription) == false && LongDescription.Length > LongDescriptionMaxLength)
            {
                LongDescription = LongDescription.Substring(0, LongDescriptionMaxLength);
            }

            if (string.IsNullOrWhiteSpace(POCOverrideText) == false && POCOverrideText.Length > LongDescriptionMaxLength)
            {
                POCOverrideText = POCOverrideText.Substring(0, LongDescriptionMaxLength);
            }

            if (string.IsNullOrWhiteSpace(ShortDescription) == false && ShortDescription.Length > ShortDescriptionMaxLength)
            {
                ShortDescription = ShortDescription.Substring(0, ShortDescriptionMaxLength);
            }

            if (TelephonyFlag)
            {
                AllowEdit = false;
                MustEdit = false;
            }
            else
            {
                GoalElementResponseTypeKey = 1;
            }
        }

        partial void OnTelephonyFlagChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            TelephonyFlagSetup();

            RaisePropertyChanged("LongDescriptionMaxLength");
            RaisePropertyChanged("ShortDescriptionMaxLength");
        }

        partial void OnAllowEditChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (AllowEdit)
            {
                MustEdit = false;
            }
        }

        partial void OnMustEditChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (MustEdit)
            {
                AllowEdit = false;
            }
        }

        partial void OnLongDescriptionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsLongDescriptionParameterized");

            if (IsLongDescriptionParameterized)
            {
                MustEdit = true;
            }
        }

        partial void OnShortDescriptionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("EditShortDescription");
        }
    }

    public partial class DisciplineInGoalElement
    {
        public bool DisciplineInGoalElementInList(List<int> disciplineList)
        {
            if (disciplineList == null)
            {
                return false;
            }

            foreach (var disciplineKey in disciplineList)
                if (disciplineKey == DisciplineKey)
                {
                    return true;
                }

            return false;
        }
    }

    public partial class QuestionGoal
    {
        // TODO returns itself - why?
        public QuestionGoal CurrentQuestionGoal
        {
            get
            {
                var cq = this;
                return cq;
            }
        }

        partial void OnConditionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("Condition");

            PropertyChanged += ChildPropertyChanged;
        }

        private void QuestionGoal_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RefreshRaiseChanged();
        }

        private void ChildPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Condition"))
            {
                RefreshRaiseChanged();
            }
        }

        public void RefreshRaiseChanged()
        {

        }
    }
}