#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight;
using Virtuoso.Core.Converters;
using Virtuoso.Core.Utility;

#endregion

namespace Virtuoso.Core.Helpers
{
    public class PopupDataTemplateCacheItem
    {
        public string Name { get; set; }
        public DependencyObject DataTemplate { get; set; }
    }

    public class DataTemplateHelper : ICleanup
    {
        private List<PopupDataTemplateCacheItem> PopupDataTemplateCacheList;

        public DependencyObject LoadDataTemplate(string PopupDataTemplate)
        {
            if (string.IsNullOrWhiteSpace(PopupDataTemplate))
            {
                return null;
            }

            if (PopupDataTemplateCacheList == null)
            {
                PopupDataTemplateCacheList = new List<PopupDataTemplateCacheItem>();
            }

            PopupDataTemplateCacheItem item = PopupDataTemplateCacheList.Where(i => i.Name == PopupDataTemplate.Trim())
                .FirstOrDefault();
            if (item != null && !PopupDataTemplate.EndsWith("Print"))
            {
                return item.DataTemplate;
            }

            DataTemplateConverter dtc = new DataTemplateConverter();
            if (dtc == null)
            {
                return null;
            }

            DependencyObject dataTemplate = null;
            try
            {
                dataTemplate = dtc.Convert(PopupDataTemplate, null, null, null) as DependencyObject;
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format(
                    "EXCEPTION: DataTemplateHelper.LoadDataTemplate(string PopupDataTemplate = {0}), Exception = {1}",
                    PopupDataTemplate, ex.ToString()));
                return null;
            }

            if (dataTemplate == null)
            {
                return null;
            }

            item = new PopupDataTemplateCacheItem { Name = PopupDataTemplate, DataTemplate = dataTemplate };
            PopupDataTemplateCacheList.Add(item);
            return dataTemplate;
        }

        public bool IsDataTemplateLoaded(string PopupDataTemplate)
        {
            if (string.IsNullOrWhiteSpace(PopupDataTemplate))
            {
                return false;
            }

            if (PopupDataTemplateCacheList == null)
            {
                return false;
            }

            PopupDataTemplateCacheItem item = PopupDataTemplateCacheList.Where(i => i.Name == PopupDataTemplate.Trim())
                .FirstOrDefault();
            if (item != null)
            {
                return true;
            }

            return false;
        }

        public DependencyObject LoadAndFocusDataTemplate(string PopupDataTemplate)
        {
            DependencyObject dataTemplate = LoadDataTemplate(PopupDataTemplate);
            if (dataTemplate != null)
            {
                SetFocusHelper.SelectFirstEditableWidget(dataTemplate);
            }

            return dataTemplate;
        }

        public void Cleanup()
        {
            if (PopupDataTemplateCacheList == null)
            {
                return;
            }

            foreach (PopupDataTemplateCacheItem pdtci in PopupDataTemplateCacheList)
                if (pdtci.DataTemplate != null)
                {
                    var icl = VirtuosoObjectCleanupHelper.FindVisualChildren<Control>(pdtci.DataTemplate)
                        .OfType<ICleanup>().ToList();
                    foreach (var rc in icl)
                    {
                        VirtuosoObjectCleanupHelper.CleanupAll(rc);
                        rc.Cleanup();
                    }

                    var fn = VirtuosoObjectCleanupHelper.FindVisualChildren<Control>(pdtci.DataTemplate).ToList();
                    foreach (var rc in fn)
                    {
                        ContentControl cc = rc as ContentControl;
                        if (cc != null)
                        {
                            cc.Content = null;
                        }
                    }

                    VirtuosoObjectCleanupHelper.CleanupAll(pdtci.DataTemplate);
                }

            PopupDataTemplateCacheList = null;
        }
    }
}