﻿using Microsoft.Extensions.Logging;
using PriceTracker.Services;

namespace PriceTracker
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
#if ANDROID
            builder.Services.AddSingleton<PriceTracker.Services.IFileService, PriceTracker.Platforms.Android.AndroidFileService>();
            builder.Services.AddSingleton<ITextRecognitionService, PriceTracker.Platforms.Android.AndroidTextRecognitionService>();
#endif
#if DEBUG
    		builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<ExportService>();

            return builder.Build();
        }
    }
}
