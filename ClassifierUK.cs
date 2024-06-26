/*************************************************************************
  Copyright 2024 Justin A T Halls (jathalls@gmail.com)

  Copyright 2011-2014 Chris Scott (fbscds@gmail.com)

  This program is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with This program.  If not, see <http://www.gnu.org/licenses/>.

*************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BatClassifySharp
{
    public class ClassifierUK
    {
        public string[] class_labels = new string[] { "Bbar", "Malc", "Mbec", "MbraMmys", "Mdau", "Mnat", "NSL", "Paur", "Ppip", "Ppyg", "Rfer", "Rhip" };

        public enum SaveMode { NONE, SAVE, APPEND};
        private static DateTime ReadTime(string filename)
        {
            return File.GetCreationTime(filename);
        }

        public Dictionary<int, Blob> PriorityBlobs(ref Dictionary<int, Blob> blobs, int n = 4)
        {
            n = n > blobs.Count ? blobs.Count : n;
            SortedList<float, int> sortedList = new SortedList<float, int>();
            foreach (var blob in blobs)
            {
                if (blob.Value.Area() < 40) continue;
                var magnitude = blob.Value.Magnitude();
                while (sortedList.ContainsKey(magnitude))
                {
                    magnitude += float.Epsilon;
                }

                sortedList.Add(magnitude, blob.Key);

            }

            Dictionary<int, Blob> result = new Dictionary<int, Blob>();
            //int i = 0;
            for (int i = sortedList.Count - 1; i >= 0 && i > sortedList.Count - n; i--)
            {

                result.Add(sortedList.GetValueAtIndex(i), blobs[sortedList.GetValueAtIndex(i)]);
                //i++;
                //if(i>n) break;
            }

            return (result);
        }

        private DecisionForest BBar = new DecisionForest(), Myotis = new DecisionForest(),
            NSL = new DecisionForest(), Pipistrellus = new DecisionForest(),
            Paur = new DecisionForest(), Rhinolophus = new DecisionForest();
        private AudioDecoder audio = new AudioDecoder();
        private BlobFinder blobFinder = new BlobFinder();
        private STFT stft = new STFT();

        //////////////////////////////////////////////////////////////
        ///
        public ClassifierUK()
        {
            BBar.Read(@".\models\Bbar.forest");
            Myotis.Read(@".\models\Myotis.forest");
            NSL.Read(@".\models\NSL.forest");
            Pipistrellus.Read(@".\models\Pipistrellus.forest");
            Paur.Read(@".\models\Paur.forest");
            Rhinolophus.Read(@".\models\Rhinolophus.forest");
        }

        public static ClassifierUK Instance
        {
            get { return ClassifierUKInstance; }
        }

        private static readonly ClassifierUK ClassifierUKInstance=new ClassifierUK();


        public string headers()
        {

            string line = "FilePath,FileName,Date,Time,";
            foreach (var label in class_labels)
            {
                line += $"{label},";
            }
            line += "\n";
            return (line);
        }

        private FilterParams? filterParams = null;

        private Bitmap? combinedBitmap = null;

        public RecordingResults AutoIdFile(string file, SaveMode saveMode=SaveMode.NONE, float startTime=0.0f, float duration=-1.0f,bool filter=false,
            int HPFreq=12000, int HPIterations=1, int LPFreq=192000,int LPIterations=1,double FilterQ=1.0d )
        {
            combinedBitmap = null;
            RecordingResults recordingResults=new RecordingResults();
            if (filter)
            {
                filterParams=new FilterParams();
                filterParams.HighPassFilterFrequency = HPFreq;
                filterParams.HighPassFilterIterations = HPIterations;
                filterParams.LowPassFilterFrequency = LPFreq;
                filterParams.LowPassFilterIterations = LPIterations;
                filterParams.FilterQ = FilterQ;
            }

            var blobs = getBlobs(file,ref recordingResults,startTime,duration,out mImage spectro,filterParams); 
            if (blobs == null) return (recordingResults);
            int combinedBlobs = 0;
            foreach (var blob in blobs)
            {
                if (blob.Value.Area() > 40)
                {
                    mImage segment= new mImage();
                    blobFinder.Mask(ref spectro, (blob.Key,blob.Value), ref segment);
                    if (segment.Width() < 6) continue;
                    
                    segment.LogCompress(20.0f);

                    

                    if (saveMode != SaveMode.NONE)
                    {
                        if (saveMode == SaveMode.SAVE)
                        {
                            combinedBitmap = null;
                            segment.Save(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file),combinedBmp: combinedBitmap);
                        }else if (saveMode == SaveMode.APPEND)
                        {
                            if(combinedBitmap == null)
                            {
                                combinedBitmap = new Bitmap(2, segment.Height());
                            }
                            combinedBitmap=segment.Save(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file), combinedBmp: combinedBitmap);
                            combinedBlobs++;
                        }
                        Debug.WriteLine($"{combinedBlobs} - New CombinedBmp={combinedBitmap.Width}");
                    }
                    List<float> features=new List<float>();
                    segment.SegmentFeatures(out features);

                    FeatureListByBlobs.Add((blob.Value, features));
                    Debug.WriteLine($"FeatureListByBlobs has {FeatureListByBlobs.Count} blobs");

                    var probs=BBar.Predict(features);
                    List<string> labels=new List<string>();
                    labels.Add("Bbar");
                    foreach(var label in labels)
                    {
                        if (!recordingResults.results.ContainsKey(label)) { recordingResults.results.Add(label, 0.0f); }
                        if(probs.ContainsKey(label))
                            recordingResults.results[label] = probs[label] > recordingResults.results[label] ? probs[label] : recordingResults.results[label];
                    }

                    probs=Myotis.Predict(features);
                    labels = new List<string> { "Malc", "Mbec", "MbraMmys", "Mdau", "Mnat"};
                    foreach (var label in labels)
                    {
                        if (!recordingResults.results.ContainsKey(label)) { recordingResults.results.Add(label, 0.0f); }
                        if (probs.ContainsKey(label))
                            recordingResults.results[label] = probs[label] > recordingResults.results[label] ? probs[label] : recordingResults.results[label];
                    }

                    probs=NSL.Predict(features);
                    labels = new List<string> { "NSL" };
                    foreach (var label in labels)
                    {
                        if (!recordingResults.results.ContainsKey(label)) { recordingResults.results.Add(label, 0.0f); }
                        if (probs.ContainsKey(label))
                            recordingResults.results[label] = probs[label] > recordingResults.results[label] ? probs[label] : recordingResults.results[label];
                    }

                    probs=Paur.Predict(features);
                    labels = new List<string> { "Paur" };
                    foreach (var label in labels)
                    {
                        if (!recordingResults.results.ContainsKey(label)) { recordingResults.results.Add(label, 0.0f); }
                        if (probs.ContainsKey(label))
                            recordingResults.results[label] = probs[label] > recordingResults.results[label] ? probs[label] : recordingResults.results[label];
                    }

                    probs=Pipistrellus.Predict(features);
                    labels = new List<string> { "Ppip", "Ppyg" };
                    foreach (var label in labels)
                    {
                        if (!recordingResults.results.ContainsKey(label)) { recordingResults.results.Add(label, 0.0f); }
                        if (probs.ContainsKey(label))
                            recordingResults.results[label] = probs[label] > recordingResults.results[label] ? probs[label] : recordingResults.results[label];
                    }

                    probs=Rhinolophus.Predict(features);
                    labels = new List<string> { "Rfer", "Rhip" };
                    foreach (var label in labels)
                    {
                        if (!recordingResults.results.ContainsKey(label)) { recordingResults.results.Add(label, 0.0f); }
                        if (probs.ContainsKey(label))
                            recordingResults.results[label] = probs[label] > recordingResults.results[label] ? probs[label] : recordingResults.results[label];
                    }



                }
            }

            if(combinedBitmap != null)
            {

                string filePath = Path.GetDirectoryName(file);
                string fileName = Path.GetFileNameWithoutExtension(file);
                fileName += $"_{startTime}.PNG";
                fileName=Path.Combine(filePath,fileName);
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
                combinedBitmap.Save(fileName,ImageFormat.Png);
            }

            return (recordingResults);


        }

        /// <summary>
        /// A list of the significant blobs, each of which is accompanied by the features as a list of floats
        /// </summary>
        public List<(Blob blob, List<float> features)> FeatureListByBlobs { get; set; } = new List<(Blob blob, List<float> features)>();

        public Dictionary<int,Blob> getBlobs(string file, ref RecordingResults recording,float startTime,float segmentDuration, out mImage spectro,FilterParams? filterParams=null)
        {
            spectro = new mImage();
            var blobs = new Dictionary<int, Blob>();
            if (!File.Exists(file)) { return (null); }
            string fp = Path.GetFullPath(file);

            if (!audio.FileSupported(file)) { return (null); }

            var date_time = File.GetCreationTime(file);
            recording.date = date_time.Date.ToShortDateString();
            recording.time = date_time.TimeOfDay.ToString();

            List<float> samples = new List<float>();
            audio.ReadFile(file,ref samples, startTime, segmentDuration,filterParams);
            float duration = samples.Count / 500000.0f;
            
            stft.CreateSpectrogram(ref samples, ref spectro);
            //spectro.Save(fp,Path.GetFileNameWithoutExtension(file));
            samples.Clear();

            spectro.Blur();
            spectro.BackgroundSubtract();
            spectro.PostMask();
            spectro.ContrastBoost();
            //spectro.Save(fp, Path.GetFileNameWithoutExtension(file));
            var blobMap = blobFinder.Extraction(ref spectro);
            int k = ((int)duration + 1) * 4;
            blobs = PriorityBlobs(ref blobMap, k);
            return (blobs);
        }
        
    }
    public struct mDateTime
    {
        public string date;
        public string time;
        public mDateTime()
        {
            date = "NA";
            time = "NA";
        }
    }

    public struct  RecordingResults
    {
        public string filepath, filename, date, time;
        public Dictionary<string, float> results = new Dictionary<string, float>();
        public RecordingResults()
        {
            filepath = "NA";
            filename = "NA";
            date = "NA";
            time = "NA";
        }

        public string FormattedString
        {
            get
            {
                string res = filepath + ", ";
                res += filename + ", ";
                res += date + ", ";
                res += time;
                foreach(KeyValuePair<string, float> kvp in results)
                {
                    res += $", {kvp.Value:0.00}";
                }
                res += "\n";
                return res;
            }
        }
    }
}
