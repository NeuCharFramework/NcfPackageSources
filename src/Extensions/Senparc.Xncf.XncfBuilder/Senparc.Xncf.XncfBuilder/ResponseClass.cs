public partial class ResponseClass
{
    public int StatusCode { get; set; }

    // New attribute test Response.cs independently modified
    public string? Message { get; set; }
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
}
