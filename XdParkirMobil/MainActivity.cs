using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Android.Content;
using System;
using Android.Graphics;
using Android.Views;
using Android.Support.V4.Content;
using Android.Media;

using Android.Bluetooth;
using System.Linq;
using Java.IO;
using System.Threading;
using System.Threading.Tasks;

namespace XdParkirMobil
{
    [Activity(Label = "@string/app_name", Theme = "@style/Theme.AppCompat.Light.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private readonly int dangerDistance = 60;  // in milimeter
        private readonly int saveDistance = 150;   // in milimeter

        MediaPlayer saveMediaplayer;
        MediaPlayer dangerMediaplayer;

        #region private field
        Button btnConnect;
        Button btnStart;
        Button btnStop;
        TextView tvDistance1;
        // TextView tvDistance2;
        TextView tvStatus;
        TextView tvTitle;

        Typeface distanceTypeface;
        Typeface typiconsTypeface;
        Typeface baseTypeface;
        Typeface baseboldTypeface;

        Mutex _mutex = new Mutex();
        int _counter = 1;
        bool _isStop = true;

        Color colorDanger;
        Color colorSave;
        Color colorFar;

        private MyBluetoothConnection btConn;

        const int RequestPermissionId = 0;
        private readonly string[] PermissionList = {
            //Android.Manifest.Permission.WriteExternalStorage,
            //Android.Manifest.Permission.ReadExternalStorage,
            Android.Manifest.Permission.AccessCoarseLocation,
            Android.Manifest.Permission.AccessFineLocation,
            Android.Manifest.Permission.AccessNetworkState,
            Android.Manifest.Permission.Bluetooth,
            Android.Manifest.Permission.BluetoothAdmin,
        };
        #endregion

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Window.RequestFeature(WindowFeatures.NoTitle);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            // RequestPermissions(PermissionList, RequestPermissionId);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            InitControls();

        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void InitControls()
        {
            distanceTypeface = Typeface.CreateFromAsset(Assets, "fonts/Quantum.otf");
            typiconsTypeface = Typeface.CreateFromAsset(Assets, "fonts/typicons.ttf");
            baseTypeface = Typeface.CreateFromAsset(Assets, "fonts/ProximaNova.otf");
            baseboldTypeface = Typeface.CreateFromAsset(Assets, "fonts/ProximaNovaBlack.otf");

            colorDanger = new Color(ContextCompat.GetColor(this, Resource.Color.myRed));
            colorSave = new Color(ContextCompat.GetColor(this, Resource.Color.myBlue));
            colorFar = new Color(ContextCompat.GetColor(this, Resource.Color.myBlack));

            btnConnect = FindViewById<Button>(Resource.Id.btnConnect);
            btnStart = FindViewById<Button>(Resource.Id.btnStart);
            btnStop = FindViewById<Button>(Resource.Id.btnStop);
            tvDistance1 = FindViewById<TextView>(Resource.Id.tvDistance1);
            tvStatus = FindViewById<TextView>(Resource.Id.tvStatus);
            tvTitle = FindViewById<TextView>(Resource.Id.tvTitle);

            tvTitle.Typeface = baseboldTypeface;
            tvStatus.Typeface = baseTypeface;
            tvDistance1.Typeface = distanceTypeface;

            btnConnect.Text = TypiconsCode.Plug;
            btnConnect.Typeface = typiconsTypeface;
            btnConnect.Click += BtnConnect_Click;

            btnStart.Text = TypiconsCode.MediaPlay;
            btnStart.Typeface = typiconsTypeface;
            btnStart.Click += BtnStart_Click;

            btnStop.Text = TypiconsCode.MediaStop;
            btnStop.Typeface = typiconsTypeface;
            btnStop.Click += BtnStop_Click;

            saveMediaplayer = MediaPlayer.Create(this, Resource.Raw.notif_blue);
            dangerMediaplayer = MediaPlayer.Create(this, Resource.Raw.notif_red);
        }

        #region Button Click
        private void BtnConnect_Click(object sender, EventArgs e)
        {
            tvStatus.Text = "";
            btConn = new MyBluetoothConnection(BluetoothAdapter.DefaultAdapter,
                "JDY-31-SPP", "00001101-0000-1000-8000-00805f9b34fb");
            if (!string.IsNullOrEmpty(btConn.ErrorMessage))
            {
                tvStatus.Text = btConn.ErrorMessage;
            }
            else
            {
                tvStatus.Text = "Bisa Connect";
                Task.Delay(250);
                string s = btConn.SendMessageA(_mutex);
                if (s.Contains("OK"))
                {
                    tvDistance1.Text = "OK";
                    tvDistance1.SetTextColor(colorFar);
                    btnConnect.Enabled = false;
                    btnStart.Enabled = true;
                    btnStop.Enabled = false;
                }
                else
                {
                    tvStatus.Text = btConn.ErrorMessage;
                    tvDistance1.SetTextColor(colorDanger);
                    tvDistance1.Text = "?";
                }
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(btConn.ErrorMessage))
            {
                tvStatus.Text = btConn.ErrorMessage;
                return;
            }

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            _isStop = false;
            _counter = 1;
            tvStatus.Text = "START Distance Evaluation";

            _ = Task.Run(() =>
            {
                while (_isStop == false)
                {
                    string distance1 = btConn.SendMessageX(_mutex);
                    EvaluateDistance(distance1);
                    Thread.Sleep(650);
                    ++_counter;
                }
            });
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            StopEvaluation();
        }
        #endregion

        #region Distance Evaluation
        private void StopEvaluation()
        {
            _isStop = true;            
            if (saveMediaplayer.IsPlaying) saveMediaplayer.Stop();
            if (dangerMediaplayer.IsPlaying) dangerMediaplayer.Stop();
            tvStatus.Text = "STOP Distance Evaluation";
            tvDistance1.Text = "-";
            tvDistance1.SetTextColor(colorFar);
            btnConnect.Enabled = true;
            btnStart.Enabled = false;
            btnStop.Enabled = false;

            btConn.CloseSocket();
        }

        private void EvaluateDistance(string distanceStr)
        {
            if (distanceStr.StartsWith("E>"))
            {
                StopEvaluation();
                tvDistance1.Text = "?";
                tvDistance1.SetTextColor(colorDanger);
                tvStatus.Text = btConn.ErrorMessage;
            }
            else if (distanceStr.Contains("<"))
            {
                int ix = distanceStr.IndexOf('>');
                distanceStr = distanceStr.Remove(0, ix + 1);
                ix = distanceStr.IndexOf('<');
                distanceStr = distanceStr.Remove(ix, distanceStr.Length - 1 - ix);

                int d0;  // in milimeter                
                if (int.TryParse(distanceStr, out d0))
                {
                    if (d0 > 300)
                    {
                        tvDistance1.Text = "30+";
                        FarPosition();
                    }
                    else
                    {
                        int d1 = d0 / 10;
                        int d2 = d0 - (d1 * 10);
                        tvDistance1.Text = $"{d1}.{d2}";
                        if (d0 < dangerDistance)
                        {
                            DangerPosition();
                        }
                        else if (d0 < saveDistance)
                        {
                            SavePosition();
                        }
                        else
                        {
                            FarPosition();
                        }
                    }
                }
            }

        }



        private void DangerPosition()
        {
            tvDistance1.SetTextColor(colorDanger);            
            if(!dangerMediaplayer.IsPlaying) dangerMediaplayer.Start();
        }

        private void SavePosition()
        {
            tvDistance1.SetTextColor(colorSave);
            if (!saveMediaplayer.IsPlaying) saveMediaplayer.Start();
        }

        private void FarPosition()
        {
            tvDistance1.SetTextColor(colorFar);
            if (saveMediaplayer.IsPlaying) saveMediaplayer.Stop();
            if (dangerMediaplayer.IsPlaying) dangerMediaplayer.Stop();
        }


        #endregion

    }
}