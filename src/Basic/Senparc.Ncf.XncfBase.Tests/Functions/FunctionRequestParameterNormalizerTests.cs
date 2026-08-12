/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FunctionRequestParameterNormalizerTests.cs
    文件功能描述：函数请求参数归一化测试


    创建标识：Senparc - 20260802

----------------------------------------------------------------*/
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.XncfBase.FunctionRenders;

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
    }
}
