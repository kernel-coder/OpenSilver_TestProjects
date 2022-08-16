#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Data;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.Services
{
    public partial class AdmissionDocumentationManager : INotifyPropertyChanged
    {
        public AdmissionDocumentationManager()
        {
            filteredInsuranceCollectionABN.SortDescriptions.Add(
                new SortDescription("Name", ListSortDirection.Ascending));
            filteredInsuranceCollectionMHES.SortDescriptions.Add(new SortDescription("Name",
                ListSortDirection.Ascending));
            filteredAttendingAdmissionPhysicianCollectionMHES.SortDescriptions.Add(
                new SortDescription("FormattedName", ListSortDirection.Ascending));
            availableTypes.Source = CodeLookupCache.GetCodeLookupsFromType("ABNType");
            availableTypes.SortDescriptions.Add(new SortDescription("CodeDescription", ListSortDirection.Ascending));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool CanEditDocumentationType(object SubItem, string DocumentType)
        {
            bool canEdit = true;
            if (!string.IsNullOrEmpty(DocumentType))
            {
                if ((DocumentType.ToLower() == "ff") || DocumentType.ToUpper().Equals("NDF2F"))
                {
                    AdmissionFaceToFace af = SubItem as AdmissionFaceToFace;
                    if ((af != null)
                        && (af.AdmissionFaceToFaceKey > 0)
                       )
                    {
                        canEdit = false;
                    }
                }

                if (DocumentType.ToLower() == "signedorder")
                {
                    AdmissionSignedInterimOrder af = SubItem as AdmissionSignedInterimOrder;
                    if ((af != null)
                        && (af.AdmissionSignedInterimOrderKey > 0)
                       )
                    {
                        canEdit = false;
                    }
                }

                if (DocumentType.ToLower() == "signedpoc")
                {
                    AdmissionSignedPOC af = SubItem as AdmissionSignedPOC;
                    if ((af != null)
                        && (af.AdmissionSignedPOCKey > 0)
                       )
                    {
                        canEdit = false;
                    }
                }
                else if (DocumentType.ToLower() == "signedbatchedorder")
                {
                    AdmissionBatchedInterimOrder af = SubItem as AdmissionBatchedInterimOrder;
                    if ((af != null)
                        && (af.AdmissionBatchedInterimOrderKey > 0)
                       )
                    {
                        canEdit = false;
                    }
                }
                else if ((DocumentType.ToLower() == "cti") || (DocumentType.ToLower() == "hospf2f") ||
                         (DocumentType.ToUpper() == "HNDF2F"))
                {
                    AdmissionCOTI ac = SubItem as AdmissionCOTI;
                    if ((ac != null)
                        && (ac.AdmissionCOTIKey > 0)
                       )
                    {
                        canEdit = false;
                    }
                }
                else if (DocumentType.ToLower() == "encounter")
                {
                    Encounter e = SubItem as Encounter;
                    if ((e != null)
                        && (e.EncounterKey > 0)
                       )
                    {
                        canEdit = false;
                    }
                }
                else if (DocumentType.ToLower() == "phi access")
                {
                    AdmissionDocumentationConsent ac = SubItem as AdmissionDocumentationConsent;
                    if ((ac != null)
                        && (ac.AdmissionDocumentationConsentKey > 0)
                       )
                    {
                        canEdit = false;
                    }
                }
                else if (DocumentType.ToLower() == "abn")
                {
                    AdmissionABN abn = SubItem as AdmissionABN;
                    if ((abn != null)
                        && (abn.AdmissionABNKey > 0)
                       )
                    {
                        canEdit = false;
                    }
                }
                else if (DocumentType.ToLower() == "mhes")
                {
                    AdmissionHospiceElectionStatement ahes = SubItem as AdmissionHospiceElectionStatement;
                    if ((ahes != null) && (ahes.AdmissionHospiceElectionStatementKey > 0))
                    {
                        canEdit = true;
                    }
                }
            }

            return canEdit;
        }

        public Entity GetSubItem(string DocumentType, AdmissionDocumentation SelectedItem, Admission CurrentAdmission)
        {
            Entity subItem = null;

            if ((SelectedItem != null)
                && !string.IsNullOrEmpty(DocumentType)
               )
            {
                if ((DocumentType.ToLower() == "ff") || DocumentType.ToUpper().Equals("NDF2F"))
                {
                    if ((SelectedItem.AdmissionFaceToFace != null)
                        && SelectedItem.AdmissionFaceToFace.Any()
                       )
                    {
                        subItem = SelectedItem.AdmissionFaceToFace.First();
                        AdmissionFaceToFace aF2F = subItem as AdmissionFaceToFace;
                        if (aF2F != null)
                        {
                            aF2F.UnattachedAdmission = CurrentAdmission;
                        }
                    }
                }
                else if (DocumentType.ToLower() == "signedorder")
                {
                    if ((SelectedItem.AdmissionSignedInterimOrder != null)
                        && SelectedItem.AdmissionSignedInterimOrder.Any()
                       )
                    {
                        subItem = SelectedItem.AdmissionSignedInterimOrder.First();
                        AdmissionSignedInterimOrder order = subItem as AdmissionSignedInterimOrder;
                        if (order != null)
                        {
                            order.OrderDateNullable = order.OrderDate;
                            order.OrderTimeNullable = order.OrderTime;
                        }
                    }
                }
                else if (DocumentType.ToLower() == "signedpoc")
                {
                    if ((SelectedItem.AdmissionSignedPOC != null)
                        && SelectedItem.AdmissionSignedPOC.Any()
                       )
                    {
                        subItem = SelectedItem.AdmissionSignedPOC.First();
                        AdmissionSignedPOC poc = subItem as AdmissionSignedPOC;
                        if (poc != null)
                        {
                            poc.CertFromDateNullable = poc.CertFromDate;
                            poc.CertThruDateNullable = poc.CertThruDate;
                            poc.SignatureDateNullable = poc.SignatureDate;
                        }
                    }
                }
                else if (DocumentType.ToLower() == "signedbatchedorder")
                {
                    if ((SelectedItem.AdmissionBatchedInterimOrder != null)
                        && SelectedItem.AdmissionBatchedInterimOrder.Any()
                       )
                    {
                        subItem = SelectedItem.AdmissionBatchedInterimOrder.First();
                        AdmissionBatchedInterimOrder batch = subItem as AdmissionBatchedInterimOrder;
                        if (batch != null)
                        {
                            batch.OrderDateNullable = batch.OrderDate;
                        }
                    }
                }
                else if (DocumentType.ToLower() == "encounter")
                {
                    subItem = SelectedItem.Encounter;
                }
                else if ((DocumentType.ToLower() == "cti") || (DocumentType.ToLower() == "hospf2f") ||
                         (DocumentType.ToUpper() == "HNDF2F"))
                {
                    if ((SelectedItem.AdmissionCOTI != null)
                        && SelectedItem.AdmissionCOTI.Any()
                       )
                    {
                        subItem = SelectedItem.AdmissionCOTI.First();
                    }
                }
                else if (DocumentType.ToLower() == "phi access")
                {
                    if ((SelectedItem.AdmissionDocumentationConsent != null)
                        && SelectedItem.AdmissionDocumentationConsent.Any()
                       )
                    {
                        subItem = SelectedItem.AdmissionDocumentationConsent.First();
                    }
                }
                else if (DocumentType.ToLower() == "abn")
                {
                    if ((SelectedItem.AdmissionABN != null)
                        && SelectedItem.AdmissionABN.Any()
                       )
                    {
                        subItem = SelectedItem.AdmissionABN.First();
                    }
                }
                else if (DocumentType.ToLower() == "mhes")
                {
                    if ((SelectedItem.AdmissionHospiceElectionStatement != null)
                        && SelectedItem.AdmissionHospiceElectionStatement.Any()
                       )
                    {
                        subItem = SelectedItem.AdmissionHospiceElectionStatement.First();
                        RefreshMHES(subItem as AdmissionHospiceElectionStatement);
                    }
                }
            }

            return subItem;
        }

        public Entity GetNewSubItem(string DocumentType, Admission CurrentAdmission)
        {
            Entity newSubItem = null;

            if (!string.IsNullOrEmpty(DocumentType))
            {
                if ((DocumentType.ToLower() == "ff") || DocumentType.ToUpper().Equals("NDF2F"))
                {
                    newSubItem = new AdmissionFaceToFace
                    {
                        AdmissionKey = CurrentAdmission.AdmissionKey,
                        UnattachedAdmission = CurrentAdmission,
                        PhysianEncounterDate = DateTime.Now.Date,
                        SigningPhysicianKey = 0,
                        SpecificDiscIdentified = null,
                        DatedSignaturePresent = null,
                        Disciplines = null,
                        ClinicalNeedDocumented = null
                    };
                }
                else if (DocumentType.ToLower() == "signedorder")
                {
                    newSubItem = new AdmissionSignedInterimOrder
                    {
                        AdmissionKey = CurrentAdmission.AdmissionKey,
                        OrderDateNullable = null,
                        OrderTimeNullable = null,
                        SigningPhysicianKey = 0
                    };
                }
                else if (DocumentType.ToLower() == "signedpoc")
                {
                    newSubItem = new AdmissionSignedPOC
                    {
                        AdmissionKey = CurrentAdmission.AdmissionKey,
                        CertFromDateNullable = null,
                        CertThruDateNullable = null,
                        SigningPhysicianKey = 0,
                        SignatureDateNullable = null
                    };
                }
                else if (DocumentType.ToLower() == "signedbatchedorder")
                {
                    newSubItem = new AdmissionBatchedInterimOrder
                    {
                        AdmissionKey = CurrentAdmission.AdmissionKey,
                        OrderDateNullable = null,
                        SigningPhysicianKey = 0,
                        InterimOrderBatchKey = 0
                    };
                }
                else if (DocumentType.ToLower() == "encounter")
                {
                    var dvalue = TenantSettingsCache.Current.TenantSettingDistanceTraveledMeasure;
                    newSubItem = new Encounter
                    {
                        AdmissionKey = CurrentAdmission.AdmissionKey,
                        PatientKey = CurrentAdmission.PatientKey,
                        AdmissionDisciplineKey =
                            0, //required for insert into DB, but don't have enough information to populate it yet
                        DistanceScale = dvalue,
                        EncounterBy =
                            WebContext.Current.User
                                .MemberID, //default to current user, they can change it though                
                        EncounterDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                        Signed = true, //can we do this - complete the encounter without it truly being signed - we don't ever have a signature for these encounters...
                        EncounterStatus = (int)EncounterStatusType.Completed,
                        UpdatedBy = WebContext.Current.User.MemberID,
                        UpdatedDate = DateTime.UtcNow,
                    };

                    Encounter CurrentEncounter = newSubItem as Encounter;
                    if (CurrentEncounter != null)
                    {
                        CurrentEncounter.SetupPatientAddressCollectionView(CurrentAdmission.Patient.PatientAddress);
                        CurrentEncounter
                            .FilterPatientAddressCollectionView(); //refresh collection will access the getter, which will initialize the collectionview...
                    }
                }
                else if ((DocumentType.ToLower() == "cti") || (DocumentType.ToLower() == "hospf2f") ||
                         (DocumentType.ToUpper() == "HNDF2F"))
                {
                    newSubItem = new AdmissionCOTI
                    {
                        AdmissionKey = CurrentAdmission.AdmissionKey,
                        SignatureDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
                    };

                    if ((DocumentType.ToLower() == "hospf2f") || (DocumentType.ToUpper() == "HNDF2F"))
                    {
                        newSubItem.As<AdmissionCOTI>().IsF2F = true;
                    }
                    else if (DocumentType.ToLower() == "cti")
                    {
                        newSubItem.As<AdmissionCOTI>().IsCOTI = true;
                    }
                }
                else if (DocumentType.ToLower() == "phi access")
                {
                    newSubItem = new AdmissionDocumentationConsent
                    {
                        AdmissionKey = CurrentAdmission.AdmissionKey
                    };
                }
                else if (DocumentType.ToLower() == "abn")
                {
                    newSubItem = new AdmissionABN
                    {
                        AdmissionKey = CurrentAdmission.AdmissionKey,
                        DateOfIssue = DateTime.Today,
                        DatedSignaturePresent = null
                    };
                }
                else if (DocumentType.ToLower() == "mhes")
                {
                    newSubItem = new AdmissionHospiceElectionStatement
                    {
                        AdmissionKey = CurrentAdmission.AdmissionKey,
                        InsuranceKey = null,
                        DesignationOfAttending = null,
                        AttendingAdmissionPhysicianKey = null,
                        HospiceEOBDate = null,
                        DatedSignaturePresent = null
                    };
                    RefreshMHES(newSubItem as AdmissionHospiceElectionStatement);
                    // Default AttendingAdmissionPhysicianKey if there is only one
                    AdmissionHospiceElectionStatement ahes = newSubItem as AdmissionHospiceElectionStatement;
                    int physicianType = (CodeLookupCache.GetKeyFromCode("PHTP", "PCP") == null)
                        ? 0
                        : (int)(CodeLookupCache.GetKeyFromCode("PHTP", "PCP"));
                    List<AdmissionPhysician> apList =
                        ((filteredAttendingAdmissionPhysicianCollectionMHES == null) ||
                         (filteredAttendingAdmissionPhysicianCollectionMHES.View == null))
                            ? null
                            : filteredAttendingAdmissionPhysicianCollectionMHES.View.Cast<AdmissionPhysician>()
                                .Where(p => p.PhysicianType == physicianType).ToList();
                    if ((apList != null) && (apList.Count == 1) && (ahes != null))
                    {
                        ahes.AttendingAdmissionPhysicianKey = apList.First().AdmissionPhysicianKey;
                    }
                }
            }

            return newSubItem;
        }

        public static bool FindPOC(Admission CurrentAdmission, Entity CurrentSubItem)
        {
            bool found = false;
            bool moreThanOne = false;
            IEnumerable<Encounter> encounters = null;
            if (CurrentSubItem != null)
            {
                AdmissionSignedPOC signedpoc = CurrentSubItem as AdmissionSignedPOC;

                encounters = GetPOCEncountersForSignedPOC(CurrentAdmission, signedpoc);

                if ((encounters != null)
                    && (encounters.Count() == 1)
                   )
                {
                    var enc = encounters.FirstOrDefault();

                    if ((enc != null)
                        && (enc.EncounterAdmission != null)
                        && enc.EncounterAdmission.Any()
                       )
                    {
                        EncounterAdmission ea = enc.EncounterAdmission.FirstOrDefault();
                        int? signingPhysicianKey = null;
                        if ((ea != null) && (CurrentAdmission != null) &&
                            (CurrentAdmission.HospiceAdmission == false) && (ea.SigningPhysicianKey != null))
                        {
                            signingPhysicianKey = (int)ea.SigningPhysicianKey;
                        }
                        else if ((ea != null) && (CurrentAdmission != null) && CurrentAdmission.HospiceAdmission &&
                                 (ea.AttendingPhysicianKey != null))
                        {
                            signingPhysicianKey = (int)ea.AttendingPhysicianKey;
                        }

                        if ((ea != null) && (signingPhysicianKey != null))
                        {
                            if (signedpoc != null)
                            {
                                found = true;
                                DateTime from = signedpoc.CertFromDate;
                                DateTime thru = signedpoc.CertThruDate;
                                TitleMessageBox mb = new TitleMessageBox("Verify Correct Plan of Care",
                                    "Verify that this is the correct Certification From and Thru Date for the Plan of Care being attached to this admisison."
                                    + Environment.NewLine + from.Date.ToString("MM/dd/yyyy")
                                    + " Thru " + thru.Date.ToString("MM/dd/yyyy"), "Yes", "No");
                                mb.Closed += (s, e) =>
                                {
                                    AdmissionSignedPOC poc1 = CurrentSubItem as AdmissionSignedPOC;
                                    IEnumerable<Encounter> enList =
                                        GetPOCEncountersForSignedPOC(CurrentAdmission, signedpoc);

                                    if ((enList != null)
                                        && enList.Any()
                                       )
                                    {
                                        var enc1 = enList.FirstOrDefault();
                                        if (mb.DialogResult.HasValue && mb.DialogResult.Value
                                                                     && (enc1 != null)
                                                                     && (enc1 != null)
                                           )
                                        {
                                            if ((enc1.EncounterAdmission != null) && enc1.EncounterAdmission.Any())
                                            {
                                                EncounterAdmission ea1 = enc1.EncounterAdmission.FirstOrDefault();
                                                if ((CurrentAdmission != null) &&
                                                    (CurrentAdmission.HospiceAdmission == false))
                                                {
                                                    poc1.SigningPhysicianKey = (ea1.SigningPhysicianKey == null)
                                                        ? 0
                                                        : (int)ea1.SigningPhysicianKey;
                                                }
                                                else if ((CurrentAdmission != null) &&
                                                         CurrentAdmission.HospiceAdmission)
                                                {
                                                    poc1.SigningPhysicianKey = (ea1.AttendingPhysicianKey == null)
                                                        ? 0
                                                        : (int)ea1.AttendingPhysicianKey;
                                                }
                                            }

                                            if ((enc1.EncounterPlanOfCare != null)
                                                && enc1.EncounterPlanOfCare.Any()
                                               )
                                            {
                                                poc1.EncounterPlanOfCareKey = enc1.EncounterPlanOfCare.First()
                                                    .EncounterPlanOfCareKey;
                                                if (enc1.EncounterPlanOfCare.First().SignedDate.HasValue)
                                                {
                                                    poc1.SignatureDateNullable = enc1.EncounterPlanOfCare.First()
                                                        .SignedDate.Value.Date;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            poc1.ValidationErrors.Add(new ValidationResult(
                                                "Cannot find order on the entered date and time",
                                                new[] { "SigningPhysicianKey" }));
                                        }
                                    }
                                };

                                mb.Show();
                            }
                        }
                    }
                }
                else
                {
                    if ((encounters != null)
                        && (encounters.Count() > 1)
                       )
                    {
                        moreThanOne = true;
                    }
                }

                if (moreThanOne)
                {
                    TitleMessageBox mb = new TitleMessageBox("Order Not Found",
                        "There is more than one active Plan of Care for the Admission and Certification Period.  A signed "
                        + "Plan of Care cannot be attached unless there is only one Plan of Care within the Certification Period. "
                        + "Please review the Plans of Care for this Admission.", "OK", null, true);
                    mb.Show();
                    signedpoc.ValidationErrors.Add(new ValidationResult(
                        "Cannot find order on the entered date and time", new[] { "SigningPhysicianKey" }));
                }
                else if (!found)
                {
                    TitleMessageBox mb = new TitleMessageBox("Order Not Found",
                        "The Certification Period From and/or Certification Period Thru Date do not represent a Plan of Care "
                        + "that has been sent and is awaiting signatures. "
                        + "Please review and update the information and press the Find Order button again.", "OK", null,
                        true);
                    mb.Show();
                    signedpoc.ValidationErrors.Add(new ValidationResult(
                        "Cannot find order on the entered date and time", new[] { "SigningPhysicianKey" }));
                }
            }

            return found && !moreThanOne;
        }

        public static IEnumerable<Encounter> GetPOCEncountersForSignedPOC(Admission CurrentAdmission,
            AdmissionSignedPOC signedpoc)
        {
            IEnumerable<Encounter> encounters = null;
            if (signedpoc != null)
            {
                if ((signedpoc.CertFromDateNullable.HasValue)
                    && (signedpoc.CertThruDateNullable.HasValue)
                   )
                {
                    signedpoc.SigningPhysicianKey = 0;
                    signedpoc.CertFromDate = signedpoc.CertFromDateNullable.Value;
                    signedpoc.CertThruDate = signedpoc.CertThruDateNullable.Value;

                    DateTime from = signedpoc.CertFromDateNullable.Value;
                    DateTime thru = signedpoc.CertThruDateNullable.Value;

                    if ((CurrentAdmission != null)
                        && (CurrentAdmission.Encounter != null)
                        && CurrentAdmission.Encounter.Any()
                        && (CurrentAdmission.Encounter.Any(e => (e.EncounterPlanOfCare != null)
                                                                && e.EncounterPlanOfCare.Any()
                            )
                        )
                       )
                    {
                        encounters = CurrentAdmission.Encounter.Where(e => e.EncounterPlanOfCare.Any(poc =>
                                (!e.Inactive)
                                && (e.EncounterStatus == (int)EncounterStatusType.Completed)
                                && poc.CertificationFromDate.HasValue
                                && (poc.CertificationFromDate.Value.Date == from.Date)
                                && poc.CertificationThruDate.HasValue
                                && (poc.CertificationThruDate.Value.Date == thru.Date)
                                && (!poc.AdmissionSignedPOC.Any(s => !s.AdmissionDocumentation.Inactive))
                            )
                        );
                    }
                }
            }

            return encounters;
        }

        public string GetPopupTitle(AdmissionDocumentation SelectedItem)
        {
            if (SelectedItem == null)
            {
                return null;
            }

            if (SelectedItem.CanEdit)
            {
                if ((SelectedItem.AdmissionFaceToFace != null)
                    && SelectedItem.AdmissionFaceToFace.Any()
                   )
                {
                    return "Edit Face to Face";
                }

                if (((SelectedItem.AdmissionCOTI != null)
                     && SelectedItem.AdmissionCOTI.Any()
                    )
                    || ((SelectedItem.AdmissionDocumentationConsent != null)
                        && SelectedItem.AdmissionDocumentationConsent.Any()
                    )
                    || ((SelectedItem.AdmissionABN != null)
                        && SelectedItem.AdmissionABN.Any()
                    )
                    || ((SelectedItem.AdmissionSignedInterimOrder != null)
                        && SelectedItem.AdmissionSignedInterimOrder.Any()
                    )
                    || ((SelectedItem.AdmissionSignedPOC != null)
                        && SelectedItem.AdmissionSignedPOC.Any()
                    )
                    || ((SelectedItem.AdmissionBatchedInterimOrder != null)
                        && SelectedItem.AdmissionBatchedInterimOrder.Any()
                    )
                    || ((SelectedItem.AdmissionHospiceElectionStatement != null)
                        && SelectedItem.AdmissionHospiceElectionStatement.Any()
                    )
                   )
                {
                    string label = SelectedItem.DocumentationTypeCodeDescription;
                    return "Edit " + label;
                }

                return "Attach Documentation";
            }

            return "View Documentation";
        }


        public static void SetViewLabel(AdmissionDocumentationItem adi)
        {
            if (adi.AdmissionDocumentationEncounterKey.HasValue)
            {
                adi.EncounterStatus = (int)EncounterStatusType.Completed;
                string serviceTypeDescription = "Service Type ?";
                if (adi.AdmissionDocumentation.AdmissionDocumentationEncounter != null)
                {
                    AdmissionDocumentationEncounter ade = adi.AdmissionDocumentation.AdmissionDocumentationEncounter
                        .FirstOrDefault();
                    if ((ade != null) && (ade.Encounter != null))
                    {
                        serviceTypeDescription =
                            ServiceTypeCache.GetDescriptionFromKey(ade.Encounter.ServiceTypeKey.Value);
                    }
                }

                adi.EncounterViewLabel = string.Format("View {0}", serviceTypeDescription);
                adi.DocumentationTypeLabel = serviceTypeDescription;
            }
            else if ((adi.AdmissionDocumentation.AdmissionFaceToFace != null)
                     && !adi.AdmissionDocumentation.Inactive
                     && (adi.AdmissionDocumentation.AdmissionFaceToFace.Any())
                    )
            {
                adi.EncounterViewLabel = "Edit Face to Face";
                adi.DocumentationTypeLabel = adi.AdmissionDocumentation.DocumentationTypeNoDocument
                    ? "Face to Face (no document)"
                    : "Face to Face";
            }
            else if ((adi.AdmissionDocumentation.AdmissionCOTI != null)
                     && !adi.AdmissionDocumentation.Inactive
                     && (adi.AdmissionDocumentation.AdmissionCOTI.Any())
                    )
            {
                string label = CodeLookupCache.GetCodeDescriptionFromKey(adi.DocumentationType);
                adi.EncounterViewLabel = string.Format("Edit " + label);
                adi.DocumentationTypeLabel = label;
                adi.DocumentationTypeLabel = adi.AdmissionDocumentation.DocumentationTypeNoDocument
                    ? "Hospice Face to Face Attestation (no document)"
                    : label;
            }
            else if ((adi.AdmissionDocumentation.AdmissionDocumentationConsent != null)
                     && !adi.AdmissionDocumentation.Inactive
                     && (adi.AdmissionDocumentation.AdmissionDocumentationConsent.Any())
                    )
            {
                string label = CodeLookupCache.GetCodeDescriptionFromKey(adi.DocumentationType);
                adi.EncounterViewLabel = string.Format("Edit " + label);
                adi.DocumentationTypeLabel = label;
            }
            else if ((adi.AdmissionDocumentation.AdmissionSignedInterimOrder != null)
                     && !adi.AdmissionDocumentation.Inactive
                     && (adi.AdmissionDocumentation.AdmissionSignedInterimOrder.Any())
                    )
            {
                string label = CodeLookupCache.GetCodeDescriptionFromKey(adi.DocumentationType);
                adi.EncounterViewLabel = string.Format("View " + label);
                adi.DocumentationTypeLabel = label;
            }
            else if ((adi.AdmissionDocumentation.AdmissionSignedPOC != null)
                     && !adi.AdmissionDocumentation.Inactive
                     && (adi.AdmissionDocumentation.AdmissionSignedPOC.Any())
                    )
            {
                string label = CodeLookupCache.GetCodeDescriptionFromKey(adi.DocumentationType);
                adi.EncounterViewLabel = string.Format("View " + label);
                adi.DocumentationTypeLabel = label;
            }
            else if ((adi.AdmissionDocumentation.AdmissionBatchedInterimOrder != null)
                     && !adi.AdmissionDocumentation.Inactive
                     && (adi.AdmissionDocumentation.AdmissionBatchedInterimOrder.Any())
                    )
            {
                string label = CodeLookupCache.GetCodeDescriptionFromKey(adi.DocumentationType);
                adi.EncounterViewLabel = string.Format("View " + label);
                adi.DocumentationTypeLabel = label;
            }
            else if ((adi.AdmissionDocumentation.AdmissionABN != null)
                     && !adi.AdmissionDocumentation.Inactive
                     && (adi.AdmissionDocumentation.AdmissionABN.Any())
                    )
            {
                string label = CodeLookupCache.GetCodeDescriptionFromKey(adi.DocumentationType);
                adi.EncounterViewLabel = string.Format("Edit " + label);
                AdmissionABN aa = adi.AdmissionDocumentation.AdmissionABN.FirstOrDefault();
                adi.DocumentationTypeLabel = ((aa == null) || string.IsNullOrWhiteSpace(aa.ABNTypeDescription))
                    ? label
                    : aa.ABNTypeDescription;
            }
            else if ((adi.AdmissionDocumentation.AdmissionHospiceElectionStatement != null)
                     && !adi.AdmissionDocumentation.Inactive
                     && (adi.AdmissionDocumentation.AdmissionHospiceElectionStatement.Any())
                    )
            {
                string label = CodeLookupCache.GetCodeDescriptionFromKey(adi.DocumentationType);
                adi.EncounterViewLabel = string.Format("Edit " + label);
                adi.DocumentationTypeLabel = label;
                adi.DocumentationTypeLabel = adi.AdmissionDocumentation.DocumentationTypeNoDocument
                    ? label + " (no document)"
                    : label;
            }
            else
            {
                string label = CodeLookupCache.GetCodeDescriptionFromKey(adi.DocumentationType);
                adi.DocumentationTypeLabel = label;
            }
        }

        public string GetAdmissionDocumentationDataTemplate(string DocumentType)
        {
            string admissionDocumentationDataTemplate = "EmptyDataTemplate";

            if (!string.IsNullOrEmpty(DocumentType))
            {
                if (DocumentType.ToLower().Equals("ff") || DocumentType.ToUpper().Equals("NDF2F"))
                {
                    admissionDocumentationDataTemplate = "F2FInfoGridDataTemplate";
                }
                else if (DocumentType.ToLower().Equals("signedorder"))
                {
                    admissionDocumentationDataTemplate = "SignedInterimOrderDataTemplate";
                }
                else if (DocumentType.ToLower().Equals("signedpoc"))
                {
                    admissionDocumentationDataTemplate = "SignedPOCDataTemplate";
                }
                else if (DocumentType.ToLower().Equals("signedbatchedorder"))
                {
                    admissionDocumentationDataTemplate = "BatchedInterimOrderDataTemplate";
                }
                ///    BatchedInterimOrderDataTemplate
                else if (DocumentType.ToLower().Equals("encounter"))
                {
                    admissionDocumentationDataTemplate = "ServiceInfoGridDataTemplate";
                }
                else if (DocumentType.ToLower().Equals("cti"))
                {
                    admissionDocumentationDataTemplate = "COTIInfoDataTemplate";
                }
                else if (DocumentType.ToLower().Equals("hospf2f") || DocumentType.ToLower().Equals("hndf2f"))
                {
                    admissionDocumentationDataTemplate = "HospiceFaceToFaceDataTemplate";
                }
                else if (DocumentType.ToLower().Equals("mhes"))
                {
                    admissionDocumentationDataTemplate = "MHESInfoDataTemplate";
                }
                else if (DocumentType.ToLower().Equals("phi access"))
                {
                    admissionDocumentationDataTemplate = "AdmissionConsentDataTemplate";
                }
                else if (DocumentType.ToLower().Equals("abn"))
                {
                    admissionDocumentationDataTemplate = "AdmissionABNDataTemplate";
                }
            }

            return admissionDocumentationDataTemplate;
        }

        public bool Validate(Entity SubItem, string DocType, AdmissionDocumentation SelectedItem,
            Admission CurrentAdmission, int? ServiceTypeKey)
        {
            bool success = true;

            if (string.IsNullOrEmpty(DocType))
            {
                return success;
            }

            var _docTypeLower = DocType.ToLower();

            __ClearSubItemValidationErrors(SubItem, _docTypeLower);

            success = __ValidateSubItem(SubItem, SelectedItem, CurrentAdmission, ServiceTypeKey, _docTypeLower);

            return success;
        }

        public static string GetDocTypeFromKey(int CodeLookupKey)
        {
            string DocumentType = null;
            var doc_type = CodeLookupCache.GetCodeLookupFromKey(CodeLookupKey);

            if (doc_type != null)
            {
                DocumentType = doc_type.Code;
            }

            return DocumentType;
        }

        public void PrepareForView(string DocumentType, ref Entity SubItem, Admission CurrentAdmission,
            AdmissionDocumentationItem adi)
        {
            if (!string.IsNullOrEmpty(DocumentType))
            {
                if ((DocumentType.ToLower() == "ff") || DocumentType.ToUpper().Equals("NDF2F"))
                {
                    if ((adi != null)
                        && (adi.AdmissionDocumentation != null)
                        && (adi.AdmissionDocumentation.AdmissionFaceToFace != null)
                       )
                    {
                        SubItem = adi.AdmissionDocumentation.AdmissionFaceToFace.FirstOrDefault();
                    }
                }
                else if (DocumentType.ToLower() == "signedorder")
                {
                    if ((adi != null)
                        && (adi.AdmissionDocumentation != null)
                        && (adi.AdmissionDocumentation.AdmissionSignedInterimOrder != null)
                       )
                    {
                        SubItem = adi.AdmissionDocumentation.AdmissionSignedInterimOrder.FirstOrDefault();
                    }
                }
                else if (DocumentType.ToLower() == "signedpoc")
                {
                    if ((adi != null)
                        && (adi.AdmissionDocumentation != null)
                        && (adi.AdmissionDocumentation.AdmissionSignedPOC != null)
                       )
                    {
                        SubItem = adi.AdmissionDocumentation.AdmissionSignedPOC.FirstOrDefault();
                    }
                }
                else if (DocumentType.ToLower() == "signedbatchedorder")
                {
                    if ((adi != null)
                        && (adi.AdmissionDocumentation != null)
                        && (adi.AdmissionDocumentation.AdmissionBatchedInterimOrder != null)
                       )
                    {
                        SubItem = adi.AdmissionDocumentation.AdmissionBatchedInterimOrder.FirstOrDefault();
                    }
                }

                else if ((DocumentType.ToLower() == "hospf2f") || (DocumentType.ToLower() == "cti") ||
                         (DocumentType.ToUpper() == "HNDF2F"))
                {
                    if ((adi != null)
                        && (adi.AdmissionDocumentation != null)
                        && (adi.AdmissionDocumentation.AdmissionCOTI != null)
                       )
                    {
                        SubItem = adi.AdmissionDocumentation.AdmissionCOTI.FirstOrDefault();
                    }
                }
                else if (DocumentType.ToLower() == "encounter")
                {
                    SubItem = adi.Encounter;
                    Encounter CurrentEncounter = adi.Encounter;
                    CurrentEncounter.SetupPatientAddressCollectionView(CurrentAdmission.Patient.PatientAddress);
                    CurrentEncounter
                        .FilterPatientAddressCollectionView(); //refresh collection will access the getter, which will initialize the collectionview...
                }
                else if (DocumentType.ToLower() == "phi access")
                {
                    if ((adi != null)
                        && (adi.AdmissionDocumentation != null)
                        && (adi.AdmissionDocumentation.AdmissionDocumentationConsent != null)
                       )
                    {
                        SubItem = adi.AdmissionDocumentation.AdmissionDocumentationConsent.FirstOrDefault();
                    }
                }
                else if (DocumentType.ToLower() == "abn")
                {
                    if ((adi != null)
                        && (adi.AdmissionDocumentation != null)
                        && (adi.AdmissionDocumentation.AdmissionABN != null)
                       )
                    {
                        SubItem = adi.AdmissionDocumentation.AdmissionABN.FirstOrDefault();
                    }
                }
                else if (DocumentType.ToLower() == "mhes")
                {
                    if ((adi != null)
                        && (adi.AdmissionDocumentation != null)
                        && (adi.AdmissionDocumentation.AdmissionHospiceElectionStatement != null)
                       )
                    {
                        SubItem = adi.AdmissionDocumentation.AdmissionHospiceElectionStatement.FirstOrDefault();
                        RefreshMHES(SubItem as AdmissionHospiceElectionStatement);
                    }
                }
            }
        }

        public void PrepareForSave(string DocumentType, object SubItem, Admission CurrentAdmission,
            AdmissionDocumentation SelectedItem, int? ServiceTypeKey, UserProfile EncounterBy)
        {
            if (!string.IsNullOrEmpty(DocumentType))
            {
                EndEditting(SubItem, SelectedItem);
                if (DocumentType.ToLower().Equals("encounter"))
                {
                    Encounter CurrentEncounter = SubItem as Encounter;
                    if (CurrentEncounter != null)
                    {
                        CurrentEncounter.ServiceTypeKey = ServiceTypeKey;
                        CurrentEncounter.EncounterBy = EncounterBy.UserId;


                        var task = new Task
                        {
                            AdmissionKey = CurrentAdmission.AdmissionKey,
                            PatientKey = CurrentAdmission.PatientKey,
                            ServiceTypeKey = ServiceTypeKey.Value,
                            TaskStartDateTime = CurrentEncounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault(),
                            TaskEndDateTime = CurrentEncounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault(),
                            //TaskDuration = 
                            UserID = CurrentEncounter.EncounterBy.Value,
                        };
                        CurrentEncounter.Task = task;

                        //NOTE: NOT setting FormKey for these types of 'encounters'. Will use absence of Encounter.FormKey to filter these tasks off of the HOME screen
                        if ((SelectedItem.AdmissionDocumentationEncounter != null) &&
                            (SelectedItem.AdmissionDocumentationEncounter.Any() == false))
                        {
                            AdmissionDocumentationEncounter ade = new AdmissionDocumentationEncounter();
                            SelectedItem.AdmissionDocumentationEncounter.Add(ade);
                            CurrentEncounter.AdmissionDocumentationEncounter.Add(ade);
                        }
                    }
                }
                else if (DocumentType.ToLower().Equals("ff") || DocumentType.ToUpper().Equals("NDF2F"))
                {
                    AdmissionFaceToFace CurrentFaceToFace = SubItem as AdmissionFaceToFace;
                    CurrentFaceToFace.AdmissionKey = SelectedItem.AdmissionKey;
                    CurrentFaceToFace.AdmissionDocumentationKey = SelectedItem.AdmissionDocumentationKey;
                    if ((SelectedItem.AdmissionFaceToFace != null) &&
                        (SelectedItem.AdmissionFaceToFace.Contains(CurrentFaceToFace) == false))
                    {
                        SelectedItem.AdmissionFaceToFace.Add(CurrentFaceToFace);
                    }

                    if ((SelectedItem.Inactive == false) && CurrentFaceToFace.AllIsValid)
                    {
                        CurrentAdmission.SetFaceToFaceEncounter(true);
                        if ((SelectedItem != null) && SelectedItem.AdmissionFaceToFace.Any())
                        {
                            CurrentAdmission.FaceToFaceEncounterDate = CurrentFaceToFace.PhysianEncounterDate;
                            CurrentAdmission.FaceToFacePhysicianKey = CurrentFaceToFace.SigningPhysicianKey;
                        }
                    }
                    else
                    {
                        if ((SelectedItem.AdmissionFaceToFace != null) &&
                            (CurrentAdmission.AdmissionFaceToFace.Any(f2f =>
                                ((f2f.AdmissionDocumentation.Inactive == false) & f2f.AllIsValid)) == false))
                        {
                            // SelectedItem is invalid - and we have no valid ones on file
                            CurrentAdmission.SetFaceToFaceEncounter(false);
                            CurrentAdmission.FaceToFaceEncounterDate = null;
                            CurrentAdmission.FaceToFacePhysicianKey = null;
                        }
                    }

                    if (CurrentFaceToFace.SpecificDiscIdentified.HasValue &&
                        !CurrentFaceToFace.SpecificDiscIdentified.Value)
                    {
                        CurrentFaceToFace.Disciplines = null;
                    }

                    if ((CurrentAdmission.HospiceAdmission == false) && (CurrentFaceToFace != null) &&
                        (CurrentFaceToFace.AdmissionDocumentation.Inactive == false) && CurrentFaceToFace.AllIsValid &&
                        (this.CurrentAdmission.OrdersTracking != null) && this.CurrentAdmission.OrdersTracking.Any())
                    {
                        foreach (var item in this.CurrentAdmission.OrdersTracking.Where(a =>
                                         a.OrderType == (int)OrderTypesEnum.FaceToFace ||
                                         a.OrderType == (int)OrderTypesEnum.FaceToFaceEncounter)
                                     .Where(a => a.Inactive == false)
                                     .Where(a => a.PhysicianKey == CurrentFaceToFace.SigningPhysicianKey))
                            // Mark each F2F as signed, there should only be one F2F OrdersTracking row in most cases
                            item.Status = (int)OrdersTrackingStatus.Signed;
                    }
                }
                else if (DocumentType.ToLower().Equals("signedorder"))
                {
                    AdmissionSignedInterimOrder CurrentSignedInterimOrder = SubItem as AdmissionSignedInterimOrder;
                    CurrentSignedInterimOrder.AdmissionKey = SelectedItem.AdmissionKey;
                    CurrentSignedInterimOrder.AdmissionDocumentationKey = SelectedItem.AdmissionDocumentationKey;
                    if (CurrentSignedInterimOrder != null && CurrentSignedInterimOrder.OrderDateNullable.HasValue)
                    {
                        CurrentSignedInterimOrder.OrderDate = CurrentSignedInterimOrder.OrderDateNullable.Value;
                    }

                    if (CurrentSignedInterimOrder != null
                        && CurrentSignedInterimOrder.OrderTimeNullable.HasValue
                        && (CurrentSignedInterimOrder.OrderTime != CurrentSignedInterimOrder.OrderTimeNullable.Value))
                    {
                        CurrentSignedInterimOrder.OrderTime = CurrentSignedInterimOrder.OrderTimeNullable.Value;
                    }

                    if (SelectedItem.AdmissionSignedInterimOrder.Any() == false)
                    {
                        SelectedItem.AdmissionSignedInterimOrder.Add(CurrentSignedInterimOrder);
                    }

                    if ((SelectedItem.AdmissionDocumentationEncounter != null) &&
                        (SelectedItem.AdmissionDocumentationEncounter.Any() == false) &&
                        (CurrentSignedInterimOrder.EncounterKey != null))
                    {
                        AdmissionDocumentationEncounter ade = new AdmissionDocumentationEncounter
                            { EncounterKey = (int)CurrentSignedInterimOrder.EncounterKey };
                        SelectedItem.AdmissionDocumentationEncounter.Add(ade);
                    }
                    else if ((SelectedItem.AdmissionDocumentationEncounter != null) &&
                             SelectedItem.AdmissionDocumentationEncounter.Any() &&
                             (CurrentSignedInterimOrder.EncounterKey != null))
                    {
                        AdmissionDocumentationEncounter ade =
                            SelectedItem.AdmissionDocumentationEncounter.FirstOrDefault();
                        if ((ade != null) && (ade.EncounterKey != (int)CurrentSignedInterimOrder.EncounterKey))
                        {
                            ade.EncounterKey = (int)CurrentSignedInterimOrder.EncounterKey;
                        }
                    }

                    if (CurrentSignedInterimOrder != null)
                    {
                        OrdersTrackingManager otm = new OrdersTrackingManager();
                        otm.SetTrackingRowToSigned(CurrentSignedInterimOrder.CurrentOrderEntry,
                            (int)OrderTypesEnum.InterimOrder,
                            ((CurrentSignedInterimOrder.CurrentOrderEntry == null)
                                ? 0
                                : CurrentSignedInterimOrder.CurrentOrderEntry.OrderEntryKey));
                    }
                }
                else if (DocumentType.ToLower().Equals("signedpoc"))
                {
                    AdmissionSignedPOC CurrentSignedPOC = SubItem as AdmissionSignedPOC;
                    CurrentSignedPOC.AdmissionKey = SelectedItem.AdmissionKey;
                    CurrentSignedPOC.AdmissionDocumentationKey = SelectedItem.AdmissionDocumentationKey;
                    if (CurrentSignedPOC != null
                        && CurrentSignedPOC.CertFromDateNullable.HasValue
                       )
                    {
                        CurrentSignedPOC.CertFromDate = CurrentSignedPOC.CertFromDateNullable.Value;
                    }

                    if (CurrentSignedPOC != null
                        && CurrentSignedPOC.CertThruDateNullable.HasValue
                        && (CurrentSignedPOC.CertThruDate != CurrentSignedPOC.CertThruDateNullable.Value)
                       )
                    {
                        CurrentSignedPOC.CertThruDate = CurrentSignedPOC.CertThruDateNullable.Value;
                    }

                    if (CurrentSignedPOC != null
                        && CurrentSignedPOC.SignatureDateNullable.HasValue
                        && (CurrentSignedPOC.SignatureDate != CurrentSignedPOC.SignatureDateNullable.Value)
                       )
                    {
                        CurrentSignedPOC.SignatureDate = CurrentSignedPOC.SignatureDateNullable.Value;
                    }

                    if (SelectedItem.AdmissionSignedPOC.Any() == false)
                    {
                        SelectedItem.AdmissionSignedPOC.Add(CurrentSignedPOC);
                    }

                    if ((SelectedItem.AdmissionDocumentationEncounter != null) &&
                        (SelectedItem.AdmissionDocumentationEncounter.Any() == false) && (CurrentSignedPOC != null) &&
                        (CurrentSignedPOC.EncounterPlanOfCare != null))
                    {
                        AdmissionDocumentationEncounter ade = new AdmissionDocumentationEncounter
                            { EncounterKey = CurrentSignedPOC.EncounterPlanOfCare.EncounterKey };
                        SelectedItem.AdmissionDocumentationEncounter.Add(ade);
                    }
                    else if ((SelectedItem.AdmissionDocumentationEncounter != null) &&
                             SelectedItem.AdmissionDocumentationEncounter.Any() && (CurrentSignedPOC != null) &&
                             (CurrentSignedPOC.EncounterPlanOfCare != null))
                    {
                        AdmissionDocumentationEncounter ade =
                            SelectedItem.AdmissionDocumentationEncounter.FirstOrDefault();
                        if ((ade != null) && (ade.EncounterKey != CurrentSignedPOC.EncounterPlanOfCare.EncounterKey))
                        {
                            ade.EncounterKey = CurrentSignedPOC.EncounterPlanOfCare.EncounterKey;
                        }
                    }

                    if (CurrentSignedPOC != null)
                    {
                        OrdersTrackingManager otm = new OrdersTrackingManager();
                        otm.SetTrackingRowToSigned(CurrentSignedPOC.EncounterPlanOfCare, (int)OrderTypesEnum.POC,
                            ((CurrentSignedPOC.EncounterPlanOfCare == null)
                                ? 0
                                : CurrentSignedPOC.EncounterPlanOfCare.EncounterPlanOfCareKey),
                            CurrentSignedPOC.SignatureDateNullable);
                    }
                }
                else if (DocumentType.ToLower().Equals("signedbatchedorder"))
                {
                    AdmissionBatchedInterimOrder CurrentBatchedInterimOrder = SubItem as AdmissionBatchedInterimOrder;
                    CurrentBatchedInterimOrder.AdmissionKey = SelectedItem.AdmissionKey;
                    if (CurrentBatchedInterimOrder != null
                        && CurrentBatchedInterimOrder.OrderDateNullable.HasValue
                       )
                    {
                        CurrentBatchedInterimOrder.OrderDate = CurrentBatchedInterimOrder.OrderDateNullable.Value;
                    }

                    CurrentBatchedInterimOrder.AdmissionDocumentationKey = SelectedItem.AdmissionDocumentationKey;
                    SelectedItem.InterimOrderBatchKey = CurrentBatchedInterimOrder.InterimOrderBatchKey;
                    if (SelectedItem.AdmissionBatchedInterimOrder.Any() == false)
                    {
                        SelectedItem.AdmissionBatchedInterimOrder.Add(CurrentBatchedInterimOrder);
                        InterimOrderBatch iob = CurrentAdmission.InterimOrderBatch.Where(p =>
                            p.InterimOrderBatchKey == CurrentBatchedInterimOrder.InterimOrderBatchKey).FirstOrDefault();
                        // This algorithm does not support changing a SignedBatchedOrder from one batch to another - assumes one-time event
                        if ((iob != null) && (iob.InterimOrderBatchDetail != null))
                        {
                            OrdersTrackingManager otm = new OrdersTrackingManager();
                            foreach (InterimOrderBatchDetail iobd in iob.InterimOrderBatchDetail)
                                if ((iobd.OrderEntry != null) && (iobd.OrderEntry.AddedFromEncounterKey != null))
                                {
                                    AdmissionDocumentationEncounter ade = new AdmissionDocumentationEncounter
                                        { EncounterKey = (int)iobd.OrderEntry.AddedFromEncounterKey };
                                    SelectedItem.AdmissionDocumentationEncounter.Add(ade);
                                    otm.SetTrackingRowToSigned(iobd.OrderEntry, (int)OrderTypesEnum.InterimOrder,
                                        iobd.OrderEntry.OrderEntryKey);
                                }
                        }
                    }
                }
                else if (DocumentType.ToLower().Equals("hospf2f") || DocumentType.ToLower().Equals("cti") ||
                         (DocumentType.ToUpper() == "HNDF2F"))
                {
                    AdmissionCOTI CurrentAdmissionCOTI = SubItem as AdmissionCOTI;
                    CurrentAdmissionCOTI.AdmissionKey = SelectedItem.AdmissionKey;
                    CurrentAdmissionCOTI.AdmissionDocumentationKey = SelectedItem.AdmissionDocumentationKey;
                    if (SelectedItem.AdmissionCOTI.Any() == false)
                    {
                        SelectedItem.AdmissionCOTI.Add(CurrentAdmissionCOTI);
                    }

                    if (DocumentType.ToLower().Equals("cti") && (CurrentAdmissionCOTI != null) &&
                        CurrentAdmissionCOTI.IsCOTI && (CurrentAdmissionCOTI.VerbalCOTIEncounterKey != null))
                    {
                        if ((SelectedItem.AdmissionDocumentationEncounter != null) &&
                            (SelectedItem.AdmissionDocumentationEncounter.Any() == false) &&
                            (CurrentAdmissionCOTI.AddedFromEncounterKey != null))
                        {
                            AdmissionDocumentationEncounter ade = new AdmissionDocumentationEncounter
                                { EncounterKey = (int)CurrentAdmissionCOTI.AddedFromEncounterKey };
                            SelectedItem.AdmissionDocumentationEncounter.Add(ade);
                        }
                        else if ((SelectedItem.AdmissionDocumentationEncounter != null) &&
                                 SelectedItem.AdmissionDocumentationEncounter.Any() &&
                                 (CurrentAdmissionCOTI.AddedFromEncounterKey != null))
                        {
                            AdmissionDocumentationEncounter ade =
                                SelectedItem.AdmissionDocumentationEncounter.FirstOrDefault();
                            if ((ade != null) && (ade.EncounterKey != (int)CurrentAdmissionCOTI.AddedFromEncounterKey))
                            {
                                ade.EncounterKey = (int)CurrentAdmissionCOTI.AddedFromEncounterKey;
                            }
                        }
                    }

                    if (DocumentType.ToLower().Equals("cti"))
                    {
                        OrdersTrackingManager otm = new OrdersTrackingManager();
                        otm.SetTrackingRowToSigned(CurrentAdmissionCOTI, (int)OrderTypesEnum.CoTI,
                            CurrentAdmissionCOTI.AdmissionCOTIKey);
                        if ((CurrentAdmissionCOTI != null) && (CurrentAdmissionCOTI.Encounter != null) &&
                            CurrentAdmissionCOTI.IsCOTI)
                        {
                            CurrentAdmissionCOTI.Encounter.Signed =
                                (CurrentAdmissionCOTI.SignatureDate == null) ? false : true;
                        }
                    }
                }
                else if (DocumentType.ToLower() == "phi access")
                {
                    AdmissionDocumentationConsent CurrentAdmissionDocumentationConsent =
                        SubItem as AdmissionDocumentationConsent;
                    CurrentAdmissionDocumentationConsent.AdmissionKey = SelectedItem.AdmissionKey;
                    CurrentAdmissionDocumentationConsent.AdmissionDocumentationKey =
                        SelectedItem.AdmissionDocumentationKey;

                    if (SelectedItem.AdmissionDocumentationConsent.Any() == false)
                    {
                        SelectedItem.AdmissionDocumentationConsent.Add(CurrentAdmissionDocumentationConsent);
                    }

                    // see if we should create an AdmissionConsent row
                    string name = null;

                    if (!string.IsNullOrEmpty(CurrentAdmissionDocumentationConsent.RequestorFirstName)
                        || !string.IsNullOrEmpty(CurrentAdmissionDocumentationConsent.RequestorLastName)
                       )
                    {
                        name = CurrentAdmissionDocumentationConsent.RequestorFirstName + " " +
                               CurrentAdmissionDocumentationConsent.RequestorLastName;
                    }

                    if (string.IsNullOrEmpty(name)
                       )
                    {
                        if (!string.IsNullOrEmpty(CurrentAdmissionDocumentationConsent.GranteeFirstName)
                            || !string.IsNullOrEmpty(CurrentAdmissionDocumentationConsent.GranteeLastName)
                           )
                        {
                            name = CurrentAdmissionDocumentationConsent.GranteeFirstName + " " +
                                   CurrentAdmissionDocumentationConsent.GranteeLastName;
                        }
                    }

                    AdmissionConsent ac = null;

                    if (SelectedItem.AdmissionDocumentationConsent.Any() == false)
                    {
                        SelectedItem.AdmissionDocumentationConsent.Add(CurrentAdmissionDocumentationConsent);
                    }

                    //if (!string.IsNullOrEmpty(name))
                    {
                        if (CurrentAdmissionDocumentationConsent.AdmissionConsent == null)
                        {
                            ac = new AdmissionConsent();
                            CurrentAdmissionDocumentationConsent.AdmissionConsent = ac;
                        }
                        else
                        {
                            ac = CurrentAdmissionDocumentationConsent.AdmissionConsent;
                        }

                        if (ac != null)
                        {
                            ac.AdmissionKey = CurrentAdmission.AdmissionKey;
                            ac.Requestor = name;
                            ac.DecisionKey = CurrentAdmissionDocumentationConsent.DecisionKey;
                            ac.DecisionMakerLName = CurrentAdmissionDocumentationConsent.DecisionMakerLName;
                            ac.DecisionMakerFName = CurrentAdmissionDocumentationConsent.DecisionMakerFName;
                            ac.DecisionDate = CurrentAdmissionDocumentationConsent.DecisionDate;
                            ac.Inactive = SelectedItem.Inactive;
                        }
                    }
                    //else
                    //{
                    //    if (CurrentAdmissionDocumentationConsent.AdmissionConsent != null)
                    //    {
                    //        CurrentAdmissionDocumentationConsent.AdmissionConsent.Inactive = true;
                    //    }
                    //}
                }
                else if (DocumentType.ToLower() == "abn")
                {
                    AdmissionABN CurrentABN = SubItem as AdmissionABN;
                    CurrentABN.AdmissionKey = SelectedItem.AdmissionKey;
                    CurrentABN.AdmissionDocumentationKey = SelectedItem.AdmissionDocumentationKey;
                    if (SelectedItem.AdmissionABN.Any() == false)
                    {
                        SelectedItem.AdmissionABN.Add(CurrentABN);
                    }
                }
                else if (DocumentType.ToLower().Equals("mhes"))
                {
                    AdmissionHospiceElectionStatement ahes = SubItem as AdmissionHospiceElectionStatement;
                    ahes.AdmissionKey = SelectedItem.AdmissionKey;
                    ahes.AdmissionDocumentationKey = SelectedItem.AdmissionDocumentationKey;
                    if (SelectedItem.AdmissionHospiceElectionStatement.Any() == false)
                    {
                        SelectedItem.AdmissionHospiceElectionStatement.Add(ahes);
                    }
                }
            }
        }

        // this is the function that will be called if the AdmissionDocumentation row that is being edited was Inactive when the edit button was pressed
        public void PrepareForSaveInactive(string DocumentType, object SubItem, Admission CurrentAdmission,
            AdmissionDocumentation SelectedItem)
        {
            if (DocumentType.ToLower() == "phi access")
            {
                AdmissionDocumentationConsent adc = SubItem as AdmissionDocumentationConsent;

                if (adc != null)
                {
                    if (adc.AdmissionConsent != null)
                    {
                        string name = null;

                        if (!string.IsNullOrEmpty(adc.RequestorFirstName)
                            || !string.IsNullOrEmpty(adc.RequestorLastName)
                           )
                        {
                            name = adc.RequestorFirstName + " " + adc.RequestorLastName;
                        }

                        if (string.IsNullOrEmpty(name)
                           )
                        {
                            if (!string.IsNullOrEmpty(adc.GranteeFirstName)
                                || !string.IsNullOrEmpty(adc.GranteeLastName)
                               )
                            {
                                name = adc.GranteeFirstName + " " + adc.GranteeLastName;
                            }
                        }

                        if (!string.IsNullOrEmpty(name))
                        {
                            adc.AdmissionConsent.Inactive = SelectedItem.Inactive;
                        }
                        else
                        {
                            adc.AdmissionConsent.Inactive = true;
                        }
                    }
                }
            }
        }

        public bool SetProtected(object CurrentSubItem, UserProfile EncounterBy, int? ServiceTypeKey,
            AdmissionDocumentationItem adi)
        {
            bool Protected = true;

            Encounter CurrentEncounter = CurrentSubItem as Encounter;
            if (CurrentEncounter != null)
            {
                EncounterBy = UserCache.Current.GetUserProfileFromUserId(adi.Encounter.EncounterBy);
                ServiceTypeKey = adi.Encounter.ServiceTypeKey; //MUST set this after setting EncounterBy
            }
            else if ((adi.AdmissionDocumentation.AdmissionFaceToFace != null)
                     && adi.AdmissionDocumentation.AdmissionFaceToFace.Any()
                    )
            {
                Protected = false;
            }
            else if ((adi.AdmissionDocumentation.AdmissionCOTI != null)
                     && adi.AdmissionDocumentation.AdmissionCOTI.Any()
                    )
            {
                Protected = false;
            }
            //else if ((adi.AdmissionDocumentation.AdmissionSignedInterimOrder != null)
            //        && adi.AdmissionDocumentation.AdmissionSignedInterimOrder.Any()
            //       )
            //{
            //    Protected = false;
            //}
            //else if ((adi.AdmissionDocumentation.AdmissionBatchedInterimOrder != null)
            //        && adi.AdmissionDocumentation.AdmissionBatchedInterimOrder.Any()
            //       )
            //{
            //    Protected = false;
            //}
            else if ((adi.AdmissionDocumentation.AdmissionDocumentationConsent != null)
                     && adi.AdmissionDocumentation.AdmissionDocumentationConsent.Any()
                    )
            {
                adi.AdmissionDocumentation.AdmissionDocumentationConsent.ForEach(adc => adc.BeginEditting());
                Protected = false;
            }
            else if ((adi.AdmissionDocumentation.AdmissionABN != null)
                     && adi.AdmissionDocumentation.AdmissionABN.Any()
                    )
            {
                Protected = false;
            }
            else if ((adi.AdmissionDocumentation.AdmissionHospiceElectionStatement != null)
                     && adi.AdmissionDocumentation.AdmissionHospiceElectionStatement.Any()
                    )
            {
                Protected = false;
            }

            return Protected;
        }

        public void BeginEditting(object CurrentSubItem, AdmissionDocumentationItem adi)
        {
            Encounter CurrentEncounter = CurrentSubItem as Encounter;
            if (CurrentEncounter != null)
            {
                return;
            }

            if ((adi.AdmissionDocumentation.AdmissionFaceToFace != null)
                && adi.AdmissionDocumentation.AdmissionFaceToFace.Any()
               )
            {
                adi.AdmissionDocumentation.AdmissionFaceToFace.ForEach(f => f.BeginEditting());
            }
            else if ((adi.AdmissionDocumentation.AdmissionCOTI != null)
                     && adi.AdmissionDocumentation.AdmissionCOTI.Any()
                    )
            {
                adi.AdmissionDocumentation.AdmissionCOTI.ForEach(f => f.BeginEditting());
            }
            else if ((adi.AdmissionDocumentation.AdmissionSignedInterimOrder != null)
                     && adi.AdmissionDocumentation.AdmissionSignedInterimOrder.Any()
                    )
            {
                adi.AdmissionDocumentation.AdmissionSignedInterimOrder.ForEach(f => f.BeginEditting());
            }
            else if ((adi.AdmissionDocumentation.AdmissionSignedPOC != null)
                     && adi.AdmissionDocumentation.AdmissionSignedPOC.Any()
                    )
            {
                adi.AdmissionDocumentation.AdmissionSignedPOC.ForEach(f => f.BeginEditting());
            }
            else if ((adi.AdmissionDocumentation.AdmissionBatchedInterimOrder != null)
                     && adi.AdmissionDocumentation.AdmissionBatchedInterimOrder.Any()
                    )
            {
                adi.AdmissionDocumentation.AdmissionBatchedInterimOrder.ForEach(f => f.BeginEditting());
            }
            else if ((adi.AdmissionDocumentation.AdmissionDocumentationConsent != null)
                     && adi.AdmissionDocumentation.AdmissionDocumentationConsent.Any()
                    )
            {
                adi.AdmissionDocumentation.AdmissionDocumentationConsent.ForEach(f => f.BeginEditting());
            }
            else if ((adi.AdmissionDocumentation.AdmissionABN != null)
                     && adi.AdmissionDocumentation.AdmissionABN.Any()
                    )
            {
                adi.AdmissionDocumentation.AdmissionABN.ForEach(f => f.BeginEditting());
            }
            else if ((adi.AdmissionDocumentation.AdmissionHospiceElectionStatement != null)
                     && adi.AdmissionDocumentation.AdmissionHospiceElectionStatement.Any()
                    )
            {
                adi.AdmissionDocumentation.AdmissionHospiceElectionStatement.ForEach(f => f.BeginEditting());
            }
        }

        public void EndEditting(object CurrentSubItem, AdmissionDocumentation adi)
        {
            Encounter CurrentEncounter = CurrentSubItem as Encounter;
            if (CurrentEncounter != null)
            {
                return;
            }

            if ((adi.AdmissionFaceToFace != null)
                && adi.AdmissionFaceToFace.Any()
               )
            {
                adi.AdmissionFaceToFace.ForEach(f => f.EndEditting());
            }
            else if ((adi.AdmissionCOTI != null)
                     && adi.AdmissionCOTI.Any()
                    )
            {
                adi.AdmissionCOTI.ForEach(f => f.EndEditting());
            }
            else if ((adi.AdmissionDocumentationConsent != null)
                     && adi.AdmissionDocumentationConsent.Any()
                    )
            {
                adi.AdmissionDocumentationConsent.ForEach(f => f.EndEditting());
            }
            else if ((adi.AdmissionSignedInterimOrder != null)
                     && adi.AdmissionSignedInterimOrder.Any()
                    )
            {
                adi.AdmissionSignedInterimOrder.ForEach(f => f.EndEditting());
            }
            else if ((adi.AdmissionSignedPOC != null)
                     && adi.AdmissionSignedPOC.Any()
                    )
            {
                adi.AdmissionSignedPOC.ForEach(f => f.EndEditting());
            }
            else if ((adi.AdmissionBatchedInterimOrder != null)
                     && adi.AdmissionBatchedInterimOrder.Any()
                    )
            {
                adi.AdmissionBatchedInterimOrder.ForEach(f => f.EndEditting());
            }
            else if ((adi.AdmissionABN != null)
                     && adi.AdmissionABN.Any()
                    )
            {
                adi.AdmissionABN.ForEach(f => f.EndEditting());
            }
            else if ((adi.AdmissionHospiceElectionStatement != null)
                     && adi.AdmissionHospiceElectionStatement.Any()
                    )
            {
                adi.AdmissionHospiceElectionStatement.ForEach(f => f.EndEditting());
            }
            else if ((adi.AdmissionHospiceElectionStatement != null)
                     && adi.AdmissionHospiceElectionStatement.Any()
                    )
            {
                adi.AdmissionHospiceElectionStatement.ForEach(f => f.EndEditting());
            }
        }

        public void CancelEditting(object CurrentSubItem, AdmissionDocumentation adi)
        {
            Encounter CurrentEncounter = CurrentSubItem as Encounter;
            if (CurrentEncounter != null)
            {
                return;
            }

            if ((adi.AdmissionFaceToFace != null)
                && adi.AdmissionFaceToFace.Any()
               )
            {
                adi.AdmissionFaceToFace.ForEach(f => f.CancelEditting());
            }
            else if ((adi.AdmissionCOTI != null)
                     && adi.AdmissionCOTI.Any()
                    )
            {
                adi.AdmissionCOTI.ForEach(f => f.CancelEditting());
            }
            else if ((adi.AdmissionSignedInterimOrder != null)
                     && adi.AdmissionSignedInterimOrder.Any()
                    )
            {
                adi.AdmissionSignedInterimOrder.ForEach(f => f.CancelEditting());
            }
            else if ((adi.AdmissionSignedPOC != null)
                     && adi.AdmissionSignedPOC.Any()
                    )
            {
                adi.AdmissionSignedPOC.ForEach(f => f.CancelEditting());
            }
            else if ((adi.AdmissionBatchedInterimOrder != null)
                     && adi.AdmissionBatchedInterimOrder.Any()
                    )
            {
                adi.AdmissionBatchedInterimOrder.ForEach(f => f.CancelEditting());
            }
            else if ((adi.AdmissionDocumentationConsent != null)
                     && adi.AdmissionDocumentationConsent.Any()
                    )
            {
                adi.AdmissionDocumentationConsent.ForEach(f =>
                {
                    // dummy edit to force 'HasChanges' to be set when only editing one field and going immediately to the 'X'.
                    f.GranteeLastName = f.GranteeLastName + ".";
                    f.CancelEditting();
                });
            }
            else if ((adi.AdmissionABN != null)
                     && adi.AdmissionABN.Any()
                    )
            {
                adi.AdmissionABN.ForEach(f => f.CancelEditting());
            }
            else if ((adi.AdmissionHospiceElectionStatement != null)
                     && adi.AdmissionHospiceElectionStatement.Any()
                    )
            {
                adi.AdmissionHospiceElectionStatement.ForEach(f => f.CancelEditting());
            }
        }

        public static bool GetCanInactivate(bool IsDocument, int? EncounterKey, int? AdmissionDocumentationEncounterKey,
            Guid CreatedBy,
            AdmissionDocumentation AdmissionDocumentation, Encounter Encounter)
        {
            if (IsDocument)
            {
                if (AdmissionDocumentation == null && Encounter == null)
                {
                    return false;
                }

                if ((EncounterKey.GetValueOrDefault() > 0) ||
                    (AdmissionDocumentationEncounterKey.GetValueOrDefault() > 0))
                {
                    return false;
                }

                //US2583/DEInactive documents will only display for the user who created/attached or the SYS ADM role.
                var isOwner = CreatedBy.ToString().Equals(WebContext.Current.User.MemberID.ToString());
                var isInRole = RoleAccessHelper.CheckPermission("Admin");
                bool isActiveFaceToFace = ((AdmissionDocumentation != null)
                                           && (!AdmissionDocumentation.Inactive)
                                           && (AdmissionDocumentation.AdmissionFaceToFace != null)
                                           && AdmissionDocumentation.AdmissionFaceToFace.Any()
                    )
                        ? true
                        : false;
                bool isSignedPOC = ((AdmissionDocumentation != null)
                                    && (!AdmissionDocumentation.Inactive)
                                    && (AdmissionDocumentation.AdmissionSignedPOC != null)
                                    && AdmissionDocumentation.AdmissionSignedPOC.Any()
                    )
                        ? true
                        : false;
                bool isCOTI = ((AdmissionDocumentation != null)
                               && (!AdmissionDocumentation.Inactive)
                               && (AdmissionDocumentation.AdmissionCOTI != null)
                               && AdmissionDocumentation.AdmissionCOTI.Any()
                    )
                        ? true
                        : false;
                bool isSignedOrder = ((AdmissionDocumentation != null)
                                      && (!AdmissionDocumentation.Inactive)
                                      && (AdmissionDocumentation.AdmissionSignedInterimOrder != null)
                                      && AdmissionDocumentation.AdmissionSignedInterimOrder.Any()
                    )
                        ? true
                        : false;
                bool isConsent = ((AdmissionDocumentation != null)
                                  && (!AdmissionDocumentation.Inactive)
                                  && (AdmissionDocumentation.AdmissionDocumentationConsent != null)
                                  && AdmissionDocumentation.AdmissionDocumentationConsent.Any()
                    )
                        ? true
                        : false;
                bool isABN = ((AdmissionDocumentation != null)
                              && (!AdmissionDocumentation.Inactive)
                              && (AdmissionDocumentation.AdmissionABN != null)
                              && AdmissionDocumentation.AdmissionABN.Any()
                    )
                        ? true
                        : false;
                bool isMHES = false; // Allow inactivation of MHES
                //((AdmissionDocumentation != null)
                //                            && (!AdmissionDocumentation.Inactive)
                //                            && (AdmissionDocumentation.AdmissionHospiceElectionStatement != null)
                //                            && (AdmissionDocumentation.AdmissionHospiceElectionStatement.Any() == true)
                //                            ) ? true : false;
                bool isSignedBatchOrder = ((AdmissionDocumentation != null)
                                           && (!AdmissionDocumentation.Inactive)
                                           && (AdmissionDocumentation.AdmissionBatchedInterimOrder != null)
                                           && AdmissionDocumentation.AdmissionBatchedInterimOrder.Any()
                    )
                        ? true
                        : false;
                return !isCOTI && !isConsent && !isActiveFaceToFace && !isABN && !isMHES && !isSignedBatchOrder &&
                       !isSignedOrder && !isSignedPOC && (isOwner || isInRole)
                    ? true
                    : false;
            }

            return (Encounter == null) ? false : Encounter.CanInactivate;
        }

        public static object GetSubItemForAdmissionDocumentation(AdmissionDocumentation AdmissionDocumentation)
        {
            object o = null;

            if (AdmissionDocumentation != null)
            {
                string code = CodeLookupCache.GetCodeFromKey(AdmissionDocumentation.DocumentationType);

                if (!string.IsNullOrEmpty(code))
                {
                    if (code.ToLower() == "encounter")
                    {
                        o = AdmissionDocumentation.Encounter;
                    }
                    else if ((code.ToLower() == "ff") || code.ToUpper().Equals("NDF2F"))
                    {
                        if (AdmissionDocumentation.AdmissionFaceToFace != null)
                        {
                            o = AdmissionDocumentation.AdmissionFaceToFace.FirstOrDefault();
                        }
                    }
                    else if (code.ToLower() == "signedorder")
                    {
                        if (AdmissionDocumentation.AdmissionSignedInterimOrder != null)
                        {
                            o = AdmissionDocumentation.AdmissionSignedInterimOrder.FirstOrDefault();
                        }
                    }
                    else if (code.ToLower() == "signedpoc")
                    {
                        if (AdmissionDocumentation.AdmissionSignedPOC != null)
                        {
                            o = AdmissionDocumentation.AdmissionSignedPOC.FirstOrDefault();
                        }
                    }
                    else if (code.ToLower() == "signedbatchedorder")
                    {
                        if (AdmissionDocumentation.AdmissionBatchedInterimOrder != null)
                        {
                            o = AdmissionDocumentation.AdmissionBatchedInterimOrder.FirstOrDefault();
                        }
                    }
                    else if ((code.ToLower() == "cti") || (code.ToLower() == "hospf2f") || (code.ToUpper() == "HNDF2F"))
                    {
                        if (AdmissionDocumentation.Encounter != null)
                        {
                            o = AdmissionDocumentation.AdmissionCOTI.FirstOrDefault();
                        }
                    }
                    else if ((code.ToLower() == "phi access")
                            )
                    {
                        if (AdmissionDocumentation.Encounter != null)
                        {
                            o = AdmissionDocumentation.AdmissionCOTI.FirstOrDefault();
                        }
                    }
                    else if ((code.ToLower() == "abn")
                            )
                    {
                        if (AdmissionDocumentation.Encounter != null)
                        {
                            o = AdmissionDocumentation.AdmissionABN.FirstOrDefault();
                        }
                    }
                    else if ((code.ToLower() == "mhes")
                            )
                    {
                        if (AdmissionDocumentation.AdmissionHospiceElectionStatement != null)
                        {
                            o = AdmissionDocumentation.AdmissionHospiceElectionStatement.FirstOrDefault();
                        }
                    }
                }
            }

            return o;
        }

        private CollectionViewSource filteredInsuranceCollectionABN = new CollectionViewSource();
        public ICollectionView FilteredInsuranceCollectionABN => filteredInsuranceCollectionABN.View;
        private CollectionViewSource filteredInsuranceCollectionMHES = new CollectionViewSource();
        public ICollectionView FilteredInsuranceCollectionMHES => filteredInsuranceCollectionMHES.View;
        private CollectionViewSource filteredAttendingAdmissionPhysicianCollectionMHES = new CollectionViewSource();

        public ICollectionView FilteredAttendingAdmissionPhysicianCollectionMHES =>
            filteredAttendingAdmissionPhysicianCollectionMHES.View;

        private CollectionViewSource availableTypes = new CollectionViewSource();
        public ICollectionView AvailableTypes => availableTypes.View;

        public Admission CurrentAdmission
        {
            get { return currentAdmission; }
            set
            {
                currentAdmission = value;
                RefreshFilteredInsuranceCollectionABN();
                RefreshMHES(null);
                RaisePropertyChanged("CurrentAdmission");
            }
        }

        private void RefreshFilteredInsuranceCollectionABN()
        {
            if ((CurrentAdmission == null) || (CurrentAdmission.AdmissionCoverage == null))
            {
                filteredInsuranceCollectionABN.Source = new List<Insurance>();
            }
            else
            {
                filteredInsuranceCollectionABN.Source
                    = InsuranceCache.GetActiveInsurances()
                        .Where(i => CurrentAdmission.AdmissionCoverage
                                        .Any(ac => (ac.AdmissionCoverageInsurance != null)
                                                   && (ac.AdmissionCoverageInsurance
                                                       .Any(aci => (aci.PatientInsurance != null)
                                                                   && (aci.PatientInsurance.InsuranceKey ==
                                                                       i.InsuranceKey)
                                                                   && (!aci.Inactive)
                                                       )
                                                   )
                                        )
                                    || ((currentAdmission != null)
                                        && (currentAdmission.AdmissionABN != null)
                                        && (currentAdmission.AdmissionABN.Any(aa => aa.InsuranceKey == i.InsuranceKey))
                                    )
                        );
            }

            RaisePropertyChanged("FilteredInsuranceCollectionABN");
        }

        private void RefreshMHES(AdmissionHospiceElectionStatement ahes)
        {
            if (ahes == null)
            {
                return;
            }

            RefreshFilteredInsuranceCollectionMHES(ahes.InsuranceKey);
            RefreshFilteredAttendingAdmissionPhysicianCollectionMHES(ahes.AttendingAdmissionPhysicianKey);
            ahes.RaiseAllPropertyChanged();
        }

        private void RefreshFilteredInsuranceCollectionMHES(int? myInsuranceKey)
        {
            if ((CurrentAdmission == null) || (CurrentAdmission.AdmissionCoverage == null))
            {
                filteredInsuranceCollectionMHES.Source = new List<Insurance>();
            }
            else
            {
                filteredInsuranceCollectionMHES.Source
                    = InsuranceCache.GetActiveInsurances()
                        .Where(i => CurrentAdmission.AdmissionCoverage
                                        .Any(ac => (ac.AdmissionCoverageInsurance != null)
                                                   && (ac.AdmissionCoverageInsurance
                                                       .Any(aci => (aci.PatientInsurance != null)
                                                                   && (aci.PatientInsurance.InsuranceKey ==
                                                                       i.InsuranceKey)
                                                                   && (!aci.Inactive)
                                                       )
                                                   )
                                        )
                                    || (myInsuranceKey == i.InsuranceKey)
                        );
            }

            RaisePropertyChanged("FilteredInsuranceCollectionMHES");
        }

        private void RefreshFilteredAttendingAdmissionPhysicianCollectionMHES(int? myAttendingAdmissionPhysicianKey)
        {
            if ((CurrentAdmission == null) || (CurrentAdmission.AdmissionPhysician == null))
            {
                filteredAttendingAdmissionPhysicianCollectionMHES.Source = new List<AdmissionPhysician>();
            }
            else
            {
                filteredAttendingAdmissionPhysicianCollectionMHES.Source = CurrentAdmission.AdmissionPhysician
                    .Where(p => (
                            (
                                (p.Inactive == false) &&
                                (p.PhysicianEffectiveFromDate.Date <= DateTime.Today.Date) &&
                                ((p.PhysicianEffectiveThruDate.HasValue == false) ||
                                 (p.PhysicianEffectiveThruDate.HasValue &&
                                  (p.PhysicianEffectiveThruDate.Value.Date >= DateTime.Today.Date)))
                            )
                            ||
                            (myAttendingAdmissionPhysicianKey == p.AdmissionPhysicianKey)
                        )
                    );
            }

            RaisePropertyChanged("FilteredAttendingAdmissionPhysicianCollectionMHES");
        }

        private Admission currentAdmission;

        private void RaisePropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}