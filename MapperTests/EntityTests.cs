using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Xunit;
using Google.Cloud.Datastore.V1;
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

    public class EntityTests
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
    }
}
