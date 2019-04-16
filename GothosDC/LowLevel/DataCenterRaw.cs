using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GothosDC.LowLevel
{
    public class DataCenterRaw
    {
        private Stream _stream;
        private MemoryStream _buffer = new MemoryStream();

        public DataCenterRegions Regions { get; private set; }

        public List<KeyValuePair<SegmentAddress, DataCenterValueRaw>> Values { get; set; }
        public List<KeyValuePair<SegmentAddress, DataCenterElementRaw>> Elements { get; set; }
        public List<KeyValuePair<SegmentAddress, string>> Strings { get; set; }
        public List<SegmentAddress> StringIds { get; set; }
        public List<KeyValuePair<SegmentAddress, UnknownStruct>> StringUnknown { get; set; }
        public List<KeyValuePair<SegmentAddress, string>> Names { get; set; }
        public List<SegmentAddress> NameIds { get; set; }
        public List<KeyValuePair<SegmentAddress, UnknownStruct>> NameUnknown { get; set; }

        public int Revision { get; private set; }

        public static DataCenterRaw Load(string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                return new DataCenterRaw(stream);
            }
        }

        internal DataCenterRaw(Stream stream)
        {
            _stream = stream;
            Console.WriteLine("Reading regions... ");
            Regions = RegionListReader.ReadAllRegions(stream);
            Console.WriteLine("Reading header... ");
            ReadHeader();
            Console.WriteLine("Reading strings... ");
            Strings = ReadSegmented(Regions.Strings, r => r.ReadTeraString());
            Console.WriteLine("Reading values... ");
            Values = ReadSegmented(Regions.Values, r => r.ReadDcValue());
            Console.WriteLine("Reading string IDs... ");
            StringIds = ReadAll(Regions.StringIds, r => r.ReadSegmentAddress());
            Console.WriteLine("Reading string unks... ");
            StringUnknown = ReadSegmented(Regions.Unknown1, r => r.ReadUnknown());
            Console.WriteLine("Reading names... ");
            Names = ReadSegmented(Regions.Names, r => r.ReadTeraString());
            Console.WriteLine("Reading name IDs... ");
            NameIds = ReadAll(Regions.NameIds, r => r.ReadSegmentAddress());
            Console.WriteLine("Reading name unks... ");
            NameUnknown = ReadSegmented(Regions.Unknown1, r => r.ReadUnknown());
            Console.WriteLine("Reading elements... ");
            Elements = ReadSegmented(Regions.Elements, r => r.ReadDcObject());

            AssertSequenceEquals(Strings.Select(x => x.Key), StringIds);
            AssertSequenceEquals(Names.Select(x => x.Key), NameIds);

            _buffer = null;
            _stream = null;
            Console.WriteLine("Done creating DataCenterRaw");
        }

        private void ReadHeader()
        {
            using (var reader = new TeraDataReader(_stream))
            {
                _stream.Position = 0x0C;
                Revision = reader.ReadInt32();
            }
        }

        private List<KeyValuePair<SegmentAddress, T>> ReadSegmented<T>(IEnumerable<DataCenterRegion> regions, Func<TeraDataReader, T> readOne)
        {
            return Flatten(regions.Select(region => ReadAll(region, TeraDataReader.WithOffset(readOne, region.ElementSize))));
        }

        private List<KeyValuePair<SegmentAddress, T>> Flatten<T>(IEnumerable<IEnumerable<KeyValuePair<ushort, T>>> segmentedData)
        {
            return segmentedData.SelectMany((x, segmentIndex) => x.Select(y => new KeyValuePair<SegmentAddress, T>(new SegmentAddress((ushort)segmentIndex, y.Key), y.Value))).ToList();
        }

        private List<T> ReadAll<T>(DataCenterRegion region, Func<TeraDataReader, T> readOne)
        {
            return TeraDataReader.ReadAll(GetStreamForRegion(region), readOne).ToList();
        }

        private Stream GetStreamForRegion(DataCenterRegion region)
        {
            _buffer.SetLength(0);
            _buffer.Position = 0;
            new StreamSlice(_stream, region.Start, region.Length).CopyTo(_buffer);
            _buffer.Position = 0;
            return _buffer;
        }

        private static void AssertSequenceEquals<T>(IEnumerable<T> x, IEnumerable<T> y)
        {
            var onlyX = x.Except(y);
            var onlyY = y.Except(x);
            if (onlyX.Any() || onlyY.Any())
                throw new Exception("Inconsitency detected");
        }
    }
}
