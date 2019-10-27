using System;
using System.Collections.Generic;
using System.IO;
using custom_message_based_implementation;
using custom_message_based_implementation.interfaces;
using custom_message_based_implementation.model;
using message_based_communication.model;
using message_based_communication.module;
using NLog;
using File = custom_message_based_implementation.model.File;

namespace file_servermodule
{
    public class FileServermodule : BaseServerModule, IFileServermodule
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public override string CALL_ID_PREFIX => "FILE_SM_CALL_ID_";

        public FileServermodule(ModuleType moduleType) : base(moduleType)
        {

        }

        public override void HandleRequest(BaseRequest message)
        {
            object responsePayload;

            switch (message)
            {
                case RequestGetListOfFiles reqGetListOfFiles:
                    responsePayload = GetListOfFiles(reqGetListOfFiles.PrimaryKey);
                    break;
                case RequestDownloadFile requestDownloadFile:
                    responsePayload = DownloadFile(requestDownloadFile.FileName, requestDownloadFile.PrimaryKey);
                    break;
                case RequestUploadFile requestUploadFile:
                    responsePayload = null;
                    UploadFile(requestUploadFile.File, requestUploadFile.PrimaryKey, requestUploadFile.Overwrite);
                    break;
                case RequestRenameFile requestRenameFile:
                    responsePayload = null;
                    RenameFile(requestRenameFile.OldFileName, requestRenameFile.NewFileName, requestRenameFile.PrimaryKey);
                    break;
                default:
                    throw new Exception("Received message that I don't know how to handle");
            }

            var response = GenerateResponseBasedOnRequestAndPayload(message, responsePayload);
            SendResponse(response);
        }

        public List<FileName> GetListOfFiles(PrimaryKey pk)
        {
            Logger.Debug("GetListOfFiles; Primary key: " + pk.TheKey);
            var path = AppDomain.CurrentDomain.BaseDirectory + pk.TheKey + Path.DirectorySeparatorChar;
            if (!Directory.Exists(path)) { 
                Logger.Debug("Directory does not exist, returning empty file name list: " + path);
                return new List<FileName>();
            }

            var fileNames = new List<FileName>();
            var fileEntries = Directory.GetFiles(path);
            foreach (var fullFileName in fileEntries)
            {
                var fileName = Path.GetFileName(fullFileName);
                fileNames.Add(new FileName{FileNameProp = fileName});
                Logger.Debug("File added to the list: " + fileName);
            }

            Logger.Debug("Returning file name list containing " + fileNames.Count + " items");
            return fileNames;
        }

        public void UploadFile(File file, PrimaryKey pk, bool overwrite)
        {
            Logger.Debug("UploadFile; File name: " + file.FileName.FileNameProp + "; Primary key: " + pk.TheKey + "; Overwrite: " + overwrite);
            var path = AppDomain.CurrentDomain.BaseDirectory + pk.TheKey + Path.DirectorySeparatorChar + file.FileName.FileNameProp;
            
            new FileInfo(path).Directory?.Create(); // If the directory already exists, this method does nothing.
            if (overwrite)
            {
                Logger.Debug("Overwriting " + path);
                System.IO.File.WriteAllBytes(path, file.FileData);
            }
            else
            {
                if (!System.IO.File.Exists(path))
                {
                    Logger.Debug("Writing " + path);
                    System.IO.File.WriteAllBytes(path, file.FileData);
                }
                else
                {
                    Logger.Debug("File " + path + " already exists and will not be overwritten");
                }
            }
        }

        public File DownloadFile(FileName fileName, PrimaryKey pk)
        {
            Logger.Debug("DownloadFile; File name: " + fileName.FileNameProp + "; Primary key: " + pk.TheKey);
            var path = AppDomain.CurrentDomain.BaseDirectory + pk.TheKey + Path.DirectorySeparatorChar + fileName.FileNameProp;

            if (System.IO.File.Exists(path))
            {
                return new File
                {
                    FileData = System.IO.File.ReadAllBytes(path),
                    FileName = fileName
                };
            }
            
            // todo maybe return null and log?
            throw new ArgumentException("There is no file in: " + path);
        }

        public void RenameFile(FileName oldFileName, FileName newFileName, PrimaryKey pk)
        {
            Logger.Debug("RenameFile; Old file name: " + oldFileName.FileNameProp + "; New file name: " + newFileName.FileNameProp + "; Primary key: " + pk.TheKey);
            var basePath = AppDomain.CurrentDomain.BaseDirectory + pk.TheKey + Path.DirectorySeparatorChar;
            var oldPath = basePath + oldFileName.FileNameProp;
            var newPath = basePath + newFileName.FileNameProp;

            if (System.IO.File.Exists(oldPath))
            {
                System.IO.File.Move(oldPath, newPath);
            } else {
                throw new ArgumentException("There is no file in: " + oldPath);
            }
        }
    }
}