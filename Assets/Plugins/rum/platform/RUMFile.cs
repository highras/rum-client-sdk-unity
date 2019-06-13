using System;
using System.IO;
using System.Text;

using UnityEngine;

namespace com.rum {

    public class RUMFile {

        private const string FILE_PRE = "rumlog_";
        private const string STORAGE_FILE = "rumlog_storage";

        private static RUMFile instance;
        private static object lock_obj = new object();

        public static RUMFile Instance {

            get{

                if (instance == null) {

                    lock (lock_obj) {

                        if (instance == null) {

                            instance = new RUMFile();
                        }
                    }
                }

                return instance;
            }
        }

        public class Result {

            public bool success;
            public object content;
        }

        private int _read_index;
        private string _directory_path;

        private static readonly System.Object locker = new System.Object();

        public void Init(int pid) {

            this._directory_path = Application.streamingAssetsPath + "/rum_events_" + pid;
            
            #if UNITY_EDITOR
            this._directory_path = Application.streamingAssetsPath + "/rum_events_" + pid;
            #elif UNITY_IPHONE
            this._directory_path = Application.persistentDataPath + "/rum_events_" + pid;
            #elif UNITY_ANDROID
            this._directory_path = Application.persistentDataPath + "/rum_events_" + pid;
            #endif

            if (Directory.Exists(this._directory_path) == false) {

                Directory.CreateDirectory(this._directory_path);
            } 
        }

        public RUMFile.Result WriteRumLog(int index, byte[] content) {

            string path = this._directory_path + "/" + FILE_PRE + index;
            return this.WriteFile(path, content);
        }

        public RUMFile.Result ReadRumLog() {

            int index = 0;
            string path = null;

            while(index < 20) {

                this._read_index = (this._read_index + 1) % RUMConfig.LOCAL_FILE_COUNT;
                path = this._directory_path + "/" + FILE_PRE + this._read_index;

                if (new FileInfo(path).Exists) {

                    break;
                }

                index++;
            }

            RUMFile.Result res = this.ReadFile(path, true);

            if (!res.success) {

                this.DeleteFile(path);
            }

            return res;
        }

        public RUMFile.Result WriteStorage(byte[] content) {

            string path = this._directory_path + "/" + STORAGE_FILE;
            return this.WriteFile(path, content);
        }

        public RUMFile.Result ReadStorage() {

            string path = this._directory_path + "/" + STORAGE_FILE;
            RUMFile.Result res = this.ReadFile(path, false);

            if (!res.success) {

                this.DeleteFile(path);
            }

            return res;
        }

        public RUMFile.Result ClearRumLog() {

            return this.DeleteDirectory(this._directory_path); 
        }

        public RUMFile.Result WriteFile(string path, string content, Encoding encoding) {

            lock(locker) {

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

            lock(locker) {

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

            lock(locker) {

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

            lock(locker) {

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

            lock(locker) {

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

            lock(locker) {

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
