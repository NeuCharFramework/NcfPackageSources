using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.SemanticKernel.Memory;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.FileManager.Domain.Services;
using Senparc.Xncf.KnowledgeBase.OHS.Local.PL;
using MemoryRecord = Microsoft.KernelMemory.MemoryStorage.MemoryRecord;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.AppService;

public class KnowledgeBaseService: AppServiceBase
{
    //private readonly IKernelMemory _kernelMemory;
    private readonly IKernelMemory _kernelMemory;
    private readonly NcfFileService _ncfFileService;
    private readonly IMemoryDb _memoryDb;

    public KnowledgeBaseService(
        IServiceProvider serviceProvider, 
        //IKernelMemory kernelMemory,
        IKernelMemory kernelMemory,
        IMemoryDb memoryDb,
        NcfFileService ncfFileService) : 
        base(serviceProvider)
    {
        //_kernelMemory = kernelMemory;
        _kernelMemory = kernelMemory;
        _memoryDb = memoryDb;
        _ncfFileService = ncfFileService;
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase> CreateCollectionAsync(CreateCollectionRequest request)
    {
        await _memoryDb.CreateIndexAsync(request.CollectionName, request.VectorSize);
        return new AppResponseBase();
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<bool>> DeleteCollectionAsync(DeleteCollectionRequest request)
    {
        return await this.GetResponseAsync<bool>(async (response, logger) =>
        {
            await _memoryDb.DeleteIndexAsync(request.CollectionName);
            return true;
        });
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public async Task<AppResponseBase<ListCollectionsResponse>> ListCollectionsAsync()
    {
        return await this.GetResponseAsync<ListCollectionsResponse>(async (response, logger) =>
        {
            var collections = await _memoryDb.GetIndexesAsync();
            return new ListCollectionsResponse(collections);
        });
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<ImportTextResponse>> ImportText(ImportTextRequest request)
    {
        return await this.GetResponseAsync<ImportTextResponse>(async (response, logger) =>
        {
            var documentId = await _kernelMemory.ImportTextAsync(
                request.Text, 
                documentId: Guid.NewGuid().ToString(), 
                index: request.CollectionName);
            return new ImportTextResponse(documentId);
        });
    }
    
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<ImportTextResponse>> ImportDocument(IFormFile file, [FromForm]string collectionName)
    {
        return await this.GetResponseAsync<ImportTextResponse>(async (response, logger) =>
        {
            var uploadedFile = await _ncfFileService.UploadFile(file);
            var documentId = await _kernelMemory.ImportDocumentAsync(
                uploadedFile.FilePath, 
                documentId: Guid.NewGuid().ToString(), 
                index: collectionName);
            return new ImportTextResponse(uploadedFile.FilePath);
        });
    }
    
    
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<bool>> DeleteChunkAsync(DeleteChunkRequest request)
    {
        return await this.GetResponseAsync<bool>(async (response, logger) =>
        {
            await _memoryDb.DeleteAsync(request.CollectionName, new MemoryRecord() { Id = request.Id });
            return true;
        });
    }
    
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<ListChunksResponse>> ListChunksAsync(ListChunksRequest request)
    {
        return await this.GetResponseAsync<ListChunksResponse>(async (response, logger) =>
        {
            var items = await _memoryDb.GetListAsync(request.CollectionName, limit: Int32.MaxValue)
                .Select(item => new ChunkDto(item.Id, item.GetPartitionText(), item.Tags))
                .ToListAsync();

            return new ListChunksResponse(items);
        });
    }
    
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<SearchResponse>> SearchAsync(SearchRequest request)
    {
        return await this.GetResponseAsync<SearchResponse>(async (response, logger) =>
        {
            var items = new List<QueryChunkDto>();
            await foreach(var item in _memoryDb.GetSimilarListAsync(
                request.CollectionName, 
                request.Query, 
                null, 
                request.MinRelevance, 
                request.Limit))
            {
                items.Add(new QueryChunkDto(
                    item.Item1.Id,
                    item.Item1.GetPartitionText(),
                    item.Item1.Tags, 
                    item.Item2));
            }

            return new SearchResponse(items);
        });
    }
}