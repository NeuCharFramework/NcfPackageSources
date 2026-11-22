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
        public SenparcTrace_DateLogModel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public string Date { get; private set; }

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

        private static bool TryValidateDate(string date)
        {
            return DateTime.TryParseExact(
                date,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _);
        }

        private bool IsAdminSession()
        {
            return User?.Identity?.IsAuthenticated == true;
        }

        private static DateTime? ParseTime(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }
    }
}
