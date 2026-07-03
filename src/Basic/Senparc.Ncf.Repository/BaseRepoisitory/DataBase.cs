/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DataBase.cs
    文件功能描述：DataBase 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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