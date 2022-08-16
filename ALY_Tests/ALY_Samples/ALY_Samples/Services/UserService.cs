#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Metrics;
using Virtuoso.Server.Data;
using Virtuoso.Services;
using Virtuoso.Services.Authentication;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IUserService))]
    public class UserService : PagedModelBase, IUserService
    {
        VirtuosoApplicationConfiguration Configuration { get; set; }
        public VirtuosoDomainContext Context { get; set; }
        string NewPassword;

        [ImportingConstructor]
        public UserService(VirtuosoApplicationConfiguration config)
        {
            Configuration = config;
            Context = new VirtuosoDomainContext();
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

        public void Clear()
        {
            Context.RejectChanges();
            Context.EntityContainer.Clear();
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

                var lastName = GetSearchParameterValue("LastName");
                var firstName = GetSearchParameterValue("FirstName");
                var userName = GetSearchParameterValue("UserName");

                bool tryInactive;
                bool? inactive = null;
                if (Boolean.TryParse(GetSearchParameterValue("Inactive"), out tryInactive))
                {
                    inactive = tryInactive;
                }

                if (isSystemSearch == false)
                {
                    //Bug 13160: Integrated System Search for DDL Smart Combo includes inactives when filters selected
                    inactive = false;
                }

                Guid tryUserId;
                Guid? userId = null;
                if (Guid.TryParse(GetSearchParameterValue("UserId"), out tryUserId))
                {
                    userId = tryUserId;
                }

                int tryServiceLineKey;
                int? serviceLineKey = null;
                if (Int32.TryParse(GetSearchParameterValue("ServiceLineKey"), out tryServiceLineKey))
                {
                    serviceLineKey = tryServiceLineKey;
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

                var query = Context.GetUserProfileSystemSearchQuery(serviceLineKey, userId.GetValueOrDefault(),
                    userName, lastName, firstName, inactive.GetValueOrDefault(), returnDeltaCredentials);

                query.IncludeTotalCount = true;

                IsLoading = true;

                Context.Load(
                    query,
                    LoadBehavior.RefreshCurrent,
                    g => HandleEntityResults(g, OnLoaded),
                    new MetricsTimer(new StopWatchFactory(), CorrelationIDHelper.ID,
                        Logging.Context.UserSearch_GetSearchAsync));
            });
        }

        private string GetSearchParameterValue(string paramName)
        {
            string ret = string.Empty;
            var param = SearchParameters.FirstOrDefault(i => i.Field.Equals(paramName));
            if (param != null)
            {
                ret = param.Value;
            }

            return ret;
        }

        public void GetAsync()
        {
            throw new NotImplementedException("UserService does not implement GetAsync()");
        }

        public void GetMaintenanceAsync(Guid id)
        {
            throw new NotImplementedException("UserService does not implement GetMaintenanceAsync()");
        }

        private void DataLoadComplete(DomainContextLoadBatch batch)
        {
            List<Exception> LoadErrors = new List<Exception>();

            if (batch.FailedOperationCount > 0)
            {
                foreach (var fop in batch.FailedOperations)
                {
                    if (fop.HasError)
                    {
                        LoadErrors.Add(fop.Error);
                    }
                }
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
            throw new Exception("Invalid call to SaveAllAsync.  No user");
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
            UserProfileChildUpdate existingRow = Context.UserProfileChildUpdates.FirstOrDefault(a => a.TableName == tableName && a.UserID == user.UserId);

            if (existingRow == null)
            {
                UserProfileChildUpdate myRow = new UserProfileChildUpdate
                {
                    TableName = tableName,
                    UserID = user.UserId,
                    UpdatedDate = DateTime.UtcNow
                };

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
                UserProfileChildUpdate myRow = new UserProfileChildUpdate
                {
                    TenantID = user.TenantID,
                    TableName = "UserProfileGroup",
                    UserID = user.UserId,
                    UpdatedDate = DateTime.UtcNow
                };

                user.UserProfileChildUpdate.Add(myRow);
            }

            if (Context.DisciplineInUserProfiles != null && Context.DisciplineInUserProfiles.HasChanges)
            {
                UserProfileChildUpdate myRow = new UserProfileChildUpdate
                {
                    TenantID = user.TenantID,
                    TableName = "DisciplineInUserProfile",
                    UserID = user.UserId,
                    UpdatedDate = DateTime.UtcNow
                };

                user.UserProfileChildUpdate.Add(myRow);
            }

            if (Context.UserProfileServiceLines != null && Context.UserProfileServiceLines.HasChanges)
            {
                UserProfileChildUpdate myRow = new UserProfileChildUpdate
                {
                    TenantID = user.TenantID,
                    TableName = "UserProfileServiceLine",
                    UserID = user.UserId,
                    UpdatedDate = DateTime.UtcNow
                };

                user.UserProfileChildUpdate.Add(myRow);
            }

            if (Context.AttributeInUserProfiles != null && Context.AttributeInUserProfiles.HasChanges)
            {
                UserProfileChildUpdate myRow = new UserProfileChildUpdate
                {
                    TenantID = user.TenantID,
                    TableName = "AttributeInUserProfile",
                    UserID = user.UserId,
                    UpdatedDate = DateTime.UtcNow
                };

                user.UserProfileChildUpdate.Add(myRow);
            }

            if (Context.UserProfileProductivities != null && Context.UserProfileProductivities.HasChanges)
            {
                UserProfileChildUpdate myRow = new UserProfileChildUpdate
                {
                    TenantID = user.TenantID,
                    TableName = "UserProfileProductivity",
                    UserID = user.UserId,
                    UpdatedDate = DateTime.UtcNow
                };

                user.UserProfileChildUpdate.Add(myRow);
            }

            if (Context.UserProfileAlternateIDs != null && Context.UserProfileAlternateIDs.HasChanges)
            {
                UserProfileChildUpdate myRow = new UserProfileChildUpdate
                {
                    TenantID = user.TenantID,
                    TableName = "UserProfileAlternateID",
                    UserID = user.UserId,
                    UpdatedDate = DateTime.UtcNow
                };

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
            UserProfile user = ioperation?.UserState as UserProfile;

            PasswordPOCO r = ioperation?.Value as PasswordPOCO;

            string enc = r?.Hashed;

            if (user != null)
            {
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
                                MessageBox.Show(String.Format(
                                    "Recover Password reported a validation error.  Error: {0}",
                                    err.ErrorMessage));

                            result.MarkErrorAsHandled(); //so that an exception is not thrown.
                        }
                    }, null);
            }
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
    }
}