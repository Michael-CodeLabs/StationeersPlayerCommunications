using System;
using System.IO;
using System.Reflection;

namespace Assets.Scripts.Networking
{
    public static class RocketBinaryReaderExtensions
    {
        private static readonly FieldInfo _readerField = typeof(RocketBinaryReader)
            .GetField("_reader", BindingFlags.NonPublic | BindingFlags.Instance);

        public static byte[] ReadBytes(this RocketBinaryReader reader, int length)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (_readerField == null)
                throw new InvalidOperationException("Could not find _reader field in RocketBinaryReader.");

            // Get the private _reader field value
            var binaryReader = _readerField.GetValue(reader) as BinaryReader;
            if (binaryReader == null)
                throw new InvalidOperationException("Failed to retrieve BinaryReader instance.");

            // Use the optimized ReadBytes method
            return binaryReader.ReadBytes(length);
        }
    }
}