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
        /// Get a strongly typed list
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
        /// Quickly add list data of entity types
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
        /// Quickly add objects, with automatic classification by type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        public void AddItem<T>(T item)
        {
            Add(new List<T>() { item });
        }

        /// <summary>
        /// Quickly add list data of entity types
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
        /// Create DataList
        /// </summary>
        /// <param name="uUID">Unique number. DataList data with the same number will not be written repeatedly in the same unit gate test.</param>
        public DataList(string uUID)
        {
            UUID = uUID;
        }
    }
}
