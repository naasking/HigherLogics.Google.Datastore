using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Google.Cloud.Datastore.V1;
using Google.Cloud.Datastore.V1.Mapper;

namespace MapperTests
{
    class Simple
    {
        public int Bar { get; set; }
        public string Baz { get; set; }
    }

    class Complex
    {
        public Guid Id { get; set; }
        public Uri Uri { get; set; }
        public decimal Amount { get; set; }
        public Stream IO { get; set; }
    }

    public class EntityTests
    {
        [Fact]
        public static void SimpleTests()
        {
            var x = new Simple { Bar = 99, Baz = "hello world!" };
            var e = Entity<Simple>.To(x, new Entity());
            var y = Entity<Simple>.From(e, new Simple());
            Assert.Equal(x.Bar, y.Bar);
            Assert.Equal(x.Baz, y.Baz);
            Assert.Equal(x.Bar, e["Bar"]);
            Assert.Equal(x.Baz, e["Baz"]);
            Assert.NotEqual(x.Bar, e["Baz"].IntegerValue);
            Assert.NotEqual(x.Baz, e["Bar"].StringValue);
        }

        [Fact]
        public static void ComplexTests()
        {
            var x = new Complex
            {
                Id = Guid.NewGuid(),
                Uri = new Uri("http://google.ca"),
                Amount = 987654321M,
                IO = new MemoryStream(Encoding.ASCII.GetBytes("hello world!")),
            };
            var e = Entity<Complex>.To(x, new Entity());
            var y = Entity<Complex>.From(e, new Complex());
            Assert.Equal(x.Id, y.Id);
            Assert.Equal(x.Uri, y.Uri);
            Assert.Equal(x.Amount, y.Amount);
            y.IO.Position = x.IO.Position = 0;
            Assert.Equal(new StreamReader(x.IO).ReadToEnd(), new StreamReader(y.IO).ReadToEnd());
            Assert.Equal(x.Id.ToByteArray(), e["Id"]);
            Assert.Equal(x.Uri.ToString(), e["Uri"]);
            Assert.Equal(x.Amount, Value<decimal>.From(e["Amount"]));
            Assert.NotEqual(x.Id.ToByteArray(), e["Uri"]);
            Assert.NotEqual(x.Uri.ToString(), e["Id"]);
        }
    }
}
