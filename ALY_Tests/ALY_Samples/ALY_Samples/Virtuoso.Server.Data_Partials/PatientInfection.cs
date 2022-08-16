#region Usings

using System.Windows;
using System.Windows.Documents;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Occasional;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class PatientInfection
    {
        private int? _ConfirmationOtherKey;
        private int? _PathogenOtherKey;
        private int? _SiteOtherKey;

        private int SiteOtherKey
        {
            get
            {
                if (!_SiteOtherKey.HasValue)
                {
                    _SiteOtherKey = CodeLookupCache.GetKeyFromCode("InfectionSite", "OTHER");
                }

                return _SiteOtherKey.HasValue ? _SiteOtherKey.Value : 0;
            }
        }

        private int ConfirmationOtherKey
        {
            get
            {
                if (!_ConfirmationOtherKey.HasValue)
                {
                    _ConfirmationOtherKey = CodeLookupCache.GetKeyFromCode("InfectionConfirmation", "OTHER");
                }

                return _ConfirmationOtherKey.HasValue ? _ConfirmationOtherKey.Value : 0;
            }
        }

        private int PathogenOtherKey
        {
            get
            {
                if (!_PathogenOtherKey.HasValue)
                {
                    _PathogenOtherKey = CodeLookupCache.GetKeyFromCode("InfectionPathogen", "OTHER");
                }

                return _PathogenOtherKey.HasValue ? _PathogenOtherKey.Value : 0;
            }
        }

        public bool ShowInfectionSiteOther => InfectionSiteKey == SiteOtherKey;

        public bool ShowInfectionConfirmationOther => InfectionConfirmationKey == ConfirmationOtherKey;

        public bool ShowInfectionPathogenOther => InfectionPathogenKey == PathogenOtherKey;

        public string InfectionSiteDescription => FormatWithOther(InfectionSiteKey, SiteOtherKey, InfectionSiteOther);

        public string InfectionConfirmationDescription => FormatWithOther(InfectionConfirmationKey,
            ConfirmationOtherKey, InfectionConfirmationOther);

        public string InfectionPathogenDescription =>
            FormatWithOther(InfectionPathogenKey, PathogenOtherKey, InfectionPathogenOther);

        public string TransmissionPrecautionsFormatted
        {
            get
            {
                if (TransmissionPrecautions == null)
                {
                    return string.Empty;
                }

                return TransmissionPrecautions.Replace(" - ", "\r\n");
            }
        }

        public bool NotPresentAtSOCROC
        {
            get { return PresentAtSOCROC != null && !PresentAtSOCROC.Value; }
            set
            {
                if (value)
                {
                    PresentAtSOCROC = false;
                }
            }
        }

        public static Paragraph POAHelp
        {
            get
            {
                var templateParagraph = new Paragraph();
                var content = templateParagraph.Inlines;

                AddText(content,
                    "Present on Admission (POA) is defined as 'Present at SOC/ROC or within 48 Hrs. of SOC/ROC'.");
                content.Add(RichTextHelper.NewLine());
                return templateParagraph;
            }
        }

        public static Paragraph TransmissionPrecautionsHelp
        {
            get
            {
                var templateParagraph = new Paragraph();
                var content = templateParagraph.Inlines;

                AddHeader(content, "Contact Precautions");
                AddText(content,
                    "Indicated in the care of patients known or suspected to have a serious illness easily transmitted by direct patient contact or by indirect contact with items in the patient’s environment.");
                AddText(content,
                    "Illnesses requiring contact precautions may include, but are not limited to: presence of stool incontinence (may include patients with norovirus, rotavirus, or Clostridium difficile), draining wounds, uncontrolled secretions, pressure ulcers, presence of generalized rash, or presence of ostomy tubes and/or bags draining body fluids.");
                AddBullet(content,
                    "Wear gloves when touching the patient and the patient’s immediate environment or belongings");
                AddBullet(content,
                    "Wear a fluid resistant, non-sterile gown if substantial contact with the patient or their environment is anticipated");
                AddBullet(content,
                    "Effective hand hygiene requires soap and water. Alcohol-based preparations are considered ineffective with C. Difficile");
                content.Add(RichTextHelper.NewLine());

                AddHeader(content, "Droplet Precautions");
                AddText(content,
                    "Droplets can be generated from the source person during coughing, sneezing, talking and during the performance of certain procedures such as suctioning or bronchoscopy. Droplets may contain microorganisms and generally travel no more than 3 feet from the patient.");
                AddText(content,
                    "These droplets can be deposited on the host’s nasal mucosa, conjunctivae or mouth. Diseases requiring droplet precautions include, but are not limited to: Pertussis, Influenza, Diphtheria and invasive Neisseria meningitis.");
                AddBullet(content,
                    "Wear a facemask, such as a procedure or surgical mask, for close contact (within 3 feet of the patient) with the patient");
                content.Add(RichTextHelper.NewLine());

                AddHeader(content, "Airborne Precautions");
                AddText(content,
                    "Diseases requiring airborne precautions include, but are not limited to: Measles, Severe Acute Respiratory Syndrome (SARS), Varicella (chickenpox), and Mycobacterium tuberculosis. Airborne precautions apply to patients known or suspected to be infected with microorganisms transmitted by airborne droplet nuclei.");
                AddBullet(content,
                    "Preventing airborne transmission requires personal respiratory protection (N95 Respirator). Prior fit-testing that must be repeated annually and fit-check / seal-check prior to each use.");
                content.Add(RichTextHelper.NewLine());

                AddHeader(content, "Full Barrier Precautions");
                AddText(content,
                    "Diseases requiring full barrier precautions include, but are not limited to: Severe Acute Respiratory Syndrome (SARS) and all known and suspect avian and pandemic influenza patients.");
                AddText(content,
                    "Full barrier precautions are a combination of airborne and contact precautions, plus eye protection, in addition to standard precautions.");
                content.Add(RichTextHelper.NewLine());
                return templateParagraph;
            }
        }

        public PatientInfection CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newInf = (PatientInfection)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newInf);
            if (newInf.HistoryKey == null)
            {
                newInf.HistoryKey = PatientInfectionKey;
            }

            RejectChanges();
            BeginEditting();
            Superceded = true;
            EndEditting();
            return newInf;
        }

        partial void OnInfectionSiteKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (!ShowInfectionSiteOther)
            {
                InfectionSiteOther = null;
            }

            RaisePropertyChanged("InfectionSiteOther");
            RaisePropertyChanged("ShowInfectionSiteOther");
            RaisePropertyChanged("InfectionSiteDescription");
        }

        partial void OnInfectionConfirmationKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (!ShowInfectionConfirmationOther)
            {
                InfectionConfirmationOther = null;
            }

            RaisePropertyChanged("InfectionConfirmationOther");
            RaisePropertyChanged("ShowInfectionConfirmationOther");
            RaisePropertyChanged("InfectionConfirmationDescription");
        }

        partial void OnInfectionPathogenKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (!ShowInfectionPathogenOther)
            {
                InfectionPathogenOther = null;
            }

            RaisePropertyChanged("InfectionPathogenOther");
            RaisePropertyChanged("ShowInfectionPathogenOther");
            RaisePropertyChanged("InfectionPathogenDescription");
        }

        partial void OnTransmissionPrecautionsChanged()
        {
            RaisePropertyChanged("TransmissionPrecautionsFormatted");
        }

        partial void OnPresentAtSOCROCChanged()
        {
            RaisePropertyChanged("NotPresentAtSOCROC");
        }

        private string FormatWithOther(int? ThisKey, int OtherKey, string OtherValue)
        {
            if (ThisKey.HasValue)
            {
                if (ThisKey.Value == OtherKey)
                {
                    return "Other: " + OtherValue;
                }

                var cl = CodeLookupCache.GetCodeLookupFromKey(ThisKey.Value);
                if (cl != null)
                {
                    return cl.CodeDescription;
                }
            }

            return string.Empty;
        }

        private static void AddHeader(InlineCollection templateContent, string text)
        {
            var span = RichTextHelper.BoldLine(text);
            span.TextDecorations = TextDecorations.Underline;
            templateContent.Add(span);
        }

        private static void AddText(InlineCollection templateContent, string text)
        {
            templateContent.Add(RichTextHelper.PlainLine(text));
            templateContent.Add(RichTextHelper.NewLine());
        }

        private static void AddBullet(InlineCollection templateContent, string text)
        {
            templateContent.Add(RichTextHelper.BulletLine(text));
        }
    }
}