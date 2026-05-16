using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Quay27.Application.Abstractions;

namespace Quay27.Infrastructure.Services;

public sealed class ExcelToPdfConverter : IExcelToPdfConverter
{
    private readonly ILogger<ExcelToPdfConverter> _logger;

    public ExcelToPdfConverter(ILogger<ExcelToPdfConverter> logger) => _logger = logger;

    public async Task<byte[]> ConvertXlsxToPdfAsync(byte[] xlsxBytes, CancellationToken cancellationToken = default)
    {
        var workDir = Path.Combine(Path.GetTempPath(), "quay27-reports", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);

        var xlsxPath = Path.Combine(workDir, "report.xlsx");
        var pdfPath = Path.Combine(workDir, "report.pdf");

        try
        {
            await File.WriteAllBytesAsync(xlsxPath, xlsxBytes, cancellationToken);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await ConvertWithExcelComAsync(xlsxPath, pdfPath, cancellationToken);
            }
            else
            {
                await ConvertWithLibreOfficeAsync(xlsxPath, workDir, cancellationToken);
                if (!File.Exists(pdfPath))
                {
                    var generated = Directory.GetFiles(workDir, "report.pdf").FirstOrDefault()
                        ?? Directory.GetFiles(workDir, "*.pdf").FirstOrDefault();
                    if (generated is not null)
                        pdfPath = generated;
                }
            }

            if (!File.Exists(pdfPath))
                throw new InvalidOperationException(
                    "Không thể chuyển Excel sang PDF. Cài Microsoft Excel (Windows) hoặc LibreOffice (soffice).");

            return await File.ReadAllBytesAsync(pdfPath, cancellationToken);
        }
        finally
        {
            try
            {
                if (Directory.Exists(workDir))
                    Directory.Delete(workDir, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temp report folder {WorkDir}", workDir);
            }
        }
    }

    private static Task ConvertWithExcelComAsync(string xlsxPath, string pdfPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            dynamic? excel = null;
            dynamic? workbook = null;
            try
            {
                var type = Type.GetTypeFromProgID("Excel.Application")
                    ?? throw new InvalidOperationException("Microsoft Excel is not installed.");
                excel = Activator.CreateInstance(type)
                    ?? throw new InvalidOperationException("Could not start Excel.");
                excel.Visible = false;
                excel.DisplayAlerts = false;
                workbook = excel.Workbooks.Open(xlsxPath);
                // xlTypePDF = 0
                workbook.ExportAsFixedFormat(0, pdfPath);
                workbook.Close(false);
                excel.Quit();
            }
            finally
            {
                if (workbook is not null)
                    Marshal.ReleaseComObject(workbook);
                if (excel is not null)
                    Marshal.ReleaseComObject(excel);
            }
        }, cancellationToken);
    }

    private async Task ConvertWithLibreOfficeAsync(string xlsxPath, string outDir, CancellationToken cancellationToken)
    {
        var soffice = ResolveLibreOfficePath()
            ?? throw new InvalidOperationException(
                "LibreOffice (soffice) is not installed. Install LibreOffice or run on Windows with Excel.");

        var args = $"--headless --convert-to pdf --outdir \"{outDir}\" \"{xlsxPath}\"";
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = soffice,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        process.Start();
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            _logger.LogError("LibreOffice convert failed ({Code}): {Error}", process.ExitCode, stderr);
            throw new InvalidOperationException("LibreOffice failed to convert Excel to PDF.");
        }
    }

    private static string? ResolveLibreOfficePath()
    {
        if (OperatingSystem.IsWindows())
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "LibreOffice", "program", "soffice.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "LibreOffice", "program", "soffice.exe"),
            };
            return candidates.FirstOrDefault(File.Exists);
        }

        return File.Exists("/usr/bin/soffice") ? "/usr/bin/soffice" : null;
    }
}
