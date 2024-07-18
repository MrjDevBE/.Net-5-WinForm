using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestApp
{
    public partial class Form1 : Form
    {
        private string baseDirectory = ConfigurationManager.AppSettings["Directory1"];
        private string folderName = ConfigurationManager.AppSettings["FolderName"];
        private string folderCount = ConfigurationManager.AppSettings["FolderCount"];
        private string faxNum = ConfigurationManager.AppSettings["MaxNum"];

        private ConcurrentBag<int> _results;
        private ConcurrentBag<string> _fileNames;
        private int _total;
        private bool _isProcessing;
        private int _remainingTime = 60; // 60 seconds countdown
        private System.Diagnostics.Stopwatch stopwatch;

        private object _lock = new object();

        public Form1()
        {
            InitializeComponent();
            InitializeVariables();
            //InitializeTimer();
        }

        private void InitializeVariables()
        {
            _results = new ConcurrentBag<int>();
            _fileNames = new ConcurrentBag<string>();
            _total = 0;
            _isProcessing = false;
            stopwatch = new System.Diagnostics.Stopwatch();
        }

        //private void InitializeTimer()
        //{
        //    timer1.Interval = 1000; // Set the timer interval to 1 second
        //    timer1.Tick += timer1_Tick;
        //}

        private void button2_Click(object sender, EventArgs e)
        {
            int numberOfFiles;
            if (!int.TryParse(textBox1.Text, out numberOfFiles) || numberOfFiles <= 0)
            {
                MessageBox.Show("Please enter a valid number of files.");
                return;
            }

            string directoryPath = Path.Combine(baseDirectory, folderName);

            try
            {
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                Random random = new Random();

                for (int i = 0; i < numberOfFiles; i++)
                {
                    string fileName = random.Next(Convert.ToInt32(folderCount)).ToString(); // Generate unique file names
                    string filePath = Path.Combine(directoryPath, fileName + ".txt");
                    int content = random.Next(Convert.ToInt32(faxNum)); // More varied content
                    File.WriteAllText(filePath, content.ToString());
                }

                MessageBox.Show($"{numberOfFiles} files generated in 'Files' folder.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating files: {ex.Message}");
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (_isProcessing) return;
            _isProcessing = true;

            _results = new ConcurrentBag<int>();
            _fileNames = new ConcurrentBag<string>();
            _total = 0;
            _remainingTime = 60; // Reset countdown timer

            stopwatch.Restart(); // Start the stopwatch

            try
            {
                await Task.Run(() => ProcessFilesAsync());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing files: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop(); // Stop the stopwatch

                // Format elapsed time including milliseconds
                string elapsedTime = $"{stopwatch.Elapsed:mm\\:ss\\.fff}";

                MessageBox.Show($"Calculation complete. Elapsed Time: {elapsedTime}");

                // Update textBoxTimer with elapsed time including milliseconds
                textBoxTimer.Text = $"Elapsed Time: {elapsedTime}";

                _isProcessing = false;
            }
        }

        private void ProcessFilesAsync()
        {
            string directoryPath = Path.GetFullPath(baseDirectory);

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Files directory '{directoryPath}' does not exist.");

            string[] files = Directory.GetFiles(directoryPath);

            Parallel.ForEach(files, file => ProcessFile(file));
        }

        private void ProcessFile(string filePath)
        {
            string content = File.ReadAllText(filePath);
            if (int.TryParse(content, out int number))
            {
                _results.Add(number);
                _fileNames.Add(Path.GetFileNameWithoutExtension(filePath));
                lock (_lock)
                {
                    _total += number;
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ShowResults(intermediate: true);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ShowResults(intermediate: false);
        }

        private void ShowResults(bool intermediate)
        {
            string sortedFileNames = string.Join(";", _fileNames.OrderBy(name => int.Parse(name)));
            int totalSum = _results.Sum();
            textBox2.Text = $"{sortedFileNames}\r\nTotal: {totalSum}";
        }

        //private void timer1_Tick(object sender, EventArgs e)
        //{
        //    if (_remainingTime > 0)
        //    {
        //        _remainingTime--;
        //        textBoxTimer.Text = $"Intermediate Total: {_total} | Time left: {_remainingTime}s";
        //    }
        //    else
        //    {
        //        timer1.Stop();
        //    }
        //}
        private void textBoxTimer_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
