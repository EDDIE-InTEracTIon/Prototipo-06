using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System.Diagnostics;
using Leap;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
//using System.Windows.Interop;

namespace ReconocimentoTexto
{

    public partial class Form1 : Form
    {

        private byte[] imagedata = new byte[1];
        private Controller controller = new Controller();
        public Int64 prevTime;
        //public Int64 currentTime;
        public Int64 changeTime;
        public Frame currentFrame;
        public Frame prevFrame;
        private long currentTime;
        private long previousTime;
        private long timeChange;
        public Leap.Vector leapPoint;
        public float xScreenIntersect;
        public float yScreenIntersect;
        public float zScreenIntersect;
        public float xLeap1;
        public float yLeap1;
        public float xLeap2;
        public float yLeap2;
        public float xLeap3;
        public float yLeap3;
        public float leapStart;
        public float leapEnd;
        public float appEnd;
        public float appStart;
        public float leapStarty;
        public float leapEndy;
        public float appEndy;
        public float appStarty;
        public float xTrans;
        public float yTrans;
        public float pendienteX;
        int caseSwitch = 1;
        public int flipX = 1;
        int x = 0;
        int y = 0;
        int nClick = 0;
        int clickState = 0;





        Bitmap bitmap = new Bitmap(640, 480, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

        VideoCapture capture;
        private static Mat imagen = new Mat();
        private static Mat imagenOut = new Mat();
        private static float handX;
        private static float handY;
        private static float handZ;
        bool Pause = false;
        //Mat picture = new Mat(@"Dr.JekyllandMr.HydeText.jpg"); //¡Elija alguna ruta en su disco!
        Mat picture = new Mat(); //¡Elija alguna ruta en su disco!
        // Determina el límite de brillo al convertir la imagen en escala de grises en imagen binaria (blanco y negro)
        private const int Threshold = 1;

        // Erosión para eliminar el ruido (reducir las zonas de píxeles blancos)
        private const int ErodeIterations = 1;

        // Dilatación para mejorar los sobrevivientes de la erosión (ampliar zonas de píxeles blancos)
        private const int DilateIterations = 7;

        private static MCvScalar drawingColor = new Bgr(Color.Red).MCvScalar;

        public Form1()
        {
            InitializeComponent();
            controller.SetPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);// Optimizado para head mounted display
            controller.EventContext = WindowsFormsSynchronizationContext.Current;
            controller.FrameReady += newFrameHandler;
            controller.ImageReady += onImageReady;
            controller.FrameReady += OnFrame;
            controller.ImageRequestFailed += onImageRequestFailed;

            //establecer paleta de escala de grises para objeto de mapa de bits de imagen
            ColorPalette grayscale = bitmap.Palette;
            for (int i = 0; i < 256; i++)
            {
                grayscale.Entries[i] = Color.FromArgb((int)255, i, i, i);
            }
            bitmap.Palette = grayscale;
        }



        private void abrirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                capture = new VideoCapture(ofd.FileName);
                Mat m = new Mat();
                capture.Read(m);
                pictureBox1.Image = m.Bitmap;
            }
        }

        private async void playToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (capture == null)
            {
                return;
            }

            try
            {
                while (!Pause)
                {
                    Mat m = new Mat();
                    capture.Read(m);

                    if (!m.IsEmpty)
                    {
                        pictureBox2.Image = m.Bitmap;
                        double fps = capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps);
                        //double fps = 30;
                        await Task.Delay(1000 / Convert.ToInt32(fps));

                    }
                    else
                    {
                        break;
                    }

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }
        }

        private async void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Pause = !Pause;

            if (capture == null)
            {
                return;
            }

            try
            {
                while (!Pause)
                {
                    Mat m = new Mat();
                    capture.Read(m);

                    if (!m.IsEmpty)
                    {
                        pictureBox2.Image = m.Bitmap;
                        double fps = capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps);
                        await Task.Delay(1000 / Convert.ToInt32(fps));

                    }
                    else
                    {
                        break;
                    }

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }
        }



        private async void detectatTextoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (capture == null)
            {
                return;
            }

            try
            {
                while (!Pause)
                {
                    Mat m = new Mat();
                    //capture.Read(m);
                    m = picture;

                    if (!m.IsEmpty)
                    {
                        pictureBox1.Image = m.Bitmap;
                        DetectarTexto(m.ToImage<Bgr, byte>());
                        //double fps = capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps);
                        double fps = 1;
                        await Task.Delay(1000 / Convert.ToInt32(fps));

                    }
                    else
                    {
                        break;
                    }

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }
        }

        private void DetectarTexto(Image<Bgr, byte> img)
        {
            Image<Gray, byte> sobel = img.Convert<Gray, byte>().Sobel(1, 0, 3).AbsDiff(new Gray(0.0)).Convert<Gray, byte>().ThresholdBinary(new Gray(100), new Gray(255));
            Mat SE = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(10, 1), new Point(-1, -1));
            sobel = sobel.MorphologyEx(Emgu.CV.CvEnum.MorphOp.Dilate, SE, new Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Reflect, new MCvScalar(255));
            Emgu.CV.Util.VectorOfVectorOfPoint contours = new Emgu.CV.Util.VectorOfVectorOfPoint();
            Mat m = new Mat();

            CvInvoke.FindContours(sobel, contours, m, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
            List<Rectangle> list = new List<Rectangle>();

            for (int i = 0; i < contours.Size; i++)
            {
                Rectangle brect = CvInvoke.BoundingRectangle(contours[i]);

                double ar = brect.Width / brect.Height;

                if (brect.Width > 1 && brect.Height > 1 && brect.Height < 50)
                {
                    list.Add(brect);
                }
            }

            Image<Bgr, byte> imgout = img.CopyBlank();
            foreach (var r in list)
            {
                CvInvoke.Rectangle(img, r, new MCvScalar(0, 0, 255), 2);
                CvInvoke.Rectangle(img, r, new MCvScalar(0, 255, 255), -1);
                CvInvoke.Rectangle(imgout, r, new MCvScalar(0, 255, 255), -1);

                imgout._And(img);


                pictureBox3.Image = img.Bitmap;
                imagen = img.Mat;
                pictureBox2.Image = imgout.Bitmap;



            }



        }
        private void DetectarTexto2
            (Image<Bgr, byte> img)
        {
            Image<Gray, byte> sobel = img.Convert<Gray, byte>().Sobel(1, 0, 3).AbsDiff(new Gray(0.0)).Convert<Gray, byte>().ThresholdBinary(new Gray(100), new Gray(255));
            Mat SE = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(10, 1), new Point(-1, -1));
            sobel = sobel.MorphologyEx(Emgu.CV.CvEnum.MorphOp.Dilate, SE, new Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Reflect, new MCvScalar(255));
            Emgu.CV.Util.VectorOfVectorOfPoint contours = new Emgu.CV.Util.VectorOfVectorOfPoint();
            Mat m = new Mat();

            CvInvoke.FindContours(sobel, contours, m, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
            List<Rectangle> list = new List<Rectangle>();

            for (int i = 0; i < contours.Size; i++)
            {
                Rectangle brect = CvInvoke.BoundingRectangle(contours[i]);

                double ar = brect.Width / brect.Height;

                if (brect.Width > 1 && brect.Height > 1 && brect.Height < 50)
                {
                    list.Add(brect);
                }
            }

            Image<Bgr, byte> imgout = img.CopyBlank();
            foreach (var r in list)
            {
                CvInvoke.Rectangle(img, r, new MCvScalar(0, 0, 255), 2);
                CvInvoke.Rectangle(img, r, new MCvScalar(0, 255, 255), -1);
                CvInvoke.Rectangle(imgout, r, new MCvScalar(0, 255, 255), -1);

                imgout._And(img);


                pictureBox3.Image = img.Bitmap;
                imagen = img.Mat;
                pictureBox2.Image = imgout.Bitmap;



            }



        }

        private void archivoToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private async void playToolStripMenuItem1_Click(object sender, EventArgs e)//Procedimiento que abre la camara
        {
            if (capture == null)
            {
                capture = new VideoCapture();
            }


            //capture.ImageGrabbed += Capture_ImageGrabbed1;
            capture.Start();
            if (capture == null)
            {
                return;
            }

            try
            {
                while (!Pause)
                {
                    Mat m = new Mat();
                    capture.Read(m);

                    if (!m.IsEmpty)
                    {
                        pictureBox1.Image = m.Bitmap;
                        //double fps = capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps);
                        double fps = 60;
                        await Task.Delay(1000 / Convert.ToInt32(fps));

                    }
                    else
                    {
                        break;
                    }

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }
        }

        private void Capture_ImageGrabbed1(object sender, EventArgs e)
        {
            try
            {
                Mat m = new Mat();
                capture.Retrieve(m);
                pictureBox1.Image = m.ToImage<Bgr, byte>().Bitmap;

            }
            catch (Exception)
            {

                throw;
            }



        }

        private async void detectarManoToolStripMenuItem_ClickAsync(object sender, EventArgs e)
        {
            if (capture == null)
            {
                return;
            }

            try
            {
                while (!Pause)
                {
                    Mat m = new Mat();
                    Mat n = new Mat();
                    Mat o = new Mat();
                    Mat binaryDiffFrame = new Mat();
                    Mat denoisedDiffFrame = new Mat();
                    Mat finalFrame = new Mat();
                    //pictureBox3.DrawToBitmap();
                    capture.Read(m);

                    if (!m.IsEmpty)
                    {
                        //CvInvoke.AbsDiff(m, imagen, n);
                        // Apply binary threshold to grayscale image (white pixel will mark difference)
                        //CvInvoke.CvtColor(n, o, ColorConversion.Bgr2Gray);
                        //CvInvoke.Threshold(o, binaryDiffFrame, 5, 255, ThresholdType.Binary);// 5 Determines boundary of brightness while turning grayscale image to binary (black-white) image

                        // Remove noise with opening operation (erosion followed by dilation)
                        //CvInvoke.Erode(binaryDiffFrame, denoisedDiffFrame, null, new Point(-1, -1), ErodeIterations, BorderType.Default, new MCvScalar(1));
                        //CvInvoke.Dilate(denoisedDiffFrame, denoisedDiffFrame, null, new Point(-1, -1), DilateIterations, BorderType.Default, new MCvScalar(1));
                        pictureBox4.Image = denoisedDiffFrame.Bitmap;
                        //Image<Bgr, Byte> imgeOrigenal = BackgroundToGreen(m.ToImage<Bgr, Byte>());
                        //pictureBox6.Image = picture.Bitmap;
                        //DetectarTexto(m.ToImage<Bgr, byte>());
                        //double fps = capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps);
                        m.CopyTo(finalFrame);
                        DetectObject(denoisedDiffFrame, finalFrame);
                        pictureBox5.Image = finalFrame.Bitmap;
                        double fps = 1;
                        await Task.Delay(1000 / Convert.ToInt32(fps));

                    }
                    else
                    {
                        break;
                    }


                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }

        }

        private static void DetectObject(Mat detectionFrame, Mat displayFrame)
        {
            CvInvoke.Circle(displayFrame, new Point((int)handX + 320, (int)handY + 240), Math.Abs((int)handZ), new MCvScalar(255, 0, 0), 3);

        }

        private static void MarkDetectedObject(Mat frame, VectorOfPoint contour, double area)
        {
            // Obteniene un rectángulo mínimo que contiene el contorno
            Rectangle box = CvInvoke.BoundingRectangle(contour);

            // Drawing contour and box around it
            CvInvoke.Polylines(frame, contour, true, drawingColor, 1, LineType.FourConnected);
            CvInvoke.Rectangle(frame, box, drawingColor);

            // Dibujar contorno y recuadro a su alrededor
            Point center = new Point(box.X + box.Width / 2, box.Y + box.Height / 2);

            var info = new string[] {
                $"Area: {area}",
                $"Position: {center.X}, {center.Y}"
            };

            WriteMultilineText(frame, info, new Point(box.Right + 5, center.Y));
        }

        private static void WriteMultilineText(Mat frame, string[] lines, Point origin)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                int y = i * 10 + origin.Y; // Bajado en cada línea
                CvInvoke.PutText(frame, lines[i], new Point(origin.X, y), FontFace.HersheyPlain, 0.8, drawingColor);
            }
        }

        public static Image<Bgr, byte> BackgroundToGreen(Image<Bgr, byte> rgbimage)
        {
            for (int i = 0; i < rgbimage.ManagedArray.GetLength(0); i++)
            {
                for (int j = 0; j < rgbimage.ManagedArray.GetLength(1); j++)
                {
                    Bgr currentColor = rgbimage[i, j];

                    if (/*currentColor.Blue >= minB && currentColor.Blue <= maxB &&*/ currentColor.Green >= 255 && 0 <= currentColor.Green /*&& currentColor.Red >= minR && currentColor.Red <= maxR*/)
                    {
                        rgbimage[i, j] = new Bgr(255, 255, 255);
                    }
                }
            }
            return rgbimage;
            /*
            Image<Bgr, byte> ret = rgbimage;
            var image = rgbimage.InRange(new Bgr(190, 190, 190), new Bgr(255, 255, 255));
            var mat = rgbimage.Mat;
            mat.SetTo(new MCvScalar(200, 237, 204), image);
            mat.CopyTo(ret);
            return ret;*/
        }

        private void capturarImagenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            picture = capture.QueryFrame();
        }

        void newFrameHandler(object sender, FrameEventArgs eventArgs)
        {
            Frame frame = eventArgs.frame;
           
            this.displayID.Text = frame.Id.ToString();
            this.displayTimestamp.Text = frame.Timestamp.ToString();
            this.displayFPS.Text = frame.CurrentFramesPerSecond.ToString();
            this.displayHandCount.Text = frame.Hands.Count.ToString();

            controller.RequestImages(frame.Id, Leap.Image.ImageType.DEFAULT, imagedata);
        }

        void onImageRequestFailed(object sender, ImageRequestFailedEventArgs e)
        {
            if (e.reason == Leap.Image.RequestFailureReason.Insufficient_Buffer)
            {
                imagedata = new byte[e.requiredBufferSize];
            }
            Console.WriteLine("Image request failed: " + e.message);
        }

        void onImageReady(object sender, ImageEventArgs e)
        {
            Rectangle lockArea = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitmapData = bitmap.LockBits(lockArea, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            byte[] rawImageData = imagedata;
            System.Runtime.InteropServices.Marshal.Copy(rawImageData, 0, bitmapData.Scan0, e.image.Width * e.image.Height * 2 * e.image.BytesPerPixel);
            bitmap.UnlockBits(bitmapData);
            pictureBox6.Image = bitmap;//imagen de motion leap
        }



        public void OnFrame(object sender, FrameEventArgs args)
        {

            // Obtener el frame actual.
            Frame currentFrame = controller.Frame();
            Vector center;
            center.x = 0;
            center.y = 0;
            center.z = 0;
            Vector size;
            size.x = 1000;
            size.y = 1000;
            size.z = 1000;


            currentTime = currentFrame.Timestamp;
            timeChange = currentTime - previousTime;

            if (timeChange > 1000)
            {
                if (currentFrame.Hands.Count() > 0)
                {
                    // Consigue el primer dedo en la lista de dedos
                    Finger finger = controller.Frame().Hands[0].Fingers[1];

                    InteractionBox screen = new InteractionBox(center, size);
                    screen = controller.Frame().InteractionBox;
                    //textBox2.AppendText("tipo de dedo : "+ finger.Type);



                   // if (screen.IsValid)
                        if (!screen.IsValid)
                    {
                        // Obtenga la velocidad de la punta del dedo
                        var tipVelocity = (int)finger.TipVelocity.Magnitude;

                        // Use tipVelocity para reducir el pulso
                        // the cursor steady if (tipVelocity > 25)
                        if (tipVelocity < 25)
                        {
                            leapStart = (float)numericUpDown9.Value;
                            leapEnd = (float)numericUpDown10.Value;
                            appStart = (float)numericUpDown11.Value;
                            appEnd = (float)numericUpDown12.Value;

                            leapPoint = finger.StabilizedTipPosition;

                            leapStarty = (float)numericUpDown17.Value;
                            leapEndy = (float)numericUpDown18.Value;
                            appStarty = (float)numericUpDown15.Value;
                            appEndy = (float)numericUpDown16.Value;


                            leapPoint = finger.StabilizedTipPosition;
                            pendienteX = (appEnd - appStart) / (leapEnd - leapStart);
                            xScreenIntersect = screen.NormalizePoint(leapPoint, true).x;
                            yScreenIntersect = screen.NormalizePoint(leapPoint, true).y;
                            zScreenIntersect = screen.NormalizePoint(leapPoint, true).z;
                            //xScreenIntersect *= 0.7f;
                            //yScreenIntersect *= 1.5f;
                            xScreenIntersect *= (float)numericUpDown1.Value;
                            yScreenIntersect *= (float)numericUpDown2.Value;
                            zScreenIntersect *= (float)numericUpDown7.Value;

                            xScreenIntersect -= (float)numericUpDown3.Value;
                            yScreenIntersect -= (float)numericUpDown4.Value;
                            zScreenIntersect -= (float)numericUpDown8.Value;



                            if (xScreenIntersect.ToString() != "NaN")
                            {

                                /*if (leapPoint.x > 0)
                                {
                                    xTrans = leapStart - leapPoint.x;
                                }
                                else
                                {
                                    xTrans = leapStart - leapPoint.x;
                                }

                                if (leapPoint.y > 0)
                                {
                                    yTrans = leapStarty - leapPoint.y;
                                }
                                else
                                {
                                    yTrans = leapStarty - leapPoint.y;
                                }*/
                                //x = (int)(xScreenIntersect * screen.Width);
                                //y = (int)(screen.Height - (yScreenIntersect * screen.Height));
                                
                                if (clickState == 0)
                                {
                                
                                    int extendedFingers = 0;
                                    for (int f = 0; f < controller.Frame().Hands[0].Fingers.Count; f++)
                                    {
                                        Finger digit = controller.Frame().Hands[0].Fingers[f];
                                        if (digit.IsExtended)
                                            extendedFingers++;
                                    }

                                    if (extendedFingers == 1)
                                    {
                                        textBox2.AppendText("Click 1");
                                        nClick++;

                                    }
                                    else
                                    {
                                        textBox2.AppendText("_");
                                        nClick = 0;
                                    }
                                    if (nClick == 3)
                                    {


                                        Clicking.SendClick(x, y);
                                        nClick = 0;
                                        clickState = 1;
                                    }
                                }

                                if (clickState == 1)
                                {
                                    int extendedFingers = 0;
                                    for (int f = 0; f < controller.Frame().Hands[0].Fingers.Count; f++)
                                    {
                                        Finger digit = controller.Frame().Hands[0].Fingers[f];
                                        if (digit.IsExtended)
                                            extendedFingers++;
                                    }

                                    if (extendedFingers == 5)
                                    {
                                        textBox2.AppendText("upClick 1");
                                        nClick++;

                                    }
                                    else
                                    {
                                        textBox2.AppendText("_");
                                        nClick = 0;

                                       
                                    }
                                    if (nClick == 3)
                                    {


                                        Clicking.SendUpClick(x, y);
                                        nClick = 0;
                                        clickState = 0;
                                    }
                                }

                                if (checkBox1.Checked)
                                {
                                    x = (int)Math.Abs((leapPoint.x - leapStart) * ((appEnd - appStart) / (leapEnd - leapStart)) + appStart);
                                }
                                else
                                {
                                    x = Math.Abs((int)((float)numericUpDown5.Value * (float)numericUpDown1.Value - xScreenIntersect * (float)numericUpDown5.Value * (float)numericUpDown1.Value + (float)numericUpDown13.Value));
                                    //x = (int)(xScreenIntersect * (float)numericUpDown5.Value);
                                    //textBox1.AppendText("Cambio de posicion de x1 " + (int)appStart + (float)numericUpDown11.Value);// valor de appstart y numericupdown11


                                }

                                if (checkBox2.Checked)
                                {
                                    y = (int)Math.Abs((leapPoint.z - leapStarty) * ((appEndy - appStarty) / (leapEndy - leapStarty)) + appStarty);
                                }
                                else
                                {
                                    //y = (int)((float)numericUpDown6.Value - (yScreenIntersect * (float)numericUpDown6.Value) + (float)numericUpDown14.Value);
                                    //y = (int)(yScreenIntersect * (float)numericUpDown6.Value + (float)numericUpDown14.Value);
                                    y = 500;
                                }
                                //x = x * flipX;

                                //x = (int)((leapPoint.x - leapStart)*(leapEnd-leapStart)*(appEnd-appStart)+appStart);
                                 

                                //textBox1. AppendText("Screen intersect X: " + xScreenIntersect.ToString());
                                //textBox1.AppendText("Screen intersect Y: " + yScreenIntersect.ToString());
                                //textBox1.AppendText("Width pixels: " + screen.Width.ToString());
                                //textBox1.AppendText("Height pixels: " + screen.Height .ToString());

                                //textBox1.AppendText("\n");

                                // textBox1.AppendText("x: " + x.ToString());
                                //textBox1.AppendText("y: " + y.ToString());

                                //textBox1.AppendText("\n");

                                // textBox1.AppendText("Tip velocity: " + tipVelocity.ToString());

                                // Move the cursor
                                MouseCursor.MoveCursor(x, y);

                                //textBox1.AppendText("\n" + new String('=', 40) + "\n");
                            }

                        }
                    }

                }

                previousTime = currentTime;
            }




            // Obtiene el marco más reciente e informa información básica
            Frame frame = args.frame;

            Console.WriteLine(
              "Frame id: {0}, timestamp: {1}, hands: {2}",
              frame.Id, frame.Timestamp, frame.Hands.Count
            );


            foreach (Hand hand in frame.Hands)
            {



                if (hand.Fingers.Count == 1)
                {


                    this.label18.Text = hand.Fingers[0].TipPosition.ToString();

                }
                this.label18.Text = hand.Fingers[0].TipPosition.ToString();



                Console.WriteLine("  Hand id: {0}, palm position: {1}, fingers: {2}",
                  hand.Id, hand.PalmPosition, hand.Fingers.Count);
                this.label15.Text = hand.Id.ToString();
                this.label5.Text = hand.PalmPosition.ToString();
                handX = hand.PalmPosition.x;
                handY = hand.PalmPosition.y;
                handZ = hand.PalmPosition.z;
                this.label7.Text = hand.Fingers.Count.ToString();
                // Obtiene el vector y la dirección normal de la mano
                Vector normal = hand.PalmNormal;
                Vector direction = hand.Direction;

                // Calcule los ángulos de pitch, roll y yaw de la mano.
                Console.WriteLine(
                  "  Hand pitch: {0} degrees, roll: {1} degrees, yaw: {2} degrees",
                  direction.Pitch * 180.0f / (float)Math.PI,
                  normal.Roll * 180.0f / (float)Math.PI,
                  direction.Yaw * 180.0f / (float)Math.PI
                );

                this.label13.Text = (direction.Pitch * 180.0f / (float)Math.PI).ToString();
                this.label11.Text = (normal.Roll * 180.0f / (float)Math.PI).ToString();
                this.label9.Text = (direction.Yaw * 180.0f / (float)Math.PI).ToString();
                this.label21.Text = x.ToString();
                this.label19.Text = y.ToString();
                this.label28.Text = xLeap1.ToString();
                this.label29.Text = yLeap1.ToString();
                this.label30.Text = xLeap2.ToString();
                this.label33.Text = yLeap2.ToString();
                this.label32.Text = xLeap3.ToString();
                this.label31.Text = yLeap3.ToString();
                this.label46.Text = xTrans.ToString();
                this.label48.Text = pendienteX.ToString();


                //Obtiene el hueso del brazo
                Arm arm = hand.Arm;
                Console.WriteLine(
                  "  Arm direction: {0}, wrist position: {1}, elbow position: {2}",
                  arm.Direction, arm.WristPosition, arm.ElbowPosition
                );

                // Obtiene los dedos
                foreach (Finger finger in hand.Fingers)
                {
                    Console.WriteLine(
                      "    Finger id: {0}, {1}, length: {2}mm, width: {3}mm",
                      finger.Id,
                      finger.Type.ToString(),
                      finger.Length,
                      finger.Width
                    );



                    // Obtiene huesos de los dedos
                    Bone bone;
                    for (int b = 0; b < 4; b++)
                    {
                        bone = finger.Bone((Bone.BoneType)b);
                        Console.WriteLine(
                          "      Bone: {0}, start: {1}, end: {2}, direction: {3}",
                          bone.Type, bone.PrevJoint, bone.NextJoint, bone.Direction
                        );
                    }
                }
            }

            if (frame.Hands.Count != 0)
            {
                Console.WriteLine("");
            }


        }

        private void proyeccionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();

            form2.ShowDialog();
        }

        private void label17_Click(object sender, EventArgs e)
        {

        }

        class MouseCursor
        {
            [DllImport("user32.dll")]
            private static extern bool SetCursorPos(int x, int y);


            public static void MoveCursor(int x, int y)
            {
                SetCursorPos(x, y);
            }



        }
        class Clicking
        {
            [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
            //[DllImport("user32.dll")]
            //static extern void Mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
            //private const UInt32 MOUSEEVENTF_LEFTDOWN = 0x0002;
            //private const UInt32 MOUSEEVENTF_LEFTUP = 0x0004;
            /*private static extern void Mouse_event(
                   UInt32 dwFlags, // motion and click options
                   UInt32 dx, // horizontal position or change
                   UInt32 dy, // vertical position or change
                   UInt32 dwData, // wheel movement
                   IntPtr dwExtraInfo // application-defined information
            );*/

            // public static void SendClick(Point location)
            public static void SendClick(int x, int y)
            {
                // Cursor.Position = location;
                //Mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, new System.IntPtr());
                //Mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, new System.IntPtr());
                mouse_event(0x0002, 0, x, y, 0);
            }
            public static void SendUpClick(int x, int y)
            {
                // Cursor.Position = location;
                //Mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, new System.IntPtr());
                //Mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, new System.IntPtr());
                mouse_event(0x0004, 0, x, y, 0);
            }


        }

        private void label23_Click(object sender, EventArgs e)
        {

        }

        private void reconocerPantallaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (true)
            {
                textBox1.AppendText("figuras de deteccion de pantalla");
            }

        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                textBox1.AppendText("Tecla space precionada \n " + " X:" + leapPoint.x + " Y:" + leapPoint.z + "\n");
                textBox1.AppendText(" Xi:" + xScreenIntersect + " Yi:" + yScreenIntersect + "\n");

            }



            switch (caseSwitch)
            {
                case 1:
                    textBox1.AppendText("Vector 1");
                    xLeap1 = xScreenIntersect;
                    yLeap1 = leapPoint.y;
                    caseSwitch++;
                    break;
                case 2:
                    textBox1.AppendText("Vector 2");
                    xLeap2 = xScreenIntersect;
                    yLeap2 = leapPoint.y;
                    caseSwitch++;
                    break;
                case 3:
                    textBox1.AppendText("Vector 3");
                    xLeap3 = xScreenIntersect;
                    yLeap3 = leapPoint.y;
                    caseSwitch = 1;
                    break;
                default:
                    textBox1.AppendText("Vector indefinido");
                    caseSwitch = 1;
                    break;
            }
        }

        private void label25_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                flipX = -1;
            }
            else
            {
                flipX = 1;
            }
            textBox1.AppendText("flipx" + checkBox1.Checked);
        }


        private void label43_Click(object sender, EventArgs e)
        {
            textBox1.AppendText("Cambio de posicion de x1 " + (int)appStart + (float)numericUpDown11.Value);
            Point loc = label43.Location;
            loc.X = (int)appStart;
            label43.Location = loc;
        }

  
    }
}
