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