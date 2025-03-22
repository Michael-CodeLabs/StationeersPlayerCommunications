using System;
using System.IO;
using System.Reflection;

namespace Assets.Scripts.Networking
{
    public static class RocketBinaryWriterExtensions
    {
        private static readonly FieldInfo _writerField = typeof(RocketBinaryWriter)
            .GetField("_writer", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void WriteBytes(this RocketBinaryWriter writer, byte[] message)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (_writerField == null)
                throw new InvalidOperationException("Could not find _writer field in RocketBinaryWriter.");

            // Get the private _writer field value
            var binaryWriter = _writerField.GetValue(writer) as BinaryWriter;
            if (binaryWriter == null)
                throw new InvalidOperationException("Failed to retrieve BinaryWriter instance.");

            // Write the bytes
            binaryWriter.Write(message);
        }
    }
}