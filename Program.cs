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
// See https://aka.ms/new-console-template for more information


using BatClassifySharp;

string[] class_labels=new string[] { "Bbar", "Malc", "Mbec", "MbraMmys", "Mdau", "Mnat", "NSL", "Paur", "Ppip", "Ppyg", "Rfer", "Rhip" };

foreach (var arg in args ?? new string[0])
{
    Console.WriteLine(arg);
}
if (args!=null && args.Length > 0)
{
    string filename = args[0];
    BatClassifySharp.ClassifierUK classifier = ClassifierUK.Instance;

    string line = "FilePath,FileName,Date,Time,";
    foreach(var label in class_labels)
    {
        line += $"{label},";
    }
    Console.WriteLine (line);
    var result = classifier.AutoIdFile(filename,false);
    Console.Write($"{Path.GetFullPath(filename)}," +
        $"{Path.GetFileName(filename)}," +
        $"{result.date}," +
        $"{result.time},") ;

    foreach(var label in class_labels)
    {
        if (result.results.ContainsKey(label))
        {
            Console.Write($"{result.results[label]:0.00},");
        }
        else
        {
            Console.Write("NA,");
        }
    }
    var best = result.results.Max(result => result.Value);
    var bestResult = result.results.Where(result => result.Value.Equals(best)).FirstOrDefault();
    var nearby = result.results.Where(result => result.Value >= 0.5f && result.Key!=bestResult.Key);
    Console.WriteLine() ;
    Console.Write($"{bestResult.Key}:{bestResult.Value:0.00} , ");
    foreach(var res in nearby)
    {
        Console.Write($", {res.Key}:{res.Value:0.00}");
    }

    Console.WriteLine("");
    
}

