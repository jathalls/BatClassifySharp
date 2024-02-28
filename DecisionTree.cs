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

namespace BatClassifySharp
{
    internal class DecisionTree
    {
        protected struct Node
        {
            public int col = 0;
            public float threshold = 0;
            public List<(string, float)> probability=new List<(string, float)>();
            public bool leaf = false;
            public bool nullNode = true;

            public Node() { }
        }

        

        protected Node[] nodes=new Node[0];
        protected int[] index_lut=new int[0];

        internal void Append(string filename)
        {
            var lut = index_lut.ToList();
            File.AppendAllText(filename, $"{index_lut.Length} {nodes.Length}\n");
            int i = 0;
            foreach(var node in nodes)
            {
                //int index = index_lut.Where(ind => ind == i).FirstOrDefault();
                int index = lut.IndexOf(i);
                File.AppendAllText(filename, $"{index} {node.col} {node.threshold} {(node.leaf ? 1:0)}\n");
                if (node.leaf)
                {
                    File.AppendAllText(filename, $"{node.probability.Count}\n");
                    foreach(var prob in node.probability)
                    {
                        File.AppendAllText(filename, $"{prob.Item1} {prob.Item2}\n");
                    }
                }
                i++;
            }
        }

        internal void Read(StreamReader? sr)
        {
             
            
            int N, num_non_null;
            string line=sr?.ReadLine()??"";
            readHeader(line, out N,out num_non_null);
            index_lut = new int[N];
            nodes=new Node[num_non_null];

            for (int i = 0; i < num_non_null; i++)
            {
                int index;
                Node node=new Node();
                int col;
                float threshold;
                bool leaf;
                line=sr?.ReadLine()?.Trim()??"";
                readEntry(line, out index, out col, out threshold, out leaf);
                node.col = col;
                node.threshold = threshold;
                node.leaf = leaf;
                index_lut[index] = i;
                nodes[i]=node;
                if (node.leaf)
                {
                    int num_labels = 0;
                    line = sr?.ReadLine()?.Trim()??"";
                    _ = int.TryParse(line??"",out num_labels);
                    for(int j=0;j< num_labels; j++)
                    {
                        string label = "";
                        float p = 0.0f;
                        line = sr?.ReadLine()?.Trim() ?? "";
                        readLabel(line, out label, out p);
                        nodes[i].probability.Add((label, p));
                    }

                }


            }

        }

        private void readLabel(string line, out string label, out float p)
        {
            label = "";
            p = 0.0f;
            if (string.IsNullOrWhiteSpace(line)) return;
            var parts = line.Split(' ');
            if(parts.Length ==2 ) 
            {
                label = parts[0].Trim();
                _=float.TryParse(parts[1], out p);
            }
            else
            {
                Debug.WriteLine($"readLabel found {line} when looking for string,float");
            }
        }

        private void readEntry(string line, out int index, out int col, out float threshold, out bool leaf)
        {
            index = 0;
            col = 0;threshold = 0.0f;
            leaf= false;
            
            int leafVal = 0;

            if (string.IsNullOrWhiteSpace(line)) return;

            var parts= line.Split(' ');
            if (parts.Length >= 4)
            {
                _ = int.TryParse(parts[0], out index);
                _ = int.TryParse(parts[1],out col);
                _ = float.TryParse(parts[2],out threshold);
                _ = int.TryParse(parts[3],out leafVal);
                leaf = leafVal == 0 ? false : true;
            }
            else
            {
                Debug.WriteLine($"Read Error - do not have 4 items to read on this line <{line}>");
            }
        }

        private void readHeader(string line, out int n, out int num_non_null)
        {
            n = 0;
            num_non_null = 0;
            
            if(string.IsNullOrWhiteSpace(line)) { return; }
            
           

            var parts=line.Split(' ');
            if(parts.Length==2 ) 
            {
                if (int.TryParse(parts[0],out n))
                {
                    if (int.TryParse(parts[1],out num_non_null))
                    {
                        return;
                    }
                }
            }
            else
            {
                Debug.WriteLine($"readHeader found <{line}> when looking for two ints");
            }
            return;
        }

        public int Search(List<float> features)
        {
            int index = 0;
            int node_index = 0;

            foreach(var node in nodes)
            {
                if (index >= index_lut.Length) return 0;
                node_index = index_lut[index];
                if (nodes[node_index].leaf) { break; }
                index = (features.Count>nodes[node_index].col 
                    && features[nodes[node_index].col] >= nodes[node_index].threshold) 
                    ? (index * 2 + 2) : (index * 2 + 1);
            }

            return node_index;
        }

        public void Predict(List<float> features,ref Dictionary<string,float> result)
        {
            if (result == null) result = new Dictionary<string, float>();
            var probs = nodes[Search(features)].probability;
            foreach(var p in probs)
            {
                
                if (result.Count == 0 || !result.ContainsKey(p.Item1)) result.Add(p.Item1,p.Item2);
                else
                {
                    result[p.Item1]+=p.Item2;

                    /*
                    (string, float) match = ("", 0.0f);
                    var matchingResults = result.Where(res => res.Item1 == p.Item1);
                    if (matchingResults?.Any() ?? false)
                    {
                        match = matchingResults.First();


                        int i = result.IndexOf(match);
                        if (i >= 0 && i < result.Count)
                        {
                            float val = result[i].Item2 + p.Item2;
                            result[i] = (result[i].Item1, val);
                        }
                    }*/
                }
            }
        }

         
    }
}
