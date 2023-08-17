using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Concurrent;
using System.Net;
using System.Drawing;
using Newtonsoft.Json;
using Camera_NET;
using DirectShowLib;

namespace GetWebCam2
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        private CameraChoice cameraChoice = new CameraChoice();
        ConcurrentQueue<FrameData> frames = new ConcurrentQueue<FrameData>();
        public MainWindow()
        {
            InitializeComponent();
        }



        bool IsCaptureContiously = true;

        async Task<string> sendAsync(string url, System.Drawing.Image img)
        {
            return await Task<string>.Run(() =>
            {
                return sendToJudge(url, img);
            });

        }

        string sendToJudge(string url, System.Drawing.Image img)
        {

            string result = "";
            Dictionary<string, string> data_send = new Dictionary<string, string>();

            var webAddr = url;
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
            httpWebRequest.ContentType = "application/json; charset=utf-8";
            httpWebRequest.Accept = "application/json";
            httpWebRequest.Method = "POST";


            byte[] arr;
            using (System.IO.Stream ms = new System.IO.MemoryStream())
            {
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                arr = (ms as System.IO.MemoryStream).ToArray();
            }
            var b64str = "data:image/jpg;base64," + Convert.ToBase64String(arr);
            data_send["img_path"] = b64str;
            //data_send["detector_backend"] = backEnd;
            //data_send["enforce_detection"] = "False";
            using (var streamWriter = new System.IO.StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = JsonConvert.SerializeObject(data_send);
                streamWriter.Write(json);
                //streamWriter.Flush();
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new System.IO.StreamReader(httpResponse.GetResponseStream()))
                result = streamReader.ReadToEnd();
            return result;


        }
        Bitmap curBitmap;
        void plotBoundingBox(string jsonStr, System.Drawing.Bitmap bitmap, int fontsize = 12)
        {
            curBitmap = (Bitmap)bitmap.Clone();
            var jsonObj = JsonConvert.DeserializeObject<Dictionary<string, List<DetectedLogo>>>(jsonStr);
            var results = jsonObj["results"];
            int count = 0;
            foreach (var result in results)
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    var scale = result.scale;
                    System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Brushes.Navy, 6);
                    Font drawFont = new Font("Arial", fontsize, System.Drawing.FontStyle.Bold);
                    SolidBrush drawBrush = new SolidBrush(pen.Color);
                    var box = result.box.Select(a => ((int)(scale * (double)a))).ToArray();
                    g.DrawRectangle(pen, new System.Drawing.Rectangle(box[0], box[1], box[2], box[3]));
                    g.DrawString($"no.{++count}\r\n{result.class_name}", drawFont, drawBrush, box[0], box[1]);
                }
            }

            Image_Out.Source = BitmapConverter.Bitmap2BitmapImage(bitmap);

        }

        async void captureImageContinuously()
        {
            string url = txtbx_url.Text;
            while (IsCaptureContiously)
            {
                {
                    Bitmap bitmp = cameraControl.CameraControl.SnapshotOutputImage();
                    {

                        var rslt = await sendAsync(txtbx_url.Text, bitmp);
                        txtblck_msg.Text = rslt;
                        plotBoundingBox(rslt, bitmp);
                    }
                    bitmp.Dispose();
                    System.Threading.SpinWait.SpinUntil(() => false, 1);
                }
            }
            Image_Out.Source = null;

        }
        private void btn_init_Click(object sender, RoutedEventArgs e)
        {
            cameraControl.CameraControl.Visible = true;
            CameraChoice cameraChoice = new CameraChoice();

            // Get List of devices (cameras)
            cameraChoice.UpdateDeviceList();

            // To get an example of camera and resolution change look at other code samples 
            if (cameraChoice.Devices.Count > 0)
            {
                // Device moniker. It's like device id or handle.
                // Run first camera if we have one
                var camera_moniker = cameraChoice.Devices[cmbx_devices.SelectedIndex].Mon;

                // Set selected camera to camera control with default resolution
                cameraControl.CameraControl.SetCamera(camera_moniker, null);
                
            }
        }

        private void btn_preview_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void btn_capture_Click(object sender, RoutedEventArgs e)
        {
            Bitmap bitmp = cameraControl.CameraControl.SnapshotOutputImage();
            var rslt=await sendAsync(txtbx_url.Text, bitmp);
            //sendToJudge(txtbx_url.Text, img);
            plotBoundingBox(rslt, bitmp);
            txtblck_msg.Text = rslt;
            bitmp.Dispose();
        }

        private void btn_inference_Click(object sender, RoutedEventArgs e)
        {
            IsCaptureContiously = true;
            captureImageContinuously();
        }

     

        private void btn_closeCamera_Click(object sender, RoutedEventArgs e)
        {
            IsCaptureContiously = false;
            cameraControl.CameraControl.CloseCamera();
        }
        private void FillCameraList()
        {
            cmbx_devices.Items.Clear();

            cameraChoice.UpdateDeviceList();

            foreach (var camera_device in cameraChoice.Devices)
            {
                cmbx_devices.Items.Add(camera_device.Name);
            }
        }

 

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FillCameraList();

            // Select the first one
            if (cmbx_devices.Items.Count > 0)
            {
                cmbx_devices.SelectedIndex = 0;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            IsCaptureContiously = false;
            cameraControl.CameraControl.CloseCamera();
        }
    }

    public class FrameData
    {
        public Bitmap image;
        public string json;
    }
}
