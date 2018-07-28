using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Imaging.Filters;
using FeatureExtraction;
using System.IO;
using SVM;

namespace DSATM_OCR
{
    public partial class Form1 : Form
    {
        Bitmap originalImage;
        Model model;
        public Form1()
        {
            InitializeComponent();
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            if(openDialog.ShowDialog() == DialogResult.OK)
            {
                originalImage = new Bitmap(openDialog.FileName);
                pictureBox1.Image = originalImage;
            }
        }

        private Bitmap preProcess(Bitmap originalImage)
        {
            Invert invertObj = new Invert();
            Bitmap invertImage = invertObj.Apply((Bitmap)originalImage.Clone());

            pictureBox2.Image = invertImage;

            invertImage = Grayscale.CommonAlgorithms.BT709.Apply(invertImage);
            Threshold bwObject = new Threshold();
            invertImage = bwObject.Apply(invertImage);
            ExtractBiggestBlob blobObject = new ExtractBiggestBlob();
            invertImage = blobObject.Apply(invertImage);

            pictureBox2.Image = invertImage;

            ResizeBicubic resize = new ResizeBicubic(60, 90);

            invertImage = resize.Apply(invertImage);

            /*CannyEdgeDetector edgeDetector = new CannyEdgeDetector();
            invertImage = edgeDetector.Apply(invertImage);*/

            return invertImage;
        }

        private void WriteToFile(List<float[]> features, int label, ref StreamWriter sw)
        {


            foreach (var featureVector in features)
            {
                sw.Write(label + " ");
                for (int index = 0; index < featureVector.Length; index++)
                {
                    sw.Write((index + 1) + ":" + featureVector[index] + " ");
                }
                sw.WriteLine();
            }



            sw.Flush();

        }

        private void button1_Click(object sender, EventArgs e)
        {

            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                string folderPath = folderDialog.SelectedPath;

                HOEF hoefObj = new HOEF();

                FileStream fs = new FileStream("Train", FileMode.Create, FileAccess.Write);

                StreamWriter sw = new StreamWriter(fs);

                for(int index = 0;index<4;index++)
                {
                    char subFolder = (char)('a' + index);
                    string finalFolder = folderPath + "\\" + subFolder;

                    string[] allfiles = Directory.GetFiles(finalFolder);

                    List<float[]> features = new List<float[]>();

                    for (int i = 0; i < allfiles.Length; i++)
                    {
                       
                            Bitmap img = new Bitmap(allfiles[i]);
                            img = preProcess((Bitmap)img.Clone());

                        
                        float[] featureVector = hoefObj.Apply(img);
                        features.Add(featureVector);


                    }

                    WriteToFile(features, index, ref sw);
                    
                }
                
                sw.Close();
                fs.Close();
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Problem train = Problem.Read("Train");
            Parameter parameters = new Parameter();
            parameters.C = 32; parameters.Gamma = 8;
            model = Training.Train(train, parameters);
            MessageBox.Show("model is trained");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            if(openDialog.ShowDialog()== DialogResult.OK)
            {
                Bitmap img = new Bitmap(openDialog.FileName);
                pictureBox1.Image = img;
                img = preProcess((Bitmap)img.Clone());

                HOEF hoefObj = new HOEF();
                float[] featureVector = hoefObj.Apply(img);

                List<float[]> features = new List<float[]>();
                features.Add(featureVector);


                FileStream fs = new FileStream("Test", FileMode.Create, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
               


                WriteToFile(features, 50, ref sw);
                sw.Flush();
                sw.Close();
                fs.Close();

                Problem test = Problem.Read("Test");
                Prediction.Predict(test, "result", model, false);

                FileStream fsRead = new FileStream("result", FileMode.Open, FileAccess.Read);

                StreamReader sr = new StreamReader(fsRead);
                string result = sr.ReadLine();

                sr.Close();
                fsRead.Close();

                int iResult = Int32.Parse(result);


                char[] lookuptable = { 'A', 'B', 'C', 'D' };
                string output = lookuptable[iResult].ToString();
                label1.Text = output.ToString();
            }
        }
    }
}
