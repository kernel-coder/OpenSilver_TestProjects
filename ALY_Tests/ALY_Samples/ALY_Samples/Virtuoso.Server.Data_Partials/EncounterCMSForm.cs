#region Usings

using System;
using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class EncounterCMSForm
    {
    }

    public partial class EncounterCMSFormField
    {
        private CMSFormField _FormField;

        public CMSFormField FormField
        {
            get
            {
                if (_FormField != null)
                {
                    return _FormField;
                }

                _FormField = CMSFormCache.GetCMSFormFieldByKey(CMSFormFieldKey == null ? 0 : (int)CMSFormFieldKey);
                return _FormField;
            }
        }

        public string DeltaFieldName => FormField?.DeltaFieldName;
        public string Label => FormField?.Label;
        public string PDFFieldName => FormField?.PDFFieldName;

        public string PDFFieldNameLabel
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Label) == false)
                {
                    return Label;
                }

                // Default label from PDFFieldName, adding space between each capatialized word (eg, "MedicationEndDate" becomes "Medication End Date"
                var pdfFieldName = PDFFieldName;
                if (string.IsNullOrWhiteSpace(pdfFieldName))
                {
                    return null;
                }

                string label = null;
                foreach (var c in pdfFieldName)
                {
                    if (label != null && c >= 'A' && c <= 'Z')
                    {
                        label = label + " ";
                    }

                    label = label + c;
                }

                return label;
            }
        }

        public int DataType => FormField?.DataType ?? 1;
        public bool Required => FormField?.Required ?? false;
        public bool ReadOnly => FormField?.ReadOnly ?? false;

        public string DateTimeDataMMMdyyyy
        {
            get
            {
                if (DateTimeData == null)
                {
                    return null;
                }

                return ((DateTime)DateTimeData).ToString("MMM d yyyy");
            }
        }

        public bool DataType_IsText => DataType == 1;
        public bool DataType_IsBool => DataType == 2;
        public bool DataType_IsDate => DataType == 3;
        public bool DataType_IsSignature => DataType == 4;
        public bool DataType_IsInt => DataType == 5;
        public bool DataType_IsYesNo => DataType == 6;
    }
}