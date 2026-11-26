/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IPData.cs
    File description: IPData related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using Senparc.Ncf.Core.Utility;
using Microsoft.AspNetCore.Http;

#pragma warning disable CS0675 // The legacy IP database format uses bitwise operations on signed values.

namespace Senparc.Ncf.Utility
{
    /// <summary>
    /// Reads location records from a QQWry-format local IP database.
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
        /// The open QQWry database stream.
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
        /// The current location result being populated.
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
        /// The default location returned when the database has no matching record.
        /// </summary>
        #endregion
        private byte[] b4;

        /// <summary>
        /// Opens a QQWry database file and reads its index boundaries.
        /// </summary>
        /// <param name="ipfile">The database path; the application default is used when null.</param>
        /// <exception cref="Exception">Thrown when the database file cannot be opened.</exception>
        public QQWry(string ipfile)
        {
            ipfile = ipfile ?? Server.GetMapPath("~/App_Data/QQWry.Dat");
            buf = new byte[100];
            b3 = new byte[3];
            b4 = new byte[4];
            try
            {
                ipFile = new FileStream(ipfile, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            ipBegin = readLong4(0);
            ipEnd = readLong4(4);
            loc = new IPLocation();
        }

        /// <summary>
        /// Opens the default application QQWry database.
        /// </summary>
        public QQWry()
            : this(null)
        { }

        /// <summary>
        /// Looks up the current remote IP address and returns its database location.
        /// </summary>
        /// <returns>The location associated with the current request IP.</returns>
        public IPLocation SearchIPLocation()
        {
            return SearchIPLocation(GetCurrentIP());
        }

        /// <summary>
        /// Gets the current request's remote IP address as text.
        /// </summary>
        /// <returns>The remote IP address string.</returns>
        public string GetCurrentIP()
        {
            //return HttpContext.Current.Request.UserHostAddress;
            return SenparcHttpContext.Current.Connection.RemoteIpAddress.ToString();
        }

        //search by IP address
        #region Legacy section
        /**/
        /// <summary>
        /// Searches the QQWry index for an IPv4 address and parses the associated country and area.
        /// </summary>
        /// <param name="ip">The dotted IPv4 address to look up.</param>
        /// <returns>The matching location, or an unknown-location object when no record matches.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the address contains an unexpected number of segments.</exception>
        #endregion
        public IPLocation SearchIPLocation(string ip)
        {
            ip = ip ?? "";
            string pattrn = @"(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])";
            if (!Regex.IsMatch(ip, pattrn))
            {
                return new IPLocation() { country = "未知", area = "" };
            }

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
        /// Reads one QQWry location record at an absolute database offset, following
        /// the format's country and area redirection records when present.
        /// </summary>
        /// <param name="offset">The absolute offset of the seven-byte IP record.</param>
        /// <returns>The location parsed from the record.</returns>
        #endregion
        private IPLocation getIPLocation(long offset)
        {
            ipFile.Position = offset + 4;
            byte one = (byte)ipFile.ReadByte();
            if (one == REDIRECT_MODE_1)
            {
                long countryOffset = readLong3();
                ipFile.Position = countryOffset;
                // A second-level redirect stores the country text at another offset.
                byte b = (byte)ipFile.ReadByte();
                if (b == REDIRECT_MODE_2)
                {
                    loc.country = readString(readLong3());
                    ipFile.Position = countryOffset + 4;
                }
                else
                    loc.country = readString(countryOffset);

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
                loc.country = readString(--ipFile.Position);
                loc.area = readString(ipFile.Position);
            }

            loc.country = loc.country.Replace("CZ88.NET", "");
            loc.area = loc.area.Replace("CZ88.NET", "");

            return loc;
        }

        //get region information
        #region Legacy section
        /**/
        /// <summary>
        /// Reads the area portion of a QQWry record, following a redirect when the first byte
        /// indicates one of the supported redirect modes.
        /// </summary>
        /// <param name="offset">The absolute offset at which the area record begins.</param>
        /// <returns>The decoded area text or the unknown-area label.</returns>
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
        /// Reads a null-terminated string from the database using the system default encoding.
        /// </summary>
        /// <param name="offset">The absolute file offset of the string.</param>
        /// <returns>The decoded string, or an empty string for an empty record.</returns>
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
        /// Performs a binary search over the QQWry index and returns the location offset
        /// for the requested four-byte address.
        /// </summary>
        /// <param name="ip">The four-byte address in comparison order.</param>
        /// <returns>The location-record offset, or <c>-1</c> when no record matches.</returns>
        #endregion
        private long locateIP(byte[] ip)
        {
            long m = 0;
            int r;

            readIP(ipBegin, b4);
            r = compareIP(ip, b4);
            if (r == 0)
                return ipBegin;
            else if (r < 0)
                return -1;
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
        /// Reads four bytes from the database and reverses their byte order into an IP address buffer.
        /// </summary>
        /// <param name="offset">The absolute file offset.</param>
        /// <param name="ip">The four-byte destination buffer.</param>
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
        /// Compares two four-byte IP addresses lexicographically.
        /// </summary>
        /// <param name="ip">The first address.</param>
        /// <param name="beginIP">The second address.</param>
        /// <returns>A positive, zero, or negative value according to address ordering.</returns>
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
        /// Compares two unsigned byte values.
        /// </summary>
        /// <param name="bsrc">The first byte.</param>
        /// <param name="bdst">The second byte.</param>
        /// <returns>1 when the first byte is greater, 0 when equal, or -1 when less.</returns>
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
        /// Reads a little-endian four-byte integer from an absolute database offset.
        /// </summary>
        /// <param name="offset">The absolute file offset.</param>
        /// <returns>The decoded unsigned 32-bit value represented as a long.</returns>
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
        /// Reads a little-endian three-byte integer from an absolute database offset.
        /// </summary>
        /// <param name="offset">The absolute file offset.</param>
        /// <returns>The decoded 24-bit value represented as a long.</returns>
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
        /// Reads a little-endian three-byte integer from the current stream position.
        /// </summary>
        /// <returns>The decoded 24-bit value represented as a long.</returns>
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
        /// Calculates a record-aligned midpoint between two index offsets for binary search.
        /// </summary>
        /// <param name="begin">The lower index offset.</param>
        /// <param name="end">The upper index offset.</param>
        /// <returns>A record-aligned midpoint offset.</returns>
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

        /// <summary>
        /// Closes the underlying QQWry database stream.
        /// </summary>
        public void Dispose()
        {
            ipFile.Close();
        }

        #endregion
    }

    /// <summary>
    /// Represents the country and area text returned by a QQWry lookup.
    /// </summary>
    public class IPLocation
    {
        public String country;
        public String area;

        /// <summary>
        /// Initializes an empty location.
        /// </summary>
        public IPLocation()
        {
            country = area = "";
        }

        /// <summary>
        /// Creates a separate location object containing the same country and area values.
        /// </summary>
        /// <returns>A copied location object.</returns>
        public IPLocation getCopy()
        {
            IPLocation ret = new IPLocation();
            ret.country = country;
            ret.area = area;
            return ret;
        }
    }
}
#pragma warning restore CS0675
