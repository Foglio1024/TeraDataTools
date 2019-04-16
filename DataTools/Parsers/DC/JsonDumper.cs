using DCTools;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DataTools.Parsers.DC
{
    class JsonDumper
    {
        public static async Task Parse(IProgress<ParseProgress> progress)
        {
            var outDir = ((App.Current.MainWindow as MainWindow).DataContext as MainVM).OutPath;
            var groups = DCT.DataCenter.Root.Children.GroupBy(x => x.Name);
            var count = groups.Count();
            var g = 0;
            var dc = DCT.GetDataCenter();
            foreach (var group in groups)
            {
                await Task.Run(async () =>
                {
                    var pi = new ParseProgress();
                    pi.CurrentGroup = group.Key;

                    string dir2, format;
                    if (group.Count() > 1)
                    {
                        dir2 = outDir + "/" + group.Key + "/";
                        format = "{0}-{1}.json";
                    }
                    else
                    {
                        dir2 = outDir + "/";
                        format = "{0}.json";
                    }

                    g++;
                    pi.OverallProgress = g / (float)count;

                    var i = 0;
                    var objectsCount = group.Count();
                    foreach (var mainObject in group)
                    {
                        await Task.Run(async () =>
                        {
                            var values = dc.GetValues(mainObject, x => x.Value);
                            var json = JsonConvert.SerializeObject(values, Formatting.Indented);
                            var fName = string.Format(format, mainObject.Name, i);
                            var path = Utils.GetOutput(dir2 + fName);
                            File.WriteAllText(path, json);
                            i++;

                            pi.CurrentFile = fName;
                            pi.GroupProgress = i / (float)objectsCount;
                            progress.Report(pi);

                        });
                    }
                });

            }
        }
    }
}
