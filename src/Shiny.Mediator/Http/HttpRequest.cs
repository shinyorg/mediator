namespace Shiny.Mediator.Http;

public abstract class HttpRequest<T> : IRequest<T>
{
    public string Route { get; set; }
    
    /// <summary>
    /// </summary>
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    
    /// <summary>
    /// Headers to include in the request
    /// </summary>
    public Dictionary<string, string> Headers { get; } = new();
    
    /// <summary>
    /// Full path of files to upload to the server
    /// </summary>
    public List<FileInfo> UploadFiles { get; } = new();
    
    /// <summary>
    /// Override the request key according to your specifications
    /// </summary>
    public string? RequestKey { get; set; }
    
    public string GetKey() => this.RequestKey ?? null;
}