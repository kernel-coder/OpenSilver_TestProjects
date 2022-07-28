#region Usings

using System;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Services
{
    public class UserSessionService
    {
        private VirtuosoDomainContext Context { get; set; }

        public UserSessionService()
        {
            Context = new VirtuosoDomainContext();
        }

        public void CreateNewSession(string userName, Guid UserId, Action<Guid> callback)
        {
            Guid newGuid = Guid.NewGuid();

            UserSession userSession = new UserSession
            {
                SessionId = newGuid,
                UserName = userName,
                Password = UserId.ToString(),
                CreatedDate = DateTime.UtcNow
            };

            Context.UserSessions.Add(userSession);
            Context.SubmitChanges(g =>
            {
                callback?.Invoke(newGuid);
            }, null);
        }
    }
}