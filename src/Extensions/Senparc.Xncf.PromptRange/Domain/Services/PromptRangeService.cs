/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptRangeService.cs
    File description: Related implementation.

    Created by: Senparc - 20240105

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20261021
    Change description: v0.20.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.20.8 add SourceFileId, SourceType, and SourceUrl columns to PromptRange table for PostgreSQL, SQL Server, and SQLite

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.PromptRange.Abstractions.Skills;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

namespace Senparc.Xncf.PromptRange.Domain.Services;

public class PromptRangeService : ServiceBase<Models.DatabaseModel.PromptRange>
{
    /// <summary>
    /// Initializes PromptRange persistence and module dependencies.
    /// </summary>
    /// <param name="repo">Repository for PromptRange records.</param>
    /// <param name="serviceProvider">Dependency-injection provider.</param>
    public PromptRangeService(IRepositoryBase<Models.DatabaseModel.PromptRange> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
    {
    }

    // public async Task<PromptRangeDto> UpdateExpectedResultsAsync(int promptRangeId, string expectedResults)
    // {
    //     var promptRange = await this.GetObjectAsync(p => p.Id == promptRangeId) ??
    //Legacy comment translated to English.
    //
    //     promptRange.UpdateExpectedResultsJson(expectedResults);
    //
    //     await this.SaveObjectAsync(promptRange);
    //
    //     return this.TransEntityToDto(promptRange);
    // }


    /// <summary>
    /// Retrieves one PromptRange DTO by identifier.
    /// </summary>
    /// <param name="Id">The PromptRange identifier.</param>
    /// <returns>The mapped PromptRange DTO.</returns>
    /// <exception cref="Exception">Thrown when the range does not exist.</exception>
    public async Task<PromptRangeDto> GetAsync(int Id)
    {
        var promptRange = await this.GetObjectAsync(p => p.Id == Id) ??
                          throw new Exception($"未找到{Id}对应的靶场");

        return this.TransEntityToDto(promptRange);
    }

    /// <summary>
    /// Retrieves all PromptRange records as DTOs.
    /// </summary>
    /// <returns>The PromptRange DTO list.</returns>
    public async Task<List<PromptRangeDto>> GetListAsync()
    {
        var promptRange = await this.GetFullListAsync(p => true);

        return promptRange.Select(TransEntityToDto).ToList();
    }

    /// <summary>
    /// Creates a new PromptRange with a date-based unique range name.
    /// </summary>
    /// <param name="alias">The display alias.</param>
    /// <returns>The persisted PromptRange DTO.</returns>
    public async Task<PromptRangeDto> AddAsync(string alias)
    {
        try
        {
            var today = SystemTime.Now;
            var todayStr = today.ToString("yyyy.MM.dd");

            List<Models.DatabaseModel.PromptRange> todayRangeList = await this.GetFullListAsync(
                p => p.RangeName.StartsWith($"{todayStr}."),
                p => p.Id,
                OrderingType.Descending
            );

            var promptRange = new Models.DatabaseModel.PromptRange($"{todayStr}.{todayRangeList.Count + 1}", alias);

            //promptRange.ChangeAlias(alias);

            await this.SaveObjectAsync(promptRange);

            return this.TransEntityToDto(promptRange);
        }
        catch (Exception ex)
        {
            SenparcTrace.BaseExceptionLog(ex);
            await Console.Out.WriteLineAsync(ex.ToString());
            throw;
        }

    }

    /// <summary>
    /// Changes a PromptRange display alias.
    /// </summary>
    /// <param name="rangeId">The PromptRange identifier.</param>
    /// <param name="alias">The replacement alias.</param>
    /// <returns>The updated DTO.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the range does not exist.</exception>
    public async Task<PromptRangeDto> ChangeAliasAsync(int rangeId, string alias)
    {
        var promptRange = await this.GetObjectAsync(r => r.Id == rangeId)
                          ?? throw new NcfExceptionBase($"没有找到{rangeId}对应的靶场");

        promptRange.ChangeAlias(alias);

        await this.SaveObjectAsync(promptRange);

        return this.TransEntityToDto(promptRange);
    }

    /// <summary>
    /// Changes the semantic kind of a PromptRange.
    /// </summary>
    /// <param name="rangeId">The PromptRange identifier.</param>
    /// <param name="kind">The new PromptRange kind.</param>
    /// <returns>The updated DTO.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the range does not exist.</exception>
    public async Task<PromptRangeDto> ChangeKindAsync(int rangeId, SkillKind kind)
    {
        var promptRange = await this.GetObjectAsync(r => r.Id == rangeId)
                          ?? throw new NcfExceptionBase($"没有找到{rangeId}对应的靶场");

        promptRange.ChangeKind(kind);
        await this.SaveObjectAsync(promptRange);
        return this.TransEntityToDto(promptRange);
    }

    /// <summary>
    /// Updates the external source metadata associated with a PromptRange.
    /// </summary>
    /// <param name="rangeId">The PromptRange identifier.</param>
    /// <param name="sourceFileId">Optional FileManager source file identifier.</param>
    /// <param name="sourceType">The source type label.</param>
    /// <param name="sourceUrl">The source URL.</param>
    /// <returns>The updated DTO.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the range does not exist.</exception>
    public async Task<PromptRangeDto> UpdateSourceAsync(
        int rangeId,
        int? sourceFileId,
        string sourceType,
        string sourceUrl)
    {
        var promptRange = await this.GetObjectAsync(r => r.Id == rangeId)
                          ?? throw new NcfExceptionBase($"没有找到{rangeId}对应的靶场");
        promptRange.UpdateSource(sourceFileId, sourceType, sourceUrl);
        await this.SaveObjectAsync(promptRange);
        return this.TransEntityToDto(promptRange);
    }

    private PromptRangeDto TransEntityToDto(Models.DatabaseModel.PromptRange promptRange)
    {
        return this.Mapper.Map<PromptRangeDto>(promptRange);
    }

    /// <summary>
    /// Deletes a PromptRange and all prompt items belonging to it.
    /// </summary>
    /// <param name="rangeId">The PromptRange identifier.</param>
    /// <returns><see langword="true"/> after the cascade completes.</returns>
    public async Task<bool> DeleteAsync(int rangeId)
    {
        await base.DeleteObjectAsync(p => p.Id == rangeId);

        //Legacy comment translated to English.
        var promptItemService = _serviceProvider.GetService<PromptItemService>();

        await promptItemService.DeleteAllAsync(p => p.RangeId == rangeId);

        return true;
    }
}
