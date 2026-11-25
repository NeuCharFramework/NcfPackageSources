/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ShowMessageBox.cs
    File description: ShowMessageBox related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.22.9-preview13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System.IO;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Senparc.Ncf.Core.Enums;

namespace System.Web.Mvc
{
    public static class ShowMessageBoxExtension
    {
        /// <summary>
        /// Starts a message-box writer for the current MVC view context.
        /// </summary>
        /// <param name="helper">The HTML helper whose view context receives markup.</param>
        /// <param name="messageType">The message category used to select the CSS class.</param>
        /// <returns>A disposable message-box writer.</returns>
        public static ShowMessageBox ShowMessageBox(this HtmlHelper helper, MessageType messageType)
        {
            return new ShowMessageBox(helper.ViewContext, messageType);
        }
    }
    public class ShowMessageBox : IDisposable
    {
        // Fields
        private bool _disposed;
        private readonly ViewContext _viewContext;
        private readonly TextWriter _writer;


        public MessageType MessageType { get; set; }
        public string Message { get; set; }

        /// <summary>
        /// Starts writing a message box with the supplied message category.
        /// </summary>
        /// <param name="viewContext">The view context whose writer receives markup.</param>
        /// <param name="messageType">The message category rendered in the opening markup.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="viewContext"/> is null.</exception>
        public ShowMessageBox(ViewContext viewContext, MessageType messageType)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException("viewContext");
            }
            this._viewContext = viewContext;
            this._writer = viewContext.Writer;

            this._writer.Write(@"
<div class=""notification {0} png_bg"">
            <a href=""#"" class=""close"">
                <img src=""/Content/backend/images/icons/cross_grey_small.png"" title=""关闭此提示""
                    alt=""close""></a>
            <div>",messageType.ToString());
        }


        /// <summary>
        /// Closes the message-box section.
        /// </summary>
        public void EndSection()
        {
            this.Dispose();
        }

        /// <summary>
        /// Closes the message-box section and suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Writes the closing message-box markup once.
        /// </summary>
        /// <param name="disposing">Whether managed resources are being released.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                this._disposed = true;
                this._writer.Write(@"</div></div>");
            }
        }
    }
}
