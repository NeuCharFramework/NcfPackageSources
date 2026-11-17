/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: DataList.cs

    File description: DataList related implementation

    

    

    Created by: Senparc - 20240713

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

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
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        public void AddItem<T>(T item)
        {
            Add(new List<T>() { item });
        }

        /// <summary>
        ///Legacy comment translated to English.
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
        /// create DataList
        /// </summary>
        ///Legacy comment translated to English.
        public DataList(string uUID)
        {
            UUID = uUID;
        }
    }
}
