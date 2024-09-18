using System.Net;
using Presentation.Blazor.Client.Services.Api.Models;

namespace Presentation.Blazor.Client.Services.Api;

public class ApiResponseException : Exception
{
    public HttpStatusCode StatusCode { get; set; }
    public ProblemDetails ProblemDetails { get; set; }

    public ApiResponseException(HttpStatusCode statusCode, ProblemDetails problemDetails)
        : base(message: $"{statusCode} : {problemDetails.Title} - {problemDetails.Detail}")
    {
        StatusCode = statusCode;
        ProblemDetails = problemDetails;
    }
}