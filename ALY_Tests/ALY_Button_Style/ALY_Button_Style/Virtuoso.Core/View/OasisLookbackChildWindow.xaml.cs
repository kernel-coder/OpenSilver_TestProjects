using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Virtuoso.Core.Services;
using Virtuoso.Server.Data;
using Virtuoso.Core.Converters;

namespace Virtuoso.Core.View
{
    public class LookbackItem
    {
        public string DataPointThumbNail { get; set; }
        public bool IsBilateralAmputee { get; set; }
        public bool IsDiagnosis { get; set; }
        public bool IsDyspnea { get; set; }
        public bool IsEdema { get; set; }
        public bool IsGoalElement { get; set; }
        public bool IsHeader { get; set; }
        public bool IsOrderEntry { get; set; }
        public bool IsPain { get; set; }
        public bool IsRisk { get; set; }
        public bool IsText { get; set; }
        public bool IsWeight { get; set; }
        public bool IsWoundM2400orM2401f { get; set; }
        public bool IsWoundM1309orM1313 { get; set; }
        public AdmissionWoundSite AdmissionWoundSite { get; set; }
        public Encounter Encounter { get; set; }
        public EncounterData EncounterData { get; set; }
        public EncounterWeight EncounterWeight { get; set; }
        public EncounterPain EncounterPain { get; set; }
        public EncounterRisk EncounterRisk { get; set; }
        public EncounterGoalElement GoalElement { get; set; }
        public string GoalText { get; set; }
        public OrderEntry OrderEntry { get; set; }
        public AdmissionDiagnosis AdmissionDiagnosis { get; set; }
        public string Text { get; set; }
        public float? Weight { get; set; }
        public DateTime WeightDate { get; set; }
    }

    public partial class OasisLookbackChildWindow : ChildWindow
    {
        public List<LookbackItem> LookbackItems = new List<LookbackItem>();
        private Admission currentAdmission = null;

        private void SetTitle(OasisManager oasisManager, string question)
        {
            string q = question;
            if (question.StartsWith("M2400orM2401"))
                q = ((oasisManager.IsOASISVersionC2orHigher) ? "M2401" : "M2400") +
                    question.Replace("M2400orM2401", "");
            this.Title = this.Title + " " + q;
        }

        public OasisLookbackChildWindow(OasisManager oasisManager, string question)
        {
            InitializeComponent();
            currentAdmission = oasisManager.CurrentAdmission;

            SetTitle(oasisManager, question);

            this.previousOasisSurvey.Text = (oasisManager.MostRecentOasisEncounterLookback == null)
                ? "No previous OASIS"
                : string.Format("Previous OASIS - RFA {0} completed on {1}",
                    oasisManager.MostRecentOasisEncounterLookback.EncounterOasisRFA,
                    ((DateTime)oasisManager.MostRecentOasisEncounterLookback.EncounterOasisM0090).ToShortDateString());
            if (oasisManager.MostRecentOasisEncounterLookback == null) return;

            if ((question.Equals("M1500")) || (question.Equals("M1501")))
            {
                //Dyspnea
                LookbackItems.Add(new LookbackItem() { IsHeader = true, Text = "Dyspnea" });
                List<EncounterData> edd = currentAdmission.GetEncounterDataDyspnea(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback);
                if (edd == null)
                {
                    LookbackItems.Add(new LookbackItem() { IsText = true, Text = "No dyspnea recorded" });
                }
                else
                {
                    foreach (EncounterData ed in edd)
                    {
                        Encounter eed = currentAdmission.Encounter.Where(e => (e.EncounterKey == ed.EncounterKey))
                            .FirstOrDefault();
                        LookbackItems.Add(new LookbackItem() { IsDyspnea = true, EncounterData = ed, Encounter = eed });
                    }
                }

                //Edema
                LookbackItems.Add(new LookbackItem() { IsHeader = true, Text = "Edema" });
                List<EncounterData> eda = currentAdmission.GetEncounterDataEdema(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback);
                if (eda == null)
                {
                    LookbackItems.Add(new LookbackItem() { IsText = true, Text = "No edema recorded" });
                }
                else
                {
                    foreach (EncounterData ed in eda)
                    {
                        Encounter eed = currentAdmission.Encounter.Where(e => (e.EncounterKey == ed.EncounterKey))
                            .FirstOrDefault();
                        LookbackItems.Add(new LookbackItem() { IsEdema = true, EncounterData = ed, Encounter = eed });
                    }
                }

                //WeightGain
                LookbackItems.Add(new LookbackItem() { IsHeader = true, Text = "Weight Gain" });
                var edListWeight = (currentAdmission == null)
                    ? null
                    : currentAdmission.GetEncounterDataWeight(oasisManager.StartDateLookback,
                        oasisManager.EndDateLookback);
                bool WeightGain = false;
                if (edListWeight != null)
                {
                    if (edListWeight.Count > 1)
                    {
                        float initialWeight = (float)edListWeight.FirstOrDefault().WeightLB;
                        float maxWeight = (float)edListWeight.Max(e => e.WeightLB);
                        if (initialWeight < maxWeight) WeightGain = true;
                    }
                }

                if (WeightGain)
                {
                    List<GraphItem> GraphItems = new List<GraphItem>();
                    bool IsWeightLB = edListWeight.FirstOrDefault().IsWeightLB;
                    foreach (var ed in edListWeight)
                    {
                        Encounter eed = currentAdmission.Encounter.Where(e => (e.EncounterKey == ed.EncounterKey))
                            .FirstOrDefault();
                        LookbackItems.Add(new LookbackItem()
                        {
                            IsWeight = true, EncounterWeight = ed, Encounter = eed,
                            Weight = (IsWeightLB) ? ed.WeightLB : ed.WeightKG,
                            WeightDate = (DateTime)eed.EncounterStartDate.GetValueOrDefault().Date,
                            DataPointThumbNail = ed.WeightThumbNail
                        });

                        bool useMilitaryTime = GraphItem.UsesMilitaryTime;

                        GraphItems.Add(new GraphItem(useMilitaryTime, eed, ed.WeightDateTime, ed.WeightThumbNail,
                            (IsWeightLB) ? ed.WeightLB : ed.WeightKG));
                    }

                    if ((GraphItems != null) && (GraphItems.Any() == true))
                    {
                        chartTabItemGraph.Visibility = Visibility.Visible;
                        chartControl.Tag = GraphItems;
                    }
                }
                else
                {
                    LookbackItems.Add(new LookbackItem() { IsText = true, Text = "No weight gain noted" });
                }
            }
            else if ((question.Equals("M1510")) || (question.Equals("M1511")))
            {
                LoadVerbalOrders(currentAdmission.GetOrderEntryM1510(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback));
                LoadGoalElements(currentAdmission.GetEncounterGoalElementM1510(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback));
            }
            else if ((question.Equals("M2004")) || (question.Equals("M2005")))
            {
                LoadVerbalOrders(currentAdmission.GetOrderEntryM2004(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback));
            }
            else if ((question.Equals("M2015")) || (question.Equals("M2016")))
            {
                bool found = false;
                oasisManager.ProcessFilteredPatientMedicationItems();
                if (oasisManager.CurrentFilteredPatientMedication != null)
                    foreach (PatientMedication p in oasisManager.CurrentFilteredPatientMedication)
                    {
                        found = true;
                        break;
                    }

                if (found == false)
                {
                    LookbackItems.Add(new LookbackItem()
                        { IsHeader = true, Text = "No medications are active for this patient" });
                }
                else
                {
                    LoadGoalElements(currentAdmission.GetEncounterGoalElementM2015(oasisManager.StartDateLookback,
                        oasisManager.EndDateLookback));
                }
            }
            else if ((question.Equals("M2300")) || (question.Equals("M2301")))
            {
                LoadVerbalOrders(
                    currentAdmission.GetOrderEntryM2300Y(oasisManager.StartDateLookback, oasisManager.EndDateLookback),
                    "Verbal Orders - Emergent Care with Hospital Stay");
                LoadVerbalOrders(
                    currentAdmission.GetOrderEntryM2300N(oasisManager.StartDateLookback, oasisManager.EndDateLookback),
                    "Verbal Orders - Emergent Care without Hospital Stay");
            }
            else if (question.Equals("M2400orM2401a"))
            {
                // Diagnosis
                if (oasisManager.IsDiabeticICD == false) LoadDiagnosis(oasisManager);
                // BilateralAmputee
                EncounterData edba =
                    currentAdmission.GetEncounterBilateralAmputee(DateTime.MinValue, oasisManager.EndDateLookback);
                if (edba != null)
                {
                    LookbackItems.Add(new LookbackItem() { IsHeader = true, Text = "Bilateral Amputee" });
                    Encounter ed = currentAdmission.Encounter.Where(e => (e.EncounterKey == edba.EncounterKey))
                        .FirstOrDefault();
                    string text = string.Format("Left  {0}   Right {0}", edba.IntDataCodeDescription,
                        edba.Int2DataCodeDescription);
                    LookbackItems.Add(new LookbackItem()
                        { IsBilateralAmputee = true, EncounterData = edba, Encounter = ed, Text = text });
                }

                LoadVerbalOrders(currentAdmission.GetOrderEntryM2400a(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback));
                if ((oasisManager.IsDiabeticICD) && (edba == null))
                    LoadGoalElements(currentAdmission.GetEncounterGoalElementM2400a(oasisManager.StartDateLookback,
                        oasisManager.EndDateLookback));
            }
            else if (question.Equals("M2400orM2401b"))
            {
                LoadRiskAssessment(currentAdmission.GetRiskAssessmentM2400b(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback));
                LoadVerbalOrders(currentAdmission.GetOrderEntryM2400b(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback));
                LoadGoalElements(currentAdmission.GetEncounterGoalElementM2400b(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback));
            }
            else if (question.Equals("M2400orM2401c"))
            {
                LoadRiskAssessment(currentAdmission.GetRiskAssessmentM2400c(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback));
                if (oasisManager.IsDepressionICD == false) LoadDiagnosis(oasisManager);
                LoadVerbalOrders(currentAdmission.GetOrderEntryM2400c(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback));
                LoadGoalElements(currentAdmission.GetEncounterGoalElementM2400c(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback));
            }
            else if (question.Equals("M2400orM2401d"))
            {
                //Pain
                List<EncounterPain> edp =
                    currentAdmission.GetEncounterPain(oasisManager.StartDateLookback, oasisManager.EndDateLookback);
                if (edp != null)
                {
                    LookbackItems.Add(new LookbackItem() { IsHeader = true, Text = "Recorded Pain" });
                    foreach (EncounterPain ep in edp)
                    {
                        Encounter eed = currentAdmission.Encounter.Where(e => (e.EncounterKey == ep.EncounterKey))
                            .FirstOrDefault();
                        LookbackItems.Add(new LookbackItem() { IsPain = true, EncounterPain = ep, Encounter = eed });
                    }
                }

                LoadVerbalOrders(currentAdmission.GetOrderEntryM2400d(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback));
                LoadGoalElements(currentAdmission.GetEncounterGoalElementM2400d(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback));
            }
            else if (question.Equals("M2400orM2401e"))
            {
                LoadRiskAssessment(currentAdmission.GetRiskAssessmentM2400e(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback));
                LoadVerbalOrders(currentAdmission.GetOrderEntryM2400e(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback));
                LoadGoalElements(currentAdmission.GetEncounterGoalElementM2400e(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback));
            }
            else if (question.Equals("M2400orM2401f"))
            {
                // Wounds
                LookbackItems.Add(new LookbackItem() { IsHeader = true, Text = "Pressure Ulcer Wounds" });
                IQueryable<AdmissionWoundSite> woundList = (oasisManager.CurrentFilteredAdmissionWoundSite == null)
                    ? null
                    : oasisManager.CurrentFilteredAdmissionWoundSite.OfType<AdmissionWoundSite>()
                        .AsQueryable<AdmissionWoundSite>();
                List<AdmissionWoundSite> unhealedPressureUlcerWoundList = (woundList == null)
                    ? null
                    : woundList.Where(w => (w.IsUnhealingPressureUlcer == true)).ToList();
                if ((unhealedPressureUlcerWoundList == null) || (unhealedPressureUlcerWoundList.Any() == false))
                {
                    LookbackItems.Add(
                        new LookbackItem() { IsText = true, Text = "No Pressure Ulcer wounds identified" });
                }
                else
                {
                    foreach (AdmissionWoundSite aw in unhealedPressureUlcerWoundList)
                        LookbackItems.Add(new LookbackItem() { IsWoundM2400orM2401f = true, AdmissionWoundSite = aw });
                }

                LoadVerbalOrders(currentAdmission.GetOrderEntryM2400f(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback));
                LoadGoalElements(currentAdmission.GetEncounterGoalElementM2400f(oasisManager.StartDateLookback,
                    oasisManager.EndDateLookback));
            }
            else if ((question.Equals("M1309")) || (question.Equals("M1313")))
            {
                // Wounds M1309 or M1313
                LookbackItems.Add(new LookbackItem()
                    { IsHeader = true, Text = "Worsening Pressure Ulcer wounds since SOC/ROC" });
                List<AdmissionWoundSite> woundList =
                    (oasisManager == null) ? null : oasisManager.GetM1309orM1313WorseningWoundList();
                if ((woundList == null) || (woundList.Any() == false))
                {
                    LookbackItems.Add(new LookbackItem()
                        { IsText = true, Text = "No Worsening Pressure Ulcer wounds identified" });
                }
                else
                {
                    foreach (AdmissionWoundSite aw in woundList)
                        LookbackItems.Add(new LookbackItem() { IsWoundM1309orM1313 = true, AdmissionWoundSite = aw });
                }
            }
            else
            {
                LookbackItem i = new LookbackItem()
                    { IsHeader = true, Text = "OASIS question " + question + " does not support look back" };
                LookbackItems.Add(i);
            }

            itemsControl.ItemsSource = LookbackItems;
        }

        private void LoadVerbalOrders(List<OrderEntry> list, string header = "Verbal Orders")
        {
            LookbackItems.Add(new LookbackItem() { IsHeader = true, Text = header });
            if (list == null)
            {
                LookbackItems.Add(new LookbackItem() { IsText = true, Text = "None since last OASIS" });
                return;
            }

            foreach (OrderEntry oe in list)
                LookbackItems.Add(new LookbackItem() { IsOrderEntry = true, OrderEntry = oe });
        }

        private void LoadGoalElements(List<EncounterGoalElement> list)
        {
            LookbackItems.Add(new LookbackItem() { IsHeader = true, Text = "Orders added to Care Plan / Addressed" });
            if (list == null)
            {
                LookbackItems.Add(new LookbackItem() { IsText = true, Text = "None since last OASIS" });
                return;
            }

            if (currentAdmission == null) return;
            foreach (EncounterGoalElement ege in list)
            {
                Encounter ge = currentAdmission.Encounter.Where(e => (e.EncounterKey == ege.EncounterKey))
                    .FirstOrDefault();
                if (ge == null) continue;
                string text = (ege.AdmissionGoalElement == null) ? "?" : ege.AdmissionGoalElement.GoalElementText;
                int? addedFromKey = (ege.AdmissionGoalElement == null)
                    ? null
                    : ege.AdmissionGoalElement.AddedFromEncounterKey;
                if ((addedFromKey != null) && (addedFromKey == ege.EncounterKey))
                {
                    LookbackItems.Add(new LookbackItem()
                    {
                        IsGoalElement = true, GoalElement = ege, Encounter = ge, Text = text,
                        GoalText = ((ege.Addressed == true) ? "Added & Addressed by" : "Added by")
                    });
                }
                else if (ege.Addressed == true)
                {
                    LookbackItems.Add(new LookbackItem()
                    {
                        IsGoalElement = true, GoalElement = ege, Encounter = ge, Text = text, GoalText = "Addressed by"
                    });
                }
            }
        }

        private void LoadDiagnosis(OasisManager oasisManager)
        {
            LookbackItems.Add(new LookbackItem() { IsHeader = true, Text = "Diagnosis" });
            bool found = false;
            if (oasisManager.CurrentFilteredAdmissionDiagnosis != null)
            {
                foreach (AdmissionDiagnosis pd in oasisManager.CurrentFilteredAdmissionDiagnosis)
                {
                    found = true;
                    LookbackItems.Add(new LookbackItem() { IsDiagnosis = true, AdmissionDiagnosis = pd });
                }
            }

            if (found == false) LookbackItems.Add(new LookbackItem() { IsText = true, Text = "No diagnosis recorded" });
        }

        private void LoadRiskAssessment(EncounterRisk er)
        {
            if (er != null)
            {
                LookbackItems.Add(new LookbackItem() { IsHeader = true, Text = "Risk Assessment" });
                Encounter eed = currentAdmission.Encounter.Where(e => (e.EncounterKey == er.EncounterKey))
                    .FirstOrDefault();
                LookbackItems.Add(new LookbackItem() { IsRisk = true, EncounterRisk = er, Encounter = eed });
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                this.DialogResult = false;
            }
        }
    }
}