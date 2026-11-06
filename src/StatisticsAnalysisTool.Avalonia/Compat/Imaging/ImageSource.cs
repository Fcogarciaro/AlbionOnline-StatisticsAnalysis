// Compatibility shim: System.Windows.Media.ImageSource / BitmapSource. Both implement Avalonia's IImage by
// delegating to the wrapped Avalonia bitmap, so ported views can bind them straight to Image.Source.
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace System.Windows.Media
{
    public sealed class ExceptionEventArgs(Exception errorException) : EventArgs
    {
        public Exception ErrorException { get; } = errorException;
    }

    public abstract class ImageSource : Freezable, IImage
    {
        public abstract Bitmap AvaloniaBitmap { get; }

        public double Width => AvaloniaBitmap?.Size.Width ?? 0;

        public double Height => AvaloniaBitmap?.Size.Height ?? 0;

        Size IImage.Size => AvaloniaBitmap?.Size ?? default;

        void IImage.Draw(DrawingContext context, Rect sourceRect, Rect destRect) =>
            ((IImage) AvaloniaBitmap)?.Draw(context, sourceRect, destRect);
    }
}

namespace System.Windows.Media.Imaging
{
    public abstract class BitmapSource : ImageSource
    {
        public int PixelWidth => AvaloniaBitmap?.PixelSize.Width ?? 0;

        public int PixelHeight => AvaloniaBitmap?.PixelSize.Height ?? 0;

        public virtual bool IsDownloading => false;

        public event EventHandler DownloadCompleted;

        public event EventHandler<ExceptionEventArgs> DownloadFailed;

        protected void RaiseDownloadCompleted() => DownloadCompleted?.Invoke(this, EventArgs.Empty);

        protected void RaiseDownloadFailed(Exception exception) => DownloadFailed?.Invoke(this, new ExceptionEventArgs(exception));
    }

    public sealed class BitmapFrame : BitmapSource
    {
        private readonly BitmapSource _source;

        private BitmapFrame(BitmapSource source)
        {
            _source = source;
        }

        public override Bitmap AvaloniaBitmap => _source.AvaloniaBitmap;

        public static BitmapFrame Create(BitmapSource source) => new(source ?? throw new ArgumentNullException(nameof(source)));
    }

    public class PngBitmapEncoder
    {
        public IList<BitmapFrame> Frames { get; } = new List<BitmapFrame>();

        public void Save(IO.Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            var bitmap = Frames.FirstOrDefault()?.AvaloniaBitmap
                         ?? throw new InvalidOperationException("No frame (or no decoded image) to encode.");
            bitmap.Save(stream);
        }
    }
}
