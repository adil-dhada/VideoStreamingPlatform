using System;
using System.Collections.Generic;

namespace Shared.Contracts;

public record VideoUploadedMessage(
    Guid VideoId,
    Guid UserId,
    string RawFilePath,
    string FileName,
    DateTime CreatedAt
);

public record TranscodedVariant(
    string Resolution,
    string ManifestPath,
    string SegmentDirectory,
    int BitrateKbps,
    int Width,
    int Height
);

public record TranscodingCompletedMessage(
    Guid VideoId,
    IReadOnlyList<TranscodedVariant> Variants,
    double DurationSeconds
);

public record TranscodingFailedMessage(
    Guid VideoId,
    string Reason
);

public record VideoDeletedMessage(
    Guid VideoId,
    Guid UserId
);
