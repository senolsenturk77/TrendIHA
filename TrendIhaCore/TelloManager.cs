using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TelloLib;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace TrendIhaCore
{



    public enum ConnectionState
    {
        Conectting = 0,
        Conected = 1,
        Disconnecting = 2,
        Disconected = 3
    }


    public class TelloInfo
    {
        public string Speed { get; set; } = "";

        public string Battery { get; set; } = "";


        public string FlyTime { get; set; } = "";

        public string WifiSNR { get; set; } = "";
    }


    public class TelloManager
    {


        public static int iFrameRate = 5;

        public static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();//used to cancel listeners


        public delegate void videoUpdateDeligate(byte[] data);
        public static event videoUpdateDeligate onVideoData;


        public delegate void videoBitmapDeligate(BitmapSource picture);
        public static event videoBitmapDeligate onVideoBitmap;


        public delegate void connectionStateDeligate(ConnectionState state);
        public static event connectionStateDeligate onConnection;

        public delegate void connectionTelloInfoDeligate(TelloInfo info);
        public static event connectionTelloInfoDeligate onTelloInfo;


        public delegate void stateInfoDeligate(string info);
        public static event stateInfoDeligate onStateInfo;


        private static UdpUser telloDevice;

        private static UdpListener videoListener;

        private static UdpListener stateListener;


        private static bool isConected = false;


        public static async Task Connect()
        {
            Received _retVal = new Received();
            bool _retTaskState = false;

            cancelTokenSource = new CancellationTokenSource();

            try
            {
                StartStateListener();
                StartOpenCV();
                // StartVideoStreamListener();


                isConected = false;

                telloDevice = UdpUser.ConnectTo("192.168.10.1", 8889);

                if (onConnection != null)
                    onConnection(ConnectionState.Conectting);




                // Set IHA to command mode
                telloDevice.Send("command");

                //_retTaskState = Task.Run(async () =>
                //{
                //    _retVal = await telloDevice.Receive();

                //}).Wait(5000);



                //if (!_retTaskState || _retVal.Message != "ok")
                //{
                //    await Disconnect();
                //}



                //if (_retTaskState && _retVal.Message == "ok")
                //{
                isConected = true;

                if (onConnection != null)
                    onConnection(ConnectionState.Conected);



                telloDevice.Send("streamoff");
                await Task.Delay(50);
                telloDevice.Send("streamon");
                await Task.Delay(50);



                //await Task.Delay(50);



                //    //Off video stream
                //    telloDevice.Send("streamoff");

                //    _retTaskState = Task.Run(async () =>
                //    {
                //        _retVal = await telloDevice.Receive();

                //    }).Wait(3000);

                //    await Task.Delay(50);


                //    //Start video stream
                //    if (_retTaskState && _retVal.Message == "ok")
                //    {

                //        telloDevice.Send("streamon");


                //        _retTaskState = Task.Run(async () =>
                //        {
                //            _retVal = await telloDevice.Receive();

                //        }).Wait(3000);


                //        await Task.Delay(50);
                //    }



                startHeartbeat();

                //                }




            }
            catch (Exception ex)
            {
                await Disconnect();
                throw ex;

            }








        }



        public static async Task Disconnect()
        {
            if (onConnection != null)
                onConnection(ConnectionState.Disconnecting);

            cancelTokenSource.Cancel();

            telloDevice?.Client?.Close();
            telloDevice?.Client?.Dispose();


            await Task.Delay(50);


            if (onConnection != null)
                onConnection(ConnectionState.Disconected);


            isConected = false;
        }

        public static void StartVideoStreamListener()
        {


            CancellationToken token = cancelTokenSource.Token;



            if (token.IsCancellationRequested)
                return;


            if (videoListener != null)
            {
                videoListener.Client?.Close();
                videoListener.Client?.Dispose();

                Task.Delay(100).Wait();
            }

            videoListener = new UdpListener(11111);



            Task.Run(async () =>
            {



                while (true)
                {

                    if (token.IsCancellationRequested)
                    {
                        videoListener.Client?.Close();
                        videoListener.Client?.Dispose();

                        await Task.Delay(50);

                        break;

                    }


                    var _videodata = await videoListener.Receive();

                    if (onVideoData != null)
                        onVideoData(_videodata.bytes);


                    //if (_videodata.bytes[2] == 0 && _videodata.bytes[3] == 0 && _videodata.bytes[4] == 0 && _videodata.bytes[5] == 1)//Wait for first NAL
                    //{
                    //    var nal = (_videodata.bytes[6] & 0x1f);
                    //    //if (nal != 0x01 && nal!=0x07 && nal != 0x08 && nal != 0x05)
                    //    //    Console.WriteLine("NAL type:" +nal);
                    //    started = true;
                    //}






                }

            }, token);


        }

        public static void StartStateListener()
        {


            CancellationToken token = cancelTokenSource.Token;


            if (token.IsCancellationRequested)
                return;


            if (stateListener != null)
            {
                stateListener.Client?.Close();
                stateListener.Client?.Dispose();

                Task.Delay(100).Wait();
            }

            stateListener = new UdpListener(8890);



            Task.Run(async () =>
            {


                while (true)
                {

                    if (token.IsCancellationRequested)
                    {
                        stateListener.Client?.Close();
                        stateListener.Client?.Dispose();

                        await Task.Delay(50);

                        break;

                    }


                    var _data = await stateListener.Receive();


                    var messageParts = _data.Message.Split(';');

                    var resultMessage = String.Join(Environment.NewLine, messageParts);


                    if (onStateInfo != null)
                        onStateInfo(resultMessage);

                    await Task.Delay(10);

                }

            }, token);


        }


        private static void startHeartbeat()
        {
            CancellationToken token = cancelTokenSource.Token;

            //heartbeat.
            Task.Run(async () =>
            {
                int tick = 0;

                while (true)
                {

                    try
                    {
                        if (token.IsCancellationRequested)
                            break;

                        if (isConected)//only send if not paused
                        {


                            //tick++;
                            //if ((tick % iFrameRate) == 0)
                            //{
                            //    tick = 0;
                            //    requestIframe();
                            //}

                            //sendControllerUpdate();

                            getTelleInformation();

                        }
                        await Task.Delay(50);//Often enough?
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Heatbeat error:" + ex.Message);
                        if (ex.Message.StartsWith("Access denied") //Denied means app paused
                            && isConected)
                        {
                            //Can this happen?
                            Console.WriteLine("Heatbeat: access denied and not paused:" + ex.Message);

                            //disconnect();
                            break;
                        }


                        if (!ex.Message.StartsWith("Access denied"))//Denied means app paused
                        {
                            //disconnect();
                            break;
                        }
                    }
                }
            }, token);

        }




        private static void getTelleInformation()
        {


            TelloInfo tInfo = new TelloInfo();




            telloDevice.Send("battery?");

            Task.Run(async () =>
           {
               var _retVal = await telloDevice.Receive();
               tInfo.Battery = _retVal.Message.Trim('\r', '\n'); ;

           }).Wait(3000);



            telloDevice.Send("speed?");

            Task.Run(async () =>
              {
                  var _retVal = await telloDevice.Receive();
                  tInfo.Speed = _retVal.Message.Trim('\r', '\n'); ;

              }).Wait(3000);



            telloDevice.Send("time?");

            Task.Run(async () =>
            {
                var _retVal = await telloDevice.Receive();
                tInfo.FlyTime = _retVal.Message.Trim('\r', '\n'); ;

            }).Wait(3000);

            telloDevice.Send("wifi?");

            Task.Run(async () =>
            {
                var _retVal = await telloDevice.Receive();
                tInfo.WifiSNR = _retVal.Message.Trim('\r', '\n');

            }).Wait(3000);



            if (onTelloInfo != null)
                onTelloInfo(tInfo);
        }



        public static void requestIframe()
        {
            var iframePacket = new byte[] { 0xcc, 0x58, 0x00, 0x7c, 0x60, 0x25, 0x00, 0x00, 0x00, 0x6c, 0x95 };
            telloDevice.Send(iframePacket);
        }


        private static VideoCapture _vc;
     //   private static Mat _frame;

        public static void StartOpenCV()
        {

            Task.Run(() =>
            {
                _vc = new VideoCapture("udp://192.168.10.1:11111");

          
                _vc.ImageGrabbed -= _vc_ImageGrabbed;

                _vc.ImageGrabbed += _vc_ImageGrabbed;
              //  _frame = new Mat();

                _vc.Start();

            });
        }

        private static void _vc_ImageGrabbed(object sender, EventArgs e)
        {
            if (onVideoBitmap!=null && _vc != null && _vc.Ptr != IntPtr.Zero)
            {

                var _frame = new Mat();


                if (_vc.Retrieve(_frame, 0))
                {

                    if (_frame != null)
                    {
                        var p = _frame.ToImage<Bgr, byte>();

                        var pdata = p.ToJpegData();

                        if (onVideoBitmap != null)
                        {
                            onVideoBitmap(pdata.ToWpJpeg());

                        }

                    }

                }

                
            }
        }
    }
}
