# HigherLogics.Google.Datastore

A convention-based Google Datastore entities to POCO auto mapper:

    using HigherLogics.Google.Datastore;

    public class Foo
    {
        [Key]
        public long Baz { get; set; }
        public string Bar { get; set; }
    }
    ...
    var db = new Google.Cloud.Datastore.V1.DatastoreDb("MyProject");
	var fooId = db.Insert(new Foo { Bar = "Hello World!" });
	var foo = db.Lookup(new Foo(), fooId);
    // returns: { Baz : <some long id>, Bar : "hello world!" }

It uses the standard attributes in the `System.ComponentModel.DataAnnotations`
namespace to designate the entity keys.

# Future Work

 * only Sytem.Int64 keys are currently supported.
 * preliminary support for value types that are inlined into entities needs completing