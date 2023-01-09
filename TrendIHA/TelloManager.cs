using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TelloLib;

namespace TrendIHA
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
                StartVideoStreamListener();


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



                //telloDevice.Send("battery?");

                //_retTaskState = Task.Run(async () =>
                //{
                //    _retVal = await telloDevice.Receive();

                //}).Wait(3000);



                //if (!_retTaskState || _retVal.Message == null)
                //{

                //    await Disconnect();
                //    return;

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



                //startHeartbeat();



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

                while (true)
                {

                    try
                    {
                        if (token.IsCancellationRequested)
                            break;

                        if (isConected)//only send if not paused
                        {


                            //sendControllerUpdate();

                            await getTelleInformation();

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




        private static async Task getTelleInformation()
        {


            TelloInfo tInfo = new TelloInfo();


            Received _retVal;

            telloDevice.Send("battery?");
            _retVal = await telloDevice.Receive();
            tInfo.Battery = _retVal.Message.Trim('\r', '\n'); ;



            // Task.Run(async () =>
            //{
            //    var _retVal = await telloDevice.Receive();
            //    tInfo.Battery = _retVal.Message.Trim('\r', '\n'); ;

            //}).Wait(3000);





            telloDevice.Send("speed?");
            _retVal = await telloDevice.Receive();
            tInfo.Speed = _retVal.Message.Trim('\r', '\n'); ;


            //Task.Run(async () =>
            //  {
            //      var _retVal = await telloDevice.Receive();
            //      tInfo.Speed = _retVal.Message.Trim('\r', '\n'); ;

            //  }).Wait(3000);



            telloDevice.Send("time?");
            _retVal = await telloDevice.Receive();
            tInfo.FlyTime = _retVal.Message.Trim('\r', '\n'); ;


            //Task.Run(async () =>
            //{
            //    var _retVal = await telloDevice.Receive();
            //    tInfo.FlyTime = _retVal.Message.Trim('\r', '\n'); ;

            //}).Wait(3000);

            telloDevice.Send("wifi?");
            _retVal = await telloDevice.Receive();
            tInfo.WifiSNR = _retVal.Message.Trim('\r', '\n');


            //Task.Run(async () =>
            //{
            //    var _retVal = await telloDevice.Receive();
            //    tInfo.WifiSNR = _retVal.Message.Trim('\r', '\n');

            //}).Wait(3000);



            if (onTelloInfo != null)
                onTelloInfo(tInfo);
        }




        public static void SendCommand(string cmd)
        {

            if (isConected && telloDevice != null && telloDevice.Client != null)
            {


                telloDevice.Send(cmd);

                //await Task.CompletedTask;

                //await Task.Delay(5);
                //  await telloDevice.Receive();

            }



        }



        public class ControllerState
        {
            public float rx, ry, lx, ly;
            public int speed;
            public double deadBand = 0.15;
            public void setAxis(float lx, float ly, float rx, float ry)
            {
                //var deadBand = 0.15f;
                //this.rx = Math.Abs(rx) < deadBand ? 0.0f : rx;
                //this.ry = Math.Abs(ry) < deadBand ? 0.0f : ry;
                //this.lx = Math.Abs(lx) < deadBand ? 0.0f : lx;
                //this.ly = Math.Abs(ly) < deadBand ? 0.0f : ly;

                this.rx = rx;
                this.ry = ry;
                this.lx = lx;
                this.ly = ly;

                //Console.WriteLine(rx + " " + ry + " " + lx + " " + ly + " SP:" + speed);
            }
            public void setSpeedMode(int mode)
            {
                speed = mode;

                //Console.WriteLine(rx + " " + ry + " " + lx + " " + ly + " SP:" + speed);
            }
        }
        public static ControllerState controllerState = new ControllerState();


        public static void sendControllerUpdate()
        {
            if (!isConected)
                return;

            var boost = 0.0f;
            if (controllerState.speed > 0)
                boost = 1.0f;

            //var limit = 1.0f;//Slow down while testing.
            //rx = rx * limit;
            //ry = ry * limit;
            var rx = controllerState.rx;
            var ry = controllerState.ry;
            var lx = controllerState.lx;
            var ly = controllerState.ly;


            rx = Clamp(rx, -1.0f, 1.0f);
            ry = Clamp(ry, -1.0f, 1.0f);
            lx = Clamp(lx, -1.0f, 1.0f);
            ly = Clamp(ly, -1.0f, 1.0f);

            //Console.WriteLine(controllerState.rx + " " + controllerState.ry + " " + controllerState.lx + " " + controllerState.ly + " SP:"+boost);

            var packet = createJoyPacket(rx, ry, lx, ly, boost);
            try
            {
                telloDevice.Send(packet);
            }
            catch (Exception ex)
            {

            }
        }


        //Create joystick packet from floating point axis.
        //Center = 0.0. 
        //Up/Right =1.0. 
        //Down/Left=-1.0. 
        private static byte[] createJoyPacket(float fRx, float fRy, float fLx, float fLy, float speed)
        {
            //template joy packet.
            var packet = new byte[] { 0xcc, 0xb0, 0x00, 0x7f, 0x60, 0x50, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x12, 0x16, 0x01, 0x0e, 0x00, 0x25, 0x54 };

            short axis1 = (short)(660.0F * fRx + 1024.0F);//RightX center=1024 left =364 right =-364
            short axis2 = (short)(660.0F * fRy + 1024.0F);//RightY down =364 up =-364
            short axis3 = (short)(660.0F * fLy + 1024.0F);//LeftY down =364 up =-364
            short axis4 = (short)(660.0F * fLx + 1024.0F);//LeftX left =364 right =-364
             

            short axis5 = (short)((short)speed & 0x01);

            //Speed.
            //short axis5 = (short)(660.0F * speed + 1024.0F);
            //if (speed > 0.1f)
            //    axis5 = 0x7fff;


            long packedAxis = ((long)axis1 & 0x7FF) | (((long)axis2 & 0x7FF) << 11) | (((long)axis3 & 0x7FF) << 22) | (((long)axis4 & 0x7FF) << 33) | ((long)axis5 << 44);

            packet[9] = ((byte)(int)(0xFF & packedAxis));
            packet[10] = ((byte)(int)(packedAxis >> 8 & 0xFF));
            packet[11] = ((byte)(int)(packedAxis >> 16 & 0xFF));
            packet[12] = ((byte)(int)(packedAxis >> 24 & 0xFF));
            packet[13] = ((byte)(int)(packedAxis >> 32 & 0xFF));
            packet[14] = ((byte)(int)(packedAxis >> 40 & 0xFF));

            //Add time info.		
            var now = DateTime.Now;
            packet[15] = (byte)now.Hour;
            packet[16] = (byte)now.Minute;
            packet[17] = (byte)now.Second;
            packet[18] = (byte)(now.Millisecond & 0xff);
            packet[19] = (byte)(now.Millisecond >> 8);

            CRC.calcUCRC(packet, 4);//Not really needed.

            //calc crc for packet. 
            CRC.calcCrc(packet, packet.Length);

            return packet;
        }


        public static float Clamp(float value, float min, float max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }
    }
}
