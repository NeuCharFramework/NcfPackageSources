/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: ResponseClass.cs
    File description: ResponseClass related implementation
    
    
    Created by: Senparc - 20250624
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260705
    Change description: v0.36.3-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.36.4-preview3 Updated module behavior and related support.
----------------------------------------------------------------*/

#nullable enable

public partial class ResponseClass
{
    public int StatusCode { get; set; }

    //Legacy comment translated to English.
    public string? Message { get; set; }
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
}
