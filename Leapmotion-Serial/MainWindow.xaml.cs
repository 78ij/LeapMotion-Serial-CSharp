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
        Byte[,] LEDs = new Byte[8, 8];
        List<Label> labels = new List<Label>();
        List<Rectangle> rects = new List<Rectangle>();
        public LeapListener(Label[] label, Grid g)
        {
            foreach(Label l in label)
            {
                labels.Add(l);
            }
            InitLED();
            foreach (Rectangle r in g.Children)
            {
                rects.Add(r);
            }
        }
        //LED光栅化方法：中心为0,0，半径为85
        public void InitLED()
        {
            for (int i = 0; i <= 7; i++)
            {
                for (int j = 0; j <= 7; j++)
                {
                    LEDs[i, j] = 0;
                }
            }
            foreach( Rectangle r in rects){
                r.Fill = Brushes.White;
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
            InitLED();

            string message = String.Format(
              "Frame id: {0}, timestamp: {1}, hands: {2}",
              frame.Id, frame.Timestamp, frame.Hands.Count
            );
            labels[0].Content = message;
            foreach (Hand hand in frame.Hands)
            {
                message = String.Format("  Hand id: {0}, palm position: {1}, fingers: {2}, width:{3}",
                  hand.Id, Transform(hand.PalmPosition), hand.Fingers.Count, hand.PalmWidth * xratio);
                labels[1].Content = message;

                // Get the hand's normal vector and direction
                Vector normal = hand.PalmNormal;
                Vector direction = hand.Direction;
                Vector point = hand.PalmPosition;
                double width = hand.PalmWidth;
                // Calculate the hand's pitch, roll, and yaw angles
                message = String.Format(
                  "  Hand pitch: {0} degrees, roll: {1} degrees, yaw: {2} degrees",
                  direction.Pitch * 180.0f / (float)Math.PI,
                  normal.Roll * 180.0f / (float)Math.PI,
                  direction.Yaw * 180.0f / (float)Math.PI
                );
                labels[2].Content = message;
                float z = 0;
                float left = Transform(hand.PalmPosition).x - hand.PalmWidth / 2 * xratio;
                float right = left + hand.PalmWidth * xratio;
                float top = Transform(hand.PalmPosition).z + hand.PalmWidth / 2 * zratio;
                float down = top - hand.PalmWidth * zratio;
                for(int i = 0;i <= 7; i++)
                {
                    for(int j = 0;j <= 7; j++)
                    {
                        if (IsInRect(new Vector(85 * j - 340, 0, 85 * i - 340),left,right,top,down)){
                            LEDs[i,j] = 1;
                            rects[i * 8 + j].Fill = Brushes.Black;
                        }
                    }
                }
            }
        }

    }
    public partial class MainWindow : Window
    {
       

        public MainWindow()
        {
            InitializeComponent();
            using (Leap.IController controller = new Leap.Controller())
            {
                controller.SetPolicy(Leap.Controller.PolicyFlag.POLICY_ALLOW_PAUSE_RESUME);
                Label[] l= new Label[] { label1, label2, label3 };
                LeapListener listener = new LeapListener(l,Rects);
                controller.Device += listener.OnConnect;
                controller.Disconnect += listener.OnDisconnect;
                controller.FrameReady += listener.OnFrame;
            }
        }
    }
}
