/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: PicHelper.Core.cs

    File description: PicHelper.Core related implementation

    

    

    Created by: Senparc - 20211124

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

namespace Senparc.ImageUtility
{
    public partial class PicHelper
    {
        //public static void Watermark(string picPath, Stream stream, string markText, int floatX, int floatY, Color color, float fontsize = 14.0f)
        //{
        //    string filepath = System.Web.HttpContext.Current.Server.MapPath(picPath);

        //    System.Drawing.Image myimage = System.Drawing.Image.FromFile(filepath);
        //    //createImage object
        //    Bitmap mybitmap = new Bitmap(myimage,/* 200, 200);*/myimage.Width, myimage.Height);
        //    //createBitmap object

        //    Graphics g = Graphics.FromImage(mybitmap);
        //    // Create a Graphics object

        //    //float fontsize = 8.0f;//Font size
        //    float fongwidth = fontsize * (fontsize * 4);//23;//Total width of the font
        //    float fontheight = fontsize + 100;// Font height
        //    Font myfont = new Font("SimSun", fontsize, FontStyle.Bold);
        //    // Define a Font text style, can be written below

        //    RectangleF Rf = new RectangleF(floatX, floatY, fongwidth, fontheight);

        //    // Create a group of rectangles. Note the RectangleF here. The F table here indicates that it stores a group of Float data. If it is an integer, you can directly use Rectangle.
        //    Brush thisbrush = new SolidBrush(color);
        //    // Define a Brush, and the style of the brush is SolidBrush, and the color of the brush is #ff00cc.

        //    g.DrawString(markText, myfont, thisbrush, Rf);
        //    //Drawn
        //    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

        //    //MemoryStream ms = new MemoryStream();
        //    //mybitmap.Save(Server.MapPath("index2.jpg"), ImageFormat.Jpeg);
        //    mybitmap.Save(stream, ImageFormat.Jpeg);
        //    stream.Position = 0;
        //    //HttpContext.Current.Response.Clear();
        //    //HttpContext.Current.Response.ContentType = "image/jpeg";
        //    //HttpContext.Current.Response.BinaryWrite(ms.ToArray());
        //}
    }
}
