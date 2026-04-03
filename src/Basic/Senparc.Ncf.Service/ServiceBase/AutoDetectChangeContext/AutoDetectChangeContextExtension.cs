using Microsoft.EntityFrameworkCore;

namespace Senparc.Ncf.Service
{
    public static class AutoDetectChangeContextExtension
    {
       /// <summary>
        /// Create an instance of AutoDetectChangeContext
       /// </summary>
       /// <param name="serviceData"></param>
       /// <returns></returns>
       public static AutoDetectChangeContextWrap InstanceAutoDetectChangeContextWrap(this IServiceDataBase serviceData)
       {
           return new AutoDetectChangeContextWrap(serviceData);
       }

       /// <summary>
       /// Create an instance of CloseAutoDetectChangeContext
       /// </summary>
       /// <param name="wrap">AutoDetectChangeContextWrap instance</param>
       /// <returns></returns>
       public static CloseAutoDetectChangeContext InstanceCloseAutoDetectChangeContext(this AutoDetectChangeContextWrap wrap)
       {
           return new CloseAutoDetectChangeContext(wrap);
       }

       /// <summary>
       /// Create an instance of CloseAutoDetectChangeContext
       /// </summary>
       /// <param name="wrap">AutoDetectChangeContextWrap instance</param>
       /// <returns></wrap>
       public static void ForceDetectChange(this AutoDetectChangeContextWrap wrap, object entity)
       {
           wrap.ServiceData.BaseData.BaseDB.BaseDataContext.Entry(entity).State = EntityState.Modified;
       }
    }
}
