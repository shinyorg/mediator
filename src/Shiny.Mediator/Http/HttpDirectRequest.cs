namespace Shiny.Mediator.Http;


public class HttpDirectRequest : IRequest<object?>, IContractKey
{
    /// <summary>
    /// This value either needs to be in the configuration Mediator:Http:Direct:{Route}:Url
    /// or is used with a baseURL in Mediator:Http:Direct:BaseUrl
    /// </summary>
    public required string ConfigNameOrRoute { get; set; }
    
    /// <summary>
    /// The type to deserialize to
    /// </summary>
    public Type? ResultType { get; set; }
    
    /// <summary>
    /// Takes a value from configuration Mediator:Http:Direct:{Route}:Method otherwise defaults to GET
    /// </summary>
    public HttpMethod? Method { get; set; }
    
    /// <summary>
    /// Headers to include in the request
    /// </summary>
    public Dictionary<string, string> Headers { get; } = new();
    
    /// <summary>
    /// Form values to include in the body.  Cannot be set along with JsonBody
    /// </summary>
    public Dictionary<string, string> FormValues { get; } = new();
    
    /// <summary>
    /// An object that is serialized to the body, cannot be set with form values
    /// </summary>
    public object? SerializableBody { get; set; }
    
    /// <summary>
    /// Timeout of the request
    /// </summary>
    public TimeSpan? Timeout { get; set; }
    
    /// <summary>
    /// Full path of files to upload to the server
    /// </summary>
    public List<FileInfo> UploadFiles { get; } = new();
    
    /// <summary>
    /// Override the request key according to your specifications
    /// </summary>
    public string? RequestKey { get; set; }
    
    public string GetKey() => this.RequestKey ?? this.ConfigNameOrRoute;
}

