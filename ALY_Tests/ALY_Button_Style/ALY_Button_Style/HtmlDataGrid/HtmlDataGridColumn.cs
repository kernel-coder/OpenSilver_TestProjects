
using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OSFControls
{
    public abstract class HtmlDataGridColumn: INotifyPropertyChanged
    {
        private string _header;
        [JsonProperty("title")]
        public string Header
        {
            get => _header;
            set
            {
                _header = value;
                RaisePropertyChanged();
            }
        }

        private double? _width;
        [JsonProperty("width")]
        public double? Width 
        { 
            get => _width;
            set
            {
                _width = value;
                RaisePropertyChanged();
            } 
        }

        private bool _canSort = true;
        [JsonProperty("orderable")]
        public bool CanUserSort 
        {
            get => _canSort;
            set
            {
                _canSort = value;
                RaisePropertyChanged();
            }
        }

        private string _cellStyle;
        [JsonProperty("className")]
        public string CellStyle 
        {
            get => _cellStyle;
            set
            {
                _cellStyle = value;
                RaisePropertyChanged();
            }
        }

        private string _binding;
        [JsonProperty("data")]
        public string Binding 
        {
            get => _binding;
            set
            {
                _binding = value;
                RaisePropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
