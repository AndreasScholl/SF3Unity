using Model;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

namespace Shiningforce
{
    public class CharacterData
    {
        string _name;

        MemoryManager _memory;

        const int MEMORY_CHRBASE = 0x280400;

        public bool ReadFile(string filePath)
        {
            if (File.Exists(filePath) == false)
            {
                return false;
            }

            // extract name from path
            _name = Util.FileSystemHelper.GetFileNameWithoutExtensionFromPath(filePath);
            Debug.Log("Map: " + _name);

            _memory = new MemoryManager(0x200000);
            _memory.LoadFile(filePath, MEMORY_CHRBASE);

            byte[] data = Decompression.DecompressData(_memory.Data, (MEMORY_CHRBASE + 0x90) - _memory.Base);

            File.WriteAllBytes(Directory.GetCurrentDirectory() + "/textures/" + _name + ".bin", data);

            return true;
        }
    }
}