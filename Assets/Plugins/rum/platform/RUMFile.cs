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

        private static int LOCAL_FILE_MAX = 300; 

        private const string FILE_PRE = "rumlog_";
        private const string STORAGE_FILE = "rumlog_storage";

        private int _read_index;
        private object index_locker = new object();
        private object file_locker = new object();

        private string _secureDataPath;

        public RUMFile(int pid) {

            this.InitDirectory(RUMPlatform.SecureDataPath + "/rum_events_" + pid);
        }

        private void InitDirectory (string secureDataPath) {

            try {

                if (Directory.Exists(secureDataPath) == false) {

                    Directory.CreateDirectory(secureDataPath);
                } 

                this._secureDataPath = secureDataPath;
            } catch (Exception ex) {

                ErrorRecorderHolder.recordError(ex);
            }
        }

        public RUMFile.Result WriteRumLog(int index, byte[] content) {

            string path = this._secureDataPath + "/" + FILE_PRE + index;
            return this.WriteFile(path, content);
        }

        public RUMFile.Result ReadRumLog() {

            string path = null;

            lock (index_locker) {

                int index = 0;

                while(index < 20) {

                    this._read_index = (this._read_index + 1) % LOCAL_FILE_MAX;
                    path = this._secureDataPath + "/" + FILE_PRE + this._read_index;

                    if (new FileInfo(path).Exists) {

                        break;
                    }

                    index++;
                }
            }

            RUMFile.Result res = this.ReadFile(path, true);

            if (!res.success) {

                this.DeleteFile(path);
            }

            return res;
        }

        public RUMFile.Result WriteStorage(byte[] content) {

            string path = this._secureDataPath + "/" + STORAGE_FILE;
            return this.WriteFile(path, content);
        }

        public RUMFile.Result ReadStorage() {

            string path = this._secureDataPath + "/" + STORAGE_FILE;
            RUMFile.Result res = this.ReadFile(path, false);

            if (!res.success) {

                this.DeleteFile(path);
            }

            return res;
        }

        public RUMFile.Result ClearRumLog() {

            return this.DeleteDirectory(this._secureDataPath); 
        }

        public RUMFile.Result WriteFile(string path, string content, Encoding encoding) {

            lock (file_locker) {

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

        public RUMFile.Result WriteFile(string path, byte[] content) {

            lock (file_locker) {

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

        public RUMFile.Result ReadFile(string path, bool delete, Encoding encoding) {

            lock (file_locker) {

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

        public RUMFile.Result ReadFile(string path, bool delete) {

            lock (file_locker) {

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

        public RUMFile.Result DeleteFile(string path) {

            lock (file_locker) {

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

        public RUMFile.Result DeleteDirectory(string path) {

            lock (file_locker) {

                try {

                    if (Directory.Exists(path)) {

                        DirectoryInfo info = new DirectoryInfo(path);

                        this.DeleteAllFiles(info);
                        info.Delete();
                    }
                } catch(Exception ex) {

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
    }
}
