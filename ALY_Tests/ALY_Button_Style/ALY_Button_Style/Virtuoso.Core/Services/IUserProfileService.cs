#region Usings

using System;
using GalaSoft.MvvmLight;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IUserProfileService : IModelDataService<UserProfile>, ICleanup
    {
        bool SaveAllAsync(UserProfile user);
        event EventHandler<MultiErrorEventArgs> OnMultiLoaded;

        PagedEntityCollectionView<UserProfile> UserProfiles { get; }
        void Remove(UserProfileInRole entity);
        void Remove(DisciplineInUserProfile entity);
        void Remove(AlertsInUserProfile entity);
        void Remove(UserProfilePhone entity);
        void Remove(UserProfileProductivity entity);
        void Remove(UserProfileAlternateID entity);
        void Remove(UserProfileGroup entity);
        void Remove(UserProfileServiceLine entity);
        void Remove(UserProfileAdmission entity);

        void GenerateUserMobileInvite(Guid UserID);

        event EventHandler<EntityEventArgs<PatientSearch>> OnGetSurveyorAdmissionsLoaded;
        void GetSurveyorAdmissionsAsync(Guid UserID);

        event EventHandler<EntityEventArgs<PatientSearch>> OnValidateSurveyorAdmissionLoaded;
        void ValidateSurveyorAdmissionAsync(string MRN, string AdmissionID);

        System.Threading.Tasks.Task<bool> AsyncValidateCrescendoConnectUsers(UserProfile userProfile,
            int CrescendoConnectMaxAllowed);
    }

    public interface IUserService : IModelDataService<UserProfile>, ICleanup
    {
        void SetLocationForMonitoring(string locationOverride);
        event EventHandler<MultiErrorEventArgs> OnMultiLoaded;
        PagedEntityCollectionView<UserProfile> UserProfiles { get; }
    }
    
    public static class UserServiceOptionalExtensions
    {
        public static void SetLocationForMonitoring(this IUserService svc)
        {
            svc.SetLocationForMonitoring(string.Empty);
        }
    }
}