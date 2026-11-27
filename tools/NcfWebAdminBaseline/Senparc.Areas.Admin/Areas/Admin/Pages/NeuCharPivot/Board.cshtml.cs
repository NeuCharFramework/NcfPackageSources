/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Board.cshtml.cs
    File description: Related implementation.
    Legacy comment translated to English.

    Created by: Senparc - 20260901

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20261101
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.12.15 enhance job management and telemetry features

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.WorkContext.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Areas.Admin.Pages.NeuCharPivot;

[IgnoreAuth]
[AdminAuthorize(BackendJwtAuthorizeAttribute.SuperAdminPolicyName)]
public class BoardModel(
    IServiceProvider serviceProvider,
    NeuCharPivotBoardService boardService,
    IAdminWorkContextProvider adminWorkContextProvider) : BaseAdminPageModel(serviceProvider)
{
    private readonly NeuCharPivotBoardService _boardService = boardService;
    private readonly IAdminWorkContextProvider _adminWorkContextProvider = adminWorkContextProvider;

    /// <summary>
    /// Renders the NeuCharPivot board administration page.
    /// </summary>
    public IActionResult OnGet()
    {
        return Page();
    }

    /// <summary>
    /// Lists all configured boards in ascending identifier order for the board editor.
    /// </summary>
    /// <returns>An HTTP 200 response containing board DTOs.</returns>
    public async Task<IActionResult> OnGetBoardsAsync()
    {
        var boards = await _boardService.GetFullListAsync(
            z => true, z => z.Id, OrderingType.Ascending).ConfigureAwait(false);
        return Ok(boards.Select(board => ToBoardDto(board)));
    }

    /// <summary>
    /// Loads the enabled board associated with a page key and annotates each block
    /// with current module/function availability.
    /// </summary>
    /// <param name="pageKey">The stable page key used to select the board.</param>
    /// <returns>The board DTO, or a JSON null value when no enabled board exists.</returns>
    public async Task<IActionResult> OnGetByPageKeyAsync([FromQuery] string pageKey)
    {
        var board = await _boardService.GetEnabledBoardByPageKeyAsync(pageKey).ConfigureAwait(false);
        if (board == null)
        {
            return Ok(null);
        }

        var blocks = NeuCharPivotBoardService.DeserializeBlocks(board.BlocksJson);
        var availability = await _boardService.GetBlockAvailabilityAsync(blocks).ConfigureAwait(false);
        return Ok(ToBoardDto(board, availability));
    }

    /// <summary>
    /// Lists the available NeuCharPivot block descriptors that can be placed on a board.
    /// </summary>
    /// <returns>An HTTP 200 response containing the block catalog.</returns>
    public async Task<IActionResult> OnGetFunctionsAsync()
    {
        var catalog = await _boardService.GetBlockCatalogAsync().ConfigureAwait(false);
        return Ok(catalog);
    }

    /// <summary>
    /// Creates a board and applies its initial enabled state and descriptive metadata.
    /// </summary>
    /// <param name="request">The board name, page key, layout, and enabled-state request.</param>
    /// <returns>The created board DTO or a localized validation/failure response.</returns>
    public async Task<IActionResult> OnPostCreateAsync([FromBody] BoardSaveRequest request)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.PageKey))
        {
            return Ok(false, "面板名称与页面标识不能为空");
        }

        var adminUserId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
        var board = await _boardService.CreateBoardAsync(
            request.Name, request.PageKey, request.Description, request.Columns, adminUserId)
            .ConfigureAwait(false);
        //Legacy comment translated to English.
        board = await _boardService.UpdateBoardAsync(
            board.Id, board.Name, null, board.Description, board.Columns, request.IsEnabled)
            .ConfigureAwait(false);
        if (board == null)
        {
            return Ok(false, "创建失败");
        }
        return Ok(ToBoardDto(board));
    }

    /// <summary>
    /// Updates an existing board's identity, layout, description, and enabled state.
    /// </summary>
    /// <param name="request">The board identifier and replacement values.</param>
    /// <returns>The updated board DTO or a validation/not-found response.</returns>
    public async Task<IActionResult> OnPostUpdateAsync([FromBody] BoardSaveRequest request)
    {
        if (request == null || request.Id <= 0 || string.IsNullOrWhiteSpace(request.Name))
        {
            return Ok(false, "面板信息无效");
        }

        var board = await _boardService.UpdateBoardAsync(
            request.Id, request.Name, request.PageKey, request.Description, request.Columns, request.IsEnabled)
            .ConfigureAwait(false);
        if (board == null)
        {
            return Ok(false, "面板不存在");
        }
        return Ok(ToBoardDto(board));
    }

    /// <summary>
    /// Deletes the board identified by the request.
    /// </summary>
    /// <param name="request">The board identifier to delete.</param>
    /// <returns>A success response or a validation message.</returns>
    public async Task<IActionResult> OnPostDeleteAsync([FromBody] BoardIdRequest request)
    {
        if (request == null || request.Id <= 0)
        {
            return Ok(false, "面板 Id 无效");
        }
        await _boardService.DeleteObjectAsync(z => z.Id == request.Id).ConfigureAwait(false);
        return Ok(true);
    }

    /// <summary>
    /// Replaces the ordered block collection associated with a board.
    /// </summary>
    /// <param name="request">The board identifier and new block definitions.</param>
    /// <returns>The updated board DTO or the service validation message.</returns>
    public async Task<IActionResult> OnPostSetBlocksAsync([FromBody] BoardBlocksRequest request)
    {
        if (request == null || request.BoardId <= 0)
        {
            return Ok(false, "面板 Id 无效");
        }

        var (success, message, board) = await _boardService.SetBlocksAsync(request.BoardId, request.Blocks)
            .ConfigureAwait(false);
        if (!success)
        {
            return Ok(false, message);
        }
        return Ok(ToBoardDto(board));
    }

    /// <summary>
    /// Uses the configured AI board assistant to apply a natural-language change
    /// request, then returns the assistant reply and resulting board representation.
    /// </summary>
    /// <param name="request">The board identifier and requested change instruction.</param>
    /// <returns>The AI reply and updated board, or the service failure message.</returns>
    public async Task<IActionResult> OnPostAiAsync([FromBody] BoardAiRequest request)
    {
        if (request == null || request.BoardId <= 0 || string.IsNullOrWhiteSpace(request.Instruction))
        {
            return Ok(false, "请输入面板需求描述");
        }

        var adminUserId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
        var (success, message, aiReply, board) = await _boardService.ApplyAiChangeAsync(
            request.BoardId, request.Instruction, adminUserId, HttpContext.RequestAborted).ConfigureAwait(false);
        if (!success)
        {
            //Legacy comment translated to English.
            return Ok(new { aiReply = aiReply ?? string.Empty }, false, message);
        }
        return Ok(new { aiReply = aiReply ?? string.Empty, board = ToBoardDto(board) });
    }

    /// <summary>
    /// Projects a persisted board and its serialized blocks into the JSON shape
    /// consumed by the administration UI.
    /// </summary>
    /// <param name="board">The persisted board entity.</param>
    /// <param name="blockAvailability">Optional availability map keyed by block key.</param>
    /// <returns>An anonymous DTO containing board metadata and block details.</returns>
    private static object ToBoardDto(
        NeuCharPivotBoard board,
        IReadOnlyDictionary<string, bool> blockAvailability = null)
    {
        return new
        {
            board.Id,
            board.PageKey,
            board.Name,
            board.Description,
            board.Columns,
            board.IsEnabled,
            board.AdminUserId,
            board.AddTime,
            board.LastUpdateTime,
            blocks = NeuCharPivotBoardService.DeserializeBlocks(board.BlocksJson)
                .Select(block =>
                {
                    bool? available = null;
                    if (blockAvailability != null)
                    {
                        available = blockAvailability.TryGetValue(
                            NeuCharPivotBoardService.GetBlockKey(block),
                            out var exists) && exists;
                    }

                    return new
                    {
                        block.Key,
                        block.ModuleUid,
                        block.FunctionKey,
                        block.FunctionName,
                        block.Title,
                        block.Summary,
                        block.Accent,
                        block.ExposedParameters,
                        available
                    };
                })
                .ToList()
        };
    }

    public sealed class BoardSaveRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PageKey { get; set; }
        public string Description { get; set; }
        public int Columns { get; set; } = 2;
        public bool IsEnabled { get; set; } = true;
    }

    public sealed class BoardIdRequest
    {
        public int Id { get; set; }
    }

    public sealed class BoardBlocksRequest
    {
        public int BoardId { get; set; }
        public List<NeuCharPivotBoardBlock> Blocks { get; set; } = new();
    }

    public sealed class BoardAiRequest
    {
        public int BoardId { get; set; }
        public string Instruction { get; set; }
    }
}
