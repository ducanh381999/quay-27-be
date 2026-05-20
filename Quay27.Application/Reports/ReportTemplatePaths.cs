namespace Quay27.Application.Reports;

public static class ReportTemplatePaths
{
    public static string Resolve(string templateFileName)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Templates", "Reports", templateFileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Reports", templateFileName),
        };
        var found = candidates.FirstOrDefault(File.Exists);
        if (found is not null)
            return found;
        return candidates[0];
    }
}
