using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using HOA.Model;
using HOA.Services;
using HOA.Export.Services;

namespace HOA.Export;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var options = ParseArguments(args);

        if (options.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        // Load configuration
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var sqlConnection = config["SqlConnectionString"];
        var azureConnection = config["AzureStorageConnectionString"];
        var exportPath = options.OutputPath ?? config["ExportPath"] ?? "./export";

        if (string.IsNullOrEmpty(sqlConnection))
        {
            Console.Error.WriteLine("Error: SqlConnectionString is not configured in appsettings.json");
            return 3;
        }

        if (string.IsNullOrEmpty(azureConnection))
        {
            Console.Error.WriteLine("Error: AzureStorageConnectionString is not configured in appsettings.json");
            return 3;
        }

        // Set up DbContext
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(sqlConnection, opts => opts.CommandTimeout(300))
            .Options;

        // Set up AzureFileStore
        AzureFileStore.ConnectionString = azureConnection;
        IFileStore fileStore = new AzureFileStore();

        options.OutputPath = exportPath;

        try
        {
            using var context = new ApplicationDbContext(dbOptions);
            var exportService = new ExportService(context, fileStore, options);

            Console.WriteLine($"Starting export to: {Path.GetFullPath(exportPath)}");
            if (options.Verbose)
            {
                Console.WriteLine($"Filters: Status={options.StatusFilter?.ToString() ?? "All"}, Address={options.AddressFilter ?? "All"}, SubmissionId={options.SubmissionId?.ToString() ?? "All"}");
            }

            var result = await exportService.ExportAsync();

            Console.WriteLine();
            Console.WriteLine("Export Complete:");
            Console.WriteLine($"  Submissions exported: {result.SubmissionsExported}");
            Console.WriteLine($"  Files downloaded: {result.FilesDownloaded}");
            Console.WriteLine($"  PDFs generated: {result.PdfsGenerated}");

            if (result.Errors.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine($"Errors ({result.Errors.Count}):");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  - {error}");
                }
                return 1;
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            if (options.Verbose)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }
            return 2;
        }
    }

    static ExportOptions ParseArguments(string[] args)
    {
        var options = new ExportOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--help":
                case "-h":
                    options.ShowHelp = true;
                    break;
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                        options.OutputPath = args[++i];
                    break;
                case "--status":
                case "-s":
                    if (i + 1 < args.Length && Enum.TryParse<Status>(args[++i], true, out var status))
                        options.StatusFilter = status;
                    break;
                case "--address":
                case "-a":
                    if (i + 1 < args.Length)
                        options.AddressFilter = args[++i];
                    break;
                case "--submission-id":
                case "-id":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var id))
                        options.SubmissionId = id;
                    break;
                case "--verbose":
                case "-v":
                    options.Verbose = true;
                    break;
            }
        }

        return options;
    }

    static void PrintHelp()
    {
        Console.WriteLine("HOA Export Tool - Export submissions to files and PDFs");
        Console.WriteLine();
        Console.WriteLine("Usage: HOA.Export [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -o, --output <path>       Output directory (default: ./export)");
        Console.WriteLine("  -s, --status <status>     Filter by status (e.g., Approved, Rejected)");
        Console.WriteLine("  -a, --address <text>      Filter by address (partial match)");
        Console.WriteLine("  -id, --submission-id <id> Export specific submission by ID");
        Console.WriteLine("  -v, --verbose             Show detailed output");
        Console.WriteLine("  -h, --help                Show this help message");
        Console.WriteLine();
        Console.WriteLine("Status values: CommunityMgrReview, ARBChairReview, CommitteeReview,");
        Console.WriteLine("               ARBTallyVotes, HOALiasonReview, FinalResponse,");
        Console.WriteLine("               Approved, ConditionallyApproved, Rejected,");
        Console.WriteLine("               MissingInformation, Retracted");
    }
}
