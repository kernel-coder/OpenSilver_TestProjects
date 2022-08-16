#region Usings

using System.Windows;
using System.Windows.Documents;
using Virtuoso.Core.Controls;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class PatientInfection
    {
        private int? _ConfirmationOtherKey;
        private int? _PathogenOtherKey;
        private int? _SiteOtherKey;

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