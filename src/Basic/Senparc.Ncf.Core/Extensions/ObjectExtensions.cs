namespace Senparc.Ncf.Core.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Determine whether the object is null
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsNull(object obj)
        {
            return obj == null;
        }
    }
}