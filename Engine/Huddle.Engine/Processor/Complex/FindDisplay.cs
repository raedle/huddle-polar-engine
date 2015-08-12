﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using AForge.Vision.GlyphRecognition;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Extensions;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Huddle.Engine.Data;
using Huddle.Engine.Extensions;
using Huddle.Engine.Util;
using Point = System.Drawing.Point;

namespace Huddle.Engine.Processor.Complex
{
    [ViewTemplate("Find Display", "FindDisplay")]
    public class FindDisplay : UMatProcessor
    {
        #region const

        #endregion

        #region static fields

        #endregion

        #region member fields

        private readonly GlyphRecognizer _glyphRecognizer;
        private GlyphMetadata[] _glyphTable;

        private readonly Dictionary<String, Glyph> _recognizedGlyphs = new Dictionary<string, Glyph>();
        private readonly Dictionary<String, Point[]> _recognizedQuads = new Dictionary<String, Point[]>();

        private readonly Dictionary<long, DisplaySample> _blobFoundInRgbImage = new Dictionary<long, DisplaySample>();

        #endregion

        #region properties

        #region InputImageBitmapSource

        /// <summary>
        /// The <see cref="InputImageBitmapSource" /> property's name.
        /// </summary>
        public const string InputImageBitmapSourcePropertyName = "InputImageBitmapSource";

        private BitmapSource _inputImageBitmapSource;

        /// <summary>
        /// Sets and gets the InputImageBitmapSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource InputImageBitmapSource
        {
            get
            {
                return _inputImageBitmapSource;
            }

            set
            {
                if (_inputImageBitmapSource == value)
                {
                    return;
                }

                RaisePropertyChanging(InputImageBitmapSourcePropertyName);
                _inputImageBitmapSource = value;
                RaisePropertyChanged(InputImageBitmapSourcePropertyName);
            }
        }

        #endregion

        #region BinaryThresholdImageBitmapSource

        /// <summary>
        /// The <see cref="BinaryThresholdImageBitmapSource" /> property's name.
        /// </summary>
        public const string BinaryThresholdImageBitmapSourcePropertyName = "BinaryThresholdImageBitmapSource";

        private BitmapSource _binaryThresholdImageBitmapSource;

        /// <summary>
        /// Sets and gets the BinaryThresholdImageBitmapSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource BinaryThresholdImageBitmapSource
        {
            get
            {
                return _binaryThresholdImageBitmapSource;
            }

            set
            {
                if (_binaryThresholdImageBitmapSource == value)
                {
                    return;
                }

                RaisePropertyChanging(BinaryThresholdImageBitmapSourcePropertyName);
                _binaryThresholdImageBitmapSource = value;
                RaisePropertyChanged(BinaryThresholdImageBitmapSourcePropertyName);
            }
        }

        #endregion

        #region DebugImageBitmapSource

        /// <summary>
        /// The <see cref="DebugImageBitmapSource" /> property's name.
        /// </summary>
        public const string DebugImageBitmapSourcePropertyName = "DebugImageBitmapSource";

        private BitmapSource _debugImageBitmapSource;

        /// <summary>
        /// Sets and gets the DebugImageBitmapSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource DebugImageBitmapSource
        {
            get
            {
                return _debugImageBitmapSource;
            }

            set
            {
                if (_debugImageBitmapSource == value)
                {
                    return;
                }

                RaisePropertyChanging(DebugImageBitmapSourcePropertyName);
                _debugImageBitmapSource = value;
                RaisePropertyChanged(DebugImageBitmapSourcePropertyName);
            }
        }

        #endregion

        #region BinaryThreshold

        /// <summary>
        /// The <see cref="BinaryThreshold" /> property's name.
        /// </summary>
        public const string BinaryThresholdPropertyName = "BinaryThreshold";

        private byte _binaryThreshold = 220;

        /// <summary>
        /// Sets and gets the BinaryThreshold property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public byte BinaryThreshold
        {
            get
            {
                return _binaryThreshold;
            }

            set
            {
                if (_binaryThreshold == value)
                {
                    return;
                }

                RaisePropertyChanging(BinaryThresholdPropertyName);
                _binaryThreshold = value;
                RaisePropertyChanged(BinaryThresholdPropertyName);
            }
        }

        #endregion

        #region MinFramesProperty

        /// <summary>
        /// The <see cref="MinFramesProperty" /> property's name.
        /// </summary>
        public const string MinFramesPropertyName = "MinFramesProperty";

        private int _minFramesProperty = 1;

        /// <summary>
        /// Sets and gets the MinFramesProperty property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int MinFramesProperty
        {
            get
            {
                return _minFramesProperty;
            }

            set
            {
                if (_minFramesProperty == value)
                {
                    return;
                }

                RaisePropertyChanging(MinFramesPropertyName);
                _minFramesProperty = value;
                RaisePropertyChanged(MinFramesPropertyName);
            }
        }

        #endregion

        #region FloodFillDifference

        /// <summary>
        /// The <see cref="FloodFillDifference" /> property's name.
        /// </summary>
        public const string FloodFillDifferencePropertyName = "FloodFillDifference";

        private float _floodFillDifference = 25.0f;

        /// <summary>
        /// Sets and gets the FloodFillDifference property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public float FloodFillDifference
        {
            get
            {
                return _floodFillDifference;
            }

            set
            {
                if (_floodFillDifference == value)
                {
                    return;
                }

                RaisePropertyChanging(FloodFillDifferencePropertyName);
                _floodFillDifference = value;
                RaisePropertyChanged(FloodFillDifferencePropertyName);
            }
        }

        #endregion

        #region RoiExpandFactor

        /// <summary>
        /// The <see cref="RoiExpandFactor" /> property's name.
        /// </summary>
        public const string RoiExpandFactorPropertyName = "RoiExpandFactor";

        private float _roiExpandFactor = 0.02f;

        /// <summary>
        /// Sets and gets the RoiExpandFactor property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public float RoiExpandFactor
        {
            get
            {
                return _roiExpandFactor;
            }

            set
            {
                if (_roiExpandFactor == value)
                {
                    return;
                }

                RaisePropertyChanging(RoiExpandFactorPropertyName);
                _roiExpandFactor = value;
                RaisePropertyChanged(RoiExpandFactorPropertyName);
            }
        }

        #endregion

        #region IsFindDisplayContinuously

        /// <summary>
        /// The <see cref="IsFindDisplayContiuously" /> property's name.
        /// </summary>
        public const string IsFindDisplayContiuouslyPropertyName = "IsFindDisplayContiuously";

        private bool _isFindDisplayContiuously = false;

        /// <summary>
        /// Sets and gets the IsFindDisplayContiuously property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsFindDisplayContiuously
        {
            get
            {
                return _isFindDisplayContiuously;
            }

            set
            {
                if (_isFindDisplayContiuously == value)
                {
                    return;
                }

                RaisePropertyChanging(IsFindDisplayContiuouslyPropertyName);
                _isFindDisplayContiuously = value;
                RaisePropertyChanged(IsFindDisplayContiuouslyPropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        public FindDisplay()
        {
            // create glyph database for 5x5 glyphs
            var glyphDatabase = new GlyphDatabase(5);

            try
            {
                using (var stream = new StreamReader("Resources/TagDefinitions.txt"))
                {
                    String line;
                    do
                    {
                        line = stream.ReadLine();
                        if (line != null)
                        {
                            var tokens = line.Split(' ');
                            var tagId = tokens[0];
                            var code = tokens[1];

                            var matrix = new byte[5, 5];

                            var i = 0;
                            for (var y = 0; y < 5; y++)
                            {
                                for (var x = 0; x < 5; x++)
                                {
                                    matrix[y, x] = byte.Parse(code.Substring(i++, 1));
                                }
                            }
                            glyphDatabase.Add(new Glyph(tagId, matrix));
                        }
                    } while (line != null);
                }

                _glyphRecognizer = new GlyphRecognizer(glyphDatabase)
                {
                    MaxNumberOfGlyphsToSearch = 1
                };
                _glyphTable = new GlyphMetadata[glyphDatabase.Count];

                PropertyChanged += (s, e) =>
                {
                    switch (e.PropertyName)
                    {
                        case MinFramesPropertyName:
                            _glyphTable = new GlyphMetadata[glyphDatabase.Count];
                            break;
                    }
                };

            }
            catch (Exception e)
            {
                LogFormat(e + e.Message + e.StackTrace);
            }
        }

        #endregion

        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            var devices = dataContainer.OfType<Device>().ToArray();
            var unknownDevices = devices.Where(d => !d.IsIdentified).ToArray();
            if (!devices.Any())
                return base.PreProcess(dataContainer);

            // For debugging the flag IsFindDisplayContinuously can be set 'true' -> 'false' is recommended however
            var devicesToFind = IsFindDisplayContiuously ? devices : unknownDevices;

            if (!devicesToFind.Any()) return null;

            var rgbImages = dataContainer.OfType<UMatData>().ToArray();

            // Do only process if RGB image is set
            if (!rgbImages.Any())
                return null;

            UMat u_rgbImage = rgbImages.First().Data.DeepClone();
            var debugImage = u_rgbImage.DeepClone();
            //UMat u_debugImage = debugImage.ToUMat();

            if (IsRenderContent)
            {
                var u_lastRgbImageCopy = u_rgbImage.DeepClone();
                Task.Factory.StartNew(() =>
                {
                    var bitmapSource = u_lastRgbImageCopy.ToImage().ToBitmapSource(true);
                    u_lastRgbImageCopy.Dispose();
                    return bitmapSource;
                }).ContinueWith(t => InputImageBitmapSource = t.Result);
            }

            var u_colorImage = u_rgbImage.DeepClone();

            // TODO: is the copy required or does convert already create a copy? _lastRgbImage.Copy()
            //var grayscaleImage = u_rgbImage.DeepClone().ToImage<Rgb, byte>().Convert<Gray, byte>();
            UMat grayscaleImage = new UMat();
            CvInvoke.CvtColor(u_rgbImage, grayscaleImage, ColorConversion.Rgb2Gray);

            var width = u_rgbImage.Cols;
            var height = u_rgbImage.Rows;

            foreach (var device in devicesToFind)
            {
                ProcessDevice(device,
                    u_colorImage,
                    grayscaleImage,
                    width, height,
                    ref debugImage);
            }

            u_colorImage.Dispose();
            grayscaleImage.Dispose();
            Push();

            if (IsRenderContent)
            {
                // draw debug output
                var debugImageCopy = debugImage.Clone();
                Task.Factory.StartNew(() =>
                {
                    var bitmapSource = debugImageCopy.ToBitmapSource(true);
                    debugImageCopy.Dispose();
                    return bitmapSource;
                }).ContinueWith(t => DebugImageBitmapSource = t.Result);
            }

            debugImage.Dispose();

            return null;
        }

        public override IData Process(IData data)
        {
            return null;
        }

        public override UMatData ProcessAndView(UMatData data)
        {
            throw new NotImplementedException();
        }

        #region private methods

        private void ProcessDevice(Device device, /*Image<Rgb, byte>*/UMat colorImage, /*Image<Gray, byte>*/UMat grayscaleImage, int width, int height, ref /*Image<Rgb, byte>*/ UMat debugImage)
        {
            var deviceRoi = CalculateRoiFromNormalizedBounds(device.Area, colorImage);
            deviceRoi = deviceRoi.GetInflatedBy(RoiExpandFactor, new Rectangle(0,0, colorImage.Size.Width, colorImage.Size.Height) /*CvInvoke.cvGetImageROI(colorImage)*/);

            var imageRoi = CvInvoke.cvGetImageROI(colorImage);

            #region workaround
            //var __i = colorImage.ToImage<Rgb, byte>();

            deviceRoi.Width = 0;
            deviceRoi.X = 0;

            //CvInvoke.cvSetImageROI(colorImage, deviceRoi);
            colorImage = new UMat(colorImage, deviceRoi);

            //colorImage = __i.ToUMat();
            #endregion

            List<Point[]> quadrilaterals;
            var markers = GetMarkers(ref colorImage, deviceRoi, width, height, ref debugImage, out quadrilaterals);
            if (markers == null)
            {
                return;
            }

            CvInvoke.cvSetImageROI(colorImage, imageRoi);

            var grayscaleImageRoi = CvInvoke.cvGetImageROI(grayscaleImage);
            CvInvoke.cvSetImageROI(grayscaleImage, deviceRoi);

            var i = 0;
            foreach (var marker in markers)
            {
                CvInvoke.FillConvexPoly(grayscaleImage,
                    new Emgu.CV.Util.VectorOfPoint(quadrilaterals[i]),
                    Rgbs.White.MCvScalar);

                var display = FindDisplayInImage(ref grayscaleImage, deviceRoi, width, height, marker, ref debugImage);

                if (display != null)
                {
                    if (IsRenderContent && IsFindDisplayContiuously)
                    {
                        var debugImageRoi = CvInvoke.cvGetImageROI(debugImage);
                        CvInvoke.cvSetImageROI(debugImage, deviceRoi);

                        var enclosingRectangle = display.EnclosingRectangle;
                        DrawEdge(ref debugImage, enclosingRectangle.LongEdge, Rgbs.Red);
                        DrawEdge(ref debugImage, enclosingRectangle.ShortEdge, Rgbs.Green);

                        CvInvoke.cvSetImageROI(debugImage, debugImageRoi);
                    }

                    DisplaySample displaySample;
                    if (_blobFoundInRgbImage.ContainsKey(device.BlobId))
                    {
                        displaySample = _blobFoundInRgbImage[device.BlobId];
                    }
                    else
                    {
                        displaySample = new DisplaySample();
                        _blobFoundInRgbImage.Add(device.BlobId, displaySample);
                    }

                    if (displaySample.NeedsSample())
                    {
                        displaySample.Sample(display.EnclosingRectangle);
                    }
                    else
                    {
                        _blobFoundInRgbImage.Remove(device.BlobId);
                        display.EnclosingRectangle = displaySample.GetBestSample();

                        Stage(display);
                    }

                    //Stage(display);
                }

                i++;
            }

            CvInvoke.cvSetImageROI(grayscaleImage, grayscaleImageRoi);
        }

        private IEnumerable<Marker> GetMarkers(ref /*Image<Rgb, byte>*/UMat image, Rectangle roi, int width, int height, ref /*Image<Rgb, byte>*/UMat debugImage, out List<Point[]> quadrilaterals)
        {
            if (_glyphRecognizer == null)
            {
                quadrilaterals = new List<Point[]>();
                return null;
            }
            if (image.Bitmap == null)
            {
                quadrilaterals = new List<Point[]>();
                return null;
            }

            var imageWidth = image.Cols;
            var imageHeight = image.Rows;
            var imageRoi = CvInvoke.cvGetImageROI(image);

            var markers = new List<Marker>();

            var minFramesProperty = MinFramesProperty;

            // Draw new Roi
            if (IsRenderContent)
            {
                CvInvoke.Rectangle(debugImage, roi, Rgbs.Red.MCvScalar, 2);
            }

            _recognizedGlyphs.Clear();
            _recognizedQuads.Clear();

            var glyphs = _glyphRecognizer.FindGlyphs(image.Bitmap);

            foreach (var glyph in glyphs)
            {
                // highlight quadrilateral (of all found glyphs)
                var tmpQuad = glyph.Quadrilateral;

                if (tmpQuad == null || tmpQuad.Count != 4) continue;

                var quad = new[]
                {
                    new Point(tmpQuad[0].X, tmpQuad[0].Y),
                    new Point(tmpQuad[1].X, tmpQuad[1].Y),
                    new Point(tmpQuad[2].X, tmpQuad[2].Y),
                    new Point(tmpQuad[3].X, tmpQuad[3].Y)
                };

                if (IsRenderContent)
                {
                    var debugImageRoi = CvInvoke.cvGetImageROI(debugImage);
                    CvInvoke.cvSetImageROI(debugImage, roi);

                    CvInvoke.Polylines(debugImage, quad, true, Rgbs.Yellow.MCvScalar);

                    CvInvoke.cvSetImageROI(debugImage, debugImageRoi);
                }

                // if glyphs are recognized then store and draw name 
                var recQuad = glyph.RecognizedQuadrilateral;
                var recGlyph = glyph.RecognizedGlyph;
                if (recQuad != null && recGlyph != null)
                {
                    _recognizedGlyphs.Add(recGlyph.Name, recGlyph);
                    _recognizedQuads.Add(recGlyph.Name, quad);

                    if (IsRenderContent)
                    {
                        var debugImageRoi = CvInvoke.cvGetImageROI(debugImage);
                        CvInvoke.cvSetImageROI(debugImage, roi);

                        var labelPos = new Point(recQuad[2].X, recQuad[2].Y);
                        debugImage.Draw(recGlyph.Name, EmguFontBig.Font, EmguFontBig.Scale, labelPos, Rgbs.Green);

                        CvInvoke.cvSetImageROI(debugImage, debugImageRoi);
                    }
                }
            }

            // update all entries in glyph table using recognized glyphs

            quadrilaterals = new List<Point[]>();
            var length = _glyphTable.Length;
            for (int i = 0; i < length; i++)
            {
                var name = (i + 1).ToString(CultureInfo.InvariantCulture);

                Glyph glyph = null;
                Point[] quad = null;
                if (_recognizedGlyphs.ContainsKey(name)) glyph = _recognizedGlyphs[name];
                if (_recognizedQuads.ContainsKey(name)) quad = _recognizedQuads[name];

                // if glyph for this entry was recognized, update entry
                if (glyph != null && quad != null)
                {
                    quadrilaterals.Add(quad);

                    // if there is no entry yet, create it
                    if (_glyphTable[i] == null)
                        _glyphTable[i] = new GlyphMetadata();

                    var gmd = _glyphTable[i];

                    gmd.aliveFrames++;

                    Point upVector1 = quad[0].Sub(quad[3]);
                    Point upVector2 = quad[1].Sub(quad[2]);
                    upVector1 = (upVector1.Add(upVector2)).Div(2);

                    double orientation = Math.Atan2(upVector1.Y, upVector2.X);

                    // always keep only the last X frames in list
                    if (gmd.prevX.Count == minFramesProperty)
                    {
                        gmd.prevX.RemoveAt(0);
                        gmd.prevY.RemoveAt(0);
                        gmd.prevOrientations.RemoveAt(0);
                    }
                    gmd.prevX.Add(quad[0].X);
                    gmd.prevY.Add(quad[0].Y);
                    gmd.prevOrientations.Add(orientation);

                    // check if marker stops moving and rotating
                    if (Math.Abs(gmd.prevX.Max() - gmd.prevX.Min()) < 5
                        && Math.Abs(gmd.prevY.Max() - gmd.prevY.Min()) < 5
                        && gmd.aliveFrames >= minFramesProperty)
                    {
                        var x = orientation + Math.PI / 2;
                        var degOrientation = (x > 0.0 ? x : (2.0 * Math.PI + x)) * 360 / (2.0 * Math.PI);

                        // find bounding rectangle
                        float minX = image.Cols;
                        float minY = image.Rows;
                        float maxX = 0;
                        float maxY = 0;

                        foreach (Point p in quad)
                        {
                            minX = Math.Min(minX, p.X);
                            minY = Math.Min(minY, p.Y);
                            maxX = Math.Max(maxX, p.X);
                            maxY = Math.Max(maxY, p.Y);
                        }

                        var centerX = roi.X + minX + (maxX - minX) / 2.0f;
                        var centerY = roi.Y + minY + (maxY - minY) / 2.0f;

                        markers.Add(new Marker(this, "Display")
                        {
                            Id = name,
                            Center = new System.Windows.Point(centerX / width, centerY / height),
                            RelativeCenter = new System.Windows.Point(centerX / imageWidth, centerY / imageHeight),
                            Angle = degOrientation
                        });

                        // Render center and orientation of marker
                        if (IsRenderContent)
                        {
                            var markerCenter = new PointF(centerX, centerY);
                            var p2 = new Point(
                                (int)(markerCenter.X + Math.Cos(orientation) * 100.0),
                                (int)(markerCenter.Y + Math.Sin(orientation) * 100.0)
                                );
                            var p3 = new Point(
                                (int)(markerCenter.X + Math.Cos(orientation + Math.PI / 16) * 75.0),
                                (int)(markerCenter.Y + Math.Sin(orientation + Math.PI / 16) * 75.0)
                                );

                            // draw a cross
                            CvInvoke.Line(debugImage,
                                new Point((int)markerCenter.X - 3, (int)markerCenter.Y),
                                new Point((int)markerCenter.X + 3,(int)markerCenter.Y),
                                Rgbs.Green.MCvScalar,
                                2);
                            CvInvoke.Line(debugImage,
                                new Point((int)markerCenter.X, (int)markerCenter.Y - 3),
                                new Point((int)markerCenter.X, (int)markerCenter.Y + 3),
                                Rgbs.Green.MCvScalar,
                                2);

                            CvInvoke.Line(debugImage,
                                new Point((int)markerCenter.X, (int)markerCenter.Y),
                                p2,
                                Rgbs.Green.MCvScalar,
                                2);
                            debugImage.Draw(string.Format("{0} deg", Math.Round(degOrientation, 1)), EmguFont.Font, EmguFont.Scale, p3, Rgbs.Green);
                        }
                    }
                    else
                    {
                        // if glyph disappeared remove entry from table
                        _recognizedGlyphs[name] = null;
                    }
                }
            }

            CvInvoke.cvSetImageROI(image, imageRoi);

            return markers;
        }

        private static Rectangle CalculateRoiFromNormalizedBounds(Rect inputRect, UMat inputImage, int marginX = 0, int marginY = 0)
        {
            var width = inputImage.Cols;
            var height = inputImage.Rows;

            var offsetX = (int)(inputRect.X * width);
            var offsetY = (int)(inputRect.Y * height);

            var roiX = Math.Max(0, offsetX - marginX);
            var roiY = Math.Max(0, offsetY - marginY);
            var roiWidth = (int)Math.Min(width - roiX, inputRect.Width * width + 2 * marginX);
            var roiHeight = (int)Math.Min(height - roiY, inputRect.Height * height + 2 * marginY);

            return new Rectangle(
                    roiX,
                    roiY,
                    roiWidth,
                    roiHeight
                    );
        }

        private Marker FindDisplayInImage(ref /*Image<Gray, byte>*/UMat grayscaleImage, Rectangle roi, int width, int height, Marker marker, ref /*Image<Rgb, byte>*/UMat debugImage)
        {
            var imageWidth = grayscaleImage.Cols;
            var imageHeight = grayscaleImage.Rows;

            var x = (int)(marker.RelativeCenter.X * imageWidth) - roi.X;
            var y = (int)(marker.RelativeCenter.Y * imageHeight) - roi.Y;

            var grayscaleImageRoi = CvInvoke.cvGetImageROI(grayscaleImage);
            CvInvoke.cvSetImageROI(grayscaleImage, roi);

            var enclosingRectangle = FindEnclosingRectangle(ref grayscaleImage, new Point(x, y), ref debugImage, roi);

            CvInvoke.cvSetImageROI(grayscaleImage, grayscaleImageRoi);

            if (enclosingRectangle == null) return null;

            return new Marker(this, "Display")
            {
                Id = marker.Id,
                Center = marker.Center,
                Angle = marker.Angle,
                RgbImageToDisplayRatio = new Ratio
                {
                    X = width / enclosingRectangle.LongEdge.Length,
                    Y = height / enclosingRectangle.ShortEdge.Length
                },
                EnclosingRectangle = enclosingRectangle
            };
        }

        private EnclosingRectangle FindEnclosingRectangle(ref /*Image<Gray, byte>*/UMat grayscaleImage, Point center, ref /*Image<Rgb, byte>*/UMat debugImage, Rectangle roi)
        {
            var binaryThreshold = BinaryThreshold;
            UMat binaryThresholdImage = new UMat();
            CvInvoke.Threshold(grayscaleImage,
                binaryThresholdImage,
                binaryThreshold,
                255,
                ThresholdType.BinaryInv);

            #region Debug Binary Image

            // Binary Threshold Image
            var binaryThresholdImageCopy = binaryThresholdImage.Copy();
            Task.Factory.StartNew(() =>
            {
                var bitmapSource = binaryThresholdImageCopy.ToBitmapSource(true);
                binaryThresholdImageCopy.Dispose();
                return bitmapSource;
            }).ContinueWith(t => BinaryThresholdImageBitmapSource = t.Result);

            #endregion

            var floodFillImage = binaryThresholdImage.Clone();
            binaryThresholdImage.Dispose();

            var imageWidth = floodFillImage.Cols;
            var imageHeight = floodFillImage.Rows;

            MCvConnectedComp comp;

            // mask needs to be 2 pixels wider and 2 pixels taller
            var mask = new Image<Gray, byte>(imageWidth + 2, imageHeight + 2);
            CvInvoke.FloodFill(floodFillImage,
                mask,
                center,
                new MCvScalar(255),
                out comp.Rect,
                new MCvScalar(FloodFillDifference),
                new MCvScalar(FloodFillDifference),
                Connectivity.FourConnected,
                FloodFillType.Default);

            #region Debug Flood Fill Image

            //// Flood fill image
            //var maskCopy = floodFillImage.Copy();
            //Task.Factory.StartNew(() =>
            //{
            //    var bitmapSource = maskCopy.ToBitmapSource(true);
            //    maskCopy.Dispose();
            //    return bitmapSource;
            //}).ContinueWith(t => BinaryThresholdImageBitmapSource = t.Result);

            #endregion

            // shrink mask back to original grayscale image size
            mask.ROI = new Rectangle(1, 1, imageWidth, imageHeight);
            var contourBinaryImage = mask.Mul(255);

            #region Debug Output Binary Image

            //var maskCopy = contourBinaryImage.Copy();
            //Task.Factory.StartNew(() =>
            //{
            //    var bitmapSource = maskCopy.ToBitmapSource(true);
            //    maskCopy.Dispose();
            //    return bitmapSource;
            //}).ContinueWith(t => BinaryThresholdImageBitmapSource = t.Result);

            #endregion

            mask.Dispose();
            floodFillImage.Dispose();

            EnclosingRectangle enclosingRectangle = null;
            Emgu.CV.Util.VectorOfVectorOfPoint contours = new Emgu.CV.Util.VectorOfVectorOfPoint();
            CvInvoke.FindContours(contourBinaryImage,
                contours,
                null,
                RetrType.External,
                ChainApproxMethod.ChainApproxSimple);

            for (int i = 0; i < contours.Size; i++ )
            {
                Emgu.CV.Util.VectorOfPoint contour = new Emgu.CV.Util.VectorOfPoint();
                CvInvoke.ApproxPolyDP(contours[i],
                    contour,
                    CvInvoke.ArcLength(contours[i], true) * 0.05,
                    true);

                var contourBounds = CvInvoke.BoundingRectangle(contour);
                if (contourBounds.Width + 5 >= roi.Width || contourBounds.Height + 5 >= roi.Height)
                    continue;

                if (!EmguExtensions.IsRectangle(contour.ToArray(), 10.0)) continue;

                var edges = GetRightAngleEdges(contour.ToArray());

                if (IsRenderContent)
                {
                    var debugImageRoi = CvInvoke.cvGetImageROI(debugImage);
                    CvInvoke.cvSetImageROI(debugImage, roi);

                    Emgu.CV.Util.VectorOfPoint ret = null;
                    CvInvoke.ConvexHull(contour,
                        ret,
                        true,
                        true);
                    CvInvoke.Polylines(debugImage, ret, false, Rgbs.Cyan.MCvScalar, 2);
                    var vert = CvInvoke.MinAreaRect(contour).GetVertices();
                    Point[] vertices = { new Point((int)vert[0].X, (int)vert[0].Y),
                                        new Point((int)vert[1].X, (int)vert[1].Y),
                                        new Point((int)vert[2].X, (int)vert[2].Y),
                                        new Point((int)vert[3].X, (int)vert[3].Y)};

                    CvInvoke.Polylines(debugImage,
                        new Emgu.CV.Util.VectorOfPoint(vertices),
                        true,
                        Rgbs.Cyan.MCvScalar,
                        2);

                    DrawEdge(ref debugImage, edges[0], Rgbs.Red);
                    DrawEdge(ref debugImage, edges[1], Rgbs.Green);

                    CvInvoke.cvSetImageROI(debugImage, debugImageRoi);
                }

                enclosingRectangle = new EnclosingRectangle
                {
                    Contour = contour.ToArray(),
                    LongEdge = edges[0],
                    ShortEdge = edges[1]
                };
            }

            contourBinaryImage.Dispose();

            return enclosingRectangle;
        }

        private LineSegment2D[] GetRightAngleEdges(Point[] contour)
        {
            var edges = PointCollection.PolyLine(contour, true);

            var longestEdge = edges[0];
            var index = 0;
            for (var i = 1; i < edges.Length; i++)
            {
                var edge = edges[i];

                // Assumption is that the longest edge defines the width of the tracked device in the blob
                if (edge.Length > longestEdge.Length)
                {
                    index = i;
                    longestEdge = edges[i];
                }
            }

            var nextEdgeToLongestEdge = edges[(index + 1) % edges.Length];

            return new[] { longestEdge, nextEdgeToLongestEdge };
        }

        private void DrawEdge(ref /*Image<Rgb, byte>*/UMat debugImage, LineSegment2D edge, Rgb color)
        {
            CvInvoke.Line(debugImage, edge.P1, edge.P2, color.MCvScalar, 10);

            var p1 = edge.P1;
            var p2 = edge.P2;

            var minX = Math.Min(p1.X, p2.X);
            var minY = Math.Min(p1.Y, p2.Y);
            var maxX = Math.Max(p1.X, p2.X);
            var maxY = Math.Max(p1.Y, p2.Y);

            var centerX = minX + (maxX - minX) / 2;
            var centerY = minY + (maxY - minY) / 2;

            debugImage.Draw(string.Format("{0:F1}", edge.Length), EmguFontBig.Font, EmguFontBig.Scale, new Point(centerX, centerY), color);
        }

        #endregion

        public override Bitmap[] TakeSnapshots()
        {
            return new[]
            {
                DebugImageBitmapSource.BitmapFromSource()
            };
        }
    }

    internal class GlyphMetadata
    {
        public Glyph Glyph { get; set; }
        public List<double> prevOrientations = new List<double> { -10000.0 };
        public List<double> prevX = new List<double> { -10000.0 };
        public List<double> prevY = new List<double> { -10000.0 };
        public int aliveFrames = 0;
    }

    public class EnclosingRectangle
    {
        public Point[] Contour { get; set; }

        public LineSegment2D LongEdge { get; set; }

        public LineSegment2D ShortEdge { get; set; }
    }

    internal class DisplaySample
    {
        internal const int Samples = 10;
        internal int SampleCounter = 0;
        internal EnclosingRectangle[] DisplaySamples = new EnclosingRectangle[Samples];

        internal bool NeedsSample()
        {
            return SampleCounter < Samples;
        }

        internal void Sample(EnclosingRectangle rectangle)
        {
            DisplaySamples[SampleCounter] = rectangle;
            SampleCounter++;
        }

        internal EnclosingRectangle GetBestSample()
        {
            var orderedEnclosingRectangles = DisplaySamples.OrderBy(r => r.LongEdge.Length).ToArray();

            return orderedEnclosingRectangles[2];
        }
    }
}
