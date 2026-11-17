/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IPData.cs
    File description: IPData related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using Senparc.Ncf.Core.Utility;
using Microsoft.AspNetCore.Http;

#pragma warning disable CS0675 //Legacy comment translated to English.

namespace Senparc.Ncf.Utility
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class QQWry : IDisposable
    {
        //mode 1
        #region Legacy section
        /**/
        /// <summary>
        /// mode 1
        /// </summary>
        #endregion
        private const byte REDIRECT_MODE_1 = 0x01;

        //mode 2
        #region Legacy section
        /**/
        /// <summary>
        /// mode 2
        /// </summary>
        #endregion
        private const byte REDIRECT_MODE_2 = 0x02;

        //record length
        #region Legacy section
        /**/
        /// <summary>
        /// record length
        /// </summary>
        #endregion
        private const int IP_RECORD_LENGTH = 7;

        //database file
        #region Legacy section
        /**/
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        #endregion
        private FileStream ipFile;

        private const string unCountry = "未知国家";
        private const string unArea = "未知地区";

        //index start position
        #region Legacy section
        /**/
        /// <summary>
        /// index start position
        /// </summary>
        #endregion
        private long ipBegin;

        //index end position
        #region Legacy section
        /**/
        /// <summary>
        /// index end position
        /// </summary>
        #endregion
        private long ipEnd;

        //IP address object
        #region Legacy section
        /**/
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        #endregion
        private IPLocation loc;

        //store text content
        #region Legacy section
        /**/
        /// <summary>
        /// store text content
        /// </summary>
        #endregion
        private byte[] buf;

        //store three bytes
        #region Legacy section
        /**/
        /// <summary>
        /// store three bytes
        /// </summary>
        #endregion
        private byte[] b3;

        //store four bytes
        #region Legacy section
        /**/
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        #endregion
        private byte[] b4;

        //#region singleton mode - TNT2

        //private static GLJK.Common.QQWry qq = new QQWry();

        ///// <summary>
        ///Legacy comment translated to English.
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


        //constructor
        #region Legacy section
        /**/
        /// <summary>
        /// constructor
        /// </summary>
        ///Legacy comment translated to English.
        #endregion
        public QQWry(string ipfile)
        {
            ipfile = ipfile ?? Server.GetMapPath("~/App_Data/QQWry.Dat");//Legacy comment translated to English.
            buf = new byte[100];
            b3 = new byte[3];
            b4 = new byte[4];
            try
            {
                ipFile = new FileStream(ipfile, FileMode.Open, FileAccess.Read, FileShare.Read);//Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        public IPLocation SearchIPLocation()
        {
            return SearchIPLocation(GetCurrentIP());
        }

        public string GetCurrentIP()
        {
            //return HttpContext.Current.Request.UserHostAddress;
            //Legacy comment translated to English.
            return SenparcHttpContext.Current.Connection.RemoteIpAddress.ToString();
        }

        //search by IP address
        #region Legacy section
        /**/
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        #endregion
        public IPLocation SearchIPLocation(string ip)
        {
            ip = ip ?? "";
            //Legacy comment translated to English.
            string pattrn = @"(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])";
            if (!Regex.IsMatch(ip, pattrn))
            {
                return new IPLocation() { country = "未知", area = "" };
            }

            //Legacy comment translated to English.
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

        //get detailed information
        #region Legacy section
        /**/
        /// <summary>
        /// get detailed information
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        #endregion
        private IPLocation getIPLocation(long offset)
        {
            ipFile.Position = offset + 4;
            //Legacy comment translated to English.
            byte one = (byte)ipFile.ReadByte();
            if (one == REDIRECT_MODE_1)
            {
                //mode 1
                //Legacy comment translated to English.
                long countryOffset = readLong3();
                //Legacy comment translated to English.
                ipFile.Position = countryOffset;
                //Legacy comment translated to English.
                byte b = (byte)ipFile.ReadByte();
                if (b == REDIRECT_MODE_2)
                {
                    loc.country = readString(readLong3());
                    ipFile.Position = countryOffset + 4;
                }
                else
                    loc.country = readString(countryOffset);

                //Legacy comment translated to English.
                loc.area = readArea(ipFile.Position);

            }
            else if (one == REDIRECT_MODE_2)
            {
                //mode 2
                loc.country = readString(readLong3());
                loc.area = readArea(offset + 8);
            }
            else
            {
                //Legacy comment translated to English.
                loc.country = readString(--ipFile.Position);
                loc.area = readString(ipFile.Position);
            }

            loc.country = loc.country.Replace("CZ88.NET", "");//Legacy comment translated to English.
            loc.area = loc.area.Replace("CZ88.NET", "");//Legacy comment translated to English.

            return loc;
        }

        //get region information
        #region Legacy section
        /**/
        /// <summary>
        ///Legacy comment translated to English.
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

        //read a string
        #region Legacy section
        /**/
        /// <summary>
        /// read a string
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

        //find the absolute offset of the IP address
        #region Legacy section
        /**/
        /// <summary>
        /// find the absolute offset of the IP address
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        #endregion
        private long locateIP(byte[] ip)
        {
            long m = 0;
            int r;

            //Legacy comment translated to English.
            readIP(ipBegin, b4);
            r = compareIP(ip, b4);
            if (r == 0)
                return ipBegin;
            else if (r < 0)
                return -1;
            //Legacy comment translated to English.
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

        //read a four-byte IP address
        #region Legacy section
        /**/
        /// <summary>
        ///Legacy comment translated to English.
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

        //compare IP addresses
        #region Legacy section
        /**/
        /// <summary>
        /// compare IP addresses
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="beginIP"></param>
        ///Legacy comment translated to English.
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

        //compare two bytes
        #region Legacy section
        /**/
        /// <summary>
        /// compare two bytes
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

        //read four bytes from the current position
        #region Legacy section
        /**/
        /// <summary>
        ///Legacy comment translated to English.
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

        //read three bytes from the current position
        #region Legacy section
        /**/
        /// <summary>
        /// read three bytes from the current position
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

        //read three bytes from the current position
        #region Legacy section
        /**/
        /// <summary>
        /// read three bytes from the current position
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

        //get the offset between begin and end
        #region Legacy section
        /**/
        /// <summary>
        ///Legacy comment translated to English.
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

        #region Legacy section

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
#pragma warning restore CS0675 //Legacy comment translated to English.
