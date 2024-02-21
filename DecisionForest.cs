using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace BatClassifySharp
{
    internal class DecisionForest
    {
        protected List<DecisionTree> _forest=new List<DecisionTree>();
        public void Read(string filename)
        {
            int forest_size = 0;
            if (File.Exists(filename))
            {
                
                //string[] Lines=File.ReadLines(filename).ToArray();
                using (StreamReader? sr = new StreamReader(filename))
                {
                    if (sr == null) return;
                    string line= sr?.ReadLine()?.Trim()??"";
                    if (!int.TryParse(line, out forest_size))
                    {
                        forest_size = 0;
                    }
                    _forest.Clear();
                    _forest.Capacity = forest_size;
                    for (int i = 0; i < forest_size; i++)
                    {
                        var dt = new DecisionTree();
                        dt.Read(sr);
                        _forest.Add(dt);
                        //_forest[i].Read(Lines);
                    }
                }
            }
            else
            {
                Debug.WriteLine($"file <{filename}> does not exist at <{Path.GetFullPath(".\\")}>");
            }
            //Write(Path.ChangeExtension(filename, ".dbg"));
            Debug.WriteLine($"Expected {forest_size} trees, and found {_forest.Count}");


        }

        private void Write(string filename)
        {
            Debug.WriteLine($"{filename} is {Path.GetFullPath(filename)}");
            if(File.Exists(filename)) { File.Delete(filename); }
            File.WriteAllText(filename,$"{_forest.Count}\n");
            foreach(var tree in _forest??new List<DecisionTree>())
            {
                tree?.Append(filename);
            }
        }

        public Dictionary<string,float> Predict(List<float> data)
        {
            Dictionary<string,float> posterior_prob = new Dictionary<string, float> ();
            Dictionary<string,float> listPP=new Dictionary<string, float> ();

            foreach(var tree in _forest)
            {
                tree.Predict(data, ref listPP);
            }
            float sum = 0.0f;
            foreach(var prob in listPP)
            {
                sum += prob.Value;
            }
            foreach(var prob in listPP)
            {
                float val = prob.Value;
                val /= sum;
                val = (val > 0.999999f)?0.999999f:val;
                val = (val < 0.0000001f) ? 0.0000001f : val;
                posterior_prob.Add(prob.Key, val);

            }
            //foreach(var prob in listPP) posterior_prob.Add(prob.Item1, prob.Item2);
            return(posterior_prob);
        }
    }

    
}
