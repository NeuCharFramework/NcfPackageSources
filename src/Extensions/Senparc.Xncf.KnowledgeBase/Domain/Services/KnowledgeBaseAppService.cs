using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.FileManager.Domain.Services;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto;
using Senparc.Xncf.KnowledgeBase.Services;

namespace Senparc.Xncf.KnowledgeBase.Domain.Services
{
    public class KnowledgeBaseAppService
    {
        private readonly KnowledgeBasesService _knowledgeBasesService;
        private readonly KnowledgeBasesDetailService _knowledgeBasesDetailService;
        private readonly NcfFileService _ncfFileService;
        private readonly IServiceProvider _serviceProvider;

        public KnowledgeBaseAppService(
            KnowledgeBasesService knowledgeBasesService,
            KnowledgeBasesDetailService knowledgeBasesDetailService,
            NcfFileService ncfFileService,
            IServiceProvider serviceProvider)
        {
            _knowledgeBasesService = knowledgeBasesService;
            _knowledgeBasesDetailService = knowledgeBasesDetailService;
            _ncfFileService = ncfFileService;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 批量将文件添加到知识库（读取、切片、保存详情）
        /// </summary>
        /// <param name="knowledgeBaseId"></param>
        /// <param name="fileIds"></param>
        /// <returns>总切片数</returns>
        public async Task<int> AddFilesToKnowledgeBaseAsync(string knowledgeBaseId, List<int> fileIds)
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
        public async Task<int> AddFileToKnowledgeBaseAsync(string knowledgeBaseId, int fileId)
        {
            // 1. 获取文件信息
            var file = await _ncfFileService.GetObjectAsync(z => z.Id == fileId);
            if (file == null)
            {
                throw new NcfExceptionBase($"File with ID {fileId} not found.");
            }

            // 2. 读取文件内容
            // 构造物理路径: App_Data/NcfFiles/{Year}/{Month}/{StorageName}{Ext}
            var baseFilePath = Path.Combine(Config.RootDirectoryPath, "App_Data", "NcfFiles");
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
            foreach (var chunk in chunks)
            {
                var detailDto = new KnowledgeBasesDetailDto
                {
                    KnowledgeBasesId = knowledgeBaseId,
                    Content = chunk,
                    ContentType = 0, // 0: Text
                    // 暂时将文件名记录在 Remark 中，方便调试
                    // 注意：KnowledgeBasesDetailDto 可能没有 Remark 字段，需要检查 Entity
                };
                
                // 创建实体并保存
                // 由于 Dto 可能不完整，直接使用 Service 的 CreateOrUpdateAsync
                // 但 Service 的 CreateOrUpdateAsync 也是基于 Dto 的。
                // 我们先检查一下 Dto 定义，如果不行就直接操作 Entity 或者扩展 Dto
                await _knowledgeBasesDetailService.CreateOrUpdateAsync(detailDto);
            }

            return chunks.Count;
        }

        /// <summary>
        /// 对知识库进行向量化（Embedding）
        /// 注意：当前为第一阶段，仅完成文本切片存储，Embedding 功能待第二阶段实现
        /// </summary>
        /// <param name="knowledgeBaseId"></param>
        /// <returns></returns>
        public async Task<string> EmbeddingKnowledgeBaseAsync(string knowledgeBaseId)
        {
            // TODO: 第二阶段实现
            // 1. 获取该知识库下所有未向量化的 KnowledgeBasesDetail
            // 2. 调用 AIKernel 生成 Embedding
            // 3. 存储到向量数据库
            
            var knowledgeBase = await _knowledgeBasesService.GetObjectAsync(z => z.Id == knowledgeBaseId);
            if (knowledgeBase == null)
            {
                throw new NcfExceptionBase($"Knowledge Base with ID {knowledgeBaseId} not found.");
            }

            // 获取该知识库的所有切片
            var details = await _knowledgeBasesDetailService.GetFullListAsync(z => z.KnowledgeBasesId == knowledgeBaseId);
            
            return $"知识库 '{knowledgeBase.Name}' 包含 {details.Count()} 个文本切片，Embedding 功能将在第二阶段实现。";
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
}
