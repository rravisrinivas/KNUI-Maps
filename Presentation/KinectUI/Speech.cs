using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Research.Kinect.Audio;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;

namespace KinectNUI.Presentation.KinectUI
{
    public class SpeechHandler
    {
        private const string RecognizerId = "SR_MS_en-US_Kinect_10.0";
        private MainWindow mainwindow;
        private static SpeechRecognitionEngine sre_speech;

        public SpeechHandler(MainWindow _mainwindow)    {
            mainwindow = _mainwindow;
            //Thread.CurrentThread.ApartmentState = ApartmentState.MTA;
            //nMessage("Constructor");
            //System.Windows.MessageBox.Show("Contructor at last!");
        }

        [MTAThread]
        public static void listen()
        {
            System.Windows.MessageBox.Show("Listening");
            System.Windows.MessageBox.Show(Thread.CurrentThread.GetApartmentState().ToString());

            using (var source = new KinectAudioSource())
            {
                source.FeatureMode = true;
                source.AutomaticGainControl = false; //Important to turn this off for speech recognition
                source.SystemMode = SystemMode.OptibeamArrayOnly; //No AEC for this sample
                //onMessage("Constructor is doing good");
                RecognizerInfo ri = SpeechRecognitionEngine.InstalledRecognizers().Where(r => r.Id == RecognizerId).FirstOrDefault();

                if (ri == null)
                {
                    //Console.WriteLine("Could not find speech recognizer: {0}. Please refer to the sample requirements.", RecognizerId);
                    //onMessage("Could not find speech recognizer: {0}. Please refer to the sample requirements." + RecognizerId);
                    System.Windows.MessageBox.Show("RI is null");
                    return;
                }

                //onMessage("Using: {0}" + ri.Name);
                using (var sre = new SpeechRecognitionEngine(ri.Id))
                {
                    sre_speech = sre;
                    var choices = new Choices();
                    choices.Add("microsoft");
                    choices.Add("google");
                    choices.Add("facebook");
                    choices.Add("bellevue");


                    var gb = new GrammarBuilder();
                    //Specify the culture to match the recognizer in case we are running in a different culture.                                 
                    gb.Culture = ri.Culture;
                    gb.Append(choices);


                    // Create the actual Grammar instance, and then load it into the speech recognizer.
                    var g = new Grammar(gb);

                    sre.LoadGrammar(g);
                    sre.SpeechRecognized += SreSpeechRecognized;
                    sre.SpeechHypothesized += SreSpeechHypothesized;
                    sre.SpeechRecognitionRejected += SreSpeechRecognitionRejected;

                    using (Stream s = source.Start())
                    {
                        sre.SetInputToAudioStream(s,
                                                  new SpeechAudioFormatInfo(
                                                      EncodingFormat.Pcm, 16000, 16, 1,
                                                      32000, 2, null));

                        //Console.WriteLine("Recognizing. Say: 'red', 'green' or 'blue'. Press ENTER to stop");
                       sre.RecognizeAsync(RecognizeMode.Multiple);
                        //Console.ReadLine();
                        //Console.WriteLine("Stopping recognizer ...");
                        //sre.RecognizeAsyncStop();
                    }
                    
                }
            }
        }

        public static void SreSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            //Console.WriteLine("\nSpeech Rejected");
            if (e.Result != null)
            {
                //onMessage("Speech Rejected");
                System.Windows.MessageBox.Show("Speech Rejected");
                return; //Hack
            }
              //  DumpRecordedAudio(e.Result.Audio);
        }

        public static void SreSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            //Console.Write("\rSpeech Hypothesized: \t{0}", e.Result.Text);
            //onMessage("Speech Hypothesized: " + e.Result.Text);
            System.Windows.MessageBox.Show("Speech Hypothesized: " + e.Result.Text);
        }

        public static void SreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            //This first release of the Kinect language pack doesn't have a reliable confidence model, so 
            //we don't use e.Result.Confidence here.
            //Console.WriteLine("\nSpeech Recognized: \t{0}", e.Result.Text);
            //onMessage("Speech Recognized: "+ e.Result.Text);
            System.Windows.MessageBox.Show("Speech Recognized: " + e.Result.Text);
            //sre_speech.RecognizeAsyncStop();
        }

        public static void SreReconizeCompleted(object sender, SpeechRecognizedEventArgs e)
        {
            //This first release of the Kinect language pack doesn't have a reliable confidence model, so 
            //we don't use e.Result.Confidence here.
            //Console.WriteLine("\nSpeech Recognized: \t{0}", e.Result.Text);
            //onMessage("Speech Recognized: "+ e.Result.Text);
            System.Windows.MessageBox.Show("Completed");
        }

        /*private static void DumpRecordedAudio(RecognizedAudio audio)
        {
            if (audio == null) return;

            int fileId = 0;
            string filename;
            while (File.Exists((filename = "RetainedAudio_" + fileId + ".wav")))
                fileId++;

            //Console.WriteLine("\nWriting file: {0}", filename);
            using (var file = new FileStream(filename, System.IO.FileMode.CreateNew))
                audio.WriteToWaveStream(file);
        }*/

        //public event MainWindow.Message onMessage;
    }
}
