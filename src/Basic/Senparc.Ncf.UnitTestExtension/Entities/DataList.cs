using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.UnitTestExtension.Entities
{
    public class DataList : Dictionary<Type, List<object>>
    {
        /// <summary>
        /// 获取强类型的列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> GetList<T>()
        {
            if (!base.ContainsKey(typeof(T)))
            {
                return null;
            }

            return base[typeof(T)].Cast<T>().ToList();
        }

        /// <summary>
        /// 快速添加实体类型的列表数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public void Add<T>(List<T> list)
        {
            base[typeof(T)] = list.Cast<object>().ToList();
        }
    }
}
