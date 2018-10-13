# Google.Cloud.Datastore.V1.Mapper

A convention-based Google Datastore entities to POCO auto mapper:

    public class Foo
    {
        public int Baz { get; set; }
        public string Bar { get; set; }
    }
    ...
    var db = new Google.Cloud.Datastore.V1.DatastoreDb("MyProject");
	var fooKeys = db.CreateKeyFactory<Foo>();
	var fooId = db.Insert(new Foo { Baz = 99, Bar = "Hello World!" }, fooKeys.CreateIncompleteKey());
	var foo = db.Lookup(new Foo(), fooId);
    // returns: { Baz : 99, Bar : "hello world!" }
