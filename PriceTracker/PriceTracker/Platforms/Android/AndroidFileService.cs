using AEnv = Android.OS.Environment;
using PriceTracker.Platforms.Android;
using PriceTracker.Services; // adjust namespace

[assembly: Dependency(typeof(AndroidFileService))]
namespace PriceTracker.Platforms.Android
{
    public class AndroidFileService : IFileService
    {
        public string GetDownloadsPath()
        {
            var downloads = AEnv.GetExternalStoragePublicDirectory(AEnv.DirectoryDownloads);
            return downloads.AbsolutePath;
        }
    }
}
