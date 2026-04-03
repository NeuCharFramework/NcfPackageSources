using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Text;
using OllamaSharp.Models;
using Senparc.AI;
using Senparc.AI.Entities.Keys;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.FileManager.Domain.Services;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto;
using Senparc.Xncf.KnowledgeBase.OHS.Local.PL.Response;
using Senparc.Xncf.KnowledgeBase.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Senparc.Xncf.KnowledgeBase.Domain.Services
{
    public class KnowledgeBaseService : ServiceBase<Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.KnowledgeBase>
    {
        private readonly KnowledgeBaseItemService _knowledgeBaseDetailService;
        private readonly NcfFileService _ncfFileService;
        private readonly AIModelService _aIModelService;

        public KnowledgeBaseService(
            RepositoryBase<KnowledgeBase.Models.DatabaseModel.KnowledgeBase> repo,
            KnowledgeBaseItemService knowledgeBasesDetailService,
            NcfFileService ncfFileService,
            AIModelService aIModelService,
            IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
            _knowledgeBaseDetailService = knowledgeBasesDetailService;
            _ncfFileService = ncfFileService;
            _aIModelService = aIModelService;
        }

        public async Task<IEnumerable<KnowledgeBaseDto>> GetKnowledgeBasesList(int PageIndex, int PageSize)
        {
            List<KnowledgeBaseDto> selectListItems = null;
            List<KnowledgeBase.Models.DatabaseModel.KnowledgeBase> knowledgeBases =
                (await GetFullListAsync(_ => true).ConfigureAwait(false))
                .OrderByDescending(_ => _.AddTime)
                .ToList();
            selectListItems = this.Mapper.Map<List<KnowledgeBaseDto>>(knowledgeBases);
            return selectListItems;
        }

        public async Task CreateOrUpdateAsync(KnowledgeBase_InsertDto dto)
        {
            KnowledgeBase.Models.DatabaseModel.KnowledgeBase knowledgeBase;

            var vectorService = base.ServiceProvider.GetService<AIVectorService>();

            if (dto.Id == 0)
            {
                //New
                knowledgeBase = new KnowledgeBase.Models.DatabaseModel.KnowledgeBase(dto);
                await SaveObjectAsync(knowledgeBase);

                //add file
                var ncfFiles = await _ncfFileService.GetFullListAsync(_ => dto.NcfFileIds.Contains(_.Id));
                if (ncfFiles != null && ncfFiles.Any())
                {
                    var knowledgeBaseItems = new List<KnowledgeBaseItem>();
                    foreach (var item in ncfFiles)
                    {
                        var fileBytesResult = await _ncfFileService.GetFileBytes(item.Id);
                        var content = Encoding.UTF8.GetString(fileBytesResult.FileBytes);

                        var knowledgeBaseItemDto = new KnowledgeBaseItemDto(0, knowledgeBase.Id, ContentType.TextFile, content, item.FileName);
                        var kbItem = new KnowledgeBaseItem(knowledgeBaseItemDto);
                        knowledgeBaseItems.Add(kbItem);
                        await _knowledgeBaseDetailService.SaveObjectAsync(kbItem);
                    }
                    //await _knowledgeBaseDetailService.SaveObjectListAsync(knowledgeBaseItems);
                }
            }
            else
            {
                //edit
                knowledgeBase = await GetObjectAsync(_ => _.Id == dto.Id);
                knowledgeBase.Update(dto);

                await SaveObjectAsync(knowledgeBase);

            }
        }

        /// <summary>
        /// Add files to the knowledge base in batches (read, slice, save details)
        /// </summary>
        /// <param name="knowledgeBaseId"></param>
        /// <param name="fileIds"></param>
        /// <returns>Total number of slices</returns>
        public async Task<int> AddFilesToKnowledgeBaseAsync(int knowledgeBaseId, List<int> fileIds)
        {
            int totalChunks = 0;
            foreach (var fileId in fileIds)
            {
                var chunks = await AddFileToKnowledgeBaseAsync(knowledgeBaseId, fileId);
                totalChunks += chunks;
            }
            return totalChunks;
        }

        /// <summary>
        /// Add file to knowledge base (read, slice, save details)
        /// </summary>
        /// <param name="knowledgeBaseId"></param>
        /// <param name="fileId"></param>
        /// <returns>Number of slices</returns>
        public async Task<int> AddFileToKnowledgeBaseAsync(int knowledgeBaseId, int fileId)
        {
            // 1. Get file information
            var file = await _ncfFileService.GetObjectAsync(z => z.Id == fileId);
            if (file == null)
            {
                throw new NcfExceptionBase($"File with ID {fileId} not found.");
            }

            // 2. Read the file content
            // Construct the physical path: App_Data/NcfFiles/{Year}/{Month}/{StorageName}{Ext}
            var baseFilePath = Path.Combine(Senparc.CO2NET.Config.RootDirectoryPath, "App_Data", "NcfFiles");
            var fullPath = Path.Combine(baseFilePath, file.FilePath, file.StorageFileName + file.FileExtension);

            if (!File.Exists(fullPath))
            {
                throw new NcfExceptionBase($"Physical file not found at {fullPath}");
            }

            string content;
            // Simple processing: currently only supports text file reading
            // TODO: Support PDF, Word and other format parsing in the future
            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream))
            {
                content = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return 0;
            }

            // 3. Text slicing
            var chunks = SplitText(content, 500, 100); // Default chunk size 500, overlap 100

            // 4. Save slices to KnowledgeBasesDetail
            int chunkIndex = 0;
            foreach (var chunk in chunks)
            {
                var detailDto = new KnowledgeBaseItemDto
                {
                    KnowledgeBasesId = knowledgeBaseId,
                    Content = chunk,
                    ContentType = 0, // 0: Text
                    FileName = file.FileName, // Record source file name
                    ChunkIndex = chunkIndex++  // Record slice index
                };

                await _knowledgeBaseDetailService.CreateOrUpdateAsync(detailDto);
            }

            return chunks.Count;
        }

        /// <summary>
        /// Vectorize the knowledge base (Embedding)
        /// </summary>
        /// <param name="knowledgeBaseId"></param>
        /// <param name="tags">Tag of the current Embedding record</param>
        /// <returns></returns>
        public async Task<string> EmbeddingKnowledgeBaseAsync(int knowledgeBaseId, params string[] tags)
        {
            var knowledgeBase = await base.GetObjectAsync(z => z.Id == knowledgeBaseId);
            if (knowledgeBase == null)
            {
                throw new NcfExceptionBase($"Knowledge Base with ID {knowledgeBaseId} not found.");
            }

            // 1. Check configuration
            if (knowledgeBase.EmbeddingModelId <= 0)
            {
                throw new NcfExceptionBase($"知识库 '{knowledgeBase.Name}' 未配置 Embedding 模型，请先在'配置'中选择模型。");
            }

            // 2. Get AI Model configuration
            var aiModelService = _serviceProvider.GetService<AIModelService>();
            var aiVectorService = _serviceProvider.GetService<AIVectorService>();
            var aiModel = await aiModelService.GetObjectAsync(z => z.Id == knowledgeBase.EmbeddingModelId);
            if (aiModel == null)
            {
                throw new NcfExceptionBase($"Embedding 模型 ID {knowledgeBase.EmbeddingModelId} 不存在。");
            }

            var aiVector = await aiVectorService.GetObjectAsync(z => z.Id == knowledgeBase.VectorDBId);
            if (aiVector == null)
            {
                throw new NcfExceptionBase($"Embedding 向量库 ID {knowledgeBase.VectorDBId} 不存在。");
            }

            var aiModelDto = new AIModelDto(aiModel);
            var aiVectorDto = new AIVectorDto(aiVector);
            //var senparcAiSetting = aiModelService.BuildSenparcAiSetting(aiModelDto, aiVectorDto);

            // 3. Get the text slice to be vectorized (unvectorized data)
            //var details = await _knowledgeBaseDetailService.GetFullListAsync(z =>
                //z.KnowledgeBasesId == knowledgeBaseId && !z.IsEmbedded);

            var details = await _knowledgeBaseDetailService.GetFullListAsync(z =>
                z.KnowledgeBasesId == knowledgeBaseId);

            var embeddingAiModel = await this._aIModelService.GetObjectAsync(z => z.Id == knowledgeBase.EmbeddingModelId);
            if (embeddingAiModel == null)
            {
                throw new NcfExceptionBase($"Embedding 模型 AIKernel 模型未找到：{knowledgeBase.EmbeddingModelId}。");
            }
            var embeddingAiModelDto = this._aIModelService.Mapper.Map<AIModelDto>(embeddingAiModel);

            var embeddingAiSetting = this._aIModelService.BuildSenparcAiSetting(embeddingAiModelDto, aiVectorDto);
            //TODO: Change to dynamic
            var embeddingModelName = embeddingAiSetting.AzureOpenAIKeys.ModelName.Embedding;
            // 4. Initialize SemanticAiHandler
            var embeddingAiHandler = new SemanticAiHandler(embeddingAiSetting);

            //_serviceProvider.GetService<SemanticAiHandler>();
            //if (semanticAiHandler == null)
            //{
            //    throw new NcfExceptionBase("SemanticAiHandler service is not registered.");
            //}

            // 5. Build IWantToRun (Embedding mode)

            var iWantToRunEmbedding = embeddingAiHandler
                 .IWantTo()
                 .ConfigModel(ConfigModel.TextEmbedding, $"NcfKnowledgeBase_{embeddingAiModel.Id}")
                 .ConfigVectorStore(embeddingAiSetting.VectorDB)
            .BuildKernel();

            // 6. Generate Embeddings in batches and store them
            int processedCount = 0;
            int successCount = 0;
            int failCount = 0;
            string collectionName = $"{knowledgeBase.Name.Replace(" ", "_")}";

            var chunkIndex = 0;

            //slice
            foreach (var detail in details)
            {
                processedCount++;

                //SenparcTrace.SendCustomLog("Knowledge Base", $"Knowledge base '{knowledgeBase.Name}' has no text slices to vectorize. Start slicing now");

                //Get the content from the associated file for slicing //TODO: Judge the ContentType to decide how to process different types of content
                var text = detail.Content;//knowledgeBase.Content;

                if (text.IsNullOrEmpty())
                {
                    continue;
                }

                List<string> paragraphs = new List<string>();
#pragma warning disable SKEXP0050 // Types are for evaluation only and may be changed or removed in future updates. Cancel this diagnostic to continue.
                paragraphs = TextChunker.SplitPlainTextParagraphs(
                         TextChunker.SplitPlainTextLines(System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Replace("\r\n", " "), 128),
                         256);
#pragma warning restore SKEXP0050 // Types are for evaluation only and may be changed or removed in future updates. Cancel this diagnostic to continue.

                var vectorName = collectionName; //$"{knowledgeBase.Name}-{knowledgeBase.Id}";
            MemoryStore:
                try
                {
                    
                    var dt = SystemTime.Now;
                    var vectorCollection = iWantToRunEmbedding.GetVectorCollection<ulong, Record>(embeddingAiSetting.VectorDB, vectorName);
                    await vectorCollection.EnsureCollectionExistsAsync();

                    var fileName = detail.FileName;
                    List<string> tagList = new List<string>();
                    if (tags != null && tags.Length > 0)
                    {
                        tagList.AddRange(tags);
                    }

                    if (!fileName.IsNullOrEmpty())
                    {
                        tagList.Add(Path.GetFileNameWithoutExtension(fileName));
                    }

                    foreach (var paragraph in paragraphs)
                    {
                        try
                        {
                            var currentIndex = chunkIndex++;
                            var descriptionEmbedding = await iWantToRunEmbedding.SemanticKernelHelper.GetEmbeddingAsync(embeddingModelName, paragraph);


                            var record = new Record()
                            {
                                Id = (ulong)chunkIndex,
                                Name = vectorName + "-paragraph-" + chunkIndex,
                                Description = paragraph,
                                DescriptionEmbedding = descriptionEmbedding,
                                Tags = tagList.ToArray()
                            };

                            await vectorCollection.UpsertAsync(record);
                        }
                        catch (Exception ex)
                        {
                            throw;
                        }

                    }

                    //test
                    //ReadOnlyMemory<float> searchVector = await iWantToRunEmbedding.SemanticKernelHelper.GetEmbeddingAsync(embeddingModelName, "What is NCF?");

                    //var vectorResult = vectorCollection.SearchAsync(searchVector, 3);
                    //await foreach (var item in vectorResult)
                    //{
                    //    Console.WriteLine($"Get the result: {item.Record.ToJson(true)}");
                    //}

                    detail.EmbeddingSuccessed(chunkIndex);
                    successCount++;
                    await _knowledgeBaseDetailService.SaveObjectAsync(detail);
                }
                catch (Exception ex)
                {
                    string pattern = @"retry after (\d+) seconds";
                    Match match = Regex.Match(ex.Message, pattern);
                    if (match.Success)
                    {
                        Console.WriteLine($"等待冷却 {match.Value} 秒");
                    }
                    //Error count increments
                    failCount++;

                    goto MemoryStore;
                }
            }



            ////iWantToRunChat
            //var result = await embeddingAiHandler.ChatAsync(iWantToRunEmbedding, "");
            //await Console.Out.WriteLineAsync(result.OutputString);


            return $"知识库 '{knowledgeBase.Name}' 向量化完成！\n" +
                   $"总计: {processedCount} 个切片\n" +
                   $"成功: {successCount}\n" +
                   $"失败: {failCount}\n" +
                   $"集合名称: {collectionName}";
        }

        /// <summary>
        /// Recall Test (Embedding)
        /// </summary>
        /// <param name="knowledgeBaseId"></param>
        /// <param name="tags">Tag of the current Embedding record</param>
        /// <returns></returns>
        public async Task<List<RecallTestResponse>> RecallTestAsync(int knowledgeBaseId, string content, int topK = 5)
        {
            List<RecallTestResponse> lstRecallTest = new List<RecallTestResponse>();

            var knowledgeBase = await base.GetObjectAsync(z => z.Id == knowledgeBaseId);
            if (knowledgeBase == null)
            {
                throw new NcfExceptionBase($"Knowledge Base with ID {knowledgeBaseId} not found.");
            }

            // 1. Check configuration
            if (knowledgeBase.EmbeddingModelId <= 0)
            {
                throw new NcfExceptionBase($"知识库 '{knowledgeBase.Name}' 未配置 Embedding 模型，请先在'配置'中选择模型。");
            }

            // 2. Get AI Model configuration
            var aiModelService = _serviceProvider.GetService<AIModelService>();
            var aiVectorService = _serviceProvider.GetService<AIVectorService>();
            var aiModel = await aiModelService.GetObjectAsync(z => z.Id == knowledgeBase.EmbeddingModelId);
            if (aiModel == null)
            {
                throw new NcfExceptionBase($"Embedding 模型 ID {knowledgeBase.EmbeddingModelId} 不存在。");
            }

            var aiVector = await aiVectorService.GetObjectAsync(z => z.Id == knowledgeBase.VectorDBId);
            if (aiVector == null)
            {
                throw new NcfExceptionBase($"Embedding 向量库 ID {knowledgeBase.VectorDBId} 不存在。");
            }

            var aiModelDto = new AIModelDto(aiModel);
            var aiVectorDto = new AIVectorDto(aiVector);

            var embeddingAiModel = await this._aIModelService.GetObjectAsync(z => z.Id == knowledgeBase.EmbeddingModelId);
            if (embeddingAiModel == null)
            {
                throw new NcfExceptionBase($"Embedding 模型 AIKernel 模型未找到：{knowledgeBase.EmbeddingModelId}。");
            }
            var embeddingAiModelDto = this._aIModelService.Mapper.Map<AIModelDto>(embeddingAiModel);

            var embeddingAiSetting = this._aIModelService.BuildSenparcAiSetting(embeddingAiModelDto, aiVectorDto);
            //TODO: Change to dynamic
            var embeddingModelName = embeddingAiSetting.AzureOpenAIKeys.ModelName.Embedding;
            // 4. Initialize SemanticAiHandler
            var embeddingAiHandler = new SemanticAiHandler(embeddingAiSetting);

            // 5. Build IWantToRun (Embedding mode)

            var iWantToRunEmbedding = embeddingAiHandler
                 .IWantTo()
                 .ConfigModel(ConfigModel.TextEmbedding, $"NcfKnowledgeBase_{embeddingAiModel.Id}")
                 .ConfigVectorStore(embeddingAiSetting.VectorDB)
            .BuildKernel();

            string collectionName = $"{knowledgeBase.Name.Replace(" ", "_")}";
            var vectorName = collectionName; //$"{knowledgeBase.Name}-{knowledgeBase.Id}";
        MemoryStore:
            try
            {

                var dt = SystemTime.Now;
                var vectorCollection = iWantToRunEmbedding.GetVectorCollection<ulong, Record>(embeddingAiSetting.VectorDB, vectorName);
                await vectorCollection.EnsureCollectionExistsAsync();

                //test
                ReadOnlyMemory<float> searchVector = await iWantToRunEmbedding.SemanticKernelHelper.GetEmbeddingAsync(embeddingModelName, content);

                var vectorResult = vectorCollection.SearchAsync(searchVector, topK);
                await foreach (var item in vectorResult)
                {
                    Console.WriteLine($"得到结果：{item.Record.ToJson(true)}");
                    RecallTestResponse recallTest = new RecallTestResponse()
                    {
                        Score = item.Score.ToString(),
                        Content = item.Record.Description,
                        RecallTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    lstRecallTest.Add(recallTest);
                }
            }
            catch (Exception ex)
            {
                string pattern = @"retry after (\d+) seconds";
                Match match = Regex.Match(ex.Message, pattern);
                if (match.Success)
                {
                    Console.WriteLine($"等待冷却 {match.Value} 秒");
                }

                goto MemoryStore;
            }


            return lstRecallTest;
        }


        /// <summary>
        /// Simple text slicing algorithm
        /// </summary>
        /// <param name="text"></param>
        /// <param name="chunkSize"></param>
        /// <param name="overlap"></param>
        /// <returns></returns>
        private List<string> SplitText(string text, int chunkSize, int overlap)
        {
            var chunks = new List<string>();
            if (string.IsNullOrEmpty(text)) return chunks;

            // Simply split by the number of characters, and can be optimized to split by token or paragraph later.
            for (int i = 0; i < text.Length; i += (chunkSize - overlap))
            {
                int length = Math.Min(chunkSize, text.Length - i);
                if (length <= 0) break;

                chunks.Add(text.Substring(i, length));

                // Prevent infinite loops (if overlap >= chunkSize)
                if (chunkSize - overlap <= 0) break;
            }

            return chunks;
        }
    }

    public class Record
    {
        [VectorStoreKey]
        public ulong Id { get; set; }

        [VectorStoreData(IsIndexed = true)]
        public string Name { get; set; }

        [VectorStoreData(IsFullTextIndexed = true)]
        public string Description { get; set; }

        [VectorStoreVector(Dimensions: 1536 /*Adjust according to the model, for example text-embedding-ada-002 is 1536, Large is 3072*/, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)]
        public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }

        [VectorStoreData(IsIndexed = true)]
        public string[] Tags { get; set; }
    }
}
