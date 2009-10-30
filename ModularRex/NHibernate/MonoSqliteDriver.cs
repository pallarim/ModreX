namespace NHibernate.Driver
{
public class MonoSqliteDriver : NHibernate.Driver.ReflectionBasedDriver
{
    public MonoSqliteDriver() : 
        base("Mono.Data.SqliteClient",
        "Mono.Data.SqliteClient.SqliteConnection",
        "Mono.Data.SqliteClient.SqliteCommand")
    {
    }
    public override bool UseNamedPrefixInParameter {
        get {
            return true;
        }
    }
    public override bool UseNamedPrefixInSql {
        get {
            return true;
        }
    }
    public override string NamedPrefix {
        get {
            return "@";
        }
    }
    public override bool SupportsMultipleOpenReaders {
        get {
            return false;
        }
    }
}
}
