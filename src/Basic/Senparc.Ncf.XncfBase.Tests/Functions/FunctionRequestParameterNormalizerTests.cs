/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FunctionRequestParameterNormalizerTests.cs
    文件功能描述：函数请求参数归一化测试


    创建标识：Senparc - 20260802

----------------------------------------------------------------*/
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions.Files;
using Senparc.Ncf.XncfBase.Functions.Parameters;
using Senparc.Ncf.XncfBase.FunctionRenders;
using System.Text.Json;

namespace Senparc.Ncf.XncfBase.Functions.Tests
{
    [TestClass]
    public class FunctionRequestParameterNormalizerTests
    {
        private sealed class NumericRequest
        {
            public int Port { get; set; }

            public int? OptionalPort { get; set; }

            public decimal Rate { get; set; }

            public double Ratio { get; set; }

            public float Weight { get; set; }
        }

        private sealed class FileRequest
        {
            [FunctionParameterUi(ParameterType.File, AllowMultiple = true, MaxFiles = 3)]
            public FunctionFileReference[] Files { get; set; } = System.Array.Empty<FunctionFileReference>();
        }

        [TestMethod]
        public void NormalizeNullToDefaultForNonNullableValueTypeTest()
        {
            const string rawJson = "{\"Port\":null,\"OptionalPort\":null}";

            var normalizedJson = FunctionRequestParameterNormalizer.NormalizeJson(rawJson, typeof(NumericRequest));
            var result = Senparc.CO2NET.Helpers.SerializerHelper.GetObject(normalizedJson, typeof(NumericRequest)) as NumericRequest;

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Port);
            Assert.IsNull(result.OptionalPort);
        }

        [TestMethod]
        public void NormalizeNumericFormulaValues_ShouldRetainNumericJsonTypes()
        {
            const string rawJson = "{\"Port\":42,\"Rate\":42.5,\"Ratio\":0.25,\"Weight\":1.5}";

            var normalizedJson = FunctionRequestParameterNormalizer.NormalizeJson(rawJson, typeof(NumericRequest));
            var result = Senparc.CO2NET.Helpers.SerializerHelper.GetObject(normalizedJson, typeof(NumericRequest)) as NumericRequest;

            Assert.IsNotNull(result);
            Assert.AreEqual(42, result.Port);
            Assert.AreEqual(42.5m, result.Rate);
            Assert.AreEqual(0.25d, result.Ratio);
            Assert.AreEqual(1.5f, result.Weight);
            Assert.IsFalse(normalizedJson.Contains("\"42.5\""));
        }

        [TestMethod]
        public void NormalizeFileIds_ShouldCreateSerializableReferences()
        {
            const string rawJson = "{\"Files\":[\"0123456789abcdef0123456789abcdef\",\"fedcba9876543210fedcba9876543210\"]}";

            var normalizedJson = FunctionRequestParameterNormalizer.NormalizeJson(rawJson, typeof(FileRequest));
            var result = JsonSerializer.Deserialize<FileRequest>(normalizedJson);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Files.Length);
            Assert.AreEqual("0123456789abcdef0123456789abcdef", result.Files[0].FileId);
            Assert.AreEqual("fedcba9876543210fedcba9876543210", result.Files[1].FileId);
        }

        [TestMethod]
        public void FileParameter_ShouldProduceFileIdArrayAiSchema()
        {
            using var document = JsonDocument.Parse(
                "{\"type\":\"object\",\"properties\":{\"Files\":{\"type\":\"array\",\"items\":{\"type\":\"string\"}}}}");
            var parameter = new FunctionParameterInfo(
                "Files",
                "文件",
                "输入文件",
                true,
                "FunctionFileReference[]",
                ParameterType.File,
                new SelectionList(SelectionType.Unknown),
                null,
                0)
            {
                AllowMultiple = true,
                MaxFiles = 3,
                Accept = ".txt"
            };

            var result = FunctionRenderAiSchemaBuilder.ApplySelectionMetadata(
                document.RootElement,
                new[] { parameter });
            var files = result.GetProperty("properties").GetProperty("Files");

            Assert.AreEqual("array", files.GetProperty("type").GetString());
            Assert.AreEqual("file", files.GetProperty("x-ncf-parameter-type").GetString());
            Assert.AreEqual(3, files.GetProperty("maxItems").GetInt32());
            Assert.AreEqual("string", files.GetProperty("items").GetProperty("type").GetString());
            StringAssert.Contains(files.GetProperty("description").GetString(), ".txt");
        }
    }
}
