using Corale.Colore.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using ColoreColor = Corale.Colore.Core.Color;
using uPLibrary.Networking.M2Mqtt.Messages;
using Newtonsoft.Json.Linq;


namespace LightingProject
{

    public partial class MainWindow : Form
    {
        MqttClient client;
        double fH = 0,fS = 0, fV = 100, prev=0;
            
        public MainWindow() {
            InitializeComponent();
        }
        public struct RGB
        {
            private byte _r;
            private byte _g;
            private byte _b;

            public RGB(byte r, byte g, byte b)
            {
                this._r = r;
                this._g = g;
                this._b = b;
            }

            public byte R
            {
                get { return this._r; }
                set { this._r = value; }
            }

            public byte G
            {
                get { return this._g; }
                set { this._g = value; }
            }

            public byte B
            {
                get { return this._b; }
                set { this._b = value; }
            }

            public bool Equals(RGB rgb)
            {
                return (this.R == rgb.R) && (this.G == rgb.G) && (this.B == rgb.B);
            }
        }

        public struct HSV
        {
            private double _h;
            private double _s;
            private double _v;

            public HSV(double h, double s, double v)
            {
                this._h = h;
                this._s = s;
                this._v = v;
            }

            public double H
            {
                get { return this._h; }
                set { this._h = value; }
            }

            public double S
            {
                get { return this._s; }
                set { this._s = value; }
            }

            public double V
            {
                get { return this._v; }
                set { this._v = value; }
            }

            public bool Equals(HSV hsv)
            {
                return (this.H == hsv.H) && (this.S == hsv.S) && (this.V == hsv.V);
            }
        }

        public static RGB HSVToRGB(HSV hsv)
        {
            double r = 0, g = 0, b = 0;

            if (hsv.S == 0)
            {
                r = hsv.V;
                g = hsv.V;
                b = hsv.V;
            }
            else
            {
                int i;
                double f, p, q, t;

                if (hsv.H == 360)
                    hsv.H = 0;
                else
                    hsv.H = hsv.H / 60;

                i = (int)Math.Truncate(hsv.H);
                f = hsv.H - i;

                p = hsv.V * (1.0 - hsv.S);
                q = hsv.V * (1.0 - (hsv.S * f));
                t = hsv.V * (1.0 - (hsv.S * (1.0 - f)));

                switch (i)
                {
                    case 0:
                        r = hsv.V;
                        g = t;
                        b = p;
                        break;

                    case 1:
                        r = q;
                        g = hsv.V;
                        b = p;
                        break;

                    case 2:
                        r = p;
                        g = hsv.V;
                        b = t;
                        break;

                    case 3:
                        r = p;
                        g = q;
                        b = hsv.V;
                        break;

                    case 4:
                        r = t;
                        g = p;
                        b = hsv.V;
                        break;

                    default:
                        r = hsv.V;
                        g = p;
                        b = q;
                        break;
                }

            }

            return new RGB((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        private void Exit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                 client = new MqttClient("broker.mqttdashboard.com");
                byte code = client.Connect(Guid.NewGuid().ToString());

                ushort msgId = client.Subscribe(new string[] { "DonBridge/from/set/#" }, new byte[] { 2 });

                client.MqttMsgSubscribed += client_MqttMsgSubscribed;
                client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                
            }catch(Exception ex)
            {
                MessageBox.Show("Failed to setup connection\n\n"+ex.Message);
            }
            Console.WriteLine("hi");
        }



        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string message = Encoding.UTF8.GetString(e.Message);
            
            try
            {
                dynamic msg = JObject.Parse(message);
                UpdateDevices(msg);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Message received but failed to parse JSON\n\n" + ex.Message);
                //Application.Exit();
            }

            


        }

        private void UpdateDevices(dynamic msg)
        {
            if (msg.characteristic == "Hue")
            UpdateLabel(HueFeed, Convert.ToString(msg));

            if (msg.characteristic == "Saturation")
                UpdateLabel(SaturationFeed, Convert.ToString(msg));


            Console.Write(msg.characteristic);
            Console.WriteLine(msg.value);

            if (msg.characteristic == "On" && msg.value == "False") fV = 0; else if (msg.characteristic == "On" && msg.value == "True") fV = prev;

            if (msg.characteristic == "Hue") fH = msg.value;
            if (msg.characteristic == "Saturation") fS = msg.value;
            if (msg.characteristic == "Brightness") fV = msg.value;



            HSV data = new HSV(fH, fS/100, fV/100);
            RGB value = HSVToRGB(data);


            UpdateLabel(label2, Convert.ToString(value.R));
            UpdateLabel(label3, Convert.ToString(value.G));
            UpdateLabel(label4, Convert.ToString(value.B));
            
      
            var color = new ColoreColor((byte)value.R, (byte)value.G, (byte)value.B);
            Chroma.Instance.SetAll(color);


            if(fV!=0)prev = fV;


        }

        void UpdateLabel(Label lbl, String text)
        {
            if (lbl.InvokeRequired)
            { lbl.Invoke(new Action<Label, String>(UpdateLabel), new object[] { lbl, text }); }
            else
            { lbl.Text = text; }
        }

        private void client_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
            Console.WriteLine("MessageId = " + e.MessageId + " Published = " + e.IsPublished);
        }
        void client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            MessageBox.Show("Sucessfully Connected and Subscribed");
        }
    }
}
