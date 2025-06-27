using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceTracker.Services
{
    public interface ITextRecognitionService
    {
        Task<string> RecognizeTextAsync(byte[] imageData);
    }
}
