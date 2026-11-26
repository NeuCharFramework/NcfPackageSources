/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DataList.cs
    File description: DataList related implementation

    Created by: Senparc - 20240713

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.3.29 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Senparc.Ncf.UnitTestExtension.Entities
{
    /// <summary>
    /// Groups test data instances by their runtime type and associates the group
    /// with a stable seed-data identifier.
    /// </summary>
    public class DataList : Dictionary<Type, List<object>>
    {
        /// <summary>
        /// Gets or sets the identifier used to avoid executing the same seed-data
        /// registration more than once in a test process.
        /// </summary>
        public string UUID { get; set; }

        /// <summary>
        /// Returns a typed copy of the values stored for <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The runtime type of the requested test data.</typeparam>
        /// <returns>
        /// A new list containing the stored values cast to <typeparamref name="T"/>,
        /// or <see langword="null"/> when no values have been registered for that type.
        /// </returns>
        public List<T> GetDataList<T>()
        {
            if (!base.ContainsKey(typeof(T)))
            {
                return null;
            }

            return base[typeof(T)].Cast<T>().ToList();
        }

        /// <summary>
        /// Returns the object list stored for an exact runtime type.
        /// </summary>
        /// <param name="type">The runtime type used as the dictionary key.</param>
        /// <returns>
        /// The mutable stored list, or <see langword="null"/> when no list exists
        /// for <paramref name="type"/>.
        /// </returns>
        public List<object> GetList(Type type)
        {
            if (!base.ContainsKey(type))
            {
                return null;
            }

            return base[type];
        }


        /// <summary>
        /// Adds all values in a typed list to the corresponding runtime-type bucket.
        /// </summary>
        /// <typeparam name="T">The runtime type represented by the values.</typeparam>
        /// <param name="list">The values to append or register.</param>
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
        /// Adds one value to the bucket associated with its generic type.
        /// </summary>
        /// <typeparam name="T">The runtime type represented by the value.</typeparam>
        /// <param name="item">The value to add.</param>
        public void AddItem<T>(T item)
        {
            Add(new List<T>() { item });
        }

        /// <summary>
        /// Merges another typed data collection into this collection.
        /// </summary>
        /// <param name="dataList">
        /// The source collection. Existing buckets are appended to; new buckets
        /// reference the source list as stored by the existing implementation.
        /// </param>
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
        /// Initializes a data collection with its seed-data identity.
        /// </summary>
        /// <param name="uUID">The identifier used to correlate this seed-data set.</param>
        public DataList(string uUID)
        {
            UUID = uUID;
        }
    }
}
