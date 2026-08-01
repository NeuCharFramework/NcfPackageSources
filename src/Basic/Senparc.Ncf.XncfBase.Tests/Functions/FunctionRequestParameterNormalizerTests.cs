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
    }
}
