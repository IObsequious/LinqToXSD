// -----------------------------------------------------------------------
// <copyright file="update.cs" company="Ollon, LLC">
//     Copyright (c) 2018 Ollon, LLC. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Text;

namespace XObjectsGenerator
{
    internal class Update : IDisposable
    {
        public readonly TextWriter Writer;
        private readonly MemoryStream stream = new MemoryStream();
        private readonly string filename;
        private readonly Encoding encoding;

        public Update(string filename, Encoding encoding)
        {
            this.filename = filename;
            this.encoding = encoding;
            Writer = new StreamWriter(stream, encoding);
        }

        public bool Close()
        {
            Writer.Close();
            var memoryString = new StreamReader(
                new MemoryStream(stream.ToArray()),
                encoding).ReadToEnd();
            var fileString = "";
            using (var file = new FileStream(
                filename,
                FileMode.OpenOrCreate))
            {
                using (var fileReader = new StreamReader(file))
                {
                    fileString = fileReader.ReadToEnd();
                }
            }

            if (memoryString != fileString)
            {
                using (
                    var file =
                        new FileStream(filename, FileMode.Create))
                {
                    using (
                        var fileWriter =
                            new StreamWriter(file, encoding))
                    {
                        fileWriter.Write(memoryString);
                        file.SetLength(file.Position);
                    }
                }

                return true;
            }

            return false;
        }

        public void Dispose()
        {
            Close();
        }
    }
}
