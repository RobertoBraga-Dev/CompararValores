// Keep this file CodeMaid organised and cleaned
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Metadata;

namespace ClosedXML.Excel.Drawings
{
    [DebuggerDisplay("{Name}")]
    internal class XLPicture : IXLPicture
    {
        private const String InvalidNameChars = @":\/?*[]";
        private static readonly IDictionary<XLPictureFormat, IImageFormat> _formatMap = new Dictionary<XLPictureFormat, IImageFormat>
        {
            [XLPictureFormat.Bmp] = BmpFormat.Instance,
            [XLPictureFormat.Gif] = GifFormat.Instance,
            [XLPictureFormat.Jpeg] = JpegFormat.Instance,
            [XLPictureFormat.Png] = PngFormat.Instance,
            [XLPictureFormat.Tiff] = TiffFormat.Instance,
        };
        private static readonly IDictionary<IImageFormat, XLPictureFormat> _reverseFormatMap = new Dictionary<IImageFormat, XLPictureFormat>
        {
            [BmpFormat.Instance] = XLPictureFormat.Bmp,
            [GifFormat.Instance] = XLPictureFormat.Gif,
            [JpegFormat.Instance] = XLPictureFormat.Jpeg,
            [PngFormat.Instance] = XLPictureFormat.Png,
            [TiffFormat.Instance] = XLPictureFormat.Tiff,
        };
        private Int32 _height;
        private Int32 _id;
        private String _name = string.Empty;
        private Int32 _width;

        internal XLPicture(IXLWorksheet worksheet, Stream stream)
            : this(worksheet)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            this.ImageStream = new MemoryStream();
            {
                stream.Position = 0;
                stream.CopyTo(ImageStream);
                ImageStream.Seek(0, SeekOrigin.Begin);

                using (var image = Image.Load(ImageStream, out var streamFormat))
                {
                    if (_reverseFormatMap.TryGetValue(streamFormat, out var format))
                        this.Format = format;

                    DeduceDimensionsFromImage(image);
                }
                ImageStream.Seek(0, SeekOrigin.Begin);
            }
        }

        internal XLPicture(IXLWorksheet worksheet, Stream stream, XLPictureFormat format)
            : this(worksheet)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            this.Format = format;

            this.ImageStream = new MemoryStream();
            stream.Position = 0;
            stream.CopyTo(ImageStream);
            ImageStream.Seek(0, SeekOrigin.Begin);

            using (var image = Image.Load(ImageStream, out var streamFormat))
            {
                if (_formatMap.TryGetValue(this.Format, out var imageFormat) && imageFormat != streamFormat)
                    throw new ArgumentException("The picture format in the stream and the parameter don't match");

                DeduceDimensionsFromImage(image);
            }
            ImageStream.Seek(0, SeekOrigin.Begin);
        }

        internal XLPicture(IXLWorksheet worksheet, Image image, IImageFormat imageFormat)
            : this(worksheet)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            this.ImageStream = new MemoryStream();
            image.Save(ImageStream, imageFormat);
            ImageStream.Seek(0, SeekOrigin.Begin);
            DeduceDimensionsFromImage(image);

            if (!_reverseFormatMap.TryGetValue(imageFormat, out var pictureFormat))
            {
                throw new ArgumentException("Unsupported or unknown image format");
            }

            this.Format = pictureFormat;
        }

        private XLPicture(IXLWorksheet worksheet)
        {
            this.Worksheet = worksheet ?? throw new ArgumentNullException(nameof(worksheet));
            this.Placement = XLPicturePlacement.MoveAndSize;
            this.Markers = new Dictionary<XLMarkerPosition, XLMarker>()
            {
                [XLMarkerPosition.TopLeft] = null,
                [XLMarkerPosition.BottomRight] = null
            };

            // Calculate default picture ID
            var allPictures = worksheet.Workbook.Worksheets.SelectMany(ws => ws.Pictures);
            if (allPictures.Any())
                this._id = allPictures.Max(p => p.Id) + 1;
            else
                this._id = 1;
        }

        public IXLCell BottomRightCell
        {
            get
            {
                return Markers[XLMarkerPosition.BottomRight].Cell;
            }

            private set
            {
                if (!value.Worksheet.Equals(this.Worksheet))
                    throw new InvalidOperationException("A picture and its anchor cells must be on the same worksheet");

                this.Markers[XLMarkerPosition.BottomRight] = new XLMarker(value);
            }
        }

        public XLPictureFormat Format { get; private set; } = XLPictureFormat.Unknown;

        public Double DpiX { get; private set; } = ImageMetadata.DefaultHorizontalResolution;

        public Double DpiY { get; private set; } = ImageMetadata.DefaultVerticalResolution;

        public Int32 Height
        {
            get { return _height; }
            set
            {
                if (this.Placement == XLPicturePlacement.MoveAndSize)
                    throw new ArgumentException("To set the height, the placement should be FreeFloating or Move");
                _height = value;
            }
        }

        public Int32 Id
        {
            get { return _id; }
            internal set
            {
                if ((Worksheet.Pictures.FirstOrDefault(p => p.Id.Equals(value)) ?? this) != this)
                    throw new ArgumentException($"The picture ID '{value}' already exists.");

                _id = value;
            }
        }

        public MemoryStream ImageStream { get; private set; }

        public Int32 Left
        {
            get { return Markers[XLMarkerPosition.TopLeft]?.Offset.X ?? 0; }
            set
            {
                if (this.Placement != XLPicturePlacement.FreeFloating)
                    throw new ArgumentException("To set the left-hand offset, the placement should be FreeFloating");

                Markers[XLMarkerPosition.TopLeft] = new XLMarker(Worksheet.Cell(1, 1), new Point(value, this.Top));
            }
        }

        public String Name
        {
            get { return _name; }
            set
            {
                if (_name == value) return;

                if ((Worksheet.Pictures.FirstOrDefault(p => p.Name.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? this) != this)
                    throw new ArgumentException($"The picture name '{value}' already exists.");

                SetName(value);
            }
        }

        public Int32 OriginalHeight { get; private set; }

        public Int32 OriginalWidth { get; private set; }

        public XLPicturePlacement Placement { get; set; }

        public Int32 Top
        {
            get { return Markers[XLMarkerPosition.TopLeft]?.Offset.Y ?? 0; }
            set
            {
                if (this.Placement != XLPicturePlacement.FreeFloating)
                    throw new ArgumentException("To set the top offset, the placement should be FreeFloating");

                Markers[XLMarkerPosition.TopLeft] = new XLMarker(Worksheet.Cell(1, 1), new Point(this.Left, value));
            }
        }

        public IXLCell TopLeftCell
        {
            get
            {
                return Markers[XLMarkerPosition.TopLeft].Cell;
            }

            private set
            {
                if (!value.Worksheet.Equals(this.Worksheet))
                    throw new InvalidOperationException("A picture and its anchor cells must be on the same worksheet");

                this.Markers[XLMarkerPosition.TopLeft] = new XLMarker(value);
            }
        }

        public Int32 Width
        {
            get { return _width; }
            set
            {
                if (this.Placement == XLPicturePlacement.MoveAndSize)
                    throw new ArgumentException("To set the width, the placement should be FreeFloating or Move");
                _width = value;
            }
        }

        public IXLWorksheet Worksheet { get; }

        internal IDictionary<XLMarkerPosition, XLMarker> Markers { get; private set; }

        internal String RelId { get; set; }

        /// <summary>
        /// Create a copy of the picture on a different worksheet.
        /// </summary>
        /// <param name="targetSheet">The worksheet to which the picture will be copied.</param>
        /// <returns>A created copy of the picture.</returns>
        public IXLPicture CopyTo(IXLWorksheet targetSheet)
        {
            return CopyTo((XLWorksheet)targetSheet);
        }

        public void Delete()
        {
            Worksheet.Pictures.Delete(this.Name);
        }

        #region IDisposable

        // Used by Janitor.Fody
        private void DisposeManaged()
        {
            this.ImageStream.Dispose();
        }

        public void Dispose()
        {
            // Leave this empty so that Janitor.Fody can do its work
        }

        #endregion IDisposable

        /// <summary>
        /// Create a copy of the picture on the same worksheet.
        /// </summary>
        /// <returns>A created copy of the picture.</returns>
        public IXLPicture Duplicate()
        {
            return CopyTo(Worksheet);
        }

        public Point GetOffset(XLMarkerPosition position)
        {
            return Markers[position].Offset;
        }

        public IXLPicture MoveTo(Int32 left, Int32 top)
        {
            this.Placement = XLPicturePlacement.FreeFloating;
            this.Left = left;
            this.Top = top;
            return this;
        }

        public IXLPicture MoveTo(IXLCell cell)
        {
            return MoveTo(cell, 0, 0);
        }

        public IXLPicture MoveTo(IXLCell cell, Int32 xOffset, Int32 yOffset)
        {
            return MoveTo(cell, new Point(xOffset, yOffset));
        }

        public IXLPicture MoveTo(IXLCell cell, Point offset)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            this.Placement = XLPicturePlacement.Move;
            this.TopLeftCell = cell;
            this.Markers[XLMarkerPosition.TopLeft].Offset = offset;
            return this;
        }

        public IXLPicture MoveTo(IXLCell fromCell, IXLCell toCell)
        {
            return MoveTo(fromCell, 0, 0, toCell, 0, 0);
        }

        public IXLPicture MoveTo(IXLCell fromCell, Int32 fromCellXOffset, Int32 fromCellYOffset, IXLCell toCell, Int32 toCellXOffset, Int32 toCellYOffset)
        {
            return MoveTo(fromCell, new Point(fromCellXOffset, fromCellYOffset), toCell, new Point(toCellXOffset, toCellYOffset));
        }

        public IXLPicture MoveTo(IXLCell fromCell, Point fromOffset, IXLCell toCell, Point toOffset)
        {
            if (fromCell == null) throw new ArgumentNullException(nameof(fromCell));
            if (toCell == null) throw new ArgumentNullException(nameof(toCell));
            this.Placement = XLPicturePlacement.MoveAndSize;

            this.TopLeftCell = fromCell;
            this.Markers[XLMarkerPosition.TopLeft].Offset = fromOffset;

            this.BottomRightCell = toCell;
            this.Markers[XLMarkerPosition.BottomRight].Offset = toOffset;

            return this;
        }

        public IXLPicture Scale(Double factor, Boolean relativeToOriginal = false)
        {
            return this.ScaleHeight(factor, relativeToOriginal).ScaleWidth(factor, relativeToOriginal);
        }

        public IXLPicture ScaleHeight(Double factor, Boolean relativeToOriginal = false)
        {
            this.Height = Convert.ToInt32((relativeToOriginal ? this.OriginalHeight : this.Height) * factor);
            return this;
        }

        public IXLPicture ScaleWidth(Double factor, Boolean relativeToOriginal = false)
        {
            this.Width = Convert.ToInt32((relativeToOriginal ? this.OriginalWidth : this.Width) * factor);
            return this;
        }

        public IXLPicture WithPlacement(XLPicturePlacement value)
        {
            this.Placement = value;
            return this;
        }

        public IXLPicture WithSize(Int32 width, Int32 height)
        {
            this.Width = width;
            this.Height = height;
            return this;
        }

        internal IXLPicture CopyTo(XLWorksheet targetSheet)
        {
            if (targetSheet == null)
                targetSheet = Worksheet as XLWorksheet;

            IXLPicture newPicture;
            if (targetSheet == Worksheet)
                newPicture = targetSheet.AddPicture(ImageStream, Format);
            else
                newPicture = targetSheet.AddPicture(ImageStream, Format, Name);

            newPicture = newPicture
                    .WithPlacement(XLPicturePlacement.FreeFloating)
                    .WithSize(Width, Height)
                    .WithPlacement(Placement);

            switch (Placement)
            {
                case XLPicturePlacement.FreeFloating:
                    newPicture.MoveTo(Left, Top);
                    break;

                case XLPicturePlacement.Move:
                    newPicture.MoveTo(targetSheet.Cell(TopLeftCell.Address), GetOffset(XLMarkerPosition.TopLeft));
                    break;

                case XLPicturePlacement.MoveAndSize:
                    newPicture.MoveTo(targetSheet.Cell(TopLeftCell.Address), GetOffset(XLMarkerPosition.TopLeft), targetSheet.Cell(BottomRightCell.Address),
                        GetOffset(XLMarkerPosition.BottomRight));
                    break;
            }

            return newPicture;
        }

        internal void SetName(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Picture names cannot be empty");

            if (value.IndexOfAny(InvalidNameChars.ToCharArray()) != -1)
                throw new ArgumentException($"Picture names cannot contain any of the following characters: {InvalidNameChars}");

            if (value.Length > 31)
                throw new ArgumentException("Picture names cannot be more than 31 characters");

            _name = value;
        }

        private void DeduceDimensionsFromImage(Image image)
        {
            switch (image.Metadata.ResolutionUnits)
            {
                case PixelResolutionUnit.AspectRatio:
                    this.DpiX = ImageMetadata.DefaultHorizontalResolution;
                    this.DpiY = ImageMetadata.DefaultVerticalResolution;
                    break;
                case PixelResolutionUnit.PixelsPerInch:
                    this.DpiX = Math.Round(image.Metadata.HorizontalResolution);
                    this.DpiY = Math.Round(image.Metadata.VerticalResolution);
                    break;
                case PixelResolutionUnit.PixelsPerCentimeter:
                    this.DpiX = Math.Round(image.Metadata.HorizontalResolution * 2.54);
                    this.DpiY = Math.Round(image.Metadata.VerticalResolution * 2.54);
                    break;
                case PixelResolutionUnit.PixelsPerMeter:
                    this.DpiX = Math.Round(image.Metadata.HorizontalResolution * 0.0254);
                    this.DpiY = Math.Round(image.Metadata.VerticalResolution * 0.0254);
                    break;
                default:
                    throw new NotImplementedException(nameof(image.Metadata.ResolutionUnits));
            }

            this.OriginalWidth = image.Width;
            this.OriginalHeight = image.Height;

            this._width = image.Width;
            this._height = image.Height;
        }
    }
}
