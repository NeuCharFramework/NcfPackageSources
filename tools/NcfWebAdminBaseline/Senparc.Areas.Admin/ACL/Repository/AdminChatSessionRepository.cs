using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL
{
    public interface IAdminChatSessionRepository : IClientRepositoryBase<AdminChatSession>
    {
    }

    public class AdminChatSessionRepository : ClientRepositoryBase<AdminChatSession>, IAdminChatSessionRepository
    {
        /// <summary>Reserved parameterless constructor for repository infrastructure.</summary>
        private AdminChatSessionRepository() : base(null) { }

        /// <summary>Initializes the chat-session repository with the NCF database provider.</summary>
        /// <param name="ncfDbData">The database data provider used by the base repository.</param>
        public AdminChatSessionRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {
        }
    }
}
