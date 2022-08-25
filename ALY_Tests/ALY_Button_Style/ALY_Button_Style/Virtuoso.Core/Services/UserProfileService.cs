#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;
using Virtuoso.Services;
using Virtuoso.Services.Authentication;
using Virtuoso.Services.Web;
using Virtuoso.Validation;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IUserProfileService))]
    public class UserProfileService : PagedModelBase, IUserProfileService
    {
        VirtuosoApplicationConfiguration Configuration { get; set; }
        public VirtuosoDomainContext Context { get; set; }
        string NewPassword;

        [ImportingConstructor]
        public UserProfileService(VirtuosoApplicationConfiguration config, IUriService _uriService)
        {
            Configuration = config;

            if (_uriService != null)
            {
                Context = new VirtuosoDomainContext(new Uri(_uriService.Uri, "Virtuoso-Services-Web-VirtuosoDomainService.svc")); //using alternate constructor, so that it can run in a thread
            }
            else
            {
                Context = new VirtuosoDomainContext();
            }

            var contextServiceProvider = new SimpleServiceProvider();
            contextServiceProvider.AddService<IPhysicianDataProvider>(new PhysicianDataProvider());

            Context.ValidationContext = new ValidationContext(this, contextServiceProvider, null);

            UserProfiles = new PagedEntityCollectionView<UserProfile>(Context.UserProfiles, this);
            Context.PropertyChanged += Context_PropertyChanged;
        }

        #region PagedModelBase Members

        public override void LoadData()
        {
            if (IsLoading || Context == null)
            {
                return;
            }

            IsLoading = true;

            GetAsync();
        }

        #endregion

        #region IModelDataService<UserProfile> Members

        public void Add(UserProfile entity)
        {
            Context.UserProfiles.Add(entity);
        }

        public void Remove(UserProfile entity)
        {
            Context.UserProfiles.Remove(entity);
        }

        public void Remove(UserProfileInRole entity)
        {
            Context.UserProfileInRoles.Remove(entity);
        }

        public void Remove(UserProfileAdmission entity)
        {
            Context.UserProfileAdmissions.Remove(entity);
        }

        public void Remove(DisciplineInUserProfile entity)
        {
            Context.DisciplineInUserProfiles.Remove(entity);
        }

        public void Remove(AttributeInUserProfile entity)
        {
            Context.AttributeInUserProfiles.Remove(entity);
        }

        public void Remove(AlertsInUserProfile entity)
        {
            Context.AlertsInUserProfiles.Remove(entity);
        }

        public void Remove(UserProfilePhone entity)
        {
            Context.UserProfilePhones.Remove(entity);
        }

        public void Remove(UserProfileProductivity entity)
        {
            Context.UserProfileProductivities.Remove(entity);
        }

        public void Remove(UserProfileAlternateID entity)
        {
            Context.UserProfileAlternateIDs.Remove(entity);
        }

        public void Remove(UserProfileGroup entity)
        {
            Context.UserProfileGroups.Remove(entity);
        }

        public void Remove(UserProfileServiceLine entity)
        {
            Context.UserProfileServiceLines.Remove(entity);
        }

        public void Clear()
        {
            Context.RejectChanges();
            Context.EntityContainer.Clear();
        }

        public void GenerateUserMobileInvite(Guid UserID)
        {
            Guid createdByUser = UserCache.Current.GetCurrentUserProfile().UserId;
            Dispatcher.BeginInvoke(() =>
            {
                var query = Context.GenerateUserMobileInviteQuery(UserID, createdByUser);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    PostUserInviteSent,
                    null);
            });
        }

        private void PostUserInviteSent(LoadOperation<UserProfile> results)
        {
            IsLoading = false;

            foreach (UserProfile p in results.Entities.ToList()) p.TriggerUserMobileChanges();
        }

        public void GetSearchAsync(bool isSystemSearch)
        {
            //Bug 13160: Integrated System Search for DDL Smart Combo includes inactives when filters selected
            //          when isSystemSearch == false, then Inactive checkbox removed from search criteria; however 
            //          we want to always assume that it is checked - e.g. add Inactive==false to query.

            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.UserProfiles.Clear();

                var query = Context.GetUserProfileQuery();

                if (SearchParameters.Count > 0)
                {
                    foreach (var item in SearchParameters)
                    {
                        string searchvalue = item.Value;

                        switch (item.Field)
                        {
                            case "id":
                            case "Key":
                            case "UserId":
                                Guid g = new Guid();
                                if (searchvalue.Equals("0") == false)
                                {
                                    g = new Guid(searchvalue);
                                }

                                query = query.Where(p => p.UserId == g);
                                break;
                            case "UserName":
                                query = query.Where(p => p.UserName.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "LastName":
                                query = query.Where(p => p.LastName.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "FirstName":
                                query = query.Where(p => p.FirstName.ToLower().Contains(searchvalue.ToLower()));
                                break;
                            case "Inactive":
                                bool inactive = Convert.ToBoolean(searchvalue);
                                if (!inactive)
                                {
                                    query = query.Where(p => p.Inactive == inactive);
                                }
                                else
                                {
                                    query = query.Where(p => p.DeltaAdmin == false);
                                }

                                break;
                        }
                    }

                    if (isSystemSearch == false)
                    {
                        //Bug 13160: Integrated System Search for DDL Smart Combo includes inactives when filters selected
                        query = query.Where(p => p.Inactive == false);
                    }
                }
                else
                {
                    query = query.Where(p => p.UserId == UserCache.Current.GetCurrentUserProfile().UserId);
                }

                //Return DeltaAdmin or DeltaUser credentials only if...
                //1. Primary Key is part of the search or...
                //2. user has a DeltaAdmin or DeltaUser credential type
                bool returnDeltaCredentials = false;
                if (SearchParameters.Any(p => p.Field == "id" || p.Field == "Key" || p.Field == "UserId"))
                {
                    returnDeltaCredentials = true;
                }

                if (WebContext.Current.User.DeltaAdmin || WebContext.Current.User.DeltaUser)
                {
                    returnDeltaCredentials = true;
                }

                if (!returnDeltaCredentials)
                {
                    query = query.Where(p => p.DeltaAdmin == false && p.DeltaUser == false);
                }

                query.IncludeTotalCount = false;


                IsLoading = true;

                Context.Load(query, LoadBehavior.RefreshCurrent, g => HandleEntityResults(g, OnLoaded), null);
            });
        }

        public void GetAsync()
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.RejectChanges();
                Context.UserProfiles.Clear();

                if (SearchParameters.Count == 1 && SearchParameters.First().Field
                        .Equals("id", StringComparison.InvariantCultureIgnoreCase))
                {
                    var item = SearchParameters.First();
                    string searchvalue = item.Value;

                    Guid g = new Guid();
                    if (searchvalue.Equals("0") == false)
                    {
                        g = new Guid(searchvalue);
                    }

                    GetMaintenanceAsync(g);
                }
                else
                {
                    //Boolean flagUserId = false;                    // Will be set to true if any item.Field value is UserId 
                    DomainContextLoadBatch batch = new DomainContextLoadBatch(DataLoadComplete);

                    var query = Context.GetUserProfileQuery();
                    //var serviceLineQuery = Context.GetServiceLineQuery();

                    if (SearchParameters.Count > 0)
                    {
                        foreach (var item in SearchParameters)
                        {
                            string searchvalue = item.Value;

                            switch (item.Field)
                            {
                                case "id":
                                case "Key":
                                case "UserId":
                                    //flagUserId = true; // Set flag variable, we must execute both queries via batch

                                    Guid g = new Guid();
                                    if (searchvalue.Equals("0") == false)
                                    {
                                        g = new Guid(searchvalue);
                                    }

                                    query = query.Where(p => p.UserId == g);
                                    //serviceLineQuery.Where(p => p.Inactive == false);
                                    break;
                                case "UserName":
                                    query = query.Where(p =>
                                        p.UserName.ToLower().Contains(searchvalue.ToLower()) && p.DeltaAdmin == false);
                                    break;
                                case "LastName":
                                    query = query.Where(p =>
                                        p.LastName.ToLower().Contains(searchvalue.ToLower()) && p.DeltaAdmin == false);
                                    break;
                                case "FirstName":
                                    query = query.Where(p =>
                                        p.FirstName.ToLower().Contains(searchvalue.ToLower()) && p.DeltaAdmin == false);
                                    break;
                                case "Inactive":
                                    bool inactive = Convert.ToBoolean(searchvalue);
                                    if (!inactive)
                                    {
                                        query = query.Where(p => p.Inactive == inactive && p.DeltaAdmin == false);
                                    }

                                    break;
                            }
                        }
                    }
                    else
                    {
                        query = query.Where(p => p.UserId == UserCache.Current.GetCurrentUserProfile().UserId);
                    }

                    query.IncludeTotalCount = true;

                    if (PageSize > 0)
                    {
                        query = query.Skip(PageSize * PageIndex);
                        query = query.Take(PageSize);
                    }

                    IsLoading = true;

                    batch.Add(Context.Load(query, LoadBehavior.RefreshCurrent, false));

                    //if (flagUserId) // If flagUserId is set then we execute both queries
                    //batch.Add(Context.Load<ServiceLine>(serviceLineQuery, LoadBehavior.RefreshCurrent, false));
                }
            });
        }

        public void GetMaintenanceAsync(Guid id)
        {
            //Boolean flagUserId = false;                    // Will be set to true if any item.Field value is UserId 
            DomainContextLoadBatch batch = new DomainContextLoadBatch(DataLoadComplete);

            //query = query.Where(p => p.UserId == g);

            var query = Context.GetUserProfileForMaintQuery(id);

            query.IncludeTotalCount = true;

            IsLoading = true;

            batch.Add(Context.Load(query, LoadBehavior.RefreshCurrent, false));
        }

        private void DataLoadComplete(DomainContextLoadBatch batch)
        {
            List<Exception> LoadErrors = new List<Exception>();

            if (batch.FailedOperationCount > 0)
            {
                foreach (var fop in batch.FailedOperations)
                    if (fop.HasError)
                    {
                        LoadErrors.Add(fop.Error);
                    }
                //Context.EntityContainer.Clear();
            }

            if (OnMultiLoaded != null)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    IsLoading = false;
                    OnMultiLoaded(this, new MultiErrorEventArgs(LoadErrors));
                });
            }
            else
            {
                IsLoading = false;
            }
        }


        public IEnumerable<UserProfile> Items => Context.UserProfiles;

        PagedEntityCollectionView<UserProfile> _UserProfiles;

        public PagedEntityCollectionView<UserProfile> UserProfiles
        {
            get { return _UserProfiles; }
            set
            {
                if (_UserProfiles != value)
                {
                    _UserProfiles = value;
                    this.RaisePropertyChanged(p => p.UserProfiles);
                }
            }
        }

        public event EventHandler<EntityEventArgs<UserProfile>> OnLoaded;

        public event EventHandler<MultiErrorEventArgs> OnMultiLoaded;

        public event EventHandler<ErrorEventArgs> OnSaved;

        public bool SaveAllAsync()
        {
            //IModelDataService requires this method - needs refactored...
            //return true;
            throw
                new Exception(
                    "Invalid call to SaveAllAsync.  No user"); //use SaveAllAsync(UserProfile user) instead - needs refactored.
        }

        public bool ContextHasChanges => (Context != null) && Context.HasChanges;

        void Context_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e != null && e.PropertyName.Equals("HasChanges"))
            {
                RaisePropertyChanged("ContextHasChanges");
            }
        }

        public bool SaveAllAsync(UserProfile user)
        {
            var open_or_invalid = OpenOrInvalidObjects(Context);
            if (open_or_invalid) //TODO: should we raise/return an error or something???
            {
                PendingSubmit = true;
                return false;
            }

            PendingSubmit = false;

            if (user.IsNew) //TODO: J.E. this is bad design IMHO - this should be in the ViewModel - NOT the Model(Service Agent) - this needs refactored...
            {
                NewPassword = GeneratePassword.Generate();
                Context.IsNewPasswordValid(NewPassword, string.Empty, user.PasswordHistory,
                    (Configuration.Setting.PasswordHistory ?? 5), PasswordValidated, user);
            }
            else if (user.ResetPassword)
            {
                user.SecurityAnswer = string.Empty;
                user.SecurityQuestion = string.Empty;

                NewPassword = GeneratePassword.Generate();
                user.Reset = NewPassword;
                Context.IsNewPasswordValid(NewPassword, user.Password, user.PasswordHistory,
                    (Configuration.Setting.PasswordHistory ?? 5), PasswordValidated, user);
            }
            else
            {
                //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
                //      thread after the current set of processing completes.
                Dispatcher.BeginInvoke(() =>
                {
                    IsLoading = true;

                    if (UserProfileChildUpdateTableNeedsUpdate())
                    {
                        UpdateUserProfileChildUpdateTable(user);
                    }

                    Context.SubmitChanges(g => HandleSubmitOperationResults(g, OnSaved), null);
                });
            }

            return true;
        }

        private void UpdateUserProfileChildUpdateTable(UserProfile user)
        {
            if (Context == null)
            {
                return;
            }

            if (Context.UserProfileGroups != null && Context.UserProfileGroups.HasChanges)
            {
                UpdateUserProfileChildUpdate(user, "UserProfileGroup");
            }

            if (Context.DisciplineInUserProfiles != null && Context.DisciplineInUserProfiles.HasChanges)
            {
                UpdateUserProfileChildUpdate(user, "DisciplineInUserProfile");
            }

            if (Context.UserProfileServiceLines != null && Context.UserProfileServiceLines.HasChanges)
            {
                UpdateUserProfileChildUpdate(user, "UserProfileServiceLine");
            }

            if (Context.AttributeInUserProfiles != null && Context.AttributeInUserProfiles.HasChanges)
            {
                UpdateUserProfileChildUpdate(user, "AttributeInUserProfile");
            }

            if (Context.UserProfileProductivities != null && Context.UserProfileProductivities.HasChanges)
            {
                UpdateUserProfileChildUpdate(user, "UserProfileProductivity");
            }

            if (Context.UserProfileAlternateIDs != null && Context.UserProfileAlternateIDs.HasChanges)
            {
                UpdateUserProfileChildUpdate(user, "UserProfileAlternateID");
            }
        }

        private void UpdateUserProfileChildUpdate(UserProfile user, string tableName)
        {
            UserProfileChildUpdate existingRow = Context.UserProfileChildUpdates
                .Where(a => a.TableName == tableName && a.UserID == user.UserId).FirstOrDefault();

            if (existingRow == null)
            {
                UserProfileChildUpdate myRow = new UserProfileChildUpdate();
                myRow.TableName = tableName;
                myRow.UserID = user.UserId;
                myRow.UpdatedDate = DateTime.UtcNow;

                Context.UserProfileChildUpdates.Add(myRow);
            }
            else
            {
                existingRow.UserID = user.UserId;
                existingRow.UpdatedBy = user.UserId;
                existingRow.UpdatedDate = DateTime.UtcNow;
            }
        }

        private void InsertUserProfileChildUpdateOnNewUser(UserProfile user)
        {
            if (Context.UserProfileGroups != null && Context.UserProfileGroups.HasChanges)
            {
                UserProfileChildUpdate myRow = new UserProfileChildUpdate();
                myRow.TenantID = user.TenantID;
                myRow.TableName = "UserProfileGroup";
                myRow.UserID = user.UserId;
                myRow.UpdatedDate = DateTime.UtcNow;

                user.UserProfileChildUpdate.Add(myRow);
            }

            if (Context.DisciplineInUserProfiles != null && Context.DisciplineInUserProfiles.HasChanges)
            {
                UserProfileChildUpdate myRow = new UserProfileChildUpdate();
                myRow.TenantID = user.TenantID;
                myRow.TableName = "DisciplineInUserProfile";
                myRow.UserID = user.UserId;
                myRow.UpdatedDate = DateTime.UtcNow;

                user.UserProfileChildUpdate.Add(myRow);
            }

            if (Context.UserProfileServiceLines != null && Context.UserProfileServiceLines.HasChanges)
            {
                UserProfileChildUpdate myRow = new UserProfileChildUpdate();
                myRow.TenantID = user.TenantID;
                myRow.TableName = "UserProfileServiceLine";
                myRow.UserID = user.UserId;
                myRow.UpdatedDate = DateTime.UtcNow;

                user.UserProfileChildUpdate.Add(myRow);
            }

            if (Context.AttributeInUserProfiles != null && Context.AttributeInUserProfiles.HasChanges)
            {
                UserProfileChildUpdate myRow = new UserProfileChildUpdate();
                myRow.TenantID = user.TenantID;
                myRow.TableName = "AttributeInUserProfile";
                myRow.UserID = user.UserId;
                myRow.UpdatedDate = DateTime.UtcNow;

                user.UserProfileChildUpdate.Add(myRow);
            }

            if (Context.UserProfileProductivities != null && Context.UserProfileProductivities.HasChanges)
            {
                UserProfileChildUpdate myRow = new UserProfileChildUpdate();
                myRow.TenantID = user.TenantID;
                myRow.TableName = "UserProfileProductivity";
                myRow.UserID = user.UserId;
                myRow.UpdatedDate = DateTime.UtcNow;

                user.UserProfileChildUpdate.Add(myRow);
            }

            if (Context.UserProfileAlternateIDs != null && Context.UserProfileAlternateIDs.HasChanges)
            {
                UserProfileChildUpdate myRow = new UserProfileChildUpdate();
                myRow.TenantID = user.TenantID;
                myRow.TableName = "UserProfileAlternateID";
                myRow.UserID = user.UserId;
                myRow.UpdatedDate = DateTime.UtcNow;

                user.UserProfileChildUpdate.Add(myRow);
            }
        }

        private bool UserProfileChildUpdateTableNeedsUpdate()
        {
            if (Context == null)
            {
                return false;
            }

            bool hasChanges = false;
            hasChanges = Context.UserProfileGroups != null && Context.UserProfileGroups.HasChanges;
            hasChanges = hasChanges ||
                         (Context.DisciplineInUserProfiles != null && Context.DisciplineInUserProfiles.HasChanges);
            hasChanges = hasChanges ||
                         (Context.UserProfileServiceLines != null && Context.UserProfileServiceLines.HasChanges);
            hasChanges = hasChanges ||
                         (Context.AttributeInUserProfiles != null && Context.AttributeInUserProfiles.HasChanges);
            hasChanges = hasChanges || (Context.UserProfileProductivities != null &&
                                        Context.UserProfileProductivities.HasChanges);

            return hasChanges;
        }

        public void PasswordValidated(object operation)
        {
            InvokeOperation ioperation = operation as InvokeOperation;
            UserProfile user = ioperation.UserState as UserProfile;

            PasswordPOCO r = ioperation.Value as PasswordPOCO;

            string enc = r.Hashed;

            user.Password = enc;
            user.PasswordReset = true;
            user.AccountLocked = false;
            user.PasswordChangeDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

            // Update UserProfileChildUpdate table on a new user, if necessary
            InsertUserProfileChildUpdateOnNewUser(user);

            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                IsLoading = true;
                Context.SubmitChanges(g => HandleSubmitOperationResults(g, OnSaved), null);
            });

            EmailContext ectx = new EmailContext();
            ectx.RecoverPassword(user.UserId, NewPassword,
                result =>
                {
                    if (result.HasError)
                    {
                        if (result.Error.InnerException != null)
                        {
                            MessageBox.Show(String.Format(
                                "Recover Password reported an error.  Error: {0}\nInner Error: {1}",
                                result.Error.Message,
                                result.Error.InnerException.Message));
                        }
                        else
                        {
                            MessageBox.Show(String.Format("Recover Password reported an error.  Error: {0}",
                                result.Error.Message));
                        }

                        foreach (var err in result.ValidationErrors)
                            MessageBox.Show(String.Format("Recover Password reported a validation error.  Error: {0}",
                                err.ErrorMessage));

                        result.MarkErrorAsHandled(); //so that an exception is not thrown.
                    }
                }, null);
        }

        public void RejectChanges()
        {
            Context.RejectChanges();
        }

        #endregion

        public void Cleanup()
        {
            UserProfiles.Cleanup();
            Context.PropertyChanged -= Context_PropertyChanged;
        }

        public void GetSurveyorAdmissionsAsync(Guid UserID)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.PatientSearches.Clear();

                var query = Context.spGetUserProfileAdmissionQuery(UserID, null, null);

                query.IncludeTotalCount = true;

                Context.Load(query, LoadBehavior.RefreshCurrent, GetSurveyorAdmissionsLoaded, null);
            });
        }

        public event EventHandler<EntityEventArgs<PatientSearch>> OnGetSurveyorAdmissionsLoaded;

        private void GetSurveyorAdmissionsLoaded(LoadOperation<PatientSearch> results)
        {
            HandleEntityResults(results, OnGetSurveyorAdmissionsLoaded);
        }

        public void ValidateSurveyorAdmissionAsync(string MRN, string AdmissionID)
        {
            //Note: DispatcherInvoke means that the code you place in the call will be executed on the UI 
            //      thread after the current set of processing completes.
            Dispatcher.BeginInvoke(() =>
            {
                Context.PatientSearches.Clear();

                var query = Context.spGetUserProfileAdmissionQuery(null, MRN, AdmissionID);

                query.IncludeTotalCount = true;

                Context.Load(query, LoadBehavior.RefreshCurrent, ValidateSurveyorAdmissionLoaded, null);
            });
        }

        public event EventHandler<EntityEventArgs<PatientSearch>> OnValidateSurveyorAdmissionLoaded;

        private void ValidateSurveyorAdmissionLoaded(LoadOperation<PatientSearch> results)
        {
            HandleEntityResults(results, OnValidateSurveyorAdmissionLoaded);
        }


        public System.Threading.Tasks.Task<bool> AsyncValidateCrescendoConnectUsers(UserProfile userProfile,
            int CrescendoConnectMaxAllowed)
        {
            var asyncValidationResultList = new Utility.ValidationResults();

            //NOTE: async-server side functions coded to return true if the error condition exists
            return Context.ValidateCrescendoConnectUserCount(
                    WebContext.Current.User.TenantID,
                    CrescendoConnectMaxAllowed)
                .AsTask()
                .ContinueWith(t =>
                {
                    var operation = t.Result;
                    if (!operation.HasError && operation.Value)
                    {
                        asyncValidationResultList.AddSingle(new ValidationResult(
                            "Cannot save the user profile as a CrescendoConnect user.  The maximum number of active CrescendoConnect users has been reached.",
                            new[] { "CrescendoConnectUser" }));
                    }

                    return t.Result.Value;
                })
                .ContinueWith(
                    task =>
                    {
                        if (task.IsFaulted)
                        {
                            return false;
                        }

                        //Add cached errors to entity on the UI thread
                        asyncValidationResultList.ForEach(error => { userProfile.ValidationErrors.Add(error); });
                        return (task.Result);
                    },
                    System.Threading.CancellationToken.None,
                    System.Threading.Tasks.TaskContinuationOptions.None,
                    Client.Utils.AsyncUtility.TaskScheduler);
        }
    }
}