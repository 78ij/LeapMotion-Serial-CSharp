using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Leap;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Frame = Leap.Frame;
using Vector = Leap.Vector;
namespace Leapmotion_Serial
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>

    class LeapListener
    {
        float xratio = 1.70f; // x放大系数
        float zratio = 3.40f; // z放大系数
        float yratio = 1.00f; // y放大系数

        float depthMax = 1000.0f;
        float depthMin = 50.0f;

        Byte[,] LEDs = new Byte[8, 8];
        List<Label> labels = new List<Label>();
        List<Rectangle> rects = new List<Rectangle>();
        public PortConnector port = new PortConnector();

        public LeapListener(Label[] label, Grid g)
        {
            foreach (Label l in label)
            {
                labels.Add(l);
            }
            InitLED(true);
            foreach (Rectangle r in g.Children)
            {
                rects.Add(r);
            }
        }
        public Byte Clamp(int color)
        {
            if (color > 255) return 255;
            else if (color < 0) return 0;
            else return (Byte)color;
        }
        //LED光栅化方法：中心为0,0，半径为85
        public void InitLED(bool isfirst)
        {
            for (int i = 0; i <= 7; i++)
            {
                for (int j = 0; j <= 7; j++)
                {
                    LEDs[i, j] = 0;
                }
            }
            foreach (Rectangle r in rects)
            {
                if (isfirst)
                {
                    r.Fill = Brushes.White;
                }
                else
                {
                    SolidColorBrush oldbrush = (SolidColorBrush)r.Fill;
                    Color oldcolor = oldbrush.Color;
                    if (oldcolor.R == 255) continue;
                    else r.Fill = new SolidColorBrush(Color.FromRgb(Clamp(oldcolor.R + 10),
                        Clamp(oldcolor.R + 10),
                        Clamp(oldcolor.R + 10)));
                }
            }
        }
        bool IsInRect(Vector point, float left, float right, float top, float down)
        {
            if (point.x >= left && point.x <= right && point.z >= down && point.z <= top)
            {
                return true;
            }
            else return false;
        }

        Vector Transform(Vector a)
        {
            a.x *= xratio;
            a.z *= zratio;
            a.y *= yratio;
            return a;
        }



        public void OnInit(Controller controller)
        {

        }
        public void OnConnect(object sender, DeviceEventArgs args)
        {
            MessageBox.Show("Connected!", "LMConnect Message");
        }

        public void OnDisconnect(object sender, ConnectionLostEventArgs args)
        {
            MessageBox.Show("Disconnected!", "LMConnect Message");

        }
        public void OnFrame(object sender, FrameEventArgs args)
        {
            Frame frame = args.frame;
            InitLED(false);

            string message = String.Format(
              "PortState: {0}\nFrame id: {1}, timestamp: {2}, hands: {3}",
              port.IsPortOpen().ToString(),
              frame.Id, frame.Timestamp, frame.Hands.Count
            );
            labels[0].Content = message;
            foreach (Hand hand in frame.Hands)
            {
                // Get the hand's normal vector and direction
                Vector normal = hand.PalmNormal;
                Vector direction = hand.Direction;
                Vector position = Transform(hand.PalmPosition);
                double width = hand.PalmWidth;
                // Calculate the hand's pitch, roll, and yaw angles

                message = String.Format("Hand id: {0}, palm position: {1}, fingers: {2}, width:{3}\nHand pitch: {4} degrees, roll: {5} degrees, yaw: {6} degrees",
                  hand.Id,
                  hand.PalmPosition, 
                  hand.Fingers.Count, 
                  hand.PalmWidth * xratio,
                  direction.Pitch * 180.0f / (float)Math.PI,
                  normal.Roll * 180.0f / (float)Math.PI,
                  direction.Yaw * 180.0f / (float)Math.PI
                  );
                labels[1].Content = message;

                
                float left = position.x - hand.PalmWidth / 2 * xratio;
                float right = left + hand.PalmWidth * xratio;
                float top = position.z + hand.PalmWidth / 2 * zratio;
                float down = top - hand.PalmWidth * zratio;
                float depth = position.y;
                byte color = (byte)(255 * Math.Max(Math.Min((depth - depthMin) / (depthMax - depthMin), 1.0f), 0.0f));


                message = String.Format(
                  "palm position scaled: {0}\nplane pos: ({1}, {2}), depth: {3}\ncolor: {4}",
                  position, (int)Math.Round(position.x), (int)Math.Round(position.z), (int)Math.Round(position.y),
                  color
                );
                labels[2].Content = message;

                for (int i = 0; i <= 7; i++)
                {
                    for (int j = 0; j <= 7; j++)
                    {
                        if (IsInRect(new Vector(85 * j - 340, 0, 85 * i - 340), left, right, top, down))
                        {
                            LEDs[i, j] = 1;
                            rects[i * 8 + j].Fill = new SolidColorBrush(Color.FromRgb(color, color, color));
                        }
                    }
                }
            }

            port.Write(LEDs);
        }

    }
    public partial class MainWindow : Window
    {
        PortConnector port;
        public void tryConnectPort(string portname)
        {
            if (port.IsPortOpen())
                return;
            try
            {
                port.OpenPort(portname);
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.Message);
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            using (Leap.IController controller = new Leap.Controller())
            {
                controller.SetPolicy(Leap.Controller.PolicyFlag.POLICY_ALLOW_PAUSE_RESUME);
                Label[] l = new Label[] { label1, label2, label3 };
                LeapListener listener = new LeapListener(l, Rects);
                port = listener.port;
                listener.InitLED(true);
                controller.Device += listener.OnConnect;
                controller.Disconnect += listener.OnDisconnect;
                controller.FrameReady += listener.OnFrame;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(COM.Text != "")
            {
                tryConnectPort(COM.Text);
            }
        }
    }
}
