namespace BMCL.Download;

public sealed record DownloadProgress(int Completed, int Total, string FileName);

public sealed record DownloadResult(int Downloaded, int Skipped);
