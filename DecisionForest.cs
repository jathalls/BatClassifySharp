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

        public Dictionary<string,float> Predict(List<float> features)
        {
            Dictionary<string,float> posterior_prob = new Dictionary<string, float> ();
            Dictionary<string,float> listPP=new Dictionary<string, float> ();

            foreach(var tree in _forest)
            {
                tree.Predict(features, ref listPP);
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
