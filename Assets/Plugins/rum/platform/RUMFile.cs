using System;
using System.IO;
using System.Text;
using com.fpnn;

using UnityEngine;

namespace com.rum {

    public class RUMFile {

        public class Result {

            public bool success;
            public object content;
        }

        private const string FILE_PRE = "rumlog_";
        private const string STORAGE_FILE = "rumlog_storage";

        private static object File_Locker = new object();

        private bool _debug;
        private string _secureDataPath;

        public RUMFile(int pid, bool debug) {
            this._debug = debug;
            this.InitDirectory(String.Format("{0}/{1}{2}", RUMPlatform.SecureDataPath, "rum_events_", pid));
        }

        private void InitDirectory (string secureDataPath) {
            if (this._debug) {
                Debug.Log("[RUM] local path: " + secureDataPath);
            }

            try {
                if (Directory.Exists(secureDataPath) == false) {
                    Directory.CreateDirectory(secureDataPath);
                }

                this._secureDataPath = secureDataPath;
            } catch (Exception ex) {
                ErrorRecorderHolder.recordError(ex);
            }
        }

        public RUMFile.Result SaveRumLog(int index, byte[] content) {
            string path = String.Format("{0}/{1}{2}", this._secureDataPath, FILE_PRE, index);
            return this.WriteFile(path, content);
        }

        public RUMFile.Result LoadRumLog() {
            string name = this.GetEarlierFile(this._secureDataPath);

            if (string.IsNullOrEmpty(name)) {
                return new RUMFile.Result() {
                    success = false,
                    content = new Exception("fail to load")
                };
            }

            string path = String.Format("{0}/{1}", this._secureDataPath, name);
            RUMFile.Result res = this.ReadFile(path, true);

            if (!res.success) {
                this.DeleteFile(path);
            }

            return res;
        }

        public RUMFile.Result SaveStorage(byte[] content) {
            string path = String.Format("{0}/{1}", this._secureDataPath, STORAGE_FILE);
            return this.WriteFile(path, content);
        }

        public RUMFile.Result LoadStorage() {
            string path = String.Format("{0}/{1}", this._secureDataPath, STORAGE_FILE);
            RUMFile.Result res = this.ReadFile(path, false);

            if (!res.success) {
                this.DeleteFile(path);
            }

            return res;
        }

        public RUMFile.Result ClearAllFile() {
            return this.DeleteDirectory(this._secureDataPath);
        }

        private RUMFile.Result WriteFile(string path, string content, Encoding encoding) {
            lock (File_Locker) {
                try {
                    using (StreamWriter writer = new StreamWriter(path, false, encoding)) {
                        writer.WriteLine(content);
                    }
                } catch (Exception ex) {
                    return new RUMFile.Result() {
                        success = false,
                        content = ex
                    };
                }

                return new RUMFile.Result() {
                    success = true,
                    content = content
                };
            }
        }

        private RUMFile.Result WriteFile(string path, byte[] content) {
            lock (File_Locker) {
                try {
                    using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write)) {
                        fs.Write(content, 0, content.Length);
                    }
                } catch (Exception ex) {
                    return new RUMFile.Result() {
                        success = false,
                        content = ex
                    };
                }

                return new RUMFile.Result() {
                    success = true,
                    content = content
                };
            }
        }

        private RUMFile.Result ReadFile(string path, bool delete, Encoding encoding) {
            lock (File_Locker) {
                string content;

                try {
                    FileInfo info = new FileInfo(path);

                    if (!info.Exists) {
                        return new RUMFile.Result() {
                            success = false,
                            content = new Exception("no file")
                        };
                    }

                    using (StreamReader reader = new StreamReader(info.OpenRead(), encoding)) {
                        content = reader.ReadToEnd();
                    }

                    if (delete) {
                        info.Delete();
                    }
                } catch (Exception ex) {
                    return new RUMFile.Result() {
                        success = false,
                        content = ex
                    };
                }

                return new RUMFile.Result() {
                    success = true,
                    content = content
                };
            }
        }

        private RUMFile.Result ReadFile(string path, bool delete) {
            lock (File_Locker) {
                byte[] content;

                try {
                    FileInfo info = new FileInfo(path);

                    if (!info.Exists) {
                        return new RUMFile.Result() {
                            success = false,
                            content = new Exception("no file")
                        };
                    }

                    using (FileStream fs = info.OpenRead()) {
                        content = new byte[fs.Length];
                        fs.Read(content, 0, content.Length);
                    }

                    if (delete) {
                        info.Delete();
                    }
                } catch (Exception ex) {
                    return new RUMFile.Result() {
                        success = false,
                        content = ex
                    };
                }

                return new RUMFile.Result() {
                    success = true,
                    content = content
                };
            }
        }

        private RUMFile.Result DeleteFile(string path) {
            lock (File_Locker) {
                try {
                    FileInfo info = new FileInfo(path);

                    if (info.Exists) {
                        info.Delete();
                    }
                } catch (Exception ex) {
                    return new RUMFile.Result() {
                        success = false,
                        content = ex
                    };
                }

                return new RUMFile.Result() {
                    success = true,
                    content = "delete file success"
                };
            }
        }

        private RUMFile.Result DeleteDirectory(string path) {
            lock (File_Locker) {
                try {
                    if (Directory.Exists(path)) {
                        DirectoryInfo info = new DirectoryInfo(path);
                        this.DeleteAllFiles(info);
                        info.Delete();
                    }
                } catch (Exception ex) {
                    return new RUMFile.Result() {
                        success = false,
                        content = ex
                    };
                }

                return new RUMFile.Result() {
                    success = true,
                    content = "delete directory success"
                };
            }
        }

        private void DeleteAllFiles(DirectoryInfo info) {
            FileInfo[] fis = info.GetFiles();

            foreach (FileInfo fi in fis) {
                fi.Delete();
            }

            DirectoryInfo[] dis = info.GetDirectories();

            foreach (DirectoryInfo di in dis) {
                DeleteAllFiles(di);
                di.Delete();
            }
        }

        private string GetEarlierFile(string path) {
            lock (File_Locker) {
                string name = null;

                try {
                    if (Directory.Exists(path)) {
                        DirectoryInfo info = new DirectoryInfo(path);
                        FileInfo[] fis = info.GetFiles();

                        if (fis != null && fis.Length > 1) {
                            Array.Sort(fis, (x, y) => {
                                return x.LastWriteTimeUtc.CompareTo(y.LastWriteTimeUtc);
                            });
                            name = fis[0].Name;

                            if (STORAGE_FILE == name) {
                                name = fis[1].Name;
                            }
                        }
                    }
                } catch (Exception ex) {
                    ErrorRecorderHolder.recordError(ex);
                }

                return name;
            }
        }
    }
}
