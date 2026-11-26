/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DataBase.cs
    File description: DataBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.13-preview12 Updated repository infrastructure and related localization metadata

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;

namespace Senparc.Ncf.Repository
{
    public interface IDataBase
    {
        INcfDbData BaseDB { get; set; }

        void CloseConnection();
    }


    public class DataBase : IDataBase
    {
        public INcfDbData BaseDB { get; set; }

        /// <summary>
        /// Initializes a database lifecycle wrapper around the NCF database abstraction.
        /// </summary>
        /// <param name="baseDB">The database connection and context provider.</param>
        public DataBase(INcfDbData baseDB)
        {
            BaseDB = baseDB;
        }

        /// <summary>
        /// Closes the underlying database connection through <see cref="BaseDB"/>.
        /// </summary>
        public virtual void CloseConnection()
        {
            BaseDB.CloseConnection();
        }
    }
}
