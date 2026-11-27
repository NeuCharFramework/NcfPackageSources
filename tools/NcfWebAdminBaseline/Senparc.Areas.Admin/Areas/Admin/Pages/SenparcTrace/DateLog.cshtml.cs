/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: DateLog.cshtml.cs
    File description: Provides d atel og.cshtml functionality.

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261124
    Change description: v0.12.29 Improved AdminChat, system log analysis, workflow commands, and workspace asset operations.

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.SenparcTraceManager;
using Senparc.Ncf.AreaBase.Admin.Filters;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
[IgnoreAuth]
[AllowAnonymous]
    public class SenparcTrace_DateLogModel : BaseAdminPageModel
    {
        /// <summary>
        /// Initializes the trace date-log page model with the request service provider.
        /// </summary>
        /// <param name="serviceProvider">The services used by the base page model.</param>
        public SenparcTrace_DateLogModel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public string Date { get; private set; }

        /// <summary>
        /// Renders the log page for one validated <c>yyyyMMdd</c> date.
        /// </summary>
        /// <param name="date">The log date in invariant <c>yyyyMMdd</c> format.</param>
        /// <returns>The date-log page, challenge, or HTTP 400 for an invalid date.</returns>
        public IActionResult OnGet(string date)
        {
            if (!IsAdminSession())
            {
                return Challenge();
            }
            if (!TryValidateDate(date))
            {
                return BadRequest();
            }
            Date = date;
            Response.Headers.CacheControl = "no-store";
            return Page();
        }

        /// <summary>
        /// Returns the first page of entries for a validated trace-log date.
        /// </summary>
        /// <param name="date">The log date in invariant <c>yyyyMMdd</c> format.</param>
        /// <returns>Log entries, HTTP 400, 401, or 404 depending on the failure.</returns>
        public async Task<IActionResult> OnGetDetailAsync(string date)
        {
            if (!IsAdminSession())
            {
                return Unauthorized();
            }
            try
            {
                var result = await SenparcTraceHelper.QueryLogsAsync(
                    base._serviceProvider, date, pageIndex: 1, pageSize: 100);
                Response.Headers.CacheControl = "no-store";
                return Ok(result);
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Queries a trace-log date with paging, text, type, exception, thread,
        /// and optional time-range filters.
        /// </summary>
        /// <param name="date">The log date in invariant <c>yyyyMMdd</c> format.</param>
        /// <param name="pageIndex">The one-based result page.</param>
        /// <param name="pageSize">The requested page size.</param>
        /// <param name="keyword">Optional text filter.</param>
        /// <param name="traceType">Optional trace type filter.</param>
        /// <param name="isException">Optional exception-presence filter.</param>
        /// <param name="threadId">Optional thread identifier filter.</param>
        /// <param name="startTime">Optional round-trip start time.</param>
        /// <param name="endTime">Optional round-trip end time.</param>
        /// <returns>The filtered log page or an HTTP validation/authentication/not-found response.</returns>
        public async Task<IActionResult> OnGetQueryAsync(
            string date,
            int pageIndex = 1,
            int pageSize = 20,
            string keyword = null,
            SenparcTraceType? traceType = null,
            bool? isException = null,
            int? threadId = null,
            string startTime = null,
            string endTime = null)
        {
            if (!IsAdminSession())
            {
                return Unauthorized();
            }
            try
            {
                var result = await SenparcTraceHelper.QueryLogsAsync(
                    base._serviceProvider,
                    date,
                    pageIndex,
                    pageSize,
                    keyword,
                    traceType,
                    isException,
                    threadId,
                    ParseTime(startTime),
                    ParseTime(endTime));
                Response.Headers.CacheControl = "no-store";
                return Ok(result);
            }
            catch (FormatException)
            {
                return BadRequest();
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Returns one trace-log entry by its source line number.
        /// </summary>
        /// <param name="date">The log date in invariant <c>yyyyMMdd</c> format.</param>
        /// <param name="line">The line number used by the log reader.</param>
        /// <returns>The entry or an HTTP 400, 401, or 404 response.</returns>
        public async Task<IActionResult> OnGetEntryAsync(string date, int line)
        {
            if (!IsAdminSession())
            {
                return Unauthorized();
            }
            try
            {
                var result = await SenparcTraceHelper.GetLogDetailAsync(base._serviceProvider, date, line);
                Response.Headers.CacheControl = "no-store";
                return Ok(result);
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Validates that a date exactly follows the trace-log <c>yyyyMMdd</c> format.
        /// </summary>
        /// <param name="date">The candidate date text.</param>
        /// <returns><see langword="true"/> when the text represents a valid date.</returns>
        private static bool TryValidateDate(string date)
        {
            return DateTime.TryParseExact(
                date,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _);
        }

        /// <summary>
        /// Checks whether the current request has an authenticated principal.
        /// </summary>
        /// <returns><see langword="true"/> when the current identity is authenticated.</returns>
        private bool IsAdminSession()
        {
            return User?.Identity?.IsAuthenticated == true;
        }

        /// <summary>
        /// Parses an optional invariant round-trip timestamp used by trace filtering.
        /// </summary>
        /// <param name="value">The timestamp text, or blank for no bound.</param>
        /// <returns>The parsed timestamp, or <see langword="null"/> for blank input.</returns>
        /// <exception cref="FormatException">Thrown when a non-blank value is not a valid timestamp.</exception>
        private static DateTime? ParseTime(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }
    }
}
