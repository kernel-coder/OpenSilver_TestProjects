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
        private Encounter currentEncounter = null;
        public List<GraphItem> GraphItems = new List<GraphItem>();
        public bool isCancel = false;


        public DynamicFormGraph(Admission admission, Encounter encounter, string question)
        {
            DoWork(admission, encounter, question);
        }

        private void DoWork(Admission admission, Encounter encounter, string question)
        {
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
                GraphItems.Add(new GraphItem(false, null, DateTime.Now, "15 F Aux", 15, 15));
                GraphItems.Add(new GraphItem(false, null, DateTime.Now.AddDays(1), "20 F Aux", 20, 20));
                GraphItems.Add(new GraphItem(false, null, DateTime.Now.AddDays(2), "25 F Aux", 25, 25));
                GraphItems.Add(new GraphItem(false, null, DateTime.Now.AddDays(3), "10 F Aux", 10, 10));
                GraphItems.Add(new GraphItem(false, null, DateTime.Now.AddDays(4), "5 F Aux", 5, 5));

                switch (question)
                {

                    case "Temperature":
                        //Chart_Temperature(question, useMilitaryTime, startDate, endDate);
                        break;
                }

                ShowAppropriateChart(question);
            }
            catch(Exception exc)
            {
                Console.WriteLine("Graph Exception: " + exc.Message);
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

        //private void Chart_Temperature(string question, bool useMilitaryTime, DateTime? startDate, DateTime? endDate)
        //{
        //    List<EncounterTemp> l = (currentAdmission == null)
        //        ? null
        //        : currentAdmission.GetEncounterTemp(startDate, endDate);
        //    if ((l != null) && (l.Any() == true))
        //    {
        //        bool isTempF = l.FirstOrDefault().IsTempF;
        //        chartNumeric.Title = string.Format("{0} ({1})", question, (isTempF) ? "F" : "C");
        //        foreach (EncounterTemp ed in l)
        //        {
        //            Encounter eed = currentAdmission.Encounter
        //                .Where(e => (e.EncounterKey == ed.EncounterKey) && (e.HistoryKey == null)).FirstOrDefault();
        //            float? temp = (isTempF) ? ed.TempF : ed.TempC;
        //            if ((eed != null) && (temp != null) && (temp != 0))
        //                GraphItems.Add(new GraphItem(useMilitaryTime, eed,
        //                    ed.GetReadingDateTime(eed.EncounterIsVisitTeleMonitoring), ed.ThumbNail, temp));
        //        }
        //    }
        //}





        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}