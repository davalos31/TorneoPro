using OpenCvSharp;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using WpfWindow = System.Windows.Window;

namespace WpfApp1
{
    public partial class MainWindow : WpfWindow
    {
        private VideoCapture? _capture;
        private Thread? _captureThread;
        private volatile bool _running;
        private readonly DispatcherTimer _clockTimer;
        private WriteableBitmap? _writeableBitmap;

        private const string PuestoName = "Puesto ABN";
        private const string AddressLine1 = "Avenida Capitán Víctor Ustáriz Quillacollo";
        private const string AddressLine2 = "Departamento de Cochabamba";

        public MainWindow()
        {
            InitializeComponent();

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, _) => UpdateClock();
            _clockTimer.Start();
            UpdateClock();

            TxtPuesto.Text = PuestoName;
            TxtAddress1.Text = AddressLine1;
            TxtAddress2.Text = AddressLine2;
        }

        private void UpdateClock()
        {
            TxtDateTime.Text = DateTime.Now.ToString("d MMM yyyy H:mm:ss",
                new System.Globalization.CultureInfo("es-BO"));
        }

        // ── INICIAR ─────────────────────────────────────────────
        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _capture = new VideoCapture(0);
                if (!_capture.IsOpened())
                {
                    MessageBox.Show(
                        "No se encontró ninguna cámara.\nVerifique que esté conectada y no esté en uso.",
                        "Sin cámara", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _capture.Dispose();
                    _capture = null;
                    return;
                }

                _running = true;
                NoCameraPanel.Visibility = Visibility.Collapsed;
                LiveDot.Fill = new SolidColorBrush(Colors.LimeGreen);
                BtnStart.IsEnabled = false;
                BtnCapture.IsEnabled = true;
                BtnStop.IsEnabled = true;

                _captureThread = new Thread(CaptureLoop) { IsBackground = true };
                _captureThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al iniciar cámara:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── DETENER ─────────────────────────────────────────────
        private void BtnStop_Click(object sender, RoutedEventArgs e) => StopCamera();

        // ── CAPTURAR ────────────────────────────────────────────
        private void BtnCapture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rtb = new RenderTargetBitmap(
                    (int)ActualWidth, (int)ActualHeight, 96, 96,
                    PixelFormats.Pbgra32);
                rtb.Render(this);

                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg",
                    FileName = $"ABN_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dlg.ShowDialog() == true)
                {
                    BitmapEncoder encoder = dlg.FilterIndex == 2
                        ? new JpegBitmapEncoder()
                        : new PngBitmapEncoder();

                    encoder.Frames.Add(BitmapFrame.Create(rtb));

                    using var fs = System.IO.File.OpenWrite(dlg.FileName);
                    encoder.Save(fs);

                    MessageBox.Show($"Imagen guardada:\n{dlg.FileName}",
                        "Guardado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── LOOP DE CAPTURA ─────────────────────────────────────
        private void CaptureLoop()
        {
            using var mat = new Mat();
            using var matBgr = new Mat();

            while (_running && _capture != null)
            {
                _capture.Read(mat);
                if (mat.Empty()) { Thread.Sleep(30); continue; }

                if (mat.Channels() == 4)
                    Cv2.CvtColor(mat, matBgr, ColorConversionCodes.BGRA2BGR);
                else if (mat.Channels() == 1)
                    Cv2.CvtColor(mat, matBgr, ColorConversionCodes.GRAY2BGR);
                else
                    mat.CopyTo(matBgr);

                int w = matBgr.Width;
                int h = matBgr.Height;
                int stride = (int)matBgr.Step();

                Dispatcher.Invoke(() =>
                {
                    if (_writeableBitmap == null ||
                        _writeableBitmap.PixelWidth != w ||
                        _writeableBitmap.PixelHeight != h)
                    {
                        _writeableBitmap = new WriteableBitmap(
                            w, h, 96, 96, PixelFormats.Bgr24, null);
                        CameraImage.Source = _writeableBitmap;
                    }

                    _writeableBitmap.WritePixels(
                        new Int32Rect(0, 0, w, h),
                        matBgr.Data,
                        stride * h,
                        stride);
                });

                Thread.Sleep(33);
            }
        }

        // ── DETENER CÁMARA ──────────────────────────────────────
        private void StopCamera()
        {
            _running = false;
            _captureThread?.Join(500);
            _capture?.Dispose();
            _capture = null;
            _writeableBitmap = null;

            Dispatcher.Invoke(() =>
            {
                CameraImage.Source = null;
                NoCameraPanel.Visibility = Visibility.Visible;
                LiveDot.Fill = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
                BtnStart.IsEnabled = true;
                BtnCapture.IsEnabled = false;
                BtnStop.IsEnabled = false;
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            _running = false;
            _capture?.Dispose();
            _clockTimer.Stop();
            base.OnClosed(e);
        }
    }
}