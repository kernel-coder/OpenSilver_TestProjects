using System.Windows.Markup;

namespace OSFControls
{
    public class HtmlDataGridTemplateColumn: HtmlDataGridColumn
    {
        private HtmlPresenter _cellTemplate;
        public HtmlPresenter CellTemplate 
        {
            get => _cellTemplate;
            set
            {
                _cellTemplate = value;
                RaisePropertyChanged();
            }
        }
    }

    [ContentProperty("Html")]
    public class HtmlPresenter
    {
       public string Html { get; set; }
    }
}
