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
        // input parameters
        string iFilename;
        string intermediateFile = "tmp_ImplicitLittleEndian.dcm";

        // output parameters
        int width;
        int height;
        Bitmap bitmap;
        List<TagElementObjectData> tagele_obj = new List<TagElementObjectData>();
        TagElementObjectData obj_data;
        double whitebalance;

        // dicom tag element parameters
        double window_center;
        double window_width;
        string photometric_interpretation;
        int planar_configuration;

        public Form1()
        {
            InitializeComponent();
        }

        private void ExecuteCommnadDisplayAndParsing()
        {
            ConvertJpegToDICOMFile(iFilename, intermediateFile);

            iFilename = intermediateFile;

            whitebalance = 0;

            int tag_h = 0;
            int tag_l = 0;
            int ele_h = 0;
            int ele_l = 0;

            var fi = new FileInfo(iFilename);
            var fileLength = fi.Length;
            Int64 fi_count = 0;
            int i = 0;

            List<string> TagEle = new List<string>();

            dataGridView1.DoubleClick += DataGridView1_DoubleClick;

            pictureBox1.MouseWheel += PictureBox1_MouseWheel;

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

                                if (photometric_interpretation == "RGB ")
                                {
                                    SetPixcelsDataContainerRGB(fs, (int)start, (int)fileLength, ref fi_count, ref obj);

                                    obj_data = obj;

                                    bitmap = CreateBitmapRGB(obj.value, width, height);

                                    if (planar_configuration == 1)
                                        DrawPictureBoxRGBPlanar_One(obj.value, width, height);
                                    else if (planar_configuration == 0)
                                        DrawPictureBoxRGBPlanar_Zero(obj.value, width, height);
                                }
                                else
                                {
                                    SetPixcelsDataContainer(fs, (int)start, (int)fileLength, ref fi_count, ref obj);

                                    obj_data = obj;

                                    bitmap = CreateBitmap(obj.value, width, height, window_center, window_width);

                                    DrawPictureBox(obj.value, width, height, window_center, window_width);
                                }
                            }
                            else
                            {
                                SetDataContainer(fs, ref fi_count, ref obj);

                                tagele_obj.Add(obj);

                                if ("0028,0004" == TagEle[i])
                                    photometric_interpretation = GetPhotometoricInterpretation(obj);

                                if ("0028,0006" == TagEle[i])
                                    planar_configuration = GetPlanarConfiguration(obj);

                                if ("0028,0010" == TagEle[i])
                                    height = GetWidthFrom00280010(obj);

                                if ("0028,0011" == TagEle[i])
                                    width = GetHeightFrom00280011(obj);

                                if ("0028,1050" == TagEle[i])
                                {
                                    window_center = double.Parse(Encoding.UTF8.GetString(obj.value));
                                }

                                if ("0028,1051" == TagEle[i])
                                {
                                    window_width = double.Parse(Encoding.UTF8.GetString(obj.value));
                                }
                            }

                            i++;
                        }
                    }
                }
            }

            ShowTagElementData(tagele_obj);

            DisplayPictureboxImageData();

            File.Delete(intermediateFile);

        }

        /// <summary>
        /// Function for mouse wheel scroll. It changes the whitebalance to display.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            whitebalance += (double)(e.Delta / 120) / 10;

            bitmap = CreateBitmapTuningLUT(obj_data.value, width, height, window_center, window_width);

            DisplayPictureboxImageData();
        }

        /// <summary>
        /// It shows bitmap image to picturebox.
        /// </summary>
        private void DisplayPictureboxImageData()
        {
            Bitmap img = new Bitmap(1024, 1024);
            Graphics g = Graphics.FromImage(img);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
            g.DrawImage(bitmap, 0, 0, 512, 512);
            img = new Bitmap(img);

            pictureBox1.Image = img;
        }

        /// <summary>
        /// It gets Photometoric parameter like RGB or MONOCHROME2.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        string GetPhotometoricInterpretation(TagElementObjectData obj)
        {
            return Encoding.ASCII.GetString(obj.value);
        }

        /// <summary>
        /// It gets Planar configuration parameters 1 or 0
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private int GetPlanarConfiguration(TagElementObjectData obj)
        {
            return ToInt32_LittleEndian(obj.value);
        }

        /// <summary>
        /// It gets width of images.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private int GetWidthFrom00280010(TagElementObjectData obj)
        {
            return ToInt32_LittleEndian(obj.value);
        }

        /// <summary>
        /// It gets height of images.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private int GetHeightFrom00280011(TagElementObjectData obj)
        {
            return ToInt32_LittleEndian(obj.value);
        }

        /// <summary>
        /// It shows detail information for dicom tag element then first datagridview column.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridView1_DoubleClick(object sender, EventArgs e)
        {
            var row = dataGridView1.CurrentCell.RowIndex;

            var form2 = new Form2(tagele_obj, row);

            form2.ShowDialog();

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
        private Bitmap CreateBitmap(byte[] source, int width, int height, double window_center, double window_width)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                                    ImageLockMode.ReadWrite, bitmap.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            byte[] rgb = new byte[width * height * 3];


            for (int i = 0; i < width * height; i++)
            {
                int value = source[2 * i] + source[2 * i + 1] * 256;

                double val = value;

                if (value >= window_width)
                {
                    val = 255;
                }
                else if (value < 0)
                {
                    val = 0;
                }
                else
                {
                    val = val / window_width;
                    val = val * 256;
                }

                value = (int)val;

                rgb[3 * i] = (byte)value;
                rgb[3 * i + 1] = (byte)value;
                rgb[3 * i + 2] = (byte)value;
            }

            System.Runtime.InteropServices.Marshal.Copy(rgb, 0, ptr, width * height * 3);
            bitmap.UnlockBits(bmpData);

            
            return bitmap;
        }

        /// <summary>
        /// For type of RGB, Create Encoded Bitmap data from Dicom file.
        /// </summary>
        /// <param name="source">'7FE0,0010' chunk pixcel image data</param>
        /// <param name="width">dicom definitioned width</param>
        /// <param name="height">dicom definitioned heigh</param>
        /// <returns></returns>
        private Bitmap CreateBitmapRGB(byte[] source, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                                    ImageLockMode.WriteOnly, bitmap.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            byte[] rgb = new byte[width * height * 3];

            int j = 0;

            for (int i = 0; i < width * height; i++)
            {
                rgb[j] = source[i + (width * height) * 2];
                rgb[++j] = source[i + width * height];
                rgb[++j] = source[i];

                j++;
            }

            System.Runtime.InteropServices.Marshal.Copy(rgb, 0, ptr, width * height * 3);
            bitmap.UnlockBits(bmpData);

            return bitmap;
        }

        /// <summary>
        /// Function for mouse wheel scroll. It tunes whitebalance according to mouse control.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="window_center"></param>
        /// <param name="window_width"></param>
        /// <returns></returns>
        private Bitmap CreateBitmapTuningLUT(byte[] source, int width, int height, double window_center, double window_width)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                                    ImageLockMode.ReadWrite, bitmap.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            byte[] rgb = new byte[width * height * 3];


            for (int i = 0; i < width * height; i++)
            {
                int value = source[2 * i] + source[2 * i + 1] * 256;

                value >>= (2 + (int)whitebalance);

                rgb[3 * i] = (byte)value;
                rgb[3 * i + 1] = (byte)value;
                rgb[3 * i + 2] = (byte)value;
            }

            System.Runtime.InteropServices.Marshal.Copy(rgb, 0, ptr, width * height * 3);
            bitmap.UnlockBits(bmpData);


            return bitmap;
        }

        /// <summary>
        /// Display Encoded Bitmap data from Dicom file.
        /// </summary>
        /// <param name="source">'7FE0,0010' chunk pixcel image data</param>
        /// <param name="width">dicom definitioned width</param>
        /// <param name="height">dicom definitioned heigh</param>
        /// <returns></returns>
        private void DrawPictureBox(byte[] source, int width, int height, double window_center, double window_width)
        {
            Form4 f4 = new Form4(width, height);

            byte[] rgb = new byte[width * height * 3];

            for (int i = 0; i < width * height; i++)
            {
                int value = source[2 * i] + source[2 * i + 1] * 256;

                double val = value;

                if (value >= window_width)
                {
                    val = 255;
                }
                else if (value < 0)
                {
                    val = 0;
                }
                else
                {
                    val = val / window_width;
                    val = val * 256;
                }
                value = (int)val;

                rgb[3 * i] = (byte)value;
                rgb[3 * i + 1] = (byte)value;
                rgb[3 * i + 2] = (byte)value;

                int R = rgb[3 * i];
                int G = rgb[3 * i + 1];
                int B = rgb[3 * i + 2];

                Color color = Color.FromArgb(R, G, B);

                f4.image.SetPixel(i % width, (int)(i / width), color);
            }

            f4.ShowDialog();
        }

        /// <summary>
        /// For planar configuration "On", Display Encoded Bitmap data from Dicom file.
        /// </summary>
        /// <param name="source">'7FE0,0010' chunk pixcel image data</param>
        /// <param name="width">dicom definitioned width</param>
        /// <param name="height">dicom definitioned heigh</param>
        /// <returns></returns>
        private void DrawPictureBoxRGBPlanar_One(byte[] source, int width, int height)
        {
            Form4 f4 = new Form4(width, height);

            for (int i = 0; i < width * height; i++)
            {
                int r = source[i];
                int g = source[i + width * height];
                int b = source[i + (width * height) * 2];

                Color color = Color.FromArgb(255, r, g, b);
                f4.image.SetPixel(i % width, (int)(i / width), color);
            }

            f4.ShowDialog();
        }

        /// <summary>
        /// For planar configuration "Off", Display Encoded Bitmap data from Dicom file.
        /// </summary>
        /// <param name="source">'7FE0,0010' chunk pixcel image data</param>
        /// <param name="width">dicom definitioned width</param>
        /// <param name="height">dicom definitioned heigh</param>
        /// <returns></returns>
        private void DrawPictureBoxRGBPlanar_Zero(byte[] source, int width, int height)
        {
            Form4 f4 = new Form4(width, height);

            Color[] rgb = new Color[width * height];

            int j = 0;

            for (int i = 0; i < source.Length; i += 3)
            {
                int R = source[i];
                int G = source[i + 1];
                int B = source[i + 2];

                rgb[j] = Color.FromArgb(255, R, G, B);

                j += 1;
            }

            for (int i = 0; i < rgb.Length; i++)
            {
                f4.image.SetPixel(i % width, (int)(i / width), rgb[i]);
            }

            f4.ShowDialog();
        }

        /// <summary>
        /// Encoding data for each VR type difinition rules.
        /// </summary>
        /// <param name="vr">VR types</param>
        /// <returns>encoded data</returns>
        private object EncodeVrData(string vr, byte[] data)
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
                    return data; 
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
            tagele_obj.length = len;
            tagele_obj.value = data;
        }

        /// <summary>
        /// For type of RGB, Set pixcel data chunk '7FE0,0010' from target opened dcm FileStream.
        /// </summary>
        /// <param name="fs">target dcm file FileStream</param>
        /// <param name="start">start point of '7FEO,0010' chunk</param>
        /// <param name="end">end point of '7FEO,0010' chunk</param>
        /// <param name="count">reference for FileStream position</param>
        /// <param name="tagele_obj">reference for TagElementObjectData parameter</param>
        private void SetPixcelsDataContainerRGB(FileStream fs, int start, int end, ref long count, ref TagElementObjectData tagele_obj)
        {
            byte[] vr = new byte[2];
            byte[] len = new byte[4];

            fs.Read(vr, 0, 2);

            fs.Position += 2;

            fs.Read(len, 0, 4);

            count += 4;

            int size = end - start;

            byte[] data = new byte[size];

            fs.Read(data, 0, size);

            count += size;

            tagele_obj.vr = vr;
            tagele_obj.length = len;
            tagele_obj.value = data;
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
            tagele_obj.length = len;
            tagele_obj.value = data;
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

                int length = ToInt32_LittleEndian(s.length);

                string str = "(" + tag_name + "," + element_name + ")";

                var data = EncodeVrData(vr, s.value);

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

        private void ConvertJpegToDICOMFile(string in_filename, string out_filename)
        {
            string arg = string.Format(@"/c dcmdjpeg +te {0} {1}", in_filename, out_filename);

            //Processオブジェクトを作成
            System.Diagnostics.Process p = new System.Diagnostics.Process();

            //ComSpec(cmd.exe)のパスを取得して、FileNameプロパティに指定
            p.StartInfo.FileName = System.Environment.GetEnvironmentVariable("ComSpec");
            //出力を読み取れるようにする
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = false;
            //ウィンドウを表示しないようにする
            p.StartInfo.CreateNoWindow = true;
            //コマンドラインを指定（"/c"は実行後閉じるために必要）
            p.StartInfo.Arguments = arg;

            //起動
            p.Start();

            //出力を読み取る
            string results = p.StandardOutput.ReadToEnd();

            //プロセス終了まで待機する
            //WaitForExitはReadToEndの後である必要がある
            //(親プロセス、子プロセスでブロック防止のため)
            p.WaitForExit();
            p.Close();
        }
        private void ToolStripTextBox1_Click(object sender, EventArgs e)
        {
            var result = openFileDialog1.ShowDialog();

            if(DialogResult.OK == result)
            {
                iFilename = openFileDialog1.FileName;

                ExecuteCommnadDisplayAndParsing();
            }
        }

        private void ToolStripTextBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void ContactUsホームページToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var f3 = new Form3();

            f3.ShowDialog();
        }

        private void Help説明ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/konekomaro/SimpleViewer");
        }
    }
}


/*
if (value >= window_width)
{
    val = 255;
}
else if (value < 0)
{
    val = 0;
}
else if (value < window_center)
{
    val = val / window_center;
    val = 127 * val;
}
else if (value >= window_center)
{
    double tmp = (window_width - window_center) / window_width;
    val = 127 + (val - window_center) * tmp;
}
*/
