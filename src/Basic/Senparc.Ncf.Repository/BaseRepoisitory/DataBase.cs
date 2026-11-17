/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: DataBase.cs

    File description: DataBase related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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

        public DataBase(INcfDbData baseDB)
        {
            BaseDB = baseDB;
        }

        public virtual void CloseConnection()
        {
            BaseDB.CloseConnection();
        }
    }
}