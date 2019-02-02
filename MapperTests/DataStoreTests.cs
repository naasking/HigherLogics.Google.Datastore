using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
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
        public static void DeleteSimple()
        {
            var db = Open();
            var x = new Simple { Baz = "Hello world!" };
            var xkey = db.Upsert(x);
            var db2 = Open();
            var y = db2.Lookup(xkey, new Simple());
            Assert.Equal(x.Bar, xkey.Id());
            Assert.Equal(x.Bar, y.Bar);
            Assert.Equal(x.Baz, y.Baz);
            
            db2.Delete<Simple>(x);
            var z = db2.Lookup(xkey, new Simple());
            Assert.Null(z);
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
            //only root entities get key values, nested entities do not?
            //Assert.True(x.SimpleList.All(z => z.Bar != 0));
            //Assert.True(rt.SimpleList.All(z => z.Bar != 0));
        }

        
        [Fact]
        public static void NestedStructTests()
        {
            var x = new NestedStruct
            {
                Foo = new Foo
                {
                    Name = "Sandro Magi",
                    Simple = new Simple
                    {
                        Bar = 33,
                        Baz = "hello world!",
                    }
                },
            };
            var db = Open();
            var xkey = db.Insert(x);
            var rt = db.Lookup(xkey, new NestedStruct());
            Assert.Equal(x.Id, rt.Id);
            Assert.Equal(x.Foo.Name, rt.Foo.Name);
            Assert.Equal(x.Foo.Simple.Bar, rt.Foo.Simple.Bar);
            Assert.Equal(x.Foo.Simple.Baz, rt.Foo.Simple.Baz);
        }

        [Fact]
        public static void FKTest()
        {
            var s = new Simple
            {
                Baz = "hello world!",
            };
            var x = new FKClass
            {
                Simple = new FK<Simple>(s),
            };
            var db = Open();
            var skey = db.Upsert(s);
            Assert.Equal(s.Bar, skey.Id());

            var xkey = db.Insert(x);
            var rt = db.Lookup(xkey, new FKClass());
            Assert.Equal(x.Id, rt.Id);
            Assert.Equal(x.Simple.Key.Id(), rt.Simple.Key.Id());
            var rts = rt.Simple.Get(db);
            Assert.Equal(s.Baz, rts.Baz);

            // mutate the property and ensure it saves
            var s2 = new Simple
            {
                Baz = "foo!",
            };
            rt.Simple.Value = s2;
            db.Upsert(s2);
            db.Upsert(rt);
            Assert.NotEqual(0, s2.Bar);
            Assert.Equal(s2.Bar, rt.Simple.Value.Bar);

            var rt2 = db.Lookup(xkey, new FKClass());
            Assert.Equal(rt.Simple.Value.Bar, rt2.Simple.Get(db).Bar);
        }

        class BulkTest
        {
            [Key]
            public long Id { get; set; }
            public FK<Simple> Simple { get; set; }
            public List<FK<Simple>> List { get; set; }
        }

        [Fact]
        public static void BulkTests()
        {
            var x = new BulkTest
            {
                Simple = new FK<Simple>(new Simple
                {
                    Baz = "first direct simple ref",
                }),
                List = new List<FK<Simple>>
                {
                    new FK<Simple>(new Simple{ Baz = "Index 0"}),
                    new FK<Simple>(new Simple{ Baz = "Index 1"}),
                    new FK<Simple>(new Simple{ Baz = "Index 2"}),
                },
            };
            var db = Open();
            var xsimple = db.Upsert(x.Simple.Value);
            Assert.NotNull(xsimple);
            Assert.NotEqual(0, x.Simple.Value.Bar);
            var xlist = db.Insert(x.List.Select(z => z.Value));
            Assert.NotNull(xlist);
            Assert.NotEmpty(xlist);
            Assert.True(x.List.All(z => 0 != z.Value.Bar));
            var xkey = db.Upsert(x);
            Assert.NotNull(xkey);

            var rt = db.Lookup(xkey, new BulkTest());
            Assert.NotNull(rt.Simple.Key);
            var rtsimple = rt.Simple.Get(db);
            Assert.Equal(x.Simple.Value.Bar, rt.Simple.Value.Bar);
            Assert.Equal(x.Simple.Value.Baz, rt.Simple.Value.Baz);
            var rtlist = db.Lookup<Simple>(rt.List.Select(z => z.Key));
            Assert.True(x.List.Select((z, i) => z.Value.Bar == rtlist[i].Bar && z.Value.Baz == rtlist[i].Baz).All(z => z));
            rt.List.Select(z => z.Get(db)).ToList();

            db.Delete(x.List.Select(z => z.Value));
            var rt2list = db.Lookup<Simple>(rt.List.Select(z => z.Key));
            Assert.True(rt2list.Select(z => z == null).All(z => z));
        }
    }
}
