/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: CheckCodeResult.cs
    File description: CheckCodeResult related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

----------------------------------------------------------------*/

using System;
using System.Drawing;
using Senparc.Core.Utility;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Utility;

namespace Senparc.Mvc
{
    public class CheckCodeResult : ActionResult
    {
        private CheckCodeKind _checkCodeKind;

        public CheckCodeResult(CheckCodeKind checkCodeKind)
        {
            _checkCodeKind = checkCodeKind;
        }

        public override void ExecuteResult(ActionContext context)
        {
            CheckCodeHandle checkcode = new CheckCodeHandle(_checkCodeKind, context.HttpContext);
            createcheckcodeimage(checkcode.GenerateCheckCode(), context);
        }


        private void createcheckcodeimage(string checkcode, ActionContext context)
        {
            VerifyCode v = new VerifyCode();
            v.CreateImageOnPage(checkcode, context);        //Legacy comment translated to English.
        }
    }
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class VerifyCode
    {
        #region Legacy section
        int fontSize = 40;
        public int FontSize
        {
            get { return fontSize; }
            set { fontSize = value; }
        }
        #endregion

        #region Legacy section
        int padding = 2;
        public int Padding
        {
            get { return padding; }
            set { padding = value; }
        }
        #endregion

        #region whether to output noise(disabled by default)
        bool chaos = true;
        public bool Chaos
        {
            get { return chaos; }
            set { chaos = value; }
        }
        #endregion

        #region Legacy section
        System.Drawing.Color chaosColor = System.Drawing.Color.LightGray;
        public System.Drawing.Color ChaosColor
        {
            get { return chaosColor; }
            set { chaosColor = value; }
        }
        #endregion

        #region Legacy section
        System.Drawing.Color backgroundColor = System.Drawing.Color.White;
        public System.Drawing.Color BackgroundColor
        {
            get { return backgroundColor; }
            set { backgroundColor = value; }
        }
        #endregion

        #region Legacy section
        Color[] colors = { Color.Black, Color.Red, Color.DarkBlue, Color.Green, Color.Orange, Color.Brown, Color.DarkCyan, Color.Purple };
        public Color[] Colors
        {
            get { return colors; }
            set { colors = value; }
        }
        #endregion

        #region Legacy section
        string[] fonts = { "Arial", "Georgia" };
        public string[] Fonts
        {
            get { return fonts; }
            set { fonts = value; }
        }
        #endregion

        #region Legacy section

        private const double PI = 3.1415926535897932384626433832795;
        private const double PI2 = 6.283185307179586476925286766559;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
        public System.Drawing.Bitmap TwistImage(Bitmap srcBmp, bool bXDir, double dMultValue, double dPhase)
        {
            Bitmap destBmp = new Bitmap(srcBmp.Width, srcBmp.Height);

            //Legacy comment translated to English.
            Graphics graph = Graphics.FromImage(destBmp);
            graph.FillRectangle(new SolidBrush(System.Drawing.Color.White), 0, 0, destBmp.Width, destBmp.Height);
            graph.Dispose();

            double dBaseAxisLen = bXDir ? (double)destBmp.Height : (double)destBmp.Width;

            for (int i = 0; i < destBmp.Width; i++)
            {
                for (int j = 0; j < destBmp.Height; j++)
                {
                    double dx = 0;
                    dx = bXDir ? (PI2 * (double)j) / dBaseAxisLen : (PI2 * (double)i) / dBaseAxisLen;
                    dx += dPhase;
                    double dy = Math.Sin(dx);

                    //Legacy comment translated to English.
                    int nOldX = 0, nOldY = 0;
                    nOldX = bXDir ? i + (int)(dy * dMultValue) : i;
                    nOldY = bXDir ? j : j + (int)(dy * dMultValue);

                    System.Drawing.Color color = srcBmp.GetPixel(i, j);
                    if (nOldX >= 0 && nOldX < destBmp.Width
                     && nOldY >= 0 && nOldY < destBmp.Height)
                    {
                        destBmp.SetPixel(nOldX, nOldY, color);
                    }
                }
            }

            return destBmp;
        }



        #endregion

        #region Legacy section
        public Bitmap CreateImageCode(string code)
        {
            int fSize = FontSize;
            int fWidth = fSize + Padding;

            int imageWidth = (int)(code.Length * fWidth) + 4 + Padding * 2;
            int imageHeight = fSize * 2 + Padding;

            Bitmap image = new Bitmap(imageWidth, imageHeight);

            Graphics g = Graphics.FromImage(image);

            g.Clear(BackgroundColor);

            Random rand = new Random();

            //Legacy comment translated to English.
            if (this.Chaos)
            {

                Pen pen = new Pen(ChaosColor, 0);
                int c = 4 * 10;

                for (int i = 0; i < c; i++)
                {
                    int x = rand.Next(image.Width);
                    int y = rand.Next(image.Height);

                    g.DrawRectangle(pen, x, y, 1, 1);
                }
            }

            int left = 0, top = 0, top1 = 1, top2 = 1;

            int n1 = (imageHeight - FontSize - Padding * 2);
            int n2 = n1 / 4;
            top1 = n2;
            top2 = n2 * 2;

            Font f;
            Brush b;

            int cindex, findex;

            //Legacy comment translated to English.
            for (int i = 0; i < code.Length; i++)
            {
                cindex = rand.Next(Colors.Length - 1);
                findex = rand.Next(Fonts.Length - 1);

                f = new System.Drawing.Font(Fonts[findex], fSize, System.Drawing.FontStyle.Bold);
                b = new System.Drawing.SolidBrush(Colors[cindex]);

                if (i % 2 == 1)
                {
                    top = top2;
                }
                else
                {
                    top = top1;
                }

                left = i * fWidth;

                g.DrawString(code.Substring(i, 1), f, b, left, top);
            }

            //Legacy comment translated to English.
            g.DrawRectangle(new Pen(Color.Gainsboro, 0), 0, 0, image.Width - 1, image.Height - 1);
            g.Dispose();

            //Legacy comment translated to English.
            image = TwistImage(image, true, 8, 4);

            return image;
        }
        #endregion

        #region Legacy section
        public void CreateImageOnPage(string code, ActionContext context)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            Bitmap image = this.CreateImageCode(code);

            image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

            context.HttpContext.Response.Body.Seek(0, System.IO.SeekOrigin.Begin);//Response.ClearContent(); 
            context.HttpContext.Response.ContentType = "image/Jpeg";
            context.HttpContext.Response.Body.Write(ms.GetBuffer());// context.HttpContext.Response.BinaryWrite(ms.GetBuffer());


            ms.Close();
            ms = null;
            image.Dispose();
            image = null;
        }
        #endregion
    }
}
