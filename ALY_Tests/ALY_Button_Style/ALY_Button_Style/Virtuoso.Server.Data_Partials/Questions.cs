#region Usings

using System.ComponentModel;
using System.Windows.Data;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class Question
    {
        public bool ProtectedOverride { get; set; } = false;

        public bool DataTemplateNewDiagnosisVersionOverride(bool NewDiagnosisVersion)
        {
            if (NewDiagnosisVersion == false)
            {
                return false;
            }

            return DataTemplate.Equals("Diagnosis") || DataTemplate.Equals("POCDiagnosis") ||
                   DataTemplate.Equals("POCDiagnosisEdit")
                ? true
                : false;
        }
    }

    public partial class QuestionGroup
    {
        public ICollectionView SortedQuestionGroupQuestion
        {
            get
            {
                var cvs = new CollectionViewSource();
                cvs.Source = QuestionGroupQuestion;
                cvs.SortDescriptions.Add(new SortDescription("Sequence", ListSortDirection.Ascending));
                cvs.View.MoveCurrentToFirst();

                return cvs.View;
            }
        }
    }
}