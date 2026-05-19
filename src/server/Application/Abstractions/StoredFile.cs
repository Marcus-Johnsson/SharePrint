namespace SharePrint.Application.Abstractions;

public sealed record StoredFile(Stream Content,string ContentType, string OriginalFileName);
