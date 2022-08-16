namespace Virtuoso.Server.Data
{
    public partial class EncounterSepsis
    {
        public bool PotentialInfectionIsYes => PotentialInfection == "1";

        public bool ShowPotentialInfectionComment => PotentialInfectionIsYes;

        public bool SystemicCriteriaIsYes => SystemicCriteria == "1";

        public bool EnableSystemicCriteria => SystemicCriteriaIsYes;

        public bool OrganDysfunctionCriteriaIsYes => OrganDysfunctionCriteria == "1";

        public bool ShowOrganDysfunctionCriteriaComment => OrganDysfunctionCriteriaIsYes;

        partial void OnPotentialInfectionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ShowPotentialInfectionComment");
            if (!PotentialInfectionIsYes)
            {
                PotentialInfectionComment = null;
            }

            SetFollowup();
        }

        partial void OnSystemicCriteriaChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("EnableSystemicCriteria");
            if (!SystemicCriteriaIsYes)
            {
                SystemicCriteriaFevor = false;
                SystemicCriteriaTachycardia = false;
                SystemicCriteriaTachypnea = false;
            }

            SetFollowup();
        }

        partial void OnOrganDysfunctionCriteriaChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ShowOrganDysfunctionCriteriaComment");
            if (!OrganDysfunctionCriteriaIsYes)
            {
                OrganDysfunctionCriteriaComment = null;
            }

            SetFollowup();
        }

        public void SetFollowup()
        {
            if (string.IsNullOrWhiteSpace(PotentialInfection) || string.IsNullOrWhiteSpace(SystemicCriteria) ||
                string.IsNullOrWhiteSpace(OrganDysfunctionCriteria))
            {
                Followup = null;
            }
            else if (!PotentialInfectionIsYes && !SystemicCriteriaIsYes && !OrganDysfunctionCriteriaIsYes)
            {
                Followup = "Screening is complete.  No further follow-up required.";
            }
            else if (PotentialInfectionIsYes && !SystemicCriteriaIsYes && !OrganDysfunctionCriteriaIsYes)
            {
                Followup = "Patient Meets Criteria for Infection.  Educate patient on signs and symptoms of Sepsis.";
            }
            else if (!PotentialInfectionIsYes && !SystemicCriteriaIsYes && OrganDysfunctionCriteriaIsYes ||
                     !PotentialInfectionIsYes && SystemicCriteriaIsYes && OrganDysfunctionCriteriaIsYes ||
                     PotentialInfectionIsYes && !SystemicCriteriaIsYes && OrganDysfunctionCriteriaIsYes)
            {
                Followup =
                    "Patient Meets Criteria for MD Notification.  Educate patient on signs and symptoms of Sepsis, notify MD of findings, and document.";
            }
            else if (PotentialInfectionIsYes && SystemicCriteriaIsYes && !OrganDysfunctionCriteriaIsYes ||
                     !PotentialInfectionIsYes && SystemicCriteriaIsYes && !OrganDysfunctionCriteriaIsYes)
            {
                Followup =
                    "Patient Meets Criteria for Sepsis.  Document findings, educate patient on signs and symptoms of Sepsis and treatment, and notify the provider and obtain MD order to draw CBC.";
            }
            else if (PotentialInfectionIsYes && SystemicCriteriaIsYes && OrganDysfunctionCriteriaIsYes)
            {
                Followup =
                    "Patient Meets Criteria for SEVERE Sepsis.  Document findings, educate patient on signs and symptoms of Sepsis and treatment, and notify the provider and have patient transported to emergency department for evaluation.";
            }
            else
            {
                Followup = null;
            }
        }
    }
}