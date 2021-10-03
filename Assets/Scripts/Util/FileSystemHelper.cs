using System;
using System.Collections;
using System.IO;

namespace Util
{
    public static class FileSystemHelper
    {
        public static bool DirectoryExistsAndHasFiles(string directory)
        {
            if (Directory.Exists(directory))
            {
                string[] dirFiles = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
                return (Directory.Exists(directory) && dirFiles != null && dirFiles.Length != 0);
            }
            else
            {
                return false;
            }
        }

        public static string[] GetFileListFromFolder(string pathToFolder, string fileType, SearchOption so = SearchOption.AllDirectories)
        {
            if (pathToFolder == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(fileType))
            {
                fileType = "*";
            }

            if (fileType.StartsWith("*") == false)
            {
                fileType = "*" + fileType;
            }


            return Directory.GetFiles(pathToFolder, fileType, so);
        }

        public static string GetFileNameWithoutExtensionFromPath(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        public static string GetFileNameFromPath(string path)
        {
            return Path.GetFileName(path);
        }

        public static string SearchFilePath(string folderPath, string filename)
        {
            string searchedFilePath = null;
            var searchResults = Directory.GetFiles(folderPath, filename, SearchOption.AllDirectories);

            if (searchResults != null && searchResults.Length > 0)
            {
                searchedFilePath = searchResults[0];
            }
            else
            {
                searchResults = Directory.GetDirectories(folderPath, filename, SearchOption.AllDirectories);
                if (searchResults != null && searchResults.Length > 0)
                {
                    searchedFilePath = searchResults[0] + "/";
                }
            }

            return searchedFilePath;
        }

        public static void CreateDirectory(string directoryPath, bool overwrite = false)
        {
            if (overwrite || Directory.Exists(directoryPath) == false)
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public static void FileCopy(string source, string destination, bool overwrite = true)
        {
            File.Copy(source, destination, overwrite);
        }

        public static string GetDirectoryWhichContainsFileType(string sourceFolder, string[] v, bool topDirectories = true)
        {
            string path = null;
            foreach (string searchString in v)
            {
                path = GetDirectoryWhichContainsFileType(sourceFolder, searchString, topDirectories);
                if (path != null)
                {
                    break;
                }
            }
            return path;
        }

        public static string GetDirectoryWhichContainsFileType(string sourceFolder, string v, bool topDirectories = true)
        {
            string path = null;
            string[] fileList = GetFileListFromFolder(sourceFolder, v, SearchOption.AllDirectories);
            if (fileList != null && fileList.Length > 0)
            {
                int id = 0;
                bool conditionMet = false;

                do
                {
                    if (id >= fileList.Length)
                    {
                        path = null;
                        break;
                    }
                    path = Path.GetDirectoryName(fileList[id]);
                    id++;

                    if (topDirectories == false)
                    {
                        conditionMet = Path.Equals(sourceFolder, path);
                    }
                    else
                    {
                        conditionMet = Path.Equals(sourceFolder, Path.GetDirectoryName(path)) == false;
                    }
                } while (conditionMet);

                if (path != null && path.EndsWith("/") == false && path.EndsWith("\\") == false)
                {
                    path += "/";
                }
            }

            return path;
        }

        public static bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public static string ClearDirectory(string folderPath)
        {
            string error = "";
            if (Directory.Exists(folderPath))
            {
                try
                {
                    Directory.Delete(folderPath, true);
                }
                catch (Exception e)
                {

                    error = e.Message;
                }
            }
            CreateDirectory(folderPath);

            return error;
        }

        public static void ClearFolder(string FolderName)
        {
            DirectoryInfo dir = new DirectoryInfo(FolderName);

            foreach (FileInfo fi in dir.GetFiles())
            {
                fi.IsReadOnly = false;
                fi.Delete();
            }

            foreach (DirectoryInfo di in dir.GetDirectories())
            {
                ClearFolder(di.FullName);
                di.Delete();
            }
        }

        public static void ClearFilesFromDirectory(string directory, string fileEnding)
        {
            string[] filestodelete = GetFileListFromFolder(directory, fileEnding);

            foreach (string filePath in filestodelete)
            {
                File.Delete(filePath);
            }
        }

        public static bool IsValidFilename(string fileName)
        {
            return (string.IsNullOrEmpty(fileName) == false)
            && fileName.StartsWith(".") == false
            && (GetInvalidFileNameCharacterIndex(fileName) < 0);
        }

        public static string GetValidFileName(string fileName, char replaceChar = '_')
        {
            int invalidChar = GetInvalidFileNameCharacterIndex(fileName);
            while (invalidChar >= 0)
            {
                fileName = fileName.Replace(fileName[invalidChar], replaceChar);
                invalidChar = GetInvalidFileNameCharacterIndex(fileName);
            }

            return fileName;
        }

        private static int GetInvalidFileNameCharacterIndex(string potentialFilename)
        {
            int id = -1;
            id = potentialFilename.IndexOfAny(Path.GetInvalidFileNameChars());
            if (id == -1)
            {
                id = potentialFilename.IndexOfAny(new char[] { '&', '\'', '.', ';' });
            }

            return id;
        }

        static public string[] GetFiles(string sourceFolder, string filter, SearchOption searchOption)
        {
            // ArrayList will hold all file names
            ArrayList alFiles = new ArrayList();

            // Create an array of filter string
            string[] MultipleFilters = filter.Split('|');

            // for each filter find mathing file names
            foreach (string FileFilter in MultipleFilters)
            {
                // add found file names to array list
                alFiles.AddRange(Directory.GetFiles(sourceFolder, FileFilter, searchOption));
            }

            // returns string array of relevant file names
            return (string[])alFiles.ToArray(typeof(string));
        }
    }
}
