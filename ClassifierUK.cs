using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatClassifySharp
{
    internal class ClassifierUK
    {
        private static DateTime ReadTime(string filename)
        {
            return File.GetCreationTime(filename);
        }

        private Dictionary<int, Blob> PriorityBlobs(ref Dictionary<int, Blob> blobs, int n = 4)
        {
            n = n > blobs.Count ? blobs.Count : n;
            SortedList<float,int> sortedList = new SortedList<float,int>();
            foreach(var blob in blobs)
            {
                if (blob.Value.Area() < 40) continue;
                var magnitude=blob.Value.Magnitude();
                while (sortedList.ContainsKey(magnitude))
                {
                    magnitude += float.Epsilon;
                }
               
               sortedList.Add(magnitude, blob.Key);
                
            }

            Dictionary<int,Blob> result = new Dictionary<int,Blob>();
            //int i = 0;
            for(int i=sortedList.Count-1;i>=0 && i>sortedList.Count-n;i--)
            {
                
                result.Add(sortedList.GetValueAtIndex(i), blobs[sortedList.GetValueAtIndex(i)]);
                //i++;
                //if(i>n) break;
            }

            return (result);
        }

        private DecisionForest BBar=new DecisionForest(), Myotis=new DecisionForest(),
            NSL = new DecisionForest(), Pipistrellus = new DecisionForest(),
            Paur = new DecisionForest(), Rhinolophus = new DecisionForest();
        private AudioDecoder audio = new AudioDecoder();
        private BlobFinder blobFinder = new BlobFinder();
        private STFT stft=new STFT();

        //////////////////////////////////////////////////////////////
        ///
        public ClassifierUK() 
        {
            BBar.Read(@"models\Bbar.forest");
            Myotis.Read(@"models\Myotis.forest");
            NSL.Read(@"models\NSL.forest");
            Pipistrellus.Read(@"models\Pipistrellus.forest");
            Paur.Read(@"models\Paur.forest");
            Rhinolophus.Read(@"models\Rhinolophus.forest");
        }

        public RecordingResults AutoIdFile(string file,bool spectrograms)
        {
            RecordingResults recording=new RecordingResults();

            if(!File.Exists(file)) { return(recording); }
            string fp=Path.GetFullPath(file);

            if(!audio.FileSupported(file)) { return(recording); }

            var date_time=File.GetCreationTime(file);
            recording.date = date_time.Date.ToShortDateString();
            recording.time=date_time.TimeOfDay.ToString();

            List<float> samples = new List<float>();
            audio.ReadFile(file, ref samples);
            float duration = samples.Count/500000.0f;
            mImage spectro=new mImage();
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
            var blobs=PriorityBlobs(ref blobMap, k);

            foreach(var blob in blobs)
            {
                if (blob.Value.Area() > 40)
                {
                    mImage segment= new mImage();
                    blobFinder.Mask(ref spectro, (blob.Key,blob.Value), ref segment);
                    if (segment.Width() < 6) continue;

                    segment.LogCompress(20.0f);
                    spectrograms = false;
                    if (spectrograms) segment.Save(fp, Path.GetFileNameWithoutExtension(file));
                    List<float> features=new List<float>();
                    segment.SegmentFeatures(out features);

                    var probs=BBar.Predict(features);
                    List<string> labels=new List<string>();
                    labels.Add("Bbar");
                    foreach(var label in labels)
                    {
                        if (!recording.results.ContainsKey(label)) { recording.results.Add(label, 0.0f); }
                        if(probs.ContainsKey(label))
                            recording.results[label] = probs[label] > recording.results[label] ? probs[label] : recording.results[label];
                    }

                    probs=Myotis.Predict(features);
                    labels = new List<string> { "Malc", "Mbec", "MbraMmys", "Mdau", "Mnat"};
                    foreach (var label in labels)
                    {
                        if (!recording.results.ContainsKey(label)) { recording.results.Add(label, 0.0f); }
                        if (probs.ContainsKey(label))
                            recording.results[label] = probs[label] > recording.results[label] ? probs[label] : recording.results[label];
                    }

                    probs=NSL.Predict(features);
                    labels = new List<string> { "NSL" };
                    foreach (var label in labels)
                    {
                        if (!recording.results.ContainsKey(label)) { recording.results.Add(label, 0.0f); }
                        if (probs.ContainsKey(label))
                            recording.results[label] = probs[label] > recording.results[label] ? probs[label] : recording.results[label];
                    }

                    probs=Paur.Predict(features);
                    labels = new List<string> { "Paur" };
                    foreach (var label in labels)
                    {
                        if (!recording.results.ContainsKey(label)) { recording.results.Add(label, 0.0f); }
                        if (probs.ContainsKey(label))
                            recording.results[label] = probs[label] > recording.results[label] ? probs[label] : recording.results[label];
                    }

                    probs=Pipistrellus.Predict(features);
                    labels = new List<string> { "Ppip", "Ppyg" };
                    foreach (var label in labels)
                    {
                        if (!recording.results.ContainsKey(label)) { recording.results.Add(label, 0.0f); }
                        if (probs.ContainsKey(label))
                            recording.results[label] = probs[label] > recording.results[label] ? probs[label] : recording.results[label];
                    }

                    probs=Rhinolophus.Predict(features);
                    labels = new List<string> { "Rfer", "Rhip" };
                    foreach (var label in labels)
                    {
                        if (!recording.results.ContainsKey(label)) { recording.results.Add(label, 0.0f); }
                        if (probs.ContainsKey(label))
                            recording.results[label] = probs[label] > recording.results[label] ? probs[label] : recording.results[label];
                    }



                }
            }

            return (recording);


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
    }
}
