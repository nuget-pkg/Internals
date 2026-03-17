using LiteDB;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if GLOBAL_SYS
namespace Global {
    public class LiteDBProps :
        IExportToPlainObject,
        IImportFromPlainObject,
        IExportToCommonJson,
        IImportFromCommonJson {
        public class Prop {
            public long Id {
                get; set;
            }
            public string? Name {
                get; set;
            }
            public object? Data {
                get; set;
            }
        }
        private string? filePath = null;
        public LiteDBProps(FileInfo fi) {
            filePath = fi.FullName;
            Sys.PrepareForFile(filePath);
            using LiteDatabase connection = new LiteDatabase(new ConnectionString(filePath) {
                Connection = ConnectionType.Shared
            });
            ILiteCollection<Prop> collection = connection.GetCollection<Prop>("properties");
            // Nameをユニークインデックスにする
            collection.EnsureIndex(x => x.Name, true);
        }
        public LiteDBProps(DirectoryInfo di) {
            filePath = Path.Combine(di.FullName, "properties.litedb");
            Sys.PrepareForFile(filePath);
            using LiteDatabase connection = new LiteDatabase(new ConnectionString(filePath) {
                Connection = ConnectionType.Shared
            });
            ILiteCollection<Prop> collection = connection.GetCollection<Prop>("properties");
            // Nameをユニークインデックスにする
            collection.EnsureIndex(x => x.Name, true);
        }
        public LiteDBProps(string orgName, string appNam) : this(new DirectoryInfo(Dirs.ProfilePath(orgName, appNam))) {
        }
        public LiteDBProps(string appNam) : this(new DirectoryInfo(Dirs.ProfilePath(appNam))) {
        }
        public EasyObject Get(string name, object? fallback = null) {
            using LiteDatabase connection = new LiteDatabase(new ConnectionString(filePath) {
                Connection = ConnectionType.Shared
            });
            connection.BeginTrans();
            ILiteCollection<Prop> collection = connection.GetCollection<Prop>("properties");
            Prop? result = collection.Find(x => x.Name == name).FirstOrDefault();
            connection.Commit();
            if (result == null) {
                return EasyObject.FromObject(fallback);
            }
            return EasyObject.FromObject(result.Data);
        }
        public void Put(string name, dynamic? data) {
            data = EasyObject.FromObject(data).ExportToPlainObject();
            using LiteDatabase connection = new LiteDatabase(new ConnectionString(filePath) {
                Connection = ConnectionType.Shared
            });
            connection.BeginTrans();
            ILiteCollection<Prop> collection = connection.GetCollection<Prop>("properties");
            Prop? result = collection.Find(x => x.Name == name).FirstOrDefault();
            if (result == null) {
                result = new Prop {
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
        public void Delete(string name) {
            using LiteDatabase connection = new LiteDatabase(new ConnectionString(filePath) {
                Connection = ConnectionType.Shared
            });
            connection.BeginTrans();
            ILiteCollection<Prop> collection = connection.GetCollection<Prop>("properties");
            Prop? result = collection.Find(x => x.Name == name).FirstOrDefault();
            if (result == null) {
                connection.Commit();
                return;
            }
            collection.Delete(result.Id);
            connection.Commit();
        }
        public void DeleteAll() {
            using LiteDatabase connection = new LiteDatabase(new ConnectionString(filePath) {
                Connection = ConnectionType.Shared
            });
            connection.BeginTrans();
            ILiteCollection<Prop> collection = connection.GetCollection<Prop>("properties");
            collection.DeleteAll();
            connection.Commit();
        }
        public List<string> Keys {
            get {
                using LiteDatabase connection = new LiteDatabase(new ConnectionString(filePath) {
                    Connection = ConnectionType.Shared
                });
                connection.BeginTrans();
                ILiteCollection<Prop> collection = connection.GetCollection<Prop>("properties");
                IEnumerable<Prop> all = collection.FindAll();
                IEnumerable<string?> keys = all.Select(_ => _.Name);
                List<string> list = [];
                foreach (string? key in keys) {
                    list.Add(key!);
                }
                connection.Commit();
                return list;
            }
        }
        public override string ToString() {
            return EasyObject.FromObject(this).ToJson(indent: true, keyAsSymbol: true);
        }

        public object? ExportToPlainObject() {
            List<string> keys = Keys;
            EasyObject eo = EasyObject.NewObject();
            foreach (string key in keys) {
                eo[key] = Get(key);
            }
            return eo.ToObject();
        }
        public void ImportFromPlainObject(object? x) {
            EasyObject eo = EasyObject.FromObject(x);
            if (!eo.IsObject) {
                Sys.Crash("LiteDBProps.ImportFromPlainObject(): argumet is not object/dictionary!");
            }
            DeleteAll();
            List<string> keys = eo.Keys;
            foreach (string key in keys) {
                Put(key, eo[key]);
            }
        }
        public string ExportToCommonJson() {
            EasyObject eo = EasyObject.FromObject(ExportToPlainObject());
            return eo.ToJson(indent: true);
        }
        public void ImportFromCommonJson(string x) {
            EasyObject eo = EasyObject.FromJson(x);
            dynamic? po = eo.ToObject();
            ImportFromPlainObject(po);
        }
    }
}
#endif
