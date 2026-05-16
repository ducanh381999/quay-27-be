namespace Quay27.Application.Abstractions;

public interface IExcelToPdfConverter
{
    Task<byte[]> ConvertXlsxToPdfAsync(byte[] xlsxBytes, CancellationToken cancellationToken = default);
}
