#region Usings

using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Occasional;
using Virtuoso.Core.Utility;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class Form : IForm
    {
        private string __FormTypeCode;
        private bool? __IsAttachedForm;
        private bool? __IsAttempted;
        private bool? __IsAuthorizationRequest;
        private bool? __IsBasicVisit;
        private bool? __IsCMSForm;
        private bool? __IsCOTI;

        private bool? __IsDischarge;

        private bool? __IsEval;
        private bool? __IsHISAdmission;
        private bool? __IsHISDischarge;
        private bool? __IsHospiceElectionAddendum;
        private bool? __IsHospiceF2F;
        private bool? __IsOasis;

        private bool? __IsOrderEntry;
        private bool? __IsPlanOfCare;
        private bool? __IsPreEval;
        private bool? __IsResumption;
        private bool? __IsTeamMeeting;
        private bool? __IsTransfer;
        private bool? __IsVerbalCOTI;
        private bool? __IsVisit;
        private bool? __IsVisitTeleMonitoring;
        private bool? __IsWOCN;

        public string FormalFormDescription
        {
            get
            {
#if DEBUG
                if (Locked)
                {
                    return string.Format("{0} (MODEL) {1}", Description, FormKey);
                }

                return string.Format("{0} {1}", Description, FormKey);
#else
                return this.Description;
#endif
            }
        }

        public string DescriptionPlusInactive => Description + (Inactive ? " (Inactive form)" : "");

        public bool Cloned { get; set; }

        public override bool CanDelete => !Locked && Encounter.Any() == false && AttachedVisitForms.Any() == false;

        public ICollectionView SortedFormSection
        {
            get
            {
                var cvs = new CollectionViewSource
                {
                    Source = FormSection
                };
                cvs.SortDescriptions.Add(new SortDescription("Sequence", ListSortDirection.Ascending));
                cvs.View.MoveCurrentToFirst();

                return cvs.View;
            }
        }

        public string AdmissionDocumentationFormTypeDescription
        {
            get
            {
                if (!FormTypeKey.HasValue)
                {
                    return null;
                }

                var clc = CodeLookupCache.GetCodeFromKey((int)FormTypeKey);
                var clcd = CodeLookupCache.GetCodeDescriptionFromKey((int)FormTypeKey);
                if (clc == null || clcd == null)
                {
                    return null;
                }

                if (clc.ToLower() == "attachedform")
                {
                    return null;
                }

                if (clc.ToLower() == "orderentry")
                {
                    return "Interim Order";
                }

                if (clc.ToLower() == "basicvisit")
                {
                    return "Visit";
                }

                if (clc.ToLower() == "hisadmission")
                {
                    return "HIS";
                }

                if (clc.ToLower() == "hisdischarge")
                {
                    return "HIS";
                }

                if (clc.ToLower() == "hisdischarge")
                {
                    return "HIS";
                }

                if (clc.ToLower() == "oasis")
                {
                    return "OASIS";
                }

                if (clc.ToLower() == "hospiceaddendum")
                {
                    return "Hospice Election Addendum";
                }

                return clcd;
            }
        }

        private string FormTypeCode
        {
            get
            {
                if (__FormTypeCode == null)
                {
                    return CodeLookupCache.GetCodeFromKey((int)FormTypeKey);
                }

                return __FormTypeCode;
            }
        }

        public bool IsDischarge
        {
            get
            {
                if (__IsDischarge.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsDischarge = false;
                        return __IsDischarge.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsDischarge = false;
                        return __IsDischarge.Value;
                    }

                    __IsDischarge = cl.ToLower() == "discharge";
                }

                return __IsDischarge.Value;
            }
        }

        public bool IsAuthorizationRequest
        {
            get
            {
                if (__IsAuthorizationRequest.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsAuthorizationRequest = false;
                        return __IsAuthorizationRequest.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsAuthorizationRequest = false;
                        return __IsAuthorizationRequest.Value;
                    }

                    __IsAuthorizationRequest = cl.ToLower() == "authorizationrequest";
                }

                return __IsAuthorizationRequest.Value;
            }
        }

        public bool IsHIS => IsHISAdmission || IsHISDischarge;

        public bool IsHISAdmission
        {
            get
            {
                if (__IsHISAdmission.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsHISAdmission = false;
                        return __IsHISAdmission.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsHISAdmission = false;
                        return __IsHISAdmission.Value;
                    }

                    __IsHISAdmission = cl.ToLower() == "hisadmission";
                }

                return __IsHISAdmission.Value;
            }
        }

        public bool IsHISDischarge
        {
            get
            {
                if (__IsHISDischarge.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsHISDischarge = false;
                        return __IsHISDischarge.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsHISDischarge = false;
                        return __IsHISDischarge.Value;
                    }

                    __IsHISDischarge = cl.ToLower() == "hisdischarge";
                }

                return __IsHISDischarge.Value;
            }
        }

        public bool IsHospiceElectionAddendum
        {
            get
            {
                if (__IsHospiceElectionAddendum.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsHospiceElectionAddendum = false;
                        return __IsHospiceElectionAddendum.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsHospiceElectionAddendum = false;
                        return __IsHospiceElectionAddendum.Value;
                    }

                    __IsHospiceElectionAddendum = cl.ToLower() == "hospiceaddendum";
                }

                return __IsHospiceElectionAddendum.Value;
            }
        }

        public bool IsAttempted
        {
            get
            {
                if (__IsAttempted.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsAttempted = false;
                        return __IsAttempted.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsAttempted = false;
                        return __IsAttempted.Value;
                    }

                    __IsAttempted = cl.ToLower() == "attempted";
                }

                return __IsAttempted.Value;
            }
        }

        public string DescriptionHeader
        {
            get
            {
                if (IsOrderEntry)
                {
                    return "Interim Order / Plan of Care Change";
                }

                return Description;
            }
        }

        public string CMSFormDescription
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Description))
                {
                    return "CMS Form";
                }

                if (IsCMSForm == false)
                {
                    return Description;
                }

                return Description.Replace("CMSForm", "").Replace("cmsform", "").Replace("CMSFORM", "");
            }
        }

        public bool IsCMSForm
        {
            get
            {
                if (__IsCMSForm.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsCMSForm = false;
                        return __IsCMSForm.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsCMSForm = false;
                        return __IsCMSForm.Value;
                    }

                    __IsCMSForm = Description.Trim().ToLower().StartsWith("cmsform") ? true : false;
                }

                return __IsCMSForm.Value;
            }
        }

        public bool IsAttachedForm
        {
            get
            {
                if (__IsAttachedForm.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsAttachedForm = false;
                        return __IsAttachedForm.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsAttachedForm = false;
                        return __IsAttachedForm.Value;
                    }

                    __IsAttachedForm = cl.ToLower() == "attachedform";
                }

                return __IsAttachedForm.Value;
            }
        }

        public bool IsBasicVisit
        {
            get
            {
                if (__IsBasicVisit.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsBasicVisit = false;
                        return __IsAttachedForm.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsBasicVisit = false;
                        return __IsBasicVisit.Value;
                    }

                    __IsBasicVisit = cl.ToLower() == "basicvisit";
                }

                return __IsBasicVisit.Value;
            }
        }

        public bool IsWOCN
        {
            get
            {
                if (__IsWOCN.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsWOCN = false;
                        return __IsWOCN.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsWOCN = false;
                        return __IsWOCN.Value;
                    }

                    __IsWOCN = cl.ToLower() == "wocn";
                }

                return __IsWOCN.Value;
            }
        }

        public bool IsEval
        {
            get
            {
                if (__IsEval.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsEval = false;
                        return __IsEval.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsEval = false;
                        return __IsEval.Value;
                    }

                    __IsEval = cl.ToLower() == "eval";
                }

                return __IsEval.Value;
            }
        }

        public bool IsOasis
        {
            get
            {
                if (__IsOasis.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsOasis = false;
                        return __IsOasis.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsOasis = false;
                        return __IsOasis.Value;
                    }

                    __IsOasis = cl.ToLower() == "oasis";
                }

                return __IsOasis.Value;
            }
        }

        public bool IsOrderEntry
        {
            get
            {
                if (__IsOrderEntry.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsOrderEntry = false;
                        return __IsOrderEntry.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsOrderEntry = false;
                        return __IsOrderEntry.Value;
                    }

                    __IsOrderEntry = cl.ToLower() == "orderentry";
                }

                return __IsOrderEntry.Value;
            }
        }

        public bool IsPlanOfCare
        {
            get
            {
                if (__IsPlanOfCare.HasValue)
                {
                    return __IsPlanOfCare.Value;
                }

                if (!FormTypeKey.HasValue)
                {
                    __IsPlanOfCare = false;
                    return false;
                }

                var cl = FormTypeCode;
                if (cl == null)
                {
                    __IsPlanOfCare = false;
                    return false;
                }

                __IsPlanOfCare = cl.ToLower() == "planofcare";
                return __IsPlanOfCare.Value;
            }
        }

        public bool IsPreEval
        {
            get
            {
                if (__IsPreEval.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsPreEval = false;
                        return __IsPreEval.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsPreEval = false;
                        return __IsPreEval.Value;
                    }

                    __IsPreEval = cl.ToLower() == "preeval";
                }

                return __IsPreEval.Value;
            }
        }

        public bool IsResumption
        {
            get
            {
                if (__IsResumption.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsResumption = false;
                        return __IsResumption.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsResumption = false;
                        return __IsResumption.Value;
                    }

                    __IsResumption = cl.ToLower() == "resumption";
                }

                return __IsResumption.Value;
            }
        }

        public bool IsTransfer
        {
            get
            {
                if (__IsTransfer.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsTransfer = false;
                        return __IsTransfer.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsTransfer = false;
                        return __IsTransfer.Value;
                    }

                    __IsTransfer = cl.ToLower() == "transfer";
                }

                return __IsTransfer.Value;
            }
        }

        public bool IsCOTI
        {
            get
            {
                if (__IsCOTI.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsCOTI = false;
                        return __IsCOTI.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsCOTI = false;
                        return __IsCOTI.Value;
                    }

                    __IsCOTI = cl.ToLower() == "cti";
                }

                return __IsCOTI.Value;
            }
        }

        public bool IsVerbalCOTI
        {
            get
            {
                if (__IsVerbalCOTI.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsVerbalCOTI = false;
                        return __IsVerbalCOTI.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsVerbalCOTI = false;
                        return __IsVerbalCOTI.Value;
                    }

                    __IsVerbalCOTI = cl.ToLower() == "verbalcti";
                }

                return __IsVerbalCOTI.Value;
            }
        }

        public bool IsHospiceF2F
        {
            get
            {
                if (__IsHospiceF2F.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsHospiceF2F = false;
                        return __IsHospiceF2F.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsHospiceF2F = false;
                        return __IsHospiceF2F.Value;
                    }

                    __IsHospiceF2F = cl.ToLower() == "hospicef2f";
                }

                return __IsHospiceF2F.Value;
            }
        }

        public bool IsTeamMeeting
        {
            get
            {
                if (__IsTeamMeeting.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsTeamMeeting = false;
                        return __IsTeamMeeting.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsTeamMeeting = false;
                        return __IsTeamMeeting.Value;
                    }

                    __IsTeamMeeting = cl.ToLower() == "teammeeting";
                }

                return __IsTeamMeeting.Value;
            }
        }

        public bool IsVisit
        {
            get
            {
                if (__IsVisit.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsVisit = false;
                        return __IsVisit.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsVisit = false;
                        return __IsVisit.Value;
                    }

                    __IsVisit = cl.ToLower() == "visit";
                }

                return __IsVisit.Value;
            }
        }

        public bool IsVisitTeleMonitoring
        {
            get
            {
                if (__IsVisitTeleMonitoring.HasValue == false)
                {
                    if (!FormTypeKey.HasValue)
                    {
                        __IsVisitTeleMonitoring = false;
                        return __IsVisitTeleMonitoring.Value;
                    }

                    var cl = FormTypeCode;
                    if (cl == null)
                    {
                        __IsVisitTeleMonitoring = false;
                        return __IsVisitTeleMonitoring.Value;
                    }

                    __IsVisitTeleMonitoring = cl.ToLower() == "visittelemonitoring";
                }

                return __IsVisitTeleMonitoring.Value;
            }
        }

        public Form CreateNewVersion(bool supercede)
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newForm = (Form)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newForm);
            RejectChanges();
            BeginEditting();
            if (supercede)
            {
                Superceded = true;
            }

            Cloned = true;
            EndEditting();
            return newForm;
        }

        public Form CloneForm(out bool cloned, bool force = false)
        {
            if (!IsNew && !Cloned && (force || HasChanges || FormSection.Where(fs =>
                        fs.HasChanges || fs.IsNew ||
                        fs.FormSectionQuestion.Where(fsq => fsq.HasChanges || fsq.IsNew).Any())
                    .Any()))
            {
                var newForm = CreateNewVersion(true);
                FormSection.ForEach(fs =>
                {
                    if (fs.IsNew)
                    {
                        newForm.FormSection.Add(fs);
                    }
                    else
                    {
                        var newfs = fs.CreateNewVersion();
                        fs.FormSectionQuestion.ForEach(fsq =>
                        {
                            if (fsq.IsNew)
                            {
                                newfs.FormSectionQuestion.Add(fsq);
                            }
                            else
                            {
                                var newfsq = fsq.CreateNewVersion();
                                fsq.FormSectionQuestionAttribute.ForEach(fsqa =>
                                {
                                    if (fsqa.IsNew)
                                    {
                                        newfsq.FormSectionQuestionAttribute.Add(fsqa);
                                    }
                                    else
                                    {
                                        var newfsqa = fsqa.CreateNewVersion();
                                        newfsq.FormSectionQuestionAttribute.Add(newfsqa);
                                    }
                                });
                                newfs.FormSectionQuestion.Add(newfsq);
                            }
                        });

                        newForm.FormSection.Add(newfs);
                    }
                });

                ServiceType.ForEach(st =>
                {
                    ServiceType.Remove(st);
                    newForm.ServiceType.Add(st);
                });

                cloned = true;
                return newForm;
            }

            cloned = false;
            return this;
        }

        partial void OnFormTypeKeyChanged()
        {
            __FormTypeCode = null;
            __IsAttachedForm = null;
            __IsAttempted = null;
            __IsAuthorizationRequest = null;
            __IsBasicVisit = null;
            __IsCMSForm = null;
            __IsCOTI = null;
            __IsDischarge = null;
            __IsEval = null;
            __IsHISAdmission = null;
            __IsHospiceElectionAddendum = null;
            __IsHISDischarge = null;
            __IsHospiceF2F = null;
            __IsOasis = null;
            __IsOrderEntry = null;
            __IsPlanOfCare = null;
            __IsPreEval = null;
            __IsResumption = null;
            __IsTeamMeeting = null;
            __IsTransfer = null;
            __IsVerbalCOTI = null;
            __IsVisit = null;
            __IsVisitTeleMonitoring = null;
            __IsWOCN = null;
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsDischarge");
            RaisePropertyChanged("IsEval");
            RaisePropertyChanged("IsOasis");
            RaisePropertyChanged("IsOrderEntry");
            RaisePropertyChanged("IsPlanOfCare");
            RaisePropertyChanged("IsPreEval");
            RaisePropertyChanged("IsResumption");
            RaisePropertyChanged("IsTransfer");
            RaisePropertyChanged("IsCOTI");
            RaisePropertyChanged("IsVerbalCOTI");
            RaisePropertyChanged("IsHospiceF2F");
            RaisePropertyChanged("IsTeamMeeting");
            RaisePropertyChanged("IsVisit");
            RaisePropertyChanged("IsVisitTeleMonitoring");
            RaisePropertyChanged("IsAttachedForm");
            RaisePropertyChanged("IsBasicVisit");
            RaisePropertyChanged("IsWOCN");
            RaisePropertyChanged("IsAuthorizationRequest");
            RaisePropertyChanged("IsHospiceElectionAddendum");
        }

        public Form CopyForm()
        {
            var newForm = CreateNewVersion(false);
            FormSection.ForEach(fs =>
            {
                if (fs.IsNew)
                {
                    newForm.FormSection.Add(fs);
                }
                else
                {
                    var newfs = fs.CreateNewVersion();
                    fs.FormSectionQuestion.ForEach(fsq =>
                    {
                        if (fsq.IsNew)
                        {
                            newfs.FormSectionQuestion.Add(fsq);
                        }
                        else
                        {
                            var newfsq = fsq.CreateNewVersion();
                            newfs.FormSectionQuestion.Add(newfsq);
                        }
                    });

                    newForm.FormSection.Add(newfs);
                }
            });
            return newForm;
        }

        public bool FormContainsQuestion(Question q)
        {
            if (q == null)
            {
                return false;
            }

            if (FormSection == null)
            {
                return false;
            }

            foreach (var fs in FormSection)
                if (fs.FormSectionQuestion != null &&
                    fs.FormSectionQuestion.Where(fsq => fsq.QuestionKey == q.QuestionKey).Any())
                {
                    return true;
                }

            return false;
        }
    }

    public partial class FormSection
    {
        public ICollectionView SortedFormSectionQuestion
        {
            get
            {
                var cvs = new CollectionViewSource();
                cvs.Source = FormSectionQuestion;
                cvs.SortDescriptions.Add(new SortDescription("Sequence", ListSortDirection.Ascending));
                cvs.View.MoveCurrentToFirst();

                return cvs.View;
            }
        }

        partial void OnRequiredChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (Required && RequiredWhenOASIS)
            {
                RequiredWhenOASIS = false;
            }
        }

        partial void OnRequiredWhenOASISChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (Required && RequiredWhenOASIS)
            {
                Required = false;
            }
        }

        public FormSection CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newFormSection = (FormSection)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newFormSection);
            RejectChanges();
            BeginEditting();
            if (HasChanges)
            {
                Superceded = true;
            }

            EndEditting();
            return newFormSection;
        }
    }

    public partial class FormSectionQuestion
    {
        public string Label => Question.Label;

        partial void OnProtectedOverrideChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (ProtectedOverride && !CopyForward)
            {
                CopyForward = true;
            }
        }

        public FormSectionQuestion CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newfsq = (FormSectionQuestion)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newfsq);
            RejectChanges();
            BeginEditting();
            if (HasChanges)
            {
                Superceded = true;
            }

            EndEditting();
            return newfsq;
        }

        public FormSectionQuestionAttribute GetQuestionAttributeForName(string AttrName)
        {
            if (FormSectionQuestionAttribute == null)
            {
                return null;
            }

            return FormSectionQuestionAttribute.FirstOrDefault(qa => qa.AttributeName.ToUpper() == AttrName.ToUpper());
        }
    }

    public partial class FormSectionQuestionAttribute
    {
        public FormSectionQuestionAttribute CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newfsqa = (FormSectionQuestionAttribute)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newfsqa);
            RejectChanges();
            BeginEditting();
            EndEditting();
            return newfsqa;
        }
    }

    public partial class Section
    {
        public bool IsMedicalEligibilitySection
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Label))
                {
                    return false;
                }

                if (Label == "Medicare Eligibility")
                {
                    return true;
                }

                if (Label == "Eligibility Status")
                {
                    return true;
                }

                if (Label == "Eligibility : Homebound Status and Medical Necessity")
                {
                    return true;
                }

                if (Label == "Eligibility: Homebound Status and Medical Necessity")
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsOasisSection
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Label))
                {
                    return false;
                }

                if (Label.ToUpper().StartsWith("OASIS - "))
                {
                    return true;
                }

                if (Label.ToUpper().StartsWith("HIS - "))
                {
                    return true;
                }

                return false;
            }
        }

        public string OasisLabel
        {
            get
            {
                if (IsOasisSection == false)
                {
                    return Label;
                }

                if (Label.ToUpper().StartsWith("OASIS - "))
                {
                    return Label.Substring(8, Label.Length - 8);
                }

                if (Label.ToUpper().StartsWith("HIS - "))
                {
                    return Label.Substring(6, Label.Length - 6);
                }

                return Label;
            }
        }
    }

    public partial class QuestionGroupQuestion
    {
        public string Label => Question.Label;
    }

    public interface IForm
    {
        bool IsPlanOfCare { get; }
        bool IsVisit { get; }
        bool IsEval { get; }
        bool IsOasis { get; }
        bool IsOrderEntry { get; }
        bool IsPreEval { get; }
        bool IsResumption { get; }
        bool IsTeamMeeting { get; }
        bool IsTransfer { get; }
        bool IsCOTI { get; }
        bool IsVerbalCOTI { get; }
        bool IsHospiceF2F { get; }
        bool IsVisitTeleMonitoring { get; }
    }
}