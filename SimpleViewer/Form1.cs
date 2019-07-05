using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace SimpleViewer
{
    public partial class Form1 : Form
    {
        string iFilename;
        Bitmap bitmap;

        public Form1()
        {
            InitializeComponent();

            iFilename = "temp.dcm";
            iFilename = @"koneko001.dcm";

            int tag_h = 0;
            int tag_l = 0;
            int ele_h = 0;
            int ele_l = 0;

            var fi = new FileInfo(iFilename);
            var fileLength = fi.Length;
            Int64 fi_count = 0;
            int i = 0;
            List<TagElementObjectData> tagele_obj = new List<TagElementObjectData>();

            List<string> TagEle = new List<string>();

            using (var fs = new FileStream(iFilename, FileMode.Open))
            {
                fs.Position = 132;
                fi_count = 132;

                while (fi_count < fileLength)
                {
                    var tag_element_name = ParseTagElementContainer(fs, ref fi_count);

                    TagEle.Add(tag_element_name);
                }
            }

            fi_count = 0;

            using (var fs = new FileStream(iFilename, FileMode.Open))
            {
                fs.Position = 132;
                fi_count = 132;

                while (fi_count < fileLength)                
                {
                    GetTagElementByte(TagEle[i], out tag_h, out tag_l, out ele_h, out ele_l);

                    var read_tag_h = fs.ReadByte();
                    var read_tag_l = fs.ReadByte();

                    fi_count += 2;

                    if (read_tag_h == tag_h && read_tag_l == tag_l)
                    {
                        var read_ele_h = fs.ReadByte();
                        var read_ele_l = fs.ReadByte();

                        fi_count += 2;

                        if (read_ele_h == ele_h && read_ele_l == ele_l)
                        {
                            TagElementObjectData obj = new TagElementObjectData();

                            obj.tag = new byte[] { (byte)tag_h, (byte)tag_l };
                            obj.element = new byte[] { (byte)ele_h, (byte)ele_l };

                            if ("7FE0,0010" == TagEle[i])
                            {
                                long start = fs.Position - 4;

                                SetPixcelsDataContainer(fs, (int)start, (int)fileLength, ref fi_count, ref obj);

                                bitmap = CreateBitmap(obj.data, 320, 320);
                            }
                            else
                            {
                                SetDataContainer(fs, ref fi_count, ref obj);

                                tagele_obj.Add(obj);
                            }

                            i++;
                        }
                    }
                }
            }

            ShowTagElementData(tagele_obj);
           
            Bitmap img = new Bitmap(1024, 1024);
            Graphics g = Graphics.FromImage(img);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
            g.DrawImage(bitmap, 0, 0, 512, 512);
            img = new Bitmap(img);

            pictureBox1.Image = img;            
        }

        /// <summary>
        /// Make string type Dicom Tag and Element name from byte data of tag and element.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        private string GetTagElementName(byte[] tag, byte[] element)
        {
            string tmp = BitConverter.ToString(tag);
            StringBuilder sb = new StringBuilder(4);
            sb.Append(tmp[3]);
            sb.Append(tmp[4]);
            sb.Append(tmp[0]);
            sb.Append(tmp[1]);
            string tag_name = sb.ToString();

            tmp = BitConverter.ToString(element);
            sb = new StringBuilder(4);
            sb.Append(tmp[3]);
            sb.Append(tmp[4]);
            sb.Append(tmp[0]);
            sb.Append(tmp[1]);
            string element_name = sb.ToString();

            string str = tag_name + "," + element_name;

            return str;
        }

        /// <summary>
        /// Create Encoded Bitmap data from Dicom file.
        /// </summary>
        /// <param name="source">'7FE0,0010' chunk pixcel image data</param>
        /// <param name="width">dicom definitioned width</param>
        /// <param name="height">dicom definitioned heigh</param>
        /// <returns></returns>
        private Bitmap CreateBitmap(byte[] source, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                                    ImageLockMode.ReadWrite, bitmap.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            byte[] rgb = new byte[width * height * 3];


            for (int i = 0; i < width * height; i++)
            {
                int value = source[2 * i] + source[2 * i + 1] * 256;
                //value >>= 4;//(bits_stored - high_bits);

                value >>= 2;

                rgb[3 * i] = (byte)value;
                rgb[3 * i + 1] = (byte)value;
                rgb[3 * i + 2] = (byte)value;
            }
            System.Runtime.InteropServices.Marshal.Copy(rgb, 0, ptr, width * height * 3);
            bitmap.UnlockBits(bmpData);
            bitmap.Save("sampleDicom.bmp");

            return bitmap;
        }

        /// <summary>
        /// Encoding data for each VR type difinition rules.
        /// </summary>
        /// <param name="vr">VR types</param>
        /// <returns>encoded data</returns>
        private object EncodeVrData(string vr, byte[] data)
        {
            switch(vr)
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
                    return BitConverter.ToSingle(data, 0);
                case "FD":
                    return BitConverter.ToDouble(data, 0);
                case "IS":
                    return Encoding.UTF8.GetString(data);
                case "LO":
                    return Encoding.UTF8.GetString(data);
                case "LT":
                    return Encoding.UTF8.GetString(data);
                case "OB":
                    return Encoding.UTF8.GetString(data);
                case "OD":
                    return BitConverter.ToDouble(data, 0);
                case "OF":
                    return BitConverter.ToSingle(data, 0);
                case "OW":
                    return data;
                case "PN":
                    return Encoding.UTF8.GetString(data);
                case "SH":
                    return Encoding.UTF8.GetString(data);
                case "SL": // 未実装
                    return BitConverter.ToInt64(data, 0);
                case "SQ": // ?
                    return data;
                case "SS":
                    return ToInt32_LittleEndian(data);
                case "ST":
                    return Encoding.UTF8.GetString(data);
                case "UI":
                    return Encoding.UTF8.GetString(data);
                case "UL": // 未実装
                    return data; //BitConverter.ToInt64(data, 0);
                case "UN":
                    return data;
                case "US":                    
                    return ToInt32_LittleEndian(data);
                case "UT":
                    return Encoding.UTF8.GetString(data);
            }

            return null;
        }

        /// <summary>
        /// Calc int type parameter from 2 bytes hex LittleEndian data.
        /// </summary>
        /// <param name="data">hex 2 bytes LittleEndian data</param>
        /// <returns>Compiled int type data</returns>
        private int ToInt32_LittleEndian(byte[] data)
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

            return ret;
        }

        /// <summary>
        /// Set pixcel data chunk '7FE0,0010' from target opened dcm FileStream.
        /// </summary>
        /// <param name="fs">target dcm file FileStream</param>
        /// <param name="start">start point of '7FEO,0010' chunk</param>
        /// <param name="end">end point of '7FEO,0010' chunk</param>
        /// <param name="count">reference for FileStream position</param>
        /// <param name="tagele_obj">reference for TagElementObjectData parameter</param>
        private void SetPixcelsDataContainer(FileStream fs, int start, int end, ref long count, ref TagElementObjectData tagele_obj)
        {
            byte[] vr = new byte[2];
            byte[] len = new byte[2];

            fs.Read(vr, 0, 2);
            fs.Read(len, 0, 2);

            count += 2;

            int size = end - start;

            byte[] data = new byte[size];

            fs.Read(data, 0, size);

            count += size;

            tagele_obj.vr = vr;
            tagele_obj.lenght = len;
            tagele_obj.data = data;
        }

        /// <summary>
        /// Specific function for parse Dicom Tag and Element.
        /// </summary>
        /// <param name="fs">target dcm file FileStream</param>
        /// <param name="count">reference for FileStream position</param>
        /// <returns>Discovered Dicom Tag and Element name like '0028,0010'</returns>
        private string ParseTagElementContainer(FileStream fs, ref Int64 count)
        {
            Int64 i = count;

            byte[] tag = new byte[2];
            byte[] element = new byte[2];

            fs.Read(tag, 0, 2);
            fs.Read(element, 0, 2);

            i += 4;

            byte[] vr = new byte[2];
            byte[] len = new byte[2];
            byte[] blanc = new byte[2];

            fs.Read(vr, 0, 2);
            fs.Read(len, 0, 2);

            i += 4;

            int size = ToInt32_LittleEndian(len);

            if (0 == size)
            {
                fs.Read(len, 0, 2);
                fs.Read(blanc, 0, 2);

                i += 4;

                if (blanc[0] == 0x00 && blanc[1] == 0x00)
                {
                    size = ToInt32_LittleEndian(len);
                }
                else
                {
                    fs.Position -= 4;
                    return GetTagElementName(tag, element);
                }
            }

            byte[] data = new byte[size];

            fs.Read(data, 0, size);

            i += size;

            count = i;

            return GetTagElementName(tag, element);
        }

        /// <summary>
        /// Set data indicated Dicom Tag and Element read from FileStream
        /// </summary>
        /// <param name="fs">target dcm file FileStream</param>
        /// <param name="count">reference for FileStream position</param>
        /// <param name="tagele_obj">reference for TagElementObjectData parameter</param>
        private void SetDataContainer(FileStream fs, ref Int64 count, ref TagElementObjectData tagele_obj)
        {
            Int64 i = count;

            byte[] vr = new byte[2];
            byte[] len = new byte[2];
            byte[] blanc = new byte[2];

            fs.Read(vr, 0, 2);
            fs.Read(len, 0, 2);

            i += 4;

            int size = ToInt32_LittleEndian(len);

            if (0 == size)
            {
                fs.Read(len, 0, 2);
                fs.Read(blanc, 0, 2);

                i += 4;

                if (blanc[0] == 0x00 && blanc[1] == 0x00)
                {
                    size = ToInt32_LittleEndian(len);
                }
                else
                {
                    fs.Position -= 4;
                    return;
                }
            }

            byte[] data = new byte[size];

            fs.Read(data, 0, size);

            i += size;

            count = i;

            tagele_obj.vr = vr;
            tagele_obj.lenght = len;
            tagele_obj.data = data;
        }

        /// <summary>
        /// Display Dicom Tag and Element Information for datagridview1 on Form1
        /// </summary>
        /// <param name="tagele_obj">Dicom Tag and Element data class like tag, element, vr, len, data etc...</param>
        private void ShowTagElementData(List<TagElementObjectData> tagele_obj)
        {
            foreach (var s in tagele_obj)
            {
                // VR を見て、データが数値なのかテキストなのかを判断する必要がある。

                string tmp = BitConverter.ToString(s.tag);
                StringBuilder sb = new StringBuilder(4);
                sb.Append(tmp[3]);
                sb.Append(tmp[4]);
                sb.Append(tmp[0]);
                sb.Append(tmp[1]);
                string tag_name = sb.ToString();

                tmp = BitConverter.ToString(s.element);
                sb = new StringBuilder(4);
                sb.Append(tmp[3]);
                sb.Append(tmp[4]);
                sb.Append(tmp[0]);
                sb.Append(tmp[1]);
                string element_name = sb.ToString();

                string vr = Encoding.ASCII.GetString(s.vr);

                int length = ToInt32_LittleEndian(s.lenght);

                string str = "(" + tag_name + "," + element_name + ")";

                var data = EncodeVrData(vr, s.data);

                dataGridView1.Rows.Add(str, data);
            }
        }

        /// <summary>
        /// Convert int type byte parameter from string type Dicom TagElement like "0028,0010".
        /// </summary>
        /// <param name="TagElement">input Dicom Tag and Element like '0028,0010'</param>
        /// <param name="tag_h">output Dicom Tag high bit</param>
        /// <param name="tag_l">output Dicom Tag low bit</param>
        /// <param name="ele_h">output Dicom Element high bit</param>
        /// <param name="ele_l">output Dicom Element low bit</param>
        private void GetTagElementByte(string TagElement, out int tag_h, out int tag_l, out int ele_h, out int ele_l)
        {
            StringBuilder hex = new StringBuilder(4);

            hex.Append("0");
            hex.Append("x");
            hex.Append(TagElement[2]);
            hex.Append(TagElement[3]);

            tag_h = Convert.ToInt32(hex.ToString(), 16);

            hex = new StringBuilder(4);

            hex.Append("0");
            hex.Append("x");
            hex.Append(TagElement[0]);
            hex.Append(TagElement[1]);

            tag_l = Convert.ToInt32(hex.ToString(), 16);

            hex = new StringBuilder(4);

            hex.Append("0");
            hex.Append("x");
            hex.Append(TagElement[7]);
            hex.Append(TagElement[8]);

            ele_h = Convert.ToInt32(hex.ToString(), 16);

            hex = new StringBuilder(4);

            hex.Append("0");
            hex.Append("x");
            hex.Append(TagElement[5]);
            hex.Append(TagElement[6]);

            ele_l = Convert.ToInt32(hex.ToString(), 16);
        }
    }
}
