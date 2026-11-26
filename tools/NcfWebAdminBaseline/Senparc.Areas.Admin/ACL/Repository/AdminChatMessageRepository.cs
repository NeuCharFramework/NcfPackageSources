using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL
{
    public interface IAdminChatMessageRepository : IClientRepositoryBase<AdminChatMessage>
    {
    }

    public class AdminChatMessageRepository : ClientRepositoryBase<AdminChatMessage>, IAdminChatMessageRepository
    {
        /// <summary>Reserved parameterless constructor for repository infrastructure.</summary>
        private AdminChatMessageRepository() : base(null) { }

        /// <summary>Initializes the chat-message repository with the NCF database provider.</summary>
        /// <param name="ncfDbData">The database data provider used by the base repository.</param>
        public AdminChatMessageRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {
        }
    }
}
