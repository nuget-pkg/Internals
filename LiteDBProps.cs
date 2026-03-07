#if GLOBAL_SYS
namespace Global
{
    using Global;
    using LiteDB;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    public class LiteDBProps :
        IExportToPlainObject,
        IImportFromPlainObject,
        IExportToCommonJson,
        IImportFromCommonJson
    {
        public class Prop
        {
            public long Id
            {
                get; set;
            }
            public string? Name
            {
                get; set;
            }
            public object? Data
            {
                get; set;
            }
        }
        private string? filePath = null;
        public LiteDBProps(FileInfo fi)
        {
            this.filePath = fi.FullName;
            Sys.PrepareForFile(this.filePath);
            using (var connection = new LiteDatabase(new ConnectionString(this.filePath)
            {
                Connection = ConnectionType.Shared
            }))
            {
                var collection = connection.GetCollection<Prop>("properties");
                // Nameをユニークインデックスにする
                collection.EnsureIndex(x => x.Name, true);
            }
        }
        public LiteDBProps(DirectoryInfo di)
        {
            this.filePath = Path.Combine(di.FullName, "properties.litedb");
            Sys.PrepareForFile(this.filePath);
            using (var connection = new LiteDatabase(new ConnectionString(this.filePath)
            {
                Connection = ConnectionType.Shared
            }))
            {
                var collection = connection.GetCollection<Prop>("properties");
                // Nameをユニークインデックスにする
                collection.EnsureIndex(x => x.Name, true);
            }
        }
        public LiteDBProps(string orgName, string appNam) : this(new DirectoryInfo(Dirs.ProfilePath(orgName, appNam)))
        {
        }
        public LiteDBProps(string appNam) : this(new DirectoryInfo(Dirs.ProfilePath(appNam)))
        {
        }
        public EasyObject Get(string name, object? fallback = null)
        {
            using (var connection = new LiteDatabase(new ConnectionString(this.filePath)
            {
                Connection = ConnectionType.Shared
            }))
            {
                connection.BeginTrans();
                var collection = connection.GetCollection<Prop>("properties");
                var result = collection.Find(x => x.Name == name).FirstOrDefault();
                connection.Commit();
                if (result == null)
                {
                    return EasyObject.FromObject(fallback);
                }
                return EasyObject.FromObject(result.Data);
            }
        }
        public void Put(string name, dynamic? data)
        {
            data = EasyObject.FromObject(data).ExportToPlainObject();
            using (var connection = new LiteDatabase(new ConnectionString(this.filePath)
            {
                Connection = ConnectionType.Shared
            }))
            {
                connection.BeginTrans();
                var collection = connection.GetCollection<Prop>("properties");
                var result = collection.Find(x => x.Name == name).FirstOrDefault();
                if (result == null)
                {
                    result = new Prop
                    {
                        Name = name,
                        Data = data
                    };
                    collection.Insert(result);
                    connection.Commit();
                    return;
                }
                result.Data = data;
                collection.Update(result);
                connection.Commit();
            }
        }
        public void Delete(string name)
        {
            using (var connection = new LiteDatabase(new ConnectionString(this.filePath)
            {
                Connection = ConnectionType.Shared
            }))
            {
                connection.BeginTrans();
                var collection = connection.GetCollection<Prop>("properties");
                var result = collection.Find(x => x.Name == name).FirstOrDefault();
                if (result == null)
                {
                    connection.Commit();
                    return;
                }
                collection.Delete(result.Id);
                connection.Commit();
            }
        }
        public void DeleteAll()
        {
            using (var connection = new LiteDatabase(new ConnectionString(this.filePath)
            {
                Connection = ConnectionType.Shared
            }))
            {
                connection.BeginTrans();
                var collection = connection.GetCollection<Prop>("properties");
                collection.DeleteAll();
                connection.Commit();
            }
        }
        public List<string> Keys
        {
            get
            {
                using (var connection = new LiteDatabase(new ConnectionString(this.filePath)
                {
                    Connection = ConnectionType.Shared
                }))
                {
                    connection.BeginTrans();
                    var collection = connection.GetCollection<Prop>("properties");
                    IEnumerable<Prop> all = collection.FindAll();
                    IEnumerable<string?> keys = all.Select(_ => _.Name);
                    var list = new List<string>();
                    foreach (var key in keys)
                    {
                        list.Add(key!);
                    }
                    connection.Commit();
                    return list;
                }
            }
        }
        public override string ToString()
        {
            return EasyObject.FromObject(this).ToJson(indent: true, keyAsSymbol: true);
        }

        public object? ExportToPlainObject()
        {
            var keys = this.Keys;
            EasyObject eo = EasyObject.NewObject();
            foreach (var key in keys)
            {
                eo[key] = this.Get(key);
            }
            return eo.ToObject();
        }
        public void ImportFromPlainObject(object? x)
        {
            var eo = EasyObject.FromObject(x);
            if (!eo.IsObject)
            {
                Sys.Crash("LiteDBProps.ImportFromPlainObject(): argumet is not object/dictionary!");
            }
            this.DeleteAll();
            var keys = eo.Keys;
            foreach (var key in keys)
            {
                this.Put(key, eo[key]);
            }
        }

        public string ExportToCommonJson()
        {
            var eo = EasyObject.FromObject(this.ExportToPlainObject());
            return eo.ToJson(indent: true);
        }

        public void ImportFromCommonJson(string x)
        {
            var eo = EasyObject.FromJson(x);
            var po = eo.ToObject();
            this.ImportFromPlainObject(po);
        }
    }
}
#endif
