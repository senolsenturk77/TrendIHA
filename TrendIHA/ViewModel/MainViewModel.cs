using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Threading;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TelloLib;
using TrendIHA.Class;

namespace TrendIHA.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {

        #region JoyisticDataString Property

        /// <summary>
        /// Private member backing variable for <see cref="JoyisticDataString" />
        /// </summary>
        private string _JoyisticDataString = "Yok";

        /// <summary>
        /// Gets and sets The property's value
        /// </summary>
        public string JoyisticDataString
        {
            get
            {
                if (_JoyisticDataString == null)
                { _JoyisticDataString = string.Empty; }

                return _JoyisticDataString;
            }
            set { Set(() => JoyisticDataString, ref _JoyisticDataString, value); }
        }

        #endregion


        #region JoyisticDataString2 Property

        /// <summary>
        /// Private member backing variable for <see cref="JoyisticDataString2" />
        /// </summary>
        private string _JoyisticDataString2 = null;

        /// <summary>
        /// Gets and sets The property's value
        /// </summary>
        public string JoyisticDataString2
        {
            get
            {
                if (_JoyisticDataString2 == null)
                { _JoyisticDataString2 = string.Empty; }

                return _JoyisticDataString2;
            }
            set { Set(() => JoyisticDataString2, ref _JoyisticDataString2, value); }
        }

        #endregion


        #region VideoBitmap Property

        /// <summary>
        /// Private member backing variable for <see cref="VideoBitmap" />
        /// </summary>
        private WriteableBitmap _VideoBitmap = new WriteableBitmap(960, 720, 96.0, 96.0, PixelFormats.Bgr24, null);

        /// <summary>
        /// Gets and sets The property's value
        /// </summary>
        public WriteableBitmap VideoBitmap
        {
            get
            {

                return _VideoBitmap;
            }
            set
            {

                _VideoBitmap = value;
                Set(() => VideoBitmap, ref _VideoBitmap, value); 

            }
        }

        #endregion


        #region ConnectionInfo Property

        /// <summary>
        /// Private member backing variable for <see cref="ConnectionInfo" />
        /// </summary>
        private String _ConnectionInfo = ConnectionState.Disconected.ToString();

        /// <summary>
        /// Gets and sets The property's value
        /// </summary>
        public String ConnectionInfo
        {
            get
            {
                if (_ConnectionInfo == null)
                { _ConnectionInfo = String.Empty; }

                return _ConnectionInfo;
            }
            set
            {


                Set(() => ConnectionInfo, ref _ConnectionInfo, value);


                // (ReConnectCommand as RelayCommand)?.RaiseCanExecuteChanged();


            }
        }

        #endregion


        #region TelloInfoCurrent Property

        /// <summary>
        /// Private member backing variable for <see cref="TelloInfoCurrent" />
        /// </summary>
        private TelloInfo _TelloInfoCurrent = null;

        /// <summary>
        /// Gets and sets The property's value
        /// </summary>
        public TelloInfo TelloInfoCurrent
        {
            get
            {


                return _TelloInfoCurrent;
            }
            set { Set(() => TelloInfoCurrent, ref _TelloInfoCurrent, value); }
        }

        #endregion


        #region StateMessage Property

        /// <summary>
        /// Private member backing variable for <see cref="StateMessage" />
        /// </summary>
        private String _StateMessage = null;

        /// <summary>
        /// Gets and sets The property's value
        /// </summary>
        public String StateMessage
        {
            get
            {
                if (_StateMessage == null)
                { _StateMessage = String.Empty; }

                return _StateMessage;
            }
            set { Set(() => StateMessage, ref _StateMessage, value); }
        }

        #endregion



        #region FrameInfo Property

        /// <summary>
        /// Private member backing variable for <see cref="FrameInfo" />
        /// </summary>
        private String _FrameInfo = "0 / 0";

        /// <summary>
        /// Gets and sets The property's value
        /// </summary>
        public String FrameInfo
        {
            get
            {
                if (_FrameInfo == null)
                { _FrameInfo = String.Empty; }

                return _FrameInfo;
            }
            set { Set(() => FrameInfo, ref _FrameInfo, value); }
        }

        #endregion





        public RelayCommand ConnectCommand { get; private set; }


        public RelayCommand DisconnectCommand { get; private set; }





        ConnectionState LastState = ConnectionState.Disconected;


        UdpListener VideoServer = null;


        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}
            ///



            //   VideoServer = new UdpListener(11111);

            ConnectCommand = new RelayCommand(async () => { await Connect(); }, () =>
            {

                return LastState == ConnectionState.Disconected;

            });

            DisconnectCommand = new RelayCommand(async () => { await Disconnect(); }, () =>
            {

                return LastState == ConnectionState.Conected;

            });

            InitJoystic();

            InitTelloManager();

            ConnectCommand.RaiseCanExecuteChanged();
            DisconnectCommand.RaiseCanExecuteChanged();
        }




        MemoryStream inputStream;

        ConcurrentQueue<byte[]> frameQueue = new ConcurrentQueue<byte[]>();

        ConcurrentQueue<Bitmap> bitmapQueue = new ConcurrentQueue<Bitmap>();


        bool waitForFirstStartPacket = true;
        bool videoFrameIsRaedy = false;


        long newFrame = 0;

        long newFrameDecoded = 0;


        int fps = 0;

        int fpsShow = 0;


        int frameRateDefault = 0;
        int frameRate = 0;

        long sendedFrame = 0;


        private static int lastTick;


        public void InitTelloManager()
        {
            TelloManager.cancelTokenSource = new CancellationTokenSource();

            waitForFirstStartPacket = true;
            videoFrameIsRaedy = false;

            inputStream = new MemoryStream();



            TelloManager.onVideoData += async (byte[] data) =>
            {
                try
                {



                    if (data.Length < 1460 && waitForFirstStartPacket)
                    {
                        waitForFirstStartPacket = false;
                        return;
                    }


                    if (videoFrameIsRaedy)
                    {
                        inputStream = new MemoryStream();
                        videoFrameIsRaedy = false;

                    }


                    inputStream.Write(data, 0, data.Length);


                    if (data.Length < 1460)
                    {
                        inputStream.Flush();
                        videoFrameIsRaedy = true;

                        //frameRate--;

                        //if (frameRate <= 0)
                        //{
                        frameRate = frameRateDefault;
                        frameQueue.Enqueue(inputStream.ToArray());

                        newFrame++;


                        //}

                        fps++;

                        if (System.Environment.TickCount - lastTick >= 1000)
                        {
                            fpsShow = fps;
                            fps = 0;
                            lastTick = System.Environment.TickCount;
                        }



                        FrameInfo = $"{newFrame.ToString("N0")} / {newFrameDecoded.ToString("N0")} Fps: {fpsShow} Senden: {sendedFrame}";




                    }



                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error on cam data : {ex.Message}");
                }
            };

            TelloManager.onConnection += (ConnectionState newState) =>
            {


                //if (newState == Tello.ConnectionState.Connected)
                //{
                //    Tello.queryAttAngle();
                //    Tello.setMaxHeight(50);

                //}


                //printAt(0, 0, "Tello " + newState.ToString());

                LastState = newState;
                ConnectionInfo = newState.ToString();




            };

            TelloManager.onTelloInfo += (TelloInfo newInfo) =>
            {

                TelloInfoCurrent = newInfo;
            };


            TelloManager.onStateInfo += (string newInfo) =>
            {

                StateMessage = newInfo;
            };




        }

        public async Task Connect()
        {



            await Task.Run(async () =>
            {
                await TelloManager.Connect();
            });


            ProcessVideo();
        }

        public async Task Disconnect()
        {
            await Task.Run(async () =>
            {
                await TelloManager.Disconnect();
            });

        }

        public void InitJoystic()
        {




            PCJoystick.onUpdate += (SharpDX.DirectInput.JoystickState joyState) =>
            {



                var roll = NormalizationJoystickData(((float)joyState.X / 0x8000) - 1);
                var pitch = NormalizationJoystickData(((float)joyState.Y / 0x8000) - 1) * -1;

                var yaw = NormalizationJoystickData(((float)joyState.RotationZ / 0x8000) - 1);



                int tortle = 0;

                if (joyState.PointOfViewControllers[0] == 0)
                {
                    tortle = 50;
                }
                else if (joyState.PointOfViewControllers[0] == 18000)
                {
                    tortle = -50;
                }



                float[] axes = new float[] { pitch, roll, yaw, tortle };

                var outStr = string.Format("JOY pitch:{0: 0.00;-0.00} roll:{1: 0.00;-0.00} yaw:{2: 0.00;-0.00} tortle:{3: 0.00;-0.00}", axes[0], axes[1], axes[2], axes[3]);


                JoyisticDataString = joyState.ToString();
                JoyisticDataString2 = outStr;


                //takeoff
                if (joyState.Buttons[7])
                {

                    TelloManager.SendCommand("speed 20");
                    Task.Delay(10).Wait();

                    TelloManager.SendCommand("takeoff");
                }


                //land
                if (joyState.Buttons[6])
                {
                    TelloManager.SendCommand("speed 10");
                    Task.Delay(10).Wait();
                    TelloManager.SendCommand("land");
                }


                //if (joyState.Buttons[8])
                //{
                //    TelloManager.SendCommand("forward 30");
                //}

                //if (joyState.Buttons[9])
                //{
                //     TelloManager.SendCommand("back 30");
                //}


                //if (lx != 0)
                //{
                //    if (lx > 0)
                //    {
                //         TelloManager.SendCommand("right 30");

                //    }
                //    else if (lx < 0)
                //    {
                //         TelloManager.SendCommand("left 30");
                //    }

                //}


                //if (ly != 0)
                //{
                //    if (ly > 0)
                //    {
                //         TelloManager.SendCommand("back 30");
                //    }
                //    else if (ly < 0)
                //    {
                //         TelloManager.SendCommand("forward 30");
                //    }
                //}



                //if (lz != 0)
                //{

                //    if (lz > 0)
                //    {
                //        var _command = $"cw 10";
                //         TelloManager.SendCommand(_command);

                //    }
                //    else
                //    {
                //        var _command = $"ccw 10";
                //         TelloManager.SendCommand(_command);

                //    }
                //}


                // printAt(0, 22, outStr);
                // Tello.controllerState.setAxis(lx, ly, rx, ry);
                // Tello.sendControllerUpdate();






                string _stickdata = $"rc {roll} {pitch} {tortle} {yaw}";
                TelloManager.SendCommand(_stickdata);



            };





            PCJoystick.init();





        }

        private static int NormalizationJoystickData(float data, float limit = 0.3f)
        {

            if (data > limit || data < -limit)
            {

                if (data > 1.0)
                    data = 1.0f;

                if (data < -1.0)
                    data = -1.0f;

                return (int)(data * 100);

            }
            else
            {
                return 0;
            }

        }


        public void ProcessVideo()
        {


            var token = TelloManager.cancelTokenSource.Token;

            frameQueue = new ConcurrentQueue<byte[]>();


            Task.Run(() =>
            {
                ProcessFrame(token);
            }, token).ConfigureAwait(false);



        }

        private void ProcessFrame(CancellationToken token)
        {


            var myDecoder = new OpenH264Lib.Decoder("openh264-1.7.0-win32.dll");

            while (true)
            {

                if (token.IsCancellationRequested)
                {
                    myDecoder = null;
                    break;
                }



                if (frameQueue.TryDequeue(out var _frame))
                {


                    if (myDecoder != null)
                    {
                        var image = myDecoder.Decode(_frame, _frame.Length);


                        newFrameDecoded++;

                        if (image != null)
                        {

                            Task.Run(() =>
                            {

                                sendedFrame++;

                                App.Current?.Dispatcher.BeginInvoke(new Action(() =>
                                {


                                    UpdateBitmapDestination(image);
                                   // RaisePropertyChanged("VideoBitmap");

                                    FrameInfo = $"{newFrame.ToString("N0")} / {newFrameDecoded.ToString("N0")} Fps: {fpsShow} Senden: {sendedFrame}";

                                }));

                            });


                        }
                    }
                }


            }






        }

        public static BitmapSource Convert(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);

            return bitmapSource;
        }

        public void UpdateBitmapDestination(Bitmap bmp)
        {

            BitmapData data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            VideoBitmap.Lock();
            CopyMemory(VideoBitmap.BackBuffer, data.Scan0, (uint)(VideoBitmap.BackBufferStride * bmp.Height));
            VideoBitmap.AddDirtyRect(new Int32Rect(0, 0, bmp.Width, bmp.Height));
            VideoBitmap.Unlock();
            bmp.UnlockBits(data);
        }


        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

    }







}