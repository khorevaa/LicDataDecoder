using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LicDataDecoder
{
    public partial class Form1 : Form
    {
        //Наличие необходимых компонентов в системе
        bool JRE = false;
        bool RING = false;
        bool LICENSE = false;

        string path; //путь к файлу
        string fileName; //имя файла
        string folderName; //путь к папке с файлом

        public Form1()
        {
            InitializeComponent();

            this.Width = 500;
            button1.Enabled = true;
            //checkAbilityAcync();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel) return;
            textBox1.Text = openFileDialog1.FileName;
            textBox2.Text = "Подождите...";

            try
            {
                setFileNameAndPath();

                string[] results = await Task.Factory.StartNew<string[]>(
                                             () => decodeLicenceFile(),
                                             TaskCreationOptions.LongRunning);

                textBox2.Text = results[0];
                if (ExternalMode.Checked)
                {
                    textBox3.Text = results[1];
                    textBox4.Text = results[2];
                }

            }
            catch
            {
                textBox2.Text = "Выбранный файл не является лицензией или поврежден.";
            }
        }

        private void setFileNameAndPath()
        {
            path = textBox1.Text;
            fileName = path.Substring(path.LastIndexOf(@"\") + 1, path.Length - path.LastIndexOf('\\') - 1);
            folderName = path.Substring(0, path.Length - fileName.Length - 1);
        }

        private string[] decodeLicenceFile()
        {
            string licName = getLicName();
            string pinCode = licName.Substring(0, 15);

            string[] results = new string[3];

            string result = "";

            result += getLicData(licName) + System.Environment.NewLine;
            result += "--------------------------------------------------------------------------------------";
            result += "Текущий пин-код: " + buildPinCode(pinCode);
            result += "--------------------------------------------------------------------------------------";
            result += System.Environment.NewLine + System.Environment.NewLine;
            result += getValidateData(licName);

            results[0] = result;

            //расширенный режим
            if (ExternalMode.Checked)
            {

                string debugInfo = getDebugInfo(licName);

                string[] debugMessages = debugInfo.Split(new string[] { "[DEBUG ] com._1c.license.activator.crypt.Converter - getLicensePermitFromBase64 : Request : Computer info : \r\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string message in debugMessages)
                {
                    if (message.Contains("pin : " + pinCode))
                    {
                        int end = message.IndexOf("Customer info :");
                        string licHWConfig = message.Substring(0, end);
                        results[1] = "Параметры компьютера, получившего лицензию. Не все из них являются ключевыми." + System.Environment.NewLine + System.Environment.NewLine + licHWConfig;
                    }
                }

                debugMessages = debugInfo.Split(new string[] { "[DEBUG ] com._1c.license.activator.hard.HardInfo - computer info : " }, StringSplitOptions.RemoveEmptyEntries);
                string HWConfig = debugMessages[1].Substring(0, debugMessages[1].IndexOf("\r\n\r\n"));
                results[2] = "Параметры этого компьютера, которые могли бы записаться в лицензию" + System.Environment.NewLine + System.Environment.NewLine + HWConfig;



            }

            return results;




        } //фоновый метод декодирования файла лицензии

        private string getDebugInfo(string licName)
        {
            string debugInfo = "";

            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/C ring -l \"debug\" license validate --name " + licName + " --path \"" + folderName + "\"" + " --send-statistics \"false\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            StreamReader reader = process.StandardOutput;
            debugInfo = reader.ReadToEnd();

            return debugInfo;
        }

        private string getLicName()
        {
            string licName = "";

            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/C ring license list --path \"" + folderName + "\"" + " --send-statistics \"false\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            StreamReader reader = process.StandardOutput;
            string output = reader.ReadToEnd();

            //Т.к. команда list выдает список всех лицензий в папке с указанным файлом, то, 
            //чтобы получить внутреннее название именно указанного файла, нужно разбить получившийся
            //список на строки и найти в нем строку, содержащую название указанного файла, а потом обрезать её начало.

            string[] stringsArray = output.Split(Environment.NewLine.ToCharArray());
            foreach (string str in stringsArray)
            {
                if (str.EndsWith(fileName + "\")"))
                {

                    int indexOfChar = str.IndexOf('('); // равно 4
                    licName = str.Substring(0, indexOfChar);
                    return licName;
                }
            }

            return licName;
        } //получить внутреннее название лицензии

        private string getLicData(string licName)
        {
            string licData = "";

            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/C ring license info --name " + licName + " --path \"" + folderName + "\"" + " --send-statistics \"false\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            StreamReader reader = process.StandardOutput;
            licData = reader.ReadToEnd();

            return licData;
        } //получить ликдату

        private string getValidateData(string licName)
        {
            string validateData = "";

            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/C ring license validate --name " + licName + " --path \"" + folderName + "\"" + " --send-statistics \"false\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            StreamReader reader = process.StandardOutput;
            validateData = reader.ReadToEnd();


            string str = "Проверка лицензии завершилась с ошибкой.\r\nПо причине: ";
            if (validateData.Contains(str))
            {
                validateData = validateData.Remove(validateData.IndexOf(str), str.Length);
                validateData = "Ключевые параметры компьютера не соответствуют лицензии." + System.Environment.NewLine
                  + "Для получения полного списка оборудования включите подробный режим." + System.Environment.NewLine
                  + System.Environment.NewLine + validateData;
            }


            return validateData;
        } //получить информацию о железе компьютера

        private StringBuilder buildPinCode(string pinCode)
        {
            StringBuilder extPinCode = new StringBuilder(pinCode.Length + 4);

            List<int> dashPositionsList = new List<int>() { 4, 7, 10, 13 };
            for (int i = 0; i < pinCode.Length; i++)
            {
                if (dashPositionsList.Contains(i + 1))
                {
                    extPinCode.Append("-");
                    extPinCode.Append(pinCode[i]);
                }
                else
                {
                    extPinCode.Append(pinCode[i]);
                }
            }

            return extPinCode;
        } //построитель пин-кода с тире

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 aboutForm = new Form2();
            aboutForm.ShowDialog();
        }

        private async void checkAbilityAcync()
        {

            textBox2.Text += Environment.NewLine + Environment.NewLine + "Выполняется проверка возможности декодирования лицензий...";

            JRE = await Task.Factory.StartNew<bool>(
                                       () => checkJRE(),
                                       TaskCreationOptions.LongRunning);

            RING = await Task.Factory.StartNew<bool>(
                                     () => checkRING(),
                                     TaskCreationOptions.LongRunning);

            LICENSE = await Task.Factory.StartNew<bool>(
                                      () => checkLICENSE(),
                                      TaskCreationOptions.LongRunning);

            if (!JRE) textBox2.Text += Environment.NewLine + Environment.NewLine + "В системе отсутствует JRE. \nСкачать: https://www.oracle.com/technetwork/java/javase/downloads/index.html";
            if (!RING) textBox2.Text += Environment.NewLine + Environment.NewLine + "В системе отсутствует утилита RING. Утилита поставляется в комплекте с дистрибутивом технологической платформы в папке \"license-tools\". Запустите файл 1ce-installer.cmd с правами администратора для установки.";
            if (!LICENSE) textBox2.Text += Environment.NewLine + Environment.NewLine + "В системе отсутствует модуль LICENSE. Модуль поставляется в комплекте с дистрибутивом технологической платформы в папке \"license-tools\". Запустите файл 1ce-installer.cmd с правами администратора  для установки.";

            if (JRE & RING & LICENSE)
            {
                textBox2.Text += Environment.NewLine + "Программа готова к работе!";
                button1.Enabled = true;
            }

        }

        private bool checkJRE()
        {

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "java";
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                return true;

                //StreamReader reader = process.StandardError;
                //string output = reader.ReadToEnd();
                //return output == "";

                // java дублирует информацию из стандартного потока вывода в стандартный поток ошибок, поэтому он никогда не будет пустым.

            }
            catch (Exception e)
            {
                return false;
            }

        }

        private bool checkRING()
        {

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/C ring";
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                StreamReader reader = process.StandardError;
                string output = reader.ReadToEnd();

                return output == "";

            }
            catch (Exception e)
            {
                return false;
            }

        }

        private bool checkLICENSE()
        {

            try
            {
                if (!RING) return false;
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/C ring license";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                StreamReader reader = process.StandardOutput;
                string output = reader.ReadToEnd();

                return (!output.Contains("ERROR"));

            }
            catch (Exception e)
            {
                return false;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Rectangle r = Screen.FromControl(this).WorkingArea;

            if (this.Width == 500)
            {
                for (int i = 500; i <= 1410; i += 10)
                {
                    this.Width = i;
                    this.Bounds = new Rectangle((r.Width - this.Width) / 2, (r.Height - this.Height) / 2, this.Width, this.Height);                                      
                }
                System.Threading.Thread.Sleep(1);
            }
            else
            {
                for (int i = 1410; i >= 500; i -= 10)
                {
                    this.Width = i;
                    this.Bounds = new Rectangle((r.Width - this.Width) / 2, (r.Height - this.Height) / 2, this.Width, this.Height);
                    System.Threading.Thread.Sleep(1);
                    
                }
            }


            //Без анимации

            //if (this.Width == 500)
            //{
            //    this.Width = 1410;
            //}
            //else
            //{
            //    this.Width = 500;
            //}
            //Rectangle r = Screen.FromControl(this).WorkingArea;
            //this.Bounds = new Rectangle((r.Width-this.Width)/2, (r.Height-this.Height)/2, this.Width, this.Height);

        }
    }
}