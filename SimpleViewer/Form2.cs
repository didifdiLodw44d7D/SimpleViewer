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
    public partial class Form2 : Form
    {
        List<TagElementObjectData> obj;
        public Form2()
        {
            InitializeComponent();
        }

        public Form2(List<TagElementObjectData> obj, int row)
        {
            InitializeComponent();

            this.obj = obj;

            textBox1.ReadOnly = true;
            textBox2.ReadOnly = true;
            textBox3.ReadOnly = true;
            textBox4.ReadOnly = true;
            textBox5.ReadOnly = true;

            textBox1.Text = GetTagName(obj[row].tag);
            textBox2.Text = GetElementName(obj[row].element);
            textBox3.Text = GetVR(obj[row].vr);
            textBox4.Text = GetLength(obj[row].length);
            textBox5.Text = GetValue(GetVR(obj[row].vr), obj[row].value);

            button1.Click += Button1_Click;
            button2.Click += Button2_Click;
            button3.Click += Button3_Click;
            button4.Click += Button4_Click;
            button5.Click += Button5_Click;

            SetTreeView();
        }

        private void SetTreeView()
        {
            List<TreeNode> treeNodeTag = new List<TreeNode>();
            List<TreeNode> treeNodeElement = new List<TreeNode>();

            int i = 0;

            var data = (from x in obj
                        select GetTagName(x.tag))
                        .Distinct();

            foreach (var s in data)
            {
                treeNodeElement = new List<TreeNode>();

                var element = from x in obj
                              where GetElementName(x.tag) == s
                              select obj;

                foreach (var t in element)
                {
                    treeNodeElement.Add(new TreeNode(GetElementName(t[i++].element)));
                }

                treeNodeTag.Add(new TreeNode(s, treeNodeElement.ToArray()));
            }

            treeView1.Nodes.AddRange(treeNodeTag.ToArray());

            treeView1.ExpandAll();
        }
        private List<string> SetTreeNodeElement(List<TagElementObjectData> obj, string tag)
        {
            List<string> ret = null;

            foreach(var s in obj)
            {
                string obj_tag = GetTagName(s.tag);

                if (obj_tag == tag)
                    ret.Add(GetElementName(s.element));
            }

            return ret;
        }
        private void CopyingDurationEvent()
        {
            var status = toolStripStatusLabel1.Text;
            toolStripStatusLabel1.Text = "copying !!";
            Task.Run(() => {
                System.Threading.Thread.Sleep(1500);
                toolStripStatusLabel1.Text = status;
            });
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBox1.Text);
            CopyingDurationEvent();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBox2.Text);
            CopyingDurationEvent();
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBox3.Text);
            CopyingDurationEvent();
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBox4.Text);
            CopyingDurationEvent();
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBox5.Text);
            CopyingDurationEvent();
        }

        /// <summary>
        /// Make string type Dicom Tag and Element name from byte data of tag and element.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        private string GetTagName(byte[] tag)
        {
            string tmp = BitConverter.ToString(tag);
            StringBuilder sb = new StringBuilder(4);
            sb.Append(tmp[3]);
            sb.Append(tmp[4]);
            sb.Append(tmp[0]);
            sb.Append(tmp[1]);
            string tag_name = sb.ToString();

            return tag_name;
        }

        /// <summary>
        /// Make string type Dicom Tag and Element name from byte data of tag and element.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        private string GetElementName(byte[] element)
        {
            string tmp = BitConverter.ToString(element);
            StringBuilder sb = new StringBuilder(4);
            sb.Append(tmp[3]);
            sb.Append(tmp[4]);
            sb.Append(tmp[0]);
            sb.Append(tmp[1]);
            string element_name = sb.ToString();

            return element_name;
        }

        private string GetVR(byte[] vr)
        {
            var vr_name = Encoding.ASCII.GetString(vr);

            return vr_name;
        }

        /// <summary>
        /// Calc int type parameter from 2 bytes hex LittleEndian data.
        /// </summary>
        /// <param name="data">hex 2 bytes LittleEndian data</param>
        /// <returns>Compiled int type data</returns>
        private string GetLength(byte[] data)
        {
            int x4 = Convert.ToInt32(data[0].ToString(), 10) % 16;
            int x3 = Convert.ToInt32(data[0].ToString(), 10) - (x4 * 16);
            int x2 = Convert.ToInt32(data[1].ToString(), 10) % 16;
            int x1 = Convert.ToInt32(data[1].ToString(), 10) - (x2 * 16);

            x4 *= 16;
            x3 *= 1;
            x2 *= 4096;
            x1 *= 256;

            var ret = x4 + x3 + x2 + x1;

            return ret.ToString();
        }

        /// <summary>
        /// Encoding data for each VR type difinition rules.
        /// </summary>
        /// <param name="vr">VR types</param>
        /// <returns>encoded data</returns>
        private string GetValue(string vr, byte[] data)
        {
            switch (vr)
            {
                case "AE":
                    return Encoding.UTF8.GetString(data);
                case "AS":
                    return Encoding.UTF8.GetString(data);
                case "AT": // ?
                    return Encoding.UTF8.GetString(data);
                case "CS":
                    return Encoding.UTF8.GetString(data);
                case "DA":
                    return Encoding.UTF8.GetString(data);
                case "DS":
                    return Encoding.UTF8.GetString(data);
                case "DT":
                    return Encoding.UTF8.GetString(data);
                case "FL":
                    return BitConverter.ToSingle(data, 0).ToString();
                case "FD":
                    return BitConverter.ToDouble(data, 0).ToString();
                case "IS":
                    return Encoding.UTF8.GetString(data);
                case "LO":
                    return Encoding.UTF8.GetString(data);
                case "LT":
                    return Encoding.UTF8.GetString(data);
                case "OB":
                    return Encoding.UTF8.GetString(data);
                case "OD":
                    return BitConverter.ToDouble(data, 0).ToString();
                case "OF":
                    return BitConverter.ToSingle(data, 0).ToString();
                case "OW":
                    return data.ToString();
                case "PN":
                    return Encoding.UTF8.GetString(data);
                case "SH":
                    return Encoding.UTF8.GetString(data);
                case "SL": // 未実装
                    return BitConverter.ToInt64(data, 0).ToString();
                case "SQ": // ?
                    return data.ToString();
                case "SS":
                    return GetLength(data);
                case "ST":
                    return Encoding.UTF8.GetString(data);
                case "UI":
                    return Encoding.UTF8.GetString(data);
                case "UL": // data 未実装
                    return BitConverter.ToInt64(data, 0).ToString();
                case "UN": // data
                    return data.ToString();
                case "US":
                    return GetLength(data);
                case "UT":
                    return Encoding.UTF8.GetString(data);
            }

            return null;
        }
    }
}
