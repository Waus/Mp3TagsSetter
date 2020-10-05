namespace Mp3TagsSetter
{
    public class FileData
    {
        public uint trackNumber { get; set; }
        public string trackName { get; set; }
        public string albumDirectoryName { get; set; }
        public uint albumYear { get; set; }
        public string albumName { get; set; }
        public string[] artists { get; set; }
        public string[] genres { get; set; }
    }
}
