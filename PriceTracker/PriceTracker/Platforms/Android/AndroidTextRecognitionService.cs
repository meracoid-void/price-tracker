using Android.Gms.Tasks;
using Android.Graphics;
using Java.Interop;
using Microsoft.Maui.Controls;
using PriceTracker.Services;
using System.Threading.Tasks;
using Xamarin.Google.MLKit.Vision.Common;
using Xamarin.Google.MLKit.Vision.Text;
using Xamarin.Google.MLKit.Vision.Text.Latin;

[assembly: Dependency(typeof(PriceTracker.Platforms.Android.AndroidTextRecognitionService))]
namespace PriceTracker.Platforms.Android
{
    public class AndroidTextRecognitionService : ITextRecognitionService
    {
        public Task<string> RecognizeTextAsync(byte[] imageData)
        {
            var tcs = new TaskCompletionSource<string>();

            try
            {
                var bitmap = BitmapFactory.DecodeByteArray(imageData, 0, imageData.Length);
                var image = InputImage.FromBitmap(bitmap, 0);

                var recognizer = TextRecognition.GetClient(new TextRecognizerOptions.Builder().Build());

                recognizer.Process(image)
                    .AddOnSuccessListener(new OnSuccessListener(text =>
                    {
                        tcs.SetResult(text.GetText());
                    }))
                    .AddOnFailureListener(new OnFailureListener(ex =>
                    {
                        System.Diagnostics.Debug.WriteLine($"OCR failed: {ex.Message}");
                        tcs.SetResult(string.Empty);
                    }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OCR crashed: {ex.Message}");
                tcs.SetResult(string.Empty);
            }

            return tcs.Task;
        }

        class OnSuccessListener : Java.Lang.Object, IOnSuccessListener
        {
            private readonly Action<Text> _onSuccess;
            public OnSuccessListener(Action<Text> onSuccess) => _onSuccess = onSuccess;
            public void OnSuccess(Java.Lang.Object result) => _onSuccess.Invoke(result.JavaCast<Text>());
        }

        class OnFailureListener : Java.Lang.Object, IOnFailureListener
        {
            private readonly Action<Exception> _onFailure;
            public OnFailureListener(Action<Exception> onFailure) => _onFailure = onFailure;
            public void OnFailure(Java.Lang.Exception e) => _onFailure.Invoke(new Exception(e.Message));
        }
    }
}