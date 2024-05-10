using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.CognitiveServices.Speech;

namespace Hello
{
    public partial class Form1 : Form
    {
        private CancellationTokenSource cancellationTokenSource;

        TaskCompletionSource<int> stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        public Form1()
        {
            InitializeComponent();
        }
        static string speechKey = "02eefff4efa440878779c8a6dec1690f";
        static string speechRegion = "southeastasia";

        async Task Transcribe(CancellationToken cancellationToken)
        {
            var filepath = "katiesteve.wav";
            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            speechConfig.SpeechRecognitionLanguage = "en-US";
            // Create an audio stream from a wav file or from the default microphone
            using (var audioConfig = AudioConfig.FromDefaultMicrophoneInput())
            {
                // Create a conversation transcriber using audio stream input
                using (var conversationTranscriber = new ConversationTranscriber(speechConfig, audioConfig))
                {
                    conversationTranscriber.Transcribing += (s, e) =>
                    {
                        Invoke(new Action(() =>
                        {
                            textBox1.Text += " \r\n" + e.Result.Text;
                            textBox1.SelectionStart = textBox1.Text.Length;
                            textBox1.ScrollToCaret();
                        }));
                        Console.WriteLine($"TRANSCRIBING: Text={e.Result.Text}");
                    };

                    conversationTranscriber.Transcribed += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            Console.WriteLine($"TRANSCRIBED: Text={e.Result.Text} Speaker ID={e.Result.SpeakerId}");
                            Invoke(new Action(() =>
                            {
                                textBox1.Text += $"TRANSCRIBED: Text={e.Result.Text} Speaker ID={e.Result.SpeakerId}" + " \r\n";
                                textBox1.SelectionStart = textBox1.Text.Length;
                                textBox1.ScrollToCaret();
                            }));
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            Console.WriteLine($"NOMATCH: Speech could not be transcribed.");
                        }
                    };

                    conversationTranscriber.Canceled += (s, e) =>
                    {
                        Console.WriteLine($"CANCELED: Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                            stopRecognition.TrySetResult(0);
                        }

                        stopRecognition.TrySetResult(0);
                    };

                    conversationTranscriber.SessionStopped += (s, e) =>
                    {
                        Console.WriteLine("\n    Session stopped event.");
                        stopRecognition.TrySetResult(0);
                    };

                    await conversationTranscriber.StartTranscribingAsync();

                    // Waits for completion. Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    await conversationTranscriber.StopTranscribingAsync();
                }
            }

            // Inside the method, check for cancellation periodically
            // and exit the method if cancellation has been requested
            if (cancellationToken.IsCancellationRequested)
            {

                return;
            }

            // ...
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            lblStatus.Visible = true;
            label1.Visible = true;
            button1.Enabled = false;
            lblStatus.Text = "recording";
            lblStatus.Font = new Font(lblStatus.Font.FontFamily, 15);
            lblStatus.ForeColor = Color.Green;

            cancellationTokenSource = new CancellationTokenSource();
            stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            // Pass the cancellation token to the Transcribe method
            await Task.Run(() => Transcribe(cancellationTokenSource.Token));

        }

        private void button2_Click(object sender, EventArgs e)
        {
            button1.Enabled = true;
            lblStatus.Text = "stop";
            lblStatus.Font = new Font(lblStatus.Font.FontFamily, 15);
            lblStatus.ForeColor = Color.Red;
            // Check if a transcription is currently running
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                // Request cancellation
                cancellationTokenSource.Cancel();
                // Stop the stopRecognition task
                stopRecognition.TrySetResult(0);

            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            lblStatus.Visible = false;
            label1.Visible = false;
            textBox1.ScrollBars = ScrollBars.Vertical;
        }
    }
}
