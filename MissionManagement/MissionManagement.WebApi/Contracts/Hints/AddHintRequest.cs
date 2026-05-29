using Microsoft.AspNetCore.Http;

namespace MissionManagement.WebApi.Contracts.Hints;

public sealed record AddHintRequest(
    string Content,
    IFormFile Attachment
);

