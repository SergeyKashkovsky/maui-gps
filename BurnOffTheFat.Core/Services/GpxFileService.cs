using BurnOffTheFat.Core.Events;
using BurnOffTheFat.Core.Interfaces;

namespace BurnOffTheFat.Core.Services;

public class GpxFileService : IGpxFileService
{
    /// <inheritdoc/>
    public Task<FileSaveResult> CreateGpxFromTextAsync(string fileName, string content)
    {
        throw new NotImplementedException();
    }
}
