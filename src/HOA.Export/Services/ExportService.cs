using Microsoft.EntityFrameworkCore;
using HOA.Model;
using HOA.Services;

namespace HOA.Export.Services;

public class ExportService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStore _fileStore;
    private readonly PdfGenerator _pdfGenerator;
    private readonly ExportOptions _options;

    public ExportService(
        ApplicationDbContext context,
        IFileStore fileStore,
        ExportOptions options)
    {
        _context = context;
        _fileStore = fileStore;
        _pdfGenerator = new PdfGenerator();
        _options = options;
    }

    public async Task<ExportResult> ExportAsync()
    {
        var result = new ExportResult();

        var submissions = await GetSubmissionsAsync();

        if (submissions.Count == 0)
        {
            Console.WriteLine("No submissions found matching the specified filters.");
            return result;
        }

        Console.WriteLine($"Found {submissions.Count} submission(s) to export.");

        // Create output directory
        Directory.CreateDirectory(_options.OutputPath!);

        // Group by address
        var groupedByAddress = submissions.GroupBy(s => s.Address);

        foreach (var addressGroup in groupedByAddress)
        {
            var addressFolder = SanitizeFolderName(addressGroup.Key);
            var addressPath = Path.Combine(_options.OutputPath!, addressFolder);
            Directory.CreateDirectory(addressPath);

            if (_options.Verbose)
            {
                Console.WriteLine($"Processing address: {addressGroup.Key}");
            }

            foreach (var submission in addressGroup)
            {
                await ExportSubmissionAsync(submission, addressPath, result);
            }
        }

        return result;
    }

    private async Task<List<Submission>> GetSubmissionsAsync()
    {
        var query = _context.Submissions
            .Include(s => s.Files)
            .Include(s => s.Reviews).ThenInclude(r => r.Reviewer)
            .Include(s => s.Comments).ThenInclude(c => c.User)
            .Include(s => s.Audits)
            .Include(s => s.Responses)
            .Include(s => s.StateHistory)
            .AsSplitQuery()
            .AsQueryable();

        // Apply filters
        if (_options.SubmissionId.HasValue)
        {
            query = query.Where(s => s.Id == _options.SubmissionId.Value);
        }

        if (!string.IsNullOrEmpty(_options.AddressFilter))
        {
            query = query.Where(s => s.Address.Contains(_options.AddressFilter));
        }

        if (_options.StatusFilter.HasValue)
        {
            query = query.Where(s => s.Status == _options.StatusFilter.Value);
        }

        return await query.OrderBy(s => s.Address).ThenBy(s => s.Id).ToListAsync();
    }

    private async Task ExportSubmissionAsync(
        Submission submission,
        string addressPath,
        ExportResult result)
    {
        var folderName = $"{submission.Code} {submission.Status}";
        var submissionPath = Path.Combine(addressPath, SanitizeFolderName(folderName));
        Directory.CreateDirectory(submissionPath);

        if (_options.Verbose)
        {
            Console.WriteLine($"  Exporting submission {submission.Id} ({submission.Code}) - {submission.Status}");
        }

        // Download attached files
        if (submission.Files != null)
        {
            foreach (var file in submission.Files)
            {
                try
                {
                    await DownloadFileAsync(file.BlobName, file.Name, submissionPath);
                    result.FilesDownloaded++;
                    if (_options.Verbose)
                    {
                        Console.WriteLine($"    Downloaded: {file.Name}");
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to download {file.Name} for submission {submission.Id}: {ex.Message}");
                }
            }
        }

        // Download response document if present
        if (!string.IsNullOrEmpty(submission.ResponseDocumentBlob))
        {
            try
            {
                var responseFileName = submission.ResponseDocumentFileName ?? "response.pdf";
                await DownloadFileAsync(submission.ResponseDocumentBlob, responseFileName, submissionPath);
                result.FilesDownloaded++;
                if (_options.Verbose)
                {
                    Console.WriteLine($"    Downloaded response: {responseFileName}");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to download response doc for submission {submission.Id}: {ex.Message}");
            }
        }

        // Generate PDF summary
        try
        {
            var pdfFileName = $"Submission_{submission.Code}_Summary.pdf";
            var pdfPath = Path.Combine(submissionPath, SanitizeFolderName(pdfFileName));
            _pdfGenerator.GenerateSubmissionPdf(submission, pdfPath);
            result.PdfsGenerated++;
            if (_options.Verbose)
            {
                Console.WriteLine($"    Generated PDF: {pdfFileName}");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to generate PDF for submission {submission.Id}: {ex.Message}");
        }

        result.SubmissionsExported++;
    }

    private async Task DownloadFileAsync(string blobName, string fileName, string outputPath)
    {
        using var stream = await _fileStore.RetriveFile(blobName);
        var filePath = Path.Combine(outputPath, SanitizeFolderName(fileName));

        // Handle duplicate filenames
        filePath = GetUniqueFilePath(filePath);

        using var fileStream = System.IO.File.Create(filePath);
        await stream.CopyToAsync(fileStream);
    }

    private static string SanitizeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static string GetUniqueFilePath(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
            return filePath;

        var directory = Path.GetDirectoryName(filePath)!;
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        var counter = 1;

        string newPath;
        do
        {
            newPath = Path.Combine(directory, $"{fileName}_{counter++}{extension}");
        } while (System.IO.File.Exists(newPath));

        return newPath;
    }
}

public class ExportOptions
{
    public string? OutputPath { get; set; }
    public int? SubmissionId { get; set; }
    public string? AddressFilter { get; set; }
    public Status? StatusFilter { get; set; }
    public bool Verbose { get; set; }
    public bool ShowHelp { get; set; }
}

public class ExportResult
{
    public int SubmissionsExported { get; set; }
    public int FilesDownloaded { get; set; }
    public int PdfsGenerated { get; set; }
    public List<string> Errors { get; } = new();
}
