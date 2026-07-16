using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Services.Comparison
{
    public interface ITextComparer
    {
        TextPresentation Compare(ComparisonRawText rawText);
    }
}
