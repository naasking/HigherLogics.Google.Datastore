using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Xunit;
using Google.Cloud.Datastore.V1;
using Google.Type;
using HigherLogics.Google.Datastore;

namespace MapperTests
{
    class Simple
    {
        [Key]
        public long Bar { get; set; }
        public string Baz { get; set; }
    }

    class Complex
    {
        [Key]
        public long ComplexId { get; set; }
        public Guid Id { get; set; }
        public Uri Uri { get; set; }
        public decimal Amount { get; set; }
        public Stream IO { get; set; }
        public Google.Type.LatLng Coords { get; set; }
    }

    class Enumerable
    {
        [Key]
        public long Id { get; set; }
        public int[] Ints { get; set; }
        public char[] Chars { get; set; }
        public IEnumerable<float> Floats { get; set; }
    }

    class NestedEntities
    {
        [Key]
        public long Id { get; set; }
        public Simple Simple { get; set; }
        public Complex Complex { get; set; }
        public Enumerable Enumerable { get; set; }
        public Simple[] SimpleList { get; set; }
    }

    struct Foo
    {
        public string Name { get; set; }
        public Simple Simple { get; set; }
    }

    class NestedStruct
    {
        [Key]
        public long Id { get; set; }
        public Foo Foo { get; set; }
        public NestedStruct Next { get; set; }
    }

    class IgnoreFields
    {
        [Key]
        public long Id { get; set; }
        [NotMapped]
        public string Data { get; set; }
    }

    class FKClass
    {
        [Key]
        public long Id { get; set; }
        public FK<Simple> Simple { get; set; }
    }

    class GoogleTypes
    {
        [Key]
        public long Id { get; set; }
        public LatLng Coords { get; set; }
        public Color Color { get; set; }
        public Date Date { get; set; }
        public Money Money { get; set; }
        public PostalAddress PostalAddress { get; set; }
        public TimeOfDay TimeOfDay { get; set; }
    }

    class StringKey
    {
        [Key]
        public string Id { get; set; }
        public float E { get; set; }
    }

    class KeyKey
    {
        [Key]
        public Key Id { get; set; }
        public float E { get; set; }
        public Entity Foo { get; set; }
    }

    public static class EntityTests
    {
        [Fact]
        public static void SimpleTests()
        {
            var x = new Simple { Bar = 99, Baz = "hello world!" };
            var e = Entity<Simple>.To(new Entity(), x);
            var y = Entity<Simple>.From(new Simple(), e);
            Assert.Equal(x.Bar, y.Bar);
            Assert.Equal(x.Baz, y.Baz);
            Assert.Equal(x.Bar, e.Key.Id());
            Assert.Equal(x.Baz, e["Baz"]);
            Assert.NotEqual(x.Bar, e["Baz"].IntegerValue);
            Assert.NotEqual(x.Baz, e.Key.ToString());
        }

        [Fact]
        public static void SimpleIncomplete()
        {
            var x = new Simple { Bar = 99, Baz = "hello world!" };
            var e = Entity<Simple>.To(new Entity(), x);
            var y = Entity<Simple>.From(new Simple(), e);
            Assert.Equal(x.Bar, y.Bar);
            Assert.Equal(x.Baz, y.Baz);
            Assert.Equal(x.Bar, e.Key.Id());
            Assert.Equal(x.Baz, e["Baz"]);
            Assert.NotEqual(x.Bar, e["Baz"].IntegerValue);
            Assert.NotEqual(x.Baz, e.Key.ToString());
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
                Coords = new Google.Type.LatLng { Latitude = 3.14, Longitude = 2.718 },
            };
            var e = Entity<Complex>.To(new Entity(), x);
            var y = Entity<Complex>.From(new Complex(), e);
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
            Assert.NotNull(y.Coords);
            Assert.Equal(x.Coords.Latitude, y.Coords.Latitude);
            Assert.Equal(x.Coords.Longitude, y.Coords.Longitude);
        }

        [Fact]
        public static void EnumerableTests()
        {
            var x = new Enumerable
            {
                Ints = new[] { 99, 23, 239233948, int.MinValue, int.MaxValue, 0 },
                Chars = "hello world!".ToCharArray(),
                Floats = new[] { float.MinValue, float.MaxValue, 0, float.NegativeInfinity, float.PositiveInfinity, float.NaN },
            };
            var e = Entity<Enumerable>.To(new Entity(), x);
            var y = Entity<Enumerable>.From(new Enumerable(), e);
            Assert.Equal(x.Ints, y.Ints);
            Assert.Equal(x.Chars, y.Chars);
            Assert.Equal(x.Floats, y.Floats);
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
            var e = Entity<NestedEntities>.To(new Entity(), x);
            var rt = Entity<NestedEntities>.From(new NestedEntities(), e);
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

        [Fact]
        public static void NestedStructTests()
        {
            var x = new NestedStruct
            {
                Id = int.MaxValue / 2,
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
            var e = Entity<NestedStruct>.To(new Entity(), x);
            var rt = Entity<NestedStruct>.From(new NestedStruct(), e);
            Assert.Equal(x.Id, rt.Id);
            Assert.Equal(x.Foo.Name, rt.Foo.Name);
            Assert.Equal(x.Foo.Simple.Bar, rt.Foo.Simple.Bar);
            Assert.Equal(x.Foo.Simple.Baz, rt.Foo.Simple.Baz);
        }

        [Fact]
        public static void IgnorePropertyTests()
        {
            var x = new IgnoreFields
            {
                Id = int.MaxValue / 2,
                Data = "hello world!",
            };
            var e = Entity<IgnoreFields>.To(new Entity(), x);
            var rt = Entity<IgnoreFields>.From(new IgnoreFields(), e);
            Assert.Equal(x.Id, rt.Id);
            Assert.Null(rt.Data);
            Assert.Null(e["Data"]);
            Assert.NotEqual(x.Data, rt.Data);
        }

        [Fact]
        public static void FKTest()
        {
            var x = new FKClass
            {
                Id = int.MaxValue / 2,
                Simple = new FK<Simple>(new Simple
                {
                    Bar = 33,
                    Baz = "hello world!",
                }),
            };
            var e = Entity<FKClass>.To(new Entity(), x);
            var rt = Entity<FKClass>.From(new FKClass(), e);
            Assert.Equal(x.Id, rt.Id);
            Assert.Equal(x.Simple.Key, rt.Simple.Key);
        }

        [Fact]
        public static void StringKeyTest()
        {
            var x = new StringKey
            {
                Id = "Hello world!",
                E = float.Epsilon,
            };
            var e = Entity<StringKey>.To(new Entity(), x);
            var rt = Entity<StringKey>.From(new StringKey(), e);
            Assert.Equal(x.Id, rt.Id);
            Assert.Equal(x.E, rt.E);
        }

        [Fact]
        public static void KeyKeyTest()
        {
            var x = new KeyKey
            {
                Id = "Hello world!".ToKey<KeyKey>(),
                E = float.Epsilon,
                Foo = new Entity
                {
                    ["Bar"] = "hello world!",
                },
            };
            var e = Entity<KeyKey>.To(new Entity(), x);
            var rt = Entity<KeyKey>.From(new KeyKey(), e);
            Assert.Equal(x.Id, rt.Id);
            Assert.Equal(x.E, rt.E);
            Assert.Equal(x.Foo, rt.Foo);
            Assert.True(ReferenceEquals(x.Foo, rt.Foo));
        }

        [Fact]
        public static void GoogleTypeTests()
        {
            var x = new GoogleTypes
            {
                Color = new Color { Red = 123, Green = 231, Blue = 312, Alpha = 1 },
                Coords = new LatLng { Latitude = 3.14, Longitude = 2.718 },
                Date = new Date { Day = 21, Month = 4, Year = 2019 },
                Money = new Money { CurrencyCode = "CAD", Units = long.MaxValue / 2, Nanos = 33 },
                PostalAddress = new PostalAddress
                {
                    Recipients = { "Someone", "No one" },
                    AddressLines = { "1234 Somewhere rd." },
                    AdministrativeArea = "Admin",
                    LanguageCode = "en-US",
                    Locality = "Some City",
                    Organization = "Org",
                    PostalCode = "6434543",
                    RegionCode = "Reg.",
                    Revision= 4,
                    SortingCode = "Sort",
                    Sublocality = "sublocal",
                },
                TimeOfDay = new TimeOfDay
                {
                    Hours = 17,
                    Minutes = 23,
                    Seconds = 59,
                    Nanos = 123,
                },
            };
            var e = Entity<GoogleTypes>.To(new Entity(), x);
            var y = Entity<GoogleTypes>.From(new GoogleTypes(), e);
            Assert.Equal(x.Color, y.Color);
            Assert.Equal(x.Coords, y.Coords);
            Assert.Equal(x.Money, y.Money);
            Assert.Equal(x.Date, y.Date);
            Assert.Equal(x.PostalAddress, y.PostalAddress);
            Assert.Equal(x.TimeOfDay, y.TimeOfDay);
        }

    }
}
