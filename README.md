# Google.Cloud.Datastore.V1.Mapper

A convention-based Google Datastore entities to POCO auto mapper:

    public class Foo
    {
        public int Id { get; set; }
        public string Bar { get; set; }
    }
    ...
    var db = new Google.Cloud.Datastore.V1.DatastoreDb("MyProject");
    var keys = db.CreateKeyFactory(nameof(Foo));
    var fooId = keys.CreateKey(1234567);
    // returns: { Id : 1234567, Bar : "hello world!" }
    var foo = Entity<Foo>.From(db.Lookup(fooId), new Foo());
