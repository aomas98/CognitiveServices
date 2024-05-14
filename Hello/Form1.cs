using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.CognitiveServices.Speech;
using Azure;
using Azure.AI.TextAnalytics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Hello
{

    public partial class Form1 : Form
    {
        private CancellationTokenSource cancellationTokenSource; // code to cancel 
        TaskCompletionSource<int> stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Dictionary to store sentiment scores by speaker
        private Dictionary<string, List<DocumentSentiment>> speakerSentiments = new Dictionary<string, List<DocumentSentiment>>();
        private Dictionary<string, Sentiment> AggregateSpeakerSentiments = new Dictionary<string, Sentiment>();
        public Form1()
        {
            InitializeComponent();
        }

        static string speechKey = "02eefff4efa440878779c8a6dec1690f";
        static string speechRegion = "southeastasia";

        // Added key for text analytics start
        private static readonly AzureKeyCredential textAnalyticsCredentials = new AzureKeyCredential("33e902f506ed4c1f865a7e3df472dab2");
        private static readonly Uri textAnalyticsEndpoint = new Uri("https://crap-textanalytics.cognitiveservices.azure.com/");
        // Added key for text analytics end

        async Task Transcribe(CancellationToken cancellationToken)
        {
            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            speechConfig.SpeechRecognitionLanguage = "en-US";

            using (var audioConfig = AudioConfig.FromDefaultMicrophoneInput()) // gets from default microphone , add code to add other devices
            {
                using (var conversationTranscriber = new ConversationTranscriber(speechConfig, audioConfig))
                {
                    conversationTranscriber.Transcribing += (s, e) =>
                    {
                        Invoke(new Action(() =>
                        {
                            // textBox1.Text += " \r\n" + e.Result.Text;
                            // textBox1.SelectionStart = textBox1.Text.Length;
                            // textBox1.ScrollToCaret();
                        }));
                        // Console.WriteLine($"TRANSCRIBING: Text={e.Result.Text}"); // to be removed
                    };

                    conversationTranscriber.Transcribed += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            Console.WriteLine($"TRANSCRIBED: Text={e.Result.Text} Speaker ID={e.Result.SpeakerId}"); //Transcribed

                            Invoke(new Action(() =>
                            {
                                textBox1.Text += $"TRANSCRIBED: Text={e.Result.Text} Speaker ID={e.Result.SpeakerId}" + " \r\n";
                                textBox1.SelectionStart = textBox1.Text.Length;
                                textBox1.ScrollToCaret();
                            }));

                            // Analyze transcribed text with Text Analytics and store the sentiment
                            AnalyzeTextWithTextAnalytics(e.Result.Text, e.Result.SpeakerId);
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

        private void AnalyzeTextWithTextAnalytics(string text, string speakerId)
        {
            // Initialize Text Analytics client
            var client = new TextAnalyticsClient(textAnalyticsEndpoint, textAnalyticsCredentials);

            // Sentiment analysis
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(speakerId))
            {
                DocumentSentiment documentSentiment = client.AnalyzeSentiment(text);
                // Store the sentiment by speaker
                if (!speakerSentiments.ContainsKey(speakerId))
                {
                    speakerSentiments[speakerId] = new List<DocumentSentiment>();
                }
                speakerSentiments[speakerId].Add(documentSentiment);

                // Display the sentiment analysis
                DisplaySentimentAnalysis(speakerId);

                // Perform additional text analysis and display results
                DisplayLanguageDetection(client, text);
                DisplayEntityRecognition(client, text);
                DisplayEntityLinking(client, text);
                DisplayKeyPhraseExtraction(client, text);

            }



        }
        List<System.Windows.Forms.ProgressBar> progressBars = new List<System.Windows.Forms.ProgressBar>();
        private void AddProgressBar(string Key)
        {
            var SentimentsProgressBar = new List<SentimentHdr>();
            foreach (var item in AggregateSpeakerSentiments)
            {
                SentimentsProgressBar.Add(new SentimentHdr()
                {
                    SpeakerId = item.Key,
                    Sentiment = new List<Sentiments>()
                    {
                        new Sentiments() { Key = "AveragePositive", Value = item.Value.AveragePositive },
                        new Sentiments() { Key = "AverageNegative", Value = item.Value.AverageNegative },
                        new Sentiments() { Key = "AverageNeutral", Value = item.Value.AverageNeutral }
                    }
                });

            }


            foreach (var Sentiments in SentimentsProgressBar)
            {
                foreach (var sentiment in Sentiments.Sentiment)
                {
                    if (progressBars.Any(x => x.Name == Sentiments.SpeakerId + sentiment.Key))
                    {
                        var ProgressBar = progressBars.First(x => x.Name == Sentiments.SpeakerId + sentiment.Key);
                        ProgressBar.Value = (int)(sentiment.Value * 100);
                    }
                    else
                    {
                        System.Windows.Forms.ProgressBar progressBar1 = new System.Windows.Forms.ProgressBar();
                        progressBar1.Name = Sentiments.SpeakerId + sentiment.Key;
                        progressBar1.Width = flowLayoutPanel1.ClientSize.Width - 10;
                        progressBar1.Minimum = 0;
                        progressBar1.Maximum = 100;
                        progressBar1.Value = (int)(sentiment.Value * 100);
                        progressBar1.Visible = true;
                        progressBars.Add(progressBar1);
                    }
                }
            }

            foreach (var progressBar in progressBars)
            {
                if (!flowLayoutPanel1.Controls.Contains(progressBar))
                {

                    flowLayoutPanel1.Controls.Add(new System.Windows.Forms.Label
                    {
                        Text = progressBar.Name,
                        Width = flowLayoutPanel1.ClientSize.Width - 10
                    });
                    flowLayoutPanel1.Controls.Add(progressBar);
                }
            }
        }
        private void DisplaySentimentAnalysis(string speakerId)
        {


            if (!speakerSentiments.ContainsKey(speakerId))
            {
                return;
            }

            var sentiments = speakerSentiments[speakerId];
            double averagePositive = 0;
            double averageNegative = 0;
            double averageNeutral = 0;

            foreach (var sentiment in sentiments)
            {
                averagePositive += sentiment.ConfidenceScores.Positive;
                averageNegative += sentiment.ConfidenceScores.Negative;
                averageNeutral += sentiment.ConfidenceScores.Neutral;
            }

            int count = sentiments.Count;
            averagePositive /= count;
            averageNegative /= count;
            averageNeutral /= count;


            if (AggregateSpeakerSentiments.ContainsKey(speakerId))
            {
                AggregateSpeakerSentiments.Remove(speakerId);
                AggregateSpeakerSentiments.Add(speakerId, new Sentiment() { AverageNegative = averageNegative, AverageNeutral = averageNeutral, AveragePositive = averagePositive });
            }
            else
            {
                AggregateSpeakerSentiments.Add(speakerId, new Sentiment() { AverageNegative = averageNegative, AverageNeutral = averageNeutral, AveragePositive = averagePositive });
            }

            Invoke(new Action(() =>
            {
                textBox2.Text = string.Empty;

            }));

            foreach (var item in AggregateSpeakerSentiments)
            {
                string sentimentText = Environment.NewLine + Environment.NewLine + $"Speaker ID:{item.Key}" + Environment.NewLine +
                         $"Average Positive Score: {item.Value.AveragePositive:0.00}\n" + Environment.NewLine +
                         $"Average Negative Score: {item.Value.AverageNegative:0.00}\n" + Environment.NewLine +
                         $"Average Neutral Score: {item.Value.AverageNeutral:0.00}\n" + Environment.NewLine;

                // Output sentiment analysis to TextBox2
                Invoke(new Action(() =>
                {
                    AddProgressBar(item.Key);
                    textBox2.Text += sentimentText;
                    textBox2.SelectionStart = textBox2.Text.Length;
                    textBox2.ScrollToCaret();
                }));

            }


        }

        private void DisplayLanguageDetection(TextAnalyticsClient client, string text)
        {
            DetectedLanguage detectedLanguage = client.DetectLanguage(text);
            string languageText = Environment.NewLine + "_______________________________________________" + Environment.NewLine + $"Language: {detectedLanguage.Name}, ISO-6391: {detectedLanguage.Iso6391Name}" + Environment.NewLine;

            Invoke(new Action(() =>
            {
                textBox3.Text += languageText;
                textBox3.SelectionStart = textBox3.Text.Length;
                textBox3.ScrollToCaret();
            }));
        }

        private void DisplayEntityRecognition(TextAnalyticsClient client, string text)
        {
            var response = client.RecognizeEntities(text);
            string entitiesText = "Named Entities: " + Environment.NewLine;
            foreach (var entity in response.Value)
            {
                entitiesText += $"\tText: {entity.Text}, Category: {entity.Category}, Sub-Category: {entity.SubCategory}" + Environment.NewLine +
                                $"\t\tScore: {entity.ConfidenceScore:F2}" + Environment.NewLine;
            }

            Invoke(new Action(() =>
            {
                textBox3.Text += entitiesText;
                textBox3.SelectionStart = textBox3.Text.Length;
                textBox3.ScrollToCaret();
            }));
        }

        private void DisplayEntityLinking(TextAnalyticsClient client, string text)
        {
            var response = client.RecognizeLinkedEntities(text);
            string linkedEntitiesText = "Linked Entities: " + Environment.NewLine;
            foreach (var entity in response.Value)
            {
                linkedEntitiesText += $"\tName: {entity.Name}, ID: {entity.DataSourceEntityId}, URL: {entity.Url}, Data Source: {entity.DataSource}\n" +
                                      "\tMatches:\n";
                foreach (var match in entity.Matches)
                {
                    linkedEntitiesText += $"\t\tText: {match.Text}\n" +
                                          $"\t\tScore: {match.ConfidenceScore:F2}\n";
                }
            }

            Invoke(new Action(() =>
            {
                textBox3.Text += linkedEntitiesText;
                textBox3.SelectionStart = textBox3.Text.Length;
                textBox3.ScrollToCaret();
            }));
        }

        private void DisplayKeyPhraseExtraction(TextAnalyticsClient client, string text)
        {
            var response = client.ExtractKeyPhrases(text);
            string keyPhrasesText = "Key Phrases: " + Environment.NewLine;
            foreach (string keyphrase in response.Value)
            {
                keyPhrasesText += $"\t{keyphrase}" + Environment.NewLine;
            }

            Invoke(new Action(() =>
            {
                textBox3.Text += keyPhrasesText;
                textBox3.SelectionStart = textBox3.Text.Length;
                textBox3.ScrollToCaret();
            }));
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
