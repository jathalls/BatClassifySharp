// See https://aka.ms/new-console-template for more information


string[] class_labels=new string[] { "Bbar", "Malc", "Mbec", "MbraMmys", "Mdau", "Mnat", "NSL", "Paur", "Ppip", "Ppyg", "Rfer", "Rhip" };

foreach (var arg in args ?? new string[0])
{
    Console.WriteLine(arg);
}
if (args!=null && args.Length > 0)
{
    string filename = args[0];
    BatClassifySharp.ClassifierUK classifier = new BatClassifySharp.ClassifierUK();

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

