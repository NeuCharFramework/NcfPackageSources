using System;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using Senparc.Ncf.Core.Utility;
using Microsoft.AspNetCore.Http;

#pragma warning disable CS0675 // Bitwise OR operator used on sign-extended operands

namespace Senparc.Ncf.Utility
{
    /// <summary>
    /// Summary description of QQWry.
    /// </summary>
    public class QQWry : IDisposable
    {
        //first mode
        #region 第一种模式
        /**/
        /// <summary>
        /// first mode
        /// </summary>
        #endregion
        private const byte REDIRECT_MODE_1 = 0x01;

        //Second mode
        #region 第二种模式
        /**/
        /// <summary>
        /// Second mode
        /// </summary>
        #endregion
        private const byte REDIRECT_MODE_2 = 0x02;

        //length of each record
        #region 每条记录长度
        /**/
        /// <summary>
        /// Length of each record
        /// </summary>
        #endregion
        private const int IP_RECORD_LENGTH = 7;

        //database file
        #region 数据库文件
        /**/
        /// <summary>
        ///file object
        /// </summary>
        #endregion
        private FileStream ipFile;

        private const string unCountry = "未知国家";
        private const string unArea = "未知地区";

        //index start position
        #region 索引开始位置
        /**/
        /// <summary>
        /// Index starting position
        /// </summary>
        #endregion
        private long ipBegin;

        //index end position
        #region 索引结束位置
        /**/
        /// <summary>
        /// index end position
        /// </summary>
        #endregion
        private long ipEnd;

        //IP address object
        #region IP地址对象
        /**/
        /// <summary>
        ///IP object
        /// </summary>
        #endregion
        private IPLocation loc;

        //Store text content
        #region 存储文本内容
        /**/
        /// <summary>
        /// store text content
        /// </summary>
        #endregion
        private byte[] buf;

        //Store 3 bytes
        #region 存储3字节
        /**/
        /// <summary>
        /// store 3 bytes
        /// </summary>
        #endregion
        private byte[] b3;

        //Store 4 bytes
        #region 存储4字节
        /**/
        /// <summary>
        /// stores 4-byte IP address
        /// </summary>
        #endregion
        private byte[] b4;

        //#region Singleton Mode - TNT2

        //private static GLJK.Common.QQWry qq = new QQWry();

        ///// <summary>
        ///// Used to lock single-process access
        ///// </summary>
        //private static readonly object objLock = new object();

        //public static QQWry GetInstance()
        //{
        //    if (qq == null)
        //    {
        //        lock (objLock)
        //        {
        //            if (qq == null)
        //            {
        //                qq = new QQWry();
        //            }
        //        }
        //    }
        //    else
        //    {
        //        qq.Dispose();
        //    }
        //    return qq;
        //}
        //#endregion


        //Constructor
        #region 构造函数
        /**/
        /// <summary>
        ///Constructor
        /// </summary>
        /// <param name="ipfile">IP database file absolute path</param>
        #endregion
        public QQWry(string ipfile)
        {
            ipfile = ipfile ?? Server.GetMapPath("~/App_Data/QQWry.Dat");//Database address
            buf = new byte[100];
            b3 = new byte[3];
            b4 = new byte[4];
            try
            {
                ipFile = new FileStream(ipfile, FileMode.Open, FileAccess.Read, FileShare.Read);//FileAccess.Read,,FileShare.Read is added after
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            ipBegin = readLong4(0);
            ipEnd = readLong4(4);
            loc = new IPLocation();
        }

        public QQWry()
            : this(null)
        { }

        /// <summary>
        ///Search based on client IP address
        /// </summary>
        /// <returns></returns>
        public IPLocation SearchIPLocation()
        {
            return SearchIPLocation(GetCurrentIP());
        }

        public string GetCurrentIP()
        {
            //return HttpContext.Current.Request.UserHostAddress;
            //COCONET can also refer to: https://blog.csdn.net/yzj_xiaoyue/article/details/79200714
            return SenparcHttpContext.Current.Connection.RemoteIpAddress.ToString();
        }

        //Search by IP address
        #region 根据IP地址搜索
        /**/
        /// <summary>
        ///Search IP address search
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        #endregion
        public IPLocation SearchIPLocation(string ip)
        {
            ip = ip ?? "";
            //Verify IP legitimacy
            string pattrn = @"(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])";
            if (!Regex.IsMatch(ip, pattrn))
            {
                return new IPLocation() { country = "未知", area = "" };
            }

            //Convert character IP to bytes
            string[] ipSp = ip.Split('.');
            if (ipSp.Length != 4)
            {
                throw new ArgumentOutOfRangeException("不是合法的IP地址!");
            }
            byte[] IP = new byte[4];
            for (int i = 0; i < IP.Length; i++)
            {
                IP[i] = (byte)(Int32.Parse(ipSp[i]) & 0xFF);
            }

            IPLocation local = null;
            long offset = locateIP(IP);

            if (offset != -1)
            {
                local = getIPLocation(offset);
            }

            if (local == null)
            {
                local = new IPLocation();
                local.area = unArea;
                local.country = unCountry;
            }
            return local;
        }

        //Get specific information
        #region 取得具体信息
        /**/
        /// <summary>
        ///Get specific information
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        #endregion
        private IPLocation getIPLocation(long offset)
        {
            ipFile.Position = offset + 4;
            //Read the first byte to determine whether it is a flag byte
            byte one = (byte)ipFile.ReadByte();
            if (one == REDIRECT_MODE_1)
            {
                //first mode
                //Read country offset
                long countryOffset = readLong3();
                //Go to offset
                ipFile.Position = countryOffset;
                //Check the flag byte again
                byte b = (byte)ipFile.ReadByte();
                if (b == REDIRECT_MODE_2)
                {
                    loc.country = readString(readLong3());
                    ipFile.Position = countryOffset + 4;
                }
                else
                    loc.country = readString(countryOffset);

                //Read area flag
                loc.area = readArea(ipFile.Position);

            }
            else if (one == REDIRECT_MODE_2)
            {
                //Second mode
                loc.country = readString(readLong3());
                loc.area = readArea(offset + 8);
            }
            else
            {
                //Normal mode
                loc.country = readString(--ipFile.Position);
                loc.area = readString(ipFile.Position);
            }

            loc.country = loc.country.Replace("CZ88.NET", "");//Replace By TNT2
            loc.area = loc.area.Replace("CZ88.NET", "");//Replace By TNT2

            return loc;
        }

        //Get area information
        #region 取得地区信息
        /**/
        /// <summary>
        ///Read region name
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        #endregion
        private string readArea(long offset)
        {
            ipFile.Position = offset;
            byte one = (byte)ipFile.ReadByte();
            if (one == REDIRECT_MODE_1 || one == REDIRECT_MODE_2)
            {
                long areaOffset = readLong3(offset + 1);
                if (areaOffset == 0)
                    return unArea;
                else
                {
                    return readString(areaOffset);
                }
            }
            else
            {
                return readString(offset);
            }
        }

        //Read string
        #region 读取字符串
        /**/
        /// <summary>
        /// read string
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        #endregion
        private string readString(long offset)
        {
            ipFile.Position = offset;
            int i = 0;
            for (i = 0, buf[i] = (byte)ipFile.ReadByte(); buf[i] != (byte)(0); buf[++i] = (byte)ipFile.ReadByte()) ;

            if (i > 0)
                return Encoding.Default.GetString(buf, 0, i);
            else
                return "";
        }

        //Find the absolute offset at which an IP address is located
        #region 查找IP地址所在的绝对偏移量
        /**/
        /// <summary>
        /// Find the absolute offset at which the IP address is located
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        #endregion
        private long locateIP(byte[] ip)
        {
            long m = 0;
            int r;

            //Compare the first IP entry
            readIP(ipBegin, b4);
            r = compareIP(ip, b4);
            if (r == 0)
                return ipBegin;
            else if (r < 0)
                return -1;
            //Start binary search
            for (long i = ipBegin, j = ipEnd; i < j; )
            {
                m = this.getMiddleOffset(i, j);
                readIP(m, b4);
                r = compareIP(ip, b4);
                if (r > 0)
                    i = m;
                else if (r < 0)
                {
                    if (m == j)
                    {
                        j -= IP_RECORD_LENGTH;
                        m = j;
                    }
                    else
                    {
                        j = m;
                    }
                }
                else
                    return readLong3(m + 4);
            }
            m = readLong3(m + 4);
            readIP(m, b4);
            r = compareIP(ip, b4);
            if (r <= 0)
                return m;
            else
                return -1;
        }

        //Read out the 4-byte IP address
        #region 读出4字节的IP地址
        /**/
        /// <summary>
        /// Read four bytes from the current location, these four bytes are the IP address
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="ip"></param>
        #endregion
        private void readIP(long offset, byte[] ip)
        {
            ipFile.Position = offset;
            ipFile.Read(ip, 0, ip.Length);
            byte tmp = ip[0];
            ip[0] = ip[3];
            ip[3] = tmp;
            tmp = ip[1];
            ip[1] = ip[2];
            ip[2] = tmp;
        }

        //Compare IP addresses to see if they are the same
        #region 比较IP地址是否相同
        /**/
        /// <summary>
        /// Compare IP addresses to see if they are the same
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="beginIP"></param>
        /// <returns>0: equal, 1: ip is greater than beginIP, -1: less than </returns>
        #endregion
        private int compareIP(byte[] ip, byte[] beginIP)
        {
            for (int i = 0; i < 4; i++)
            {
                int r = compareByte(ip[i], beginIP[i]);
                if (r != 0)
                    return r;
            }
            return 0;
        }

        //Compare two bytes for equality
        #region 比较两个字节是否相等
        /**/
        /// <summary>
        /// Compare two bytes to see if they are equal
        /// </summary>
        /// <param name="bsrc"></param>
        /// <param name="bdst"></param>
        /// <returns></returns>
        #endregion
        private int compareByte(byte bsrc, byte bdst)
        {
            if ((bsrc & 0xFF) > (bdst & 0xFF))
                return 1;
            else if ((bsrc ^ bdst) == 0)
                return 0;
            else
                return -1;
        }

        //Read 4 bytes based on the current position
        #region 根据当前位置读取4字节
        /**/
        /// <summary>
        /// Read 4 bytes from the current position and convert to long integer
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        #endregion
        private long readLong4(long offset)
        {
            long ret = 0;
            ipFile.Position = offset;
            ret |= (ipFile.ReadByte() & 0xFF);
            ret |= ((ipFile.ReadByte() << 8) & 0xFF00);
            ret |= ((ipFile.ReadByte() << 16) & 0xFF0000);
            ret |= ((ipFile.ReadByte() << 24) & 0xFF000000);
            return ret;
        }

        //According to the current position, read 3 bytes
        #region 根据当前位置,读取3字节
        /**/
        /// <summary>
        /// According to the current position, read 3 bytes
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        #endregion
        private long readLong3(long offset)
        {
            long ret = 0;
            ipFile.Position = offset;
            ret |= (ipFile.ReadByte() & 0xFF);
            ret |= ((ipFile.ReadByte() << 8) & 0xFF00);
            ret |= ((ipFile.ReadByte() << 16) & 0xFF0000);
            return ret;
        }

        //Read 3 bytes from current position
        #region 从当前位置读取3字节
        /**/
        /// <summary>
        /// Read 3 bytes from the current position
        /// </summary>
        /// <returns></returns>
        #endregion
        private long readLong3()
        {
            long ret = 0;
            ret |= (ipFile.ReadByte() & 0xFF);
            ret |= ((ipFile.ReadByte() << 8) & 0xFF00);
            ret |= ((ipFile.ReadByte() << 16) & 0xFF0000);
            return ret;
        }

        //Get the offset between begin and end
        #region 取得begin和end之间的偏移量
        /**/
        /// <summary>
        /// Get the offset between begin and end
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        #endregion
        private long getMiddleOffset(long begin, long end)
        {
            long records = (end - begin) / IP_RECORD_LENGTH;
            records >>= 1;
            if (records == 0)
                records = 1;
            return begin + records * IP_RECORD_LENGTH;
        }

        #region IDisposable 成员

        public void Dispose()
        {
            ipFile.Close();
        }

        #endregion
    }

    public class IPLocation
    {
        public String country;
        public String area;

        public IPLocation()
        {
            country = area = "";
        }

        public IPLocation getCopy()
        {
            IPLocation ret = new IPLocation();
            ret.country = country;
            ret.area = area;
            return ret;
        }
    }
}
#pragma warning restore CS0675 // Bitwise OR operator used on sign-extended operands
