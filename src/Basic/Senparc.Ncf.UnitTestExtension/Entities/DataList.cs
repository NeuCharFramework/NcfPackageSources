using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Senparc.Ncf.UnitTestExtension.Entities
{
    public class DataList : Dictionary<Type, List<object>>
    {
        /// <summary>
        /// 
        /// </summary>
        public string UUID { get; set; }

        /// <summary>
        /// 获取强类型的列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> GetDataList<T>()
        {
            if (!base.ContainsKey(typeof(T)))
            {
                return null;
            }

            return base[typeof(T)].Cast<T>().ToList();
        }

        public List<object> GetList(Type type)
        {
            if (!base.ContainsKey(type))
            {
                return null;
            }

            return base[type];
        }


        /// <summary>
        /// 快速添加实体类型的列表数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public void Add<T>(List<T> list)
        {
            if (base.ContainsKey(typeof(T)))
            {
                base[typeof(T)].AddRange(list.Cast<object>().ToList());
            }
            else
            {
                base[typeof(T)] = list.Cast<object>().ToList();
            }
        }

        /// <summary>
        /// 快速添加 个对象，兼备自动根据类型分类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        public void AddItem<T>(T item)
        {
            Add(new List<T>() { item });
        }

        /// <summary>
        /// 快速添加实体类型的列表数据
        /// </summary>
        /// <param name="DataList"></param>
        public void AddRange(DataList dataList)
        {
            foreach (var item in dataList)
            {
                if (base.ContainsKey(item.Key))
                {
                    base[item.Key].AddRange(item.Value);
                }
                else
                {
                    base[item.Key] = item.Value;
                }
            }
        }

        /// <summary>
        /// 创建 DataList
        /// </summary>
        /// <param name="uUID">唯一编号，相同编号的 DataList 数据在同一次单元门测试中不会被反复写入</param>
        public DataList(string uUID)
        {
            UUID = uUID;
        }
    }
}
