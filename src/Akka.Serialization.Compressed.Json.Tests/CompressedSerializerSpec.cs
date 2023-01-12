using Akka.Serialization.TestKit;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Serialization.Compressed.Json.Tests
{
    public class CompressedSerializerSpec: AkkaSerializationSpec
    {
        public CompressedSerializerSpec(ITestOutputHelper helper) : base(typeof(CompressedJsonSerializer), helper)
        {
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
        
        private class BigData
        {
            public string Message { get; } = new('x', 5 * 1024 * 1024); // 5 megabyte worth of chars
        }
    }
}

