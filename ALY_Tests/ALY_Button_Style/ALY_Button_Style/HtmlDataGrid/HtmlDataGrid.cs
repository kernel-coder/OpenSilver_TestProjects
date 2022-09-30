using CSHTML5.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace OSFControls
{
    public class HtmlDataGrid: Control
    {
        private object _table;
        private object _container;

        #region Events
        public event SelectionChangedEventHandler SelectionChanged;
        public event EventHandler Initialized;
        #endregion

        #region Dependency Properties
        public static DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(HtmlDataGrid),
            new PropertyMetadata(OnItemsSourceChanged));

        public static DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
            nameof(SelectedItems),
            typeof(IEnumerable),
            typeof(HtmlDataGrid),
            new PropertyMetadata(OnSelectedItemsChanged));

        public static DependencyProperty SelectedIndexesProperty = DependencyProperty.Register(
            nameof(SelectedIndexes),
            typeof(int[]),
            typeof(HtmlDataGrid),
            new PropertyMetadata(OnSelectedIndexesChanged));

        public static DependencyProperty GridLinesVisibilityProperty = DependencyProperty.Register(
            nameof(GridLinesVisibility),
            typeof(HtmlDataGridLinesVisibility),
            typeof(HtmlDataGrid),
            new PropertyMetadata(HtmlDataGridLinesVisibility.None, OnGridLinesVisibilityChanged)
            );

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public IEnumerable SelectedItems
        {
            get => (IEnumerable)GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        public int[] SelectedIndexes
        {
            get=> (int[])GetValue(SelectedIndexesProperty);
            set => SetValue(SelectedIndexesProperty, value);
        }

        public int SelectedIndex
        {
            get
            {
                if (SelectedIndexes != null && SelectedIndexes.Length > 0)
                    return SelectedIndexes[0];

                return -1;
            }
            set
            {
                if (_table == null)
                    return;

                if(value < 0)
                {
                    string script = $"let table = $('#{GetTableId()}').DataTable();"
                        + $"table.rows('.selected').deselect();";
                    OpenSilver.Interop.ExecuteJavaScript(script);
                }
                else
                {
                    string script = $"let table = $('#{GetTableId()}').DataTable();"
                        + $"table.rows({value}).select();";
                    OpenSilver.Interop.ExecuteJavaScript(script);
                }
            }
        }

        public HtmlDataGridLinesVisibility GridLinesVisibility
        {
            get => (HtmlDataGridLinesVisibility)GetValue(GridLinesVisibilityProperty);
            set => SetValue(GridLinesVisibilityProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == e.OldValue)
                return;

            ((HtmlDataGrid)d).SetDataGridSource((IEnumerable)e.NewValue);
        }

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == e.OldValue)
                return;
        }

        private static void OnSelectedIndexesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue)
                return;
        }

        private static void OnGridLinesVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }
        #endregion

        #region Public Properties
        public bool AutoGenerateColumns { get; set; }

        public ObservableCollection<HtmlDataGridColumn> Columns { get; set; }

        public bool CanUserSortColumns { get; set; }

        public bool CanUserReorderColumns { get; set; }

        public bool CanUserResizeColumns { get; set; }

        public bool AllowPaging { get; set; }

        public int PageSize { get; set; }

        public bool AutoWidth { get; set; }

        public string RowBackground { get; set; }

        public new string Foreground { get; set; }

        public string AlternatingRowBackground { get; set; }

        public string HoverColor { get; set; }

        public string HorizontalGridLinesColor { get; set; }

        public string VerticalGridLinesColor { get; set; }

        public double? ColumnHeaderHeight { get; set; }

        public string CellStyle { get; set; }

        public string HeaderStyle { get; set; }
        #endregion

        private void SetDataGridSource(IEnumerable items)
        {
            if (_table == null)
                return;

            string script = $"let table =  $('#{GetTableId()}').DataTable();"
                   + "table.clear().draw();";
            if (items == null)
            {
                OpenSilver.Interop.ExecuteJavaScript(script);
                return;
            }

            var data = JsonConvert.SerializeObject(items);
            script += $"table.rows.add({data}).draw();";
            OpenSilver.Interop.ExecuteJavaScript(script);

            foreach(var item in items)
            {
                if(item is INotifyPropertyChanged npc)
                {
                    npc.PropertyChanged += (s, e) =>
                    {
                        var rowIndex = items.Cast<object>().ToList().IndexOf(item);
                        var newData = JsonConvert.SerializeObject(item);
                        var js = $"let table =  $('#{GetTableId()}').DataTable();"
                            + $"table.row({rowIndex}).data(JSON.parse('{newData}'));";
                        OpenSilver.Interop.ExecuteJavaScript(js);
                    };
                }
            }
        }

        public HtmlDataGrid()
        {
            Columns = new ObservableCollection<HtmlDataGridColumn>();
            Columns.CollectionChanged += OnColumnsChanged; ;
            Loaded += OnLoaded;
        }

        private void OnColumnsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Add)
            {
                int index = e.NewStartingIndex;
                foreach(HtmlDataGridColumn column in e.NewItems)
                {
                    column.PropertyChanged += (s, arg) =>
                    {
                        switch (arg.PropertyName)
                        {
                            case nameof(HtmlDataGridColumn.Header):
                                {
                                    if (_table == null || string.IsNullOrEmpty(column.Header))
                                        return;

                                    string script = $"let table = $('#{GetTableId()}').DataTable();";
                                    script += $"let header = table.columns({index}).header();";
                                    script += $"$(header).html('{column.Header}')";
                                    OpenSilver.Interop.ExecuteJavaScript(script);
                                    break;
                                }
                        }
                    };
                    index++;
                }
            }
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var div = OpenSilver.Interop.GetDiv(this);
            OpenSilver.Interop.ExecuteJavaScript("$0.style.pointerEvents = 'auto'", div);
            Init();
            CreateCustomStyles();
            RegisterEvents();
        }

        public HtmlDataGridSelectionMode SelectionMode { get; set; } = HtmlDataGridSelectionMode.Single;

        public override object CreateDomElement(object parentRef, out object domElementWhereToPlaceChildren)
        {
            var outerDivStyle = INTERNAL_HtmlDomManager.CreateDomElementAppendItAndGetStyle("div", parentRef, this, out _container);
            _table = INTERNAL_HtmlDomManager.CreateDomFromStringAndAppendIt("<table class='stripe hover'/>", _container, this);
           
            domElementWhereToPlaceChildren = _container;
            return _container;
        }

        public string GetTableId()
        {
            if (_table == null)
                return null;

            return ((INTERNAL_HtmlDomElementReference)_table).UniqueIdentifier;
        }

        private string GetContainerId()
        {
            if (_container == null)
                return null;

            return ((INTERNAL_HtmlDomElementReference)_container).UniqueIdentifier;
        }

        private void Init()
        {
            var options = new HtmlDataGridOptions()
            {
                AllowPaging = AllowPaging,
                PageSize = PageSize,
                AllowSearching = false,
                CanSortColumn = CanUserSortColumns,
                CanReorderColumn = CanUserReorderColumns,
                Columns = Columns,
                ScrollY = this.Height > 0 ? (double?)this.Height : null,
                AutoWidth = AutoWidth,
                CanChangePageSize = false,
                SelectionOption = new HtmlDataGridSelectionOption(SelectionMode),
                ColumnDefitions = GetColumnDefs(),
                ShowInfo = false,
            };

            if (!string.IsNullOrEmpty(HeaderStyle))
            {
                options.HeaderCallBack = $"<func>function(thead,data){{$(thead).find('th').addClass('{HeaderStyle}');}}</func>";
            }
                
            string jOptions = JsonConvert.SerializeObject(options, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
            });

            jOptions = jOptions.Replace("\"<func>", "").Replace("</func>\"", "");
            string script = $"$('#{GetTableId()}').DataTable({jOptions});";
            OpenSilver.Interop.ExecuteJavaScript(script);

            SetDataGridSource(ItemsSource);
            Initialized?.Invoke(this, EventArgs.Empty);
        }

        private void RegisterEvents()
        {
            RegisterSelectionChangedEvent();
        }

        private void RegisterSelectionChangedEvent()
        {
            Action<string, string> onSelectionChanged = (selectionIndexes, selectedIndexes) =>
            {
                var newSelectionIndexes = JsonConvert.DeserializeObject<int[]>(selectionIndexes);
                var indexes = JsonConvert.DeserializeObject<int[]>(selectedIndexes);
                
                var selectedItems = new List<object>();
                if (ItemsSource != null)
                {
                    var items = ItemsSource.Cast<object>();
                    foreach (var index in indexes)
                    {
                        selectedItems.Add(items.ElementAt(index));
                    }
                }

                SelectedItems = selectedItems;
                SelectedIndexes = indexes;
                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(new List<object>(), new List<int>(newSelectionIndexes)));
            };

            string script = $"let table = $('#{GetTableId()}').DataTable();"
            + "table.on('select', function(e,dt,type,idx){"
            + " if(type === 'row'){"
            + "     let rows = table.rows('.selected');"
            + "     $0(JSON.stringify(idx), JSON.stringify(rows[0]));"
            + " }"
            + "})";
            OpenSilver.Interop.ExecuteJavaScript(script, onSelectionChanged);
        }

        private void CreateCustomStyles()
        {
            string id = GetTableId();
            string containerId = GetContainerId();
            string innerStyles = "";

            if(!string.IsNullOrEmpty(RowBackground)){
                innerStyles += $"#{id}.dataTable.stripe tbody tr{{background-color: {RowBackground}}}";
            }

            if (!string.IsNullOrEmpty(AlternatingRowBackground)){
                innerStyles += $"#{id}.dataTable.stripe tbody tr:nth-child(even){{background-color: {AlternatingRowBackground}}}";
            }

            if (!string.IsNullOrEmpty(Foreground))
            {
                innerStyles += $"#{id}.dataTable.stripe tbody {{color: {Foreground}}}";
            }

            if (!string.IsNullOrEmpty(HoverColor))
            {
                innerStyles += $"#{id}.dataTable.stripe tbody tr:hover{{background-color: {HoverColor}}}";
            }

            //grid lines
            string hColor = HorizontalGridLinesColor ?? Foreground ?? "lightgray";
            string vColor = VerticalGridLinesColor ?? Foreground ?? "lightgray";
            if (GridLinesVisibility == HtmlDataGridLinesVisibility.Horizontal)
            {
                innerStyles += $"#{id}.dataTable.stripe tbody tr td{{border-bottom:1px solid {hColor}}}";
                innerStyles += $"#{id}.dataTable.stripe tbody tr td:first-child{{border-left: 1px solid {vColor}}}";
                innerStyles += $"#{id}.dataTable.stripe tbody tr td:last-child{{border-right: 1px solid {vColor}}}";
            }
            else if(GridLinesVisibility == HtmlDataGridLinesVisibility.Vertical)
            {
                innerStyles += $"#{id}.dataTable.stripe tbody tr td{{border-right:1px solid {vColor}}}";
                innerStyles += $"#{id}.dataTable.stripe tbody tr td:first-child{{border-left: 1px solid {vColor}}}";
                innerStyles += $"#{id}.dataTable.stripe tbody tr:last-child td{{border-bottom: 1px solid {hColor}}}";
            }
            else if(GridLinesVisibility == HtmlDataGridLinesVisibility.All)
            {
                innerStyles += $"#{id}.dataTable.stripe tbody tr td{{border-right: 1px solid {vColor};border-bottom: 1px solid {hColor}}}";
                innerStyles += $"#{id}.dataTable.stripe tbody tr td:first-child{{border-left: 1px solid {vColor}}}";
            }

            //header
            string headerHeight = ColumnHeaderHeight != null ? $"{ColumnHeaderHeight}px" : "normal";
            innerStyles += $"#{containerId} table.dataTable.stripe thead th{{border-top: 1px solid rgba(0,0,0,0.3);border-right: 1px solid rgba(0,0,0,0.3);border-bottom: 2px solid rgba(0,0,0,0.3);padding:3px;background: rgb(235,235,235);line-height: {headerHeight};}}";
            innerStyles += $"#{containerId} table.dataTable.stripe thead th:first-child{{border-left: 1px solid rgba(0,0,0,0.3)}}";
            
            string script = "let style = document.createElement('style');"
                + "style.type = 'text/css';"
                + $"style.innerHTML = '{innerStyles}';"
                + "document.getElementsByTagName('head')[0].appendChild(style);";
            OpenSilver.Interop.ExecuteJavaScript(script);
        }

        private IEnumerable<HtmlDataGridColumnDefinition> GetColumnDefs()
        {
            if (Columns == null || Columns.Count == 0)
                return null;

            var columnDefs = new List<HtmlDataGridColumnDefinition>();
            for (int i = 0; i < Columns.Count; i++)
            {
                var column = Columns[i];
                var columnDef = new HtmlDataGridColumnDefinition()
                {
                    Targets = new int[] { i },
                };
                
                if(column is HtmlDataGridTemplateColumn templateColumn)
                {
                    columnDef.Render = $"<func>function(data,type,row,meta){{return `{templateColumn.CellTemplate.Html}`;}}</func>";
                    columnDefs.Add(columnDef);
                }  
            }

            if (!string.IsNullOrEmpty(CellStyle))
            {
                var columnsDef = new HtmlDataGridColumnDefinition()
                {
                    Targets = "_all",
                    ClassName = CellStyle
                };
                columnDefs.Add(columnsDef);
            }

            return columnDefs;
        }
    }
}
