/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ContentBox.cs
    File description: ContentBox related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.22.9-preview13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.IO;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Senparc.Ncf.Mvc.UI
{
    public class ContentBox : IDisposable
    {
        // Fields
        private bool _disposed;
        private readonly ViewContext _viewContext;
        private readonly TextWriter _writer;
        private bool _showDefaultTabContainer;

        /// <summary>
        /// Starts writing a backend content-box wrapper to the supplied view.
        /// </summary>
        /// <param name="viewContext">The view context whose writer receives the closing markup.</param>
        /// <param name="showDefaultTabContainer">Whether the default tab container should be closed.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="viewContext"/> is null.</exception>
        public ContentBox(ViewContext viewContext, bool showDefaultTabContainer)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException("viewContext");
            }
            this._viewContext = viewContext;
            this._writer = viewContext.Writer;
            this._showDefaultTabContainer = showDefaultTabContainer;
        }

        /// <summary>
        /// Closes the content-box markup and releases the writer-related state.
        /// </summary>
        public void EndCotnentBox()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Closes the content-box markup and suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Writes closing markup once for the content box.
        /// </summary>
        /// <param name="disposing">Whether managed resources are being released.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                string contentBoxFooter = (_showDefaultTabContainer ? @"
        </div>
        <!-- End #tab1 -->" :"") +
@"
    </div>
    <!-- End .content-box-content -->
</div>
<!-- End .content-box -->";
                this._disposed = true;
                this._writer.Write(contentBoxFooter);
            }
        }
    }
}
