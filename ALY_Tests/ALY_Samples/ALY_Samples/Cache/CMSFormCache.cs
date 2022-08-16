#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.CMSForm)]
    [Export(typeof(ICache))]
    public class CMSFormCache : ReferenceCacheBase<CMSForm>
    {
        public static CMSFormCache Current { get; private set; }

        [ImportingConstructor]
        public CMSFormCache(ILogger logManager)
            : base(logManager, ReferenceTableName.CMSForm, "006")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("CMSFormCache already initialized.");
            }

            Current = this;
            CacheName = ReferenceTableName.CMSForm;
            RequireCacheRecords = true;
        }

        protected override EntitySet EntitySet => Context.CMSForms;

        protected override EntityQuery<CMSForm> GetEntityQuery()
        {
            return Context.GetCMSFormQuery();
        }

        public static CMSForm GetCMSFormByKey(int cmsFormKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.CMSForms == null))
            {
                return null;
            }

            CMSForm cf = Current.Context.CMSForms.Where(p => p.CMSFormKey == cmsFormKey).FirstOrDefault();
            if (cf == null)
            {
                MessageBox.Show(String.Format(
                    "Error CMSFormCache.GetCMSFormByKey: CMSFormKey {0} is not defined.  Contact your system administrator.",
                    cmsFormKey));
            }

            return cf;
        }

        public static List<CMSForm> GetActiveVersionOfCMSForms(bool includeEmpty = false,
            DateTime? EffectiveDate = null)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.CMSForms == null))
            {
                return null;
            }

            DateTime effectiveDate = (EffectiveDate == null) ? DateTime.Today.Date : EffectiveDate.Value.Date;
            List<CMSForm> cfList = Current.Context.CMSForms
                .Where(p => ((p.Inactive == false)
                             && (p.EffectiveFromDate.Date <= effectiveDate)
                             && ((p.EffectiveThruDate == null) || (p.EffectiveThruDate.Value.Date >= effectiveDate))))
                .ToList();
            if (includeEmpty)
            {
                cfList.Insert(0, new CMSForm { CMSFormKey = 0, Name = " " });
            }

            return cfList;
        }

        public static CMSForm GetActiveVersionOfCMSForm(string name, DateTime? effectiveDate = null)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.CMSForms == null))
            {
                return null;
            }

            DateTime date = (effectiveDate == null) ? DateTime.Today.Date : effectiveDate.Value.Date;
            List<CMSForm> cfList = GetActiveVersionOfCMSForms(false, date);
            if (cfList == null)
            {
                return null;
            }

            CMSForm cf = cfList.Where(p => p.Name == name).FirstOrDefault();
            if (cf == null)
            {
                MessageBox.Show(String.Format(
                    "Error CMSFormCache.GetActiveVersionOfCMSForm: Form name {0} with effective date of {1} is not defined.  Contact your system administrator.",
                    name, date.ToShortDateString()));
            }

            return cf;
        }

        public static CMSFormField GetCMSFormFieldByKey(int cmsFormFieldKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.CMSFormFields == null))
            {
                return null;
            }

            CMSFormField cff = Current.Context.CMSFormFields.Where(p => p.CMSFormFieldKey == cmsFormFieldKey)
                .FirstOrDefault();
            if (cff == null)
            {
                MessageBox.Show(String.Format(
                    "Error CMSFormCache.GetCMSFormFieldByKey: CMSFormFieldKey {0} is not defined.  Contact your system administrator.",
                    cmsFormFieldKey));
            }

            return cff;
        }
    }
}