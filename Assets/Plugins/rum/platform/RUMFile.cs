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

        private class FileLocker {

            public int Status = 0;
        }

        private static int LOCAL_FILE_MAX = 300; 

        private const string FILE_PRE = "rumlog_";
        private const string STORAGE_FILE = "rumlog_storage";

        private int _read_index;
        private string _directory_path;

        private FileLocker file_locker = new FileLocker();

        public RUMFile(int pid) {

            this._directory_path = this.GetSecureDataPath() + "/rum_events_" + pid;
            this.InitDirectory();
        }

        private void InitDirectory () {

            try {

                if (Directory.Exists(this._directory_path) == false) {

                    Directory.CreateDirectory(this._directory_path);
                } 
            } catch (Exception ex) {

                ErrorRecorderHolder.recordError(ex);
            }
        }

        private string GetSecureDataPath() {

            string secureDataPath = Application.temporaryCachePath;

            #if !UNITY_EDITOR && UNITY_ANDROID
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var getFilesDir = currentActivity.Call<AndroidJavaObject>("getFilesDir")) {

                secureDataPath = getFilesDir.Call<string>("getCanonicalPath");
            }

            #elif !UNITY_EDITOR && UNITY_IPHONE
            secureDataPath = Application.persistentDataPath;
            #endif

            return secureDataPath;
        }

        public RUMFile.Result WriteRumLog(int index, byte[] content) {

            string path = this._directory_path + "/" + FILE_PRE + index;
            return this.WriteFile(path, content);
        }

        public RUMFile.Result ReadRumLog() {

            int index = 0;
            string path = null;

            while(index < 20) {

                this._read_index = (this._read_index + 1) % LOCAL_FILE_MAX;
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

            lock(file_locker) {

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

            lock(file_locker) {

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

            lock(file_locker) {

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

            lock(file_locker) {

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

            lock(file_locker) {

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

            lock(file_locker) {

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
