using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleViewer
{
    public partial class Form4 : Form
    {
        public Bitmap image;

        public Form4()
        {
            InitializeComponent();

            image = new Bitmap(512, 512);

            pictureBox1.Image = image;

            image.SetPixel(100, 100, Color.Black);            
        }
        public Form4(int width, int height)
        {
            InitializeComponent();

            image = new Bitmap(width, height);

            pictureBox1.Image = image;

            image.SetPixel(100, 100, Color.Black);
        }
    }
}
