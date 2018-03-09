using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Google.Cloud.Datastore.V1;
using Google.Cloud.Datastore.V1.Mapper;

namespace MapperTests
{
    class Foo
    {
        public int Bar { get; set; }
        public string Baz { get; set; }
    }

    class Bar
    {

    }

    public class EntityTests
    {
        [Fact]
        public static void FooTests()
        {
            var x = new Foo { Bar = 99, Baz = "hello world!" };
            var e = Entity<Foo>.To(x, new Entity());
            var y = Entity<Foo>.From(e, new Foo());
            Assert.Equal(x.Bar, y.Bar);
            Assert.Equal(x.Baz, y.Baz);
            Assert.Equal(x.Bar, e["Bar"]);
            Assert.Equal(x.Baz, e["Baz"]);
        }
    }
}
