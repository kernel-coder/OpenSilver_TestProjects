#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Virtuoso.Client.Offline;
using Virtuoso.Core.Log;
using Virtuoso.Helpers;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.Cache.Extensions
{
    public static class UserCacheExtensions
    {
        public static List<UserProfile> GetUsers(this UserCache me, bool includeEmpty = false)
        {
            var ret = me.Context.UserProfiles.Where(u => u.TenantID != 0).ToList();
            if (includeEmpty)
            {
                ret.Insert(0, new UserProfile { FirstName = " ", LastName = " ", UserId = Guid.Empty });
            }

            return ret;
        }

        public static List<UserProfile> GetActiveUsersPlusMe(this UserCache me, Guid? userID, bool includeEmpty = false,
            Guid? NewUserId = null, String LastNameToUse = null)
        {
            if ((me == null) || (me.Context == null) || (me.Context.UserProfiles == null))
            {
                return null;
            }

            var ret = me.Context.UserProfiles.Where(u => u.TenantID != 0)
                .Where(p => ((p.Inactive == false) || (p.UserId == userID))).OrderBy(p => p.FullName).ToList();
            if (includeEmpty)
            {
                var LName = LastNameToUse ?? " ";
                if (NewUserId != null)
                {
                    ret.Insert(0, new UserProfile { UserId = NewUserId.Value, FirstName = " ", LastName = LName });
                }
                else
                {
                    ret.Insert(0, new UserProfile { FirstName = " ", LastName = LName });
                }
            }

            return ret;
        }

        public static List<UserProfile> GetActiveUsersPlusMeWithDeltaUserFiltering(this UserCache me, bool isDeltaUser,
            Guid? userID, bool includeEmpty = false, Guid? NewUserId = null, String LastNameToUse = null)
        {
            if ((me == null) || (me.Context == null) || (me.Context.UserProfiles == null))
            {
                return null;
            }

            var _users = me.Context.UserProfiles
                .Where(u => u.TenantID != 0)
                .Where(p => ((p.Inactive == false) || (p.UserId == userID)))
                .OrderBy(p => p.FullName).ToList();

            List<UserProfile> ret = null;

            if (isDeltaUser == false) //logged in user is not DeltaUser/DeltaAdmin - filter out all DeltaUser/DeltaAdmin
            {
                ret = _users
                    .Where(u => !(u.DeltaUser || u.DeltaAdmin) || (u.UserId == userID))
                    .OrderBy(p => p.FullName).ToList();
            }
            else
            {
                ret = _users.OrderBy(p => p.FullName).ToList();
            }

            if (includeEmpty)
            {
                var LName = LastNameToUse ?? " ";
                if (NewUserId != null)
                {
                    ret.Insert(0, new UserProfile { UserId = NewUserId.Value, FirstName = " ", LastName = LName });
                }
                else
                {
                    ret.Insert(0, new UserProfile { FirstName = " ", LastName = LName });
                }
            }

            return ret;
        }


        public static List<UserProfile> GetActiveTeleMonitoringUsersPlusMe(this UserCache me, Guid? userID)
        {
            if ((me == null) || (me.Context == null) || (me.Context.UserProfiles == null))
            {
                return null;
            }

            List<UserProfile> upList = me.GetActiveUsersPlusMe(userID);
            if (upList == null)
            {
                return null;
            }

            List<UserProfile> retList = new List<UserProfile>();
            foreach (UserProfile up in upList)
                if ((up.UserId == userID) || (me.IsUserInRole(up, Constants.Cache.ROLE_TELEMONITORING)))
                {
                    retList.Add(up);
                }

            return retList;
        }

        public static bool IsUserInRole(this UserCache me, UserProfile up, string roleName)
        {
            if (up == null)
            {
                return false;
            }

            int? roleKey = RoleCache.GetRoleKeyFromRoleName(roleName);
            if (roleKey == null)
            {
                return false;
            }

            List<UserProfileInRole> upirList = me.GetCurrentUserProfileInRoleFromUserId(up.UserId);
            if (upirList == null)
            {
                return false;
            }

            foreach (UserProfileInRole upir in upirList)
                if (upir.RoleKey == roleKey)
                {
                    return true;
                }

            return false;
        }

        public static bool IsUserInRoleNoDelta(this UserCache me, UserProfile up, string roleName)
        {
            if (up == null)
            {
                return false;
            }

            if (up.DeltaAdmin)
            {
                return false;
            }

            if (up.DeltaUser)
            {
                return false;
            }

            int? roleKey = RoleCache.GetRoleKeyFromRoleName(roleName);
            if (roleKey == null)
            {
                return false;
            }

            List<UserProfileInRole> upirList = me.GetCurrentUserProfileInRoleFromUserId(up.UserId);
            if (upirList == null)
            {
                return false;
            }

            foreach (UserProfileInRole upir in upirList)
                if (upir.RoleKey == roleKey)
                {
                    return true;
                }

            return false;
        }

        public static List<UserProfileInRole> GetCurrentUserProfileInRoleFromUserId(this UserCache me, Guid? userID)
        {
            if (userID == null)
            {
                return null;
            }

            if ((UserCache.Current == null) || (UserCache.Current.Context == null))
            {
                return null;
            }

            List<UserProfileInRole> upirList = UserCache.Current.Context.UserProfileInRoles
                .Where(u => (u.UserId == userID) && (u.RoleEnd == null)).ToList();

            return (upirList.Any() == false) ? null : upirList;
        }

        public static UserProfile GetUserProfileFromUserId(this UserCache me, Guid userID, bool displayErrors = true)
        {
            try
            {
                if ((UserCache.Current == null) || (UserCache.Current.Context == null))
                {
                    return null;
                }

                //UserProfile u = (from c in UserCache.Current.Context.UserProfiles.AsQueryable<UserProfile>() where (c.UserId == userID) select c).FirstOrDefault();
                if (UserCache.Current.UserProfileDictionary.Count <= 0)
                {
                    throw new InvalidOperationException("Invalid attempt to call GetUserProfileFromUserId before UserProfileDictionary initialization.");
                }

                UserProfile u = null;
                UserCache.Current.UserProfileDictionary.TryGetValue(userID, out u);
                if ((u == null) && (userID != null))
                {
                    var msg = String.Format(
                        "Error UserCache.GetUserProfileFromUserId: UserID {0} is not defined.  Contact your system administrator.",
                        userID.ToString());
                    if (displayErrors)
                    {
                        MessageBox.Show(msg);
                    }

                    throw new Exception(msg);
                }

                return u;
            }
            catch (Exception ExceptionReceived)
            {
                //Log error and stack trace so that we can determine the origin of - "Error UserCache.GetUserProfileFromUserId: UserID {0} is not defined.  Contact your system administrator."
                var errorDetailHelper = new ErrorDetailHelper();
                var errorDetail = errorDetailHelper.GetErrorDetail(ExceptionReceived,
                    string.Format("GetUserProfileFromUserId({0})", userID));
                var errorDetaiLog = new ErrorDetailLog();
                if (EntityManager.Current.IsOnline)
                {
                    errorDetaiLog.Add(errorDetail);
                }
                else
                {
                    errorDetaiLog.SaveToDisk(errorDetail);
                }

                return null; //fatal exception, return null from method...
            }
        }

        public static UserProfile GetUserProfileFromUserId(this UserCache me, Guid? userID, bool displayErrors = true)
        {
            if (userID == null)
            {
                return null;
            }

            return me.GetUserProfileFromUserId((Guid)userID, displayErrors);
        }

        public static UserProfile GetFirstUserProfileIsAgencyAdmin(this UserCache me)
        {
            try
            {
                if ((UserCache.Current == null) || (UserCache.Current.Context == null))
                {
                    return null;
                }

                UserProfile up = me.Context.UserProfiles
                    .Where(u => ((u.TenantID != 0) && u.IsAgencyAdmin))
                    .FirstOrDefault();
                return up;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string GetFullNameFromUserId(this UserCache me, Guid? userID)
        {
            UserProfile u = me.GetUserProfileFromUserId(userID);
            if (u == null)
            {
                return null;
            }

            return u.FullName;
        }

        public static string GetFullNameWithSuffixFromUserId(this UserCache me, Guid? userID)
        {
            UserProfile u = me.GetUserProfileFromUserId(userID);
            if (u == null)
            {
                return null;
            }

            return u.FullNameWithSuffix;
        }

        public static string GetFormalNameFromUserId(this UserCache me, Guid? userID)
        {
            UserProfile u = me.GetUserProfileFromUserId(userID);
            if (u == null)
            {
                return null;
            }

            return u.FormalName;
        }

        private static UserProfile _CurrentUserProfile;

        public static UserProfile GetCurrentUserProfile(this UserCache me)
        {
            if (_CurrentUserProfile != null)
            {
                return _CurrentUserProfile;
            }

            if (WebContext.Current.User.IsAuthenticated == false)
            {
                return null;
            }

            _CurrentUserProfile = me.GetUserProfileFromUserId(WebContext.Current.User.MemberID);
            if (_CurrentUserProfile != null)
            {
                _CurrentUserProfile.ServiceLineGroupIdsUserCanSeeAsStr =
                    WebContext.Current.User.ServiceLineGroupIdsUserCanSee;
            }

            return _CurrentUserProfile;
        }

        private static List<UserProfile> _brList;

        public static List<UserProfile> GetBereavementRoleUserProfilePlusCurrentList(this UserCache me)
        {
            if (_brList != null)
            {
                return _brList;
            }

            if ((me == null) || (me.Context == null) || (me.Context.UserProfiles == null))
            {
                return null;
            }

            List<UserProfile> upList = me.GetActiveUsersPlusMe(_CurrentUserProfile?.UserId);
            if (upList == null)
            {
                return null;
            }

            _brList = new List<UserProfile>();
            foreach (UserProfile up in upList)
                if ((up.UserId == _CurrentUserProfile?.UserId) ||
                    (me.IsUserInRoleNoDelta(up, Constants.Cache.ROLE_BEREAVEMENT)))
                {
                    _brList.Add(up);
                }

            return _brList;
        }

        public static List<UserProfile> GetUserProfileByDisciplineKey(this UserCache me, int disciplinekey)
        {
            return UserCache.Current.Context.UserProfiles.Where(u => u.TenantID != 0).Where(p =>
                !p.Inactive && p.DisciplineInUserProfile.Where(d => d.DisciplineKey == disciplinekey).Any()).ToList();
        }

        public static List<UserProfile> GetUserProfilePlusMeByDisciplineKey(this UserCache me, int disciplinekey,
            Guid? UserID)
        {
            return UserCache.Current.Context.UserProfiles.Where(u => u.TenantID != 0).Where(p =>
                !p.Inactive && ((p.DisciplineInUserProfile.Where(d => d.DisciplineKey == disciplinekey).Any()) ||
                                (p.UserId == UserID))).ToList();
        }

        public static bool IsDisciplineInCurrentUserProfile(this UserCache me, int disciplinekey)
        {
            UserProfile up = UserCache.Current.Context.UserProfiles.Where(u => u.TenantID != 0).Where(p =>
                !p.Inactive && p.UserId == WebContext.Current.User.MemberID &&
                p.DisciplineInUserProfile.Where(d => d.DisciplineKey == disciplinekey).Any()).FirstOrDefault();
            return (up == null) ? false : true;
        }

        public static bool IsDisciplineInUserProfile(this UserCache me, int disciplinekey, Guid UserID)
        {
            UserProfile up = UserCache.Current.Context.UserProfiles.Where(u => u.TenantID != 0).Where(p =>
                !p.Inactive && p.UserId == UserID &&
                p.DisciplineInUserProfile.Where(d => d.DisciplineKey == disciplinekey).Any()).FirstOrDefault();
            return (up == null) ? false : true;
        }

        public static bool UserIdIsHospiceMedicalDirector(this UserCache me, Guid? userID)
        {
            UserProfile u = me.GetUserProfileFromUserId(userID);
            if (u == null)
            {
                return false;
            }

            return u.DisciplineInUserProfile.Where(d => d.IsHospiceMedicalDirector).Any();
        }

        public static bool UserIdIsHospicePhysician(this UserCache me, Guid? userID)
        {
            UserProfile u = me.GetUserProfileFromUserId(userID);
            if (u == null)
            {
                return false;
            }

            return u.DisciplineInUserProfile.Where(d => d.IsHospicePhysician).Any();
        }

        public static bool UserIdIsHospiceNursePractitioner(this UserCache me, Guid? userID)
        {
            UserProfile u = me.GetUserProfileFromUserId(userID);
            if (u == null)
            {
                return false;
            }

            return u.DisciplineInUserProfile.Where(d => d.IsHospiceNursePractitioner).Any();
        }

        public static UserProfile GetUserProfileFromPhysicianKey(this UserCache me, int? physicianKey)
        {
            if ((UserCache.Current == null) || (UserCache.Current.Context == null) || (physicianKey == null))
            {
                return null;
            }

            if (physicianKey <= 0)
            {
                return null;
            }

            UserProfile u =
                (from c in UserCache.Current.Context.UserProfiles.AsQueryable()
                    where (c.PhysicianKey == physicianKey)
                    select c).FirstOrDefault();
            return u;
        }

        public static UserProfile GetUserProfileFromPhysicianKeyWherePhysicianIsMedicalDirectorOrHospicePhysician(
            this UserCache me, int? physicianKey)
        {
            UserProfile u = GetUserProfileFromPhysicianKey(me, physicianKey);
            if (u == null)
            {
                return null;
            }

            if ((UserIdIsHospiceMedicalDirector(me, u.UserId) == false) &&
                (UserIdIsHospicePhysician(me, u.UserId) == false))
            {
                return null;
            }

            return u;
        }
    }
}