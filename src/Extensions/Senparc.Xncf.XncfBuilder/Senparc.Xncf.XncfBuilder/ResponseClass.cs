public partial class ResponseClass
{
    public int StatusCode { get; set; }

    // 新增属性测试 Response.cs 独立修改
    public string? Message { get; set; }
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
}
