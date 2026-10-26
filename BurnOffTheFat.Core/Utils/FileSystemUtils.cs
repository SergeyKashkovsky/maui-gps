namespace BurnOffTheFat.Core.Utils;
/// <summary>
/// Утилиты для работы с файловой системой, в том числе и Android
/// </summary>
public class FileSystemUtils
{
    /// <summary>
    /// Получаем каталог, доступный и для приложения и для пользователей
    /// </summary>
    /// <returns></returns>
    public static string GetApllicationPublicDirectory()
    {
        var appDirectory = FileSystem.Current.AppDataDirectory;

#if ANDROID
        // Для Android берем специальную папку Documents внутри Android/data/..., доступную по USB
        var context = Android.App.Application.Context;
        var externalFilesDir = context.GetExternalFilesDir(Android.OS.Environment.DirectoryDocuments);
        if (externalFilesDir != null)
        {
            appDirectory = externalFilesDir.AbsolutePath;
        }
#endif
        return appDirectory;
    }
}
