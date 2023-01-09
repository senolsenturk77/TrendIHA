using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TelloLib;



namespace TrendIhaCore.ViewModel
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
        private BitmapSource _VideoBitmap = null;

        /// <summary>
        /// Gets and sets The property's value
        /// </summary>
        public BitmapSource VideoBitmap
        {
            get
            {

                return _VideoBitmap;
            }
            set { Set(() => VideoBitmap, ref _VideoBitmap, value); }
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


            InitTelloManager();

            InitJoystic();


            ConnectCommand.RaiseCanExecuteChanged();
            DisconnectCommand.RaiseCanExecuteChanged();
        }






        bool waitForFirstStartPacket = true;
        bool videoFrameIsRaedy = false;


        MemoryStream inputStream;

        public void InitTelloManager()
        {
            TelloManager.cancelTokenSource = new CancellationTokenSource();

            waitForFirstStartPacket = true;
            videoFrameIsRaedy = false;

            inputStream = new MemoryStream();


            TelloManager.onVideoData += (byte[] data) =>
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
                    }



                    if (videoFrameIsRaedy)
                    {
                        var _frame = inputStream.ToArray();



                        //Bitmap image = null;  //decoder.Decode(_frame, _frame.Length);

                        //    if (image != null)
                        //    {
                        //        App.Current.Dispatcher.Invoke(() =>
                        //       {
                        //           VideoBitmap = image.ToWpfBitmap();
                        //       });

                        //    }

                    }







                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error on cam data : {ex.Message}");
                }
            };

            TelloManager.onVideoBitmap += (BitmapSource picture) =>
            {
                try
                {

                    App.Current.Dispatcher.Invoke(() =>
                               {
                                   VideoBitmap = picture;
                               });

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



                var rx = ((float)joyState.RotationX / 0x8000) - 1;
                var ry = (((float)joyState.RotationY / 0x8000) - 1);
                var lx = ((float)joyState.X / 0x8000) - 1;
                var ly = (((float)joyState.Y / 0x8000) - 1);
                //var boost = joyState.Z
                float[] axes = new float[] { lx, ly, rx, ry, 0 };
                var outStr = string.Format("JOY {0: 0.00;-0.00} {1: 0.00;-0.00} {2: 0.00;-0.00} {3: 0.00;-0.00} {4: 0.00;-0.00}", axes[0], axes[1], axes[2], axes[3], axes[4]);

                JoyisticDataString = joyState.ToString();
                JoyisticDataString2 = outStr;

                // printAt(0, 22, outStr);
                // Tello.controllerState.setAxis(lx, ly, rx, ry);
                // Tello.sendControllerUpdate();
            };


            PCJoystick.init();











        }










    }





}