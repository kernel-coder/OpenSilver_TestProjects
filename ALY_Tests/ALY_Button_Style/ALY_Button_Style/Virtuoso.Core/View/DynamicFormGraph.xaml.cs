using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Virtuoso.Server.Data;
using Virtuoso.Core.Converters;
using Newtonsoft.Json;

namespace Virtuoso.Core.View
{
    public partial class DynamicFormGraph : ChildWindow
    {
        private Admission currentAdmission = null;
        private AdmissionWoundSite currentAdmissionWoundSite = null;
        private Encounter currentEncounter = null;
        public List<GraphItem> GraphItems = new List<GraphItem>();
        public bool isCancel = false;

        public DynamicFormGraph(Admission admission, Encounter encounter, string question, AdmissionWoundSite wound)
        {
            currentAdmissionWoundSite = wound;
            DoWork(admission, encounter, question);
        }

        public DynamicFormGraph(Admission admission, Encounter encounter, string question)
        {
            DoWork(admission, encounter, question);
        }

        private void DoWork(Admission admission, Encounter encounter, string question)
        {
            try
            {
                var jAdmission = JsonConvert.SerializeObject(admission);
                Console.WriteLine("jAdmission: " + jAdmission);
                var jEncounter = JsonConvert.SerializeObject(encounter);
                Console.WriteLine("jEncounter: " + jEncounter);
            }
            catch (Exception ex)
            {
            }
            try
            {
                InitializeComponent();

                bool useMilitaryTime = GraphItem.UsesMilitaryTime;

                currentAdmission = admission;
                currentEncounter = encounter;
                GraphItems = new List<GraphItem>();

                this.Title = this.Title + " " + question;
                chartBP.Title = question;

                DateTime? startDate = DateTime.MinValue;
                DateTime? endDate = DateTime.MaxValue;

                switch (question)
                {
                    case "Blood Pressure":
                        Chart_BloodPressure(useMilitaryTime, startDate, endDate);
                        break;
                    case "Blood Glucose":
                        Chart_BloodGlucose(useMilitaryTime, startDate, endDate);
                        break;
                    case "Pulse":
                        Chart_Pulse(useMilitaryTime, startDate, endDate);
                        break;
                    case "Respiration":
                        Chart_Respiration(useMilitaryTime, startDate, endDate);
                        break;
                    case "Oxygen Saturation":
                        Chart_OxygenSaturation(useMilitaryTime, startDate, endDate);
                        break;
                    case "Temperature":
                        Chart_Temperature(question, useMilitaryTime, startDate, endDate);
                        break;
                    case "Pain":
                        Chart_Pain(useMilitaryTime, startDate, endDate);
                        break;
                    case "Barthel Index":
                        Chart_BarthelIndex(useMilitaryTime, startDate, endDate);
                        break;
                    case "Weight":
                        Chart_Weight(question, useMilitaryTime, startDate, endDate);
                        break;
                    case "Wound PUSH© scores":
                        Chart_Wound(useMilitaryTime, startDate, endDate);
                        break;
                    case "BMI":
                        Chart_BMI(useMilitaryTime, startDate, endDate);
                        break;
                    case "BSA":
                        Chart_BSA(useMilitaryTime, startDate, endDate);
                        break;
                    case "Left Arm Circumference":
                        Chart_LeftArmCircumference(useMilitaryTime, startDate, endDate);
                        break;
                    case "Right Arm Circumference":
                        Chart_RightArmCircumference(useMilitaryTime, startDate, endDate);
                        break;
                    case "Abdominal Girth":
                        Chart_AbdominalGirth(question, useMilitaryTime, startDate, endDate);
                        break;
                    default:
                        Chart_Numeric(question, useMilitaryTime, startDate, endDate);
                        break;
                }

                ShowAppropriateChart(question);
            }
            catch(Exception exc)
            {
                Console.WriteLine("Graph Exception: " + exc.Message);
            }
        }

        private void Chart_Numeric(string question, bool useMilitaryTime, DateTime? startDate, DateTime? endDate)
        {
            // Generic questions - Assumes Integer
            List<EncounterData> l = (currentAdmission == null)
                ? null
                : currentAdmission.GetEncounterDataIntegerLabel(question, startDate, endDate);
            if ((l != null) && (l.Any() == true))
            {
                foreach (EncounterData ed in l)
                {
                    Encounter eed = currentAdmission.Encounter
                        .Where(e => (e.EncounterKey == ed.EncounterKey) && (e.HistoryKey == null)).FirstOrDefault();
                    if ((eed != null) && (ed.IntData != null))
                        GraphItems.Add(new GraphItem(useMilitaryTime, eed, ed.DateTimeData, ed.IntData.ToString(),
                            (int)ed.IntData));
                }
            }
        }

        private void ShowAppropriateChart(string question)
        {
            if ((GraphItems != null) && (GraphItems.Any() == true))
            {
                if (question.Equals("Blood Pressure"))
                {
                    chartBP.Tag = GraphItems;
                    chartBP.Visibility = Visibility.Visible;

                    chartNumeric.Visibility = Visibility.Collapsed;
                }
                else
                {
                    chartNumeric.Tag = GraphItems;
                    chartNumeric.Visibility = Visibility.Visible;

                    chartBP.Visibility = Visibility.Collapsed;
                }

                noReading.Visibility =
                    Visibility.Collapsed; // Have items to graph in chartBP or chartNumeric - hide No Reading
            }
            else
            {
                chartBP.Visibility = Visibility.Collapsed;
                chartNumeric.Visibility = Visibility.Collapsed;

                noReading.Visibility = Visibility.Visible;
            }
        }

        private void Chart_RightArmCircumference(bool useMilitaryTime, DateTime? startDate, DateTime? endDate)
        {
            List<EncounterData> l = (currentAdmission == null)
                ? null
                : currentAdmission.GetRightArmCircumferenceData(startDate, endDate);
            if ((l != null) && (l.Any() == true))
            {
                chartNumeric.Title = "Right Arm";
                foreach (EncounterData ed in l)
                {
                    try
                    {
                        float data = float.Parse(ed.Text2Data);
                        Encounter eed = currentAdmission.Encounter
                            .Where(e => (e.EncounterKey == ed.EncounterKey) && (e.HistoryKey == null)).FirstOrDefault();
                        if (eed != null)
                            GraphItems.Add(new GraphItem(useMilitaryTime, eed, ed.DateTimeData, ed.Text2Data,
                                (float?)data));
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void Chart_LeftArmCircumference(bool useMilitaryTime, DateTime? startDate, DateTime? endDate)
        {
            List<EncounterData> l = (currentAdmission == null)
                ? null
                : currentAdmission.GetLeftArmCircumferenceData(startDate, endDate);
            if ((l != null) && (l.Any() == true))
            {
                chartNumeric.Title = "Left Arm";
                foreach (EncounterData ed in l)
                {
                    try
                    {
                        float data = float.Parse(ed.TextData);
                        Encounter eed = currentAdmission.Encounter
                            .Where(e => (e.EncounterKey == ed.EncounterKey) && (e.HistoryKey == null)).FirstOrDefault();
                        if (eed != null)
                            GraphItems.Add(new GraphItem(useMilitaryTime, eed, ed.DateTimeData, ed.TextData,
                                (float?)data));
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void Chart_AbdominalGirth(string question, bool useMilitaryTime, DateTime? startDate, DateTime? endDate)
        {
            // Generic questions - Assumes Integer
            List<EncounterData> l = (currentAdmission == null)
                ? null
                : currentAdmission.GetEncounterDataIntegerLabel(question, startDate, endDate);
            if ((l != null) && (l.Any() == true))
            {
                chartNumeric.Title = question;
                foreach (EncounterData ed in l)
                {
                    Encounter eed = currentAdmission.Encounter
                        .Where(e => (e.EncounterKey == ed.EncounterKey) && (e.HistoryKey == null)).FirstOrDefault();
                    if ((eed != null) && (ed.IntData != null))
                        GraphItems.Add(new GraphItem(useMilitaryTime, eed, ed.DateTimeData, ed.IntData.ToString(),
                            (int)ed.IntData));
                }
            }
        }

        private void Chart_BSA(bool useMilitaryTime, DateTime? startDate, DateTime? endDate)
        {
            var l = (currentAdmission == null) ? null : currentAdmission.GetEncounterDataBSA(startDate, endDate);
            if ((l != null) && (l.Any() == true))
            {
                chartNumeric.Title = "BSA";
                foreach (var ed in l)
                {
                    try
                    {
                        Encounter eed = currentAdmission.Encounter
                            .Where(e => (e.EncounterKey == ed.EncounterKey) && (e.HistoryKey == null)).FirstOrDefault();
                        if ((eed != null) && (ed != null) && (ed.BSAValue != null))
                            GraphItems.Add(new GraphItem(useMilitaryTime, eed, ed.WeightDateTime, ed.BSAThumbNail,
                                ed.BSAValue));
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void Chart_BMI(bool useMilitaryTime, DateTime? startDate, DateTime? endDate)
        {
            var l = (currentAdmission == null) ? null : currentAdmission.GetEncounterDataBMI(startDate, endDate);
            if ((l != null) && (l.Any() == true))
            {
                chartNumeric.Title = "BMI";
                foreach (var ed in l)
                {
                    Encounter eed = currentAdmission.Encounter
                        .Where(e => (e.EncounterKey == ed.EncounterKey) && (e.HistoryKey == null)).FirstOrDefault();
                    if ((eed != null) && (ed != null) && (ed.BMIValue != null))
                        GraphItems.Add(new GraphItem(useMilitaryTime, eed, ed.WeightDateTime, ed.BMIThumbNail,
                            ed.BMIValue));
                }
            }
        }

        private void Chart_Wound(bool useMilitaryTime, DateTime? startDate, DateTime? endDate)
        {
            List<EncounterWoundSite> l = (currentAdmission == null)
                ? null
                : currentAdmission.GetWoundPushScored(startDate, endDate, currentAdmissionWoundSite.Number);
            if ((l != null) && (l.Any() == true))
            {
                chartNumeric.Title = "Wound Push Score";
                foreach (EncounterWoundSite ed in l)
                {
                    Encounter eed = currentAdmission.Encounter
                        .Where(e => (e.EncounterKey == ed.EncounterKey) && (e.HistoryKey == null)).FirstOrDefault();
                    if ((eed != null) && (ed != null) && (ed.AdmissionWoundSite != null) &&
                        (ed.AdmissionWoundSite.PushScore != null))
                        GraphItems.Add(new GraphItem(useMilitaryTime, eed, null,
                            ed.AdmissionWoundSite.PushScore.ToString(), (int?)ed.AdmissionWoundSite.PushScore));
                }
            }
        }

        private void Chart_Weight(string question, bool useMilitaryTime, DateTime? startDate, DateTime? endDate)
        {
            var l = (currentAdmission == null) ? null : currentAdmission.GetEncounterDataWeight(startDate, endDate);
            if ((l != null) && (l.Any() == true))
            {
                bool isWeightLB = l.FirstOrDefault().IsWeightLB;
                chartNumeric.Title = string.Format("{0} ({1}s)", question, (isWeightLB) ? "lb" : "kg");
                foreach (var ed in l)
                {
                    Encounter eed = currentAdmission.Encounter
                        .Where(e => (e.EncounterKey == ed.EncounterKey) && (e.HistoryKey == null)).FirstOrDefault();
                    float? weight = (isWeightLB) ? ed.WeightLB : ed.WeightKG;
                    if ((eed != null) && (weight != null) && (weight != 0))
                        GraphItems.Add(new GraphItem(useMilitaryTime, eed, ed.WeightDateTime, ed.WeightThumbNail,
                            weight));
                }
            }
        }

        private void Chart_BarthelIndex(bool useMilitaryTime, DateTime? startDate, DateTime? endDate)
        {
            List<EncounterData> l = (currentAdmission == null)
                ? null
                : currentAdmission.GetEncounterDataTypeRealForQuestionLabel(startDate, endDate, "Barthel Index Score");
            if ((l != null) && (l.Any() == true))
            {
                chartNumeric.Title = "Barthel Index";
                foreach (EncounterData ed in l)
                {
                    Encounter eed = currentAdmission.Encounter
                        .Where(e => (e.EncounterKey == ed.EncounterKey) && (e.HistoryKey == null)).FirstOrDefault();
                    if ((eed != null) && (ed.RealData != null))
                        GraphItems.Add(new GraphItem(useMilitaryTime, eed, ed.DateTimeData, ed.RealData.ToString(),
                            ed.RealData));
                }
            }
        }

        private void Chart_Pain(bool useMilitaryTime, DateTime? startDate, DateTime? endDate)
        {
            List<EncounterPain> l = (currentAdmission == null)
                ? null
                : currentAdmission.GetEncounterPainScored(startDate, endDate);
            if ((l != null) && (l.Any() == true))
            {
                chartNumeric.Title = "Pain";
                foreach (EncounterPain ed in l)
                {
                    Encounter eed = currentAdmission.Encounter
                        .Where(e => (e.EncounterKey == ed.EncounterKey) && (e.HistoryKey == null)).FirstOrDefault();
                    if ((eed != null) && (ed.PainScoreInt != null))
                        GraphItems.Add(new GraphItem(useMilitaryTime, eed, null, ed.ThumbNail, (int?)ed.PainScoreInt));
                }
            }
        }

        private void Chart_Temperature(string question, bool useMilitaryTime, DateTime? startDate, DateTime? endDate)
        {
            List<EncounterTemp> l = (currentAdmission == null)
                ? null
                : currentAdmission.GetEncounterTemp(startDate, endDate);
            if ((l != null) && (l.Any() == true))
            {
                bool isTempF = l.FirstOrDefault().IsTempF;
                chartNumeric.Title = string.Format("{0} ({1})", question, (isTempF) ? "F" : "C");
                foreach (EncounterTemp ed in l)
                {
                    Encounter eed = currentAdmission.Encounter
                        .Where(e => (e.EncounterKey == ed.EncounterKey) && (e.HistoryKey == null)).FirstOrDefault();
                    float? temp = (isTempF) ? ed.TempF : ed.TempC;
                    if ((eed != null) && (temp != null) && (temp != 0))
                        GraphItems.Add(new GraphItem(useMilitaryTime, eed,
                            ed.GetReadingDateTime(eed.EncounterIsVisitTeleMonitoring), ed.ThumbNail, temp));
                }
            }
        }

        private void Chart_OxygenSaturation(bool useMilitaryTime, DateTime? startDate, DateTime? endDate)
        {
            List<EncounterSpo2> l = (currentAdmission == null)
                ? null
                : currentAdmission.GetEncounterSpo2(startDate, endDate);
            if ((l != null) && (l.Any() == true))
            {
                chartNumeric.Title = "Oxygen Saturation";
                foreach (EncounterSpo2 ed in l)
                {
                    Encounter eed = currentAdmission.Encounter
                        .Where(e => (e.EncounterKey == ed.EncounterKey) && (e.HistoryKey == null)).FirstOrDefault();
                    if ((eed != null) && (ed.Spo2Percent != 0))
                        GraphItems.Add(new GraphItem(useMilitaryTime, eed,
                            ed.GetReadingDateTime(eed.EncounterIsVisitTeleMonitoring), ed.ThumbNail,
                            (int?)ed.Spo2Percent));
                }
            }
        }

        private void Chart_Respiration(bool useMilitaryTime, DateTime? startDate, DateTime? endDate)
        {
            List<EncounterResp> l = (currentAdmission == null)
                ? null
                : currentAdmission.GetEncounterResp(startDate, endDate);
            if ((l != null) && (l.Any() == true))
            {
                chartNumeric.Title = "Respiration";
                foreach (EncounterResp ed in l)
                {
                    Encounter eed = currentAdmission.Encounter
                        .Where(e => (e.EncounterKey == ed.EncounterKey) && (e.HistoryKey == null)).FirstOrDefault();
                    if ((eed != null) && (ed.RespRate != 0))
                        GraphItems.Add(new GraphItem(useMilitaryTime, eed,
                            ed.GetReadingDateTime(eed.EncounterIsVisitTeleMonitoring), ed.ThumbNail,
                            (int?)ed.RespRate));
                }
            }
        }

        private void Chart_Pulse(bool useMilitaryTime, DateTime? startDate, DateTime? endDate)
        {
            List<EncounterPulse> l = (currentAdmission == null)
                ? null
                : currentAdmission.GetEncounterPulse(startDate, endDate);
            if ((l != null) && (l.Any() == true))
            {
                chartNumeric.Title = "Pulse";
                foreach (EncounterPulse ed in l)
                {
                    Encounter eed = currentAdmission.Encounter
                        .Where(e => (e.EncounterKey == ed.EncounterKey) && (e.HistoryKey == null)).FirstOrDefault();
                    if ((eed != null) && (ed.PulseRate != 0))
                        GraphItems.Add(new GraphItem(useMilitaryTime, eed,
                            ed.GetReadingDateTime(eed.EncounterIsVisitTeleMonitoring), ed.ThumbNail,
                            (int?)ed.PulseRate));
                }
            }
        }

        private void Chart_BloodGlucose(bool useMilitaryTime, DateTime? startDate, DateTime? endDate)
        {
            List<EncounterCBG> l = (currentAdmission == null)
                ? null
                : currentAdmission.GetEncounterCBG(startDate, endDate);
            if ((l != null) && (l.Any() == true))
            {
                chartNumeric.Title = "Blood Glucose";
                foreach (EncounterCBG ed in l)
                {
                    Encounter eed = currentAdmission.Encounter
                        .Where(e => (e.EncounterKey == ed.EncounterKey) && (e.HistoryKey == null)).FirstOrDefault();
                    if ((eed != null) && (ed.CBG != 0))
                        GraphItems.Add(new GraphItem(useMilitaryTime, eed, ed.CBGDateTime, ed.ThumbNail, (int?)ed.CBG));
                }
            }
        }

        private void Chart_BloodPressure(bool useMilitaryTime, DateTime? startDate, DateTime? endDate)
        {
            chartBP.Title = "Blood Pressure";
            List<EncounterBP> l = (currentAdmission == null)
                ? null
                : currentAdmission.GetEncounterBP(startDate, endDate);
            if ((l != null) && (l.Any() == true))
            {
                foreach (EncounterBP ed in l)
                {
                    Encounter eed = currentAdmission.Encounter
                        .Where(e => (e.EncounterKey == ed.EncounterKey) && (e.HistoryKey == null)).FirstOrDefault();
                    if ((eed != null) && (ed.BPSystolic != 0) && (ed.BPDiastolic != 0))
                    {
                        // value1 = BPSystolic
                        // value2 = BPDiastolic
                        GraphItems.Add(new GraphItem(useMilitaryTime, eed,
                            ed.GetReadingDateTime(eed.EncounterIsVisitTeleMonitoring), ed.ThumbNail,
                            (int?)ed.BPSystolic, (int?)ed.BPDiastolic));
                    }
                }
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}