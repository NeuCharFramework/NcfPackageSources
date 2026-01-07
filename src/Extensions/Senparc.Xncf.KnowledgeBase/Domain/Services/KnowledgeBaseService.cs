using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Text;
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
using Senparc.Xncf.KnowledgeBase.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Senparc.Xncf.KnowledgeBase.Domain.Services
{
    public class KnowledgeBaseService : ServiceBase<Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.KnowledgeBase>
    {
        private readonly KnowledgeBaseItemService _knowledgeBasesDetailService;
        private readonly NcfFileService _ncfFileService;
        private readonly AIModelService _aIModelService;
        private readonly IServiceProvider _serviceProvider;

        public KnowledgeBaseService(
            RepositoryBase<KnowledgeBase.Models.DatabaseModel.KnowledgeBase> repo,
            KnowledgeBaseItemService knowledgeBasesDetailService,
            NcfFileService ncfFileService,
            AIModelService aIModelService,
            IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
            _knowledgeBasesDetailService = knowledgeBasesDetailService;
            _ncfFileService = ncfFileService;
            _aIModelService = aIModelService;
            _serviceProvider = serviceProvider;
        }

        public async Task<IEnumerable<KnowledgeBasesDto>> GetKnowledgeBasesList(int PageIndex, int PageSize)
        {
            List<KnowledgeBasesDto> selectListItems = null;
            List<KnowledgeBase.Models.DatabaseModel.KnowledgeBase> knowledgeBases = 
                (await GetFullListAsync(_ => true).ConfigureAwait(false))
                .OrderByDescending(_ => _.AddTime)
                .ToList();
            selectListItems = this.Mapper.Map<List<KnowledgeBasesDto>>(knowledgeBases);
            return selectListItems;
        }

        public async Task CreateOrUpdateAsync(KnowledgeBasesDto dto)
        {
            KnowledgeBase.Models.DatabaseModel.KnowledgeBase knowledgeBases;
            if (dto.Id == 0)
            {
                knowledgeBases = new KnowledgeBase.Models.DatabaseModel.KnowledgeBase(dto);
            }
            else
            {
                knowledgeBases = await GetObjectAsync(_ => _.Id == dto.Id);
                knowledgeBases.Update(dto);
            }
            await SaveObjectAsync(knowledgeBases);
        }

        /// <summary>
        /// 批量将文件添加到知识库（读取、切片、保存详情）
        /// </summary>
        /// <param name="knowledgeBaseId"></param>
        /// <param name="fileIds"></param>
        /// <returns>总切片数</returns>
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
        /// 将文件添加到知识库（读取、切片、保存详情）
        /// </summary>
        /// <param name="knowledgeBaseId"></param>
        /// <param name="fileId"></param>
        /// <returns>切片数</returns>
        public async Task<int> AddFileToKnowledgeBaseAsync(int knowledgeBaseId, int fileId)
        {
            // 1. 获取文件信息
            var file = await _ncfFileService.GetObjectAsync(z => z.Id == fileId);
            if (file == null)
            {
                throw new NcfExceptionBase($"File with ID {fileId} not found.");
            }

            // 2. 读取文件内容
            // 构造物理路径: App_Data/NcfFiles/{Year}/{Month}/{StorageName}{Ext}
            var baseFilePath = Path.Combine(Senparc.CO2NET.Config.RootDirectoryPath, "App_Data", "NcfFiles");
            var fullPath = Path.Combine(baseFilePath, file.FilePath, file.StorageFileName + file.FileExtension);

            if (!File.Exists(fullPath))
            {
                throw new NcfExceptionBase($"Physical file not found at {fullPath}");
            }

            string content;
            // 简单处理：目前只支持文本文件读取
            // TODO: 后续支持 PDF, Word 等格式解析
            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream))
            {
                content = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return 0;
            }

            // 3. 文本切片
            var chunks = SplitText(content, 500, 100); // 默认 chunk size 500, overlap 100

            // 4. 保存切片到 KnowledgeBasesDetail
            int chunkIndex = 0;
            foreach (var chunk in chunks)
            {
                var detailDto = new KnowledgeBasesDetalDto
                {
                    KnowledgeBasesId = knowledgeBaseId,
                    Content = chunk,
                    ContentType = 0, // 0: Text
                    FileName = file.FileName, // 记录源文件名
                    ChunkIndex = chunkIndex++  // 记录切片索引
                };

                await _knowledgeBasesDetailService.CreateOrUpdateAsync(detailDto);
            }

            return chunks.Count;
        }

        /// <summary>
        /// 对知识库进行向量化（Embedding）
        /// </summary>
        /// <param name="knowledgeBaseId"></param>
        /// <returns></returns>
        public async Task<string> EmbeddingKnowledgeBaseAsync(int knowledgeBaseId)
        {
            var knowledgeBase = await base.GetObjectAsync(z => z.Id == knowledgeBaseId);
            if (knowledgeBase == null)
            {
                throw new NcfExceptionBase($"Knowledge Base with ID {knowledgeBaseId} not found.");
            }

            // 1. 检查配置
            if (knowledgeBase.EmbeddingModelId <= 0)
            {
                throw new NcfExceptionBase($"知识库 '{knowledgeBase.Name}' 未配置 Embedding 模型，请先在'配置'中选择模型。");
            }

            // 2. 获取AI Model配置
            var aiModelService = _serviceProvider.GetService<AIModelService>();
            var aiModel = await aiModelService.GetObjectAsync(z => z.Id == knowledgeBase.EmbeddingModelId);
            if (aiModel == null)
            {
                throw new NcfExceptionBase($"Embedding 模型 ID {knowledgeBase.EmbeddingModelId} 不存在。");
            }

            var aiModelDto = new AIModelDto(aiModel);
            var senparcAiSetting = aiModelService.BuildSenparcAiSetting(aiModelDto);

            // 3. 获取待向量化的文本切片（未向量化的数据）
            var details = await _knowledgeBasesDetailService.GetFullListAsync(z =>
                z.KnowledgeBasesId == knowledgeBaseId && !z.IsEmbedded);

            var embeddingAiModel = await this._aIModelService.GetObjectAsync(z => z.Id == knowledgeBase.EmbeddingModelId);
            if (embeddingAiModel == null)
            {
                throw new NcfExceptionBase($"Embedding 模型 AIKernel 模型未找到：{knowledgeBase.EmbeddingModelId}。");
            }
            var embeddingAiModelDto = this._aIModelService.Mapper.Map<AIModelDto>(embeddingAiModel);

            var embeddingAiSetting = this._aIModelService.BuildSenparcAiSetting(embeddingAiModelDto);
            //TODO:改成动态
            var embeddingModelName = embeddingAiSetting.AzureOpenAIKeys.ModelName.Embedding;
            // 4. 初始化 SemanticAiHandler
            var embeddingAiHandler = new SemanticAiHandler(embeddingAiSetting);

            //_serviceProvider.GetService<SemanticAiHandler>();
            //if (semanticAiHandler == null)
            //{
            //    throw new NcfExceptionBase("SemanticAiHandler 服务未注册。");
            //}

            // 5. 构建 IWantToRun (Embedding 模式)

            var iWantToRunEmbedding = embeddingAiHandler
                 .IWantTo()
                 .ConfigModel(ConfigModel.TextEmbedding, $"NcfKnowledgeBase_{embeddingAiModel.Id}")
                 .ConfigVectorStore(embeddingAiSetting.VectorDB)
            .BuildKernel();

            // 6. 批量生成 Embeddings 并存储
            int processedCount = 0;
            int successCount = 0;
            int failCount = 0;
            string collectionName = $"KB_{knowledgeBaseId}";

            //进行切片
            if (!details.Any())
            {
                SenparcTrace.SendCustomLog("知识库", $"知识库 '{knowledgeBase.Name}' 没有待向量化的文本切片。现在开始切片");

                //从关联的文件中获取内容进行切片

                var text = knowledgeBase.Content;

                List<string> paragraphs = new List<string>();
#pragma warning disable SKEXP0050 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
                paragraphs = TextChunker.SplitPlainTextParagraphs(
                         TextChunker.SplitPlainTextLines(System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Replace("\r\n", " "), 128),
                         256);
#pragma warning restore SKEXP0050 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。

                var vectorName = $"{knowledgeBase.Name}-{knowledgeBase.Id}";
            MemoryStore:
                try
                {
                    var i = 0;
                    var dt = SystemTime.Now;
                    var vectorCollection = iWantToRunEmbedding.GetVectorCollection<ulong, Record>(embeddingAiSetting.VectorDB, vectorName);
                    await vectorCollection.EnsureCollectionExistsAsync();
                    paragraphs.ForEach(async paragraph =>
                    {
                        var currentI = i++;

                        var record = new Record()
                        {
                            Id = (ulong)i,
                            Name = vectorName + "-paragraph-" + i,
                            Description = paragraph,
                            DescriptionEmbedding = await iWantToRunEmbedding.SemanticKernelHelper.GetEmbeddingAsync(embeddingModelName, paragraph),
                            Tags = new[] { i.ToString() }
                        };


                        await vectorCollection.UpsertAsync(record);
                    });

                    //测试
                    ReadOnlyMemory<float> searchVector = await iWantToRunEmbedding.SemanticKernelHelper.GetEmbeddingAsync(embeddingModelName, "什么是NCF？");

                    var vectorResult = vectorCollection.SearchAsync(searchVector, 3);
                    await foreach (var item in vectorResult)
                    {
                        Console.WriteLine($"得到结果：{item.Record.ToJson(true)}");
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
        /// 简单的文本切片算法
        /// </summary>
        /// <param name="text"></param>
        /// <param name="chunkSize"></param>
        /// <param name="overlap"></param>
        /// <returns></returns>
        private List<string> SplitText(string text, int chunkSize, int overlap)
        {
            var chunks = new List<string>();
            if (string.IsNullOrEmpty(text)) return chunks;

            // 简单按字符数切分，后续可以优化为按 Token 或段落切分
            for (int i = 0; i < text.Length; i += (chunkSize - overlap))
            {
                int length = Math.Min(chunkSize, text.Length - i);
                if (length <= 0) break;

                chunks.Add(text.Substring(i, length));

                // 防止死循环（如果 overlap >= chunkSize）
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

        [VectorStoreVector(Dimensions: 1536 /*根据模型调整，例如 text-embedding-ada-002 为 1536，Large 为 3072*/, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)]
        public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }

        [VectorStoreData(IsIndexed = true)]
        public string[] Tags { get; set; }
    }
}
