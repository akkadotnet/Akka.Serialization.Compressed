using System;
using System.Collections.Generic;
using System.Text;
using Akka.Configuration;
using Akka.Serialization.TestKit;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Serialization.Compressed.Json.Tests
{
    public class CompressedSerializerSpec: AkkaSerializationSpec
    {
        private readonly ITestOutputHelper _output;
        public CompressedSerializerSpec(ITestOutputHelper helper) 
            : base(CompressedJsonSerializer.DefaultConfiguration(), "json-gzip", helper)
        {
            _output = helper;
        }
        
        [Fact(Skip = "JSON serializer can not serialize Config")]
        public override void CanSerializeConfig()
        {
            base.CanSerializeConfig();
        }
        
        [Fact(DisplayName = "Compression must be turned on")]
        public void SettingTest()
        {
            var serializer = (CompressedJsonSerializer) Sys.Serialization.FindSerializerForType(typeof(BigData));
            var bytes = serializer.ToBinary(new BigData());
            bytes.Length.Should().BeLessThan(10 * 1024); // compressed size should be less than 10Kb
            var deserialized = serializer.FromBinary<BigData>(bytes);
            deserialized.Message.Should().Be(new string('x', 5 * 1024 * 1024));
        }

        [Fact(DisplayName = "Compression must work on all data length")]
        public void DataLengthTest()
        {
            var serializer = (CompressedJsonSerializer) Sys.Serialization.FindSerializerForType(typeof(BigData));
            for (var i = 0; i < 1025; i = ( i == 0 ? 1 : i == 1 ? 2 : (int)Math.Pow(i, 2)))
            {
                for (var j = 0; j < 100; j++)
                {
                    var data = GenerateString(i);
                    var bytes = serializer.ToBinary(data);
                    var deserialized = serializer.FromBinary<string>(bytes);
                    _output.WriteLine($"{data} >> {deserialized}");
                    deserialized.Should().Be(data);
                }
            }
        }

        private readonly Random _rnd = new ();
        private readonly StringBuilder _sb = new ();
        private string GenerateString(int len)
        {
            _sb.Clear();
            for (var i = 0; i < len; ++i)
            {
                _sb.Append((char)_rnd.Next(32, 126));
            }
            return _sb.ToString();
        }
    }

    internal class TestClassBase
    {
        [JsonExtensionData]
        public Dictionary<string, object> CustomProperties { get; set; } = new ();
        
        
    }
}

