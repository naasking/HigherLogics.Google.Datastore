using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Datastore.V1;
using Grpc.Core;
using Xunit;
using HigherLogics.Google.Datastore;

namespace MapperTests
{
    public static class DataStoreTests
    {
        const string emulatorHost = "localhost";
        const int emulatorPort = 8081;
        const string projectId = "mappertests";
        const string namespaceId = "";

        //FIXME: test upserts

        static DatastoreDb Open()
        {
            var client = DatastoreClient.Create(new Channel(emulatorHost, emulatorPort, ChannelCredentials.Insecure));
            return DatastoreDb.Create(projectId, namespaceId, client);
        }

        [Fact]
        public static void Simple()
        {
            var db = Open();
            var x = new Simple { Baz = "Hello world!" };
            var xkey = db.Insert(x);
            var db2 = Open();
            var y = db2.Lookup(xkey, new Simple());
            Assert.Equal(x.Bar, xkey.Id());
            Assert.Equal(x.Bar, y.Bar);
            Assert.Equal(x.Baz, y.Baz);
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
            var db = Open();
            var xkey = db.Insert(x);
            var y = db.Lookup(xkey, new Complex());
            Assert.Equal(x.Id, y.Id);
            Assert.Equal(x.Uri, y.Uri);
            Assert.Equal(x.Amount, y.Amount);
            y.IO.Position = x.IO.Position = 0;
            Assert.Equal(new StreamReader(x.IO).ReadToEnd(), new StreamReader(y.IO).ReadToEnd());
            //Assert.Equal(x.Id.ToByteArray(), e["Id"]);
            //Assert.Equal(x.Uri.ToString(), e["Uri"]);
            //Assert.Equal(x.Amount, Value<decimal>.From(e["Amount"]));
            //Assert.NotEqual(x.Id.ToByteArray(), e["Uri"]);
            //Assert.NotEqual(x.Uri.ToString(), e["Id"]);
        }

        [Fact]
        public static void NestedEntityTests()
        {
            var x = new NestedEntities
            {
                Simple = new Simple { Baz = "hello world!" },
                Complex = new Complex { Amount = 99, Id = Guid.NewGuid(), Uri = new Uri("https://google.com") },
                Enumerable = new Enumerable
                {
                    Ints = null,
                    Chars = "hello world!".ToCharArray(),
                    Floats = new[] { float.MinValue, float.MaxValue, 0, float.NegativeInfinity, float.PositiveInfinity, float.NaN },
                },
                SimpleList = new[]
                {
                    new Simple { Baz = "Simple0" },
                    new Simple { Baz = "Simple1" },
                    new Simple { Baz = "Simple2" },
                }
            };
            var db = Open();
            var xkey = db.Insert(x);
            var rt = db.Lookup(xkey, new NestedEntities());
            Assert.Equal(x.Id, rt.Id);
            Assert.Equal(x.Simple.Bar, rt.Simple.Bar);
            Assert.Equal(x.Simple.Baz, rt.Simple.Baz);
            Assert.Equal(x.Complex.Amount, rt.Complex.Amount);
            Assert.Equal(x.Complex.Id, rt.Complex.Id);
            Assert.Equal(x.Complex.Uri, rt.Complex.Uri);
            Assert.Equal(x.Enumerable.Ints, rt.Enumerable.Ints);
            Assert.Equal(x.Enumerable.Chars, rt.Enumerable.Chars);
            Assert.Equal(x.Enumerable.Floats, rt.Enumerable.Floats);
            Assert.Equal(x.SimpleList.Select(z => z.Bar), rt.SimpleList.Select(z => z.Bar));
            Assert.Equal(x.SimpleList.Select(z => z.Baz), rt.SimpleList.Select(z => z.Baz));
        }
    }
}
