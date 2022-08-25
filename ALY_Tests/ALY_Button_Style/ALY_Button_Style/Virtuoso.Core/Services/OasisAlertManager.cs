#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Utility;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public class OasisAlertManager : GenericBase
    {
        public IDynamicFormService FormModel { get; set; }
        private Admission CurrentAdmission { get; set; }
        private Encounter _CurrentEncounter;

        private Encounter CurrentEncounter
        {
            get { return _CurrentEncounter; }
            set
            {
                _CurrentEncounter = value;
                RaisePropertyChanged("CurrentEncounter");
            }
        }

        private EncounterOasis _CurrentEncounterOasis;

        private EncounterOasis CurrentEncounterOasis
        {
            get { return _CurrentEncounterOasis; }
            set
            {
                _CurrentEncounterOasis = value;
                RaisePropertyChanged("CurrentEncounterOasis");
            }
        }

        private EncounterOasis CurrentEncounterOasisStart { get; set; }

        private Guid CurrentOasisManagerGUID { get; set; }
        private int CurrentOasisVersionKey { get; set; }
        private bool ST_Episode { get; set; }
        private bool AlertsActive { get; set; }

        public static OasisAlertManager Create(Guid oasisManagerGUID, Encounter encounter,
            IDynamicFormService pFormModel)
        {
            return new OasisAlertManager(oasisManagerGUID, encounter, pFormModel);
        }

        private OasisAlertManager(Guid oasisManagerGUID, Encounter encounter, IDynamicFormService pFormModel)
        {
            FormModel = pFormModel;
            if (CurrentAdmission != null)
            {
                CurrentAdmission.Cleanup();
            }

            CurrentAdmission = null;
            if (CurrentEncounter != null)
            {
                CurrentEncounter.Cleanup();
            }

            CurrentEncounter = null;
            ST_Episode = false;
            AlertsActive = false;
            CurrentOasisManagerGUID = oasisManagerGUID;

            if (encounter == null)
            {
                return;
            }

            CurrentAdmission = encounter.Admission;
            CurrentEncounter = encounter;
            if (CurrentEncounter.EncounterOasis != null)
            {
                CurrentEncounterOasis = CurrentEncounter.EncounterOasis
                    .Where(e => ((e.InactiveDate == null) && (e.REC_ID != "X1"))).OrderBy(e => e.AddedDate)
                    .FirstOrDefault();
            }

            if ((CurrentAdmission == null) || (CurrentEncounter == null) || (CurrentEncounterOasis == null))
            {
                return;
            }

            CurrentOasisVersionKey = CurrentEncounterOasis.OasisVersionKey;
            if ((CurrentEncounter.EncounterOasisRFA != "06") && (CurrentEncounter.EncounterOasisRFA != "07") &&
                (CurrentEncounter.EncounterOasisRFA != "08") && (CurrentEncounter.EncounterOasisRFA != "09"))
            {
                return;
            }

            EncounterOasisAlert eoa = CurrentEncounter.GetFirstEncounterOasisAlert();
            if (eoa != null)
            {
                Encounter startEncounter = CurrentAdmission.Encounter.FirstOrDefault(e => (e.EncounterKey == eoa.EncounterStartKey));
                if (startEncounter != null)
                {
                    CurrentEncounterOasisStart = startEncounter.EncounterOasis
                        .Where(e => ((e.InactiveDate == null) && (e.REC_ID != "X1")))
                        .OrderByDescending(e => e.AddedDate).FirstOrDefault();
                }

                ST_Episode = eoa.ST_Episode;

                AlertsActive = false;
                return;
            }
            
            Encounter mostRecentOasisEncounterSOCROC = CurrentAdmission.Encounter
                .Where(eo =>
                    (((eo.EncounterOasisRFA == "01") || (eo.EncounterOasisRFA == "03")) &&
                     (eo.EncounterOasisM0090 != null) && (eo.EncounterKey != CurrentEncounter.EncounterKey) &&
                     CurrentEncounter.IsEncounterOasisActive))
                .OrderByDescending(eo => eo.EncounterOasisM0090).FirstOrDefault();
            if (mostRecentOasisEncounterSOCROC != null)
            {
                CurrentEncounterOasisStart = mostRecentOasisEncounterSOCROC.EncounterOasis
                    .Where(e => ((e.InactiveDate == null) && (e.REC_ID != "X1"))).OrderByDescending(e => e.AddedDate)
                    .FirstOrDefault();
            }

            if (CurrentEncounterOasisStart == null)
            {
                return;
            }

            // Most Recent survey
            Encounter mostRecentOasisEncounter = CurrentAdmission.Encounter
                .Where(eo =>
                    ((eo.EncounterOasisRFA != null) && (eo.EncounterOasisM0090 != null) &&
                     (eo.EncounterKey != CurrentEncounter.EncounterKey) && CurrentEncounter.IsEncounterOasisActive))
                .OrderByDescending(eo => eo.EncounterOasisM0090).FirstOrDefault();
            ST_Episode = (mostRecentOasisEncounter == null)
                ? false
                : (((mostRecentOasisEncounter.EncounterOasisRFA == "04") ||
                    (mostRecentOasisEncounter.EncounterOasisRFA == "05"))
                    ? true
                    : false);

            AlertsActive = false;
        }

        public override void Cleanup()
        {
            VirtuosoObjectCleanupHelper.CleanupAll(this);
            base.Cleanup();
        }

        public void OasisAlertCheckBypass()
        {
            if (CurrentEncounter.EncounterOasisAlert == null)
            {
                return;
            }

            if (CurrentEncounterOasis == null)
            {
                return;
            }

            if (CurrentEncounterOasis.BypassFlag != true)
            {
                return;
            }

            List<EncounterOasisAlert> eoaList = (from i in CurrentEncounter.EncounterOasisAlert select i).ToList();
            if (eoaList != null)
            {
                foreach (EncounterOasisAlert eoa in eoaList)
                {
                    CurrentEncounter.EncounterOasisAlert.Remove(eoa);
                    if (FormModel != null)
                    {
                        ((IPatientService)FormModel).Remove(eoa);
                    }
                }
            }

            FormModel = null;
        }

        public void OasisAlertCheckAllMeasures()
        {
            if (AlertsActive == false)
            {
                return;
            }

            ProcessOasisAlerts(OasisCache.GetOasisAlertsByOasisVersionKey(CurrentOasisVersionKey));
        }

        public void OasisAlertCheckMeasuresForQuestion(int oasisQuestionKey)
        {
            if (AlertsActive == false)
            {
                return;
            }

            ProcessOasisAlerts(
                OasisCache.GetOasisAlertsByOasisVersionKeyAndOasisQuestionKey(CurrentOasisVersionKey,
                    oasisQuestionKey));
        }

        private void ProcessOasisAlerts(List<OasisAlert> oaList)
        {
            if (oaList == null)
            {
                return;
            }

            foreach (OasisAlert oa in oaList) ProcessOasisAlert(oa);
            Messenger.Default.Send(
                ((CurrentEncounter.EncounterOasisAlert == null) ? 0 : CurrentEncounter.EncounterOasisAlert.Count),
                string.Format("OasisAlertsChanged{0}", CurrentOasisManagerGUID.ToString().Trim()));
        }

        private void ProcessOasisAlert(OasisAlert oa)
        {
            if (oa == null)
            {
                return;
            }

            var properties = GetType().GetProperties();
            var prop = properties.Where(p => p.Name.Equals(oa.Measure, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            if (prop != null)
            {
                bool
                    measureExists =
                        false; // on exception - assume some piece of data used to calculate the measure is missing and the measure does not exist
                try
                {
                    measureExists = (bool)GetType().GetProperty(prop.Name).GetValue(this, null);
                }
                catch
                {
                }

                EncounterOasisAlert eoa = CurrentEncounter.EncounterOasisAlert
                    .Where(e => (e.OasisAlertKey == oa.OasisAlertKey)).FirstOrDefault();
                if (measureExists) // if measure exists - add the alert - otherwise remove it
                {
                    if (eoa == null)
                    {
                        EncounterOasisAlert eoaNew = new EncounterOasisAlert
                        {
                            EncounterKey = CurrentEncounter.EncounterKey,
                            EncounterStartKey = CurrentEncounterOasisStart.EncounterKey, ST_Episode = ST_Episode,
                            OasisAlertKey = oa.OasisAlertKey
                        };
                        CurrentEncounter.EncounterOasisAlert.Add(eoaNew);
                    }
                }
                else
                {
                    if (eoa != null)
                    {
                        CurrentEncounter.EncounterOasisAlert.Remove(eoa);
                        if (FormModel != null)
                        {
                            ((IPatientService)FormModel).Remove(eoa);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show(String.Format(
                    "Error OasisAlertMeasure.ProcessOasisAlert: Measure {0}, OasisAlertKey {1} is not defined.  Contact your system administrator.",
                    oa.Measure, oa.OasisAlertKey.ToString()));
            }
        }

        private int VALUE1(string cmsField)
        {
            try
            {
                return GetValueB1Record(cmsField, CurrentEncounterOasisStart.B1Record);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private int VALUE2(string cmsField)
        {
            try
            {
                return GetValueB1Record(cmsField, CurrentEncounterOasis.B1Record);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private DateTime DATEVALUE1(string cmsField)
        {
            try
            {
                return GetDateValueB1Record(cmsField, CurrentEncounterOasisStart.B1Record);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private DateTime DATEVALUE2(string cmsField)
        {
            try
            {
                return GetDateValueB1Record(cmsField, CurrentEncounterOasis.B1Record);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private bool MISSINGVALUE1(string cmsField)
        {
            try
            {
                OasisLayout ol = GetOasisLayout(cmsField);
                string text = (CurrentEncounterOasisStart.B1Record.Substring(ol.StartPos - 1, ol.Length));
                return (string.IsNullOrWhiteSpace(text)) ? true : false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return true;
            }
        }

        private bool MISSINGVALUE2(string cmsField)
        {
            try
            {
                OasisLayout ol = GetOasisLayout(cmsField);
                string text = (CurrentEncounterOasis.B1Record.Substring(ol.StartPos - 1, ol.Length));
                return (string.IsNullOrWhiteSpace(text)) ? true : false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return true;
            }
        }

        private string TEXTVALUE1(string cmsField)
        {
            try
            {
                return GetTextValueB1Record(cmsField, CurrentEncounterOasisStart.B1Record);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private string TEXTVALUE2(string cmsField)
        {
            try
            {
                return GetTextValueB1Record(cmsField, CurrentEncounterOasis.B1Record);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private int GetValueB1Record(string cmsField, string b1Record)
        {
            try
            {
                string text = GetTextValueB1Record(cmsField, b1Record);
                int i = 0;
                try
                {
                    i = Int32.Parse(text.Trim());
                }
                catch
                {
                    throw new Exception("OasisAlertManager.GetValueB1Record CMSField " + cmsField + " is MISSING");
                }

                return i;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private DateTime GetDateValueB1Record(string cmsField, string b1Record)
        {
            try
            {
                string stringDate = GetTextValueB1Record(cmsField, b1Record);
                if (stringDate.Equals("--------"))
                {
                    throw new Exception("OasisAlertManager.GetValueB1Record CMSField " + cmsField + " is MISSING");
                }

                stringDate = stringDate.Substring(4, 2) + "/" + stringDate.Substring(6, 2) + "/" +
                             stringDate.Substring(0, 4);
                DateTime date = DateTime.MinValue;
                try
                {
                    date = (DateTime.TryParse(stringDate, out date)) ? date : DateTime.MinValue;
                }
                catch
                {
                    throw new Exception("OasisAlertManager.GetValueB1Record CMSField " + cmsField + " is MISSING");
                }

                if (date == DateTime.MinValue)
                {
                    throw new Exception("OasisAlertManager.GetValueB1Record CMSField " + cmsField + " is MISSING");
                }

                return date;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private string GetTextValueB1Record(string cmsField, string b1Record)
        {
            try
            {
                OasisLayout ol = GetOasisLayout(cmsField);
                string text = (b1Record.Substring(ol.StartPos - 1, ol.Length));
                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new Exception("OasisAlertManager.GetValueB1Record CMSField " + cmsField + " is MISSING");
                }

                return text;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private OasisLayout GetOasisLayout(string cmsField)
        {
            OasisLayout ol = OasisCache.GetOasisLayoutByCMSField(CurrentOasisVersionKey, cmsField);
            if (ol == null)
            {
                MessageBox.Show(String.Format(
                    "Error OasisAlertMeasure.GetOasisLayout: CMSField {0}, is not defined.  Contact your system administrator.",
                    cmsField));
                throw new Exception("OasisAlertManager.GetOasisLayout CMSField " + cmsField + " does not exist");
            }

            return ol;
        }

        public bool ED_Use_Fall
        {
            get
            {
                try
                {
                    if (VALUE2("M2310_ECR_INJRY_BY_FALL") == 1)
                    {
                        return true;
                    }

                    if (VALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == 00 ||
                        ((VALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == 01 ||
                          VALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == 02) && VALUE2("M2310_ECR_UNKNOWN") == 0))
                    {
                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool ED_Use_Fall_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M2310_ECR_INJRY_BY_FALL") == 1)
                    {
                        return true;
                    }

                    if (VALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == 00 ||
                        ((VALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == 01 ||
                          VALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == 02) && VALUE2("M2310_ECR_UNKNOWN") == 0))
                    {
                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool ED_Use_Wound_Status
        {
            get
            {
                try
                {
                    if (VALUE2("M2310_ECR_WND_INFCTN_DTRORTN") == 1)
                    {
                        return true;
                    }

                    if (VALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == 00 ||
                        ((VALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == 01 ||
                          VALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == 02) && VALUE2("M2310_ECR_UNKNOWN") == 0))
                    {
                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool ED_Use_Wound_Status_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M2310_ECR_WND_INFCTN_DTRORTN") == 1)
                    {
                        return true;
                    }

                    if (VALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == 00 ||
                        ((VALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == 01 ||
                          VALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == 02) && VALUE2("M2310_ECR_UNKNOWN") == 0))
                    {
                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool ED_Use_Medications
        {
            get
            {
                try
                {
                    if (VALUE2("M2310_ECR_MEDICATION") == 1)
                    {
                        return true;
                    }

                    if (VALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == 00 ||
                        ((VALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == 01 ||
                          VALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == 02) && VALUE2("M2310_ECR_UNKNOWN") == 0))
                    {
                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool ED_Use_Medications_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M2310_ECR_MEDICATION") == 1)
                    {
                        return true;
                    }

                    if (VALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == 00 ||
                        ((VALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == 01 ||
                          VALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == 02) && VALUE2("M2310_ECR_UNKNOWN") == 0))
                    {
                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool ED_Use_Blood_Sugar
        {
            get
            {
                try
                {
                    if (VALUE2("M2310_ECR_HYPOGLYC") == 1)
                    {
                        return true;
                    }

                    if (VALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == 00 ||
                        ((VALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == 01 ||
                          VALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == 02) && VALUE2("M2310_ECR_UNKNOWN") == 0))
                    {
                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool ED_Use_Blood_Sugar_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M2310_ECR_HYPOGLYC") == 1)
                    {
                        return true;
                    }

                    if (VALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == 00 ||
                        ((VALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == 01 ||
                          VALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == 02) && VALUE2("M2310_ECR_UNKNOWN") == 0))
                    {
                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool New_UTI
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 9)
                    {
                        if (VALUE1("M1600_UTI") == 00 && VALUE2("M1600_UTI") == 01)
                        {
                            return true;
                        }

                        if (VALUE1("M1600_UTI") == 00 && VALUE2("M1600_UTI") == 00)
                        {
                            return false;
                        }

                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool More_Pressure_Ulcers
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE2("M1306_UNHLD_STG2_PRSR_ULCR") == 1)
                        {
                            if (VALUE1("M1306_UNHLD_STG2_PRSR_ULCR") == 0)
                            {
                                return true;
                            }

                            if ((VALUE1("M1308_NBR_PRSULC_STG2") + VALUE1("M1308_NBR_PRSULC_STG3") +
                                 VALUE1("M1308_NBR_PRSULC_STG4") + VALUE1("M1308_NSTG_DRSG") +
                                 VALUE1("M1308_NSTG_CVRG") + VALUE1("M1308_NSTG_DEEP_TISUE")) <
                                (VALUE2("M1308_NBR_PRSULC_STG2") + VALUE2("M1308_NBR_PRSULC_STG3") +
                                 VALUE2("M1308_NBR_PRSULC_STG4") + VALUE2("M1308_NSTG_DRSG") +
                                 VALUE2("M1308_NSTG_CVRG") + VALUE2("M1308_NSTG_DEEP_TISUE")))
                            {
                                return true;
                            }

                            return false;
                        }

                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool More_Pressure_Ulcers_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE2("M1306_UNHLD_STG2_PRSR_ULCR") == 1)
                        {
                            if (VALUE1("M1306_UNHLD_STG2_PRSR_ULCR") == 0)
                            {
                                return true;
                            }

                            if ((VALUE1("M1311_NBR_PRSULC_STG2_A1") + VALUE1("M1311_NBR_PRSULC_STG3_B1") +
                                 VALUE1("M1311_NBR_PRSULC_STG4_C1") + VALUE1("M1311_NSTG_DRSG_D1") +
                                 VALUE1("M1311_NSTG_CVRG_E1") + VALUE1("M1311_NSTG_DEEP_TSUE_F1")) <
                                (VALUE2("M1311_NBR_PRSULC_STG2_A1") + VALUE2("M1311_NBR_PRSULC_STG3_B1") +
                                 VALUE2("M1311_NBR_PRSULC_STG4_C1") + VALUE2("M1311_NSTG_DRSG_D1") +
                                 VALUE2("M1311_NSTG_CVRG_E1") + VALUE2("M1311_NSTG_DEEP_TSUE_F1")))
                            {
                                return true;
                            }

                            return false;
                        }

                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool ADL_Decline
        {
            get
            {
                try
                {
                    int ADL_Temp1 = 0;
                    if (VALUE1("M1800_CRNT_GROOMING") == 03 || VALUE1("M1800_CRNT_GROOMING") == 02)
                    {
                        ADL_Temp1++;
                    }

                    if (VALUE1("M1830_CRNT_BATHG") == 06 || VALUE1("M1830_CRNT_BATHG") == 05)
                    {
                        ADL_Temp1++;
                    }

                    if (VALUE1("M1840_CRNT_TOILTG") == 04 || VALUE1("M1840_CRNT_TOILTG") == 03)
                    {
                        ADL_Temp1++;
                    }

                    if (VALUE1("M1845_CRNT_TOILTG_HYGN") == 03 || VALUE1("M1845_CRNT_TOILTG_HYGN") == 02)
                    {
                        ADL_Temp1++;
                    }

                    if (VALUE1("M1850_CRNT_TRNSFRNG") == 05 || VALUE1("M1850_CRNT_TRNSFRNG") == 04)
                    {
                        ADL_Temp1++;
                    }

                    if (VALUE1("M1860_CRNT_AMBLTN") == 06 || VALUE1("M1860_CRNT_AMBLTN") == 05)
                    {
                        ADL_Temp1++;
                    }

                    if (VALUE1("M1870_CRNT_FEEDING") == 05 || VALUE1("M1870_CRNT_FEEDING") == 04)
                    {
                        ADL_Temp1++;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09 && ADL_Temp1 < 5)
                    {
                        int ADL_Temp2 = 0;
                        if (VALUE2("M1800_CRNT_GROOMING") > VALUE1("M1800_CRNT_GROOMING") + 1)
                        {
                            ADL_Temp2++;
                        }

                        if (VALUE2("M1830_CRNT_BATHG") > VALUE1("M1830_CRNT_BATHG") + 1)
                        {
                            ADL_Temp2++;
                        }

                        if (VALUE2("M1840_CRNT_TOILTG") > VALUE1("M1840_CRNT_TOILTG") + 1)
                        {
                            ADL_Temp2++;
                        }

                        if (VALUE2("M1845_CRNT_TOILTG_HYGN") > VALUE1("M1845_CRNT_TOILTG_HYGN") + 1)
                        {
                            ADL_Temp2++;
                        }

                        if (VALUE2("M1850_CRNT_TRNSFRNG") > VALUE1("M1850_CRNT_TRNSFRNG") + 1)
                        {
                            ADL_Temp2++;
                        }

                        if (VALUE2("M1860_CRNT_AMBLTN") > VALUE1("M1860_CRNT_AMBLTN") + 1)
                        {
                            ADL_Temp2++;
                        }

                        if (VALUE2("M1870_CRNT_FEEDING") > VALUE1("M1870_CRNT_FEEDING") + 1)
                        {
                            ADL_Temp2++;
                        }

                        if (ADL_Temp2 >= 3)
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Oral_Med_Decline
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") != 09 || TEXTVALUE2("M2020_CRNT_MGMT_ORAL_MDCTN") == "NA" ||
                        TEXTVALUE1("M2000_DRUG_RGMN_RVW") == "NA" || VALUE1("M2020_CRNT_MGMT_ORAL_MDCTN") != 00)
                    {
                        return false;
                    }

                    if (VALUE2("M2020_CRNT_MGMT_ORAL_MDCTN") == 03 && VALUE1("M2020_CRNT_MGMT_ORAL_MDCTN") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Oral_Med_Decline_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") != 09 || TEXTVALUE2("M2020_CRNT_MGMT_ORAL_MDCTN") == "NA" ||
                        TEXTVALUE1("M2001_DRUG_RGMN_RVW") == "9" || TEXTVALUE1("M2001_DRUG_RGMN_RVW") == "-" ||
                        VALUE1("M2020_CRNT_MGMT_ORAL_MDCTN") != 00)
                    {
                        return false;
                    }

                    if (VALUE2("M2020_CRNT_MGMT_ORAL_MDCTN") == 03 && VALUE1("M2020_CRNT_MGMT_ORAL_MDCTN") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool DC_w_Stg_II_Press_Ulc
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE2("M2420_DSCHRG_DISP") == 01 && (VALUE2("M1306_UNHLD_STG2_PRSR_ULCR") == 1
                                                                  && ((VALUE2("M1307_OLDST_STG2_AT_DSCHRG") == 02 &&
                                                                       DATEVALUE2("M0906_DC_TRAN_DTH_DT") >
                                                                       DATEVALUE2("M1307_OLDST_STG2_ONST_DT")
                                                                           .AddDays(30)) ||
                                                                      (VALUE2("M1307_OLDST_STG2_AT_DSCHRG") == 01
                                                                       && VALUE1("M0100_ASSMT_REASON") == 01 &&
                                                                       DATEVALUE2("M0906_DC_TRAN_DTH_DT") >
                                                                       DATEVALUE1("M0030_START_CARE_DT").AddDays(30)) ||
                                                                      (VALUE2("M1307_OLDST_STG2_AT_DSCHRG") == 01
                                                                       && VALUE1("M0100_ASSMT_REASON") == 03 &&
                                                                       DATEVALUE2("M0906_DC_TRAN_DTH_DT") >
                                                                       DATEVALUE1("M0032_ROC_DT").AddDays(30)))))
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool DC_Need_Wound_Care
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE2("M2420_DSCHRG_DISP") == 01 && (VALUE2("M2100_CARE_TYPE_SRC_MDCTN") > 2 ||
                                                                  VALUE2("M2100_CARE_TYPE_SRC_PRCDR") > 2)
                                                              && (TEXTVALUE2("M1710_WHEN_CONFUSED") == "NA" ||
                                                                  VALUE2("M1710_WHEN_CONFUSED") > 2)
                                                              && (VALUE2("M1324_STG_PRBLM_ULCER") == 03 ||
                                                                  VALUE2("M1324_STG_PRBLM_ULCER") == 04 ||
                                                                  VALUE2("M1334_STUS_PRBLM_STAS_ULCR") == 03 ||
                                                                  VALUE2("M1342_STUS_PRBLM_SRGCL_WND") == 03 ||
                                                                  VALUE2("M2020_CRNT_MGMT_ORAL_MDCTN") == 03))
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool DC_Need_Wound_Care_C1
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE2("M2420_DSCHRG_DISP") == 01 && (VALUE2("M2102_CARE_TYPE_SRC_MDCTN") > 2 ||
                                                                  VALUE2("M2102_CARE_TYPE_SRC_PRCDR") > 2)
                                                              && (TEXTVALUE2("M1710_WHEN_CONFUSED") == "NA" ||
                                                                  VALUE2("M1710_WHEN_CONFUSED") > 2)
                                                              && (VALUE2("M1324_STG_PRBLM_ULCER") == 03 ||
                                                                  VALUE2("M1324_STG_PRBLM_ULCER") == 04 ||
                                                                  VALUE2("M1334_STUS_PRBLM_STAS_ULCR") == 03 ||
                                                                  VALUE2("M1342_STUS_PRBLM_SRGCL_WND") == 03 ||
                                                                  VALUE2("M2020_CRNT_MGMT_ORAL_MDCTN") == 03))
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool DC_Need_Toilet_Assist
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE2("M2420_DSCHRG_DISP") == 01 && (VALUE2("M2100_CARE_TYPE_SRC_ADL") > 2 &&
                                                                  (VALUE2("M1840_CRNT_TOILTG") == 4 ||
                                                                   VALUE2("M1845_CRNT_TOILTG_HYGN") == 3)))
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool DC_Need_Toilet_Assist_C1
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE2("M2420_DSCHRG_DISP") == 01 && (VALUE2("M2102_CARE_TYPE_SRC_ADL") > 2 &&
                                                                  (VALUE2("M1840_CRNT_TOILTG") == 4 ||
                                                                   VALUE2("M1845_CRNT_TOILTG_HYGN") == 3)))
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool DC_Behavior_Problem
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE2("M2420_DSCHRG_DISP") == 01 && VALUE2("M2100_CARE_TYPE_SRC_SPRVSN") > 2 &&
                            (VALUE2("M1740_BD_MEM_DEFICIT") + VALUE2("M1740_BD_IMP_DECISN") +
                             VALUE2("M1740_BD_VERBAL") + VALUE2("M1740_BD_PHYSICAL") + VALUE2("M1740_BD_SOC_INAPPRO") +
                             VALUE2("M1740_BD_DELUSIONS")) > 1)
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool DC_Behavior_Problem_C1
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE2("M2420_DSCHRG_DISP") == 01 && VALUE2("M2102_CARE_TYPE_SRC_SPRVSN") > 2 &&
                            (VALUE2("M1740_BD_MEM_DEFICIT") + VALUE2("M1740_BD_IMP_DECISN") +
                             VALUE2("M1740_BD_VERBAL") + VALUE2("M1740_BD_PHYSICAL") + VALUE2("M1740_BD_SOC_INAPPRO") +
                             VALUE2("M1740_BD_DELUSIONS")) > 1)
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Timely_Care
        {
            get
            {
                try
                {
                    if (VALUE1("M0100_ASSMT_REASON") == 01)
                    {
                        if (VALUE1("M0102_PHYSN_ORDRD_SOCROC_DT_NA") != 1)
                        {
                            if (DATEVALUE1("M0030_START_CARE_DT") <= DATEVALUE1("M0102_PHYSN_ORDRD_SOCROC_DT"))
                            {
                                return false;
                            }

                            return true;
                        }

                        if (DATEVALUE1("M0030_START_CARE_DT") <= DATEVALUE1("M0104_PHYSN_RFRL_DT").AddDays(2))
                        {
                            return false;
                        }

                        if (VALUE1("M1000_DC_NONE_14_DA") == 1)
                        {
                            return true;
                        }

                        if (DATEVALUE1("M1005_INP_DISCHARGE_DT") > DATEVALUE1("M0104_PHYSN_RFRL_DT"))
                        {
                            if (DATEVALUE1("M0030_START_CARE_DT") <= DATEVALUE1("M1005_INP_DISCHARGE_DT").AddDays(2))
                            {
                                return false;
                            }

                            return true;
                        }

                        return true;
                    }

                    if (VALUE1("M0102_PHYSN_ORDRD_SOCROC_DT_NA") != 1)
                    {
                        if (DATEVALUE1("M0032_ROC_DT") <= DATEVALUE1("M0102_PHYSN_ORDRD_SOCROC_DT"))
                        {
                            return false;
                        }

                        return true;
                    }

                    if (DATEVALUE1("M0032_ROC_DT") <= DATEVALUE1("M0104_PHYSN_RFRL_DT").AddDays(2))
                    {
                        return false;
                    }

                    if (VALUE1("M1000_DC_NONE_14_DA") == 1)
                    {
                        return true;
                    }

                    if (DATEVALUE1("M1005_INP_DISCHARGE_DT") > DATEVALUE1("M0104_PHYSN_RFRL_DT"))
                    {
                        if (DATEVALUE1("M0032_ROC_DT") <= DATEVALUE1("M1005_INP_DISCHARGE_DT").AddDays(2))
                        {
                            return false;
                        }

                        return true;
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool MD_Notification
        {
            get
            {
                try
                {
                    if (TEXTVALUE1("M2250_PLAN_SMRY_PTNT_SPECF") == "NA")
                    {
                        return false;
                    }

                    if (VALUE1("M2250_PLAN_SMRY_PTNT_SPECF") == 01)
                    {
                        return false;
                    }

                    if (VALUE1("M2250_PLAN_SMRY_PTNT_SPECF") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Depression_Asmt
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE1("M1730_STDZ_DPRSN_SCRNG") == 01 || VALUE1("M1730_STDZ_DPRSN_SCRNG") == 02 ||
                        VALUE1("M1730_STDZ_DPRSN_SCRNG") == 03)
                    {
                        return false;
                    }

                    if (VALUE1("M1730_STDZ_DPRSN_SCRNG") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Fall_Risk_Asmt
        {
            get
            {
                try
                {
                    if (((VALUE1("M0100_ASSMT_REASON") == 01)
                         && ((DATEVALUE1("M0032_ROC_DT").Year - DATEVALUE1("M0066_PAT_BIRTH_DT").Year >= 65)
                             || ((DATEVALUE1("M0032_ROC_DT").Year - DATEVALUE1("M0066_PAT_BIRTH_DT").Year == 65)
                                 && ((DATEVALUE1("M0032_ROC_DT").Month > DATEVALUE1("M0066_PAT_BIRTH_DT").Month)
                                     || ((DATEVALUE1("M0032_ROC_DT").Month == DATEVALUE1("M0066_PAT_BIRTH_DT").Month)
                                         && (DATEVALUE1("M0032_ROC_DT").Day >=
                                             DATEVALUE1("M0066_PAT_BIRTH_DT").Day))))))
                        || ((VALUE1("M0100_ASSMT_REASON") == 03)
                            && ((DATEVALUE1("M0032_ROC_DT").Year - DATEVALUE1("M0066_PAT_BIRTH_DT").Year >= 65)
                                || ((DATEVALUE1("M0032_ROC_DT").Year - DATEVALUE1("M0066_PAT_BIRTH_DT").Year == 65)
                                    && ((DATEVALUE1("M0032_ROC_DT").Month > DATEVALUE1("M0066_PAT_BIRTH_DT").Month)
                                        || ((DATEVALUE1("M0032_ROC_DT").Month == DATEVALUE1("M0066_PAT_BIRTH_DT").Month)
                                            && (DATEVALUE1("M0032_ROC_DT").Day >=
                                                DATEVALUE1("M0066_PAT_BIRTH_DT").Day)))))))
                    {
                        if (VALUE1("M1910_MLT_FCTR_FALL_RISK_ASMT") == 01 ||
                            VALUE1("M1910_MLT_FCTR_FALL_RISK_ASMT") == 02)
                        {
                            return false;
                        }

                        if (VALUE1("M1910_MLT_FCTR_FALL_RISK_ASMT") == 00)
                        {
                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Pain_Asmt
        {
            get
            {
                try
                {
                    if (VALUE1("M1240_FRML_PAIN_ASMT") == 01 || VALUE1("M1240_FRML_PAIN_ASMT") == 02)
                    {
                        return false;
                    }

                    if (VALUE1("M1240_FRML_PAIN_ASMT") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool P_U_Risk_Asmt
        {
            get
            {
                try
                {
                    if (VALUE1("M1300_PRSR_ULCR_RISK_ASMT") == 01 || VALUE1("M1300_PRSR_ULCR_RISK_ASMT") == 02)
                    {
                        return false;
                    }

                    if (VALUE1("M1300_PRSR_ULCR_RISK_ASMT") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Depression_POC
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (TEXTVALUE1("M2250_PLAN_SMRY_DPRSN_INTRVTN") == "NA")
                    {
                        return false;
                    }

                    if (VALUE1("M2250_PLAN_SMRY_DPRSN_INTRVTN") == 01)
                    {
                        return false;
                    }

                    if (VALUE1("M2250_PLAN_SMRY_DPRSN_INTRVTN") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Diabetic_Ft_Care_POC
        {
            get
            {
                try
                {
                    if (TEXTVALUE1("M2250_PLAN_SMRY_DBTS_FT_CARE") == "NA")
                    {
                        return false;
                    }

                    if (VALUE1("M2250_PLAN_SMRY_DBTS_FT_CARE") == 01)
                    {
                        return false;
                    }

                    if (VALUE1("M2250_PLAN_SMRY_DBTS_FT_CARE") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Falls_Prvnt_POC
        {
            get
            {
                try
                {
                    if (TEXTVALUE1("M2250_PLAN_SMRY_FALL_PRVNT") == "NA")
                    {
                        return false;
                    }

                    if (VALUE1("M2250_PLAN_SMRY_FALL_PRVNT") == 01)
                    {
                        return false;
                    }

                    if (VALUE1("M2250_PLAN_SMRY_FALL_PRVNT") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Pain_POC
        {
            get
            {
                try
                {
                    if (TEXTVALUE1("M2250_PLAN_SMRY_PAIN_INTRVTN") == "NA")
                    {
                        return false;
                    }

                    if (VALUE1("M2250_PLAN_SMRY_PAIN_INTRVTN") == 01)
                    {
                        return false;
                    }

                    if (VALUE1("M2250_PLAN_SMRY_PAIN_INTRVTN") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool P_U_Prvnt_POC
        {
            get
            {
                try
                {
                    if (TEXTVALUE1("M2250_PLAN_SMRY_PRSULC_PRVNT") == "NA")
                    {
                        return false;
                    }

                    if (VALUE1("M2250_PLAN_SMRY_PRSULC_PRVNT") == 01)
                    {
                        return false;
                    }

                    if (VALUE1("M2250_PLAN_SMRY_PRSULC_PRVNT") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool P_U_Healing_POC
        {
            get
            {
                try
                {
                    if (TEXTVALUE1("M2250_PLAN_SMRY_PRSULC_TRTMT") == "NA")
                    {
                        return false;
                    }

                    if (VALUE1("M2250_PLAN_SMRY_PRSULC_TRTMT") == 01)
                    {
                        return false;
                    }

                    if (VALUE1("M2250_PLAN_SMRY_PRSULC_TRTMT") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Depression_Implmnt_S_T
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (ST_Episode)
                    {
                        return Depression_Implmnt_All;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Depression_Implmnt_S_T_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (ST_Episode)
                    {
                        return Depression_Implmnt_All_C2;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Depression_Implmnt_L_T
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (ST_Episode == false)
                    {
                        return Depression_Implmnt_All;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Depression_Implmnt_L_T_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (ST_Episode == false)
                    {
                        return Depression_Implmnt_All_C2;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Depression_Implmnt_All
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (TEXTVALUE2("M2400_INTRVTN_SMRY_DPRSN") == "NA")
                    {
                        return false;
                    }

                    if (VALUE2("M2400_INTRVTN_SMRY_DPRSN") == 01)
                    {
                        return false;
                    }

                    if (VALUE2("M2400_INTRVTN_SMRY_DPRSN") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Depression_Implmnt_All_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (TEXTVALUE2("M2401_INTRVTN_SMRY_DPRSN") == "NA")
                    {
                        return false;
                    }

                    if (VALUE2("M2401_INTRVTN_SMRY_DPRSN") == 01)
                    {
                        return false;
                    }

                    if (VALUE2("M2401_INTRVTN_SMRY_DPRSN") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool NonresponsiveAtSOCROC
        {
            get
            {
                try
                {
                    return (TEXTVALUE1("M1710_WHEN_CONFUSED") == "NA" || TEXTVALUE1("M1720_WHEN_ANXIOUS") == "NA")
                        ? true
                        : false;
                }
                catch
                {
                    return false;
                }
            }
        }


        public bool Diabetic_Ft_Care_Implmnt_S_T
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode)
                    {
                        return Diabetic_Ft_Care_Implmnt_All;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Diabetic_Ft_Care_Implmnt_S_T_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode)
                    {
                        return Diabetic_Ft_Care_Implmnt_All_C2;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Diabetic_Ft_Care_Implmnt_L_T
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode == false)
                    {
                        return Diabetic_Ft_Care_Implmnt_All;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Diabetic_Ft_Care_Implmnt_L_T_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode == false)
                    {
                        return Diabetic_Ft_Care_Implmnt_All_C2;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Diabetic_Ft_Care_Implmnt_All
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (TEXTVALUE2("M2400_INTRVTN_SMRY_DBTS_FT") == "NA")
                    {
                        return false;
                    }

                    if (VALUE2("M2400_INTRVTN_SMRY_DBTS_FT") == 01)
                    {
                        return false;
                    }

                    if (VALUE2("M2400_INTRVTN_SMRY_DBTS_FT") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Diabetic_Ft_Care_Implmnt_All_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (TEXTVALUE2("M2401_INTRVTN_SMRY_DBTS_FT") == "NA")
                    {
                        return false;
                    }

                    if (VALUE2("M2401_INTRVTN_SMRY_DBTS_FT") == 01)
                    {
                        return false;
                    }

                    if (VALUE2("M2401_INTRVTN_SMRY_DBTS_FT") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Heart_Failure_Implmnt_S_T
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode)
                    {
                        return Heart_Failure_Implmnt_All;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Heart_Failure_Implmnt_S_T_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode)
                    {
                        return Heart_Failure_Implmnt_All_C2;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Heart_Failure_Implmnt_L_T
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode == false)
                    {
                        return Heart_Failure_Implmnt_All;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Heart_Failure_Implmnt_L_T_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode == false)
                    {
                        return Heart_Failure_Implmnt_All_C2;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Heart_Failure_Implmnt_All
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (VALUE2("M1500_SYMTM_HRT_FAILR_PTNTS") == 01)
                    {
                        if (VALUE2("M1510_HRT_FAILR_NO_ACTN") == 1)
                        {
                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Heart_Failure_Implmnt_All_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (VALUE2("M1501_SYMTM_HRT_FAILR_PTNTS") == 01)
                    {
                        if (VALUE2("M1511_HRT_FAILR_NO_ACTN") == 1)
                        {
                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Pain_Implmnt_S_T
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode)
                    {
                        return Pain_Implmnt_All;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Pain_Implmnt_S_T_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode)
                    {
                        return Pain_Implmnt_All_C2;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Pain_Implmnt_L_T
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode == false)
                    {
                        return Pain_Implmnt_All;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Pain_Implmnt_L_T_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode == false)
                    {
                        return Pain_Implmnt_All_C2;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Pain_Implmnt_All
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (TEXTVALUE2("M2400_INTRVTN_SMRY_PAIN_MNTR") == "NA")
                    {
                        return false;
                    }

                    if (VALUE2("M2400_INTRVTN_SMRY_PAIN_MNTR") == 01)
                    {
                        return false;
                    }

                    if (VALUE2("M2400_INTRVTN_SMRY_PAIN_MNTR") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Pain_Implmnt_All_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (TEXTVALUE2("M2401_INTRVTN_SMRY_PAIN_MNTR") == "NA")
                    {
                        return false;
                    }

                    if (VALUE2("M2401_INTRVTN_SMRY_PAIN_MNTR") == 01)
                    {
                        return false;
                    }

                    if (VALUE2("M2401_INTRVTN_SMRY_PAIN_MNTR") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool P_U_Implmnt_S_T
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode)
                    {
                        return P_U_Implmnt_All;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool P_U_Implmnt_S_T_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode)
                    {
                        return P_U_Implmnt_All_C2;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool P_U_Implmnt_L_T
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode == false)
                    {
                        return P_U_Implmnt_All;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool P_U_Implmnt_L_T_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode == false)
                    {
                        return P_U_Implmnt_All_C2;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool P_U_Implmnt_All
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (TEXTVALUE2("M2400_INTRVTN_SMRY_PRSULC_WET") == "NA")
                    {
                        return false;
                    }

                    if (VALUE2("M2400_INTRVTN_SMRY_PRSULC_WET") == 01)
                    {
                        return false;
                    }

                    if (VALUE2("M2400_INTRVTN_SMRY_PRSULC_WET") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool P_U_Implmnt_All_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (TEXTVALUE2("M2401_INTRVTN_SMRY_PRSULC_WET") == "NA")
                    {
                        return false;
                    }

                    if (VALUE2("M2401_INTRVTN_SMRY_PRSULC_WET") == 01)
                    {
                        return false;
                    }

                    if (VALUE2("M2401_INTRVTN_SMRY_PRSULC_WET") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Drug_Ed_Hi_Risk_SOC
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (TEXTVALUE1("M2000_DRUG_RGMN_RVW") == "NA" || TEXTVALUE1("M2010_HIGH_RISK_DRUG_EDCTN") == "NA")
                    {
                        return false;
                    }

                    if (VALUE1("M2010_HIGH_RISK_DRUG_EDCTN") == 01)
                    {
                        return false;
                    }

                    if (VALUE1("M2010_HIGH_RISK_DRUG_EDCTN") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Drug_Ed_Hi_Risk_SOC_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (TEXTVALUE1("M2001_DRUG_RGMN_RVW") == "9" || TEXTVALUE1("M2001_DRUG_RGMN_RVW") == "-" ||
                        TEXTVALUE1("M2010_HIGH_RISK_DRUG_EDCTN") == "NA")
                    {
                        return false;
                    }

                    if (VALUE1("M2010_HIGH_RISK_DRUG_EDCTN") == 01)
                    {
                        return false;
                    }

                    if (VALUE1("M2010_HIGH_RISK_DRUG_EDCTN") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Drug_Ed_Implmnt_S_T
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode)
                    {
                        return Drug_Ed_Implmnt_All;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Drug_Ed_Implmnt_S_T_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode)
                    {
                        return Drug_Ed_Implmnt_All_C2;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Drug_Ed_Implmnt_L_T
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode == false)
                    {
                        return Drug_Ed_Implmnt_All;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Drug_Ed_Implmnt_L_T_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode == false)
                    {
                        return Drug_Ed_Implmnt_All_C2;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Drug_Ed_Implmnt_All
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (TEXTVALUE2("M2015_DRUG_EDCTN_INTRVTN") == "NA")
                    {
                        return false;
                    }

                    if (VALUE2("M2015_DRUG_EDCTN_INTRVTN") == 01)
                    {
                        return false;
                    }

                    if (VALUE2("M2015_DRUG_EDCTN_INTRVTN") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Drug_Ed_Implmnt_All_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (TEXTVALUE2("M2016_DRUG_EDCTN_INTRVTN") == "NA")
                    {
                        return false;
                    }

                    if (VALUE2("M2016_DRUG_EDCTN_INTRVTN") == 01)
                    {
                        return false;
                    }

                    if (VALUE2("M2016_DRUG_EDCTN_INTRVTN") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Fall_Prvnt_Implmnt_S_T
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode)
                    {
                        return Fall_Prvnt_Implmnt_All;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Fall_Prvnt_Implmnt_S_T_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode)
                    {
                        return Fall_Prvnt_Implmnt_All_C2;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Fall_Prvnt_Implmnt_L_T
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode == false)
                    {
                        return Fall_Prvnt_Implmnt_All;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Fall_Prvnt_Implmnt_L_T_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode == false)
                    {
                        return Fall_Prvnt_Implmnt_All_C2;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Fall_Prvnt_Implmnt_All
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (TEXTVALUE2("M2400_INTRVTN_SMRY_FALL_PRVNT") == "NA")
                    {
                        return false;
                    }

                    if (VALUE2("M2400_INTRVTN_SMRY_FALL_PRVNT") == 01)
                    {
                        return false;
                    }

                    if (VALUE2("M2400_INTRVTN_SMRY_FALL_PRVNT") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Fall_Prvnt_Implmnt_All_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (TEXTVALUE2("M2401_INTRVTN_SMRY_FALL_PRVNT") == "NA")
                    {
                        return false;
                    }

                    if (VALUE2("M2401_INTRVTN_SMRY_FALL_PRVNT") == 01)
                    {
                        return false;
                    }

                    if (VALUE2("M2401_INTRVTN_SMRY_FALL_PRVNT") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Influenza_Immunization
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (Date_Exclusions_Apply)
                    {
                        return false;
                    }

                    if (VALUE2("M1040_INFLNZ_RCVD_AGNCY") == 01)
                    {
                        return false;
                    }

                    if (VALUE2("M1040_INFLNZ_RCVD_AGNCY") == 00)
                    {
                        if (VALUE2("M1045_INFLNZ_RSN_NOT_RCVD") == 05)
                        {
                            return false;
                        }

                        if (VALUE2("M1045_INFLNZ_RSN_NOT_RCVD") == 01 || VALUE2("M1045_INFLNZ_RSN_NOT_RCVD") == 02)
                        {
                            return false;
                        }

                        return true;
                    }

                    if (TEXTVALUE2("M1040_INFLNZ_RCVD_AGNCY") == "NA")
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Influenza_Immunization_C1
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (Date_Exclusions_Apply)
                    {
                        return false;
                    }

                    if (VALUE2("M1041_IN_INFLNZ_SEASON") == 0)
                    {
                        return false;
                    }

                    if (VALUE2("M1041_IN_INFLNZ_SEASON") == 1)
                    {
                        if (VALUE2("M1046_INFLNZ_RECD_CRNT_SEASON") == 06)
                        {
                            return false;
                        }

                        if (VALUE2("M1046_INFLNZ_RECD_CRNT_SEASON") == 01 ||
                            VALUE2("M1046_INFLNZ_RECD_CRNT_SEASON") == 02 ||
                            VALUE2("M1046_INFLNZ_RECD_CRNT_SEASON") == 03)
                        {
                            return false;
                        }

                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Influenza_Refused
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (Date_Exclusions_Apply)
                    {
                        return false;
                    }

                    if (VALUE2("M1040_INFLNZ_RCVD_AGNCY") == 01 || TEXTVALUE2("M1040_INFLNZ_RCVD_AGNCY") == "NA")
                    {
                        return true;
                    }

                    if (VALUE2("M1040_INFLNZ_RCVD_AGNCY") == 00)
                    {
                        if (VALUE2("M1045_INFLNZ_RSN_NOT_RCVD") == 05)
                        {
                            return false;
                        }

                        if (VALUE2("M1045_INFLNZ_RSN_NOT_RCVD") == 03)
                        {
                            return false;
                        }

                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Influenza_Refused_C1
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (Date_Exclusions_Apply)
                    {
                        return false;
                    }

                    if (VALUE2("M1041_IN_INFLNZ_SEASON") == 0)
                    {
                        return true;
                    }

                    if (VALUE2("M1041_IN_INFLNZ_SEASON") == 1)
                    {
                        if (VALUE2("M1046_INFLNZ_RECD_CRNT_SEASON") == 06)
                        {
                            return false;
                        }

                        if (VALUE2("M1046_INFLNZ_RECD_CRNT_SEASON") == 04)
                        {
                            return false;
                        }

                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Influenza_Contraindicated
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (Date_Exclusions_Apply)
                    {
                        return false;
                    }

                    if (VALUE2("M1040_INFLNZ_RCVD_AGNCY") == 01 || TEXTVALUE2("M1040_INFLNZ_RCVD_AGNCY") == "NA")
                    {
                        return true;
                    }

                    if (VALUE2("M1040_INFLNZ_RCVD_AGNCY") == 00)
                    {
                        if (VALUE2("M1045_INFLNZ_RSN_NOT_RCVD") == 05)
                        {
                            return false;
                        }

                        if (VALUE2("M1045_INFLNZ_RSN_NOT_RCVD") == 04)
                        {
                            return false;
                        }

                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Influenza_Contraindicated_C1
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (Date_Exclusions_Apply)
                    {
                        return false;
                    }

                    if (VALUE2("M1041_IN_INFLNZ_SEASON") == 0)
                    {
                        return true;
                    }

                    if (VALUE2("M1041_IN_INFLNZ_SEASON") == 1)
                    {
                        if (VALUE2("M1046_INFLNZ_RECD_CRNT_SEASON") == 06)
                        {
                            return false;
                        }

                        if (VALUE2("M1046_INFLNZ_RECD_CRNT_SEASON") == 05)
                        {
                            return false;
                        }

                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Date_Exclusions_Apply
        {
            get
            {
                try
                {
                    if ((DATEVALUE2("M0906_DC_TRAN_DTH_DT").Month > 3 && DATEVALUE2("M0906_DC_TRAN_DTH_DT").Month < 10)
                        && ((VALUE1("M0100_ASSMT_REASON") == 01
                             && DATEVALUE1("M0030_START_CARE_DT").Month > 3 &&
                             DATEVALUE1("M0030_START_CARE_DT").Month < 10
                             && DATEVALUE2("M0906_DC_TRAN_DTH_DT").Year == DATEVALUE1("M0030_START_CARE_DT").Year)
                            || (VALUE1("M0100_ASSMT_REASON") == 03
                                && DATEVALUE1("M0032_ROC_DT").Month > 3 && DATEVALUE1("M0032_ROC_DT").Month < 10
                                && DATEVALUE2("M0906_DC_TRAN_DTH_DT").Year == DATEVALUE1("M0032_ROC_DT").Year)))
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return true;
                }
            }
        }

        public bool Pneumococcal_Vaccine
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (VALUE2("M1050_PPV_RCVD_AGNCY") == 01)
                    {
                        return false;
                    }

                    if (VALUE2("M1050_PPV_RCVD_AGNCY") == 00)
                    {
                        if (VALUE2("M1055_PPV_RSN_NOT_RCVD_AGNCY") == 04)
                        {
                            return false;
                        }

                        if (VALUE2("M1055_PPV_RSN_NOT_RCVD_AGNCY") == 01)
                        {
                            return false;
                        }

                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Pneumococcal_Vaccine_C1
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (VALUE2("M1051_PVX_RCVD_AGNCY") == 01)
                    {
                        return false;
                    }

                    if (VALUE2("M1051_PVX_RCVD_AGNCY") == 00)
                    {
                        if (VALUE2("M1056_PVX_RSN_NOT_RCVD_AGNCY") == 03)
                        {
                            return false;
                        }

                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Pneumococcal_Refused
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (VALUE2("M1050_PPV_RCVD_AGNCY") == 01)
                    {
                        return true;
                    }

                    if (VALUE2("M1050_PPV_RCVD_AGNCY") == 00)
                    {
                        if (VALUE2("M1055_PPV_RSN_NOT_RCVD_AGNCY") == 04)
                        {
                            return false;
                        }

                        if (VALUE2("M1055_PPV_RSN_NOT_RCVD_AGNCY") == 02)
                        {
                            return false;
                        }

                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Pneumococcal_Refused_C1
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (VALUE2("M1051_PVX_RCVD_AGNCY") == 01)
                    {
                        return true;
                    }

                    if (VALUE2("M1051_PVX_RCVD_AGNCY") == 00)
                    {
                        if (VALUE2("M1056_PVX_RSN_NOT_RCVD_AGNCY") == 03)
                        {
                            return false;
                        }

                        if (VALUE2("M1056_PVX_RSN_NOT_RCVD_AGNCY") == 01)
                        {
                            return false;
                        }

                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Pneumococcal_Contradicated
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (VALUE2("M1050_PPV_RCVD_AGNCY") == 01)
                    {
                        return true;
                    }

                    if (VALUE2("M1050_PPV_RCVD_AGNCY") == 00)
                    {
                        if (VALUE2("M1055_PPV_RSN_NOT_RCVD_AGNCY") == 04)
                        {
                            return false;
                        }

                        if (VALUE2("M1055_PPV_RSN_NOT_RCVD_AGNCY") == 03)
                        {
                            return false;
                        }

                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Pneumococcal_Contradicated_C1
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (VALUE2("M1051_PVX_RCVD_AGNCY") == 01)
                    {
                        return true;
                    }

                    if (VALUE2("M1051_PVX_RCVD_AGNCY") == 00)
                    {
                        if (VALUE2("M1056_PVX_RSN_NOT_RCVD_AGNCY") == 03)
                        {
                            return false;
                        }

                        if (VALUE2("M1056_PVX_RSN_NOT_RCVD_AGNCY") == 02)
                        {
                            return false;
                        }

                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Med_Monitoring_SOC
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (VALUE1("M2000_DRUG_RGMN_RVW") != 1)
                    {
                        return false;
                    }

                    if (VALUE1("M2002_MDCTN_FLWP") == 1)
                    {
                        return false;
                    }

                    if (VALUE1("M2002_MDCTN_FLWP") == 0)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Med_Monitoring_SOC_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (TEXTVALUE1("M2001_DRUG_RGMN_RVW") != "1")
                    {
                        return false;
                    }

                    if (TEXTVALUE1("M2003_MDCTN_FLWP") == "1")
                    {
                        return false;
                    }

                    if (TEXTVALUE1("M2003_MDCTN_FLWP") == "0")
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Med_Monitoring_Implmnt_S_T
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode)
                    {
                        return Med_Monitoring_Implmnt_All;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Med_Monitoring_Implmnt_S_T_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode)
                    {
                        return Med_Monitoring_Implmnt_All_C2;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Med_Monitoring_Implmnt_L_T
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode == false)
                    {
                        return Med_Monitoring_Implmnt_All;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Med_Monitoring_Implmnt_L_T_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode == false)
                    {
                        return Med_Monitoring_Implmnt_All_C2;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Med_Monitoring_Implmnt_All
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (TEXTVALUE2("M2004_MDCTN_INTRVTN") == "NA")
                    {
                        return false;
                    }

                    if (VALUE2("M2004_MDCTN_INTRVTN") == 01)
                    {
                        return false;
                    }

                    if (VALUE2("M2004_MDCTN_INTRVTN") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Med_Monitoring_Implmnt_All_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if ((TEXTVALUE2("M2005_MDCTN_INTRVTN") == "9") || (TEXTVALUE2("M2005_MDCTN_INTRVTN") == "-"))
                    {
                        return false;
                    }

                    if (TEXTVALUE2("M2005_MDCTN_INTRVTN") == "1")
                    {
                        return false;
                    }

                    if (TEXTVALUE2("M2005_MDCTN_INTRVTN") == "0")
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool P_U_Prvnt_Implmnt_S_T
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode)
                    {
                        return P_U_Prvnt_Implmnt_All;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool P_U_Prvnt_Implmnt_S_T_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode)
                    {
                        return P_U_Prvnt_Implmnt_All_C2;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool P_U_Prvnt_Implmnt_L_T
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode == false)
                    {
                        return P_U_Prvnt_Implmnt_All;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool P_U_Prvnt_Implmnt_L_T_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (ST_Episode == false)
                    {
                        return P_U_Prvnt_Implmnt_All_C2;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool P_U_Prvnt_Implmnt_All
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (TEXTVALUE2("M2400_INTRVTN_SMRY_PRSULC_PRVN") == "NA")
                    {
                        return false;
                    }

                    if (VALUE2("M2400_INTRVTN_SMRY_PRSULC_PRVN") == 01)
                    {
                        return false;
                    }

                    if (VALUE2("M2400_INTRVTN_SMRY_PRSULC_PRVN") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool P_U_Prvnt_Implmnt_All_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 8)
                    {
                        return false;
                    }

                    if (TEXTVALUE2("M2401_INTRVTN_SMRY_PRSULC_PRVN") == "NA")
                    {
                        return false;
                    }

                    if (VALUE2("M2401_INTRVTN_SMRY_PRSULC_PRVN") == 01)
                    {
                        return false;
                    }

                    if (VALUE2("M2401_INTRVTN_SMRY_PRSULC_PRVN") == 00)
                    {
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Grooming
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1800_CRNT_GROOMING") > 00)
                        {
                            if (VALUE2("M1800_CRNT_GROOMING") < VALUE1("M1800_CRNT_GROOMING"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Stablz_Grooming
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1800_CRNT_GROOMING") < 03)
                        {
                            if (VALUE2("M1800_CRNT_GROOMING") <= VALUE1("M1800_CRNT_GROOMING"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Upper_Dress
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1810_CRNT_DRESS_UPPER") > 00)
                        {
                            if (VALUE2("M1810_CRNT_DRESS_UPPER") < VALUE1("M1810_CRNT_DRESS_UPPER"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Lower_Dress
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1820_CRNT_DRESS_LOWER") > 00)
                        {
                            if (VALUE2("M1820_CRNT_DRESS_LOWER") < VALUE1("M1820_CRNT_DRESS_LOWER"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Bathing
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1830_CRNT_BATHG") > 00)
                        {
                            if (VALUE2("M1830_CRNT_BATHG") < VALUE1("M1830_CRNT_BATHG"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Stablz_Bathing
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1830_CRNT_BATHG") < 06)
                        {
                            if (VALUE2("M1830_CRNT_BATHG") <= VALUE1("M1830_CRNT_BATHG"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Toilet_Transfer
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1840_CRNT_TOILTG") > 00)
                        {
                            if (VALUE2("M1840_CRNT_TOILTG") < VALUE1("M1840_CRNT_TOILTG"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Stablz_Toilet_Transfer
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1840_CRNT_TOILTG") < 04)
                        {
                            if (VALUE2("M1840_CRNT_TOILTG") <= VALUE1("M1840_CRNT_TOILTG"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Toilet_Hygeine
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1845_CRNT_TOILTG_HYGN") > 00)
                        {
                            if (VALUE2("M1845_CRNT_TOILTG_HYGN") < VALUE1("M1845_CRNT_TOILTG_HYGN"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Stablz_Toilet_Hygeine
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1845_CRNT_TOILTG_HYGN") < 03)
                        {
                            if (VALUE2("M1845_CRNT_TOILTG_HYGN") <= VALUE1("M1845_CRNT_TOILTG_HYGN"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Bed_Transfer
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1850_CRNT_TRNSFRNG") > 00)
                        {
                            if (VALUE2("M1850_CRNT_TRNSFRNG") < VALUE1("M1850_CRNT_TRNSFRNG"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Stablz_Bed_Transfer
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1850_CRNT_TRNSFRNG") < 05)
                        {
                            if (VALUE2("M1850_CRNT_TRNSFRNG") <= VALUE1("M1850_CRNT_TRNSFRNG"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Ambulation
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1860_CRNT_AMBLTN") > 00)
                        {
                            if (VALUE2("M1860_CRNT_AMBLTN") < VALUE1("M1860_CRNT_AMBLTN"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Eating
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1870_CRNT_FEEDING") > 00)
                        {
                            if (VALUE2("M1870_CRNT_FEEDING") < VALUE1("M1870_CRNT_FEEDING"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Light_Meal
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1880_CRNT_PREP_LT_MEALS") > 00)
                        {
                            if (VALUE2("M1880_CRNT_PREP_LT_MEALS") < VALUE1("M1880_CRNT_PREP_LT_MEALS"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Stablz_Light_Meal
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1880_CRNT_PREP_LT_MEALS") < 02)
                        {
                            if (VALUE2("M1880_CRNT_PREP_LT_MEALS") <= VALUE1("M1880_CRNT_PREP_LT_MEALS"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Phone
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1890_CRNT_PHONE_USE") > 00)
                        {
                            if (VALUE2("M1890_CRNT_PHONE_USE") < VALUE1("M1890_CRNT_PHONE_USE"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Stablz_Phone
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1890_CRNT_PHONE_USE") < 05)
                        {
                            if (VALUE2("M1890_CRNT_PHONE_USE") <= VALUE1("M1890_CRNT_PHONE_USE"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Oral_Meds
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09 && MISSINGVALUE1("M2020_CRNT_MGMT_ORAL_MDCTN") == false &&
                        TEXTVALUE1("M2020_CRNT_MGMT_ORAL_MDCTN") != "NA" &&
                        TEXTVALUE2("M2020_CRNT_MGMT_ORAL_MDCTN") != "NA")
                    {
                        if (VALUE1("M2020_CRNT_MGMT_ORAL_MDCTN") > 00)
                        {
                            if (VALUE2("M2020_CRNT_MGMT_ORAL_MDCTN") < VALUE1("M2020_CRNT_MGMT_ORAL_MDCTN"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Stablz_Oral_Meds
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09 && MISSINGVALUE1("M2020_CRNT_MGMT_ORAL_MDCTN") == false &&
                        TEXTVALUE1("M2020_CRNT_MGMT_ORAL_MDCTN") != "NA" &&
                        TEXTVALUE2("M2020_CRNT_MGMT_ORAL_MDCTN") != "NA")
                    {
                        if (VALUE1("M2020_CRNT_MGMT_ORAL_MDCTN") < 03)
                        {
                            if (VALUE2("M2020_CRNT_MGMT_ORAL_MDCTN") <= VALUE1("M2020_CRNT_MGMT_ORAL_MDCTN"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Dyspnea
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1400_WHEN_DYSPNEIC") > 00)
                        {
                            if (VALUE2("M1400_WHEN_DYSPNEIC") < VALUE1("M1400_WHEN_DYSPNEIC"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Pain
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1242_PAIN_FREQ_ACTVTY_MVMT") > 00)
                        {
                            if (VALUE2("M1242_PAIN_FREQ_ACTVTY_MVMT") < VALUE1("M1242_PAIN_FREQ_ACTVTY_MVMT"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Speech
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1230_SPEECH") > 00)
                        {
                            if (VALUE2("M1230_SPEECH") < VALUE1("M1230_SPEECH"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Stablz_Speech
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1230_SPEECH") < 05)
                        {
                            if (VALUE2("M1230_SPEECH") <= VALUE1("M1230_SPEECH"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Status_Wounds
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1340_SRGCL_WND_PRSNT") == 01 && VALUE1("M1342_STUS_PRBLM_SRGCL_WND") > 00 &&
                            VALUE2("M1340_SRGCL_WND_PRSNT") != 02)
                        {
                            if (VALUE2("M1340_SRGCL_WND_PRSNT") == 00 || VALUE2("M1342_STUS_PRBLM_SRGCL_WND") <
                                VALUE1("M1342_STUS_PRBLM_SRGCL_WND"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_UTI
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") == 09 && TEXTVALUE1("M1600_UTI") != "UK" &&
                        TEXTVALUE1("M1600_UTI") != "NA" && TEXTVALUE2("M1600_UTI") != "NA")
                    {
                        if (VALUE1("M1600_UTI") < 00)
                        {
                            if (VALUE2("M1600_UTI") < VALUE1("M1600_UTI"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Incont
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1610_UR_INCONT") > 00)
                        {
                            if (VALUE2("M1610_UR_INCONT") < VALUE1("M1610_UR_INCONT") ||
                                (VALUE2("M1610_UR_INCONT") == 01 &&
                                 VALUE2("M1615_INCNTNT_TIMING") < VALUE1("M1615_INCNTNT_TIMING")))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Bowel_Incont
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09 && TEXTVALUE1("M1620_BWL_INCONT") != "UK" &&
                        TEXTVALUE1("M1620_BWL_INCONT") != "NA" && TEXTVALUE2("M1620_BWL_INCONT") != "NA")
                    {
                        if (VALUE1("M1620_BWL_INCONT") > 00)
                        {
                            if (VALUE2("M1620_BWL_INCONT") < VALUE1("M1620_BWL_INCONT"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Confusion
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09 && TEXTVALUE2("M1710_WHEN_CONFUSED") != "NA")
                    {
                        if (VALUE1("M1710_WHEN_CONFUSED") > 00)
                        {
                            if (VALUE2("M1710_WHEN_CONFUSED") < VALUE1("M1710_WHEN_CONFUSED"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Stablz_Cognitive
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1700_COG_FUNCTION") < 04)
                        {
                            if (VALUE2("M1700_COG_FUNCTION") <= VALUE1("M1700_COG_FUNCTION"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Anxiety
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09 && TEXTVALUE2("M1720_WHEN_ANXIOUS") != "NA")
                    {
                        if (VALUE1("M1720_WHEN_ANXIOUS") > 00)
                        {
                            if (VALUE2("M1720_WHEN_ANXIOUS") < VALUE1("M1720_WHEN_ANXIOUS"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Stablz_Anxiety
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09 && TEXTVALUE2("M1720_WHEN_ANXIOUS") != "NA")
                    {
                        if (VALUE1("M1720_WHEN_ANXIOUS") < 03)
                        {
                            if (VALUE2("M1720_WHEN_ANXIOUS") <= VALUE1("M1720_WHEN_ANXIOUS"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Imprv_Behavior
        {
            get
            {
                try
                {
                    if (NonresponsiveAtSOCROC)
                    {
                        return false;
                    }

                    if (VALUE2("M0100_ASSMT_REASON") == 09)
                    {
                        if (VALUE1("M1745_BEH_PROB_FREQ") > 00)
                        {
                            if (VALUE2("M1745_BEH_PROB_FREQ") < VALUE1("M1745_BEH_PROB_FREQ"))
                            {
                                return false;
                            }

                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Util_ED_Use_No_Hosp
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") != 08)
                    {
                        if (VALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == 01)
                        {
                            return false;
                        }

                        if (VALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == 00 ||
                            VALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == 02)
                        {
                            return true;
                        }

                        if (TEXTVALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == "UK")
                        {
                            return false;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Util_ED_Use_No_Hosp_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") != 08)
                    {
                        if (VALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == 01)
                        {
                            return false;
                        }

                        if (VALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == 00 ||
                            VALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == 02)
                        {
                            return true;
                        }

                        if (TEXTVALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == "UK")
                        {
                            return false;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Util_ED_Use_W_Hosp
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") != 08)
                    {
                        if (VALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == 02)
                        {
                            return false;
                        }

                        if (VALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == 00 ||
                            VALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == 01)
                        {
                            return true;
                        }

                        if (TEXTVALUE2("M2300_EMER_USE_AFTR_LAST_ASMT") == "UK")
                        {
                            return false;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Util_ED_Use_W_Hosp_C2
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") != 08)
                    {
                        if (VALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == 02)
                        {
                            return false;
                        }

                        if (VALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == 00 ||
                            VALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == 01)
                        {
                            return true;
                        }

                        if (TEXTVALUE2("M2301_EMER_USE_AFTR_LAST_ASMT") == "UK")
                        {
                            return false;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Util_Hosp
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") != 08)
                    {
                        if ((VALUE2("M0100_ASSMT_REASON") == 06 || VALUE2("M0100_ASSMT_REASON") == 07) &&
                            VALUE2("M2410_INPAT_FACILITY") == 01 && VALUE2("M2430_HOSP_SCHLD_TRTMT") != 1)
                        {
                            return false;
                        }

                        if (VALUE2("M0100_ASSMT_REASON") == 09 ||
                            ((VALUE2("M0100_ASSMT_REASON") == 06 || VALUE2("M0100_ASSMT_REASON") == 07) &&
                                VALUE2("M2410_INPAT_FACILITY") != 01 || (VALUE2("M2410_INPAT_FACILITY") == 01 &&
                                                                         VALUE2("M2430_HOSP_SCHLD_TRTMT") == 1)))
                        {
                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Util_DC_Comm
        {
            get
            {
                try
                {
                    if (VALUE2("M0100_ASSMT_REASON") != 08)
                    {
                        if (VALUE2("M0100_ASSMT_REASON") == 09 &&
                            (VALUE2("M2420_DSCHRG_DISP") == 01 || VALUE2("M2420_DSCHRG_DISP") == 02))
                        {
                            return false;
                        }

                        if ((VALUE2("M0100_ASSMT_REASON") == 09 && VALUE2("M2420_DSCHRG_DISP") == 03) ||
                            VALUE2("M0100_ASSMT_REASON") == 06 || VALUE2("M0100_ASSMT_REASON") == 07)
                        {
                            return true;
                        }

                        if (VALUE2("M0100_ASSMT_REASON") == 09 && (TEXTVALUE2("M2420_DSCHRG_DISP") == "UK" ||
                                                                   TEXTVALUE2("M2420_DSCHRG_DISP") == "NA"))
                        {
                            return false;
                        }
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}