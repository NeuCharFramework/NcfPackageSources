using Senparc.Ncf.Core.Models;

namespace Senparc.Ncf.Repository
{
    public interface IDataBase
    {
        ISqlBaseFinanceData BaseDB { get; set; }

        void CloseConnection();
    }


    public class DataBase : IDataBase
    {
        public ISqlBaseFinanceData BaseDB { get; set; }

        public DataBase(ISqlBaseFinanceData baseDB)
        {
            BaseDB = baseDB;
        }

        public virtual void CloseConnection()
        {
            BaseDB.CloseConnection();
        }
    }
}