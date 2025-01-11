using System.Linq;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;

namespace Senparc.Ncf.Service;
public static class ServiceExtension
{
    public static PagedList<TDto> ToDto<T,TDto>(this PagedList<T> pagedList, IServiceBase<T> serviceBase) 
        where T : class, IEntityBase
    {
        return new PagedList<TDto>(pagedList.Select(item => serviceBase.Mapping<TDto>(item)).ToList(), pagedList.PageIndex, pagedList.PageCount, pagedList.TotalCount);
    }
}
