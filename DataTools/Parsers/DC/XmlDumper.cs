using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using DataTools;
using DataTools.Parsers.DC;
using DCTools;
using GothosDC;
using Microsoft.Win32;

namespace DataTools.Parsers.DC
{
    public struct ParseProgress
    {
        public string CurrentGroup { get; set; }
        public string CurrentFile { get; set; }

        public float GroupProgress { get; set; }
        public float OverallProgress { get; set; }
    }
    public class XmlDumper
    {
        public async static Task Parse(IProgress<ParseProgress> progress)
        {
            var dc = DCT.GetDataCenter();
            var groups = DCT.DataCenter.Root.Children.GroupBy(x => x.Name);
            var count = groups.Count();
            var g = 0;
            var outDir = ((App.Current.MainWindow as MainWindow).DataContext as MainVM).OutPath;
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
                        format = "{0}-{1}.xml";
                    }
                    else
                    {
                        dir2 = outDir + "/";
                        format = "{0}.xml";
                    }

                    g++;
                    pi.OverallProgress = g / (float)count;
                    Directory.CreateDirectory(dir2);
                    int i = 0;
                    var objectsCount = group.Count();
                    foreach (var mainObject in group)
                    {
                        await Task.Run(() =>
                        {
                            var element = ConvertToXElement(mainObject);
                            var fName = string.Format(format, mainObject.Name, i);
                            var path = dir2 + fName;
                            element.Save(path);
                            i++;
                            pi.CurrentFile = fName;
                            pi.GroupProgress = i / (float)objectsCount;
                            progress.Report(pi);
                            //Console.WriteLine($"[{i / (float)group.Count():P2}]\tCurrent: {fName}");
                        });
                    }
                });
            }
        }

        private static XElement ConvertToXElement(DataCenterElement obj)
        {
            var element = new XElement(obj.Name);
            foreach (var arg in obj.Attributes)
            {
                element.SetAttributeValue(arg.Name, arg.ValueToString(CultureInfo.InvariantCulture));
            }
            foreach (var child in obj.Children)
            {
                var childElement = ConvertToXElement(child);
                element.Add(childElement);
            }
            return element;
        }
    }
}
